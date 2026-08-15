using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ProductService.Domain.Entities;

namespace ProductService.Infrastructure.Persistence.Configurations;

internal sealed class CatalogSectionConfiguration : IEntityTypeConfiguration<CatalogSection>
{
    public void Configure(EntityTypeBuilder<CatalogSection> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.DisplayOrder)
            .IsRequired();

        builder.Property(s => s.SectionType)
            .IsRequired();

        builder.Property(s => s.MaxItems)
            .IsRequired()
            .HasDefaultValue(12);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Global query filter — only return active sections by default
        builder.HasQueryFilter(s => s.IsActive);
    }
}
