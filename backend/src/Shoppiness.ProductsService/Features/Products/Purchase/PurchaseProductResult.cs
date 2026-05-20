namespace Shoppiness.ProductService.Features.Products.Purchase;

public sealed record PurchaseProductResult(bool Success, string? Error, PurchaseProductResponse? Response, int StatusCode);
