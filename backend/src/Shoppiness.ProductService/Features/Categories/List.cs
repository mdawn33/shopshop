using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Categories;

public static class List
{
    public sealed record CategoryItem(
        Guid Id,
        string Name,
        string? Description,
        Guid? ParentCategoryId,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public static async Task<Ok<IReadOnlyList<CategoryItem>>> HandleAsync(
        Guid? parentCategoryId,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var query = db.Categories.AsQueryable();

        if (parentCategoryId.HasValue)
            query = query.Where(c => c.ParentCategoryId == parentCategoryId.Value);

        var categories = await query
            .OrderBy(c => c.Name)
            .Select(c => new CategoryItem(c.Id, c.Name, c.Description, c.ParentCategoryId, c.CreatedAt, c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<CategoryItem>>(categories);
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/categories", HandleAsync)
            .WithName("ListCategories")
            .WithTags("Categories")
            .WithSummary("List active categories, optionally filtered by parent")
            .Produces<IReadOnlyList<CategoryItem>>(StatusCodes.Status200OK);
    }
}
