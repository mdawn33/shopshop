using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using Gateway.Api;
using Gateway.Api.Extensions;
using Gateway.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);



// Forces .NET HttpClient to allow unencrypted HTTP/2 gRPC calls to Jaeger v2
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);


// Add Aspire service discovery functionality
builder.AddServiceDefaults();


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApiWithAuth(builder.Configuration);

// Do I need this???????????????
builder.Services.AddDataProtection();

// Register authentication services
builder.Services.AddAuthenticationSchemes(builder);

// Add authorization and policies
builder.Services.AddAuthorization(options =>
{
    // options.AddPolicy("Admin", policy => policy.RequireClaim("role", "admin"));
    options.AddPolicy("authentication_required",
        policy => policy.RequireAuthenticatedUser().RequireClaim("api-access", true.ToString()));
});


// Add a scoped service to handle token refresh. The service is scoped to handle concurrent requests
builder.Services.AddScoped<TokenRefreshService>();

// Configure Anti forgery to look for the XSRF-TOKEN header
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN"; 
});

builder.Services.AddHttpClient();

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        if (builderContext.Route.RouteId == "angular-spa-fallback")
        {
            return;
        }
        
        // RequestTransforms intercept requests before YARP forwards them downstream.
        builderContext.AddRequestTransform(async transformContext =>
        {
            // Upstream clients (mobile apps, other services): Bearer token already present — forward as-is.
            var incoming = transformContext.HttpContext.Request.Headers.Authorization.FirstOrDefault();
            if (incoming?.StartsWith("Bearer ") == true)
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    AuthenticationHeaderValue.Parse(incoming);
                return;
            }

            // SPA clients: read the access token from the session cookie, refreshing it if needed.
            var tokenService = transformContext.HttpContext.RequestServices
                .GetRequiredService<TokenRefreshService>();
            var validAccessToken = await tokenService.GetValidTokenAsync(transformContext.HttpContext);
            if (validAccessToken != null)
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", validAccessToken);
            }
        });
    })
    .AddServiceDiscoveryDestinationResolver();

// builder.Services.AddCors(options =>
// {
//     options.AddPolicy("AngularDevPolicy", policy =>
//     {
//         policy.WithOrigins("http://localhost:4200")
//             .AllowAnyHeader()
//             .AllowAnyMethod()
//             .AllowCredentials();
//     });
// });


var app = builder.Build();

//// Middleware configuration

// UseRouting is added automatically 


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

// app.UseCors("AngularDevPolicy");

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAntiforgery();
app.UseAuthorization();

app.MapEndpoints();

app.MapReverseProxy();


// TODO: Include this? What happens if I remove it?
// Fallback to serving the Angular index.html
// GET and HEAD requests
// https://andrewlock.net/adding-metadata-to-fallback-endpoints-in-aspnetcore/
app.MapFallbackToFile("index.html"); 

app.Run();