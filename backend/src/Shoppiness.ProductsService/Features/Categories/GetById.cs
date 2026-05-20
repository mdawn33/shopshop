using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Categories;

public static class GetById
{
    public sealed record SubCategoryItem(Guid Id, string Name);

    public sealed record Response(
        Guid Id,
        string Name,
        string? Description,
        Guid? ParentCategoryId,
        DateTime CreatedAt,
        DateTime UpdatedAt,
        IReadOnlyList<SubCategoryItem> SubCategories);

    public static async Task<Results<Ok<Response>, NotFound>> HandleAsync(
        Guid id,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var category = await db.Categories
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
            return TypedResults.NotFound();

        var response = new Response(
            category.Id,
            category.Name,
            category.Description,
            category.ParentCategoryId,
            category.CreatedAt,
            category.UpdatedAt,
            category.SubCategories
                .Select(sc => new SubCategoryItem(sc.Id, sc.Name))
                .ToList());

        return TypedResults.Ok(response);
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/categories/{id:guid}", HandleAsync)
            .WithName("GetCategoryById")
            .WithTags("Categories")
            .WithSummary("Get a category by ID including its direct subcategories")
            .Produces<Response>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }
}
