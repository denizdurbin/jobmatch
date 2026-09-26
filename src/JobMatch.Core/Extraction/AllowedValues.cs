namespace JobMatch.Core.Extraction;

public static class AllowedValues
{
    public static readonly string[] Seniority = ["intern", "junior", "mid", "senior", "lead", "unspecified"];
    public static readonly string[] ContractType = ["CDI", "CDD", "freelance", "internship", "apprenticeship", "unspecified"];
    public static readonly string[] RemotePolicy = ["onsite", "hybrid", "remote", "unspecified"];
    public static readonly string[] SalaryPeriod = ["year", "month", "day"];
}
