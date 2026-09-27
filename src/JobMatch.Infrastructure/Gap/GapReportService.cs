using System.Text;
using System.Text.Json;
using JobMatch.Core.Entities;
using JobMatch.Core.Gap;
using JobMatch.Infrastructure.Ai;
using JobMatch.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace JobMatch.Infrastructure.Gap;

public class PostingNotReadyException(string message) : Exception(message);

public class GapReportService(AppDbContext db, IChatClient chatClient, PromptLoader prompts, ILogger<GapReportService> logger)
{
    public const string PromptName = "gap-report.v1";
    private const int SectionsToRetrieve = 3;

    public async Task<GapReport?> GetOrCreateAsync(Guid profileId, Guid postingId, CancellationToken ct = default)
    {
        var profile = await db.CvProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == profileId, ct);
        var posting = await db.Postings.AsNoTracking()
            .Include(p => p.Skills)
            .FirstOrDefaultAsync(p => p.Id == postingId, ct);
        if (profile == null || posting == null)
        {
            return null;
        }
        if (posting.Status != PostingStatus.Embedded)
        {
            throw new PostingNotReadyException($"Posting is not processed yet (status: {posting.Status}).");
        }

        var match = await db.MatchResults.FirstOrDefaultAsync(m => m.ProfileId == profileId && m.PostingId == postingId, ct);
        if (match?.GapReportJson != null)
        {
            return JsonSerializer.Deserialize<GapReport>(match.GapReportJson);
        }

        // retrieval: the CV sections closest to the posting
        var sections = await RetrieveSectionsAsync(profileId, postingId, ct);

        // generation
        Prompt prompt = prompts.Load(PromptName);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, prompt.Text),
            new(ChatRole.User, BuildUserMessage(posting, sections))
        };
        var response = await chatClient.GetResponseAsync<GapReportDraft>(messages, new ChatOptions { Temperature = 0 }, cancellationToken: ct);

        GapReportDraft draft;
        try
        {
            draft = response.Result;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            throw new InvalidOperationException("The LLM did not return a valid gap report.", ex);
        }

        // check every quote against the real texts
        var report = GapReportVerifier.Verify(draft, posting.RawText, profile.FullText);
        logger.LogInformation("Gap report for {Posting}: {Met} met, {Partial} partial, {Missing} missing, {Unverified} unverified",
            postingId, report.Met.Count, report.PartiallyMet.Count, report.Missing.Count, report.Unverified.Count);

        // cache it. if the posting was never ranked yet there is no row, so make one
        // (its score gets filled the next time /matches is called)
        if (match == null)
        {
            match = new MatchResult { ProfileId = profileId, PostingId = postingId, CreatedAt = DateTime.UtcNow };
            db.MatchResults.Add(match);
        }
        match.GapReportJson = JsonSerializer.Serialize(report);
        await db.SaveChangesAsync(ct);

        return report;
    }

    // top-k CV sections by cosine distance to the posting embedding (pgvector <=>)
    private async Task<List<RetrievedSection>> RetrieveSectionsAsync(Guid profileId, Guid postingId, CancellationToken ct)
    {
        return await db.Database.SqlQuery<RetrievedSection>($"""
            SELECT s."Heading" AS "Heading", s."Text" AS "Text"
            FROM "CvSections" s
            WHERE s."ProfileId" = {profileId}
            ORDER BY s."Vector" <=> (
                SELECT e."Vector" FROM "PostingEmbeddings" e
                WHERE e."PostingId" = {postingId} AND e."Kind" = 'FullText')
            LIMIT {SectionsToRetrieve}
            """).ToListAsync(ct);
    }

    private static string BuildUserMessage(Posting posting, List<RetrievedSection> sections)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Job posting");
        sb.AppendLine(posting.RawText);
        sb.AppendLine();
        sb.AppendLine("## Skills extracted from the posting");
        foreach (var skill in posting.Skills)
        {
            sb.AppendLine($"- {skill.SkillName} ({(skill.Required ? "required" : "nice to have")})");
        }
        sb.AppendLine();
        sb.AppendLine("## CV excerpts");
        foreach (var section in sections)
        {
            sb.AppendLine($"### {section.Heading}");
            sb.AppendLine(section.Text);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

public class RetrievedSection
{
    public string Heading { get; set; } = "";
    public string Text { get; set; } = "";
}
