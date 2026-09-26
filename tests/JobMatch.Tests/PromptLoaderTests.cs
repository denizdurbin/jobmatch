using JobMatch.Infrastructure.Ai;

namespace JobMatch.Tests;

public class PromptLoaderTests
{
    [Fact]
    public void Loads_extraction_prompt_from_output_folder()
    {
        var prompt = new PromptLoader().Load("extraction.v1");

        prompt.Version.ShouldBe("extraction.v1");
        prompt.Text.ShouldContain("Never guess");
    }

    [Fact]
    public void Missing_prompt_throws()
    {
        Should.Throw<FileNotFoundException>(() => new PromptLoader().Load("nope.v9"));
    }
}
