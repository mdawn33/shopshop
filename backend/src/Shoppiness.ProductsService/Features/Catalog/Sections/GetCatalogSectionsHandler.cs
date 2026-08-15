using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using ProductService.Domain.Enums;
using ProductService.Infrastructure.Persistence;
using Shoppiness.ProductService.Features.Catalog.Sections.Resolvers;
using Shoppiness.ProductService.Features.Products.Search;

namespace Shoppiness.ProductService.Features.Catalog.Sections;

/// <summary>
/// Handles <c>GET /catalog/sections</c>: loads active <see cref="CatalogSection"/> rows ordered by
/// <c>DisplayOrder</c>/<c>CreatedAt</c>, then resolves each section's products via the
/// <see cref="ICatalogSectionResolver"/> registered for its <see cref="CatalogSectionType"/>
/// (design D2), capped at <c>MaxItems</c>, using the shared effective-price helpers (design D3) so
/// a section product's price always agrees with <c>GET /products</c> / <c>GET /products/{id}</c>.
/// </summary>
public sealed class GetCatalogSectionsHandler
{
    private readonly ProductDbContext _db;
    private readonly IReadOnlyDictionary<CatalogSectionType, ICatalogSectionResolver> _resolversByType;

    /// <summary>
    /// Indexes the DI-registered <see cref="ICatalogSectionResolver"/>s by their
    /// <see cref="ICatalogSectionResolver.SectionType"/> once per request, so per-section
    /// resolution is a dictionary lookup rather than an inline switch (design D2 — SRP/OCP).
    /// </summary>
    public GetCatalogSectionsHandler(ProductDbContext db, IEnumerable<ICatalogSectionResolver> resolvers)
    {
        _db = db;
        _resolversByType = resolvers.ToDictionary(r => r.SectionType);
    }

    public async Task<IReadOnlyList<CatalogSectionResponse>> HandleAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var sections = await _db.CatalogSections
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.CreatedAt)
            .ToListAsync(cancellationToken);

        var responses = new List<CatalogSectionResponse>(sections.Count);

        foreach (var section in sections)
        {
            var products = await ResolveProductsAsync(section, now, cancellationToken);
            responses.Add(new CatalogSectionResponse(
                section.Id,
                section.Title,
                section.SectionType,
                section.DisplayOrder,
                products));
        }

        return responses;
    }

    /// <summary>
    /// Resolves a single section's products via the <see cref="ICatalogSectionResolver"/>
    /// registered for its <see cref="CatalogSectionType"/> (design D2). A section whose type has no
    /// registered resolver — a data/deployment mismatch that the compiler can't catch, since
    /// <see cref="CatalogSection"/> rows are data, not a compile-time-checked set — resolves to an
    /// empty product list rather than throwing. A section that matches zero products likewise still
    /// returns an empty list, never null or an error (design D5, spec requirement).
    /// </summary>
    /// <remarks>
    /// The DB-only filter/sort/cap operations run via EF Core (translated to SQL); the final
    /// shaping into <see cref="CatalogSectionProductItem"/> happens client-side, after
    /// materialization, since arbitrary DTO-construction helpers aren't SQL-translatable.
    /// </remarks>
    private async Task<IReadOnlyList<CatalogSectionProductItem>> ResolveProductsAsync(
        CatalogSection section, DateTime now, CancellationToken cancellationToken)
    {
        if (!_resolversByType.TryGetValue(section.SectionType, out var resolver))
        {
            return [];
        }

        var resolved = await resolver
            .ResolveProducts(_db, now)
            .Take(section.MaxItems)
            .ToListAsync(cancellationToken);

        return resolved
            .Select(x => ToItem(x.Product, x.EffectivePrice))
            .ToList();
    }

    private static CatalogSectionProductItem ToItem(Product product, decimal effectivePrice) => new(
        product.Id,
        product.Name,
        product.Brand,
        product.Sku,
        product.Variant,
        product.BasePrice,
        effectivePrice,
        product.CategoryId);
}
