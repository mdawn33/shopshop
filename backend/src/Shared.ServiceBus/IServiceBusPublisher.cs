namespace Shared.ServiceBus;

public interface IServiceBusPublisher
{
    Task PublishAsync<T>(string queueOrTopic, T message, CancellationToken cancellationToken = default);
}