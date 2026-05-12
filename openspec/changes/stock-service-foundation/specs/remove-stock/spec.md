## ADDED Requirements

### Requirement: Remove stock quantity for a product
The system SHALL expose a write endpoint that decreases the stock quantity for a given product. The quantity MUST NOT go below zero. Every successful remove operation MUST create a `StockMovement` record for audit purposes. The `MovementType` for a remove is either `SalesDeduction` or `ManualAdjustment`, determined by the caller-supplied `movementType` field.

#### Scenario: Stock quantity is removed successfully
- **WHEN** `PATCH /stock/{productId}/remove` is called with a valid `productId`, a positive `quantity` that does not exceed the current stock level, and a valid `movementType` (`SalesDeduction` or `ManualAdjustment`)
- **THEN** the system decreases `Stock.Quantity` by the requested amount, creates a `StockMovement` record with the corresponding `MovementType`, and returns `200 OK` with `{ "productId": "<guid>", "newQuantity": <int> }`

#### Scenario: Insufficient stock
- **WHEN** `PATCH /stock/{productId}/remove` is called with a `quantity` that exceeds the current `Stock.Quantity`
- **THEN** the system returns `409 Conflict` with a descriptive message and does not modify the stock level or create a StockMovement

#### Scenario: Product stock record does not exist
- **WHEN** `PATCH /stock/{productId}/remove` is called with a `productId` for which no active Stock record exists
- **THEN** the system returns `404 Not Found`

#### Scenario: Quantity is zero or negative
- **WHEN** `PATCH /stock/{productId}/remove` is called with `quantity` less than or equal to 0
- **THEN** the system returns `400 Bad Request` with a validation error

#### Scenario: MovementType is not valid for a remove operation
- **WHEN** `PATCH /stock/{productId}/remove` is called with a `movementType` that is not `SalesDeduction` or `ManualAdjustment`
- **THEN** the system returns `400 Bad Request`

#### Scenario: Optional reference fields are provided
- **WHEN** `PATCH /stock/{productId}/remove` is called with optional `referenceType` (string) and `referenceId` (Guid) fields
- **THEN** the system stores both fields on the created `StockMovement` record
