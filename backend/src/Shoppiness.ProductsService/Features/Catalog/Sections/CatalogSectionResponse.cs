using ProductService.Domain.Enums;

namespace Shoppiness.ProductService.Features.Catalog.Sections;

/// <summary>
/// A single product entry within a resolved <see cref="CatalogSectionResponse"/>, shaped like
/// <see cref="Shoppiness.ProductService.Features.Products.Search.SearchProductsHandler.ProductSearchItem"/>
/// so a section product and a search-result product look the same to a frontend consumer
/// (design D5), plus <c>BasePrice</c> so the resolved-vs-base price relationship is visible.
/// </summary>
public sealed record CatalogSectionProductItem(
    Guid Id,
    string Name,
    string? Brand,
    string? Sku,
    string? Variant,
    decimal BasePrice,
    decimal Price,
    Guid CategoryId);

/// <summary>
/// An active <see cref="ProductService.Domain.Entities.CatalogSection"/> with its resolved list of
/// products. <see cref="Products"/> is an empty array (never omitted) when the section's
/// resolution rule currently matches nothing (design D5).
/// </summary>
public sealed record CatalogSectionResponse(
    Guid Id,
    string Title,
    CatalogSectionType SectionType,
    int DisplayOrder,
    IReadOnlyList<CatalogSectionProductItem> Products);
