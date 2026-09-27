using System.Text.Json;
using JobMatch.Core.Entities;
using JobMatch.Core.Matching;
using JobMatch.Core.Skills;
using JobMatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace JobMatch.Infrastructure.Matching;

public record MatchFilters(string? City, bool? Remote, string? Contract, string? Seniority, DateTime? Since, int Limit = 20);

public record MatchItem(Guid PostingId, string? Title, string? Company, string? City, string RemotePolicy, string ContractType, double Score, ScoreBreakdown Breakdown);

public class MatchingService(AppDbContext db, IOptions<MatchingOptions> options)
{
    public async Task<List<MatchItem>?> GetMatchesAsync(Guid profileId, MatchFilters filters, CancellationToken ct = default)
    {
        var profile = await db.CvProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == profileId, ct);
        if (profile == null)
        {
            return null;
        }

        // 1. hard filters, done in SQL
        IQueryable<Posting> query = db.Postings.AsNoTracking()
            .Where(p => p.Status == PostingStatus.Embedded && p.Extraction != null);

        bool wantsRemote = filters.Remote == true;
        bool hasCity = !string.IsNullOrWhiteSpace(filters.City);
        if (hasCity && wantsRemote)
        {
            query = query.Where(p => EF.Functions.ILike(p.Extraction!.City ?? "", filters.City!)
                || p.Extraction!.RemotePolicy == "remote" || p.Extraction.RemotePolicy == "hybrid");
        }
        else if (hasCity)
        {
            query = query.Where(p => EF.Functions.ILike(p.Extraction!.City ?? "", filters.City!));
        }
        else if (wantsRemote)
        {
            query = query.Where(p => p.Extraction!.RemotePolicy == "remote" || p.Extraction.RemotePolicy == "hybrid");
        }

        if (!string.IsNullOrWhiteSpace(filters.Contract))
        {
            query = query.Where(p => p.Extraction!.ContractType == filters.Contract);
        }
        if (!string.IsNullOrWhiteSpace(filters.Seniority))
        {
            query = query.Where(p => p.Extraction!.Seniority == filters.Seniority);
        }
        if (filters.Since != null)
        {
            var sinceUtc = DateTime.SpecifyKind(filters.Since.Value, DateTimeKind.Utc);
            query = query.Where(p => p.FetchedAt >= sinceUtc);
        }

        var candidates = await query
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Company,
                p.Extraction!.City,
                p.Extraction.RemotePolicy,
                p.Extraction.ContractType,
                Skills = p.Skills.Select(s => new SkillRequirement(s.NormalizedName, s.Required)).ToList()
            })
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            return new List<MatchItem>();
        }

        // 2. semantic similarity with pgvector
        var similarities = await GetBestSimilaritiesAsync(profileId, candidates.Select(c => c.Id).ToArray(), ct);

        // 3. skill overlap + final score
        var cvSkills = SkillAliases.FindSkillsIn(profile.FullText).ToHashSet();
        var items = new List<MatchItem>();
        foreach (var c in candidates)
        {
            similarities.TryGetValue(c.Id, out double similarity);
            var breakdown = MatchScorer.Score(c.Skills, cvSkills, similarity, options.Value.SkillOverlapWeight, options.Value.SemanticWeight);
            items.Add(new MatchItem(c.Id, c.Title, c.Company, c.City, c.RemotePolicy, c.ContractType, breakdown.Score, breakdown));
        }

        items = items.OrderByDescending(i => i.Score).Take(Math.Clamp(filters.Limit, 1, 100)).ToList();
        await SaveResultsAsync(profileId, items, ct);
        return items;
    }

    // best cosine similarity between each posting and any section of the CV.
    // <=> is pgvector's cosine distance, similarity = 1 - distance.
    // this is an exact scan over (postings x sections), fine for a personal amount of data
    private async Task<Dictionary<Guid, double>> GetBestSimilaritiesAsync(Guid profileId, Guid[] postingIds, CancellationToken ct)
    {
        var rows = await db.Database.SqlQuery<SimilarityRow>($"""
            SELECT e."PostingId" AS "PostingId", MAX(1 - (e."Vector" <=> s."Vector")) AS "Similarity"
            FROM "PostingEmbeddings" e
            CROSS JOIN "CvSections" s
            WHERE s."ProfileId" = {profileId}
              AND e."Kind" = 'FullText'
              AND e."PostingId" = ANY({postingIds})
            GROUP BY e."PostingId"
            """).ToListAsync(ct);

        return rows.ToDictionary(r => r.PostingId, r => r.Similarity);
    }

    // keep the latest score per (cv, posting), the cached gap report is kept
    private async Task SaveResultsAsync(Guid profileId, List<MatchItem> items, CancellationToken ct)
    {
        var postingIds = items.Select(i => i.PostingId).ToList();
        var existing = await db.MatchResults
            .Where(m => m.ProfileId == profileId && postingIds.Contains(m.PostingId))
            .ToDictionaryAsync(m => m.PostingId, ct);

        foreach (var item in items)
        {
            if (!existing.TryGetValue(item.PostingId, out var result))
            {
                result = new MatchResult { ProfileId = profileId, PostingId = item.PostingId };
                db.MatchResults.Add(result);
            }
            result.Score = item.Score;
            result.ScoreBreakdownJson = JsonSerializer.Serialize(item.Breakdown);
            result.CreatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
    }
}

public class SimilarityRow
{
    public Guid PostingId { get; set; }
    public double Similarity { get; set; }
}
