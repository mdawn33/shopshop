## 1. CORS cleanup (bff-cors-cleanup)

- [x] 1.1 Remove `app.UseCors("AngularDevPolicy")` from `Gateway.Api/Program.cs`
- [x] 1.3 Remove the `BFF:FrontendOrigin` key from `Gateway.Api/appsettings.json` and
  `Gateway.Api/appsettings.Development.json`
- [x] 1.4 Remove the commented-out `var frontendOrigin = config["BFF:FrontendOrigin"] ?? string.Empty;`
  line (and its accompanying dead comment block) from the `/bff/login` handler in
  `Gateway.Api/Endpoints.cs`
- [x] 1.5 Grep the project for any remaining `FrontendOrigin` references to
  confirm none remain

## 2. Antiforgery enforcement for YARP-proxied routes (bff-antiforgery-wiring)

> Corrects the capability's original (inverted) requirement — antiforgery IS required on
> mutating YARP-proxied requests, not exempt from it — and completes the wiring left dead by
> `gateway-bff-auth`. Route-metadata cleanup (2.1-2.3) was already correct and stays done;
> everything from 2.4 on replaces the previous unchecked, contradictory task bullets.

- [x] 2.1 Remove the `"AntiforgeryRequired": "true"` entry from the `Metadata` block of
  `products-route`, `stocks-route`, and `payments-route` in `Gateway.Api/appsettings.json`. 
  - This is not required because we are not passing the antiforgery token in the request to the DS services.
- [x] 2.2 Remove the same entries from `Gateway.Api/appsettings.Development.json`
- [x] 2.3 Grep the project for `AntiforgeryRequired` to confirm no remaining references
- [x] 2.4 Remove the broken/mis-scoped WIP route-group attempt from `Gateway.Api/Program.cs`
  (`app.MapGroup("/api-")` (or similarly-named group) calling `.RequireAuthorization()` and
  `.UseAntiforgery()` before `.MapReverseProxy()`) — `UseAntiforgery()` is an
  `IApplicationBuilder` extension, not a valid `RouteGroupBuilder`/`IEndpointConventionBuilder`
  extension, so this does not compile; it also targets the wrong URL space (`/api-...`) while the
  real proxied paths are `/products-api/...`, `/stocks-api/...`, `/payments-api/...` with no
  `/api` prefix (per `appsettings.Development.json`)
- [x] 2.5 In `Gateway.Api/Program.cs`, extract the existing "does this request already carry a
  Bearer token" check out of the YARP `AddRequestTransform` lambda into a small shared
  helper/local function (e.g. `static bool HasBearerToken(HttpRequest request)`), and use it from
  both the transform and the new middleware in 2.6, so the two checks cannot drift apart
- [x] 2.6 In `Gateway.Api/Program.cs`, add a custom middleware
  (`app.Use(async (context, next) => ...)`) registered after `app.UseAuthorization()` and before
  `app.MapReverseProxy()` that:
  - calls `next()` immediately for non-mutating methods (anything other than `POST`, `PUT`,
    `PATCH`, `DELETE`, checked via `HttpMethods.IsPost/IsPut/IsPatch/IsDelete`)
  - calls `next()` immediately if `HasBearerToken(context.Request)` (from 2.5) is `true`
  - otherwise resolves `IAntiforgery` and calls `await antiforgery.ValidateRequestAsync(context)`,
    catching `AntiforgeryValidationException` and short-circuiting with
    `context.Response.StatusCode = StatusCodes.Status400BadRequest` (no `next()` call, no
    downstream request made)
- [x] 2.7 Confirm/document whether the middleware from 2.6 is registered globally (simplest — it
  is a no-op for GET/HEAD/OPTIONS and for Gateway-native mutating endpoints that don't yet exist)
  or scoped specifically ahead of the reverse-proxy paths; either is acceptable as long as
  Gateway-native endpoints in `Endpoints.cs` are unaffected
- [x] 2.8 Confirm `app.MapReverseProxy()` remains mapped exactly once, at its existing unprefixed
  paths, with no redundant `MapGroup`-based re-mapping left over from the removed WIP in 2.4
- [x] 2.9 In `Gateway.Api/Program.cs`'s `AddAntiforgery(options => ...)` call, add
  `options.Cookie.SameSite = SameSiteMode.Strict;` and
  `options.Cookie.SecurePolicy = CookieSecurePolicy.Always;` so the antiforgery tracking cookie
  matches the `__Host-Shoppiness_bff` auth cookie's posture. This cookie stays `HttpOnly = true`;
  the separate `XSRF-TOKEN` cookie written by `GET /api/antiforgery/token` is unaffected and
  stays `HttpOnly = false`
- [x] 2.10 Remove the `GET /api/antiforgery/token` endpoint from `Gateway.Api/Endpoints.cs`.
  Instead, have the `/bff/user` handler call `IAntiforgery.GetAndStoreTokens(HttpContext)` (or
  equivalent) so that the `XSRF-TOKEN` cookie is set as a side effect of calling `/bff/user`.
  This task is coupled to the `/bff/user` handler rewrite in section 6 (6.1-6.7) — implement it
  alongside those changes, since both touch the same handler
- [ ] 2.11 Document, as a dependency note for `frontend-bff-auth` (not a task performed in this
  change, which is backend-only): there is no longer a separate token-issuance endpoint to call —
  the `XSRF-TOKEN` cookie is now set automatically as a side effect of the frontend's existing
  required call to `GET /bff/user` immediately after login (per section 6, `bff-user-claims`).
  The Angular `csrf-interceptor` therefore needs no explicit token-fetch step of its own; it only
  needs to ensure `/bff/user` has been called at least once in the session before issuing a
  mutating request
- [ ] 2.12 Manually verify: with a valid session cookie but no antiforgery token fetched yet,
  `POST`/`PUT`/`PATCH`/`DELETE` a YARP-proxied route (e.g. `/products-api/...`) and confirm
  `400 Bad Request` with no request reaching the downstream service
- [ ] 2.13 Manually verify: call `GET /bff/user`, confirm the response sets the `XSRF-TOKEN`
  cookie, then repeat a mutating YARP-proxied request with that cookie value echoed back in the
  `X-XSRF-TOKEN` header, and confirm the request is forwarded downstream (subject to normal
  auth/authorization results)
- [ ] 2.14 Manually verify: send the same mutating request with a fabricated
  `Authorization: Bearer <token>` header and no antiforgery token, and confirm the antiforgery
  middleware is bypassed (the request either succeeds or fails solely on JWT
  authentication/authorization grounds, never on a 400 from antiforgery validation)
- [ ] 2.15 Manually verify: `GET` a YARP-proxied route with a valid session but no antiforgery
  token and confirm it succeeds (antiforgery validation must never trigger on non-mutating
  methods)

## 3. Challenge-vs-401 fix (challenge-scheme-routing)

> Depends on: nothing structurally, but should land early since items 6-8 assume the Gateway's
> own auth boundary behaves correctly.

- [ ] 3.1 Review whether the Cookie scheme's default challenge redirect and JwtBearer's default
  401 challenge behave as expected with the OnRedirectToIdentityProvider event added
- [ ] 3.2 **Verification task (not just implementation):** with a running Gateway and reachable
  Keycloak, `curl -i` a protected route (e.g. `/api/antiforgery/token` or a proxied route) with
  no credentials and confirm `302` to Keycloak's authorize URL; repeat with a fabricated
  `Authorization: Bearer <garbage>` header and confirm `401` with no `Location` header
- [ ] 3.3 Record the verification outcome (pass/fail per scenario) — if results don't match the
  `challenge-scheme-routing` spec, treat as a defect and revisit design.md D1 before closing
  this task group

## 4. Logout redirectUrl (bff-logout-redirect)

- [x] 4.1 In `Gateway.Api/Endpoints.cs`, change the `/bff/logout` handler to validate
  `redirectUrl` via `UrlHelpers.IsLocalUrl(redirectUrl)`, using it as `RedirectUri` when valid
  and falling back to `/` otherwise (mirror the existing `/bff/login` pattern)
- [x] 4.2 Remove the stale `// context.BuildRedirectUrl(redirectUrl)` comment line
- [x] 4.3 Update or remove the `// TODO: Handle the error when user is not authenticated...`
  comment to reflect the decision in design.md D3: `Results.SignOut` is safe when
  unauthenticated, and the `id_token_hint`-on-sign-out gap is an explicit follow-up, not fixed
  here — leave a clear comment referencing this decision rather than deleting the concern
  silently
- [ ] 4.4 Manually verify: call `/bff/logout?redirectUrl=/orders` and `/bff/logout` with no
  parameter and confirm the resulting redirect targets match the spec

## 5. Registration prompt verification (bff-registration-flow)

> Depends on: 3.3 (Gateway + Keycloak must be reachable for verification)

- [ ] 5.1 **Verification task:** with a running Gateway and reachable `shoppinessrealm`
  Keycloak instance, follow `GET /bff/register` through to Keycloak's hosted UI and confirm
  whether the registration form (not the login form) is shown
- [ ] 5.2 If verification passes, close this capability with no code change, documenting the
  confirmed outcome
- [ ] 5.3 If verification fails, implement the fallback from design.md D4: redirect the OIDC
  challenge for `/bff/register` to Keycloak's `protocol/openid-connect/registrations` endpoint
  instead of the standard authorize endpoint, scoped to this one flow only
- [ ] 5.4 If 5.3 is implemented, re-verify by following `GET /bff/register` through to Keycloak
  again and confirm the registration form is now shown

## 6. GET /bff/user claims fix (bff-user-claims)

> Highest-severity item in this change. Depends on: 3.1-3.4 (Challenge fix should be verified
> first, since this capability's failure responses route through the same auth pipeline).

- [ ] 6.1 In `Gateway.Api/Endpoints.cs`, change the `/bff/user` handler to resolve
  `TokenRefreshService` and call `GetValidTokenAsync(context)` instead of reading
  `context.User.Claims`
- [ ] 6.2 Parse the returned access token using `JsonWebTokenHandler().ReadJsonWebToken(token)`
  (or equivalent) to extract its claims, without a full re-validation pass (see design.md D5 for
  the trust-level rationale)
- [ ] 6.3 Map an explicit allow-list of claims into the response: user id (`sub`), email,
  display name (`preferred_username`/`name`), and role claims (per the configured
  `RoleClaimType`) — preserve the existing `{ Type, Value }[]` response shape the frontend
  already expects
- [ ] 6.4 Verify during implementation whether the access token actually carries `email` and a
  display-name-suitable claim, given the current `Scope.Add("roles-only")` configuration (no
  `profile`/`email` scope requested) — if claims are missing, either add the needed scopes to
  `AuthenticationExtension.cs`'s `options.Scope` list or fall back to a `/userinfo` call per
  design.md D5's documented alternative
- [ ] 6.5 Confirm `OnTicketReceived` in `AuthenticationExtension.cs` is unchanged — the cookie
  identity must remain stripped of claims after this fix
- [ ] 6.6 Return `401 Unauthorized` (unchanged) when `GetValidTokenAsync` returns `null` due to
  no session; when it returns `null` specifically because a refresh attempt was rejected by
  Keycloak, also attach the signal implemented in section 7 below
- [ ] 6.7 Manually verify: call `GET /bff/user` with a valid session and confirm the response
  contains real, non-empty claims (id, email, display name, roles if any)

## 7. Refresh-expiry signal (refresh-expiry-signal)

> Depends on: 6.1-6.2 (reuses the same `GetValidTokenAsync`/`RefreshTokensAsync` call path)

- [ ] 7.1 In `Gateway.Api/Services/TokenRefreshService.cs`, change `RefreshTokensAsync`/
  `GetValidTokenAsync` to distinguish "no refresh token present" from "refresh token present but
  rejected by Keycloak" (e.g. via a small result type or an out parameter/property indicating
  which case occurred)
- [ ] 7.2 In `Gateway.Api/Program.cs`'s YARP `AddRequestTransform` lambda, when the "refresh
  rejected" case occurs, set `transformContext.HttpContext.Response.Headers["X-Shoppiness-Session-Expired"] = "true"`
  before the request is forwarded
- [ ] 7.3 In the `/bff/user` handler (`Gateway.Api/Endpoints.cs`), apply the same header when
  the "refresh rejected" case occurs (see task 6.6)
- [ ] 7.4 Confirm the header is absent for fully anonymous callers (no session ever existed) —
  add a check/comment distinguishing this from the "refresh rejected" case
- [ ] 7.5 Manually verify with a session whose refresh token has been revoked in Keycloak (or a
  simulated rejection) that both a proxied call and `GET /bff/user` return the
  `X-Shoppiness-Session-Expired: true` header

## 8. Downstream JWT validation (downstream-jwt-validation)

> Largest-scope item. Independent of sections 3-7, but should land after section 3 so the
> Gateway's own auth boundary is confirmed sound first.

- [ ] 8.1 Add `Authentication:MetadataAddress`, `Authentication:ValidIssuer`, and
  `Authentication:Audience` configuration keys to
  `Shoppiness.ProductsService/appsettings.json` (and a Development variant if one exists),
  mirroring `Gateway.Api`'s values
- [ ] 8.2 Add the `Microsoft.AspNetCore.Authentication.JwtBearer` package reference to
  `Shoppiness.ProductsService.csproj` (match the version already used by `Gateway.Api.csproj`)
- [ ] 8.3 In `Shoppiness.ProductsService/Program.cs`, register
  `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`
  configured from the keys added in 8.1, with `RequireHttpsMetadata = !env.IsDevelopment()`
- [ ] 8.4 Add `app.UseAuthentication()` and `app.UseAuthorization()` to
  `Shoppiness.ProductsService/Program.cs`, positioned before endpoint mapping
- [ ] 8.5 Review `Shoppiness.ProductsService`'s Category and Product feature endpoints
  (`Features/Categories/*.cs`, `Features/Products/*.cs`, `Features/Products/Purchase/*.cs`) and
  confirm/add `.RequireAuthorization()` on write operations (Create/Update/Delete/Purchase),
  noting that `Categories/Create.cs` already calls `.RequireAuthorization()` today with no
  authentication pipeline registered (a pre-existing latent defect this task resolves)
- [ ] 8.6 Repeat 8.1-8.4 for `Shoppiness.StocksService` (config keys, package reference,
  `AddAuthentication`/`AddJwtBearer`, `UseAuthentication`/`UseAuthorization`)
- [ ] 8.7 Add `.RequireAuthorization()` to `Shoppiness.StocksService`'s write endpoints
  (`InitializeStock`, `AddStock`, `RemoveStock`); document the decision on whether
  `GetStockLevel` (read) also requires authorization
- [ ] 8.8 Repeat 8.1-8.4 for `Shoppiness.PaymentsService` (config keys, package reference,
  `AddAuthentication`/`AddJwtBearer`, `UseAuthentication`/`UseAuthorization`) — no endpoints
  exist yet, so this is pipeline-readiness only; do not add placeholder endpoints solely to
  exercise the pipeline unless verification in 8.9 requires one
- [ ] 8.9 Manually verify: call a protected endpoint on each of ProductsService and
  StocksService directly (bypassing the Gateway) with no `Authorization` header and confirm
  `401`; repeat with a valid Bearer token obtained via the Gateway's forwarding and confirm
  success
- [ ] 8.10 Manually verify: confirm `Shoppiness.PaymentsService` starts successfully with the
  new authentication pipeline registered and no mapped endpoints

## 9. Cross-cutting verification

- [ ] 9.1 Run `openspec validate gateway-bff-auth-part-2 --strict` and confirm it passes
- [ ] 9.2 Confirm no changes were made under `web-client/`,
  `openspec/changes/frontend-auth/`, or `openspec/changes/frontend-bff-auth/`
- [ ] 9.3 Re-read `openspec/temporal-discussion.md` sections 7 and 11 against the final
  implementation and confirm every numbered gap (1-8) has a corresponding completed task group
  above
- [ ] 9.4 Note in the PR/change summary which verification tasks (2.12-2.15, 3.3-3.4, 5.1,
  8.9-8.10) actually ran against a live instance versus which remain simulated/unverified, so
  `frontend-bff-auth` implementers know what's empirically confirmed
