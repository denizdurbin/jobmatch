using JobMatch.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobMatch.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    // nomic-embed-text gives 768 numbers per text. if the model changes, this and the migrations change too
    public const int EmbeddingDimensions = 768;

    public DbSet<Posting> Postings => Set<Posting>();
    public DbSet<PostingExtraction> PostingExtractions => Set<PostingExtraction>();
    public DbSet<PostingSkill> PostingSkills => Set<PostingSkill>();
    public DbSet<PostingEmbedding> PostingEmbeddings => Set<PostingEmbedding>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // pgvector extension, needed later for the embeddings
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<Posting>(posting =>
        {
            posting.Property(p => p.Source).HasConversion<string>().HasMaxLength(20);
            posting.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);
            posting.Property(p => p.ContentHash).HasMaxLength(64);
            posting.HasIndex(p => p.ContentHash).IsUnique();
            posting.HasIndex(p => new { p.Source, p.ExternalId }).IsUnique();
            posting.HasIndex(p => p.Status);

            posting.HasOne(p => p.Extraction).WithOne().HasForeignKey<PostingExtraction>(e => e.PostingId).OnDelete(DeleteBehavior.Cascade);
            posting.HasMany(p => p.Skills).WithOne().HasForeignKey(s => s.PostingId).OnDelete(DeleteBehavior.Cascade);
            posting.HasMany(p => p.Embeddings).WithOne().HasForeignKey(e => e.PostingId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PostingExtraction>(extraction =>
        {
            extraction.HasKey(e => e.PostingId);
            extraction.Property(e => e.LanguagesJson).HasColumnType("jsonb");
            extraction.Property(e => e.RawJson).HasColumnType("jsonb");
            extraction.Property(e => e.SalaryMin).HasPrecision(12, 2);
            extraction.Property(e => e.SalaryMax).HasPrecision(12, 2);
            extraction.HasIndex(e => e.City);
            extraction.HasIndex(e => e.ContractType);
        });

        modelBuilder.Entity<PostingSkill>(skill =>
        {
            skill.HasIndex(s => s.NormalizedName);
        });

        modelBuilder.Entity<PostingEmbedding>(embedding =>
        {
            embedding.Property(e => e.Kind).HasConversion<string>().HasMaxLength(20);
            embedding.Property(e => e.Vector).IsVector();
            embedding.HasIndex(e => new { e.PostingId, e.Kind }).IsUnique();
            // HNSW index for fast nearest neighbour search with cosine distance
            embedding.HasIndex(e => e.Vector).HasMethod("hnsw").HasOperators("vector_cosine_ops");
        });
    }
}
