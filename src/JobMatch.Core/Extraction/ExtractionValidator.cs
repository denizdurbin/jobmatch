using JobMatch.Core.Text;

namespace JobMatch.Core.Extraction;

public class ValidationOutcome
{
    public List<string> Errors { get; } = new();
    public List<ExtractedSkill> DroppedSkills { get; } = new();
    public ExtractionResult Cleaned { get; set; } = new();
    public bool IsValid => Errors.Count == 0;
}

// checks the LLM output with plain C# rules (we don't trust the model on this)
public static class ExtractionValidator
{
    public static ValidationOutcome Validate(ExtractionResult result, string rawText)
    {
        var outcome = new ValidationOutcome();

        string? seniority = Canonical(result.Seniority, AllowedValues.Seniority);
        if (seniority == null)
        {
            outcome.Errors.Add($"seniority '{result.Seniority}' must be one of: {string.Join(", ", AllowedValues.Seniority)}");
        }

        string? contractType = Canonical(result.ContractType, AllowedValues.ContractType);
        if (contractType == null)
        {
            outcome.Errors.Add($"contract_type '{result.ContractType}' must be one of: {string.Join(", ", AllowedValues.ContractType)}");
        }

        string? remotePolicy = Canonical(result.RemotePolicy, AllowedValues.RemotePolicy);
        if (remotePolicy == null)
        {
            outcome.Errors.Add($"remote_policy '{result.RemotePolicy}' must be one of: {string.Join(", ", AllowedValues.RemotePolicy)}");
        }

        if (result.MinYearsExperience is < 0 or > 30)
        {
            outcome.Errors.Add($"min_years_experience must be between 0 and 30 (got {result.MinYearsExperience})");
        }

        SalaryInfo? salary = result.Salary;
        if (salary != null)
        {
            if (salary.Min != null && salary.Max != null && salary.Min > salary.Max)
            {
                outcome.Errors.Add($"salary.min ({salary.Min}) is greater than salary.max ({salary.Max})");
            }

            if (salary.Period != null)
            {
                string? period = Canonical(salary.Period, AllowedValues.SalaryPeriod);
                if (period == null)
                {
                    outcome.Errors.Add($"salary.period '{salary.Period}' must be one of: {string.Join(", ", AllowedValues.SalaryPeriod)} or null");
                }
                else
                {
                    salary = salary with { Period = period };
                }
            }
        }

        // a skill only counts if its evidence is really in the posting
        var keptSkills = new List<ExtractedSkill>();
        foreach (var skill in result.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Name) || !QuoteVerifier.IsFoundIn(rawText, skill.Evidence))
            {
                outcome.DroppedSkills.Add(skill);
            }
            else
            {
                keptSkills.Add(skill);
            }
        }

        outcome.Cleaned = result with
        {
            Seniority = seniority ?? result.Seniority,
            ContractType = contractType ?? result.ContractType,
            RemotePolicy = remotePolicy ?? result.RemotePolicy,
            Salary = salary,
            Skills = keptSkills
        };
        return outcome;
    }

    // returns the allowed spelling ("cdi" -> "CDI"), or null if the value is not allowed
    private static string? Canonical(string? value, string[] allowed)
    {
        if (value == null)
        {
            return null;
        }

        foreach (var option in allowed)
        {
            if (string.Equals(option, value.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return option;
            }
        }
        return null;
    }
}
