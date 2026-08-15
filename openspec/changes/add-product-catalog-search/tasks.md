## 1. Shared Search/Pagination Support

- [x] 1.1 Create `Features/Products/Search/PagedResult.cs` — generic `PagedResult<T>` record with
  `Items`, `TotalCount`, `Page`, `PageSize`, `TotalPages`
- [x] 1.2 Create `Features/Products/Search/ProductSortBy.cs` and `SortDirection.cs` enums
  (`Price`, `Name`, `Newest`; `Asc`, `Desc`)

## 2. Search Products Feature (replaces `Features/Products/List.cs`)

- [x] 2.1 Create the `Features/Products/Search/` folder; delete the existing
  `Features/Products/List.cs`
- [x] 2.2 Create `SearchProductsRequest.cs` — query-bound request with `Guid[] CategoryId`,
  `decimal? MinPrice`, `decimal? MaxPrice`, `string? Q`, `ProductSortBy SortBy`,
  `SortDirection SortDirection`, `int Page`, `int PageSize` (defaults: `SortBy=Name`,
  `SortDirection=Ascending`, `Page=1`, `PageSize=20`)
- [x] 2.3 Create `SearchProductsValidator.cs` (FluentValidation) — `MinPrice`/`MaxPrice`
  non-negative when supplied, `MinPrice <= MaxPrice` when both supplied, `Page >= 1`,
  `1 <= PageSize <= 100`, `SortBy`/`SortDirection` must be defined enum values
- [x] 2.4 Create `SearchProductsHandler.cs` — build the effective-price correlated subquery
  (design D1: active `ProductPrice` where `StartDate <= now` and `EndDate` is null or `>= now`,
  most recent `StartDate` wins, else `Product.BasePrice`); apply `IsActive`, category (`Contains`
  over the `categoryId` array, empty array = no filter), price-range, and case-insensitive
  keyword (`EF.Functions.ILike` over `Name` or `Description`) filters; apply the requested sort;
  run a `CountAsync` for `TotalCount` before paging; apply `Skip`/`Take`; project to a
  `ProductSearchItem` DTO that includes the resolved effective price
- [x] 2.5 Create `SearchProductsEndpoint.cs` — map `GET /products` binding repeated `categoryId`
  query values to `Guid[]`, wire the validator (400 on failure) and handler, return
  `PagedResult<ProductSearchItem>`

## 3. Product Details Enhancement

- [x] 3.1 Extend `Features/Products/GetById.cs`'s handler to resolve the product's effective
  price using the same active-`ProductPrice`-else-`BasePrice` rule as the search handler (design
  D1) — either via `.Include(p => p.Prices)` + in-memory resolution for the single loaded
  product, or the same correlated-subquery pattern for consistency
- [x] 3.2 Add a `Price` field to `GetById.Response` carrying the resolved effective price
- [x] 3.3 Confirm existing 404 behavior for a missing or inactive product is unchanged

## 4. Endpoint Registration

- [x] 4.1 Update the endpoint registration call site (`Extensions/ApiServiceExtensions.cs` or
  `Program.cs`) to map the new `Search` feature instead of the removed `List` feature
- [x] 4.2 Confirm `GetById`'s endpoint registration is unaffected by the response change

## 5. Verification

- [ ] 5.1 Smoke-test category filtering via Scalar/OpenAPI: single category, multiple categories,
  no category, a category with no matches
- [ ] 5.2 Smoke-test price-range filtering: `minPrice` only, `maxPrice` only, both, invalid range
  (`min > max`), and a product whose active `ProductPrice` (not `BasePrice`) determines the match
- [ ] 5.3 Smoke-test keyword search: match on `Name`, match on `Description`, no match, blank `q`
- [ ] 5.4 Smoke-test each `sortBy`/`sortDirection` combination and the default sort
- [ ] 5.5 Smoke-test pagination: default page/size, a specific page, a page beyond the last page,
  and invalid `page`/`pageSize` values (expect `400`)
- [ ] 5.6 Smoke-test a combined request (category + price range + keyword + sort + pagination
  together) against `openspec/changes/add-product-catalog-search/specs/product-catalog-browsing/spec.md`'s
  "Combine multiple search criteria" scenario
- [ ] 5.7 Smoke-test `GET /products/{id}` for a product with an active `ProductPrice`, a product
  with none (falls back to `BasePrice`), and a missing/inactive product (`404`)
- [ ] 5.8 Confirm soft-deleted (`IsActive = false`) products are excluded from both search results
  and `GET /products/{id}`
