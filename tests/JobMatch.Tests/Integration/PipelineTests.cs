using JobMatch.Core.Entities;
using JobMatch.Core.Text;
using JobMatch.Infrastructure.Ai;
using JobMatch.Infrastructure.Embeddings;
using JobMatch.Infrastructure.Extraction;
using JobMatch.Infrastructure.Processing;
using JobMatch.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace JobMatch.Tests.Integration;

[Collection("postgres")]
public class PipelineTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Raw_posting_ends_up_embedded_with_verified_skills()
    {
        string text = File.ReadAllText("TestData/posting-dotnet-junior.txt");
        var posting = new Posting
        {
            Id = Guid.NewGuid(),
            Source = PostingSource.Manual,
            RawText = text,
            ContentHash = TextNormalizer.Hash(text + Guid.NewGuid()),
            Status = PostingStatus.Raw,
            FetchedAt = DateTime.UtcNow
        };
        await using (var db = fixture.CreateContext())
        {
            db.Postings.Add(posting);
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            var chat = new FakeChatClient(File.ReadAllText("TestData/extraction-fake-evidence.json"));
            var extractor = new PostingExtractor(chat, new PromptLoader(), Options.Create(new GroqOptions()), NullLogger<PostingExtractor>.Instance);
            var embeddings = new EmbeddingService(new FakeEmbeddingGenerator(_ => FakeEmbeddingGenerator.UnitVector(0)));
            var pipeline = new PostingPipeline(db, extractor, embeddings, NullLogger<PostingPipeline>.Instance);

            await pipeline.ProcessAsync(posting.Id);
        }

        await using (var db = fixture.CreateContext())
        {
            var saved = await db.Postings
                .Include(p => p.Extraction)
                .Include(p => p.Skills)
                .Include(p => p.Embeddings)
                .SingleAsync(p => p.Id == posting.Id);

            saved.Status.ShouldBe(PostingStatus.Embedded);
            saved.Extraction!.ContractType.ShouldBe("CDI");
            saved.Extraction.PromptVersion.ShouldBe("extraction.v1");
            saved.Skills.Select(s => s.NormalizedName).ShouldBe(["csharp", "aspnet", "sql-server", "docker"], ignoreOrder: true);
            saved.Embeddings.Count.ShouldBe(2);
        }
    }

    [Fact]
    public async Task Failed_extraction_marks_posting_failed()
    {
        var posting = new Posting
        {
            Id = Guid.NewGuid(),
            Source = PostingSource.Manual,
            RawText = "Some posting text that the fake model will answer badly for.",
            ContentHash = TextNormalizer.Hash(Guid.NewGuid().ToString()),
            Status = PostingStatus.Raw,
            FetchedAt = DateTime.UtcNow
        };
        await using (var db = fixture.CreateContext())
        {
            db.Postings.Add(posting);
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            var chat = new FakeChatClient("not json", "still not json");
            var extractor = new PostingExtractor(chat, new PromptLoader(), Options.Create(new GroqOptions()), NullLogger<PostingExtractor>.Instance);
            var embeddings = new EmbeddingService(new FakeEmbeddingGenerator(_ => FakeEmbeddingGenerator.UnitVector(0)));
            var pipeline = new PostingPipeline(db, extractor, embeddings, NullLogger<PostingPipeline>.Instance);

            await pipeline.ProcessAsync(posting.Id);
        }

        await using (var db = fixture.CreateContext())
        {
            var saved = await db.Postings.SingleAsync(p => p.Id == posting.Id);
            saved.Status.ShouldBe(PostingStatus.Failed);
            saved.Error!.ShouldContain("Extraction failed");
        }
    }
}
