using JobMatch.Core.Matching;

namespace JobMatch.Tests;

public class MatchScorerTests
{
    private static readonly List<SkillRequirement> Skills =
    [
        new("csharp", true),
        new("sql-server", true),
        new("docker", false),
        new("azure", false)
    ];

    [Fact]
    public void All_skills_matched_gives_full_overlap()
    {
        var b = MatchScorer.Score(Skills, ["csharp", "sql-server", "docker", "azure"], 0.5, 0.6, 0.4);

        b.SkillOverlap.ShouldBe(1.0);
        b.Score.ShouldBe(0.6 * 1.0 + 0.4 * 0.5, 0.0001);
    }

    [Fact]
    public void Nice_to_have_counts_half()
    {
        // matched: csharp (1) + docker (0.5) out of 1 + 1 + 0.5 + 0.5 = 3
        var b = MatchScorer.Score(Skills, ["csharp", "docker"], 0, 0.6, 0.4);

        b.SkillOverlap.ShouldBe(1.5 / 3, 0.0001);
        b.MissingRequiredSkills.ShouldBe(["sql-server"]);
        b.MissingNiceToHaveSkills.ShouldBe(["azure"]);
        b.MatchedSkills.ShouldBe(["csharp", "docker"]);
    }

    [Fact]
    public void No_skills_means_zero_overlap()
    {
        MatchScorer.Score([], ["csharp"], 0.8, 0.6, 0.4).SkillOverlap.ShouldBe(0);
    }

    [Fact]
    public void Similarity_is_clamped()
    {
        MatchScorer.Score(Skills, [], 1.3, 0.6, 0.4).Semantic.ShouldBe(1.0);
    }
}
