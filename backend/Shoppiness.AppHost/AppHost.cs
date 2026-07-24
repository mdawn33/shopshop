using System.Net.Sockets;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment(name: "shoppiness-env");

// Create the DBs for the products, stocks and payments services

// var postgresPassword = builder.AddParameter("postgresPassword", "Abc1234", secret: true);

// Next line creates the postgres resource, and automatically creates the postgres user and password. Password can be found in the Aspire Dashboard
var shoppinessPostgres = builder.AddPostgres("postgres")
    .WithImage("postgres", "18.3-alpine3.23")
    .WithHostPort(5432)
    .WithInitFiles("../init/1CreateDbsUsers.sql")
    // .WithEnvironment() // Add db user password to pass to the init script
    .WithDataVolume(isReadOnly: false);

// This allows me to use an already created database, and inject it into the application
var productsPgDb = shoppinessPostgres.AddDatabase("ProductsPgDb");
// var stocksPgDb = shoppinessPostgres.AddDatabase("StocksPgDb");


// Configure Azure Service Bus
// var serviceBus = builder.AddAzureServiceBus("service-bus")
//     .RunAsEmulator();
//
// serviceBus.AddServiceBusQueue("update-stock");
// serviceBus.AddServiceBusQueue("payment-created");
// serviceBus.AddServiceBusQueue("fraud-decision");


// Stocks API
// var stocksApi = builder.AddProject<Projects.Shoppiness_StocksService>("api-stocks")
//     .WithReference(stocksPgDb)
//     // .WithReference(serviceBus)
//     .WithHttpEndpoint(port: 5200, name: "http");


// Products API
var productsApi = builder.AddProject<Projects.Shoppiness_ProductsService>("api-products")
    .WithReference(productsPgDb)
    // .WithReference(stocksApi)
    // .WithReference(serviceBus)
    .WithHttpEndpoint(port: 5100, name: "http");

var keycloak = builder.AddKeycloak("keycloak", 8080)
    .WithDataVolume("keycloak-data");

var jaeger = builder.AddContainer("jaeger", "jaegertracing/jaeger:latest")
    .WithHttpEndpoint(
        port: 16686,
        targetPort: 16686,
        name: "ui")
    .WithEndpoint(
        targetPort: 4317,
        name: "otlp",
        scheme: "http",
        isProxied: false);

// API Gateway
builder.AddProject<Projects.Gateway_Api>("gateway-api")
    .WithReference(keycloak)
    // .WithReference(stocksApi)
    .WithReference(productsApi)
    .WaitFor(keycloak)
    // .WaitFor(stocksApi)
    .WaitFor(productsApi)
    .WithHttpsEndpoint(5001)
    .WithHttpEndpoint(5000)
    .WithExternalHttpEndpoints() // Mark only the gateway API as the resource to be exposed externally
    .WithEnvironment("JAEGER_OTLP_ENDPOINT", jaeger.GetEndpoint("otlp"));





builder.Build().Run();
