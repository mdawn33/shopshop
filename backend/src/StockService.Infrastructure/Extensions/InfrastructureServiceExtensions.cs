using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StockService.Infrastructure.Persistence;

namespace StockService.Infrastructure.Extensions;

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
        services.AddDbContext<StockDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("StockPgDb"),
                npgsql => npgsql.MigrationsAssembly(typeof(StockDbContext).Assembly.FullName)));
        

        return services;
    }
}
