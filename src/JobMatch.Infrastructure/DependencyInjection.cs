using System.ClientModel;
using JobMatch.Infrastructure.Ai;
using JobMatch.Infrastructure.Data;
using JobMatch.Infrastructure.Embeddings;
using JobMatch.Infrastructure.Extraction;
using JobMatch.Infrastructure.Gap;
using JobMatch.Infrastructure.Ingestion;
using JobMatch.Infrastructure.Matching;
using JobMatch.Infrastructure.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OpenAI;

namespace JobMatch.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        string connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));

        services.AddHttpClient<UrlTextFetcher>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobMatch/0.1 (personal project)");
        });

        // LLM (groq)
        services.Configure<GroqOptions>(config.GetSection("Groq"));
        services.AddSingleton<PromptLoader>();
        services.AddSingleton<IChatClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GroqOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.ApiKey))
            {
                throw new InvalidOperationException(
                    "Groq:ApiKey is not set. Run: dotnet user-secrets set \"Groq:ApiKey\" \"<your key>\" --project src/JobMatch.Api");
            }

            var client = new OpenAIClient(new ApiKeyCredential(options.ApiKey), new OpenAIClientOptions
            {
                Endpoint = new Uri(options.BaseUrl)
            });
            return client.GetChatClient(options.Model).AsIChatClient();
        });

        // embeddings (ollama, runs in docker compose)
        services.Configure<OllamaOptions>(config.GetSection("Ollama"));
        services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
            return new OllamaApiClient(new Uri(options.Url), options.EmbeddingModel);
        });

        services.AddScoped<PostingExtractor>();
        services.AddScoped<EmbeddingService>();
        services.AddScoped<PostingPipeline>();

        services.Configure<MatchingOptions>(config.GetSection("Matching"));
        services.AddScoped<MatchingService>();
        services.AddScoped<GapReportService>();

        return services;
    }
}
