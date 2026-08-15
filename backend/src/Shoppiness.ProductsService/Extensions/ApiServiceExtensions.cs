using FluentValidation;
using Shoppiness.ProductService.Features.Catalog.Sections;
using Shoppiness.ProductService.Features.Catalog.Sections.Resolvers;
using Shoppiness.ProductService.Features.Products.Purchase;
using Shoppiness.ProductService.Features.Products.Search;
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

        // Register Search handler — scoped because it uses ProductDbContext (also scoped)
        services.AddScoped<SearchProductsHandler>();

        // Register Catalog Sections handler — scoped because it uses ProductDbContext (also scoped)
        services.AddScoped<GetCatalogSectionsHandler>();

        // Register one ICatalogSectionResolver per CatalogSectionType (design D2 — SRP/OCP).
        // GetCatalogSectionsHandler resolves IEnumerable<ICatalogSectionResolver> and indexes it by
        // SectionType. Adding a future section type (e.g. BestSellers) is: add a new resolver class
        // implementing ICatalogSectionResolver, then add one more AddScoped line here — no existing
        // resolver, the handler, or this file's other registrations need to change.
        services.AddScoped<ICatalogSectionResolver, NewSectionResolver>();
        services.AddScoped<ICatalogSectionResolver, OffersSectionResolver>();

        services
            .AddRefitClient<IStocksApiClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https+http://stocks-api"));

        // services.AddSingleton<IServiceBusPublisher, ServiceBusPublisher>();

        return services;
    }
}
