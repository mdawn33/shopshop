namespace StockService.Domain.Enums;

/// <summary>
/// Classifies the reason a <see cref="Entities.StockMovement"/> was created.
/// </summary>
public enum MovementType
{
    /// <summary>Stock increased due to a purchase order or supplier delivery.</summary>
    Replenishment,

    /// <summary>Stock decreased due to a sales order fulfilment.</summary>
    SalesDeduction,

    /// <summary>Stock adjusted manually (e.g., damage write-off, inventory count correction).</summary>
    ManualAdjustment,

    /// <summary>Stock increased due to a customer return.</summary>
    Return
}
