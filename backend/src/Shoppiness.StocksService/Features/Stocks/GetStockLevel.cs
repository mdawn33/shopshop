using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using StockService.Infrastructure.Persistence;

namespace Shoppiness.StockService.Features.Stocks;

public static class GetStockLevel
{
    public sealed record Response(Guid ProductId, int Quantity, DateTime UpdatedAt);

    public static async Task<Results<Ok<Response>, NotFound>> HandleGetStocksLevelAsync(
        Guid productId,
        StockDbContext db,
        CancellationToken cancellationToken)
    {
        var stock = await db.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);

        if (stock is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(new Response(stock.ProductId, stock.Quantity, stock.UpdatedAt));
    }
    
    public static async Task<Results<Ok<Response>, NotFound, BadRequest>> HandleCheckStocksLevelAsync(
        Guid productId,
        int quantity,
        StockDbContext db,
        CancellationToken cancellationToken)
    {
        var stock = await db.Stocks
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.ProductId == productId, cancellationToken);
        
        if (quantity <= 0)
        {
            return TypedResults.BadRequest();
        }
        
        if (stock is null)
            return TypedResults.NotFound();
        
        return TypedResults.Ok(new Response(stock.ProductId, stock.Quantity, stock.UpdatedAt));
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/stocks/{productId:guid}", HandleGetStocksLevelAsync)
            .WithName("GetStockLevel")
            .WithTags("Stocks")
            .WithSummary("Get current stock level for a product")
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        routes.MapGet("/stocks/check/{productId:guid}", HandleCheckStocksLevelAsync)
            .WithName("HasEnoughStock")
            .WithTags("Stocks")
            .WithSummary("Check if there is enough stock for a product")
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);
    }
}
