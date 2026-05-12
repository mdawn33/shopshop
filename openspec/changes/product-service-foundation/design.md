## Context

The `Shoppiness.ProductService` exists as an empty Web API scaffold. No domain model, infrastructure, or features are implemented. This design establishes the full foundation: domain entities, EF Core persistence, and vertical-slice features — including the first transactional operation, Purchase Product.

The service follows the project-wide hybrid architecture: Domain (pure), Infrastructure (persistence), API (vertical slices). Cross-service calls to Stock and Payment are deferred via stub interfaces.

## Goals / Non-Goals

**Goals:**
- Establish `ProductService.Domain` with `Category`, `Product`, and `ProductPrice` entities
- Establish `ProductService.Infrastructure` with EF Core + PostgreSQL and Fluent API configurations
- Implement Category CRUD, Product CRUD, and Purchase Product vertical-slice features
- Declare `IStockService` and `IPaymentService` stub interfaces for future wiring

**Non-Goals:**
- Actual integration with StockService or PaymentService (deferred)
- Authentication / authorization
- Pagination or advanced filtering beyond `categoryId`
- Multi-warehouse stock support

## Decisions

### D1: Self-referencing Category hierarchy
`Category` references itself via nullable `ParentCategoryId`. Root categories have `ParentCategoryId = null`. The FK uses `DeleteBehavior.Restrict` — deleting a parent category is blocked if children exist, preventing orphaned subtrees.

**Alternative considered:** Closure table for efficient ancestor queries. Rejected as premature — simple parent FK is sufficient for Phase 1 and avoids extra complexity.

### D2: Active price resolution with BasePrice fallback
`PurchaseProductHandler` queries for the active `ProductPrice` where `StartDate <= now <= EndDate` (or `EndDate` is null) ordered by `StartDate DESC`, taking the first. If none exists, falls back to `Product.BasePrice`.

**Alternative considered:** Always requiring a `ProductPrice` record. Rejected — forces unnecessary setup for products with a fixed price.

### D3: Stub interfaces for cross-service calls
`IStockService` and `IPaymentService` are declared in `Shoppiness.ProductService` (not in Domain). Stub implementations return hardcoded success responses and are registered in DI. Real implementations will replace stubs in a future change.

**Alternative considered:** Direct HTTP calls now. Rejected — wires services before they are ready and couples this change to StockService/PaymentService readiness.

### D4: No SalesOrder entity in this service
The `PurchaseProductResponse` returns an `OrderId` (a new `Guid` generated at handler time) but no `SalesOrder` is persisted in ProductService. Order persistence belongs to a future Order microservice.

**Alternative considered:** Persist a SalesOrder here. Rejected — violates service boundaries; order management is its own domain.

### D5: UpdatedAt managed in infrastructure, not domain
`UpdatedAt` is set via `SaveChanges` override in `ProductDbContext` rather than in entity setters, keeping entities clean.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Stub stock check always returns "in stock" — purchase can succeed with zero inventory | Acceptable for Phase 1; will be enforced once StockService is wired |
| Stub payment notification is a no-op — no payment is triggered | Acceptable for Phase 1; documented as stub behavior |
| Self-referencing category with Restrict delete may confuse clients | API should return a clear 409 Conflict with a descriptive message |
| BasePrice fallback could mask missing ProductPrice data | Log a warning when fallback is used |

## Migration Plan

1. Add NuGet packages to each project
2. Create domain entities and enums
3. Create `ProductDbContext` with configurations
4. Add EF Core migration: `InitialCreate`
5. Apply migration to development PostgreSQL instance
6. Implement and register vertical-slice features
7. Register stub services in DI
8. Smoke-test all endpoints via Scalar/OpenAPI

Rollback: Drop the new tables; no existing data is affected (greenfield service).
