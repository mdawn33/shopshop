## Context

The Gateway.Api project is the single entry point for the Angular web client. It uses YARP to reverse-proxy requests to downstream services and had a partially-configured authentication setup prior to this change. The project already referenced Keycloak and YARP packages.

The `frontend-auth` change established that the Angular client:
- Never handles tokens in JavaScript
- Sends every request with `withCredentials: true`
- Expects the BFF to manage an HttpOnly encrypted cookie after Keycloak authentication

Downstream services (ProductService, StocksService, etc.) expect an `Authorization: Bearer` JWT header — they have no concept of browser cookies.

What this change added to Gateway.Api:
1. A full three-scheme authentication stack (Cookie + OpenIdConnect + JwtBearer) plus a "smart" policy scheme dispatcher
2. `TokenRefreshService` for proactive access-token renewal within the YARP pipeline
3. An inline YARP transform that injects `Authorization: Bearer` on every proxied request
4. Antiforgery middleware with a custom `X-XSRF-TOKEN` header
5. Explicit Data Protection registration and a named authorization policy

## Goals / Non-Goals

**Goals:**
- Register Cookie authentication that issues an ASP.NET Core encrypted cookie storing Keycloak tokens via `SaveTokens = true` (tokens in `AuthenticationProperties`, no claims in cookie identity)
- Integrate Keycloak via OIDC Authorization Code + PKCE so Keycloak manages the login UI and token issuance
- Dispatch authentication to JwtBearer for machine-to-machine callers (those sending `Authorization: Bearer`) and to Cookie for SPA browser sessions — via a "smart" policy scheme
- Proactively refresh expired access tokens inside the YARP pipeline via `TokenRefreshService` before forwarding to downstream services
- Configure antiforgery protection with `X-XSRF-TOKEN` custom header
- Add `AddAuthentication().AddKeycloakJwtBearer(...)` to ProductService so it can independently validate forwarded bearer tokens

**Non-Goals:**
- ROPC / Direct Access Grant (`grant_type=password`) form-based login — OIDC redirect is used instead
- User self-registration via the Keycloak Admin REST API — Keycloak's self-registration UI handles this
- Five manual BFF auth endpoints (`/auth/login`, `/auth/register`, `/auth/logout`, `/auth/refresh`, `/auth/user`) — OIDC redirect handles authentication; `/signin-oidc` and `/signout-callback-oidc` are the only auth-related entry points
- CORS policy — not implemented in this change; deferred
- Password reset, email verification, or multi-factor authentication
- Social login / external identity providers
- Multi-tab / cross-device session synchronization
- Test suite (separate change)
- Swagger/OpenAPI document changes

## Decisions

### D1: BFF issues HttpOnly cookies; downstream services receive Bearer token

The Gateway is the sole party that holds Keycloak tokens securely. It stores the access token and refresh token inside an ASP.NET Core authentication cookie (encrypted via Data Protection, using `SaveTokens = true` on the OIDC options). Downstream services never see or set cookies — they receive a forwarded `Authorization: Bearer <access_token>` injected by the YARP transform.

**Rationale:** This enforces a single security boundary. JavaScript in the browser can never read the token (`HttpOnly`). Downstream services remain stateless and independently verifiable with a standard JwtBearer validator.

**Alternative considered:** Store the token in Redis with a session ID cookie. Rejected — adds infrastructure dependency and latency for every proxied request. ASP.NET Core Data Protection encryption provides equivalent security without a network hop.

### D2: "Smart" policy scheme dispatches between Cookie and JwtBearer

`AddAuthentication` sets `DefaultAuthenticateScheme = "smart"` (a policy scheme), `DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme`, and `DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme`. The "smart" scheme inspects the `Authorization` request header: if it starts with `"Bearer "`, it forwards to `JwtBearerDefaults.AuthenticationScheme`; otherwise it forwards to `CookieAuthenticationDefaults.AuthenticationScheme`. This means three schemes are registered: Cookie (browser sessions), OpenIdConnect (OIDC redirect), and JwtBearer (machine-to-machine).

**Rationale:** A single Gateway entry point must serve both browser clients (cookie-based) and potential machine-to-machine callers (bearer-based) without requiring separate ports or path prefixes. The policy scheme makes the dispatch transparent to the rest of the pipeline.

**Alternative considered:** Separate the BFF and API gateway responsibilities into two distinct ASP.NET Core applications. Rejected — adds operational complexity for this scale.

### D3: OIDC Authorization Code + PKCE for authentication

Authentication uses `AddOpenIdConnect` with `ResponseType = Code`, `UsePkce = true`, and no client secret — this is a public PKCE client. Keycloak issues tokens; the OIDC middleware exchanges the authorization code and stores the resulting tokens via `SaveTokens = true`. The scopes requested are `["openid", "roles-only", "offline_access"]` — `offline_access` enables refresh tokens.

**Rationale:** OIDC Authorization Code + PKCE is the recommended OAuth 2.1 flow for confidential web applications. It delegates the login UI to Keycloak (so the BFF never handles raw credentials), supports MFA transparently, and aligns with current security best practices. ROPC (Direct Access Grant) was the originally considered alternative but is deprecated in OAuth 2.1 and requires the BFF to handle user passwords directly.

**Alternative considered:** ROPC (`grant_type=password`). Rejected — deprecated in OAuth 2.1; couples the BFF to credential handling; Keycloak explicitly discourages it for new clients.

### D4: Tokens stored in cookie Properties via SaveTokens; cookie identity carries no claims

`options.SaveTokens = true` on the OIDC handler instructs ASP.NET Core to store `access_token`, `refresh_token`, and `expires_at` in the `AuthenticationProperties` of the cookie ticket. In `OnTicketReceived`, `id_token` and `token_type` are removed from Properties, and all claims are removed from the cookie identity. Tokens are subsequently read via `context.GetTokenAsync("access_token")`, `GetTokenAsync("refresh_token")`, and `GetTokenAsync("expires_at")`.

**Rationale:** Storing tokens in Properties (not as claims) keeps the cookie identity lean — no sensitive token strings live in the claims principal. `GetTokenAsync` is the idiomatic ASP.NET Core API for reading saved tokens. Stripping claims from the cookie identity avoids leaking Keycloak claim names into the cookie payload and prevents stale claim data after a token refresh.

**Alternative considered:** Store `access_token` and `refresh_token` as custom claims in the `ClaimsPrincipal` (as originally specified). Rejected — the cookie grows unnecessarily large and the claims principal contains sensitive raw token strings.

### D5: Inline YARP transform lambda + TokenRefreshService

The YARP transform is registered as an inline lambda via `builder.Services.AddReverseProxy().LoadFromConfig(...).AddTransforms(builderContext => { ... })`. The `angular-spa-fallback` route is skipped. For each other route, a `RequestTransform` is added that: (1) passes through any existing `Authorization: Bearer` header from upstream API callers, or (2) calls `TokenRefreshService.GetValidTokenAsync(context)` for SPA browser sessions and sets the resulting token as the `Authorization: Bearer` header. `TokenRefreshService` reads `expires_at` from the cookie Properties, returns the current `access_token` if it has more than 30 seconds remaining, or performs a `grant_type=refresh_token` POST to Keycloak and updates the cookie via `SignInAsync` if the token is expired or expiring soon.

**Rationale:** An inline lambda is simpler than a separate `ITransformProvider` class for this use case — the logic is self-contained and registered in one place. `TokenRefreshService` as a scoped dependency keeps the refresh logic testable and separates it from the transform registration. Proactive refresh inside the YARP pipeline prevents downstream services from receiving an already-expired token.

**Alternative considered:** `ITransformProvider` class (`BffTokenForwardingTransformProvider`). Rejected — the extra class adds indirection without benefit; the inline lambda is clearer and sufficient. Alternative: let downstream 401s trigger a separate `/auth/refresh` call from Angular. Rejected — adds a round-trip latency penalty for every request made just after token expiry.

### D6: Tokens stored in cookie Properties via SaveTokens; cookie identity carries no claims

See D4 — this decision covers both the storage mechanism and the access pattern. `GetTokenAsync("access_token")`, `GetTokenAsync("refresh_token")`, and `GetTokenAsync("expires_at")` are the only supported APIs for reading tokens from the cookie.

### D7: Cookie security settings

| Setting | Value | Rationale |
|---------|-------|-----------|
| `Cookie.Name` | `__Host-Shoppiness_bff` | `__Host-` prefix enforces `Secure`, `Path=/`, and no `Domain` at the browser level — strongest browser-enforced cookie binding |
| `HttpOnly` | `true` | Prevents JavaScript access |
| `SecurePolicy` | `Always` | Cookie only sent over HTTPS; `__Host-` prefix requires this |
| `SameSite` | `Strict` | Strongest CSRF protection; no cross-site requests carry the cookie, including top-level navigations from external links |
| `Path` | `/` (implied by `__Host-`) | Cookie sent on all BFF and YARP routes |
| `ExpireTimeSpan` | 10 minutes | Short-lived; access token refresh is handled proactively by `TokenRefreshService` inside YARP; no need for a long-lived session cookie |
| Sliding expiration | Disabled | Expiry is fixed; `TokenRefreshService` renews tokens on demand rather than sliding the cookie |

**Alternative considered:** `SameSite=Lax` with 60-minute sliding expiration (original spec). Rejected — `Strict` is safer because no cross-site requests ever carry the cookie. 10-minute expiry with proactive `TokenRefreshService` refresh is effectively transparent to the user.

## Component / Service Diagram

```
Angular (http://localhost:4200)
  │   withCredentials: true on every request
  │
  ▼
Gateway.Api (BFF)
  ├─ OIDC redirect flow (handled by ASP.NET Core OIDC middleware)
  │    /signin-oidc  ← Keycloak authorization code callback
  │    /signout-callback-oidc  ← Keycloak post-logout callback
  │
  ├─ "smart" policy scheme
  │    Authorization: Bearer  →  JwtBearerDefaults (machine-to-machine)
  │    (no header)            →  CookieDefaults    (SPA browser session)
  │
  ├─ YARP reverse proxy
  │    └─ AddTransforms (inline lambda)
  │         ├─ Skip: angular-spa-fallback route
  │         ├─ Pass-through: existing Authorization: Bearer header
  │         └─ SPA path: TokenRefreshService.GetValidTokenAsync()
  │              ├─ Token still valid → return access_token from Properties
  │              └─ Token expired/expiring → POST Keycloak /token (refresh_token)
  │                   └─ SignInAsync(Cookie, principal, updatedProperties)
  │                   └─ return new access_token
  │                        │
  │                        ▼
  │              inject Authorization: Bearer <access_token>
  │                        │
  │                        ▼
  │         ProductService / StocksService / PaymentsService
  │              └─ AddKeycloakJwtBearer validates token
  │
  └─ Antiforgery middleware (X-XSRF-TOKEN header)
```

## Data Flows

### OIDC Login Flow

```
Angular navigates to /signin (Keycloak redirect)
  → ASP.NET Core OIDC middleware redirects browser to Keycloak
    → User authenticates at Keycloak login page
      → Keycloak redirects to /signin-oidc?code=...
        → OIDC middleware exchanges code for tokens (PKCE)
          → SaveTokens=true: stores access_token, refresh_token, expires_at
             in AuthenticationProperties
          → OnTicketReceived: removes id_token, token_type, strips all claims
             from cookie identity
          → SignInAsync(Cookie) → sets HttpOnly __Host-Shoppiness_bff cookie
          → Redirects Angular to original destination
```

### YARP Proxied Request Flow (SPA Browser Session)

```
GET /products-api/products (from Angular, cookie included)
  → YARP pipeline
    → UseAuthentication() reads cookie → populates HttpContext.User
    → AddTransforms inline lambda
      → No Authorization: Bearer header present
      → TokenRefreshService.GetValidTokenAsync(context)
           → GetTokenAsync("expires_at") → parse expiry
           → If > 30s remaining: return GetTokenAsync("access_token")
           → If ≤ 30s remaining:
                → GetTokenAsync("refresh_token")
                → POST Keycloak:TokenUrl grant_type=refresh_token
                → SignInAsync(Cookie) with updated tokens in Properties
                → return new access_token
      → Sets Authorization: Bearer <access_token> on upstream request
    → Forward to http://products-api/products
      → ProductService validates JWT with AddKeycloakJwtBearer
      → Returns response
```

### YARP Proxied Request Flow (Machine-to-Machine)

```
GET /products-api/products (Authorization: Bearer <token>)
  → YARP pipeline
    → "smart" scheme → JwtBearer validates the incoming token
    → AddTransforms inline lambda
      → Authorization: Bearer header present → pass through unchanged
    → Forward to http://products-api/products
```

## BFF API Contract

There are no manual BFF auth endpoints. Authentication is handled entirely via the OIDC redirect flow managed by ASP.NET Core middleware:

| Callback Path | Registered By | Purpose |
|---------------|---------------|---------|
| `/signin-oidc` | OIDC middleware (`options.CallbackPath`) | Receives the authorization code from Keycloak; exchanges for tokens; sets the cookie |
| `/signout-callback-oidc` | OIDC middleware (`options.SignedOutCallbackPath`) | Receives the post-logout redirect from Keycloak; clears the cookie |

These paths are handled automatically by the OIDC middleware and are not manually registered endpoints. Angular initiates login by triggering a challenge (redirect to Keycloak) and logout via the OIDC logout flow.

## Files Changed

| File | Status | Notes |
|------|--------|-------|
| `Gateway.Api/Extensions/AuthenticationExtension.cs` | Modified | Register Cookie, OIDC (PKCE), JwtBearer, and "smart" policy scheme; configure `OnTicketReceived` claim stripping; set `__Host-Shoppiness_bff` cookie with `SameSite=Strict` and 10-minute expiry |
| `Gateway.Api/Services/TokenRefreshService.cs` | New | Scoped service; reads `expires_at` from cookie Properties; refreshes via `grant_type=refresh_token`; updates cookie via `SignInAsync` |
| `Gateway.Api/Program.cs` | Modified | Register `AddDataProtection`, `AddScoped<TokenRefreshService>`, `AddAntiforgery`; inline YARP `AddTransforms` lambda; `UseAntiforgery()` between `UseAuthentication()` and `UseAuthorization()`; `authentication_required` authorization policy |
| `Gateway.Api/appsettings.json` | Modified | OIDC configuration (`Authority`, `ClientId`, `Audience`); Keycloak token URL |
| `Gateway.Api/appsettings.Development.json` | Modified | Development OIDC/Keycloak settings |

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| OIDC redirect flow breaks SPA form-login UX — the user is redirected away from the Angular app to Keycloak's login page | Accepted trade-off. Keycloak supports customizable login themes. Using Keycloak's hosted login page is the recommended approach for security; it keeps credentials off the BFF. |
| `SameSite=Strict` prevents the cookie from being sent on the first top-level navigation from an external link (e.g., a link in an email) — the user sees a logged-out state on first load | Accepted trade-off. The Angular app should handle 401 responses by redirecting to the OIDC login flow, which re-establishes the session transparently. |
| 10-minute cookie expiry requires the frontend to handle 401s and trigger re-authentication if `TokenRefreshService` cannot refresh (e.g., refresh token itself expired) | Angular `ErrorInterceptor` should catch 401s from the Gateway and trigger the OIDC challenge. `offline_access` scope provides a long-lived refresh token, so this should be rare. |
| `offline_access` scope issues long-lived refresh tokens, which increases the window if a refresh token is compromised | Keycloak supports refresh token rotation. Revocation is possible via the Keycloak admin console. Monitor Keycloak's token revocation events. |
| Storing tokens in cookie `AuthenticationProperties` increases cookie size (JWTs are large) | ASP.NET Core Data Protection cookies have no hard browser 4 KB per-cookie limit concern in practice for typical Keycloak JWTs (1–2 KB). Monitor cookie size; if needed, store tokens server-side (Redis) keyed by a session ID cookie. |
