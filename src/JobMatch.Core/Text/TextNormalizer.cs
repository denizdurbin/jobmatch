using System.Security.Cryptography;
using System.Text;

namespace JobMatch.Core.Text;

public static class TextNormalizer
{
    // turns every run of whitespace (spaces, tabs, newlines, nbsp) into one space
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return "";
        }

        var sb = new StringBuilder(text.Length);
        bool lastWasSpace = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                }
                lastWasSpace = true;
            }
            else
            {
                sb.Append(c);
                lastWasSpace = false;
            }
        }
        return sb.ToString().Trim();
    }

    // sha256 of the normalized, lowercased text, used to spot the same posting pasted twice
    public static string Hash(string text)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(text).ToLowerInvariant()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
