## Why

The `frontend-auth` change delivers Angular components that send all auth requests to the Gateway with `withCredentials: true` and expect HttpOnly cookies in return — but the Gateway.Api had no cookie authentication scheme, no OIDC integration, and no mechanism to forward a bearer token to downstream services. Without this backend counterpart the Angular auth flow cannot function at all.

## What Changes

- Add Cookie authentication scheme to Gateway.Api so the BFF can issue and validate ASP.NET Core encrypted authentication cookies storing Keycloak tokens via `SaveTokens = true`
- Add OpenID Connect (Authorization Code + PKCE) scheme so Keycloak manages the login/logout redirect flow; no manual auth endpoints are needed
- Add a "smart" policy scheme that dispatches authentication to JwtBearer when an `Authorization: Bearer` header is present, and to Cookie otherwise — enabling both browser SPA clients and machine-to-machine callers on the same Gateway
- Add `TokenRefreshService` that proactively checks token expiry before each YARP proxy request and performs a `grant_type=refresh_token` exchange when the access token has 30 seconds or fewer remaining, then updates the cookie via `SignInAsync`
- Add an inline YARP request transform (registered via `AddTransforms` lambda) that reads the access token via `TokenRefreshService` for SPA sessions, or passes through an existing `Authorization: Bearer` header for upstream API callers
- Add antiforgery middleware with `X-XSRF-TOKEN` as the custom header name
- Add explicit `AddDataProtection()` registration to ensure cookie encryption is stable across restarts
- Add authorization policy `authentication_required` requiring both an authenticated user and an `api-access` claim

## Capabilities

### New Capabilities

- `bff-cookie-auth`: Cookie authentication scheme on Gateway.Api — issues and validates encrypted ASP.NET Core auth cookies. Tokens (`access_token`, `refresh_token`, `expires_at`) are stored in the cookie `AuthenticationProperties` via `SaveTokens = true`. The cookie identity carries no claims — all claims are stripped in `OnTicketReceived`. Cookie name is `__Host-Shoppiness_bff`, `SameSite=Strict`, `HttpOnly`, `Secure`, 10-minute expiry.
- `bff-token-forwarding`: Inline YARP `AddTransforms` lambda that skips the `angular-spa-fallback` route, passes through an existing `Authorization: Bearer` header for upstream callers, and falls back to `TokenRefreshService.GetValidTokenAsync()` for SPA browser sessions
- `bff-token-refresh`: `TokenRefreshService` — a scoped service that reads `expires_at` from the cookie properties, returns the current `access_token` if it is still valid, or POSTs to `Keycloak:TokenUrl` with `grant_type=refresh_token` and updates the cookie via `SignInAsync` if the token is expired or expiring within 30 seconds
- `downstream-jwt-validation`: JwtBearer validation in downstream services (ProductService as canonical example) so they can validate the bearer token forwarded by the Gateway

### Modified Capabilities

- `gateway-authentication`: `AuthenticationExtension.cs` updated to register three schemes (Cookie, OpenIdConnect, JwtBearer) plus the "smart" policy scheme dispatcher, replacing the previous single-scheme setup

## Impact

- **Modified file:** `backend/src/Gateway.Api/Extensions/AuthenticationExtension.cs` — register Cookie, OIDC (PKCE), JwtBearer, and "smart" policy scheme; configure OIDC options including `SaveTokens`, `GetClaimsFromUserInfoEndpoint`, `UsePkce`, scopes, and `OnTicketReceived` claim stripping
- **New file:** `backend/src/Gateway.Api/Services/TokenRefreshService.cs` — proactive token refresh service used by the YARP transform
- **Modified file:** `backend/src/Gateway.Api/Program.cs` — register `AddDataProtection`, `AddScoped<TokenRefreshService>`, `AddAntiforgery`, authorization policy; add inline YARP `AddTransforms` lambda; add `UseAntiforgery()` between `UseAuthentication()` and `UseAuthorization()`
- **Modified file:** `backend/src/Gateway.Api/appsettings.json` — OIDC configuration keys (`Authority`, `ClientId`, `CallbackPath`)
- **Modified file:** `backend/src/Gateway.Api/appsettings.Development.json` — development OIDC and Keycloak settings
