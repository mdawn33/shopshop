namespace Shoppiness.StockService.Messaging;

/// <summary>
/// No-op stub implementation of <see cref="IStockEventPublisher"/>.
/// Logs a warning on every call so the absence of real publishing is visible in logs.
/// Replace this with the Azure Service Bus implementation in a future change.
/// </summary>
internal sealed class StockEventPublisherStub : IStockEventPublisher
{
    private readonly ILogger<StockEventPublisherStub> _logger;

    public StockEventPublisherStub(ILogger<StockEventPublisherStub> logger)
    {
        _logger = logger;
    }

    public Task PublishStockUpdatedAsync(Guid productId, int newQuantity)
    {
        _logger.LogWarning(
            "StockEventPublisherStub is active — stock update for ProductId={ProductId} NewQuantity={NewQuantity} was NOT published. Wire a real IStockEventPublisher implementation to enable event publishing.",
            productId,
            newQuantity);

        return Task.CompletedTask;
    }
}
