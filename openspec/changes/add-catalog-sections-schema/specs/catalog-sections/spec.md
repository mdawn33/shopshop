## ADDED Requirements

### Requirement: List active catalog sections in display order
`GET /catalog/sections` SHALL return all active (`IsActive = true`) `CatalogSection` rows,
ordered by `DisplayOrder` ascending, with `CreatedAt` ascending as a tiebreaker for equal
`DisplayOrder` values.

#### Scenario: Sections returned in configured order
- **WHEN** a GET request is sent to `/catalog/sections` and two active sections exist with
  `DisplayOrder` `1` and `2`
- **THEN** the system SHALL return `200 OK` with the `DisplayOrder = 1` section first

#### Scenario: Sections with equal display order break ties by creation time
- **WHEN** two active sections share the same `DisplayOrder`
- **THEN** the system SHALL order them by `CreatedAt` ascending (the section created first appears
  first)

#### Scenario: Inactive section excluded
- **WHEN** a `CatalogSection` has `IsActive = false`
- **THEN** the system SHALL exclude it from the `/catalog/sections` response

#### Scenario: No active sections configured
- **WHEN** a GET request is sent to `/catalog/sections` and no active `CatalogSection` rows exist
- **THEN** the system SHALL return `200 OK` with an empty sections array, not an error

### Requirement: Resolve "New" section products by recency
A `CatalogSection` with `SectionType = New` SHALL resolve its `products` to active
(`Product.IsActive = true`) products ordered by `Product.CreatedAt` descending, limited to the
section's `MaxItems`.

#### Scenario: New section returns most recently created products first
- **WHEN** a `New` section is resolved and multiple active products exist with different
  `CreatedAt` values
- **THEN** the system SHALL return them ordered by `CreatedAt` descending (most recently created
  first)

#### Scenario: New section is capped at MaxItems
- **WHEN** a `New` section has `MaxItems = 12` and more than 12 active products exist
- **THEN** the system SHALL return exactly 12 products — the 12 most recently created

### Requirement: Resolve "Offers" section products by active promotional price
A `CatalogSection` with `SectionType = Offers` SHALL resolve its `products` to active products
that currently have an active `ProductPrice` (`StartDate <= now` and `EndDate` is null or `>=
now`) whose `PriceType` is `Sale` or `Clearance`, ordered by that active `ProductPrice`'s
`StartDate` descending, limited to the section's `MaxItems`.

#### Scenario: Offers section includes products with an active Sale price
- **WHEN** an active product has an active `ProductPrice` with `PriceType = Sale`
- **THEN** the system SHALL include that product in an `Offers` section's resolved products

#### Scenario: Offers section includes products with an active Clearance price
- **WHEN** an active product has an active `ProductPrice` with `PriceType = Clearance`
- **THEN** the system SHALL include that product in an `Offers` section's resolved products

#### Scenario: Offers section excludes products with only a Regular price
- **WHEN** an active product's only `ProductPrice` row (active or not) has `PriceType = Regular`
- **THEN** the system SHALL exclude that product from an `Offers` section's resolved products

#### Scenario: Offers section excludes products relying on BasePrice alone
- **WHEN** an active product has no `ProductPrice` rows at all and only `Product.BasePrice`
- **THEN** the system SHALL exclude that product from an `Offers` section's resolved products

#### Scenario: Offers section excludes products with an expired or future promotional price
- **WHEN** a product's `ProductPrice` of `PriceType` `Sale` or `Clearance` has `EndDate` in the
  past, or `StartDate` in the future
- **THEN** the system SHALL exclude that product from an `Offers` section's resolved products

#### Scenario: Offers section is capped at MaxItems
- **WHEN** an `Offers` section has `MaxItems = 12` and more than 12 active products currently
  qualify
- **THEN** the system SHALL return exactly 12 products — the 12 with the most recently started
  active promotional price

### Requirement: Section products carry the resolved effective price
Each product returned within a section SHALL include its resolved effective price — the active
`ProductPrice` as of now (per D4), falling back to `Product.BasePrice` when none is active —
computed with the same rule used by `GET /products` and `GET /products/{id}`.

#### Scenario: Section product price matches the active promotional price
- **WHEN** a product included in a resolved section has an active `ProductPrice`
- **THEN** the system SHALL return that product's `price` field equal to the active
  `ProductPrice.Price`, not `Product.BasePrice`

#### Scenario: Section product price falls back to BasePrice
- **WHEN** a product included in a resolved section (e.g. a `New` section product) has no active
  `ProductPrice`
- **THEN** the system SHALL return that product's `price` field equal to `Product.BasePrice`

### Requirement: Section products include catalog attributes
Each product returned within a section SHALL include its `Brand`, `Sku`, and `Variant` fields
(each nullable), matching the values stored on the underlying `Product`.

#### Scenario: Product with all catalog attributes set
- **WHEN** a resolved section includes a product with non-null `Brand`, `Sku`, and `Variant`
- **THEN** the system SHALL return all three fields with their stored values

#### Scenario: Product missing some catalog attributes
- **WHEN** a resolved section includes a product with a null `Variant` (e.g. a book or grocery
  item with no meaningful variant)
- **THEN** the system SHALL return `variant` as `null` rather than omitting the product or
  substituting a placeholder value

### Requirement: Sections exclude soft-deleted products
Regardless of `SectionType`, a `CatalogSection`'s resolved `products` SHALL only include products
where `Product.IsActive = true`.

#### Scenario: Soft-deleted product excluded from every section type
- **WHEN** a product has `IsActive = false` and would otherwise match a section's resolution rule
  (e.g. it is the newest product, or currently has an active Sale price)
- **THEN** the system SHALL exclude it from that section's resolved `products`

### Requirement: Section with no matching products returns an empty list
A `CatalogSection` whose resolution rule currently matches zero products SHALL still be included
in the `/catalog/sections` response, with an empty `products` array.

#### Scenario: Offers section with nothing currently on sale
- **WHEN** an active `Offers` section is resolved and no active product currently has an active
  `Sale` or `Clearance` `ProductPrice`
- **THEN** the system SHALL return `200 OK` including that section with `products: []`, not omit
  the section from the response
