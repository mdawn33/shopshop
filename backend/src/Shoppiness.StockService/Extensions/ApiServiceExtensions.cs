using FluentValidation;
using Shoppiness.StockService.Features.Stocks;
using Shoppiness.StockService.Messaging;

namespace Shoppiness.StockService.Extensions;

/// <summary>
/// Registers all API-layer services with the DI container (validators, stubs, etc.).
/// Called once from <c>Program.cs</c>.
/// </summary>
public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssemblyContaining<InitializeStock.Validator>();

        // Register stub publisher — replace with real Azure Service Bus implementation when ready
        services.AddScoped<IStockEventPublisher, StockEventPublisherStub>();

        return services;
    }
}
