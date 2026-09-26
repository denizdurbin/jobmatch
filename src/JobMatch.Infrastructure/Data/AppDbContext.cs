using Microsoft.EntityFrameworkCore;

namespace JobMatch.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // pgvector extension, needed later for the embeddings
        modelBuilder.HasPostgresExtension("vector");
    }
}
