using System.ComponentModel;
using System.Text.Json.Serialization;

namespace JobMatch.Core.Extraction;

// what the LLM has to return for one posting (same shape as the spec)
public record ExtractionResult
{
    [JsonPropertyName("title")]
    public string Title { get; init; } = "";

    [JsonPropertyName("company")]
    public string? Company { get; init; }

    [JsonPropertyName("seniority")]
    [Description("One of: intern, junior, mid, senior, lead, unspecified")]
    public string Seniority { get; init; } = "unspecified";

    [JsonPropertyName("min_years_experience")]
    public int? MinYearsExperience { get; init; }

    [JsonPropertyName("contract_type")]
    [Description("One of: CDI, CDD, freelance, internship, apprenticeship, unspecified")]
    public string ContractType { get; init; } = "unspecified";

    [JsonPropertyName("remote_policy")]
    [Description("One of: onsite, hybrid, remote, unspecified")]
    public string RemotePolicy { get; init; } = "unspecified";

    [JsonPropertyName("city")]
    public string? City { get; init; }

    [JsonPropertyName("salary")]
    public SalaryInfo? Salary { get; init; }

    [JsonPropertyName("languages")]
    public List<LanguageRequirement> Languages { get; init; } = new();

    [JsonPropertyName("skills")]
    public List<ExtractedSkill> Skills { get; init; } = new();
}

public record SalaryInfo
{
    [JsonPropertyName("min")]
    public decimal? Min { get; init; }

    [JsonPropertyName("max")]
    public decimal? Max { get; init; }

    [JsonPropertyName("currency")]
    public string? Currency { get; init; }

    [JsonPropertyName("period")]
    [Description("One of: year, month, day, or null")]
    public string? Period { get; init; }
}

public record LanguageRequirement
{
    [JsonPropertyName("language")]
    public string Language { get; init; } = "";

    [JsonPropertyName("level")]
    public string? Level { get; init; }
}

public record ExtractedSkill
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("required")]
    public bool Required { get; init; }

    [JsonPropertyName("evidence")]
    [Description("Exact quote copied from the posting that mentions this skill")]
    public string Evidence { get; init; } = "";
}
