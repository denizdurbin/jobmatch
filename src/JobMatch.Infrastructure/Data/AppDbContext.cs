using JobMatch.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobMatch.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Posting> Postings => Set<Posting>();

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
        });
    }
}
