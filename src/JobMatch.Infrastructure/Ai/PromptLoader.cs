namespace JobMatch.Infrastructure.Ai;

public record Prompt(string Version, string Text);

// prompts are markdown files in /prompts, the file name is the version (e.g. extraction.v1)
public class PromptLoader
{
    private readonly string _folder;

    public PromptLoader() : this(Path.Combine(AppContext.BaseDirectory, "prompts"))
    {
    }

    public PromptLoader(string folder)
    {
        _folder = folder;
    }

    public Prompt Load(string name)
    {
        string path = Path.Combine(_folder, name + ".md");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Prompt file not found: {path}");
        }
        return new Prompt(name, File.ReadAllText(path));
    }
}
