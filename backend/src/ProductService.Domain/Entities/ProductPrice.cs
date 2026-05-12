using ProductService.Domain.Enums;

namespace ProductService.Domain.Entities;

/// <summary>
/// Represents a time-bounded price for a product.
/// An active price record is one where <see cref="StartDate"/> &lt;= now and (<see cref="EndDate"/> &gt;= now OR <see cref="EndDate"/> is null).
/// </summary>
public class ProductPrice
{
    public Guid Id { get; set; }

    public Guid ProductId { get; set; }

    public decimal Price { get; set; }

    public PriceType PriceType { get; set; }

    public DateTime StartDate { get; set; }

    /// <summary>
    /// Null means the price is open-ended with no expiry.
    /// </summary>
    public DateTime? EndDate { get; set; }

    // Navigation property
    public Product Product { get; set; } = null!;
}
