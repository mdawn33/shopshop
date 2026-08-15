namespace Shoppiness.ProductService.Features.Products.Search;

/// <summary>
/// The field a product search may be sorted by. A deliberately small, closed vocabulary —
/// popularity/best-seller sorting is out of scope (ProductService owns no sales data).
/// </summary>
public enum ProductSortBy
{
    Name,
    Price,
    Newest
}
