namespace JobMatch.Api.Endpoints;

public record ManualPostingRequest(string? Text, string? Url);

public record PostingCreatedResponse(Guid Id, string Status, bool Duplicate);
