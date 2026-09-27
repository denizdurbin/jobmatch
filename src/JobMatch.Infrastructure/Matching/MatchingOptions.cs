namespace JobMatch.Infrastructure.Matching;

public class MatchingOptions
{
    public double SkillOverlapWeight { get; set; } = 0.6;
    public double SemanticWeight { get; set; } = 0.4;
}
