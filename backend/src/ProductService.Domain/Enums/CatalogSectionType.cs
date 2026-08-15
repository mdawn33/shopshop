namespace ProductService.Domain.Enums;

/// <summary>
/// Discriminates how a <see cref="Entities.CatalogSection"/> resolves its list of products.
/// Closed set today; adding a future rule (e.g. a sales-derived "BestSellers") is an additive
/// enum value plus a new resolver branch, not a schema change (design D2).
/// </summary>
public enum CatalogSectionType
{
    New = 0,
    Offers = 1,

    // BestSellers = 2  -- reserved for a future change; NOT added now (no SalesCount data source)
}
