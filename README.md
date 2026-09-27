# JobMatch

An ASP.NET Core API that reads job postings, turns each one into structured data with an LLM, and ranks them against my CV. For a posting I pick, it writes a gap report (what I have, what I'm missing), and every claim in it has to quote both the posting and the CV.

I built it to learn how to use LLM APIs properly from C#: structured outputs, validating what the model says instead of trusting it, embeddings with pgvector, and a small RAG step for the gap report.

## What it does

- **Ingest** a posting from pasted text or a URL. Duplicates are caught with a hash of the normalized text.
- **Extract** a strict JSON record with an LLM (Groq): seniority, contract type (CDI, CDD...), remote policy, city, salary, languages, and skills.
- **Validate** the result in plain C#. Unknown enum values, salary min > max and the like are sent back to the model once with the errors. Every skill must come with a quote from the posting, and a skill whose quote is not really in the text is dropped.
- **Embed** postings and CV sections with a local model (Ollama, `nomic-embed-text`) and store them in PostgreSQL with pgvector (HNSW index, cosine distance).
- **Rank** postings for a CV without an LLM: SQL filters, then skill overlap plus the best cosine similarity between the posting and any CV section. The score breakdown is returned so you can see why a posting ranks where it does.
- **Gap report (RAG):** retrieve the 3 CV sections closest to the posting, ask the LLM for met / partially met / missing requirements with quotes, then check every quote in C#. Claims whose quotes can't be found go into an `unverified` list instead of being shown as facts.

## How it works

```text
POST /postings/manual ──► Raw ──► (worker) extract with Groq ──► validate + check evidence ──► Extracted
                                                                                                  │
                                              Embedded ◄── embed full text + skills (Ollama) ◄───┘
                     anything that throws ──► Failed (error saved on the posting)
```

The API never waits for the LLM. New postings go into an in-memory `Channel<Guid>` and a `BackgroundService` processes them one by one. On startup, postings that were not finished are queued again.

The raw text is always saved first, so extraction can be run again later (`/postings/{id}/reprocess`) with a better prompt or model without fetching anything again. Each extraction stores the model and the prompt version it came from.

### Projects

| Project | What's in it |
| --- | --- |
| `JobMatch.Core` | Entities, the extraction schema, validation, skill aliases, quote checking, CV splitting, scoring. No NuGet packages, no EF, no HTTP. |
| `JobMatch.Infrastructure` | EF Core + Npgsql + pgvector, migrations, Groq and Ollama clients, extraction, embeddings, matching, gap reports. |
| `JobMatch.Api` | Minimal API endpoints, the processing queue and the background worker. |
| `JobMatch.Tests` | xUnit + Shouldly. Unit tests use a fake `IChatClient`, integration tests run against a real pgvector container (Testcontainers). |

Prompts are markdown files in [`prompts/`](prompts/), not strings in the code. The file name is the version (`extraction.v1`).

## Stack

| | |
| --- | --- |
| Runtime | .NET 10, C# (nullable on, warnings as errors) |
| API | ASP.NET Core Minimal APIs, OpenAPI + Scalar |
| Database | PostgreSQL 17 + pgvector, EF Core 10 |
| LLM | Groq (`openai/gpt-oss-120b`) through `Microsoft.Extensions.AI` (`IChatClient`) and the OpenAI .NET SDK, since Groq's API is OpenAI compatible |
| Embeddings | Ollama with `nomic-embed-text` (768 dimensions) through OllamaSharp (`IEmbeddingGenerator`) |
| Tests | xUnit, Shouldly, Testcontainers |
| CI | GitHub Actions: build, test, `dotnet format` check |

Because everything goes through the `Microsoft.Extensions.AI` interfaces, switching to OpenAI, Azure OpenAI or a local model is a change in `DependencyInjection.cs` only.

## Running it

You need the .NET 10 SDK, Docker, and a free Groq API key from [console.groq.com](https://console.groq.com).

```bash
# postgres + pgvector, ollama, and a one-off container that pulls the embedding model
docker compose up -d

# the key is stored in user secrets, outside the repo
dotnet user-secrets set "Groq:ApiKey" "<your key>" --project src/JobMatch.Api

dotnet run --project src/JobMatch.Api
```

Migrations run on startup. The API docs are at <http://localhost:5132/scalar>.

The database user and password in `docker-compose.yml` and `appsettings.Development.json` are only for the local container.

## Endpoints

| Method | Path | |
| --- | --- | --- |
| GET | `/health` | API + database check |
| POST | `/postings/manual` | `{ "text": "..." }` or `{ "url": "..." }`. Returns 202 and the id (200 with `duplicate: true` if already known) |
| GET | `/postings` | Filters: `status`, `city`, `contract`, `remote`, `skill`, `since`, plus `page` / `pageSize` |
| GET | `/postings/{id}` | Raw text, extraction, skills with their evidence quotes |
| POST | `/postings/{id}/reprocess` | Throw away the extraction and run the pipeline again |
| POST | `/cv` | `{ "name": "...", "text": "..." }`. Splits the CV into sections and embeds them |
| GET | `/cv/{id}/matches` | Ranked postings with score breakdown. Filters: `city`, `remote`, `contract`, `seniority`, `since`, `limit` |
| POST | `/cv/{id}/matches/{postingId}/gap` | Gap report (generated once, then cached) |

Example:

```bash
curl -X POST http://localhost:5132/postings/manual \
  -H "Content-Type: application/json" \
  -d '{"text": "Développeur .NET Junior (H/F) - CDI - Lyon ... Bonne connaissance de SQL Server ... Docker est un plus"}'

curl http://localhost:5132/postings/<id>
```

Part of a gap report for a junior .NET posting and a made-up test CV:

```json
{
  "met": [
    { "requirement": "C#", "posting_quote": "Première expérience (stage ou alternance acceptés) en C#",
      "cv_quote": "built a REST API in C# with ASP.NET Core", "note": "..." }
  ],
  "partially_met": [
    { "requirement": "SQL Server", "posting_quote": "Bonne connaissance de SQL Server", "cv_quote": "SQL", "note": "..." }
  ],
  "missing": [
    { "requirement": "Revues de code", "posting_quote": "Participer aux revues de code", "cv_quote": null, "note": "..." }
  ],
  "unverified": []
}
```

## Scoring

```text
score = 0.6 × skill overlap + 0.4 × semantic similarity
```

- **Skill overlap:** required skills count 1, nice-to-have skills count 0.5. Skills are compared on normalized names ("C#", "csharp" and "C Sharp" all become `csharp`), see `SkillAliases`.
- **Semantic similarity:** the best cosine similarity between the posting embedding and any CV section, computed in PostgreSQL with pgvector's `<=>` operator.

The weights are in `appsettings.json` under `Matching`.

## Tests

```bash
dotnet test
```

The unit tests don't call any real LLM: `FakeChatClient` returns canned JSON, including broken answers, to test the retry and the evidence check. The integration tests in `tests/JobMatch.Tests/Integration` start a `pgvector/pgvector:pg17` container, so Docker has to be running.

## Things I learned / limitations

- The model doesn't always follow the schema perfectly. At temperature 0 it still sometimes returned `"C# / ASP.NET Core"` or `".NET (C# / ASP.NET Core)"` as one skill, so those get split in code.
- The gap report only sees the 3 closest CV sections, so a requirement covered in another section (languages, for example) can end up in `missing`.
- CV sections are found with simple heading rules (all-caps lines, `#` headings, lines ending with `:`). A CV with an unusual layout falls back to fixed-size chunks.

## Not done yet

- JWT auth (single user for now)
- France Travail API as a second source of postings
- An evaluation set of hand-labeled postings to measure extraction accuracy per field
- PDF upload for the CV
- A front end
