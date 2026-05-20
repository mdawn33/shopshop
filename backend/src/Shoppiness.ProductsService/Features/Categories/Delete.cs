using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Categories;

public static class Delete
{
    public static async Task<Results<NoContent, NotFound, Conflict<string>>> HandleAsync(
        Guid id,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
            return TypedResults.NotFound();

        // Check for active subcategories — use IgnoreQueryFilters to avoid double-filtering on IsActive
        var hasActiveSubcategories = await db.Categories
            .AnyAsync(c => c.ParentCategoryId == id && c.IsActive, cancellationToken);

        if (hasActiveSubcategories)
            return TypedResults.Conflict(
                $"Category '{id}' cannot be deleted because it has active subcategories. Delete or deactivate subcategories first.");

        category.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/categories/{id:guid}", HandleAsync)
            .WithName("DeleteCategory")
            .WithTags("Categories")
            .WithSummary("Soft-delete a category. Returns 409 if the category has active subcategories.")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces<string>(StatusCodes.Status409Conflict);
    }
}
