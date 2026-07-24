namespace Gateway.Api.Helpers;

public static class RequestHelpers
{
    public static bool HasBearerToken(this HttpRequest request, out string? authHeader)
    {
        authHeader = request.Headers.Authorization.FirstOrDefault();
        return authHeader?.StartsWith("Bearer ") == true;
    }
}