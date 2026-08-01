using Gateway.Api.Helpers;
using Gateway.Api.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.JsonWebTokens;

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
            
            // Trigger the OIDC challenge
            return Results.Challenge(
                properties: new AuthenticationProperties { RedirectUri = path },
                authenticationSchemes: [OpenIdConnectDefaults.AuthenticationScheme]
            );
        });
        
        routes.MapGet("/bff/logout", (HttpContext context, string? redirectUrl) =>
        {
            // TODO: Check if there's an error when the user is not authenticated or cookie is not provided and this endpoint is called
            
            var path = !string.IsNullOrEmpty(redirectUrl) && UrlHelpers.IsLocalUrl(redirectUrl)
                ? redirectUrl
                : "/";
            
            // Clear both the local BFF session cookie AND the Identity Provider session (OIDC scheme)
            return Results.SignOut(
                properties: new AuthenticationProperties { RedirectUri = path },
                authenticationSchemes:
                [
                    CookieAuthenticationDefaults.AuthenticationScheme, 
                    OpenIdConnectDefaults.AuthenticationScheme
                ]
            );
        });
        

        routes.MapGet("/bff/user", async (IAntiforgery antiforgery, HttpContext context, TokenRefreshService tokenRefreshService) =>
        {
            // The cookie identity's claims are deliberately stripped in OnTicketReceived
            // (AuthenticationExtension.cs) to keep the auth cookie small, so claims can no longer
            // be read from context.User.Claims. GetValidTokenAsync reads the access token instead
            // (same call path the YARP request transform uses), proactively refreshing it if it's
            // within 30 seconds of expiry. It returns null when there's no session or the refresh
            // attempt failed.
            var accessToken = await tokenRefreshService.GetValidTokenAsync(context);
            if (accessToken is null)
            {
                return Results.Unauthorized();
            }

            // This sets a non-HttpOnly cookie that JavaScript CAN read to extract the token string.
            // If the token expires mid-session, mutating requests will fail with a 400 Bad Request until something re-triggers /bff/user
            // This can be handled by moving the token generation and cookie setting to a middleware that runs on every request. In this case I should be careful to not overwrite the cookie if it already exists and to identify the requests that should trigger the cookie update.
            var tokens = antiforgery.GetAndStoreTokens(context);
            context.Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!, new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            
            // At this point context.User.Claims is empty because we stripped IdP claims from the cookie to avoid 400 Bad Request - Request header or cookie too long error.
            
            // Parse (not fully re-validate) the access token to extract its claims: it was already
            // validated at OIDC sign-in / previous refresh cycles by the OIDC handler, and only
            // ever lives in an HttpOnly, Secure, SameSite=Strict cookie server-side (design.md D5).
            var accessTokenJwt = new JsonWebTokenHandler().ReadJsonWebToken(accessToken);

            // Explicit allow-list: user id, email, display name, and role claims. Preserves the
            // existing { Type, Value }[] shape the frontend expects and parses as Claim[].
            // Note: this is a raw, unvalidated parse (ReadJsonWebToken), not a ClaimsPrincipal
            // built via full token validation, so ASP.NET's inbound claim type mapping (which
            // would normally map "role" -> ClaimTypes.Role) never runs. Keycloak's "User Realm
            // Role" protocol mapper on the roles-only client scope emits role claims with the
            // literal Type "role", so that's what must be matched here (same literal already
            // relied on by the "Admin"/"User" authorization policies in Program.cs).
            var claims = accessTokenJwt.Claims
                .Where(c => c.Type is JwtRegisteredClaimNames.Sub
                    or JwtRegisteredClaimNames.Email
                    or JwtRegisteredClaimNames.PreferredUsername
                    or JwtRegisteredClaimNames.Name
                    or "role")
                .Select(c => new { c.Type, c.Value });

            return Results.Ok(claims);
        });

        routes.MapGet("/bff/register", () =>
        {
            var properties = new AuthenticationProperties { RedirectUri = "/" };
            // This line instructs keycloak to skip the login screen and directly show the signup screen
            properties.Parameters.Add("prompt", "register");
            
            return Results.Challenge(properties);
        });


        // routes.MapPost("/bff/refresh", async (HttpContext context, TokenRefreshService tokenRefreshService) =>
        // {
        //     var token = await tokenRefreshService.GetValidTokenAsync(context);
        //     return token is not null ? Results.Ok() : Results.Unauthorized();
        // })
        // .RequireAuthorization();

    }
}

