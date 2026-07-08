using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;
using Shared.ServiceBus;
using SharedContracts.Events;
using Shoppiness.ProductService.Features.Stocks;

namespace Shoppiness.ProductService.Features.Products.Purchase;

public sealed class PurchaseProductHandler(
    ProductDbContext db,
    IStocksApiClient stocksApiClient,
    // IServiceBusPublisher busPublisher,
    ILogger<PurchaseProductHandler> logger)
    : IPurchaseProductHandler
{
    public async Task<PurchaseProductResult> HandleAsync(
        Guid productId,
        PurchaseProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await db.Products
            .Include(p => p.Prices)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product is null)
            return new PurchaseProductResult(false, "Product not found.", null, StatusCodes.Status404NotFound);

        var now = DateTime.UtcNow;
        var activePrice = product.GetActivePrice(now);
        var unitPrice = activePrice ?? product.BasePrice;

        if (activePrice is null)
            logger.LogWarning(
                "No active ProductPrice found for product {ProductId}. Falling back to BasePrice {BasePrice}.",
                product.Id,
                product.BasePrice);

        // Check stock availability
        var isAvailable = await stocksApiClient.IsAvailableAsync(productId, request.Quantity, cancellationToken);
        if (!isAvailable)
            return new PurchaseProductResult(false, "Insufficient stock for the requested quantity.", null, StatusCodes.Status409Conflict);

        // await busPublisher.PublishAsync("update-stock", new UpdateStockEvent(productId, -request.Quantity), cancellationToken);
        
        var orderId = Guid.NewGuid();

        var response = new PurchaseProductResponse(
            OrderId: orderId,
            ProductId: productId,
            UnitPrice: unitPrice,
            Total: unitPrice * request.Quantity);

        return new PurchaseProductResult(true, null, response, StatusCodes.Status200OK);
    }
}
