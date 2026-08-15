using ProductService.Infrastructure.Extensions;
using Shoppiness.ProductService.Extensions;
using Shoppiness.ProductService.Features.Catalog.Sections;
using CategoryCreate = Shoppiness.ProductService.Features.Categories.Create;
using CategoryGetById = Shoppiness.ProductService.Features.Categories.GetById;
using CategoryList = Shoppiness.ProductService.Features.Categories.List;
using CategoryUpdate = Shoppiness.ProductService.Features.Categories.Update;
using CategoryDelete = Shoppiness.ProductService.Features.Categories.Delete;
using ProductCreate = Shoppiness.ProductService.Features.Products.Create;
using ProductGetById = Shoppiness.ProductService.Features.Products.GetById;
using ProductUpdate = Shoppiness.ProductService.Features.Products.Update;
using ProductDelete = Shoppiness.ProductService.Features.Products.Delete;
using Shoppiness.ProductService.Features.Products.Purchase;
using Shoppiness.ProductService.Features.Products.Search;

var builder = WebApplication.CreateBuilder(args);

// Allow Aspire to discover the service
builder.AddServiceDefaults();

builder.Services.AddOpenApi();

// // Register Azure Service Bus client
// builder.AddAzureServiceBusClient("service-bus");
// // Enable distributed tracing for Azure Service Bus
// AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

// Map health check endpoints (only for development, for now)
app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Shoppiness.ProductsService v1"));
}

app.UseHttpsRedirection();

// Category endpoints
CategoryCreate.MapEndpoint(app);
CategoryGetById.MapEndpoint(app);
CategoryList.MapEndpoint(app);
CategoryUpdate.MapEndpoint(app);
CategoryDelete.MapEndpoint(app);

// Product endpoints
ProductCreate.MapEndpoint(app);
ProductGetById.MapEndpoint(app);
SearchProductsEndpoint.MapEndpoint(app);
ProductUpdate.MapEndpoint(app);
ProductDelete.MapEndpoint(app);

// Purchase endpoint
PurchaseProductEndpoint.MapEndpoint(app);

// Catalog endpoints
GetCatalogSectionsEndpoint.MapEndpoint(app);

app.Run();
