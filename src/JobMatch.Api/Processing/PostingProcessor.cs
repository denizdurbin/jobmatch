using JobMatch.Core.Entities;
using JobMatch.Infrastructure.Data;
using JobMatch.Infrastructure.Processing;
using Microsoft.EntityFrameworkCore;

namespace JobMatch.Api.Processing;

// background worker: takes ids from the queue and runs the pipeline on them one by one
public class PostingProcessor(ProcessingQueue queue, IServiceScopeFactory scopeFactory, ILogger<PostingProcessor> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RequeueUnfinishedAsync(stoppingToken);

        await foreach (Guid postingId in queue.ReadAllAsync(stoppingToken))
        {
            // DbContext is scoped, so we need a new scope for every posting
            using var scope = scopeFactory.CreateScope();
            try
            {
                var pipeline = scope.ServiceProvider.GetRequiredService<PostingPipeline>();
                await pipeline.ProcessAsync(postingId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Processing posting {Id} crashed", postingId);
            }
        }
    }

    // if the app stopped in the middle of something, pick it up again
    private async Task RequeueUnfinishedAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ids = await db.Postings
            .Where(p => p.Status == PostingStatus.Raw || p.Status == PostingStatus.Extracted)
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var id in ids)
        {
            await queue.EnqueueAsync(id, ct);
        }

        if (ids.Count > 0)
        {
            logger.LogInformation("Re-queued {Count} unfinished postings", ids.Count);
        }
    }
}
