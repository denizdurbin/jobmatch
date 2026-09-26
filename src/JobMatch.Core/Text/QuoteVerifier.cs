namespace JobMatch.Core.Text;

// checks that a quote given by the LLM really appears in the source text
public static class QuoteVerifier
{
    public static bool IsFoundIn(string source, string? quote)
    {
        if (string.IsNullOrWhiteSpace(quote))
        {
            return false;
        }

        string cleanSource = Clean(source);
        // the model sometimes wraps quotes in "..." or adds a final dot
        string cleanQuote = Clean(quote).Trim('"', '.', ',', ';', ':', ' ', '…');
        if (cleanQuote.Length < 2)
        {
            return false;
        }

        return cleanSource.Contains(cleanQuote, StringComparison.OrdinalIgnoreCase);
    }

    private static string Clean(string text)
    {
        string t = text
            .Replace('’', '\'')
            .Replace('‘', '\'')
            .Replace('“', '"')
            .Replace('”', '"')
            .Replace('«', '"')
            .Replace('»', '"');
        return TextNormalizer.Normalize(t);
    }
}
