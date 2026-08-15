## Why

The frontend's Amazon-style catalog homepage (`web-client/src/app/shared/data/sample-data.ts`)
hardcodes three `CATALOG_SECTIONS` — "Más vendidos" (best-sellers), "Ofertas del día" (today's
offers), "Nuevos ingresos" (new arrivals) — each a fixed, code-shipped array of `Product` objects.
None of it comes from `ProductService`. This blocks the storefront from ever showing real,
changing inventory on its homepage, and any section change today requires a frontend code deploy.
`ProductService` has no concept of a homepage "section" at all. This change gives ProductService
an ordered, configurable `CatalogSection` schema and a read endpoint that resolves each section's
products from real data, so the homepage can be wired to the backend instead of `sample-data.ts`.

## What Changes

- Add a new `CatalogSection` entity/table: `Title`, `DisplayOrder`, a `SectionType` discriminator,
  `IsActive`, plus whatever else the two supported section types need (see `design.md`). Sections
  are configured data (insertable/reorderable without a code deploy), not hardcoded endpoint logic.
- Support exactly two `SectionType` values end-to-end in this change: `New` (products ordered by
  `Product.CreatedAt` descending) and `Offers` (active products with a currently-active
  `ProductPrice` of `PriceType` `Sale` or `Clearance`, per D4's effective-price rule). The
  discriminator is designed so a future `BestSellers` (or category-spotlight) type is a
  straightforward enum/config addition later — not a schema rework — but no such type is built now.
- Add `Brand`, `Sku`, `Variant` (all `string`, nullable) to `Product`, with EF configuration and a
  migration, so a product card can render a brand, a SKU, and a variant (e.g. color/size) —
  attributes the frontend's `Product` model expects today but the backend has never stored.
- Add a new read endpoint, `GET /catalog/sections`, returning ordered active `CatalogSection`s,
  each with its resolved product list, reusing the effective-price resolution pattern
  (`add-product-catalog-search`'s correlated-subquery approach) so section products carry the same
  resolved price a shopper would see in search results or on the product-details page.
- Update `sql-practice/seed-products.sql` (task only, not implemented in this change): populate
  `Brand`/`Sku`/`Variant` for the existing 100 seeded products, and add seed rows for the new
  `CatalogSections` table (at least one "New" and one "Offers" section) using the script's
  existing fixed-literal-UUID convention.

Explicitly out of scope for this change:
- Best-sellers / popularity section type — ProductService does not own sales/order data (that
  lives in SalesOrder/PaymentService); no `SalesCount` or similar field is added to `Product`, and
  no `BestSellers` `SectionType` value is added yet. The `SectionType` discriminator is shaped so
  it can be added later without a schema rework (see `design.md`), but that is future work.
- `Rating`, `ReviewCount`, `FastShipping` on `Product` — these stay frontend-only
  placeholder/hardcoded values. They arguably belong to a future Reviews/Shipping concern, not
  Product, and are not added to the schema in this change.
- `Stock` on `Product` — Stock lives in `StockService`, 1:1 with `Product`, per decision D3. No
  stock field is added to `Product` and no cross-service call to `StockService` is made from the
  new endpoint.
- Section personalization/recommendations (e.g. "Recomendados para ti") — no ranking or
  per-shopper logic; sections return the same products for every caller.
- Admin CRUD for `CatalogSection` (create/update/reorder via API) — this change adds the schema,
  the read endpoint, and seed data; authoring sections happens via seed/manual SQL for now, the
  same way `Product`/`Category` creation is still out of scope per `add-product-catalog-search`.
- Rewiring the Angular catalog homepage to call the new endpoint instead of `sample-data.ts` — this
  change is backend-only (schema + endpoint + seed script); frontend consumption is a follow-up
  change.
- SQL edits to `sql-practice/seed-products.sql` itself — this change specifies what the seed
  update must contain (task 6 in `tasks.md`); executing that edit is implementation, not proposal.

## Capabilities

### New Capabilities
- `catalog-sections`: Configurable, ordered homepage catalog sections (`CatalogSection` entity)
  each resolving to a live list of products via a `SectionType` rule (`New`, `Offers`), exposed
  through `GET /catalog/sections` with resolved effective pricing per product.

### Modified Capabilities
- `product-catalog-browsing`: `Product` gains `Brand`, `Sku`, `Variant` fields. This capability
  (introduced by the in-flight, unarchived `add-product-catalog-search` change) does not change
  its search/filter/sort/pagination *requirements*, but its `Product`-shaped response fields grow
  by three optional attributes, so a delta spec documents the addition against the same
  `GET /products` / `GET /products/{id}` responses. Reconciled with `add-product-catalog-search`
  at archive time, the same way that change reconciled against `product-service-foundation`.

## Impact

- **New domain code:** `ProductService.Domain/Entities/CatalogSection.cs`,
  `ProductService.Domain/Enums/CatalogSectionType.cs`. `Product.cs` gains `Brand`, `Sku`, `Variant`
  properties.
- **New/modified infrastructure code:**
  `ProductService.Infrastructure/Persistence/Configurations/CatalogSectionConfiguration.cs` (new);
  `ProductConfiguration.cs` (modified, maps the three new columns);
  `ProductDbContext.cs` (adds `DbSet<CatalogSection>`); one or two new EF Core migrations under
  `ProductService.Infrastructure/Persistence/Migrations/` (see `design.md` for the
  one-vs-two-migrations decision).
- **New API code:** `Shoppiness.ProductsService/Features/Catalog/Sections/` (new folder-per-feature
  slice: endpoint, response DTOs, handler), reusing the effective-price correlated-subquery pattern
  from `Features/Products/Search/SearchProductsHandler.cs`. `Extensions/ApiServiceExtensions.cs`
  (or `Program.cs`) registers the new endpoint.
- **Database schema:** new `CatalogSections` table; `Products` table gains `Brand`, `Sku`,
  `Variant` nullable columns.
- **Dev tooling:** `sql-practice/seed-products.sql` needs an update (specified in `tasks.md`, not
  performed here) to keep seeding valid against the new schema.
- **No new NuGet dependencies** — reuses EF Core, Npgsql, FluentValidation already in place.
- **Cross-service impact:** none — stays within `Shoppiness.ProductsService` /
  `ProductService.Domain` / `ProductService.Infrastructure`. No call to `StockService` is added.
- **Frontend:** not modified by this change (see "Explicitly out of scope" above); the existing
  `CatalogSection`/`Product` frontend models and `sample-data.ts` are unaffected until a follow-up
  change wires the homepage to `GET /catalog/sections`.
