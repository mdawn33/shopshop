using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Shared.ServiceBus;

public class ServiceBusPublisher(ServiceBusClient client) : IServiceBusPublisher, IAsyncDisposable
{
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

    public async Task PublishAsync<T>(string queueOrTopic, T message,
        CancellationToken cancellationToken = default)
    {
        var sender = _senders.GetOrAdd(queueOrTopic, client.CreateSender);

        var json = JsonSerializer.Serialize(message);
        var serviceBusMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json",
            MessageId = Guid.NewGuid().ToString()
        };

        await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var sender in _senders.Values)
        {
            await sender.DisposeAsync();
        }

        _senders.Clear();
    }

}