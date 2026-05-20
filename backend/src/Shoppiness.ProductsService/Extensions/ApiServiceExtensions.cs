using FluentValidation;
using Shoppiness.ProductService.Features.Products.Purchase;
using Shoppiness.ProductService.Features.Stocks;
using Shared.ServiceBus;
using Refit;


namespace Shoppiness.ProductService.Extensions;

/// <summary>
/// Registers all API-layer services with the DI container (validators, handlers, etc.).
/// Called once from <c>Program.cs</c>.
/// </summary>
public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // Register all FluentValidation validators from this assembly
        services.AddValidatorsFromAssemblyContaining<PurchaseProductValidator>();

        // Register Purchase handler — scoped because it uses ProductDbContext (also scoped)
        services.AddScoped<IPurchaseProductHandler, PurchaseProductHandler>();
        
        services
            .AddRefitClient<IStocksApiClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https+http://stocks-api"));

        services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();

        return services;
    }
}
