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
