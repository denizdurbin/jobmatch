using JobMatch.Core.Text;

namespace JobMatch.Tests;

public class TextNormalizerTests
{
    [Fact]
    public void Normalize_collapses_whitespace()
    {
        TextNormalizer.Normalize("  Dev   C#\n\n\tjunior  ").ShouldBe("Dev C# junior");
    }

    [Fact]
    public void Normalize_handles_non_breaking_spaces()
    {
        TextNormalizer.Normalize("Salaire : 40k").ShouldBe("Salaire : 40k");
    }

    [Fact]
    public void Normalize_empty_returns_empty()
    {
        TextNormalizer.Normalize("   ").ShouldBe("");
    }

    [Fact]
    public void Hash_ignores_whitespace_differences()
    {
        TextNormalizer.Hash("Backend dev\nParis").ShouldBe(TextNormalizer.Hash("Backend dev   Paris "));
    }

    [Fact]
    public void Hash_is_64_hex_chars()
    {
        TextNormalizer.Hash("hello").Length.ShouldBe(64);
    }
}
