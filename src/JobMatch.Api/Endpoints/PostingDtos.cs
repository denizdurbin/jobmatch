namespace JobMatch.Api.Endpoints;

public record ManualPostingRequest(string? Text, string? Url);

public record PostingCreatedResponse(Guid Id, string Status, bool Duplicate);

public record PostingSummary(Guid Id, string? Title, string? Company, string Source, string Status, DateTime FetchedAt);

public record PostingDetails(
    Guid Id,
    string Source,
    string? Url,
    string? Title,
    string? Company,
    string Status,
    string? Error,
    DateTime FetchedAt,
    string RawText,
    ExtractionDto? Extraction,
    List<SkillDto> Skills);

public record ExtractionDto(
    string Seniority,
    int? MinYears,
    string ContractType,
    string RemotePolicy,
    string? City,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string? SalaryCurrency,
    string? SalaryPeriod,
    string Model,
    string PromptVersion,
    DateTime ExtractedAt);

public record SkillDto(string Name, string NormalizedName, bool Required, string Evidence);

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int Total);
