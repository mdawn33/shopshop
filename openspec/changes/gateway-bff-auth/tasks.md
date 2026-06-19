## 1. Configuration

- [x] 1.1 Add OIDC configuration keys (`Authority`, `ClientId`, `CallbackPath`) and `Keycloak:TokenUrl` to `Gateway.Api/appsettings.json`
- [x] 1.2 Add development values for OIDC/Keycloak settings to `Gateway.Api/appsettings.Development.json`
- [x] 1.3 Add `Authentication:Audience` key to `Gateway.Api/appsettings.json` for JwtBearer validation

## 2. Data Protection

- [x] 2.1 Register `builder.Services.AddDataProtection()` in `Gateway.Api/Program.cs` to ensure cookie encryption is stable across application restarts

## 3. Authentication Schemes

> Depends on: 1.1 (OIDC config keys must exist)

- [x] 3.1 In `Gateway.Api/Extensions/AuthenticationExtension.cs`, call `AddAuthentication(options => { ... })` with `DefaultAuthenticateScheme = "smart"`, `DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme`, `DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme`
- [x] 3.2 Chain `.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options => { ... })`: set `Cookie.Name = "__Host-Shoppiness_bff"`, `SameSite = SameSiteMode.Strict`, `SecurePolicy = CookieSecurePolicy.Always`, `HttpOnly = true`, `IsEssential = true`, `ExpireTimeSpan = TimeSpan.FromMinutes(10)`; do NOT enable sliding expiration
- [x] 3.3 In the `OnTicketReceived` handler: remove `id_token` and `token_type` from `context.Properties`; remove all claims from `context.Principal.Identity` so the cookie identity carries no claims
- [x] 3.4 Chain `.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options => { ... })`: set `Authority`, `ClientId` from config; `ResponseType = Code`; `ResponseMode = Query`; `UsePkce = true`; `SaveTokens = true`; `GetClaimsFromUserInfoEndpoint = true`; `MapInboundClaims = false`; `Scope = ["openid", "roles-only", "offline_access"]`; `CallbackPath = "/signin-oidc"`; `SignedOutCallbackPath = "/signout-callback-oidc"`; `TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name`; no `ClientSecret` (public PKCE client)
- [x] 3.5 Chain `.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options => { ... })`: set `Authority` and `Audience` from config; `RequireHttpsMetadata = !env.IsDevelopment()`
- [x] 3.6 Chain `.AddPolicyScheme("smart", "smart", options => { ... })`: in `ForwardDefaultSelector`, read `Authorization` header — if it starts with `"Bearer "` forward to `JwtBearerDefaults.AuthenticationScheme`, else forward to `CookieAuthenticationDefaults.AuthenticationScheme`

## 4. TokenRefreshService

> Depends on: 3.2–3.4 (cookie and OIDC must be registered so `GetTokenAsync` works)

- [x] 4.1 Create `Gateway.Api/Services/TokenRefreshService.cs` as a scoped service
- [x] 4.2 Implement `GetValidTokenAsync(HttpContext context)`: read `expires_at` via `context.GetTokenAsync("expires_at")`; parse as `DateTimeOffset`; if more than 30 seconds remain, return `context.GetTokenAsync("access_token")`
- [x] 4.3 If token is expired or within 30 seconds of expiry, call `RefreshTokensAsync(context)`
- [x] 4.4 Implement `RefreshTokensAsync(HttpContext context)`: read `refresh_token` via `context.GetTokenAsync("refresh_token")`; POST to `Keycloak:TokenUrl` with `grant_type=refresh_token`; on success, call `context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties)` using `authResult.Properties.StoreTokens([...])` to store the new `access_token`, `refresh_token`, and `expires_at`; return the new `access_token`
- [x] 4.5 Register `builder.Services.AddScoped<TokenRefreshService>()` in `Gateway.Api/Program.cs`

## 5. Antiforgery

- [x] 5.1 Register `builder.Services.AddAntiforgery(options => { options.HeaderName = "X-XSRF-TOKEN"; })` in `Gateway.Api/Program.cs`
- [x] 5.2 Add `app.UseAntiforgery()` to the pipeline in `Gateway.Api/Program.cs`, positioned between `app.UseAuthentication()` and `app.UseAuthorization()`

## 6. Authorization Policy

- [x] 6.1 Register a named policy `authentication_required` via `builder.Services.AddAuthorization(options => { options.AddPolicy("authentication_required", policy => policy.RequireAuthenticatedUser().RequireClaim("api-access", true.ToString())); })` in `Gateway.Api/Program.cs`

## 7. YARP Token Forwarding Transform

> Depends on: 4.1–4.5 (TokenRefreshService must be registered)

- [x] 7.1 On the `AddReverseProxy()` call in `Gateway.Api/Program.cs`, chain `.AddTransforms(builderContext => { ... })`
- [x] 7.2 Inside the lambda, return early (skip transform registration) if `builderContext.Route.RouteId == "angular-spa-fallback"`
- [x] 7.3 For all other routes, call `builderContext.AddRequestTransform(async transformContext => { ... })` with the following logic:
  - Read `transformContext.HttpContext.Request.Headers.Authorization.FirstOrDefault()`
  - If the value starts with `"Bearer "`, set `transformContext.ProxyRequest.Headers.Authorization = AuthenticationHeaderValue.Parse(incoming)` and return (pass-through for upstream API callers)
  - Otherwise, resolve `TokenRefreshService` from `transformContext.HttpContext.RequestServices` and call `GetValidTokenAsync(context)`; if the result is non-null, set `Authorization: Bearer <token>` on the upstream request

## 8. Downstream JWT Validation — ProductService

> Depends on: no Gateway changes (independent); requires Keycloak config to be correct

- [x] 8.1 In `Shoppiness.ProductsService/Program.cs`, add `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddKeycloakJwtBearer(...)` reading from `Authentication:MetadataAddress`, `Authentication:ValidIssuer`, `Authentication:Audience`
- [x] 8.2 Add `builder.Services.AddAuthorization()` after the authentication registration
- [x] 8.3 Add `app.UseAuthentication()` and `app.UseAuthorization()` before the endpoint mappings

## 9. Smoke Verification

> Depends on: all previous tasks

- [x] 9.1 Run `dotnet build` from the solution root and confirm zero compilation errors across all projects
- [x] 9.2 Start the full stack (Gateway, Keycloak, ProductService) and navigate to a protected Angular route; confirm the browser is redirected to Keycloak's login page
- [x] 9.3 Authenticate at Keycloak; confirm the browser is redirected back to the Angular app and the `__Host-Shoppiness_bff` cookie is set with `HttpOnly`, `Secure`, `SameSite=Strict`; confirm no token appears in `localStorage` or JavaScript-accessible memory
- [x] 9.4 Using the session cookie, call `GET /products-api/products` from the Angular app (no manual `Authorization` header); confirm the request reaches ProductService and returns `200 OK` (the Gateway injected the Bearer token via YARP transform)
- [x] 9.5 Wait for the access token to expire (or artificially set `ExpireTimeSpan` to a short value); make a new proxied request; confirm `TokenRefreshService` silently refreshes the token and the request succeeds without Angular needing to take action
- [x] 9.6 Send a request with a machine-to-machine `Authorization: Bearer <token>` header; confirm the "smart" scheme routes to JwtBearer validation and the token is passed through unchanged to the downstream service
