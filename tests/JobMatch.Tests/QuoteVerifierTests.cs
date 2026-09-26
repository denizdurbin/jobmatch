using JobMatch.Core.Text;

namespace JobMatch.Tests;

public class QuoteVerifierTests
{
    private const string Posting = "Vous maîtrisez  C# et\nASP.NET Core. L’anglais est un plus.";

    [Fact]
    public void Finds_quote_with_different_whitespace_and_case()
    {
        QuoteVerifier.IsFoundIn(Posting, "vous maîtrisez c# et asp.net core").ShouldBeTrue();
    }

    [Fact]
    public void Handles_curly_apostrophes()
    {
        QuoteVerifier.IsFoundIn(Posting, "L'anglais est un plus").ShouldBeTrue();
    }

    [Fact]
    public void Ignores_trailing_punctuation_added_by_the_model()
    {
        QuoteVerifier.IsFoundIn(Posting, "\"ASP.NET Core.\"").ShouldBeTrue();
    }

    [Fact]
    public void Rejects_made_up_quote()
    {
        QuoteVerifier.IsFoundIn(Posting, "5 years of Kubernetes").ShouldBeFalse();
    }

    [Fact]
    public void Rejects_empty_quote()
    {
        QuoteVerifier.IsFoundIn(Posting, "").ShouldBeFalse();
        QuoteVerifier.IsFoundIn(Posting, null).ShouldBeFalse();
    }
}
