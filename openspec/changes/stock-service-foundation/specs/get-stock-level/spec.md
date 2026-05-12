## ADDED Requirements

### Requirement: Query current stock level by product
The system SHALL expose a read endpoint that returns the current stock quantity for a given product. The lookup is by `productId` (Guid). Only active Stock records are considered.

#### Scenario: Product stock exists and is active
- **WHEN** `GET /stock/{productId}` is called with a valid `productId` that has an active Stock record
- **THEN** the system returns `200 OK` with `{ "productId": "<guid>", "quantity": <int>, "quantityReserved": <int>, "reorderLevel": <int>, "reorderQuantity": <int>, "updatedAt": "<datetime>" }`

#### Scenario: Product stock does not exist
- **WHEN** `GET /stock/{productId}` is called with a `productId` for which no Stock record exists (or the record is soft-deleted)
- **THEN** the system returns `404 Not Found`

#### Scenario: ProductId path parameter is not a valid Guid
- **WHEN** `GET /stock/{productId}` is called with a non-Guid value for `productId`
- **THEN** the system returns `400 Bad Request`
