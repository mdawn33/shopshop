## ADDED Requirements

### Requirement: Add stock quantity for a product
The system SHALL expose a write endpoint that increases the stock quantity for a given product. Every successful add operation MUST create a `StockMovement` record for audit purposes. The `MovementType` for an add is either `Replenishment` or `Return`, determined by the caller-supplied `movementType` field.

#### Scenario: Stock quantity is added successfully
- **WHEN** `PATCH /stock/{productId}/add` is called with a valid `productId`, a positive `quantity`, and a valid `movementType` (`Replenishment` or `Return`)
- **THEN** the system increases `Stock.Quantity` by the requested amount, creates a `StockMovement` record with the corresponding `MovementType`, and returns `200 OK` with `{ "productId": "<guid>", "newQuantity": <int> }`

#### Scenario: Product stock record does not exist
- **WHEN** `PATCH /stock/{productId}/add` is called with a `productId` for which no active Stock record exists
- **THEN** the system returns `404 Not Found`

#### Scenario: Quantity is zero or negative
- **WHEN** `PATCH /stock/{productId}/add` is called with `quantity` less than or equal to 0
- **THEN** the system returns `400 Bad Request` with a validation error

#### Scenario: MovementType is not valid for an add operation
- **WHEN** `PATCH /stock/{productId}/add` is called with a `movementType` that is not `Replenishment` or `Return`
- **THEN** the system returns `400 Bad Request`

#### Scenario: Optional reference fields are provided
- **WHEN** `PATCH /stock/{productId}/add` is called with optional `referenceType` (string) and `referenceId` (Guid) fields
- **THEN** the system stores both fields on the created `StockMovement` record
