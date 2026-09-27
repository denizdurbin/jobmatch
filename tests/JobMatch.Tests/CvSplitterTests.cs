using JobMatch.Core.Cv;

namespace JobMatch.Tests;

public class CvSplitterTests
{
    [Fact]
    public void Splits_on_all_caps_headings()
    {
        string cv = """
            Deniz Durbin
            Software developer

            EXPERIENCE
            Intern at Acme, built a REST API in C#.

            PROJECTS
            JobMatch: matching postings with LLMs.

            SKILLS
            C#, SQL, Docker
            """;

        var chunks = CvSplitter.Split(cv);

        chunks.Select(c => c.Heading).ShouldBe(["Intro", "EXPERIENCE", "PROJECTS", "SKILLS"]);
        chunks[1].Text.ShouldContain("REST API in C#");
        chunks[3].Text.ShouldContain("C#, SQL, Docker");
    }

    [Fact]
    public void Splits_on_markdown_and_colon_headings()
    {
        string cv = "# Experience\nDev at X\n\nEducation:\nMaster at Sorbonne\n";

        var chunks = CvSplitter.Split(cv);

        chunks.Select(c => c.Heading).ShouldBe(["Experience", "Education"]);
    }

    [Fact]
    public void Line_of_skills_in_caps_is_not_a_heading()
    {
        var chunks = CvSplitter.Split("SKILLS\nSQL, AWS, GCP\n");

        chunks.Count.ShouldBe(1);
        chunks[0].Text.ShouldContain("SQL, AWS, GCP");
    }

    [Fact]
    public void No_headings_falls_back_to_chunks()
    {
        string cv = string.Join(" ", Enumerable.Repeat("word", 500)); // ~2500 chars

        var chunks = CvSplitter.Split(cv);

        chunks.Count.ShouldBeGreaterThan(2);
        chunks.ShouldAllBe(c => c.Text.Length <= 800);
        chunks[0].Heading.ShouldBe("Part 1");
    }
}
