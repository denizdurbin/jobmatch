using JobMatch.Core.Text;

namespace JobMatch.Core.Skills;

public static class SkillAliases
{
    // alias (lowercase) -> normalized name
    private static readonly Dictionary<string, string> Aliases = new()
    {
        ["c#"] = "csharp",
        ["csharp"] = "csharp",
        ["c sharp"] = "csharp",
        [".net"] = "dotnet",
        ["dotnet"] = "dotnet",
        [".net core"] = "dotnet",
        [".net framework"] = "dotnet",
        ["asp.net"] = "aspnet",
        ["asp.net core"] = "aspnet",
        ["asp.net mvc"] = "aspnet",
        ["entity framework"] = "entity-framework",
        ["entity framework core"] = "entity-framework",
        ["ef core"] = "entity-framework",
        ["linq"] = "linq",
        ["blazor"] = "blazor",
        ["wpf"] = "wpf",
        ["winforms"] = "winforms",
        [".net maui"] = "maui",
        ["maui"] = "maui",
        ["signalr"] = "signalr",
        ["javascript"] = "javascript",
        ["typescript"] = "typescript",
        ["react"] = "react",
        ["react.js"] = "react",
        ["reactjs"] = "react",
        ["angular"] = "angular",
        ["vue"] = "vue",
        ["vue.js"] = "vue",
        ["node.js"] = "nodejs",
        ["nodejs"] = "nodejs",
        ["html"] = "html",
        ["css"] = "css",
        ["python"] = "python",
        ["java"] = "java",
        ["kotlin"] = "kotlin",
        ["golang"] = "go",
        ["rust"] = "rust",
        ["c++"] = "cpp",
        ["cpp"] = "cpp",
        ["php"] = "php",
        ["sql"] = "sql",
        ["postgresql"] = "postgresql",
        ["postgres"] = "postgresql",
        ["mysql"] = "mysql",
        ["sql server"] = "sql-server",
        ["mssql"] = "sql-server",
        ["t-sql"] = "sql-server",
        ["mongodb"] = "mongodb",
        ["redis"] = "redis",
        ["elasticsearch"] = "elasticsearch",
        ["docker"] = "docker",
        ["kubernetes"] = "kubernetes",
        ["k8s"] = "kubernetes",
        ["azure"] = "azure",
        ["microsoft azure"] = "azure",
        ["aws"] = "aws",
        ["gcp"] = "gcp",
        ["google cloud"] = "gcp",
        ["git"] = "git",
        ["ci/cd"] = "ci-cd",
        ["github actions"] = "github-actions",
        ["azure devops"] = "azure-devops",
        ["terraform"] = "terraform",
        ["rest"] = "rest-api",
        ["rest api"] = "rest-api",
        ["api rest"] = "rest-api",
        ["restful"] = "rest-api",
        ["graphql"] = "graphql",
        ["grpc"] = "grpc",
        ["microservices"] = "microservices",
        ["microservice"] = "microservices",
        ["rabbitmq"] = "rabbitmq",
        ["kafka"] = "kafka",
        ["linux"] = "linux",
        ["agile"] = "agile",
        ["scrum"] = "scrum",
        ["tdd"] = "tdd",
        ["xunit"] = "xunit",
        ["nunit"] = "nunit",
        ["unit tests"] = "unit-testing",
        ["unit testing"] = "unit-testing",
        ["tests unitaires"] = "unit-testing",
        ["llm"] = "llm",
        ["llms"] = "llm",
        ["rag"] = "rag",
        ["openai"] = "openai",
        ["azure openai"] = "azure-openai",
        ["machine learning"] = "machine-learning",
    };

    public static string Normalize(string name)
    {
        string key = TextNormalizer.Normalize(name).ToLowerInvariant();
        if (Aliases.TryGetValue(key, out var normalized))
        {
            return normalized;
        }
        return key.Replace(' ', '-');
    }

    // the LLM sometimes returns "C# / ASP.NET Core" as one skill, split it into "C#" and "ASP.NET Core".
    // known names with a slash in them (like "CI/CD") are kept as they are
    public static List<string> SplitCombined(string name)
    {
        string trimmed = name.Trim();
        if (Aliases.ContainsKey(trimmed.ToLowerInvariant()))
        {
            return [trimmed];
        }

        string[] separators = ["/", ",", " & ", " and ", " et "];
        var parts = trimmed.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return [trimmed];
        }
        return parts.ToList();
    }

    // finds every known skill mentioned in a text (used on the CV)
    public static List<string> FindSkillsIn(string text)
    {
        var found = new List<string>();
        foreach (var pair in Aliases)
        {
            if (!found.Contains(pair.Value) && ContainsWord(text, pair.Key))
            {
                found.Add(pair.Value);
            }
        }
        return found;
    }

    // like Contains but "java" must not match inside "javascript"
    private static bool ContainsWord(string text, string word)
    {
        int index = text.IndexOf(word, StringComparison.OrdinalIgnoreCase);
        while (index >= 0)
        {
            bool startOk = index == 0 || !char.IsLetterOrDigit(text[index - 1]);
            int end = index + word.Length;
            bool endOk = end == text.Length || !char.IsLetterOrDigit(text[end]);
            if (startOk && endOk)
            {
                return true;
            }
            index = text.IndexOf(word, index + 1, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }
}
