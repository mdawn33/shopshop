using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockService.Domain.Entities;

namespace StockService.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MovementType)
            .IsRequired();

        builder.Property(m => m.Quantity)
            .IsRequired();

        builder.Property(m => m.ReferenceType)
            .HasMaxLength(200);

        builder.Property(m => m.CreatedAt)
            .IsRequired();

        // Append-only audit records — restricting delete prevents orphaned movements
        // No soft-delete filter (D1 in design.md)
        builder.HasOne(m => m.Stock)
            .WithMany(s => s.Movements)
            .HasForeignKey(m => m.StockId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
