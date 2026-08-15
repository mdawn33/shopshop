using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;
using Shoppiness.ProductService.Features.Products.Search;

namespace Shoppiness.ProductService.Features.Products;

public static class GetById
{
    public sealed record Response(
        Guid Id,
        string Name,
        string? Description,
        string? Brand,
        string? Sku,
        string? Variant,
        decimal BasePrice,
        decimal Price,
        Guid CategoryId,
        string CategoryName,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public static async Task<Results<Ok<Response>, NotFound>> HandleAsync(
        Guid id,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Resolved effective price (design D1): active ProductPrice as of now, else BasePrice —
        // computed via the same shared helper Features/Products/Search/SearchProductsHandler.cs
        // and Features/Catalog/Sections/GetCatalogSectionsHandler.cs use (design D3), so this
        // detail response always agrees with the list/section views on what a product costs.
        var response = await db.Products
            .Where(p => p.Id == id)
            .SelectWithEffectivePrice(now)
            .Select(x => new Response(
                x.Product.Id,
                x.Product.Name,
                x.Product.Description,
                x.Product.Brand,
                x.Product.Sku,
                x.Product.Variant,
                x.Product.BasePrice,
                x.EffectivePrice,
                x.Product.CategoryId,
                x.Product.Category!.Name,
                x.Product.CreatedAt,
                x.Product.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return response is null ? TypedResults.NotFound() : TypedResults.Ok(response);
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
