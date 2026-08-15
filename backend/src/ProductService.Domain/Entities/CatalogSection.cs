using ProductService.Domain.Enums;

namespace ProductService.Domain.Entities;

/// <summary>
/// A configured, ordered homepage catalog section (e.g. "Nuevos ingresos", "Ofertas del día")
/// whose product list is resolved at request time by <see cref="SectionType"/>'s rule, rather
/// than stored as an explicit product-to-section mapping (design D1).
/// </summary>
public class CatalogSection
{
    public Guid Id { get; set; }

    /// <summary>
    /// Display label shown on the homepage (e.g. "Nuevos ingresos").
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Ascending sort key for section order on the homepage. Not unique — ties are broken by
    /// <see cref="CreatedAt"/> ascending in the query.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// The resolution-rule discriminator (design D2).
    /// </summary>
    public CatalogSectionType SectionType { get; set; }

    /// <summary>
    /// Caps how many products this section resolves to. Defaults to <c>12</c>.
    /// </summary>
    public int MaxItems { get; set; } = 12;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
