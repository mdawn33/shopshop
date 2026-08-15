## ADDED Requirements

### Requirement: Search products by category
`GET /products` SHALL support filtering results to one or more categories via repeated
`categoryId` query parameters.

#### Scenario: Filter by a single category
- **WHEN** a GET request is sent to `/products?categoryId={id}` for a category with active products
- **THEN** the system SHALL return `200 OK` with only active products belonging to that category

#### Scenario: Filter by multiple categories
- **WHEN** a GET request is sent to `/products?categoryId={id1}&categoryId={id2}`
- **THEN** the system SHALL return `200 OK` with active products belonging to either category

#### Scenario: No category filter
- **WHEN** a GET request is sent to `/products` with no `categoryId` parameter
- **THEN** the system SHALL NOT filter by category (all active products are eligible, subject to
  any other filters supplied)

#### Scenario: Category with no matching products
- **WHEN** a GET request is sent to `/products?categoryId={id}` for a category that has no
  active products (or does not exist)
- **THEN** the system SHALL return `200 OK` with an empty `items` array and `totalCount` of `0`,
  not an error

### Requirement: Search products by price range
`GET /products` SHALL support filtering results by `minPrice` and/or `maxPrice`, evaluated
against each product's resolved effective price (the active `ProductPrice` as of now, per D4,
falling back to `Product.BasePrice` when none is active).

#### Scenario: Filter by minimum price only
- **WHEN** a GET request is sent to `/products?minPrice={value}`
- **THEN** the system SHALL return `200 OK` with only products whose resolved effective price is
  greater than or equal to `value`

#### Scenario: Filter by maximum price only
- **WHEN** a GET request is sent to `/products?maxPrice={value}`
- **THEN** the system SHALL return `200 OK` with only products whose resolved effective price is
  less than or equal to `value`

#### Scenario: Filter by a price range
- **WHEN** a GET request is sent to `/products?minPrice={min}&maxPrice={max}` with `min <= max`
- **THEN** the system SHALL return `200 OK` with only products whose resolved effective price
  falls within `[min, max]` inclusive

#### Scenario: Invalid price range
- **WHEN** a GET request is sent to `/products?minPrice={min}&maxPrice={max}` with `min > max`,
  or with a negative `minPrice` or `maxPrice`
- **THEN** the system SHALL return `400 Bad Request` with validation errors

#### Scenario: Price filter uses the active sale price, not the base price
- **WHEN** a product has `BasePrice = 100` and an active `ProductPrice` of `60` (current time
  within `StartDate`/`EndDate`), and a GET request is sent to `/products?minPrice=50&maxPrice=70`
- **THEN** the system SHALL include that product in the results (matched on `60`, not `100`)

### Requirement: Search products by keyword
`GET /products` SHALL support a case-insensitive keyword search (`q`) matched as a substring
against `Product.Name` or `Product.Description`.

#### Scenario: Keyword matches product name
- **WHEN** a GET request is sent to `/products?q={term}` and `term` is a case-insensitive
  substring of an active product's `Name`
- **THEN** the system SHALL return `200 OK` including that product

#### Scenario: Keyword matches product description
- **WHEN** a GET request is sent to `/products?q={term}` and `term` is a case-insensitive
  substring of an active product's `Description` but not its `Name`
- **THEN** the system SHALL return `200 OK` including that product

#### Scenario: Keyword with no matches
- **WHEN** a GET request is sent to `/products?q={term}` and no active product's `Name` or
  `Description` contains `term`
- **THEN** the system SHALL return `200 OK` with an empty `items` array and `totalCount` of `0`

#### Scenario: Blank keyword is treated as no filter
- **WHEN** a GET request is sent to `/products?q=` or `/products?q=%20` (empty or
  whitespace-only)
- **THEN** the system SHALL NOT apply a keyword filter (equivalent to omitting `q`)

### Requirement: Sort search results
`GET /products` SHALL support sorting by `sortBy` (`price`, `name`, or `newest`) and
`sortDirection` (`asc` or `desc`), defaulting to `sortBy=name`, `sortDirection=asc` when omitted.

#### Scenario: Sort by price
- **WHEN** a GET request is sent to `/products?sortBy=price&sortDirection=asc` (or `desc`)
- **THEN** the system SHALL return `200 OK` with results ordered by each product's resolved
  effective price, ascending (or descending)

#### Scenario: Sort by name
- **WHEN** a GET request is sent to `/products?sortBy=name&sortDirection=asc` (or `desc`)
- **THEN** the system SHALL return `200 OK` with results ordered alphabetically by `Name`,
  ascending (or descending)

#### Scenario: Sort by newest
- **WHEN** a GET request is sent to `/products?sortBy=newest&sortDirection=desc`
- **THEN** the system SHALL return `200 OK` with results ordered by `CreatedAt` descending
  (most recently created first)

#### Scenario: Default sort
- **WHEN** a GET request is sent to `/products` with no `sortBy`/`sortDirection`
- **THEN** the system SHALL order results by `Name` ascending

#### Scenario: Invalid sort parameters
- **WHEN** a GET request is sent to `/products?sortBy={value}` or `?sortDirection={value}` where
  `value` is not one of the supported values
- **THEN** the system SHALL return `400 Bad Request` with validation errors

### Requirement: Paginate search results
`GET /products` SHALL support offset-based pagination via `page` (1-based, default `1`) and
`pageSize` (default `20`, max `100`), returning a paginated envelope with `items`, `totalCount`,
`page`, `pageSize`, and `totalPages`.

#### Scenario: Default pagination
- **WHEN** a GET request is sent to `/products` with no `page`/`pageSize`
- **THEN** the system SHALL return `200 OK` with `page=1`, `pageSize=20`, up to 20 items, and the
  total count of matching products in `totalCount`

#### Scenario: Request a specific page
- **WHEN** a GET request is sent to `/products?page=2&pageSize=10` and more than 10 matching
  products exist
- **THEN** the system SHALL return `200 OK` with the 11th–20th matching products (per the active
  sort order) and `totalCount` reflecting all matching products, not just the returned page

#### Scenario: Page beyond available results
- **WHEN** a GET request is sent to `/products?page={n}` where `n` exceeds the number of
  available pages for the current filter/sort
- **THEN** the system SHALL return `200 OK` with an empty `items` array and the correct
  `totalCount`

#### Scenario: Invalid pagination parameters
- **WHEN** a GET request is sent to `/products?page={value}` or `?pageSize={value}` where
  `value` is less than `1`, or `pageSize` exceeds `100`
- **THEN** the system SHALL return `400 Bad Request` with validation errors

### Requirement: Combine multiple search criteria
`GET /products` SHALL apply category, price-range, and keyword filters together (as an AND
across filter types) before sorting and paginating.

#### Scenario: All filters combined
- **WHEN** a GET request is sent to `/products?categoryId={id}&minPrice={min}&maxPrice={max}&q={term}&sortBy=price&sortDirection=desc&page=1&pageSize=10`
- **THEN** the system SHALL return `200 OK` with only active products that belong to the
  specified category, have a resolved effective price within `[min, max]`, and match `term` in
  `Name` or `Description`, ordered by resolved effective price descending, limited to the first
  10 matches, with `totalCount` reflecting all matches before pagination

### Requirement: Search results exclude inactive products
`GET /products` SHALL only return products where `IsActive = true`, regardless of which filters
are applied.

#### Scenario: Soft-deleted product excluded
- **WHEN** a product has `IsActive = false` and would otherwise match the requested filters
- **THEN** the system SHALL exclude it from the search results and from `totalCount`

### Requirement: Get product details with resolved price
`GET /products/{id}` SHALL return a single active product's details, including its category name
and its resolved effective price (the active `ProductPrice` as of now, per D4, falling back to
`Product.BasePrice` when none is active).

#### Scenario: Product with an active sale price
- **WHEN** a GET request is sent to `/products/{id}` for an active product that has an active
  `ProductPrice` (current time within `StartDate`/`EndDate`)
- **THEN** the system SHALL return `200 OK` with the product details and a `price` field equal to
  that active `ProductPrice.Price`

#### Scenario: Product with no active price record
- **WHEN** a GET request is sent to `/products/{id}` for an active product with no active
  `ProductPrice`
- **THEN** the system SHALL return `200 OK` with the product details and a `price` field equal to
  `Product.BasePrice`

#### Scenario: Non-existent or inactive product
- **WHEN** a GET request is sent to `/products/{id}` for an ID that does not exist, or whose
  product has `IsActive = false`
- **THEN** the system SHALL return `404 Not Found`
