using System.Linq.Expressions;
using ProductService.Domain.Entities;
using ProductService.Domain.Enums;

namespace Shoppiness.ProductService.Features.Products.Search;

/// <summary>
/// Shared, EF Core-translatable building blocks for resolving a product's effective price
/// (design D1 of <c>add-product-catalog-search</c>, extracted per design D3 of
/// <c>add-catalog-sections-schema</c>): the active <see cref="ProductPrice"/> as of a point in
/// time — <c>StartDate &lt;= at</c> and (<c>EndDate</c> is null or <c>&gt;= at</c>), most recent
/// <c>StartDate</c> wins — else <see cref="Product.BasePrice"/>.
///
/// Used by <see cref="SearchProductsHandler"/>, <c>Features/Products/GetById.cs</c>, and
/// <c>Features/Catalog/Sections/GetCatalogSectionsHandler.cs</c> so the three consumers can never
/// drift out of sync on this business rule.
/// </summary>
public static class EffectivePriceExpressions
{
    /// <summary>
    /// A product paired with its resolved effective price, as produced by
    /// <see cref="SelectWithEffectivePrice"/>.
    /// </summary>
    public sealed record ProductWithEffectivePrice(Product Product, decimal EffectivePrice);

    /// <summary>
    /// Projects each product in <paramref name="products"/> alongside its resolved effective
    /// price as of <paramref name="at"/>, computed as a correlated subquery so it can be further
    /// filtered/sorted/projected server-side by the caller.
    /// </summary>
    public static IQueryable<ProductWithEffectivePrice> SelectWithEffectivePrice(
        this IQueryable<Product> products, DateTime at) =>
        products.Select(p => new ProductWithEffectivePrice(
            p,
            p.Prices
                .Where(pp => pp.StartDate <= at && (pp.EndDate == null || pp.EndDate >= at))
                .OrderByDescending(pp => pp.StartDate)
                .Select(pp => (decimal?)pp.Price)
                .FirstOrDefault() ?? p.BasePrice));

    /// <summary>
    /// Predicate: true when a product currently carries an active (<c>StartDate &lt;= at</c>,
    /// <c>EndDate</c> null or <c>&gt;= at</c>) <see cref="ProductPrice"/> whose <c>PriceType</c>
    /// is <see cref="PriceType.Sale"/> or <see cref="PriceType.Clearance"/>. Used by the
    /// <c>Offers</c> catalog-section resolver to select promotionally-priced products.
    /// </summary>
    public static Expression<Func<Product, bool>> HasActivePromotionalPrice(DateTime at) =>
        p => p.Prices.Any(pp =>
            (pp.PriceType == PriceType.Sale || pp.PriceType == PriceType.Clearance) &&
            pp.StartDate <= at && (pp.EndDate == null || pp.EndDate >= at));
}
