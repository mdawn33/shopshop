# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**Shoppiness** is a distributed e-commerce application built with a microservices architecture on .NET 10.

## Architecture

### Hybrid Architecture per Microservice

Each microservice follows a hybrid of **Vertical Slice Architecture** (self-contained features in the API layer) and a **Clean Architecture** with a separation of business core from external concerns:

- `Shoppiness.<ServiceName>` — REST API project (Vertical Slice features)
- `<ServiceName>.Domain` — Domain layer (Library)
- `<ServiceName>.Infrastructure` — Infrastructure layer (Library)

### Vertical Slice Conventions

Each feature file contains:
1. **Endpoint definition** - Route, HTTP method, metadata
2. **Request/Response DTOs** - Scoped to the feature
3. **Validation** - FluentValidation rules for the request
4. **Handler logic** - The actual operation (uses DbContext directly)

Single file per operation for simple CRUD. Folder with multiple files for complex features.

### Microservices (Phase 1)

| Service | API Project | Domain | Infrastructure |
|---|---|---|---|
| Product | `Shoppiness.ProductService` | `ProductService.Domain` | `ProductService.Infrastructure` |
| Stock | `Shoppiness.StockService` | `StockService.Domain` | `StockService.Infrastructure` |
| Payment | `Shoppiness.PaymentService` | `PaymentService.Domain` | `PaymentService.Infrastructure` |
| Notification | `Shoppiness.NotificationService` | `NotificationService.Domain` | `NotificationService.Infrastructure` |

### Data Storage

- **PostgreSQL** — relational data
- **MongoDB** — document storage
- **Redis** — caching

## Development Methodology

This project follows **Spec Driven Development (SDD)** using the **OpenSpec framework**. All features must be spec-first: design artifacts (OpenSpec documents, task lists, architecture decisions) are produced before implementation begins.

## Project Setup

- **Target framework:** .NET 10
- **Solution file:** `Shoppiness.slnx`
- **API projects:** Web API template
- **Domain & Infrastructure projects:** Library template

**Constraints:**
- Clean Architecture: Domain has no dependencies
- Guid IDs for distributed systems compatibility
- Soft deletes via `IsActive` flags



## Decisions

### D1: Project Structure - Hybrid Architecture

**Decision:** Use Hybrid Architecture with 3 projects: Domain (pure), Infrastructure (persistence), API (vertical slices).

**Rationale:**
- Domain stays clean (no framework dependencies) - this is the protected core
- Vertical slices in API reduce ceremony and keep related code together
- No Application layer needed - feature handlers contain use-case logic
- No Repository pattern needed - DbContext provides sufficient abstraction for CRUD

**Alternatives Considered:**
- Full Clean Architecture: Too much ceremony for this scope
- Pure Vertical Slices (no Domain project): Couples entities to EF Core
- Onion Architecture: Similar to Clean but with different layer names - same ceremony problem

### D2: Entity Identity - Guid vs int

**Decision:** Use `Guid` for all entity IDs.

**Rationale:**
- Distributed systems ready (no central ID generator needed)
- Can generate IDs client-side before persistence
- No sequential ID leakage (security)

**Alternatives Considered:**
- `int` with identity: Simpler but problematic for distributed scenarios
- `long`: Same issues as int
- Sequential Guid (COMB): Could optimize for clustered indexes, but adds complexity

### D3: Stock-Product Relationship

**Decision:** 1:1 relationship between Stock and Product with `Stock.ProductId` as unique.

**Rationale:** Single warehouse assumption simplifies the model. When multi-warehouse is needed, add `WarehouseId` and change unique constraint to `(ProductId, WarehouseId)`.

**Alternatives Considered:**
- No Stock entity (quantity on Product): Loses audit capability via StockMovement
- Many-to-many with Warehouse: Premature complexity

### D4: Time-Based Pricing via ProductPrice

**Decision:** Separate `ProductPrice` entity with `StartDate`/`EndDate` and `PriceType` enum.

**Rationale:**
- Allows scheduling sales and clearances
- Historical price tracking
- `Product.BasePrice` serves as fallback when no active ProductPrice exists

**Alternatives Considered:**
- Price on Product only: No time-based pricing capability
- Price history table: Read-only audit, not scheduling

### D5: Inventory Audit via StockMovement

**Decision:** All stock changes create a `StockMovement` record with `MovementType`, `ReferenceType`, and `ReferenceId`.

**Rationale:**
- Complete audit trail for inventory
- Traceable back to PurchaseOrder, SalesOrder, or ManualAdjustment
- Enables inventory reconciliation and fraud detection

**Alternatives Considered:**
- Direct Stock updates: No audit trail
- Event sourcing: Overkill for this scope

### D6: Separate User and Customer

**Decision:** `User` handles authentication, `Customer` handles business data with optional `UserId` link.

**Rationale:**
- Allows guest checkout (Customer without User)
- Clean separation of auth vs business concerns
- Admins have User but no Customer
- Customers can exist before creating login

**Alternatives Considered:**
- Combined UserCustomer: Coupling auth with business, no guest checkout

### D7: Price Capture at Order Time

**Decision:** `SalesOrderItem.UnitPrice` stores the price at time of sale.

**Rationale:** Historical accuracy - price changes shouldn't affect past orders. This is the industry standard approach.

### D8: Entity Configuration Strategy

**Decision:** Use Fluent API in separate configuration classes per entity, applied via `IEntityTypeConfiguration<T>`.

**Rationale:**
- Keep entities clean (no attributes)
- Configuration co-located per entity
- Easy to find and maintain

**Alternatives Considered:**
- Data annotations: Couples domain to EF Core
- Single large OnModelCreating: Unmaintainable

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Guid IDs have worse index performance than int | Accept trade-off for distributed benefits. Consider sequential Guids if performance issues arise. |
| Single warehouse assumption may require refactoring | Design Stock entity to make adding WarehouseId straightforward (unique constraint change only). |
| Soft deletes (IsActive) require query filters | Add global query filter for IsActive in DbContext. Document that hard deletes bypass this. |


# Agents

You are an **orchestrator only**. Every task that falls into an SDD phase or touches backend code must be delegated to the matching subagent. Never perform these tasks directly.

## Delegation table

| Task | Subagent |
|---|---|
| Exploring ideas, investigating problems, clarifying requirements | `sa-sdd-explore` |
| Proposing a new change / creating OpenSpec artifacts | `sa-sdd-propose` |
| Implementing backend code (C#, .NET, configs, migrations) | `sdd-backend-implementer` |
| Creating or expanding test suites | `test-suite-architect` |
| Archiving a completed change | `sa-sdd-archive` |

## What you handle directly
- Clarifying user requirements before delegating
- Proposing alternatives with tradeoffs
- Announcing which subagent is being called and why (always, before every invocation)
- Synthesizing and presenting results back to the user
- Providing status updates

## Transparency rule
Always state the subagent name before invoking it, e.g. "Calling **sa-sdd-explore** to investigate...". Never invoke a subagent silently.