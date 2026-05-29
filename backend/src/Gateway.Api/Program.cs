using System.Security.Claims;
using Gateway.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Forces .NET HttpClient to allow unencrypted HTTP/2 gRPC calls to Jaeger v2
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);


// Add service discovery functionality
builder.AddServiceDefaults();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApiWithAuth(builder.Configuration);


// Register authentication services
builder.Services.AddAuthenticationSchemes(builder);

// Add authorization
builder.Services.AddAuthorization(options =>
{
    // options.AddPolicy("Admin", policy => policy.RequireClaim("role", "admin"));
    options.AddPolicy("Default", policy => policy.RequireAuthenticatedUser());
});


// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();


// builder.Services.AddOpenTelemetry()
//     .ConfigureResource(resource => resource.AddService("gateway-api"))
//     .WithTracing(tracing =>
//     {
//         tracing.AddAspNetCoreInstrumentation()
//             .AddHttpClientInstrumentation();
//
//         tracing.AddOtlpExporter();
//     });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Gateway.Api v1");
        options.OAuthUsePkce();
    });
}

app.UseHttpsRedirection();

app.MapDefaultEndpoints();


// app.MapGet("users/me", (ClaimsPrincipal claimsPrincipal) =>
// {
//     return claimsPrincipal.Claims.ToDictionary(c => c.Type, c => c.Value);
// }).RequireAuthorization();


app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();