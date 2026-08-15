## Context

`Shoppiness.ProductsService` currently models `Product`, `Category`, `ProductPrice`
(`ProductService.Domain.Entities`), all mapped by `ProductDbContext` via
`IEntityTypeConfiguration<T>` classes (per D8). `add-product-catalog-search` (in-flight,
unarchived) added `GET /products` (multi-criteria search) and enriched `GET /products/{id}` with a
resolved effective price, computed as a correlated subquery over `ProductPrice`
(`SearchProductsHandler.cs`, design D1 of that change): the active `ProductPrice` — `StartDate <=
now` and (`EndDate` is null or `EndDate >= now`), most recent `StartDate` wins — else
`Product.BasePrice`.

The frontend's catalog homepage (`sample-data.ts`) hardcodes three named sections, each a fixed
product list. There is no backend concept of a "section" today. This design introduces
`CatalogSection` as configured data — an ordered, named container whose product list is *resolved*
at request time by a small, closed set of rules (`SectionType`), not stored as an explicit
product-to-section mapping. That keeps sections cheap to add/reorder (an `INSERT`/`UPDATE`, no
per-product wiring) and keeps them automatically fresh (a "New" section always reflects the
actual newest active products, not a stale snapshot).

## Goals / Non-Goals

**Goals:**
- Let a `CatalogSection` be added, reordered, or deactivated via data changes alone (seed/manual
  SQL for now; admin CRUD is future work), with no code deploy.
- Support exactly two section-resolution rules end-to-end: `New` (newest active products by
  `Product.CreatedAt` descending) and `Offers` (active products carrying a currently-active
  `ProductPrice` of `PriceType.Sale` or `PriceType.Clearance`).
- Shape the `SectionType` discriminator so a future rule (e.g. `BestSellers`, a category-scoped
  spotlight) is an additive enum value plus a new resolver branch — not a schema migration — while
  keeping today's schema minimal (no field either supported type doesn't need).
- Serve one endpoint, `GET /catalog/sections`, returning ordered active sections with each
  section's resolved products, each product carrying the same resolved effective price as
  `GET /products` / `GET /products/{id}`.
- Keep the vertical-slice convention: endpoint, DTOs, handler using `ProductDbContext` directly —
  no repository layer, no Application layer.

**Non-Goals:**
- `BestSellers` (or any sales/popularity-derived) section type — no `SalesCount` or similar field
  on `Product`; ProductService still doesn't own order data. The design leaves room for this later
  (see D2) but does not build it.
- `Rating`, `ReviewCount`, `FastShipping`, `Stock` on `Product` — explicitly out of scope per
  `proposal.md`; not touched by this design.
- Admin CRUD for `CatalogSection` (create/update/reorder/delete via API) — this change ships the
  schema, the read endpoint, and a seed-script task; authoring happens outside the API for now.
- Per-shopper personalization/ranking within a section.
- Pagination within a section — a section returns a bounded, curated list (see D4's `MaxItems`),
  not a paged browse surface; `GET /products` already covers full catalog browsing.
- Frontend consumption of the new endpoint — backend-only change (see `proposal.md`).

## Decisions

### D1: `CatalogSection` schema — minimal fields for the two supported types, no `CategoryId`

```csharp
public class CatalogSection
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public CatalogSectionType SectionType { get; set; }
    public int MaxItems { get; set; } = 12;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

- `Title`: display label (e.g. "Nuevos ingresos", "Ofertas del día"). `Link`/routing is a frontend
  concern (already modeled in the frontend's `CatalogSection.link`) — not stored here.
- `DisplayOrder`: `int`, ascending sort key for section order on the homepage. Not enforced unique
  at the DB level — two sections can share a value; ties are broken by `CreatedAt` ascending in the
  query (stable, deterministic, no extra column needed). Reordering the homepage is an `UPDATE`
  of `DisplayOrder` values.
- `SectionType`: the resolution-rule discriminator (D2).
- `MaxItems`: caps how many products a section resolves to (default `12`). Added because every
  section-resolution rule needs *some* bound — without it, "New" would return the entire active
  catalog ordered by date, which isn't a homepage section, it's `GET /products?sortBy=newest`.
  Both supported types need this field, so it's on the base entity, not type-specific.
- No `CategoryId` FK. **Considered and rejected for this change**: a nullable `CategoryId` would
  let a section scope itself to a category (useful for a future "Category spotlight" type), but
  neither `New` nor `Offers` as specified uses it — `New` and `Offers` are catalog-wide by design
  in this change. Adding an unused nullable FK now violates the "only add fields the two supported
  section types actually need" instruction and adds a column every future migration would need to
  reconsider (e.g. does `CategoryId` scope `New` too, or only a future spotlight type?) without a
  concrete requirement driving the answer today. If a category-scoped section type is added later,
  `CategoryId` (nullable, `OnDelete(DeleteBehavior.Restrict)` matching `Product.CategoryId`'s
  existing pattern) is a straightforward additive migration at that time — no rework of the fields
  above.
- No `Link`/URL field — the frontend already derives section anchors/links from its own routing
  (`NAV_LINKS` in `sample-data.ts` uses hash anchors like `#nuevos`); the backend doesn't need to
  own a frontend routing concern.
- `IsActive`: soft-delete flag, consistent with `Product`/`Category` (mirrors the project's global
  `IsActive` convention); a global EF query filter excludes inactive sections by default, same
  pattern as `ProductConfiguration`/`CategoryConfiguration`.

**Alternative considered:** An explicit `CatalogSectionProduct` join table (curator manually picks
which products belong to which section). Rejected — that's a fundamentally different model
(editorial curation vs. rule-based resolution) that the user's decision already ruled out by
specifying `New`/`Offers` as *rules* over existing `Product`/`ProductPrice` data, not a curated
list. A join table also reintroduces the staleness problem sections are meant to avoid (a
"New" section frozen at insert time, not living).

### D2: `SectionType` as a closed C# enum, resolved via an `ICatalogSectionResolver` strategy per type

```csharp
public enum CatalogSectionType
{
    New = 0,
    Offers = 1,
    // BestSellers = 2  -- reserved for a future change; NOT added now (no SalesCount data source)
}
```

The enum-to-implementation mapping is a one-interface-per-behavior, one-class-per-type strategy
pattern rather than an inline `switch` in the handler (revised from this design's original draft —
see below):

```csharp
public interface ICatalogSectionResolver
{
    CatalogSectionType SectionType { get; }

    IQueryable<EffectivePriceExpressions.ProductWithEffectivePrice> ResolveProducts(
        ProductDbContext context, DateTime now);
}
```

- `NewSectionResolver` (`Features/Catalog/Sections/Resolvers/NewSectionResolver.cs`) —
  `SectionType => CatalogSectionType.New`; resolves active products ordered by
  `Product.CreatedAt` descending.
- `OffersSectionResolver` (`Features/Catalog/Sections/Resolvers/OffersSectionResolver.cs`) —
  `SectionType => CatalogSectionType.Offers`; resolves active products carrying a currently-active
  Sale/Clearance `ProductPrice` (via D3's shared `HasActivePromotionalPrice` predicate), ordered by
  that price's `StartDate` descending (most recently discounted first).

Each resolver is registered in DI as `services.AddScoped<ICatalogSectionResolver,
NewSectionResolver>()` / `...OffersSectionResolver>()` (`ApiServiceExtensions.cs`).
`GetCatalogSectionsHandler` takes `IEnumerable<ICatalogSectionResolver>` via constructor injection,
indexes it once into a `Dictionary<CatalogSectionType, ICatalogSectionResolver>`, and for each
`CatalogSection` looks up the resolver matching `section.SectionType`, calls `ResolveProducts`, then
applies `Take(section.MaxItems)` — capping is identical across every type, so it stays common in the
handler rather than duplicated per resolver. A section whose `SectionType` has no registered
resolver (a data/deployment mismatch — `CatalogSection` rows are data, not compile-time-checked)
resolves to an empty product list rather than throwing, consistent with the "zero-match section
still returns `products: []`" rule (D5).

**Why this shape (SRP/OCP):**
- **Single Responsibility** — each resolver class owns exactly one section type's filter +
  effective-price projection + ordering rule. `GetCatalogSectionsHandler`'s only responsibilities
  are loading/ordering `CatalogSection` rows, dispatching to the right resolver, capping, and
  shaping the response DTO — it no longer also contains every type's query logic inline.
- **Open/Closed** — adding a third type (e.g. `BestSellers`) is: add one enum value, add one new
  `ICatalogSectionResolver` implementation file, add one `AddScoped` DI line. No existing resolver
  file and no line of `GetCatalogSectionsHandler` needs to change — the handler is closed for
  modification but open for extension via the resolver set it's handed at construction time.

Ordering is section-type-specific business logic (two genuinely different sort keys — `New` sorts
by `Product.CreatedAt`, `Offers` sorts by a correlated `ProductPrice.StartDate`) and therefore lives
inside each resolver's `ResolveProducts`, not factored into shared handler code; `Take(MaxItems)`
is identical for every type and is the one piece of behavior the handler still owns directly.

**This design originally specified** (and an earlier implementation session built) "a closed enum,
resolved by a per-type `switch`/`case` branch inline in the handler." That version worked
correctly but put every section type's query logic in one method, violating SRP (the handler had a
reason to change for every section type) and OCP (adding `BestSellers` would mean editing that
`switch`, not just adding new code). This revision keeps the closed enum (still correct — see
alternative below) but replaces the inline switch with the interface/strategy approach above,
per explicit direction to make the mapping SOLID-compliant. Response shape, ordering, and filtering
semantics are unchanged — this is a structural refactor, not a behavior change.

**Alternative considered:** Store the resolution rule as data (e.g. a JSON filter/sort spec column
on `CatalogSection`) instead of a closed enum, for a more "generic rule engine" feel. Rejected as
premature — two hardcoded rules don't justify a rule-interpretation layer, and a closed enum is
far easier to validate, test, and reason about than a JSON DSL; the enum-plus-resolver-strategy
approach already satisfies "additive, not a redesign" for the next type without needing a rule DSL.

### D3: Reuse `add-product-catalog-search`'s effective-price expression via a shared helper

The effective-price correlated subquery (active `ProductPrice` else `BasePrice`) already exists
twice — `SearchProductsHandler.cs` and `GetById.cs` (each computing it independently per that
change's design). This change adds a third consumer (`CatalogSectionsHandler`), and a fourth
consumer for `Offers` resolution's own "does this product have an active Sale/Clearance price"
predicate, which is a close cousin of the same expression. Three-plus independent copies of the
same business rule (D4 of the project's overall architecture: active `ProductPrice` wins over
`BasePrice`) cross the threshold where duplication risks the rule drifting out of sync (e.g. one
copy using `>=` and another `>` on `EndDate`).

This change extracts the effective-price expression into a single shared, reusable
`Expression<Func<Product, decimal>>` (or equivalent static helper returning the LINQ subquery
shape), placed alongside the existing `Features/Products/Search/` code (e.g.
`Features/Products/Search/EffectivePriceExpressions.cs`) since that's where the pattern was
established, and referenced by both `GetById.cs` and the new `Features/Catalog/Sections/` handler.
The `Offers` predicate (`Prices.Any(...)` on `PriceType`/`StartDate`/`EndDate`) is a related but
distinct expression (a boolean "has an active promo" check, not a resolved price), given its own
small shared helper next to the price one.

**Alternative considered:** Leave the duplication as-is (each handler writes its own subquery).
Rejected — acceptable at two copies (as `add-product-catalog-search` shipped), no longer acceptable
at three-plus; extracting now is a small, low-risk refactor that also gives `add-product-catalog-search`'s
existing two call sites a shared source of truth as a side benefit.

### D4: `Product.Brand`, `Sku`, `Variant` — all nullable `string`, no uniqueness constraint

```csharp
public string? Brand { get; set; }
public string? Sku { get; set; }
public string? Variant { get; set; }
```

All three nullable — not every product realistically has all three (books have an author, not a
"brand" in the retail sense, though the seed script may map author-as-brand; groceries/books often
have no meaningful "variant"). Making them required would force fabricated values into categories
where they don't fit, which is worse than a nullable gap. `Sku`, despite being an industry-standard
"unique per catalog" identifier, gets no DB-level uniqueness constraint in this change — enforcing
it now would require guaranteeing all 100 existing seeded products get a globally-unique `Sku` as
part of this change's seed-script task (task 6), which is achievable but not something this design
mandates; flagged as an open question below rather than decided unilaterally.

**Alternative considered:** Add a `ProductVariant` as a child entity (one `Product` → many
variants, each with its own `Sku`/price/stock) for a "real" variant model (e.g. a shirt in 3
colors x 4 sizes = 12 orderable variants). Rejected for this change — the user's decision scopes
`Variant` as a single descriptive string on `Product` itself ("e.g. color, size, capacity" per the
existing frontend model), not a first-class variant/SKU-matrix system; that's a materially larger
change (affects Stock's 1:1 `ProductId` relationship, order line items, pricing) than "add three
catalog-attribute columns," and isn't what was asked for here.

### D5: One new endpoint, `GET /catalog/sections`, folder-per-feature under `Features/Catalog/Sections/`

Per the project's vertical-slice convention (folder-per-feature for complex features): the new
slice mirrors `Features/Products/Search/`'s shape —
`Features/Catalog/Sections/GetCatalogSectionsEndpoint.cs` (route + DI wiring),
`Features/Catalog/Sections/CatalogSectionResponse.cs` (response DTOs: a section wrapper +
a product-item DTO reusing the same shape as `ProductSearchItem` — `Id`, `Name`, `Brand`, `Sku`,
`Variant`, `BasePrice`, `Price` (resolved), `CategoryId` — so a section product and a search-result
product look the same to a frontend consumer), and
`Features/Catalog/Sections/GetCatalogSectionsHandler.cs` (loads active `CatalogSection`s ordered by
`DisplayOrder`/`CreatedAt`, resolves each section's products by dispatching to the
`ICatalogSectionResolver` registered for its `SectionType` per D2, projects through D3's shared
effective-price helper) plus `Features/Catalog/Sections/Resolvers/` (D2's per-type
`ICatalogSectionResolver` implementations). No request DTO/validation — the endpoint takes no
query parameters in this change (no filtering by section type, no pagination — see Non-Goals).

A section that resolves to zero products (e.g. an `Offers` section when nothing is currently on
sale) is still included in the response with an empty `products` array, rather than omitted —
consistent with the search endpoint's "empty `items`, still `200 OK`" pattern (D-search's spirit),
and predictable for a frontend that renders a fixed set of homepage section slots.

**Alternative considered:** Fold section resolution into the existing `/products` search endpoint
via a `sectionId` query parameter. Rejected — a section response is fundamentally
one-endpoint/many-sections/many-products-per-section (a nested shape), not a flat paginated product
list; overloading `/products` with that shape would fight `PagedResult<T>`'s existing (flat)
contract from `add-product-catalog-search`.

### D6: Two migrations, not one — `Product` columns and `CatalogSection` table are independent changes

- Migration 1: `AddProductBrandSkuVariant` — three nullable columns on the existing `Products`
  table.
- Migration 2: `AddCatalogSections` — the new `CatalogSections` table.

Kept separate because they're conceptually and operationally independent: one alters an existing
table's shape (low risk, purely additive nullable columns), the other creates a new table
(zero risk to existing data). Splitting them gives a cleaner migration history (each migration's
name says exactly one thing), makes it possible to apply/rollback either independently if a future
issue is isolated to one of them, and matches how EF Core migrations are conventionally scoped
(one logical schema change per migration) rather than batching unrelated changes to minimize
migration *count*.

**Alternative considered:** A single combined migration for both. Rejected — no technical reason
requires bundling them (they touch different tables with no FK relationship between the new
columns and the new table), and bundling only optimizes for fewer migration files, which isn't a
real constraint here.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| `MaxItems` default (`12`) is a guess at homepage section size, not derived from a design spec | Configurable per-section via the `CatalogSection` row itself (D1); adjust per section via data, no code change needed if `12` proves wrong |
| No DB-level uniqueness on `Sku` despite being conventionally unique | Deferred as an open question (below) rather than assumed; flag for the seed-script task and any future admin-CRUD change to revisit |
| `Offers` section resolution re-implements a "has an active promo" predicate that's structurally close to, but distinct from, the effective-price expression — risk of the two drifting apart if one is edited without the other | D3 gives both their own named, shared helper placed together, so a future edit to the active-price window rule (`StartDate`/`EndDate` semantics) is a one-place change reused by both |
| `GET /catalog/sections` has no way to request a single section, only "all active sections" | Acceptable for a homepage-load use case (one call renders the whole page); a `sectionId`/`type` filter can be added additively later if a client needs it |
| Two overlapping spec files (`product-catalog-browsing`'s existing requirements from `add-product-catalog-search` and this change's `Product` field additions) describe the same `Product` shape until `add-product-catalog-search` is archived | Documented in `proposal.md`; reconciled at archive time, consistent with how `add-product-catalog-search` itself handled the same situation against `product-service-foundation` |
| Nullable `Brand`/`Sku`/`Variant` means the frontend still can't assume every product has them, even after this change ships | Matches the "not every product realistically has all three" reality (D4); frontend already treats these as optional-ish display fields today, so this isn't a new constraint being introduced |

## Migration Plan

1. Add `Brand`, `Sku`, `Variant` to `Product.cs`; update `ProductConfiguration.cs` (max lengths:
   `Brand` 200, `Sku` 100, `Variant` 200 — matching the project's existing string-length
   conventions on `Name`/`Description`); generate migration `AddProductBrandSkuVariant`.
2. Add `CatalogSection.cs` and `CatalogSectionType.cs` to `ProductService.Domain`; add
   `CatalogSectionConfiguration.cs` to `ProductService.Infrastructure` (global `IsActive` query
   filter, matching `ProductConfiguration`/`CategoryConfiguration`); register `DbSet<CatalogSection>`
   on `ProductDbContext`; generate migration `AddCatalogSections`.
3. Extract the shared effective-price helper(s) (D3) out of `SearchProductsHandler.cs`/`GetById.cs`
   into `Features/Products/Search/EffectivePriceExpressions.cs`; update both existing call sites to
   use it (behavior-preserving refactor — same SQL shape, no response contract change).
4. Add `Features/Catalog/Sections/` (endpoint, response DTOs, handler) per D5, using the shared
   helper(s) from step 3 and the per-type resolution branches from D2.
5. Register the new endpoint in `Extensions/ApiServiceExtensions.cs`/`Program.cs`.
6. Update `sql-practice/seed-products.sql` per `tasks.md` task 6 (separate implementation step, not
   performed by this proposal) — populate `Brand`/`Sku`/`Variant` for the 100 seeded products and
   add `CatalogSections` seed rows.
7. Smoke-test via Scalar/OpenAPI: `GET /catalog/sections` returns both seeded sections in
   `DisplayOrder`, `New`'s products match `Product.CreatedAt` descending, `Offers`'s products all
   carry a currently-active `Sale`/`Clearance` `ProductPrice`, an inactive `CatalogSection` is
   excluded, a section with `MaxItems` smaller than its matching product count is capped correctly,
   and each returned product's `Price` matches what `GET /products/{id}` would return for that same
   product (D3 consistency).

No data migration is required beyond the two schema migrations above; the seed-script update (step
6) is dev-only convenience data, not a production data migration. Rollback is reverting the code
change plus the two EF Core migrations (`dotnet ef database update <previous-migration>`) — no
destructive data to recover, since both migrations are purely additive (new nullable columns, new
table).

## Open Questions

- Should `Sku` be enforced unique at the DB level (`HasIndex(p => p.Sku).IsUnique()`, nullable-safe
  via a filtered/partial index so multiple `NULL`s don't collide)? Proposed default: not in this
  change — revisit once the seed-script task (task 6) confirms all 100 products get sensible,
  actually-unique SKU values; adding the constraint after the fact is a small additive migration.
- Should `MaxItems` have an enforced upper bound (e.g. `<= 50`) to prevent an accidentally huge
  homepage section? Proposed default: no server-side cap in this change — `CatalogSection` rows are
  seed/manual-SQL authored for now (no untrusted API input), so this risk is deferred until admin
  CRUD (out of scope here) introduces an actual attack/misconfiguration surface worth validating
  against.
- Should `GET /catalog/sections` support an `If-None-Match`/short-lived cache header, given it's
  meant to back a homepage that's hit far more often than any single `GET /products` search? Proposed
  default: not in this change — no caching layer (Redis or HTTP) is introduced here; revisit once
  real homepage traffic patterns are known.
