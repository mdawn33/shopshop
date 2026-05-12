using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockService.Domain.Entities;

namespace StockService.Infrastructure.Persistence.Configurations;

internal sealed class StockConfiguration : IEntityTypeConfiguration<Stock>
{
    public void Configure(EntityTypeBuilder<Stock> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.ProductId)
            .IsRequired();

        // D3: single-warehouse — one Stock record per product
        builder.HasIndex(s => s.ProductId)
            .IsUnique();

        builder.Property(s => s.Quantity)
            .IsRequired();

        // Secondary guard against negative quantities (application-level 409 is primary)
        builder.ToTable(t => t.HasCheckConstraint("CK_Stocks_Quantity_NonNegative", "\"Quantity\" >= 0"));

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Global query filter — soft-deleted Stock records are invisible by default (D2 in design.md)
        builder.HasQueryFilter(s => s.IsActive);
    }
}
