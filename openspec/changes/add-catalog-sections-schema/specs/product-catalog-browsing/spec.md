## ADDED Requirements

### Requirement: Search results and product details include catalog attributes
`GET /products` search results and the `GET /products/{id}` detail response SHALL each include
the product's `brand`, `sku`, and `variant` fields (each nullable), in addition to the fields
already returned by `add-product-catalog-search` (name, description, resolved price, category,
etc.).

#### Scenario: Search result includes catalog attributes
- **WHEN** a GET request is sent to `/products` and a matching active product has non-null
  `Brand`, `Sku`, and `Variant` values
- **THEN** the system SHALL include `brand`, `sku`, and `variant` in that product's entry in the
  `items` array, matching the stored values

#### Scenario: Product detail includes catalog attributes
- **WHEN** a GET request is sent to `/products/{id}` for an active product with non-null `Brand`,
  `Sku`, and `Variant` values
- **THEN** the system SHALL include `brand`, `sku`, and `variant` in the response, matching the
  stored values

#### Scenario: Product missing some catalog attributes
- **WHEN** a product has a null `Brand`, `Sku`, or `Variant` (e.g. a book with no retail "brand",
  or a grocery item with no meaningful "variant")
- **THEN** the system SHALL return the corresponding field(s) as `null` rather than omitting the
  product or substituting a placeholder value, in both the search results and the detail response

#### Scenario: Existing search, sort, filter, and pagination behavior is unaffected
- **WHEN** a GET request is sent to `/products` with any combination of `categoryId`, `minPrice`,
  `maxPrice`, `q`, `sortBy`, `sortDirection`, `page`, `pageSize`
- **THEN** the system SHALL apply filtering, sorting, and pagination exactly as specified by
  `add-product-catalog-search`'s existing requirements — the addition of `brand`/`sku`/`variant` to
  each result item does not change which products match, how they are ordered, or how they are
  paged
