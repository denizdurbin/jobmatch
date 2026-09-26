using System.Text.Json;
using JobMatch.Core.Extraction;
using JobMatch.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JobMatch.Infrastructure.Extraction;

public record ExtractionAttempt(bool Success, ExtractionResult? Result, string? RawJson, string? Error, string PromptVersion, string Model);

public class PostingExtractor(IChatClient chatClient, PromptLoader prompts, IOptions<GroqOptions> groqOptions, ILogger<PostingExtractor> logger)
{
    public const string PromptName = "extraction.v1";
    private const int MaxAttempts = 2;

    public async Task<ExtractionAttempt> ExtractAsync(string rawText, CancellationToken ct = default)
    {
        Prompt prompt = prompts.Load(PromptName);
        string model = groqOptions.Value.Model;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, prompt.Text),
            new(ChatRole.User, "Job posting:\n\n" + rawText)
        };
        var chatOptions = new ChatOptions { Temperature = 0 };

        List<string> errors = new();
        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            // GetResponseAsync<T> sends a json schema generated from ExtractionResult
            ChatResponse<ExtractionResult> response = await chatClient.GetResponseAsync<ExtractionResult>(messages, chatOptions, cancellationToken: ct);

            ExtractionResult? result = null;
            try
            {
                result = response.Result;
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException)
            {
                logger.LogWarning("Attempt {Attempt}: response was not valid JSON", attempt);
            }

            if (result == null)
            {
                errors = ["The answer was not valid JSON matching the schema."];
            }
            else
            {
                var outcome = ExtractionValidator.Validate(result, rawText);
                foreach (var dropped in outcome.DroppedSkills)
                {
                    logger.LogInformation("Dropped skill '{Skill}': evidence '{Evidence}' not found in the posting", dropped.Name, dropped.Evidence);
                }

                if (outcome.IsValid)
                {
                    return new ExtractionAttempt(true, outcome.Cleaned, response.Text, null, prompt.Version, model);
                }
                errors = outcome.Errors;
                logger.LogWarning("Attempt {Attempt} had validation errors: {Errors}", attempt, string.Join("; ", errors));
            }

            // give the model its answer and the problems, then ask again
            messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));
            messages.Add(new ChatMessage(ChatRole.User,
                "Your answer has these problems:\n- " + string.Join("\n- ", errors) + "\nReturn the corrected JSON."));
        }

        return new ExtractionAttempt(false, null, null, "Extraction failed: " + string.Join("; ", errors), prompt.Version, model);
    }
}
