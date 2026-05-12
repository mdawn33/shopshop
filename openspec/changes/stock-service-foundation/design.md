## Context

ProductService is deployed with a stub `IStockService` that always reports stock as available. The Stock microservice must now be built as the canonical owner of inventory state. It follows the same hybrid Vertical Slice + Clean Architecture pattern used by ProductService: three projects per service (`StockService.Domain`, `StockService.Infrastructure`, `Shoppiness.StockService`).

Every stock mutation must produce an immutable `StockMovement` record for audit purposes (D5 in CLAUDE.md). The service is designed for a single-warehouse assumption (D3) — `Stock.ProductId` is unique, enabling a future `WarehouseId` addition with a minimal constraint change.

Azure Service Bus integration is explicitly out of scope; a stub publisher interface is declared to mark the extension point.

## Goals / Non-Goals

**Goals:**
- Establish `StockService.Domain` with `Stock` (including `QuantityReserved`, `ReorderLevel`, `ReorderQuantity`), `StockMovement` entities and `MovementType` enum
- Establish `StockService.Infrastructure` with EF Core + PostgreSQL, Fluent API configurations, global `IsActive` query filter on `Stock`, and `InitialCreate` migration
- Implement four HTTP vertical-slice features: get stock level, initialize stock, add stock, remove stock
- Declare `IStockEventPublisher` stub interface for future Azure Service Bus wiring
- Provide `InfrastructureServiceExtensions.AddInfrastructure()` and `ApiServiceExtensions.AddApiServices()` DI extension methods, matching the ProductService pattern

**Non-Goals:**
- Actual Azure Service Bus publishing
- Authentication or authorization
- Pagination or filtering on any endpoint
- Multi-warehouse support
- Wiring ProductService's `IStockService` driven port to a real HTTP client (deferred to a future change)

## Decisions

### D1: StockMovement is append-only with no soft delete

`StockMovement` records are immutable audit entries. They have no `IsActive` flag and no `UpdatedAt`. Deleting or updating a movement would compromise the audit trail.

**Alternative considered:** Include `IsActive` on `StockMovement` to allow "voiding" movements. Rejected — voiding should be expressed as a new compensating movement (e.g., a `Return`), not by hiding records.

### D2: Global IsActive query filter on Stock only

`Stock` carries `IsActive` for soft delete. A global EF Core query filter is registered in `StockDbContext` so that deactivated stock rows are invisible to all queries by default. `StockMovement` has no such filter.

**Alternative considered:** Filter at the feature handler level. Rejected — easy to forget; a global filter is safer and matches D8 + ProductService convention.

### D3: UpdatedAt managed in SaveChangesAsync override

`Stock.UpdatedAt` is set inside a `SaveChangesAsync` override in `StockDbContext` rather than inside entity setters, keeping the domain model free of infrastructure concerns. This matches the ProductService pattern (design.md D5).

**Alternative considered:** Set `UpdatedAt` in the feature handler. Rejected — error-prone; every handler would need to remember to set it.

### D4: Insufficient stock returns 409 Conflict

When a remove-stock request would drive `Stock.Quantity` below zero, the endpoint returns `409 Conflict` with a descriptive message. The stock quantity is never allowed to go negative.

**Alternative considered:** Return 422 Unprocessable Entity. Rejected — 409 better expresses a state conflict (the current stock level prevents the operation) and is consistent with how ProductService uses 409 for duplicate records.

### D5: IStockEventPublisher declared in API project, not Domain

The publisher interface is a cross-service messaging concern, not a domain concept. It lives in `Shoppiness.StockService` alongside its no-op stub, following the same pattern as `IStockService` in `Shoppiness.ProductService/Features/Stocks/`.

**Alternative considered:** Declare in Domain. Rejected — Domain has zero framework or infrastructure dependencies; messaging is an infrastructure concern.

### D6: MovementType enum drives audit semantics

`MovementType` (`Replenishment`, `SalesDeduction`, `ManualAdjustment`, `Return`) is the sole type discriminator on `StockMovement`. `ReferenceType` (string) and `ReferenceId` (Guid?) provide optional linkage to external records (e.g., a purchase order or sales order ID) without requiring FK relationships to other services.

**Alternative considered:** Separate tables per movement type. Rejected — unnecessary complexity; the enum is sufficient for Phase 1 reporting and audit queries.

### D7: QuantityReserved tracks soft-hold for pending orders

`Stock.QuantityReserved` records how many units are soft-held by open orders without removing them from `Quantity`. Available quantity is computed as `Quantity - QuantityReserved`. This avoids premature stock deduction while still preventing over-selling.

**Alternative considered:** Deduct immediately on order placement. Rejected — if an order is cancelled, a compensating `StockMovement` would be needed; the reservation pattern is cleaner for pending-order semantics.

### D8: ReorderLevel and ReorderQuantity enable future auto-replenishment

`ReorderLevel` and `ReorderQuantity` are stored on the `Stock` record so that a future replenishment service can query stocks below threshold and know the standard order quantity without additional configuration. Both default to 0 (no auto-reorder) for Phase 1.

**Alternative considered:** Store reorder config in a separate table. Rejected — premature for Phase 1; a single entity is sufficient until multi-SKU reorder rules are needed.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Stock can be initialized multiple times for the same product if concurrent POST requests race | Add a unique index on `Stock.ProductId`; the database constraint produces a 409 response |
| Quantity going negative under concurrent remove requests | Use a database-level check constraint `Quantity >= 0` in the EF configuration as a secondary guard beyond the application-level 409 |
| Stub `IStockEventPublisher` means downstream services won't know about stock changes until wired | Document clearly in code and in this spec; the stub logs a warning |
| Soft-deleted Stock records could be re-initialized via POST, bypassing IsActive filter | `POST /stock` should check for any existing record including inactive ones, or re-activate with a reset — for Phase 1, reject with 409 if any record (active or not) exists for the ProductId |

## Migration Plan

1. Create `StockService.Domain` and `StockService.Infrastructure` library projects; create `Shoppiness.StockService` Web API project
2. Add all three to `Shoppiness.slnx` and set project references (Infrastructure → Domain; API → Domain + Infrastructure)
3. Add NuGet packages: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design` to Infrastructure; `FluentValidation.AspNetCore` to API
4. Implement domain entities and enum
5. Implement `StockDbContext` with Fluent API configurations
6. Generate `InitialCreate` migration and verify SQL
7. Implement vertical-slice feature files
8. Wire DI via extension methods; configure `appsettings.json` with connection string placeholder
9. Smoke-test all endpoints via Scalar/OpenAPI against a local PostgreSQL instance

Rollback: Drop the `Stocks` and `StockMovements` tables; remove the three projects from the solution. No existing service data is affected.
