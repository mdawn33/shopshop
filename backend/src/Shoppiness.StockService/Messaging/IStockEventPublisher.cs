namespace Shoppiness.StockService.Messaging;

/// <summary>
/// Declares the port for publishing stock-change events to downstream services.
/// The concrete implementation (Azure Service Bus) is out of scope for Phase 1.
/// The registered implementation is <see cref="StockEventPublisherStub"/> until wired.
/// </summary>
public interface IStockEventPublisher
{
    /// <summary>
    /// Publishes a notification that the stock level for <paramref name="productId"/>
    /// has changed to <paramref name="newQuantity"/>.
    /// </summary>
    Task PublishStockUpdatedAsync(Guid productId, int newQuantity);
}
