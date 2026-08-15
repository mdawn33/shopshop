using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Persistence.Configurations;

namespace ProductService.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Product microservice.
/// </summary>
public sealed class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductPrice> ProductPrices => Set<ProductPrice>();
    public DbSet<CatalogSection> CatalogSections => Set<CatalogSection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CategoryConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductPriceConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogSectionConfiguration());
    }

    /// <summary>
    /// Automatically sets <c>UpdatedAt</c> on any modified entity that exposes the property before persisting.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified)
                continue;

            var updatedAt = entry.Properties.FirstOrDefault(p => p.Metadata.Name == nameof(Category.UpdatedAt));
            if (updatedAt is not null)
                updatedAt.CurrentValue = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
