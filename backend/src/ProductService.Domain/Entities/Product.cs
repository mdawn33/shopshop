namespace ProductService.Domain.Entities;

/// <summary>
/// Represents a product available for purchase in the catalog.
/// </summary>
public class Product
{
    public Guid Id { get; set; }

    public Guid CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Product brand or manufacturer label. Not every product realistically has one
    /// (e.g. books, groceries) — nullable rather than a fabricated placeholder.
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Catalog SKU. No DB-level uniqueness constraint in this change (see design's Open Questions).
    /// </summary>
    public string? Sku { get; set; }

    /// <summary>
    /// Descriptive variant (e.g. color, size, capacity). A single free-text attribute, not a
    /// first-class variant/SKU-matrix model.
    /// </summary>
    public string? Variant { get; set; }

    /// <summary>
    /// Fallback price used when no active <see cref="ProductPrice"/> record exists.
    /// </summary>
    public decimal BasePrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Category? Category { get; set; }

    public ICollection<ProductPrice> Prices { get; set; } = new List<ProductPrice>();

    /// <summary>
    /// Returns the active <see cref="ProductPrice"/> at <paramref name="at"/>,
    /// or <c>null</c> when no active record exists. Use <see cref="BasePrice"/> as fallback (D4).
    /// </summary>
    public decimal? GetActivePrice(DateTime at) =>
        Prices
            .Where(pp => pp.StartDate <= at && (pp.EndDate == null || pp.EndDate >= at))
            .OrderByDescending(pp => pp.StartDate)
            .FirstOrDefault()?.Price;
}
