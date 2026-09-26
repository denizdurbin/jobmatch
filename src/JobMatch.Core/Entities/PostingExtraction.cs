namespace JobMatch.Core.Entities;

public class PostingExtraction
{
    public Guid PostingId { get; set; }
    public string Seniority { get; set; } = "unspecified";
    public int? MinYears { get; set; }
    public string ContractType { get; set; } = "unspecified";
    public string RemotePolicy { get; set; } = "unspecified";
    public string? City { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string? SalaryCurrency { get; set; }
    public string? SalaryPeriod { get; set; }
    public string LanguagesJson { get; set; } = "[]";
    // full answer from the LLM, kept so we can look at it later
    public string RawJson { get; set; } = "{}";
    public string Model { get; set; } = "";
    public string PromptVersion { get; set; } = "";
    public DateTime ExtractedAt { get; set; }
}
