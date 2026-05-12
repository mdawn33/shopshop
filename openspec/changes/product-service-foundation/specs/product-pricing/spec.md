## ADDED Requirements

### Requirement: Resolve active product price
The system SHALL resolve the effective price for a product at a given point in time. The active price is the `ProductPrice` record where `StartDate <= now` and (`EndDate >= now` OR `EndDate` is null), ordered by `StartDate DESC`. If no active `ProductPrice` exists, the system SHALL fall back to `Product.BasePrice`.

#### Scenario: Active ProductPrice exists
- **WHEN** a product has a `ProductPrice` record valid at the current UTC time
- **THEN** the system SHALL use that record's `Price` as the effective unit price

#### Scenario: Multiple overlapping ProductPrice records
- **WHEN** a product has multiple valid `ProductPrice` records at the current UTC time
- **THEN** the system SHALL use the one with the most recent `StartDate`

#### Scenario: No active ProductPrice exists
- **WHEN** a product has no `ProductPrice` record valid at the current UTC time
- **THEN** the system SHALL fall back to `Product.BasePrice` as the effective unit price and log a warning

### Requirement: ProductPrice type classification
Each `ProductPrice` SHALL have a `PriceType` of `Regular`, `Sale`, or `Clearance` to classify the nature of the price.

#### Scenario: PriceType is included in responses
- **WHEN** an active `ProductPrice` is resolved
- **THEN** the system SHALL include the `PriceType` value in any response that exposes pricing information
