var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment(name: "shoppiness-env");

// Create the DBs for the products, stocks and payments services

// var productPostgres = builder.AddPostgres("productPostgres")
//     .WithImage("postgres", "18.3-alpine3.23")
//     .WithHostPort(5432)
//     .WithInitFiles("../init/1CreateProductDbUser.sql")
//     .WithDataVolume(isReadOnly: false);

// var postgresPassword = builder.AddParameter("postgresPassword", "Abc1234", secret: true);

// Next line creates the postgres resource, and automatically creates the postgres user and password. Password can be found in the Aspire Dashboard
var shoppinessPostgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "18.3-alpine3.23")
    .WithHostPort(5432)
    .WithInitFiles("../init/1CreateDbsUsers.sql")
    .WithDataVolume(isReadOnly: false);

// This allows me to use an already created database, and inject it into the application
var productsPgDb = shoppinessPostgres.AddDatabase("ProductsPgDb");
var stocksPgDb = shoppinessPostgres.AddDatabase("StocksPgDb");


// Configure Azure Service Bus
// var serviceBus = builder.AddAzureServiceBus("service-bus")
//     .RunAsEmulator();
//
// serviceBus.AddServiceBusQueue("update-stock");
// serviceBus.AddServiceBusQueue("payment-created");
// serviceBus.AddServiceBusQueue("fraud-decision");


// Stocks API
var stocksApi = builder.AddProject<Projects.Shoppiness_StocksService>("stocks-api")
    .WithReference(stocksPgDb)
    // .WithReference(serviceBus)
    .WithHttpEndpoint(port: 5200, name: "http")
    .WithExternalHttpEndpoints();


var productsApi = builder.AddProject<Projects.Shoppiness_ProductsService>("products-api")
    .WithReference(stocksApi)
    .WithReference(productsPgDb)
    // .WithReference(serviceBus)
    .WithHttpEndpoint(port: 5100, name: "http")
    .WithExternalHttpEndpoints();

// API Gateway
builder.AddProject<Projects.Gateway_Api>("gateway-api")
    .WithReference(stocksApi)
    .WithReference(productsApi)
    .WaitFor(stocksApi)
    .WaitFor(productsApi)
    .WithHttpsEndpoint(5001)
    .WithHttpEndpoint(5000)
    .WithExternalHttpEndpoints();



builder.Build().Run();
