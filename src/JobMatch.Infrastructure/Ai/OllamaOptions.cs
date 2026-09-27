namespace JobMatch.Infrastructure.Ai;

public class OllamaOptions
{
    public string Url { get; set; } = "http://localhost:11434";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
}
