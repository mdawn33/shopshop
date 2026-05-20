namespace Shoppiness.ProductService.Features.Products.Purchase;

/// <summary>
/// Request payload for purchasing a product.
/// </summary>
public sealed record PurchaseProductRequest(Guid CustomerId, int Quantity);
