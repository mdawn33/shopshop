## Why

The Product microservice is the core of the Shoppiness platform — without it, no other service can function. Establishing its domain model, infrastructure, and the first transactional feature (Purchase Product) unlocks the end-to-end flow needed to wire Stock and Payment services.

## What Changes

- Add `ProductService.Domain` library project with `Category`, `Product`, and `ProductPrice` entities and `PriceType` enum
- Add `ProductService.Infrastructure` library project with `ProductDbContext`, EF Core + PostgreSQL, and Fluent API entity configurations
- Replace the empty `Shoppiness.ProductService` Web API scaffold with a working service: Category CRUD, Product CRUD, and Purchase Product feature
- Introduce `IStockService` and `IPaymentService` stub interfaces for future cross-service wiring

## Capabilities

### New Capabilities

- `category-management`: CRUD operations for hierarchical categories (self-referencing, multi-level)
- `product-management`: CRUD operations for products, including category assignment and base price
- `product-pricing`: Time-based pricing via `ProductPrice` with `PriceType` (Regular, Sale, Clearance) and active price resolution
- `purchase-product`: Transactional endpoint that validates a purchase request, resolves the active price, checks stock (stub), and notifies payment (stub)

### Modified Capabilities

## Impact

- **New projects:** `ProductService.Domain`, `ProductService.Infrastructure`
- **Modified project:** `Shoppiness.ProductService` (replaces scaffold)
- **Database:** New PostgreSQL schema — `Categories`, `Products`, `ProductPrices` tables
- **Dependencies added:** `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `FluentValidation.AspNetCore`
- **Cross-service interfaces:** `IStockService`, `IPaymentService` declared in ProductService but not yet implemented — wired in a future change
