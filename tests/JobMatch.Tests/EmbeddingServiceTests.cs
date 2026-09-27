using JobMatch.Infrastructure.Embeddings;
using JobMatch.Tests.Fakes;

namespace JobMatch.Tests;

public class EmbeddingServiceTests
{
    [Fact]
    public async Task Returns_vector_and_adds_prefix()
    {
        var fake = new FakeEmbeddingGenerator(_ => FakeEmbeddingGenerator.UnitVector(0));

        float[] vector = await new EmbeddingService(fake).EmbedAsync("hello");

        vector.Length.ShouldBe(768);
        fake.Inputs.Single().ShouldBe("search_document: hello");
    }

    [Fact]
    public async Task Wrong_dimension_throws()
    {
        var fake = new FakeEmbeddingGenerator(_ => new float[384]);

        await Should.ThrowAsync<InvalidOperationException>(() => new EmbeddingService(fake).EmbedAsync("hello"));
    }
}
