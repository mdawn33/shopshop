using ProductService.Infrastructure.Extensions;
using Shoppiness.ProductService.Extensions;
using CategoryCreate = Shoppiness.ProductService.Features.Categories.Create;
using CategoryGetById = Shoppiness.ProductService.Features.Categories.GetById;
using CategoryList = Shoppiness.ProductService.Features.Categories.List;
using CategoryUpdate = Shoppiness.ProductService.Features.Categories.Update;
using CategoryDelete = Shoppiness.ProductService.Features.Categories.Delete;
using ProductCreate = Shoppiness.ProductService.Features.Products.Create;
using ProductGetById = Shoppiness.ProductService.Features.Products.GetById;
using ProductList = Shoppiness.ProductService.Features.Products.List;
using ProductUpdate = Shoppiness.ProductService.Features.Products.Update;
using ProductDelete = Shoppiness.ProductService.Features.Products.Delete;
using Shoppiness.ProductService.Features.Products.Purchase;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

// Register Azure Service Bus client
builder.AddAzureServiceBusClient("service-bus");
// Enable distributed tracing for Azure Service Bus
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Shoppiness.ProductService v1"));
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
ProductList.MapEndpoint(app);
ProductUpdate.MapEndpoint(app);
ProductDelete.MapEndpoint(app);

// Purchase endpoint
PurchaseProductEndpoint.MapEndpoint(app);

app.Run();
