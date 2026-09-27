using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pgvector;

namespace JobMatch.Infrastructure.Data;

// Core uses float[] (no pgvector dependency there), the database column is vector(768)
public static class VectorConversion
{
    public static PropertyBuilder<float[]> IsVector(this PropertyBuilder<float[]> property)
    {
        var comparer = new ValueComparer<float[]>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            v => v.Length,
            v => v.ToArray());

        return property
            .HasColumnType($"vector({AppDbContext.EmbeddingDimensions})")
            .HasConversion(v => new Vector(v), v => v.ToArray(), comparer);
    }
}
