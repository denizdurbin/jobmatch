using JobMatch.Core.Text;

namespace JobMatch.Core.Gap;

public static class GapReportVerifier
{
    public static GapReport Verify(GapReportDraft draft, string postingText, string cvText)
    {
        var unverified = new List<GapItem>();

        var met = KeepVerified(draft.Met, postingText, cvText, needsCvQuote: true, unverified);
        var partiallyMet = KeepVerified(draft.PartiallyMet, postingText, cvText, needsCvQuote: true, unverified);
        // for missing items there is nothing to quote from the CV
        var missing = KeepVerified(draft.Missing, postingText, cvText, needsCvQuote: false, unverified);

        return new GapReport
        {
            Met = met,
            PartiallyMet = partiallyMet,
            Missing = missing,
            Unverified = unverified
        };
    }

    private static List<GapItem> KeepVerified(List<GapItem> items, string postingText, string cvText, bool needsCvQuote, List<GapItem> unverified)
    {
        var kept = new List<GapItem>();
        foreach (var item in items)
        {
            bool postingOk = QuoteVerifier.IsFoundIn(postingText, item.PostingQuote);
            bool cvOk = !needsCvQuote || QuoteVerifier.IsFoundIn(cvText, item.CvQuote);

            if (postingOk && cvOk)
            {
                kept.Add(item);
            }
            else
            {
                unverified.Add(item);
            }
        }
        return kept;
    }
}
