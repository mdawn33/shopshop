## Why

`product-service-foundation` (complete, 30/30 tasks, not yet archived) shipped a minimal
`GET /products` (single `categoryId` filter, no pagination) and `GET /products/{id}` for
`Shoppiness.ProductsService`. That is enough for admin CRUD but not enough for a B2C storefront:
shoppers need to filter by multiple categories, filter by price range, search by keyword, sort
results, and page through large catalogs. Without these, the catalog UI cannot be built. This
change adds the read/query endpoints a B2C shopper needs to find and browse products, without
touching product/category/stock creation or management, which stay out of scope for a later phase.

## What Changes

- Expand `GET /products` into a multi-criteria catalog search: filter by one or more
  `categoryId` values, filter by price range (`minPrice`/`maxPrice`, evaluated against each
  product's resolved effective price — active `ProductPrice` per D4, falling back to
  `Product.BasePrice`), keyword search against product `Name`/`Description`, sort by price, name,
  or newest (ascending/descending), and offset-based pagination (`page`/`pageSize`).
- Expand `GET /products/{id}` ("product details") to also return the product's resolved
  effective price (active `ProductPrice` if one applies "now", else `BasePrice`), so a shopper
  lands on a detail page showing the same price they saw in search results.
- No new entities. Reuses `Product`, `Category`, `ProductPrice` from `product-service-foundation`.
- **BREAKING** (pre-release, not yet consumed by a client): `GET /products` response shape
  changes from a bare array (`IReadOnlyList<ProductItem>`) to a paginated envelope
  (`items` + `totalCount` + `page` + `pageSize` + `totalPages`).

Explicitly out of scope for this change:
- Product, Category, ProductPrice, and Stock creation/management (CRUD) — later phase.
- Recommendations (e.g. "customers also bought") — deferred, no ranking/personalization here.
- Best-sellers / popularity sorting — ProductService does not own sales/order data (that lives in
  SalesOrder/PaymentService); no `SalesCount` or similar field is added to `Product`.
- Cursor-based pagination — offset-based only; see `design.md` for a forward-looking note.
- SQL sample/seed data generation — handled separately, outside OpenSpec.

## Capabilities

### New Capabilities
- `product-catalog-browsing`: Multi-criteria product search (category, price range, keyword,
  sort, offset pagination) and product-details lookup with resolved effective price, for B2C
  shoppers finding and browsing the catalog.

### Modified Capabilities
_None._ No capability has been archived to `openspec/specs/` yet (`product-service-foundation`
is complete but unarchived), so there is no baseline spec to delta against. The requirements
below supersede the "List products" and "Get a product by ID" requirements drafted in
`openspec/changes/product-service-foundation/specs/product-management/spec.md` for the same
`GET /products` and `GET /products/{id}` routes; reconciliation happens when both changes are
archived.

## Impact

- **Modified code:** `Shoppiness.ProductsService/Features/Products/List.cs` (rewritten as a
  folder-per-feature slice under `Features/Products/Search/`), `Features/Products/GetById.cs`
  (extended, stays single-file).
- **No domain/infrastructure schema changes** — reuses existing `Product`, `Category`,
  `ProductPrice` entities and `ProductDbContext` from `product-service-foundation`. May add a
  database index to support keyword search and price-range filtering performance (see
  `design.md`).
- **No new NuGet dependencies** expected; reuses `FluentValidation`, EF Core, Npgsql already in
  place.
- **Cross-service impact:** none — this stays within `Shoppiness.ProductsService`.
