namespace JobMatch.Core.Entities;

public class MatchResult
{
    public int Id { get; set; }
    public Guid ProfileId { get; set; }
    public Guid PostingId { get; set; }
    public double Score { get; set; }
    public string ScoreBreakdownJson { get; set; } = "{}";
    public string? GapReportJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
