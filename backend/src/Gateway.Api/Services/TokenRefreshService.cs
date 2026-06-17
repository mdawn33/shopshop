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
        
        if (DateTime.TryParse(expiresAtStr, null, DateTimeStyles.RoundtripKind, out var expiresAt))
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
        var tokenEndpoint = _configuration["Oidc:TokenEndpoint"];

        var requestBody = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken },
            { "client_id", _configuration["Oidc:ClientId"]! },
            { "client_secret", _configuration["Oidc:ClientSecret"]! }
        };

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(requestBody));
        if (!response.IsSuccessStatusCode) return null;

        var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();
        if (tokenResponse == null) return null;

        // 3. Calculate new expiration datetime string
        var newExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn).ToString("o");

        // 4. Update the values inside the authentication cookie context
        var authResult = await context.AuthenticateAsync();
        if (authResult.Succeeded && authResult.Properties != null)
        {
            authResult.Properties.UpdateTokenValue("access_token", tokenResponse.AccessToken);
            authResult.Properties.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken);
            authResult.Properties.UpdateTokenValue("expires_at", newExpiresAt);

            // Re-sign and write the updated cookie back to the response headers
            await context.SignInAsync(authResult.Principal, authResult.Properties);
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
