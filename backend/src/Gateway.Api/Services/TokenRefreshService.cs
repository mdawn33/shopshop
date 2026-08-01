using Microsoft.AspNetCore.Authentication.Cookies;

namespace Gateway.Api.Services;

// Services/TokenRefreshService.cs
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;

public class TokenRefreshService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public TokenRefreshService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string?> GetValidTokenAsync(HttpContext context)
    {
        // 1. Extract expiration and check if token is still valid
        var expiresAtStr = await context.GetTokenAsync("expires_at");
        
        if (DateTime.TryParse(expiresAtStr, null, DateTimeStyles.AdjustToUniversal, out var expiresAt))
        {
            // If token is valid for more than 30 seconds, return it immediately
            if (expiresAt > DateTime.UtcNow.AddSeconds(30))
            {
                return await context.GetTokenAsync("access_token");
            }
        }

        // 2. Token is expired or expiring soon. Attempt to refresh.
        var refreshToken = await context.GetTokenAsync("refresh_token");
        if (string.IsNullOrEmpty(refreshToken)) return null;

        return await RefreshTokensAsync(context, refreshToken);
    }

    private async Task<string?> RefreshTokensAsync(HttpContext context, string refreshToken)
    {
        var client = _httpClientFactory.CreateClient();
        
        // Keycloak token endpoint URL (e.g., https://keycloak/realms/myrealm/protocol/openid-connect/token)
        var tokenEndpoint = _configuration["Keycloak:TokenUrl"];

        var requestBody = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", _configuration["Authentication:OpenIdConnect:ClientId"]! },
            // { "client_secret", _configuration["Authentication:ClientSecret"]! }
        };

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestBody));
        if (!response.IsSuccessStatusCode) return null;

        // Is the refresh token renewed?
        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();
        if (tokenResponse == null) return null;

        // 3. Calculate new expiration datetime string
        var newExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o");

        // 4. Update the values inside the authentication cookie context
        var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        if (authResult is { Succeeded: true, Properties: not null })
        {
            // Remove old tokens
            authResult.Properties.StoreTokens([
                new AuthenticationToken { Name = "access_token", Value = tokenResponse.AccessToken },
                new AuthenticationToken { Name = "refresh_token", Value = tokenResponse.RefreshToken },
                new AuthenticationToken { Name = "expires_at", Value = newExpiresAt }
            ]);
            
            // authResult.Properties.UpdateTokenValue("access_token", tokenResponse.AccessToken);
            // authResult.Properties.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken);
            // authResult.Properties.UpdateTokenValue("expires_at", newExpiresAt);

            // 6. Persist changes back into the stateless browser cookie on-the-fly
            // Re-sign and write the updated cookie back to the response headers
            // This line registers a callback on the response pipeline to write the updated cookie
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, authResult.Principal, authResult.Properties);
        }

        return tokenResponse.AccessToken;
    }
}

public class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")] public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = string.Empty;
    [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
}
