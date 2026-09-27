using JobMatch.Core.Entities;
using JobMatch.Core.Text;
using JobMatch.Infrastructure.Matching;
using JobMatch.Tests.Fakes;
using Microsoft.Extensions.Options;

namespace JobMatch.Tests.Integration;

[Collection("postgres")]
public class MatchingTests(PostgresFixture fixture)
{
    [Fact]
    public async Task Posting_close_to_a_cv_section_ranks_first()
    {
        // CV has one section pointing in direction 1
        var profile = new CvProfile { Id = Guid.NewGuid(), Name = "test", FullText = "EXPERIENCE\nC# developer", CreatedAt = DateTime.UtcNow };
        profile.Sections.Add(new CvSection { Heading = "EXPERIENCE", Text = "C# developer", Vector = FakeEmbeddingGenerator.UnitVector(1) });

        var close = MakePosting("close", FakeEmbeddingGenerator.UnitVector(1));
        var far = MakePosting("far", FakeEmbeddingGenerator.UnitVector(2));

        await using (var db = fixture.CreateContext())
        {
            db.CvProfiles.Add(profile);
            db.Postings.AddRange(close, far);
            await db.SaveChangesAsync();
        }

        await using (var db = fixture.CreateContext())
        {
            var service = new MatchingService(db, Options.Create(new MatchingOptions()));
            var matches = await service.GetMatchesAsync(profile.Id, new MatchFilters(null, null, null, null, null, 100));

            var mine = matches!.Where(m => m.PostingId == close.Id || m.PostingId == far.Id).ToList();
            mine[0].PostingId.ShouldBe(close.Id);
            mine[0].Breakdown.Semantic.ShouldBe(1.0, 0.001);
            mine[1].Breakdown.Semantic.ShouldBe(0.0, 0.001);
            mine[0].Breakdown.MatchedSkills.ShouldContain("csharp");
        }
    }

    [Fact]
    public async Task Unknown_profile_returns_null()
    {
        await using var db = fixture.CreateContext();
        var service = new MatchingService(db, Options.Create(new MatchingOptions()));

        (await service.GetMatchesAsync(Guid.NewGuid(), new MatchFilters(null, null, null, null, null))).ShouldBeNull();
    }

    private static Posting MakePosting(string title, float[] vector)
    {
        var id = Guid.NewGuid();
        return new Posting
        {
            Id = id,
            Source = PostingSource.Manual,
            Title = title,
            RawText = title + " posting, C# required",
            ContentHash = TextNormalizer.Hash(id.ToString()),
            Status = PostingStatus.Embedded,
            FetchedAt = DateTime.UtcNow,
            Extraction = new PostingExtraction { PostingId = id, ContractType = "CDI", RemotePolicy = "onsite", City = "Paris", Model = "test", PromptVersion = "test" },
            Skills = [new PostingSkill { SkillName = "C#", NormalizedName = "csharp", Required = true, Evidence = "C# required" }],
            Embeddings = [new PostingEmbedding { Kind = EmbeddingKind.FullText, Vector = vector }]
        };
    }
}
