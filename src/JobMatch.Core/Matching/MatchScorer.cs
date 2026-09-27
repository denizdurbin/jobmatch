namespace JobMatch.Core.Matching;

public record SkillRequirement(string NormalizedName, bool Required);

public class ScoreBreakdown
{
    public double SkillOverlap { get; set; }
    public double Semantic { get; set; }
    public double OverlapWeight { get; set; }
    public double SemanticWeight { get; set; }
    public double Score { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingRequiredSkills { get; set; } = new();
    public List<string> MissingNiceToHaveSkills { get; set; } = new();
}

// deterministic ranking, no LLM here
public static class MatchScorer
{
    private const double NiceToHaveWeight = 0.5;

    public static ScoreBreakdown Score(IEnumerable<SkillRequirement> postingSkills, ICollection<string> cvSkills,
        double bestSimilarity, double overlapWeight, double semanticWeight)
    {
        var breakdown = new ScoreBreakdown
        {
            OverlapWeight = overlapWeight,
            SemanticWeight = semanticWeight,
            Semantic = Math.Clamp(bestSimilarity, 0, 1)
        };

        double total = 0;
        double matched = 0;
        foreach (var skill in postingSkills.DistinctBy(s => s.NormalizedName))
        {
            double weight = skill.Required ? 1.0 : NiceToHaveWeight;
            total += weight;

            if (cvSkills.Contains(skill.NormalizedName))
            {
                matched += weight;
                breakdown.MatchedSkills.Add(skill.NormalizedName);
            }
            else if (skill.Required)
            {
                breakdown.MissingRequiredSkills.Add(skill.NormalizedName);
            }
            else
            {
                breakdown.MissingNiceToHaveSkills.Add(skill.NormalizedName);
            }
        }

        breakdown.SkillOverlap = total == 0 ? 0 : matched / total;
        breakdown.Score = Math.Round(overlapWeight * breakdown.SkillOverlap + semanticWeight * breakdown.Semantic, 4);
        return breakdown;
    }
}
