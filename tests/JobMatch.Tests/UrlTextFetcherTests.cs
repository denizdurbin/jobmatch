using JobMatch.Infrastructure.Ingestion;

namespace JobMatch.Tests;

public class UrlTextFetcherTests
{
    [Fact]
    public void HtmlToText_drops_scripts_and_keeps_text()
    {
        string html = """
            <html><head><style>p{color:red}</style><script>var x = 1;</script></head>
            <body><nav>Menu</nav><h1>Développeur C#</h1><p>Vous maîtrisez&nbsp;ASP.NET Core.</p></body></html>
            """;

        string text = UrlTextFetcher.HtmlToText(html);

        text.ShouldContain("Développeur C#");
        text.ShouldContain("ASP.NET Core");
        text.ShouldNotContain("var x");
        text.ShouldNotContain("color:red");
        text.ShouldNotContain("Menu");
    }

    [Fact]
    public void HtmlToText_puts_blocks_on_separate_lines()
    {
        string text = UrlTextFetcher.HtmlToText("<ul><li>C#</li><li>SQL</li></ul><p>Paris</p>");

        text.Split('\n').ShouldBe(["C#", "SQL", "Paris"]);
    }
}
