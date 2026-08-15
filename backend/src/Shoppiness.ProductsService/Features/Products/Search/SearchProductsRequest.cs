namespace Shoppiness.ProductService.Features.Products.Search;

/// <summary>
/// Query-bound request for <c>GET /products</c> — multi-criteria catalog search.
/// Built by <see cref="SearchProductsEndpoint"/> from the raw query string (see that class for
/// how <see cref="SortBy"/>/<see cref="SortDirection"/> text values are resolved), then validated
/// by <see cref="SearchProductsValidator"/> before reaching <see cref="SearchProductsHandler"/>.
/// </summary>
public sealed record SearchProductsRequest(
    Guid[] CategoryId,
    decimal? MinPrice,
    decimal? MaxPrice,
    string? Q,
    ProductSortBy SortBy,
    SortDirection SortDirection,
    int Page,
    int PageSize)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public static readonly ProductSortBy DefaultSortBy = ProductSortBy.Name;
    public static readonly SortDirection DefaultSortDirection = SortDirection.Asc;
}
