using Microsoft.EntityFrameworkCore;
using ProductService.Infrastructure.Persistence;

namespace Shoppiness.ProductService.Features.Products.Search;

/// <summary>
/// Handles <c>GET /products</c> catalog search: category, price-range, and keyword filters,
/// sorting, and offset pagination. Resolves each product's effective price (active
/// <see cref="ProductService.Domain.Entities.ProductPrice"/>, else <c>BasePrice</c>) as a
/// correlated subquery so it can be filtered and sorted on server-side (design D1).
/// </summary>
public sealed class SearchProductsHandler(ProductDbContext db)
{
    /// <summary>
    /// A single search-result row, including the product's resolved effective price.
    /// </summary>
    public sealed record ProductSearchItem(
        Guid Id,
        string Name,
        string? Description,
        string? Brand,
        string? Sku,
        string? Variant,
        decimal Price,
        Guid CategoryId,
        string CategoryName,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    public async Task<PagedResult<ProductSearchItem>> HandleAsync(
        SearchProductsRequest request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var query = db.Products
            .Where(p => p.IsActive)
            .SelectWithEffectivePrice(now)
            .Select(x => new
            {
                x.Product.Id,
                x.Product.Name,
                x.Product.Description,
                x.Product.Brand,
                x.Product.Sku,
                x.Product.Variant,
                x.Product.CategoryId,
                CategoryName = x.Product.Category!.Name,
                x.Product.CreatedAt,
                x.Product.UpdatedAt,
                x.EffectivePrice
            });

        if (request.CategoryId.Length > 0)
            query = query.Where(p => request.CategoryId.Contains(p.CategoryId));

        if (request.MinPrice.HasValue)
            query = query.Where(p => p.EffectivePrice >= request.MinPrice.Value);

        if (request.MaxPrice.HasValue)
            query = query.Where(p => p.EffectivePrice <= request.MaxPrice.Value);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var term = request.Q.Trim();
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, $"%{term}%") ||
                (p.Description != null && EF.Functions.ILike(p.Description, $"%{term}%")));
        }

        query = (request.SortBy, request.SortDirection) switch
        {
            (ProductSortBy.Price, SortDirection.Desc) => query.OrderByDescending(p => p.EffectivePrice),
            (ProductSortBy.Price, _) => query.OrderBy(p => p.EffectivePrice),
            (ProductSortBy.Newest, SortDirection.Desc) => query.OrderByDescending(p => p.CreatedAt),
            (ProductSortBy.Newest, _) => query.OrderBy(p => p.CreatedAt),
            (ProductSortBy.Name, SortDirection.Desc) => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new ProductSearchItem(
                p.Id,
                p.Name,
                p.Description,
                p.Brand,
                p.Sku,
                p.Variant,
                p.EffectivePrice,
                p.CategoryId,
                p.CategoryName,
                p.CreatedAt,
                p.UpdatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<ProductSearchItem>.Create(items, totalCount, request.Page, request.PageSize);
    }
}
