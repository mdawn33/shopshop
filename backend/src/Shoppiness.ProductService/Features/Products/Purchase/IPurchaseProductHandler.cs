namespace Shoppiness.ProductService.Features.Products.Purchase;

/// <summary>
/// Inbound port for the purchase product use case.
/// </summary>
public interface IPurchaseProductHandler
{
    Task<PurchaseProductResult> HandleAsync(
        Guid productId,
        PurchaseProductRequest request,
        CancellationToken cancellationToken);
}
