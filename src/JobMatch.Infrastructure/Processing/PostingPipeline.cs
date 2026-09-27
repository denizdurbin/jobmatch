using System.Text.Json;
using JobMatch.Core.Entities;
using JobMatch.Core.Skills;
using JobMatch.Infrastructure.Data;
using JobMatch.Infrastructure.Embeddings;
using JobMatch.Infrastructure.Extraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace JobMatch.Infrastructure.Processing;

// Raw -> Extracted -> Embedded, anything that throws -> Failed
public class PostingPipeline(AppDbContext db, PostingExtractor extractor, EmbeddingService embeddings, ILogger<PostingPipeline> logger)
{
    public async Task ProcessAsync(Guid postingId, CancellationToken ct = default)
    {
        var posting = await db.Postings
            .Include(p => p.Extraction)
            .Include(p => p.Skills)
            .Include(p => p.Embeddings)
            .FirstOrDefaultAsync(p => p.Id == postingId, ct);
        if (posting == null)
        {
            logger.LogWarning("Posting {Id} not found, skipping", postingId);
            return;
        }

        try
        {
            if (posting.Status == PostingStatus.Raw)
            {
                await ExtractAsync(posting, ct);
            }
            if (posting.Status == PostingStatus.Extracted)
            {
                await EmbedAsync(posting, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Posting {Id} failed", posting.Id);
            posting.Status = PostingStatus.Failed;
            posting.Error = ex.Message;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task ExtractAsync(Posting posting, CancellationToken ct)
    {
        var attempt = await extractor.ExtractAsync(posting.RawText, ct);
        if (!attempt.Success || attempt.Result == null)
        {
            posting.Status = PostingStatus.Failed;
            posting.Error = attempt.Error;
            await db.SaveChangesAsync(ct);
            return;
        }

        var result = attempt.Result;
        if (string.IsNullOrWhiteSpace(posting.Title))
        {
            posting.Title = result.Title;
        }
        if (string.IsNullOrWhiteSpace(posting.Company))
        {
            posting.Company = result.Company;
        }

        if (posting.Extraction != null)
        {
            db.PostingExtractions.Remove(posting.Extraction);
        }
        posting.Extraction = new PostingExtraction
        {
            PostingId = posting.Id,
            Seniority = result.Seniority,
            MinYears = result.MinYearsExperience,
            ContractType = result.ContractType,
            RemotePolicy = result.RemotePolicy,
            City = result.City,
            SalaryMin = result.Salary?.Min,
            SalaryMax = result.Salary?.Max,
            SalaryCurrency = result.Salary?.Currency,
            SalaryPeriod = result.Salary?.Period,
            LanguagesJson = JsonSerializer.Serialize(result.Languages),
            RawJson = attempt.RawJson ?? "{}",
            Model = attempt.Model,
            PromptVersion = attempt.PromptVersion,
            ExtractedAt = DateTime.UtcNow
        };

        posting.Skills.Clear();
        foreach (var skill in result.Skills)
        {
            posting.Skills.Add(new PostingSkill
            {
                SkillName = skill.Name,
                NormalizedName = SkillAliases.Normalize(skill.Name),
                Required = skill.Required,
                Evidence = skill.Evidence
            });
        }

        posting.Status = PostingStatus.Extracted;
        posting.Error = null;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Posting {Id} extracted: {SkillCount} skills", posting.Id, posting.Skills.Count);
    }

    private async Task EmbedAsync(Posting posting, CancellationToken ct)
    {
        posting.Embeddings.Clear();

        posting.Embeddings.Add(new PostingEmbedding
        {
            Kind = EmbeddingKind.FullText,
            Vector = await embeddings.EmbedAsync(posting.RawText, ct)
        });

        if (posting.Skills.Count > 0)
        {
            string skillsText = string.Join(", ", posting.Skills.Select(s => s.SkillName));
            posting.Embeddings.Add(new PostingEmbedding
            {
                Kind = EmbeddingKind.Skills,
                Vector = await embeddings.EmbedAsync(skillsText, ct)
            });
        }

        posting.Status = PostingStatus.Embedded;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Posting {Id} embedded", posting.Id);
    }
}
