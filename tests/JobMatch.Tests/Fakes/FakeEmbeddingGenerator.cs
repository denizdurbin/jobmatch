using Microsoft.Extensions.AI;

namespace JobMatch.Tests.Fakes;

// gives back a vector made by a function instead of calling ollama
public class FakeEmbeddingGenerator(Func<string, float[]> makeVector) : IEmbeddingGenerator<string, Embedding<float>>
{
    public List<string> Inputs { get; } = new();

    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var result = new GeneratedEmbeddings<Embedding<float>>();
        foreach (var value in values)
        {
            Inputs.Add(value);
            result.Add(new Embedding<float>(makeVector(value)));
        }
        return Task.FromResult(result);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    public static float[] UnitVector(int index, int dimensions = 768)
    {
        var v = new float[dimensions];
        v[index] = 1f;
        return v;
    }
}
