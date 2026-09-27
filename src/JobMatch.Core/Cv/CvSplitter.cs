using System.Text;
using JobMatch.Core.Text;

namespace JobMatch.Core.Cv;

public record CvChunk(string Heading, string Text);

// splits a CV into sections (experience, projects, skills...) so each can be embedded on its own
public static class CvSplitter
{
    private const int FallbackChunkSize = 800;

    public static List<CvChunk> Split(string cvText)
    {
        var chunks = new List<CvChunk>();
        string heading = "Intro";
        var current = new StringBuilder();
        bool foundHeading = false;

        foreach (string rawLine in cvText.Split('\n'))
        {
            string line = rawLine.Trim();
            if (IsHeading(line))
            {
                AddChunk(chunks, heading, current);
                heading = line.TrimStart('#').Trim().TrimEnd(':').Trim();
                current.Clear();
                foundHeading = true;
            }
            else if (line.Length > 0)
            {
                current.AppendLine(line);
            }
        }
        AddChunk(chunks, heading, current);

        if (!foundHeading)
        {
            return SplitBySize(cvText);
        }
        return chunks;
    }

    private static bool IsHeading(string line)
    {
        if (line.Length == 0 || line.Length > 60)
        {
            return false;
        }
        if (line.StartsWith('#'))
        {
            return true;
        }
        if (line.EndsWith(':'))
        {
            return true;
        }

        // ALL CAPS line like "EXPERIENCE", but not "SQL, AWS, GCP"
        int letters = line.Count(char.IsLetter);
        return letters >= 3 && !line.Contains(',') && line == line.ToUpperInvariant();
    }

    private static void AddChunk(List<CvChunk> chunks, string heading, StringBuilder text)
    {
        string content = text.ToString().Trim();
        if (content.Length > 0)
        {
            chunks.Add(new CvChunk(heading, content));
        }
    }

    // no headings: cut the text in ~800 char pieces without cutting words
    private static List<CvChunk> SplitBySize(string cvText)
    {
        var chunks = new List<CvChunk>();
        var current = new StringBuilder();
        foreach (string word in TextNormalizer.Normalize(cvText).Split(' '))
        {
            if (current.Length + word.Length + 1 > FallbackChunkSize && current.Length > 0)
            {
                chunks.Add(new CvChunk($"Part {chunks.Count + 1}", current.ToString()));
                current.Clear();
            }
            if (current.Length > 0)
            {
                current.Append(' ');
            }
            current.Append(word);
        }
        if (current.Length > 0)
        {
            chunks.Add(new CvChunk($"Part {chunks.Count + 1}", current.ToString()));
        }
        return chunks;
    }
}
