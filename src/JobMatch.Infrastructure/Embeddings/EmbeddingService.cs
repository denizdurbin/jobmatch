using JobMatch.Infrastructure.Data;
using Microsoft.Extensions.AI;

namespace JobMatch.Infrastructure.Embeddings;

public class EmbeddingService(IEmbeddingGenerator<string, Embedding<float>> generator)
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken ct = default)
    {
        // nomic-embed-text works better with a task prefix
        ReadOnlyMemory<float> vector = await generator.GenerateVectorAsync("search_document: " + text, cancellationToken: ct);

        if (vector.Length != AppDbContext.EmbeddingDimensions)
        {
            throw new InvalidOperationException(
                $"Embedding has {vector.Length} dimensions but the database expects {AppDbContext.EmbeddingDimensions}. Did you change the model?");
        }
        return vector.ToArray();
    }
}
