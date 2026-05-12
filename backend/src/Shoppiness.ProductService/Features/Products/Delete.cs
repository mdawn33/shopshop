using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Products;

public static class Delete
{
    public static async Task<Results<NoContent, NotFound>> HandleAsync(
        Guid id,
        ProductDbContext db,
        CancellationToken cancellationToken)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return TypedResults.NotFound();

        product.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapDelete("/products/{id:guid}", HandleAsync)
            .WithName("DeleteProduct")
            .WithTags("Products")
            .WithSummary("Soft-delete a product by setting IsActive to false")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
}
