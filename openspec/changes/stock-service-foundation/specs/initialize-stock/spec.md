## ADDED Requirements

### Requirement: Initialize a stock record for a product
The system SHALL expose a write endpoint that creates a new Stock record for a given product. The initial quantity defaults to 0. `QuantityReserved`, `ReorderLevel`, and `ReorderQuantity` all default to 0 on initialization. Only one Stock record per product is allowed, even if a previous record was soft-deleted.

#### Scenario: Stock is initialized successfully
- **WHEN** `POST /stock` is called with a valid `productId` that has no existing Stock record (active or inactive)
- **THEN** the system creates a Stock record with `Quantity = 0`, `QuantityReserved = 0`, `ReorderLevel = 0`, `ReorderQuantity = 0`, `IsActive = true`, and returns `201 Created` with `{ "id": "<guid>", "productId": "<guid>", "quantity": 0, "quantityReserved": 0, "reorderLevel": 0, "reorderQuantity": 0 }`

#### Scenario: Stock already exists for the product
- **WHEN** `POST /stock` is called with a `productId` that already has a Stock record (active or soft-deleted)
- **THEN** the system returns `409 Conflict` with a descriptive error message and does not create a duplicate record

#### Scenario: Request body is missing productId
- **WHEN** `POST /stock` is called without a `productId` in the request body
- **THEN** the system returns `400 Bad Request` with validation error details

#### Scenario: productId in request body is an empty Guid
- **WHEN** `POST /stock` is called with `productId` equal to `00000000-0000-0000-0000-000000000000`
- **THEN** the system returns `400 Bad Request`
