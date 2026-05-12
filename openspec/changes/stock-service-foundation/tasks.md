## 1. Solution Setup

- [x] 1.1 Create `StockService.Domain` library project and add to `Shoppiness.slnx`
- [x] 1.2 Create `StockService.Infrastructure` library project and add to `Shoppiness.slnx`
- [x] 1.3 Create `Shoppiness.StockService` Web API project and add to `Shoppiness.slnx`
- [x] 1.4 Set project references: Infrastructure → Domain; StockService API → Domain + Infrastructure
- [x] 1.5 Add NuGet packages to Infrastructure: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`
- [x] 1.6 Add NuGet package to StockService API: `FluentValidation.AspNetCore`

## 2. Domain Layer

- [x] 2.1 Create `MovementType` enum (`Replenishment`, `SalesDeduction`, `ManualAdjustment`, `Return`) in `StockService.Domain`
- [x] 2.2 Create `Stock` entity with properties: `Id` (Guid), `ProductId` (Guid), `Quantity` (int), `CreatedAt` (DateTime), `UpdatedAt` (DateTime), `IsActive` (bool)
- [x] 2.3 Create `StockMovement` entity with properties: `Id` (Guid), `StockId` (Guid), `Stock` (navigation), `MovementType` (MovementType), `Quantity` (int), `ReferenceType` (string?), `ReferenceId` (Guid?), `CreatedAt` (DateTime)

## 3. Infrastructure Layer

- [x] 3.1 Create `StockDbContext` with `DbSet<Stock>` and `DbSet<StockMovement>`
- [x] 3.2 Override `SaveChangesAsync` in `StockDbContext` to auto-set `Stock.UpdatedAt` on modified Stock entries
- [x] 3.3 Create `StockConfiguration` implementing `IEntityTypeConfiguration<Stock>`: primary key, unique index on `ProductId`, required fields, check constraint `Quantity >= 0`, global `IsActive` query filter
- [x] 3.4 Create `StockMovementConfiguration` implementing `IEntityTypeConfiguration<StockMovement>`: primary key, FK to Stock with `DeleteBehavior.Restrict`, required fields, no soft-delete filter
- [x] 3.5 Apply both configurations in `StockDbContext.OnModelCreating`
- [x] 3.6 Create `InfrastructureServiceExtensions` class with `AddInfrastructure(IServiceCollection, IConfiguration)` extension method that registers `StockDbContext` with the PostgreSQL connection string
- [x] 3.7 Add EF Core migration `InitialCreate` and verify the generated SQL creates `Stocks` and `StockMovements` tables with correct constraints

## 4. Stub Interface

- [x] 4.1 Create `IStockEventPublisher` interface in `Shoppiness.StockService` with method `Task PublishStockUpdatedAsync(Guid productId, int newQuantity)`
- [x] 4.2 Create `StockEventPublisherStub` implementing `IStockEventPublisher` as a no-op (log a warning indicating the stub is active)
- [x] 4.3 Register `StockEventPublisherStub` as the `IStockEventPublisher` implementation in `ApiServiceExtensions.AddApiServices()`

## 5. Get Stock Level Feature

- [x] 5.1 Implement `Features/Stock/GetStockLevel.cs` — `GET /stock/{productId}`, query active Stock by ProductId, return 200 with `{ productId, quantity, updatedAt }`, return 404 if not found

## 6. Initialize Stock Feature

- [x] 6.1 Implement `Features/Stock/InitializeStock.cs` — `POST /stock`, FluentValidation (productId not empty), check for existing Stock record (active or inactive) and return 409 if found, create new Stock with `Quantity = 0` and `IsActive = true`, return 201 with `{ id, productId, quantity }`

## 7. Add Stock Feature

- [x] 7.1 Implement `Features/Stock/AddStock.cs` — `PATCH /stock/{productId}/add`, FluentValidation (quantity > 0, movementType must be `Replenishment` or `Return`), return 404 if active Stock not found, increase `Stock.Quantity`, create `StockMovement` with optional `referenceType` and `referenceId`, return 200 with `{ productId, newQuantity }`

## 8. Remove Stock Feature

- [x] 8.1 Implement `Features/Stock/RemoveStock.cs` — `PATCH /stock/{productId}/remove`, FluentValidation (quantity > 0, movementType must be `SalesDeduction` or `ManualAdjustment`), return 404 if active Stock not found, return 409 if `Stock.Quantity < requested quantity`, decrease `Stock.Quantity`, create `StockMovement` with optional `referenceType` and `referenceId`, return 200 with `{ productId, newQuantity }`

## 9. API Wiring

- [x] 9.1 Create `ApiServiceExtensions` class with `AddApiServices(IServiceCollection)` extension method that registers FluentValidation and `StockEventPublisherStub`
- [x] 9.2 Configure `appsettings.json` with a `ConnectionStrings:StockPgDb` placeholder
- [x] 9.3 Wire `AddInfrastructure()` and `AddApiServices()` in `Program.cs`
- [x] 9.4 Map all four feature endpoints in `Program.cs`
- [ ] 9.5 Verify OpenAPI (Scalar) shows all endpoints correctly
- [ ] 9.6 Smoke-test all endpoints against a local PostgreSQL instance
