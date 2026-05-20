using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Infrastructure.Extensions;

/// <summary>
/// Registers all Infrastructure services with the DI container.
/// Called once from the API project's <c>Program.cs</c>.
/// </summary>
public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ProductDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("ProductsPgDb"),
                npgsql => npgsql.MigrationsAssembly(typeof(ProductDbContext).Assembly.FullName)));

        return services;
    }
}
