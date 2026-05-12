## 1. Solution Setup

- [x] 1.1 Create `ProductService.Domain` library project and add to `Shoppiness.slnx`
- [x] 1.2 Create `ProductService.Infrastructure` library project and add to `Shoppiness.slnx`
- [x] 1.3 Add project references: Infrastructure → Domain; ProductService API → Domain + Infrastructure
- [x] 1.4 Add NuGet packages to Infrastructure: `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.EntityFrameworkCore.Design`
- [x] 1.5 Add NuGet packages to ProductService API: `FluentValidation.AspNetCore`

## 2. Domain Layer

- [x] 2.1 Create `PriceType` enum (`Regular`, `Sale`, `Clearance`) in `ProductService.Domain`
- [x] 2.2 Create `Category` entity with all properties (Id, Name, Description, ParentCategoryId, ParentCategory, SubCategories, Products, CreatedAt, UpdatedAt, IsActive)
- [x] 2.3 Create `Product` entity with all properties (Id, CategoryId, Category, Name, Description, BasePrice, CreatedAt, UpdatedAt, IsActive)
- [x] 2.4 Create `ProductPrice` entity with all properties (Id, ProductId, Product, Price, PriceType, StartDate, EndDate)

## 3. Infrastructure Layer

- [x] 3.1 Create `ProductDbContext` with `DbSet<Category>`, `DbSet<Product>`, `DbSet<ProductPrice>`
- [x] 3.2 Override `SaveChangesAsync` to auto-set `UpdatedAt` on modified entities
- [x] 3.3 Create `CategoryConfiguration`: primary key, required fields, self-referencing FK with `DeleteBehavior.Restrict`, global `IsActive` query filter
- [x] 3.4 Create `ProductConfiguration`: primary key, required fields, FK to Category, decimal precision for `BasePrice`, global `IsActive` query filter
- [x] 3.5 Create `ProductPriceConfiguration`: primary key, FK to Product, decimal precision for `Price`, index on `(ProductId, StartDate)`
- [x] 3.6 Apply all configurations via `IEntityTypeConfiguration<T>` in `OnModelCreating`
- [x] 3.7 Add EF Core migration `InitialCreate` and verify generated SQL

## 4. Stub Interfaces

- [x] 4.1 Create `IStockService` interface with `Task<bool> IsAvailableAsync(Guid productId, int quantity)`
- [x] 4.2 Create `StockServiceStub` implementing `IStockService` — always returns `true`
- [x] 4.3 Create `IPaymentService` interface with `Task NotifyPurchaseAsync(Guid orderId, Guid productId, Guid customerId, decimal unitPrice, int quantity)`
- [x] 4.4 Create `PaymentServiceStub` implementing `IPaymentService` — no-op implementation

## 5. Category Features

- [x] 5.1 Implement `Features/Categories/Create.cs` — `POST /categories`, FluentValidation (name required, optional parentCategoryId exists check), return 201
- [x] 5.2 Implement `Features/Categories/GetById.cs` — `GET /categories/{id}`, include SubCategories, return 404 if not found
- [x] 5.3 Implement `Features/Categories/List.cs` — `GET /categories`, optional `?parentCategoryId` filter
- [x] 5.4 Implement `Features/Categories/Update.cs` — `PUT /categories/{id}`, update name/description/parentCategoryId, set UpdatedAt, return 404 if not found
- [x] 5.5 Implement `Features/Categories/Delete.cs` — `DELETE /categories/{id}`, reject with 409 if active subcategories exist, soft-delete otherwise

## 6. Product Features

- [x] 6.1 Implement `Features/Products/Create.cs` — `POST /products`, FluentValidation (name required, basePrice > 0, categoryId exists), return 201
- [x] 6.2 Implement `Features/Products/GetById.cs` — `GET /products/{id}`, include Category name, return 404 if not found
- [x] 6.3 Implement `Features/Products/List.cs` — `GET /products`, optional `?categoryId` filter
- [x] 6.4 Implement `Features/Products/Update.cs` — `PUT /products/{id}`, update fields, set UpdatedAt, return 404 if not found
- [x] 6.5 Implement `Features/Products/Delete.cs` — `DELETE /products/{id}`, soft-delete, return 404 if not found

## 7. Purchase Product Feature

- [x] 7.1 Create `Features/Products/Purchase/PurchaseProductRequest.cs` — `{ CustomerId: Guid, Quantity: int }`
- [x] 7.2 Create `Features/Products/Purchase/PurchaseProductResponse.cs` — `{ OrderId: Guid, ProductId: Guid, UnitPrice: decimal, Total: decimal }`
- [x] 7.3 Create `Features/Products/Purchase/PurchaseProductValidator.cs` — `Quantity > 0`, `CustomerId` not empty
- [x] 7.4 Create `Features/Products/Purchase/PurchaseProductHandler.cs` — load product, resolve active price (fallback to BasePrice), call IStockService, call IPaymentService, generate OrderId, return response
- [x] 7.5 Create `Features/Products/Purchase/PurchaseProductEndpoint.cs` — `POST /products/{id}/purchase`, wire validator and handler, return 404/400/409/200 as appropriate

## 8. API Wiring

- [x] 8.1 Register `ProductDbContext` with PostgreSQL connection string in `Program.cs`
- [x] 8.2 Register `StockServiceStub` and `PaymentServiceStub` in DI
- [x] 8.3 Register FluentValidation
- [x] 8.4 Map all feature endpoints in `Program.cs`
- [ ] 8.5 Verify OpenAPI (Scalar) shows all endpoints correctly
- [ ] 8.6 Smoke-test all endpoints against a local PostgreSQL instance
