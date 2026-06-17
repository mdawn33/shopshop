using Gateway.Api.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace Gateway.Api;

public static class Endpoints
{
    
    public static void MapEndpoints(this IEndpointRouteBuilder routes)
    {

        // Passes control to the OIDC middleware for authentication
        // The middleware will redirect the user to the Identity Provider login screen
        // After successful login, the IdP sends a authorization code back to the BFF callback path (/signin-oidc)
        // Then, the BFF exchanges this code for tokens, encrypts those tokens inside the __Host cookie and returns the cookie to the browser, while completing the final redirect back to the Angular app path that was originally requested (/dashboard)
        // What happens if this is called but the user is already authenticated?
        routes.MapGet("/bff/login", async (string? returnUrl) =>
        {
            var path = !string.IsNullOrEmpty(returnUrl) && UrlHelpers.IsLocalUrl(returnUrl)
                ? returnUrl
                : "/";

            // var frontendOrigin = config["BFF:FrontendOrigin"] ?? string.Empty;
            // var redirectUri = $"{frontendOrigin}{path}";
            //
            // var properties = new AuthenticationProperties 
            // { 
            //     RedirectUri = "http://localhost:4200" 
            // };

            // await context.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, properties);
            
            // Trigger the OIDC challenge
            return Results.Challenge(
                properties: new AuthenticationProperties { RedirectUri = returnUrl },
                authenticationSchemes: [OpenIdConnectDefaults.AuthenticationScheme]
            );
        });
        
        routes.MapGet("/bff/logout", (HttpContext context) =>
        {
            // Clear both the local BFF session cookie AND the Identity Provider session
            return Results.SignOut(
                properties: new AuthenticationProperties { RedirectUri = "/" },
                authenticationSchemes: new[] 
                { 
                    CookieAuthenticationDefaults.AuthenticationScheme, 
                    OpenIdConnectDefaults.AuthenticationScheme 
                }
            );
        });

        routes.MapGet("/bff/user", (HttpContext context) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
                return Results.Unauthorized();

            return Results.Ok(context.User.Claims.Select(c => new { c.Type, c.Value }));
        });

        routes.MapGet("/bff/register", () =>
        {
            var properties = new AuthenticationProperties { RedirectUri = "/" };
            // This line instructs keycloak to skip the login screen and directly show the signup screen
            properties.Parameters.Add("prompt", "register");
            
            
            return Results.Challenge(properties);
        });

    }
}

