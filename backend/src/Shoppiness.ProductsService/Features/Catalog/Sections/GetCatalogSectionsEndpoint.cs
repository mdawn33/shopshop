namespace Shoppiness.ProductService.Features.Catalog.Sections;

/// <summary>
/// Maps <c>GET /catalog/sections</c> — no request parameters, returns all active
/// <see cref="ProductService.Domain.Entities.CatalogSection"/>s in <c>DisplayOrder</c> order, each
/// with its resolved product list (design D5).
/// </summary>
public static class GetCatalogSectionsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/catalog/sections", HandleAsync)
            .WithName("GetCatalogSections")
            .WithTags("Catalog")
            .WithSummary("List active homepage catalog sections with their resolved products")
            .Produces<IReadOnlyList<CatalogSectionResponse>>(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleAsync(
        GetCatalogSectionsHandler handler,
        CancellationToken cancellationToken)
    {
        var sections = await handler.HandleAsync(cancellationToken);
        return TypedResults.Ok(sections);
    }
}
