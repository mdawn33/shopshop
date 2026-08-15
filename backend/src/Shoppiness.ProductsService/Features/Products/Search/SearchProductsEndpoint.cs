using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Shoppiness.ProductService.Features.Products.Search;

public static class SearchProductsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder routes)
    {
        // Individual scalar/array parameters (rather than a single [AsParameters] request type)
        // so categoryId's repeated-key array binding and sortBy/sortDirection's text-to-enum
        // mapping (see ParseSortBy/ParseSortDirection below) stay under our control — see
        // design.md D2 and D4.
        routes.MapGet("/products", HandleAsync)
            .WithName("SearchProducts")
            .WithTags("Products")
            .WithSummary(
                "Search active products by category, price range, and keyword, with sorting and pagination")
            .Produces<PagedResult<SearchProductsHandler.ProductSearchItem>>(StatusCodes.Status200OK)
            .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest)
            .RequireAuthorization();
    }

    private static async Task<IResult> HandleAsync(
        Guid[]? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? q,
        string? sortBy,
        string? sortDirection,
        IValidator<SearchProductsRequest> validator,
        SearchProductsHandler handler,
        CancellationToken cancellationToken,
        // DI-resolved services and CancellationToken precede these so the two page/pageSize
        // C# default values don't force validator/handler to become nullable — C# requires
        // optional parameters to be trailing.
        int page = SearchProductsRequest.DefaultPage,
        int pageSize = SearchProductsRequest.DefaultPageSize)
    {
        var request = new SearchProductsRequest(
            CategoryId: categoryId ?? [],
            MinPrice: minPrice,
            MaxPrice: maxPrice,
            Q: q,
            SortBy: ParseSortBy(sortBy),
            SortDirection: ParseSortDirection(sortDirection),
            Page: page,
            PageSize: pageSize);

        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            return TypedResults.BadRequest(new ValidationProblemDetails(errors));
        }

        var result = await handler.HandleAsync(request, cancellationToken);
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Resolves the raw <c>sortBy</c> query text to a <see cref="ProductSortBy"/>. Values match
    /// the enum member names case-insensitively (<c>price</c>, <c>name</c>, <c>newest</c>).
    /// An unrecognized, non-blank value maps to an out-of-range sentinel so
    /// <see cref="SearchProductsValidator"/>'s <c>IsInEnum()</c> rule reports it as a structured
    /// 400 rather than the framework short-circuiting model binding.
    /// </summary>
    private static ProductSortBy ParseSortBy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SearchProductsRequest.DefaultSortBy;

        return Enum.TryParse<ProductSortBy>(value, ignoreCase: true, out var result) && Enum.IsDefined(result)
            ? result
            : (ProductSortBy)(-1);
    }

    /// <summary>
    /// Resolves the raw <c>sortDirection</c> query text to a <see cref="SortDirection"/>. Values
    /// match the enum member names case-insensitively (<c>asc</c>, <c>desc</c>, per design D4).
    /// An unrecognized, non-blank value maps to an out-of-range sentinel so
    /// <see cref="SearchProductsValidator"/>'s <c>IsInEnum()</c> rule reports it as a structured
    /// 400 rather than the framework short-circuiting model binding.
    /// </summary>
    private static SortDirection ParseSortDirection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return SearchProductsRequest.DefaultSortDirection;

        return Enum.TryParse<SortDirection>(value, ignoreCase: true, out var result) && Enum.IsDefined(result)
            ? result
            : (SortDirection)(-1);
    }
}
