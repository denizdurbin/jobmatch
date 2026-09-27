namespace JobMatch.Core.Entities;

public class PostingEmbedding
{
    public int Id { get; set; }
    public Guid PostingId { get; set; }
    public EmbeddingKind Kind { get; set; }
    public float[] Vector { get; set; } = [];
}
