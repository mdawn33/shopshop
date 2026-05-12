namespace StockService.Domain.Entities;

/// <summary>
/// Represents the current inventory level for a single product (single-warehouse model, D3).
/// <see cref="ProductId"/> is unique — one Stock record per product.
/// </summary>
public class Stock
{
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the Product in the Product microservice.
    /// Unique constraint enforced at the database level.
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Current quantity on hand. Must never go below zero (enforced by check constraint).
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Quantity reserved for pending orders. Reduces available stock without physically removing it.
    /// Available quantity = <see cref="Quantity"/> - <see cref="QuantityReserved"/>.
    /// </summary>
    public int QuantityReserved { get; set; }

    /// <summary>
    /// Minimum quantity on hand that triggers a reorder alert or automatic replenishment.
    /// A reorder is required when <see cref="Quantity"/> falls at or below this value.
    /// </summary>
    public int ReorderLevel { get; set; }

    /// <summary>
    /// Quantity to order when stock reaches <see cref="ReorderLevel"/>.
    /// Represents the standard replenishment batch size for this product.
    /// </summary>
    public int ReorderQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<StockMovement> Movements { get; set; } = new List<StockMovement>();
}
