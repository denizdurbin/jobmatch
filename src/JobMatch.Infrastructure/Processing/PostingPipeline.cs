using JobMatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobMatch.Infrastructure.Processing;

public class PostingPipeline(AppDbContext db, ILogger<PostingPipeline> logger)
{
    public async Task ProcessAsync(Guid postingId, CancellationToken ct = default)
    {
        var posting = await db.Postings.FirstOrDefaultAsync(p => p.Id == postingId, ct);
        if (posting == null)
        {
            logger.LogWarning("Posting {Id} not found, skipping", postingId);
            return;
        }

        logger.LogInformation("Processing posting {Id} (status {Status})", posting.Id, posting.Status);
        // extraction comes next
    }
}
