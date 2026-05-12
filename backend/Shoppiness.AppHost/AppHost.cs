var builder = DistributedApplication.CreateBuilder(args);

// var productPostgres = builder.AddPostgres("productPostgres")
//     .WithImage("postgres", "18.3-alpine3.23")
//     .WithHostPort(5432)
//     .WithInitFiles("../init/1CreateProductDbUser.sql")
//     .WithDataVolume(isReadOnly: false);
var shoppinessPostgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "18.3-alpine3.23")
    .WithHostPort(5432)
    .WithInitFiles("../init/1CreatePostgresDbUser.sql")
    .WithDataVolume(isReadOnly: false);

// This allows me to use an already created database, and inject it into the application
var shoppinessPgDb = shoppinessPostgres.AddDatabase("ProductPgDb");


// Configure Azure Service Bus
var serviceBus = builder.AddAzureServiceBus("service-bus")
    .RunAsEmulator();

serviceBus.AddServiceBusQueue("update-stock");
// serviceBus.AddServiceBusQueue("payment-created");
// serviceBus.AddServiceBusQueue("fraud-decision");


// Stocks API
var stocksApi = builder.AddProject<Projects.Shoppiness_StockService>("stocks-api")
    .WithReference(shoppinessPostgres)
    .WithReference(serviceBus)
    .WithExternalHttpEndpoints();


builder.AddProject<Projects.Shoppiness_ProductService>("productService")
    .WithReference(stocksApi)
    .WithReference(shoppinessPgDb)
    .WithReference(serviceBus)
    .WithExternalHttpEndpoints();

builder.Build().Run();
