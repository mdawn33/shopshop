using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Persistence.Configurations;

internal sealed class ProductPriceConfiguration : IEntityTypeConfiguration<ProductPrice>
{
    public void Configure(EntityTypeBuilder<ProductPrice> builder)
    {
        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.Price)
            .IsRequired()
            .HasPrecision(18, 4);

        builder.Property(pp => pp.PriceType)
            .IsRequired();

        builder.Property(pp => pp.StartDate)
            .IsRequired();

        builder.HasOne(pp => pp.Product)
            .WithMany(p => p.Prices)
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        // Mirror the Product IsActive filter so prices of soft-deleted products are excluded
        builder.HasQueryFilter(pp => pp.Product.IsActive);

        // Index to optimise active-price lookups
        builder.HasIndex(pp => new { pp.ProductId, pp.StartDate });
    }
}
