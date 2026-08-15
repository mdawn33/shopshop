using ProductService.Domain.Enums;
using ProductService.Infrastructure.Persistence;
using Shoppiness.ProductService.Features.Products.Search;

namespace Shoppiness.ProductService.Features.Catalog.Sections.Resolvers;

/// <summary>
/// Resolves the ordered candidate-product query for one <see cref="CatalogSectionType"/>
/// (design D2). Each concrete resolver owns exactly one section type's filter + effective-price
/// projection + ordering rule — this is the Single Responsibility piece. Adding a future section
/// type (e.g. <c>BestSellers</c>) means adding a new <see cref="ICatalogSectionResolver"/>
/// implementation and registering it in DI; no existing resolver and no consumer of this interface
/// (<see cref="GetCatalogSectionsHandler"/>) needs to change — this is the Open/Closed piece.
/// </summary>
public interface ICatalogSectionResolver
{
    /// <summary>
    /// The <see cref="CatalogSectionType"/> this resolver handles.
    /// <see cref="GetCatalogSectionsHandler"/> looks up the resolver whose
    /// <see cref="SectionType"/> matches a given
    /// <see cref="ProductService.Domain.Entities.CatalogSection.SectionType"/>.
    /// </summary>
    CatalogSectionType SectionType { get; }

    /// <summary>
    /// Builds the still-<see cref="IQueryable{T}"/>, already-ordered (most relevant first) query of
    /// active products paired with their effective price as of <paramref name="now"/>. Ordering is
    /// section-type-specific business logic (e.g. <c>New</c> sorts by <c>Product.CreatedAt</c>,
    /// <c>Offers</c> sorts by the matching promotional price's <c>StartDate</c>) and therefore lives
    /// here, inside the resolver, rather than in the caller. Capping (<c>Take(section.MaxItems)</c>)
    /// is identical across every type and stays in <see cref="GetCatalogSectionsHandler"/>.
    /// </summary>
    IQueryable<EffectivePriceExpressions.ProductWithEffectivePrice> ResolveProducts(
        ProductDbContext context, DateTime now);
}
