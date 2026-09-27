using System.Text.Json.Serialization;

namespace JobMatch.Core.Gap;

public record GapItem
{
    [JsonPropertyName("requirement")]
    public string Requirement { get; init; } = "";

    [JsonPropertyName("posting_quote")]
    public string PostingQuote { get; init; } = "";

    [JsonPropertyName("cv_quote")]
    public string? CvQuote { get; init; }

    [JsonPropertyName("note")]
    public string Note { get; init; } = "";
}

// what the LLM returns
public record GapReportDraft
{
    [JsonPropertyName("met")]
    public List<GapItem> Met { get; init; } = new();

    [JsonPropertyName("partially_met")]
    public List<GapItem> PartiallyMet { get; init; } = new();

    [JsonPropertyName("missing")]
    public List<GapItem> Missing { get; init; } = new();
}

// what we return after checking the quotes
public record GapReport
{
    [JsonPropertyName("met")]
    public List<GapItem> Met { get; init; } = new();

    [JsonPropertyName("partially_met")]
    public List<GapItem> PartiallyMet { get; init; } = new();

    [JsonPropertyName("missing")]
    public List<GapItem> Missing { get; init; } = new();

    // claims whose quotes we could not find, shown separately instead of hidden
    [JsonPropertyName("unverified")]
    public List<GapItem> Unverified { get; init; } = new();
}
