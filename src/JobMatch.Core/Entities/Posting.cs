namespace JobMatch.Core.Entities;

public class Posting
{
    public Guid Id { get; set; }
    public PostingSource Source { get; set; }
    public string? ExternalId { get; set; }
    public string? Url { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string RawText { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public PostingStatus Status { get; set; }
    public DateTime FetchedAt { get; set; }
    public string? Error { get; set; }

    public PostingExtraction? Extraction { get; set; }
    public List<PostingSkill> Skills { get; set; } = new();
}
