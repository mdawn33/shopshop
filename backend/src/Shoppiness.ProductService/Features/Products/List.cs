using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Products;

public static class List
{
    public sealed record ProductItem(
        Guid Id,
        string Name,
        string? Description,
        decimal BasePrice,
        Guid CategoryId,
        string CategoryName,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public static async Task<Ok<IReadOnlyList<ProductItem>>> HandleAsync(
        Guid? categoryId,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Products.Include(p => p.Category).AsQueryable();

        if (categoryId.HasValue)
            query = query.Where(p => p.CategoryId == categoryId.Value);

        var products = await query
            .OrderBy(p => p.Name)
            .Select(p => new ProductItem(
                p.Id,
                p.Name,
                p.Description,
                p.BasePrice,
                p.CategoryId,
                p.Category!.Name,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<ProductItem>>(products);
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/products", HandleAsync)
            .WithName("ListProducts")
            .WithTags("Products")
            .WithSummary("List active products, optionally filtered by category")
            .Produces<IReadOnlyList<ProductItem>>(StatusCodes.Status200OK);
    }
}
