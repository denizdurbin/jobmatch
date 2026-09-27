using JobMatch.Core.Skills;

namespace JobMatch.Tests;

public class SkillAliasesTests
{
    [Theory]
    [InlineData("C#", "csharp")]
    [InlineData("csharp", "csharp")]
    [InlineData("C Sharp", "csharp")]
    [InlineData(" .NET Core ", "dotnet")]
    [InlineData("PostgreSQL", "postgresql")]
    [InlineData("Postgres", "postgresql")]
    [InlineData("K8s", "kubernetes")]
    public void Normalize_maps_aliases(string input, string expected)
    {
        SkillAliases.Normalize(input).ShouldBe(expected);
    }

    [Fact]
    public void Normalize_unknown_skill_is_lowercased_with_dashes()
    {
        SkillAliases.Normalize("Some Framework").ShouldBe("some-framework");
    }

    [Fact]
    public void FindSkillsIn_finds_skills_in_free_text()
    {
        var skills = SkillAliases.FindSkillsIn("Projects in C# and ASP.NET Core, deployed with Docker on Azure. Also some React.");

        skills.ShouldContain("csharp");
        skills.ShouldContain("aspnet");
        skills.ShouldContain("docker");
        skills.ShouldContain("azure");
        skills.ShouldContain("react");
    }

    [Fact]
    public void FindSkillsIn_respects_word_boundaries()
    {
        var skills = SkillAliases.FindSkillsIn("JavaScript and reactive programming with PostgreSQL");

        skills.ShouldContain("javascript");
        skills.ShouldContain("postgresql");
        skills.ShouldNotContain("java");
        skills.ShouldNotContain("react");
        skills.ShouldNotContain("sql");
    }

    [Theory]
    [InlineData("C# / ASP.NET Core", new[] { "C#", "ASP.NET Core" })]
    [InlineData("React, TypeScript et Node.js", new[] { "React", "TypeScript", "Node.js" })]
    [InlineData("Docker & Kubernetes", new[] { "Docker", "Kubernetes" })]
    [InlineData(".NET (C# / ASP.NET Core)", new[] { ".NET", "C#", "ASP.NET Core" })]
    [InlineData("CI/CD", new[] { "CI/CD" })]
    [InlineData("C++", new[] { "C++" })]
    [InlineData("Docker", new[] { "Docker" })]
    public void SplitCombined_splits_skills_the_llm_put_together(string input, string[] expected)
    {
        SkillAliases.SplitCombined(input).ShouldBe(expected);
    }
}
