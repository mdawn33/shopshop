using StockService.Domain.Enums;

namespace StockService.Domain.Entities;

/// <summary>
/// Immutable audit record for every change to a <see cref="Stock"/> quantity (D1 in design.md).
/// Has no <c>IsActive</c> or <c>UpdatedAt</c> — records are append-only and must not be mutated.
/// </summary>
public class StockMovement
{
    public Guid Id { get; set; }

    public Guid StockId { get; set; }

    /// <summary>Classifies the reason this movement was created.</summary>
    public MovementType MovementType { get; set; }

    /// <summary>
    /// Delta applied to <see cref="Stock.Quantity"/>. Always positive —
    /// direction is implied by <see cref="MovementType"/>.
    /// </summary>
    public int Quantity { get; set; }

    /// <summary>
    /// Optional: human-readable identifier of the external record that triggered this movement
    /// (e.g., "PurchaseOrder", "SalesOrder").
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Optional: ID of the external record that triggered this movement.
    /// </summary>
    public Guid? ReferenceId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property
    public Stock? Stock { get; set; }
}
