using ProductService.Domain.Enums;
using ProductService.Infrastructure.Persistence;
using Shoppiness.ProductService.Features.Products.Search;

namespace Shoppiness.ProductService.Features.Catalog.Sections.Resolvers;

/// <summary>
/// Resolves the <see cref="CatalogSectionType.New"/> section: active products ordered by
/// <c>Product.CreatedAt</c> descending (design D2).
/// </summary>
public sealed class NewSectionResolver : ICatalogSectionResolver
{
    public CatalogSectionType SectionType => CatalogSectionType.New;

    public IQueryable<EffectivePriceExpressions.ProductWithEffectivePrice> ResolveProducts(
        ProductDbContext context, DateTime now) =>
        context.Products
            .Where(p => p.IsActive)
            .SelectWithEffectivePrice(now)
            .OrderByDescending(x => x.Product.CreatedAt);
}
