namespace Shoppiness.ProductService.Features.Products.Search;

/// <summary>
/// Generic offset-pagination envelope for list endpoints.
/// </summary>
/// <typeparam name="T">The item type returned in <see cref="Items"/>.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages)
{
    /// <summary>
    /// Builds a <see cref="PagedResult{T}"/>, computing <see cref="TotalPages"/> from
    /// <paramref name="totalCount"/> and <paramref name="pageSize"/>.
    /// </summary>
    public static PagedResult<T> Create(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
    {
        var totalPages = pageSize > 0 ? (int)Math.Ceiling(totalCount / (double)pageSize) : 0;
        return new PagedResult<T>(items, totalCount, page, pageSize, totalPages);
    }
}
