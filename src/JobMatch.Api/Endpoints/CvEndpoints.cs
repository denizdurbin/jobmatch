using JobMatch.Core.Cv;
using JobMatch.Core.Entities;
using JobMatch.Infrastructure.Data;
using JobMatch.Infrastructure.Embeddings;
using JobMatch.Infrastructure.Gap;
using JobMatch.Infrastructure.Matching;

namespace JobMatch.Api.Endpoints;

public record CvUploadRequest(string Name, string Text);

public record CvCreatedResponse(Guid Id, string Name, List<string> Sections);

public static class CvEndpoints
{
    public static void MapCvEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/cv").WithTags("CV");

        group.MapPost("/", UploadCv);
        group.MapGet("/{id:guid}/matches", GetMatches);
        group.MapPost("/{id:guid}/matches/{postingId:guid}/gap", GetGapReport);
    }

    private static async Task<IResult> UploadCv(CvUploadRequest request, AppDbContext db, EmbeddingService embeddings,
        ILogger<CvUploadRequest> logger, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Trim().Length < 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["text"] = ["Paste the full CV text (at least 100 characters)."]
            });
        }

        var profile = new CvProfile
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? "My CV" : request.Name.Trim(),
            FullText = request.Text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        try
        {
            foreach (var chunk in CvSplitter.Split(profile.FullText))
            {
                profile.Sections.Add(new CvSection
                {
                    Heading = chunk.Heading,
                    Text = chunk.Text,
                    Vector = await embeddings.EmbedAsync(chunk.Heading + "\n" + chunk.Text, ct)
                });
            }
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Embedding the CV failed");
            return Results.Problem("The embedding service (Ollama) is not reachable. Is docker compose running?", statusCode: 503);
        }

        db.CvProfiles.Add(profile);
        await db.SaveChangesAsync(ct);

        return Results.Created($"/cv/{profile.Id}", new CvCreatedResponse(profile.Id, profile.Name, profile.Sections.Select(s => s.Heading).ToList()));
    }

    private static async Task<IResult> GetMatches(Guid id, MatchingService matching, string? city, bool? remote, string? contract,
        string? seniority, DateTime? since, int limit = 20, CancellationToken ct = default)
    {
        var matches = await matching.GetMatchesAsync(id, new MatchFilters(city, remote, contract, seniority, since, limit), ct);
        if (matches == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(matches);
    }

    private static async Task<IResult> GetGapReport(Guid id, Guid postingId, GapReportService gapReports, CancellationToken ct)
    {
        try
        {
            var report = await gapReports.GetOrCreateAsync(id, postingId, ct);
            if (report == null)
            {
                return Results.NotFound();
            }
            return Results.Ok(report);
        }
        catch (PostingNotReadyException ex)
        {
            return Results.Problem(ex.Message, statusCode: 409);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: 502);
        }
    }
}
