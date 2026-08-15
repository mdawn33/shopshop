using ProductService.Domain.Entities;
using ProductService.Domain.Enums;
using ProductService.Infrastructure.Persistence;
using Shoppiness.ProductService.Features.Products.Search;

namespace Shoppiness.ProductService.Features.Catalog.Sections.Resolvers;

/// <summary>
/// Resolves the <see cref="CatalogSectionType.Offers"/> section: active products carrying a
/// currently-active Sale/Clearance <see cref="ProductPrice"/>, ordered by that price's
/// <c>StartDate</c> descending — i.e. most recently discounted first (design D2).
/// </summary>
public sealed class OffersSectionResolver : ICatalogSectionResolver
{
    public CatalogSectionType SectionType => CatalogSectionType.Offers;

    public IQueryable<EffectivePriceExpressions.ProductWithEffectivePrice> ResolveProducts(
        ProductDbContext context, DateTime now) =>
        context.Products
            .Where(p => p.IsActive)
            .Where(EffectivePriceExpressions.HasActivePromotionalPrice(now))
            .SelectWithEffectivePrice(now)
            .OrderByDescending(x => x.Product.Prices
                .Where(pp =>
                    (pp.PriceType == PriceType.Sale || pp.PriceType == PriceType.Clearance) &&
                    pp.StartDate <= now && (pp.EndDate == null || pp.EndDate >= now))
                .OrderByDescending(pp => pp.StartDate)
                .Select(pp => pp.StartDate)
                .FirstOrDefault());
}
