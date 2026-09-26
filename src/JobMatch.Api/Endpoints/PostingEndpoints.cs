using JobMatch.Api.Processing;
using JobMatch.Core.Entities;
using JobMatch.Core.Text;
using JobMatch.Infrastructure.Data;
using JobMatch.Infrastructure.Ingestion;
using Microsoft.EntityFrameworkCore;

namespace JobMatch.Api.Endpoints;

public static class PostingEndpoints
{
    public static void MapPostingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/postings").WithTags("Postings");

        group.MapPost("/manual", AddManualPosting);
        group.MapGet("/", ListPostings);
        group.MapGet("/{id:guid}", GetPosting);
    }

    private static async Task<IResult> AddManualPosting(ManualPostingRequest request, AppDbContext db, ProcessingQueue queue,
        UrlTextFetcher fetcher, CancellationToken ct)
    {
        string text;
        if (!string.IsNullOrWhiteSpace(request.Text))
        {
            text = request.Text.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(request.Url))
        {
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["url"] = ["Not a valid http(s) url."]
                });
            }

            try
            {
                text = await fetcher.FetchTextAsync(uri, ct);
            }
            catch (HttpRequestException ex)
            {
                return Results.Problem($"Could not download the page: {ex.Message}", statusCode: 502);
            }
            catch (TaskCanceledException)
            {
                return Results.Problem("Downloading the page timed out.", statusCode: 504);
            }
        }
        else
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["text"] = ["Provide either the posting text or a url."]
            });
        }

        if (text.Length < 50)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["text"] = ["Posting text is too short to be a real job ad."]
            });
        }

        // same text pasted twice -> return the posting we already have
        string hash = TextNormalizer.Hash(text);
        var existing = await db.Postings.FirstOrDefaultAsync(p => p.ContentHash == hash, ct);
        if (existing != null)
        {
            return Results.Ok(new PostingCreatedResponse(existing.Id, existing.Status.ToString(), true));
        }

        var posting = new Posting
        {
            Id = Guid.NewGuid(),
            Source = PostingSource.Manual,
            Url = request.Url,
            RawText = text,
            ContentHash = hash,
            Status = PostingStatus.Raw,
            FetchedAt = DateTime.UtcNow
        };
        db.Postings.Add(posting);
        await db.SaveChangesAsync(ct);

        await queue.EnqueueAsync(posting.Id, ct);

        return Results.Accepted($"/postings/{posting.Id}", new PostingCreatedResponse(posting.Id, posting.Status.ToString(), false));
    }

    private static async Task<IResult> ListPostings(AppDbContext db, string? status, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }
        pageSize = Math.Clamp(pageSize, 1, 100);

        IQueryable<Posting> query = db.Postings.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PostingStatus>(status, true, out var parsedStatus))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = [$"Unknown status '{status}'."]
                });
            }
            query = query.Where(p => p.Status == parsedStatus);
        }

        int total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(p => p.FetchedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PostingSummary(p.Id, p.Title, p.Company, p.Source.ToString(), p.Status.ToString(), p.FetchedAt))
            .ToListAsync(ct);

        return Results.Ok(new PagedResult<PostingSummary>(items, page, pageSize, total));
    }

    private static async Task<IResult> GetPosting(Guid id, AppDbContext db, CancellationToken ct)
    {
        var p = await db.Postings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p == null)
        {
            return Results.NotFound();
        }
        return Results.Ok(new PostingDetails(p.Id, p.Source.ToString(), p.Url, p.Title, p.Company, p.Status.ToString(), p.Error, p.FetchedAt, p.RawText));
    }
}
