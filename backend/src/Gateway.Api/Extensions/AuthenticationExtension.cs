using System.Security.Claims;
using Gateway.Api.Helpers;
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
                // If the gateway receives a request from the browser, it should redirect to the OIDC provider's login page.
                // if the request is from a machine-to-machine client, it should return a 401 Unauthorized response.
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;

            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.Name = "__Host-Shoppiness_bff";
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
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
                
                options.SaveTokens = true; // Saves access and refresh tokens inside the cookie
                
                options.GetClaimsFromUserInfoEndpoint = false; // Get claims from the UserInfo endpoint in case it requires additional claims
                options.UsePkce= true;
                options.CallbackPath = "/signin-oidc";
                options.SignedOutCallbackPath = "/signout-callback-oidc";
                options.MapInboundClaims = false;  // Prevents WS-Federation claims conversion mapping boilerplate
                options.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                options.TokenValidationParameters.RoleClaimType = ClaimTypes.Role;
                
                
                // Use the below code in case keycloak returns claims in a format different from JSON
                options.Events = new OpenIdConnectEvents
                {
                    // Using this event to strip all the claims off the cookie's claim identity before it is even stored in the cookie
                    // This event runs after the IdP successfully authenticates the user but before the local cookie is created
                    OnTicketReceived = context =>
                    {
                        //https://dev.to/devin-rosario/fixing-request-header-or-cookie-too-large-nginx-error-48fp
                        
                        // Keycloak and other OIDC providers may return duplicated, unnecessary claims, which can cause the cookie to be too large. 
                        
                        // context.Properties?.Items.Remove(".Token.id_token"); // El ID token no lo necesito para el downstream pero si para el logout,
                        // para que Keycloak pueda reconocer el cliente y no muestre el diálogo para re-confirmar el cierre de sesion
                        // context.Properties?.Items.Remove(".Token.token_type");
                        
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            // 1. Define the critical claims that MUST stay
                            var claimsToKeep = new[] 
                            {
                                "name",                    // Matches your JwtRegisteredClaimNames.Name
                                "sub",                     // Unique identifier for Antiforgery validation
                                "role",                    // Keep if you use role-based authorization
                                ClaimTypes.NameIdentifier, // Standard identity fallback
                                ClaimTypes.Name
                            };

                            // 2. Identify claims that are safe to drop (like heavy metadata or the raw ID token)
                            var claimsToRemove = identity.Claims
                                .Where(c => !claimsToKeep.Contains(c.Type))
                                .ToList();

                            // 3. Remove only the non-essential claims
                            foreach (var claim in claimsToRemove)
                            {
                                identity.RemoveClaim(claim);
                            }
                            
                        }
                        
                        return Task.CompletedTask;
                    },
                    
                    // Challenge handler - By default, ASP.NET redirects to the login page. Following code issues a 401 Unauthorized instead for the SPA to handle.
                    // This is required if the app is hosted on a different domain than the identity provider ???
                    OnRedirectToIdentityProvider = context =>
                    {
                        // Example: If it's an API request, don't redirect to the external IdP. Return 401.
                        if (context.Request.Path.StartsWithSegments("/api-"))
                        {
                            // Stops the OIDC redirect engine entirely
                            context.HandleResponse(); 
                            // Return 401 Unauthorized
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            
                            // Optional: return a JSON error object
                            context.Response.ContentType = "application/json";
                            return context.Response.WriteAsJsonAsync(new { error = "Unauthorized", message = "API access requires valid tokens." });
                        }
                        
                        // Redirect to the OIDC provider's login page normally
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
                
                // Clears the default scopes and adds the required scopes
                // options.Scope.Clear();
                // options.Scope.Add("openid"); // for session management
                // options.Scope.Add("roles-only");
                
                
                // "profile"/"email" are the standard OIDC scopes that grant preferred_username,
                // name, and email claims on the access token. Without them, /bff/user's claims
                // fix (bff-user-claims, design.md D5) has no email/display-name claim to map,
                // since "roles-only" alone does not carry them. Added here so those claims exist
                // on the token; empirical verification against a live Keycloak instance is still
                // tracked separately (tasks.md 6.4).
                // options.Scope.Add("profile");
                // options.Scope.Add("email");
                
                
         
                // This code allows passing id_token_hint parameter during logout without prompting the user with an extra ""Are you sure you want to sign out?"" Keycloak confirmation screen
                // options.Events.OnRedirectToIdentityProviderForSignOut = context =>
                // {
                //     // Keycloak requires the original ID token to process a silent back-channel logout
                //     if (context.Properties.Items.TryGetValue(".Token.id_token", out var idToken))
                //     {
                //         context.ProtocolMessage.IdTokenHint = idToken;
                //     }
                //     return Task.CompletedTask;
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
                options.ForwardDefaultSelector = context => context.Request.HasBearerToken(out _)
                    ? JwtBearerDefaults.AuthenticationScheme
                    : CookieAuthenticationDefaults.AuthenticationScheme;
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
