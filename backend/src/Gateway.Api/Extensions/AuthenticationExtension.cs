using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using Microsoft.OpenApi;

namespace Gateway.Api.Extensions;

public static class AuthenticationExtension
{

    public static IServiceCollection AddAuthenticationSchemes(this IServiceCollection services, WebApplicationBuilder builder)
    {
        
        // Do I need a Cookie Authentication scheme?
        services.AddAuthentication(options =>
            {
                // Specify Cookies for the UI to get the user ID. 
                // Invoked by the Authorization middleware to verify there's an active session cookie on incoming requests
                options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                
                // Explicitly tell OIDC to sign the user into the Cookie scheme
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // Define the schema to use when the user is not authenticated.
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "__Host-Shoppiness_bff";
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                // options.SlidingExpiration = true;
                
                options.Events.OnRedirectToLogin = context =>
                {
                    if (context.Request.Path.StartsWithSegments("/api"))
                    {
                        context.Response.StatusCode = 401;
                    }
                    else
                    {
                        context.Response.Redirect(context.RedirectUri);
                    }
                    return Task.CompletedTask;
                };

                options.Events.OnRedirectToAccessDenied = context =>
                {
                    context.Response.StatusCode = 403;
                    return Task.CompletedTask;
                };
            })
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                var oidcConfig = builder.Configuration.GetSection("Authentication:OpenIdConnect");
                
                options.Authority = oidcConfig["Authority"];
                options.ClientId = oidcConfig["ClientId"];
                // options.ClientSecret = oidcConfig["ClientSecret"];
                
                // ??????????????????????????
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                
                // options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                options.SaveTokens = true; // Saves access and refresh tokens inside the cookie:w
                
                options.GetClaimsFromUserInfoEndpoint = true; // Get claims from the UserInfo endpoint in case it requires additional claims:w
                options.UsePkce= true;
                options.CallbackPath = "/signin-oidc";
                // options.MapInboundClaims = false;
                options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                // options.TokenValidationParameters.RoleClaimType = "roles";
                
                
                // Use the below code in case keycloak returns claims in a format different from JSON
                // options.Events = new OpenIdConnectEvents
                // {
                //     OnTokenValidated = context =>
                //     {
                //         var identity = context.Principal?.Identity as ClaimsIdentity;
                //         if (identity != null)
                //         {
                //             // Example: Hardcode a custom claim or extract nested Keycloak roles
                //             identity.AddClaim(new Claim("custom_bff_claim", "hello-world"));
                //         }
                //         return Task.CompletedTask;
                //     }
                // };
                
                // Redirect back to the web app after login
                // This is required if the app is hosted on a different domain than the identity provider
                // options.Events = new OpenIdConnectEvents
                // {
                //     OnRedirectToIdentityProvider = context =>
                //     {
                //         context.Properties.RedirectUri = "http://localhost:4200";
                //         return Task.CompletedTask;
                //     }
                // };

            });
            // .AddKeycloakJwtBearer(
            //     serviceName: "keycloak",
            //     realm: "shoppinessrealm",
            //     options =>
            //     {
            //         options.Audience = builder.Configuration["Authentication:Audience"];
            //         options.MetadataAddress = builder.Configuration["Authentication:MetadataAddress"]!;
            //         options.TokenValidationParameters = new TokenValidationParameters
            //         {
            //             ValidateAudience = true,
            //             ValidIssuer = builder.Configuration["Authentication:ValidIssuer"]
            //         };
            //
            //         // For development only - disable HTTPS metadata validation
            //         // In production, use explicit Authority configuration instead
            //         if (builder.Environment.IsDevelopment())
            //         {
            //             options.RequireHttpsMetadata = false;
            //         }
            //     });
            
        
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
