namespace JobMatch.Infrastructure.Ai;

public class GroqOptions
{
    public string ApiKey { get; set; } = "";
    // groq has an openai compatible api, so the openai client works with it
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public string Model { get; set; } = "openai/gpt-oss-120b";
}
