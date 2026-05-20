using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.ServiceBus;

public class ServiceBusQueueConsumerService<TMessage>(
    ServiceBusClient client,
    IServiceScopeFactory scopeFactory,
    ILogger<ServiceBusQueueConsumerService<TMessage>> logger,
    string queueName) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // var processor = client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        // {
        //     AutoCompleteMessages = true,
        //     MaxConcurrentCalls = 10
        // });
        //
        // processor.ProcessMessageAsync += async args =>
        // {
        //     try
        //     {
        //         var message = JsonSerializer.Deserialize<TMessage>(args.Message.Body.ToString());
        //         if (message is not null)
        //         {
        //             using var scope = scopeFactory.CreateScope();
        //             var handler = scope.ServiceProvider.GetRequiredService<IServiceBusMessageHandler<TMessage>>();
        //             await handler.HandleAsync(message, stoppingToken);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
        //         throw;
        //     }
        // };
        //
        // processor.ProcessErrorAsync += args =>
        // {
        //     logger.LogError(args.Exception, "Error in Service Bus processor for queue {QueueName}", queueName);
        //     return Task.CompletedTask;
        // };
        //
        // await processor.StartProcessingAsync(stoppingToken);
        //
        // while (!stoppingToken.IsCancellationRequested) {}
        //
        // await processor.StopProcessingAsync(stoppingToken);
        // await processor.DisposeAsync();
    }
}