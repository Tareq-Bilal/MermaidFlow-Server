using MermaidFlow.Domain.Auth;
using MermaidFlow.Domain.Documents;
using MermaidFlow.Domain.Mermaid;
using MermaidFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace MermaidFlow.Infrastructure.Persistence;

public class MermaidFlowDbContext : DbContext
{
    public MermaidFlowDbContext(DbContextOptions<MermaidFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<DiagramCache> DiagramCaches { get; set; }
    public DbSet<Theme> Themes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MermaidFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
