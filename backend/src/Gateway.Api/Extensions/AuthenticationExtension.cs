using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

namespace Gateway.Api.Extensions;

public static class AuthenticationExtension
{

    public static IServiceCollection AddAuthenticationSchemes(this IServiceCollection services, WebApplicationBuilder builder)
    {
        
        // Do I need a Cookie Authentication scheme?
        services.AddAuthentication()
            .AddKeycloakJwtBearer(
                serviceName: "keycloak",
                realm: "shoppinessrealm",
                options =>
                {
                    options.Audience = builder.Configuration["Authentication:Audience"];
                    options.MetadataAddress = builder.Configuration["Authentication:MetadataAddress"]!;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidIssuer = builder.Configuration["Authentication:ValidIssuer"]
                    };

                    // For development only - disable HTTPS metadata validation
                    // In production, use explicit Authority configuration instead
                    if (builder.Environment.IsDevelopment())
                    {
                        options.RequireHttpsMetadata = false;
                    }
                });
            
        
        return services;
    }
    
    public static IServiceCollection AddOpenApiWithAuth(this IServiceCollection services, IConfiguration configuration)
    {
        
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                // Ensure instances exist
                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        
                // Add OAuth2 security scheme (Authorization Code flow only)
                document.Components.SecuritySchemes.Add("keycloak", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(configuration["Keycloak:AuthorizationUrl"]!),
                            TokenUrl = new Uri(configuration["Keycloak:TokenUrl"]!),
                            Scopes = new Dictionary<string, string>
                            {
                                { "openid", "Access the OpenID Connect user profile" },
                                { "profile", "Access the user's profile" }
                            }
                        }
                    }
                });

                // Apply security requirement globally
                document.Security = [
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecuritySchemeReference("keycloak"),
                            ["openid", "profile"]
                        }
                    }
                ];
        
                // Set the host document for all elements
                // including the security scheme references
                document.SetReferenceHostDocument();

                return Task.CompletedTask;
            });
        });
        
        return services;
    }
}
