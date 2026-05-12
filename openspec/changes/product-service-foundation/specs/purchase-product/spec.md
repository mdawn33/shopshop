## ADDED Requirements

### Requirement: Purchase a product
The system SHALL allow a customer to purchase a quantity of an active product. The operation validates the request, resolves the effective price, verifies stock availability, and notifies the payment service.

#### Scenario: Successful purchase
- **WHEN** a POST request is sent to `/products/{id}/purchase` with a valid `customerId` and `quantity > 0`
- **THEN** the system SHALL resolve the active price, confirm stock is available, notify the payment service, and return `200 OK` with `{ orderId, productId, unitPrice, total }`

#### Scenario: Product not found
- **WHEN** a POST request is sent to `/products/{id}/purchase` for a product ID that does not exist or is inactive
- **THEN** the system SHALL return `404 Not Found`

#### Scenario: Invalid quantity
- **WHEN** a POST request is sent to `/products/{id}/purchase` with `quantity <= 0`
- **THEN** the system SHALL return `400 Bad Request` with a validation error

#### Scenario: Missing customer ID
- **WHEN** a POST request is sent to `/products/{id}/purchase` with an empty `customerId`
- **THEN** the system SHALL return `400 Bad Request` with a validation error

#### Scenario: Stock unavailable
- **WHEN** the stock check via `IStockService` returns false for the requested quantity
- **THEN** the system SHALL return `409 Conflict` with a message indicating insufficient stock

### Requirement: Stock availability check (stub)
The system SHALL check stock availability by calling `IStockService.IsAvailableAsync(productId, quantity)` before confirming a purchase. In Phase 1, the stub implementation SHALL always return `true`.

#### Scenario: Stub always approves stock
- **WHEN** `IStockService` stub is called with any product ID and quantity
- **THEN** it SHALL return `true`

### Requirement: Payment notification (stub)
The system SHALL notify the payment service by calling `IPaymentService.NotifyPurchaseAsync(...)` after stock is confirmed. In Phase 1, the stub implementation SHALL be a no-op that returns successfully.

#### Scenario: Stub notification is a no-op
- **WHEN** `IPaymentService` stub is called
- **THEN** it SHALL complete without error and without performing any real payment action

### Requirement: Order ID generation
The system SHALL generate a new `Guid` as the `orderId` for each successful purchase. No `SalesOrder` entity is persisted in ProductService — order persistence is deferred to the Order microservice.

#### Scenario: OrderId is unique per purchase
- **WHEN** a purchase is completed successfully
- **THEN** the response SHALL include a unique `orderId` (newly generated Guid)
