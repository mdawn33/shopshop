using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Products;

public static class GetById
{
    public sealed record Response(
        Guid Id,
        string Name,
        string? Description,
        decimal BasePrice,
        Guid CategoryId,
        string CategoryName,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public static async Task<Results<Ok<Response>, NotFound>> HandleAsync(
        Guid id,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return TypedResults.NotFound();

        var response = new Response(
            product.Id,
            product.Name,
            product.Description,
            product.BasePrice,
            product.CategoryId,
            product.Category!.Name,
            product.CreatedAt,
            product.UpdatedAt);

        return TypedResults.Ok(response);
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/products/{id:guid}", HandleAsync)
            .WithName("GetProductById")
            .WithTags("Products")
            .WithSummary("Get a product by ID including its category")
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
