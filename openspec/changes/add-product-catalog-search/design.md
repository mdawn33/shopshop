## Context

`Shoppiness.ProductsService` currently exposes `GET /products` (single optional `categoryId`
filter, no sort, no pagination, returns a bare array) and `GET /products/{id}` (returns the
product and its category name, no resolved price). Both are minimal, admin-facing reads shipped
as part of `product-service-foundation`. This design turns them into the B2C-facing "find a
product" surface: multi-value category filter, price-range filter, keyword search, sorting, and
offset pagination on the list; resolved effective price on both the list and the detail endpoint.

No new entities are introduced. `Product`, `Category`, and `ProductPrice` already exist
(`ProductService.Domain.Entities`), including `Product.GetActivePrice(DateTime at)`, a domain
method that resolves the active `ProductPrice` (or `null`) as of a given instant, per D4/D2 of
`product-service-foundation`'s design. That method operates on an in-memory `Prices` collection,
so it cannot run inside an EF Core LINQ query — the query-side equivalent has to be re-expressed
as a correlated subquery (Decision D1 below).

## Goals / Non-Goals

**Goals:**
- Let a B2C client filter the catalog by one or more categories, a price range, and a keyword,
  in any combination.
- Let a B2C client sort results by price, name, or newest, ascending or descending.
- Page results with `page`/`pageSize`, returning total count so a client can render page controls.
- Return each product's resolved effective price (active `ProductPrice`, else `BasePrice`) in
  both the list and the detail response, consistently computed the same way in both places.
- Keep the vertical-slice convention: endpoint, DTOs, FluentValidation, and handler using
  `ProductDbContext` directly — no repository layer, no Application layer.

**Non-Goals:**
- Recommendations, "customers also bought", personalization — deferred entirely.
- Best-sellers / popularity sorting or any `SalesCount`-like field — ProductService does not own
  order/sales data; that boundary stays intact.
- Cursor-based (keyset) pagination — noted below as a future improvement, not built now.
- Full-text search relevance ranking, typo tolerance, or a search index (Elasticsearch/OpenSearch,
  Postgres `tsvector`) — keyword search is a simple case-insensitive substring match this phase.
- Faceted search (counts per category/price bucket) — not requested, not built.
- Changes to Product/Category/ProductPrice creation, update, or deletion.

## Decisions

### D1: Effective price computed as a correlated subquery, not `GetActivePrice()`
`Product.GetActivePrice(DateTime at)` is a domain method over the loaded `Prices` collection —
useful once a single `Product` is materialized, but it can't be translated to SQL by EF Core, and
loading every product's full price history into memory just to filter/sort one search request
doesn't scale. Instead, the query layer expresses the same rule (active `ProductPrice` where
`StartDate <= now` and (`EndDate` is null or `EndDate >= now`), most recent `StartDate` wins,
else `BasePrice`) as a per-product correlated subquery inside the `Select`/`Where`/`OrderBy`:

```csharp
var effectivePrice = db.Products
    .Select(p => new
    {
        Product = p,
        EffectivePrice = p.Prices
            .Where(pp => pp.StartDate <= now && (pp.EndDate == null || pp.EndDate >= now))
            .OrderByDescending(pp => pp.StartDate)
            .Select(pp => (decimal?)pp.Price)
            .FirstOrDefault() ?? p.BasePrice
    });
```

This single expression is reused for: (a) the `minPrice`/`maxPrice` filter, (b) `sortBy=price`,
and (c) the `Price` field on both the search-result item and the product-details response — so
the list and the detail page always agree on what a product costs "right now". `now` (`DateTime`
or `TimeProvider.GetUtcNow()`) is captured once per request so a single request is internally
consistent even if it straddles a price-transition boundary.

**Alternative considered:** Keep `GetActivePrice()` and filter/sort in memory after loading all
products. Rejected — defeats the purpose of server-side pagination/filtering and won't scale past
a small catalog.

**Alternative considered:** Denormalize a `CurrentPrice` column on `Product`, updated by a
background job or trigger whenever a `ProductPrice` becomes active/inactive. Rejected as premature
for Phase 1 — adds a consistency-maintenance concern (job scheduling or DB triggers) to buy back
query simplicity that a subquery already provides at acceptable cost for current catalog sizes.

### D2: Multi-category filter via repeated `categoryId` query parameters
`GET /products?categoryId=<guid>&categoryId=<guid>` binds to `Guid[] categoryId` in the endpoint
delegate (ASP.NET Core minimal API supports repeated-key array binding for query strings
natively — no custom model binder needed). The handler applies `categoryId.Length == 0 ||
categoryId.Contains(p.CategoryId)` (empty/absent array means "no category filter"). This keeps the
same parameter name shoppers/clients already use for the single-category case in
`product-service-foundation`, just widened to accept multiple values.

**Alternative considered:** Comma-separated `?categoryIds=id1,id2`. Rejected — requires manual
parsing/splitting and a custom validator for malformed Guids, where repeated-key binding gets
`Guid[]` model-binding validation for free.

### D3: Keyword search is a case-insensitive substring match over Name + Description
`q` (optional query string) matches when `EF.Functions.ILike(p.Name, $"%{q}%")` OR
`EF.Functions.ILike(p.Description, $"%{q}%")` (Postgres `ILIKE`, case-insensitive, translated
server-side by Npgsql). No stemming, ranking, or relevance scoring — first match, ordered by
whatever `sortBy` the caller requested (default: name ascending).

**Alternative considered:** Postgres full-text search (`tsvector`/`tsquery` with a GIN index).
Rejected for this change as unnecessary complexity — catalog size in Phase 1 doesn't justify it,
and it can be layered in later behind the same `q` parameter without changing the API contract.

### D4: Sorting via a closed `SortBy` + `SortDirection` pair
Two query parameters: `sortBy` (`price` | `name` | `newest`, default `name`) and `sortDirection`
(`asc` | `desc`, default `asc`). Modeled as two string-backed enums validated by FluentValidation
against the allowed set (invalid value → `400 Bad Request`, not a silent fallback). `newest` sorts
by `Product.CreatedAt`. This is a deliberately small, closed vocabulary — no free-form
`sortBy=fieldName` — since "best sellers" and any popularity-based sort are explicitly deferred
(ProductService has no sales data to sort by) and this keeps the surface easy to validate and
document. `SortDirection`'s enum members are named `Asc`/`Desc` (matching the `asc`/`desc` wire
tokens exactly, rather than `Ascending`/`Descending`), so the endpoint's token-to-enum parse step
is a plain case-insensitive `Enum.TryParse`, with no custom token-mapping table needed.

**Alternative considered:** A single `sort=price_desc` combined token (à la many public APIs).
Rejected — two explicit parameters are simpler to validate independently with FluentValidation
and simpler for a client to construct from separate UI controls (a sort-field dropdown + a
direction toggle).

### D5: Offset pagination now; cursor pagination is a documented future improvement
`page` (1-based, default `1`, min `1`) and `pageSize` (default `20`, min `1`, max `100`) drive a
standard `Skip((page-1)*pageSize).Take(pageSize)`. The response is a `PagedResult<T>` envelope:
`items`, `totalCount`, `page`, `pageSize`, `totalPages`. `totalCount` requires a second query
(`CountAsync` against the filtered-but-unpaged query, before `Skip`/`Take`) — accepted as the
standard cost of offset pagination with a total-count UI (page numbers, "N results").

**Future improvement (explicitly not built in this change):** Cursor/keyset pagination (e.g. an
opaque cursor encoding the last-seen sort key + `Id` as a tiebreaker) avoids the `OFFSET` cost on
deep pages and stays stable when the underlying data changes between page fetches. Revisit this
once catalog size or traffic makes `OFFSET`-based paging a measured problem — it would replace
`page` with a `cursor` parameter and drop `totalCount` (or make it a separate, optional/cached
endpoint), which is a breaking change to this contract, so it's worth doing deliberately rather
than retrofitting silently.

**Alternative considered:** Build cursor pagination now. Rejected per explicit scope decision —
offset pagination is sufficient for Phase 1 catalog sizes and ships faster; documented above so
it isn't forgotten.

### D6: Feature file layout — folder-per-feature for Search, single-file for GetById
Per the project's vertical-slice convention (single file for simple CRUD, folder-per-feature for
complex features): the search endpoint becomes `Features/Products/Search/` with
`SearchProductsEndpoint.cs` (route + DI wiring), `SearchProductsRequest.cs` (query-bound request +
`SortBy`/`SortDirection` enums), `SearchProductsValidator.cs` (FluentValidation), `PagedResult.cs`
(shared envelope — reusable if another list endpoint needs pagination later), and
`SearchProductsHandler.cs` (query logic against `ProductDbContext`), replacing the existing
single-file `Features/Products/List.cs`. `Features/Products/GetById.cs` stays a single file —
adding one computed `Price` field to its existing response record is not enough complexity to
warrant a folder.

**Alternative considered:** Keep everything in one `List.cs` file. Rejected — five concerns
(routing, request binding, two enums, validation, and a multi-clause query) in one file starts to
fight the "simple CRUD only" threshold for single-file features.

### D7: No new database migration required
Filtering, sorting, and pagination are expressed entirely in the query layer against existing
columns (`Product.Name`, `Product.Description`, `Product.BasePrice`, `Product.CategoryId`,
`Product.CreatedAt`, `ProductPrice.StartDate`, `ProductPrice.EndDate`, `ProductPrice.Price`), all
already mapped by `product-service-foundation`'s `ProductConfiguration`/`ProductPriceConfiguration`.
No entity changes, so no EF Core migration is added by this change.

**Alternative considered:** Add a case-insensitive/trigram index (`pg_trgm` + GIN) to speed up the
`ILIKE` keyword search, and/or an index on `(CategoryId, BasePrice)` to help the price-range +
category-filter combination. Deferred — flagged as a performance follow-up under Risks below
rather than built speculatively before there's real query-plan evidence it's needed.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| `ILIKE '%term%'` keyword search can't use a standard B-tree index and will full-scan `Products` as the catalog grows | Acceptable for Phase 1 catalog size; revisit with a `pg_trgm` GIN index or Postgres full-text search if query plans show it's a bottleneck (see D3, D7) |
| The effective-price correlated subquery runs once per product per request (list) and could show up in query plans on large catalogs | Acceptable for Phase 1; if it becomes a bottleneck, denormalizing a maintained `CurrentPrice` column (rejected in D1 for now) is the fallback |
| `totalCount`'s second `COUNT(*)` query duplicates the filter predicate and adds a round trip | Standard, accepted cost of offset pagination with total-count UI; cursor pagination (D5) removes this if it's ever revisited |
| Offset pagination (`OFFSET`) degrades on very deep pages and can skip/duplicate rows if the result set changes between page fetches | Accepted trade-off for Phase 1; documented cursor-pagination path in D5 for when it matters |
| `GET /products` response shape changes from a bare array to a `PagedResult<T>` envelope | **BREAKING**, but no client depends on the old shape yet (product-service-foundation is unarchived/unreleased); safe to change now rather than version the endpoint |
| Two overlapping spec files (`product-management`'s "List products"/"Get a product by ID" and this change's `product-catalog-browsing`) describe the same routes until `product-service-foundation` is archived | Documented in `proposal.md`; reconciled at archive time, consistent with how `gateway-bff-auth-part-2` handled the same situation |

## Migration Plan

1. Replace `Features/Products/List.cs` with the `Features/Products/Search/` folder (endpoint,
   request/DTOs, validator, handler, shared `PagedResult<T>`).
2. Extend `Features/Products/GetById.cs`'s handler to compute and include the resolved effective
   price in its `Response`.
3. Re-register the endpoint mapping in `ApiServiceExtensions.cs`/`Program.cs` if the route
   registration call site changes (e.g. `Search.MapEndpoint` instead of `List.MapEndpoint`).
4. Smoke-test via Scalar/OpenAPI: category filter (single + multiple), price range, keyword,
   each `sortBy`/`sortDirection` combination, pagination boundaries (`page=1`, last page, `page`
   past the last page → empty `items` with correct `totalCount`), and the detail endpoint's
   resolved price against a product with and without an active `ProductPrice`.

No data migration; no schema change. Rollback is reverting the code change — no DB rollback
needed.

## Open Questions

- Should `pageSize`'s max (currently proposed as `100`) be configurable per environment, or is a
  hardcoded cap acceptable for Phase 1? Proposed default: hardcoded `100`, revisit if a client
  need emerges.
- Should the keyword search (`q`) also match `Category.Name` (e.g. searching "shoes" surfaces
  products in a "Shoes" category even if "shoes" isn't in the product name/description)? Proposed
  default: no, `q` matches `Product.Name`/`Description` only — keep the mental model simple;
  category-name matching can be added later without changing the parameter shape.
