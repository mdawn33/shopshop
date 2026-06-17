using System.Net.Http.Headers;
using Gateway.Api;
using Gateway.Api.Extensions;
using Gateway.Api.Services;
using Yarp.ReverseProxy.Transforms;

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

// Add authorization and policies
builder.Services.AddAuthorization(options =>
{
    // options.AddPolicy("Admin", policy => policy.RequireClaim("role", "admin"));
    options.AddPolicy("authentication_required", policy => policy.RequireAuthenticatedUser().RequireClaim("api-access", true.ToString()));
});


// What about token refresh?????????????????

// Add YARP
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    // .AddTransforms(builderContext =>
    // {
    //     builderContext.AddRequestTransform(async transformContext =>
    //     {
    //         // Resolve the Scoped service from the current request container
    //         var tokenService = transformContext.HttpContext.RequestServices.GetRequiredService<TokenRefreshService>();
    //         
    //         // This handles the expiration check and background update automatically
    //         var validAccessToken = await tokenService.GetValidTokenAsync(transformContext.HttpContext);
    //         // var accessToken = await transformContext.HttpContext.GetTokenAsync("access_token");
    //         
    //         if (validAccessToken != null)
    //         {
    //             transformContext.ProxyRequest.Headers.Authorization =
    //                 new AuthenticationHeaderValue("Bearer", validAccessToken);
    //         }
    //     });
    // })
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevPolicy", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


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

app.UseCors("AngularDevPolicy");

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapEndpoints();

app.MapReverseProxy();


// TODO: Include this? What happens if I remove it?
// Fallback to serving the Angular index.html
// GET and HEAD requests
// https://andrewlock.net/adding-metadata-to-fallback-endpoints-in-aspnetcore/
app.MapFallbackToFile("index.html"); 

app.Run();