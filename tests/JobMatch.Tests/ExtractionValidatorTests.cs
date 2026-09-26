using JobMatch.Core.Extraction;

namespace JobMatch.Tests;

public class ExtractionValidatorTests
{
    private const string RawText = "Développeur .NET junior (CDI) à Lyon. Compétences : C#, SQL Server. Docker est un plus.";

    private static ExtractionResult Valid() => new()
    {
        Title = "Développeur .NET",
        Seniority = "junior",
        ContractType = "CDI",
        RemotePolicy = "unspecified",
        City = "Lyon",
        Skills =
        [
            new ExtractedSkill { Name = "C#", Required = true, Evidence = "Compétences : C#" },
            new ExtractedSkill { Name = "Docker", Required = false, Evidence = "Docker est un plus" }
        ]
    };

    [Fact]
    public void Valid_result_has_no_errors()
    {
        var outcome = ExtractionValidator.Validate(Valid(), RawText);

        outcome.IsValid.ShouldBeTrue();
        outcome.Cleaned.Skills.Count.ShouldBe(2);
    }

    [Fact]
    public void Unknown_enum_value_is_an_error()
    {
        var outcome = ExtractionValidator.Validate(Valid() with { ContractType = "permanent" }, RawText);

        outcome.IsValid.ShouldBeFalse();
        outcome.Errors.ShouldContain(e => e.Contains("contract_type"));
    }

    [Fact]
    public void Salary_min_above_max_is_an_error()
    {
        var result = Valid() with { Salary = new SalaryInfo { Min = 50000, Max = 40000, Period = "year" } };

        ExtractionValidator.Validate(result, RawText).Errors.ShouldContain(e => e.Contains("salary"));
    }

    [Fact]
    public void Unknown_salary_period_is_an_error()
    {
        var result = Valid() with { Salary = new SalaryInfo { Min = 400, Period = "week" } };

        ExtractionValidator.Validate(result, RawText).Errors.ShouldContain(e => e.Contains("salary.period"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void Years_out_of_range_is_an_error(int years)
    {
        var outcome = ExtractionValidator.Validate(Valid() with { MinYearsExperience = years }, RawText);

        outcome.Errors.ShouldContain(e => e.Contains("min_years_experience"));
    }

    [Fact]
    public void Skill_with_fake_evidence_is_dropped_not_an_error()
    {
        var result = Valid() with
        {
            Skills = [.. Valid().Skills, new ExtractedSkill { Name = "Kubernetes", Required = true, Evidence = "Kubernetes obligatoire" }]
        };

        var outcome = ExtractionValidator.Validate(result, RawText);

        outcome.IsValid.ShouldBeTrue();
        outcome.DroppedSkills.Single().Name.ShouldBe("Kubernetes");
        outcome.Cleaned.Skills.ShouldNotContain(s => s.Name == "Kubernetes");
    }
}
