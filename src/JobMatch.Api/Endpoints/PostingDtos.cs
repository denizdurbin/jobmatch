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
    string RawText);

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int Total);
