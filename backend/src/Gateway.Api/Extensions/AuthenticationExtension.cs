using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

using Microsoft.OpenApi;

namespace Gateway.Api.Extensions;

public static class AuthenticationExtension
{

    public static IServiceCollection AddAuthenticationSchemes(this IServiceCollection services, WebApplicationBuilder builder)
    {
        
        // Do I need a Cookie Authentication scheme? yes, this tells ASP.NET we have to look for a cookie in the requests to authenticate the user
        services.AddAuthentication(options =>
            {
                // "smart" policy scheme dispatches to JWT Bearer for machine-to-machine requests
                // (Authorization: Bearer <token>) and falls back to Cookie for SPA browser sessions.
                options.DefaultAuthenticateScheme = "smart";

                // Explicitly tell OIDC to sign the user into the Cookie scheme
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // Define the schema to use when the user is not authenticated.
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "__Host-Shoppiness_bff";
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
                // options.SlidingExpiration = true;
                
                // Challenge handler - OnRedirectToLogin event is skipped because we are using the OpenID Connect middleware to handle the challenge.
                // By default,.NET issues a 302 Redirect. We want to issue a 401 Unauthorized instead for the SPA to handle.
                // options.Events.OnRedirectToLogin = context =>
                // {
                //     context.Response.StatusCode = 401;
                //     return Task.CompletedTask;
                // };

                // Forbid handler - Handles Authorization issues (User is logged in but not authorized to access the resource)
                // By default, the middleware set the 403 Forbidden status
                // options.Events.OnRedirectToAccessDenied = context =>
                // {
                //     context.Response.StatusCode = 403;
                //     return Task.CompletedTask;
                // };
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
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.MapInboundClaims = false;  // Prevents WS-Federation claims conversion mapping boilerplate
                options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                // options.TokenValidationParameters.RoleClaimType = "roles";
                
                
                // Use the below code in case keycloak returns claims in a format different from JSON
                options.Events = new OpenIdConnectEvents
                {
                    OnTicketReceived = context =>
                    {
                        //https://dev.to/devin-rosario/fixing-request-header-or-cookie-too-large-nginx-error-48fp
                        
                        // Keycloak and other OIDC providers may return duplicated, unnecessary claims, which can cause the cookie to be too large. 
                        // Eliminamos metadatos basura del protocolo que ocupan mucho espacio
                        context.Properties.Items.Remove(".Token.id_token"); // El ID token no lo necesitas para el downstream
                        context.Properties.Items.Remove(".Token.token_type");

                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            // Vaciamos los claims de la identidad de la cookie. 
                            // YARP no los necesita porque leerá directamente el "access_token" guardado.
                            var claimsToRemove = identity.Claims.ToList();
                            foreach (var claim in claimsToRemove)
                            {
                                identity.RemoveClaim(claim);
                            }
                        }
                        
                        return Task.CompletedTask;
                    }
                    
                    // OnTokenValidated = context =>
                    // {
                    //     var identity = context.Principal?.Identity as ClaimsIdentity;
                    //     // Example: Hardcode a custom claim or extract nested Keycloak roles
                    //     identity?.AddClaim(new Claim("custom_bff_claim", "hello-world"));
                    //     return Task.CompletedTask;
                    // }
                };
                
                
                options.Scope.Clear();  
                options.Scope.Add("openid");
                options.Scope.Add("roles-only");
                options.Scope.Add("offline_access"); // Enforces issuing a refresh token ????????
                
                // Challenge handler - By default, ASP.NET redirects to the login page. We want to issue a 401 Unauthorized instead for the SPA to handle.
                // This is required if the app is hosted on a different domain than the identity provider
                // options.Events = new OpenIdConnectEvents
                // {
                //     OnRedirectToIdentityProvider = context =>
                //     {
                //         // Example: If it's an API request, don't redirect to the external IDP. Return 401.
                //         if (context.Request.Path.StartsWithSegments("/api"))
                //         {
                //             context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                //             context.HandleResponse(); // Stops the OIDC redirect engine entirely
                //         }
                //         
                //         // Example: Add custom query parameters to the external login URL
                //         // context.ProtocolMessage.Prompt = "login"; 
                //         
                //         return Task.CompletedTask;
                //     }
                // };

                // // Forbid handler - By default, the middleware set the 403 Forbidden status
                // options.Events.OnAccessDenied = context =>
                // {
                //     context.Response.StatusCode = StatusCodes.Status403Forbidden;
                //
                //     // Mark it as handled so the pipeline stops executing the default behavior
                //     context.HandleResponse(); 
                //     return Task.CompletedTask;
                // };
            })
            // JWT Bearer: validates tokens sent directly by upstream clients (mobile apps, other services)
            // using Authorization: Bearer <token>. Authority and Audience mirror the OIDC configuration.
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var oidcConfig = builder.Configuration.GetSection("Authentication:OpenIdConnect");
                options.Authority = oidcConfig["Authority"];
                options.Audience = builder.Configuration["Authentication:Audience"];
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
            })
            // Policy scheme: selects the concrete authentication scheme per request.
            // Bearer header → JWT Bearer (machine-to-machine). No Bearer header → Cookie (SPA session).
            .AddPolicyScheme("smart", "smart", options =>
            {
                options.ForwardDefaultSelector = context =>
                {
                    var auth = context.Request.Headers.Authorization.FirstOrDefault();
                    return auth?.StartsWith("Bearer ") == true
                        ? JwtBearerDefaults.AuthenticationScheme
                        : CookieAuthenticationDefaults.AuthenticationScheme;
                };
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
                                // { "openid", "Access the OpenID Connect user profile" },
                                // { "profile", "Access the user's profile" }
                                { "roles-only", "" }
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
                            ["roles-only"]
                            // ["openid", "profile"]
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
