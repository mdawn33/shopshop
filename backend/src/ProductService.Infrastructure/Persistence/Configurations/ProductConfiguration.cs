using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        builder.Property(p => p.Brand)
            .HasMaxLength(200);

        builder.Property(p => p.Sku)
            .HasMaxLength(100);

        builder.Property(p => p.Variant)
            .HasMaxLength(200);

        builder.Property(p => p.BasePrice)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        // Global query filter — only return active products by default
        builder.HasQueryFilter(p => p.IsActive);
    }
}
