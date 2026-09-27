using JobMatch.Core.Gap;

namespace JobMatch.Tests;

public class GapReportVerifierTests
{
    private const string Posting = "Vous maîtrisez C# et ASP.NET Core. Une expérience avec Kubernetes est un plus.";
    private const string Cv = "Internship: built a REST API with ASP.NET Core and C#.";

    [Fact]
    public void Keeps_items_whose_quotes_are_real()
    {
        var draft = new GapReportDraft
        {
            Met = [new GapItem { Requirement = "C#", PostingQuote = "Vous maîtrisez C#", CvQuote = "REST API with ASP.NET Core and C#", Note = "Used in internship" }],
            Missing = [new GapItem { Requirement = "Kubernetes", PostingQuote = "Une expérience avec Kubernetes", CvQuote = null, Note = "Not in CV" }]
        };

        var report = GapReportVerifier.Verify(draft, Posting, Cv);

        report.Met.Count.ShouldBe(1);
        report.Missing.Count.ShouldBe(1);
        report.Unverified.ShouldBeEmpty();
    }

    [Fact]
    public void Made_up_cv_quote_goes_to_unverified()
    {
        var draft = new GapReportDraft
        {
            Met = [new GapItem { Requirement = "Kubernetes", PostingQuote = "Kubernetes est un plus", CvQuote = "3 years running Kubernetes clusters", Note = "" }]
        };

        var report = GapReportVerifier.Verify(draft, Posting, Cv);

        report.Met.ShouldBeEmpty();
        report.Unverified.Single().Requirement.ShouldBe("Kubernetes");
    }

    [Fact]
    public void Made_up_posting_quote_goes_to_unverified()
    {
        var draft = new GapReportDraft
        {
            Missing = [new GapItem { Requirement = "Go", PostingQuote = "Go is required", CvQuote = null, Note = "" }]
        };

        GapReportVerifier.Verify(draft, Posting, Cv).Unverified.Count.ShouldBe(1);
    }
}
