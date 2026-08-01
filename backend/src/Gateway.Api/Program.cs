using System.Net.Http.Headers;
using Gateway.Api;
using Gateway.Api.Extensions;
using Gateway.Api.Helpers;
using Gateway.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
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
// This registers the services that protect cryptographically sensitive information, such as session tokens, anti-forgery tokens and authentication cookies
// By default, keys are stored in the local file system, so when having multiple instances it must be configured properly to use either Cloud Storage, Shared File System, or a DB.
builder.Services.AddDataProtection();
// builder.Services.AddDataProtection()
//     .SetApplicationName("MyCloudApp")
//     .PersistKeysToAzureBlobStorage(new Uri("https://windows.net"), new DefaultAzureCredential())
//     .ProtectKeysWithAzureKeyVault(new Uri("https://azure.net"), new DefaultAzureCredential());

// Register authentication services
builder.Services.AddAuthenticationSchemes(builder);

// Add authorization and policies
// Check required policies
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("authentication_required", policy => policy.RequireAuthenticatedUser().RequireClaim("api-access", true.ToString()))
    .AddPolicy("Admin", policy => policy.RequireAuthenticatedUser().RequireClaim("role", "admin"))
    .AddPolicy("User", policy => policy.RequireAuthenticatedUser().RequireClaim("role", "user"));


// Add a scoped service to handle token refresh. The service is scoped to handle concurrent requests
builder.Services.AddScoped<TokenRefreshService>();

// Configure Anti forgery to look for the XSRF-TOKEN header
builder.Services.AddAntiforgery(options =>
{
    // Angular reads this header name
    options.HeaderName = "X-XSRF-TOKEN"; 
    
    // set the Antiforgery tracking cookie
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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
            if (transformContext.HttpContext.Request.HasBearerToken(out var authHeader))
            {
                transformContext.ProxyRequest.Headers.Authorization =
                    AuthenticationHeaderValue.Parse(authHeader!);
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
    .AddServiceDiscoveryDestinationResolver(); // Resolve downstream services discovery

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

// Cors will be required when the SPA is published in a different domain
// For dev purposes, the site is proxied by YARP
// app.UseCors("AngularDevPolicy");

app.UseHttpsRedirection();

app.UseRouting();

// Set the HttpContext.User property
app.UseAuthentication();

// Run the authorization middleware
app.UseAuthorization();

// Middleware to strictly block unauthorized state-mutations BEFORE YARP
//
// Task 2.7 confirmation: this is registered globally via app.Use(...) (not scoped to a
// route group ahead of MapReverseProxy()). This is safe for Gateway-native endpoints in
// Endpoints.cs because every one of them (/bff/login, /bff/logout, /bff/user,
// /bff/register, /api/antiforgery/token) is a GET handler — the isMutating check below is
// false for all of them, so they hit the early `await next(context); return;` path below
// without ever calling into IAntiforgery. No mutating Gateway-native endpoints exist yet;
// if any are added later, they will need a valid antiforgery token (or a Bearer token) like
// any other mutating request, which is the intended behavior.
app.Use(async (context, next) =>
{
    // No need to validate antiforgery token for API requests
    if (context.Request.HasBearerToken(out _))
    { 
        await next(context);
        return;
    }
    
    
    var method = context.Request.Method;
    var isMutating = HttpMethods.IsPost(method) ||
                     HttpMethods.IsPut(method) ||
                     HttpMethods.IsDelete(method) ||
                     HttpMethods.IsPatch(method);
    
    // only validate antiforgery token for state-mutations and requests from the web client
    if (isMutating)
    {
        var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
        try
        {
            // Validates the X-XSRF-TOKEN header matches the encrypted cookie token
            await antiforgery.ValidateRequestAsync(context);
        }
        catch (AntiforgeryValidationException e)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Antiforgery token validation failed.");
            return; // Stops execution before YARP intercepts the request
        }
    }
    
    await next(context);
    
});

// Goes after Routing middleware registration
// Does not short-circuit the request pipeline
// Must run after the user is Authenticated
app.UseAntiforgery();



app.MapEndpoints();

// Do I need an entry point for the requests that go directly to the DS services?


app.MapReverseProxy();


// TODO: Include this? What happens if I remove it?
// Fallback to serving the Angular index.html
// GET and HEAD requests
// https://andrewlock.net/adding-metadata-to-fallback-endpoints-in-aspnetcore/
app.MapFallbackToFile("index.html"); 

app.Run();