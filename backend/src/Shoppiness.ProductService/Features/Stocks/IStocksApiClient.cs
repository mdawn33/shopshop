using Refit;

namespace Shoppiness.ProductService.Features.Stocks;

/// <summary>
/// Driven port for stock availability checks.
/// </summary>
public interface IStocksApiClient
{
    /// <summary>
    /// Returns true if the requested <paramref name="quantity"/> of the specified product is available.
    /// </summary>
    [Get("/stocks/check/{productId}/{quantity}")]
    Task<bool> IsAvailableAsync(Guid productId, int quantity, CancellationToken cancellationToken = default);
}
