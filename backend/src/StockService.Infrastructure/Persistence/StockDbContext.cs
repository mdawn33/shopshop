using Microsoft.EntityFrameworkCore;
using StockService.Domain.Entities;
using StockService.Infrastructure.Persistence.Configurations;

namespace StockService.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Stock microservice.
/// </summary>
public sealed class StockDbContext : DbContext
{
    public StockDbContext(DbContextOptions<StockDbContext> options) : base(options)
    {
    }

    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new StockConfiguration());
        modelBuilder.ApplyConfiguration(new StockMovementConfiguration());
    }

    /// <summary>
    /// Automatically sets <c>UpdatedAt</c> on any modified <see cref="Stock"/> entry before persisting.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<Stock>())
        {
            if (entry.State != EntityState.Modified)
                continue;

            entry.Property(s => s.UpdatedAt).CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
