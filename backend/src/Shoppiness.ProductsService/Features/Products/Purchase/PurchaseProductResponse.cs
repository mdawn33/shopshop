namespace Shoppiness.ProductService.Features.Products.Purchase;

/// <summary>
/// Response returned when a product purchase is successfully processed.
/// </summary>
public sealed record PurchaseProductResponse(
    Guid OrderId,
    Guid ProductId,
    decimal UnitPrice,
    decimal Total);
