using System.Text.Json;
using JobMatch.Core.Extraction;

namespace JobMatch.Tests;

public class ExtractionResultTests
{
    [Fact]
    public void Deserializes_snake_case_json()
    {
        string json = """
            {"title":"Dev .NET","company":null,"seniority":"junior","min_years_experience":1,
             "contract_type":"CDI","remote_policy":"hybrid","city":"Paris",
             "salary":{"min":38000,"max":45000,"currency":"EUR","period":"year"},
             "languages":[{"language":"English","level":"B2"}],
             "skills":[{"name":"C#","required":true,"evidence":"maîtrise de C#"}]}
            """;

        var result = JsonSerializer.Deserialize<ExtractionResult>(json)!;

        result.MinYearsExperience.ShouldBe(1);
        result.ContractType.ShouldBe("CDI");
        result.Salary!.Max.ShouldBe(45000m);
        result.Skills.Single().Evidence.ShouldBe("maîtrise de C#");
    }
}
