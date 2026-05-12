## Why

ProductService is live but has no real stock enforcement — its `IStockService` stub always returns "in stock." The StockService microservice must be built so that inventory is tracked and enforced before any real purchase flow can be wired end-to-end.

## What Changes

- Create three new projects: `StockService.Domain` (library), `StockService.Infrastructure` (library), `Shoppiness.StockService` (Web API)
- Add all three to `Shoppiness.slnx`
- Establish the domain model: `Stock` entity, `StockMovement` entity, `MovementType` enum
  - `Stock` entity carries `QuantityReserved`, `ReorderLevel`, and `ReorderQuantity` in addition to `Quantity`
- Implement EF Core persistence with PostgreSQL and Fluent API entity configurations
- Expose four HTTP endpoints (get stock level, initialize stock, add stock, remove stock)
- Declare `IStockEventPublisher` stub interface for future Azure Service Bus wiring
- Wire DI, FluentValidation, and OpenAPI via extension classes in each layer

## Capabilities

### New Capabilities

- `get-stock-level`: Query current stock level for a product by ProductId; response includes `quantity`, `quantityReserved`, `reorderLevel`, and `reorderQuantity`; returns 404 if no stock record exists
- `initialize-stock`: Create a new Stock record for a product with quantity defaulting to 0; returns 409 if a record already exists
- `add-stock`: Increase stock quantity for a product (replenishment or return); creates a `StockMovement` audit record; returns 404 if product stock not found
- `remove-stock`: Decrease stock quantity for a product (sales deduction or adjustment); creates a `StockMovement` audit record; returns 404 if not found, 409 if insufficient quantity

### Modified Capabilities

## Impact

- **New projects:** `StockService.Domain`, `StockService.Infrastructure`, `Shoppiness.StockService` added to solution
- **Database:** New PostgreSQL schema with `Stocks` and `StockMovements` tables, managed by EF Core migrations
- **ProductService:** `IStockService` stub was removed; the interface now lives at `Shoppiness.ProductService/Features/Stocks/IStockService.cs` as a driven port with no registered implementation — real wiring is a future change
- **Solution file:** `Shoppiness.slnx` gains three new project entries
- **Dependencies added:** `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design` (Infrastructure); `FluentValidation.AspNetCore` (API)
