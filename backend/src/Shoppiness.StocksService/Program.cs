using Shoppiness.StockService.Extensions;
using StockService.Infrastructure.Extensions;
using GetStockLevel = Shoppiness.StockService.Features.Stocks.GetStockLevel;
using InitializeStock = Shoppiness.StockService.Features.Stocks.InitializeStock;
using AddStock = Shoppiness.StockService.Features.Stocks.AddStock;
using RemoveStock = Shoppiness.StockService.Features.Stocks.RemoveStock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.AddAzureServiceBusClient("service-bus");
AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);


builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "Shoppiness.StocksService v1"));
}

app.UseHttpsRedirection();


// Stock endpoints
GetStockLevel.MapEndpoint(app);
InitializeStock.MapEndpoint(app);
AddStock.MapEndpoint(app);
RemoveStock.MapEndpoint(app);

app.Run();
