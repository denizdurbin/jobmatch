using HtmlAgilityPack;

namespace JobMatch.Infrastructure.Ingestion;

public class UrlTextFetcher(HttpClient http)
{
    public async Task<string> FetchTextAsync(Uri url, CancellationToken ct = default)
    {
        string html = await http.GetStringAsync(url, ct);
        return HtmlToText(html);
    }

    public static string HtmlToText(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // remove the parts that are not the actual posting
        var junk = doc.DocumentNode.SelectNodes("//script|//style|//noscript|//nav|//footer|//header");
        if (junk != null)
        {
            foreach (var node in junk.ToList())
            {
                node.Remove();
            }
        }

        // block elements should end up on their own line
        var blocks = doc.DocumentNode.SelectNodes("//p|//li|//h1|//h2|//h3|//h4|//div|//br");
        if (blocks != null)
        {
            foreach (var node in blocks)
            {
                node.AppendChild(doc.CreateTextNode("\n"));
            }
        }

        string text = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText);
        var lines = text.Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.Length > 0);
        return string.Join("\n", lines);
    }
}
