## Why

A fresh code-level security audit of `Gateway.Api`'s BFF/OIDC implementation (recorded in
`openspec/temporal-discussion.md`, sections 7 and 11) found eight concrete gaps/bugs between the
intended auth design and the actual shipped code in `gateway-bff-auth` (30/30 tasks, marked
complete, not yet archived). Two are severe: `GET /bff/user` always returns an empty claim array
to every authenticated caller (breaks all claim-based frontend logic), and none of the three
downstream services validate the Bearer tokens the Gateway now correctly forwards to them (the
Gateway's auth boundary is currently the *only* enforcement point in the system). The rest are
real defects or missing wiring — a Challenge-vs-401 routing bug that turns every failed
authorization on every proxied route into a 302 instead of a 401 for API-style callers, dead/
orphaned CORS config, an unused logout redirect parameter, an unverified OIDC prompt value,
no session-truly-over signal for the SPA, and antiforgery/CSRF wiring that is registered but
never consumed. This change fixes all eight so the BFF's auth surface actually behaves as
documented and so `frontend-bff-auth` (which already assumes several of these fixes as
prerequisites) has a solid backend to build against.

This change also **supersedes the previously-planned standalone `gateway-challenge-fix` change**
(see `openspec/temporal-discussion.md` section 10, step 5) — that narrower scope (item 1 below)
is folded into this broader change instead of shipping separately, to avoid two changes touching
overlapping areas of `Gateway.Api` in parallel.

This proposal also **corrects an inverted requirement in this change's own
`bff-antiforgery-wiring` capability**: the original wording stated antiforgery SHALL NOT be
required for YARP-proxied downstream calls, which is backwards — CSRF protection is meaningless
if it only guards Gateway-native endpoints while the actual state-changing traffic (product,
stock, payment mutations) flows through YARP unchecked. The capability below reflects the
corrected requirement and completes wiring that was left as dead/uncompiled WIP.

## What Changes

- Wire `DefaultChallengeScheme = "smart"` in `AuthenticationExtension.cs` so a failed
  `.RequireAuthorization()` on any route (Gateway-native or YARP-proxied — all three of
  `products-route`, `stocks-route`, `payments-route` carry `"AuthorizationPolicy": "default"`)
  challenges Bearer-style callers with a bare 401 and cookie-style/no-credential callers with a
  302 redirect to Keycloak, instead of unconditionally redirecting every failed authorization to
  Keycloak. **Needs empirical curl verification** (no running instance was available when the
  fix was chosen from a code trace alone — flagged as a verification task, not just an
  implementation task).
- Remove the orphaned `app.UseCors("AngularDevPolicy")` call from `Program.cs` (the matching
  `AddCors(...)` registration is commented out and CORS is not architecturally needed — the
  `angular-spa-fallback` YARP route already proxies the Angular dev server through the Gateway
  itself, making this a true same-origin setup). Remove the commented-out `AddCors` block and the
  now-fully-unused `BFF:FrontendOrigin` config key (confirmed via grep: only referenced in a
  commented-out line).
- Honor the `redirectUrl` query parameter on `GET /bff/logout`, validating it with the same
  `UrlHelpers.IsLocalUrl` check already used by `/bff/login`'s `returnUrl`, instead of hardcoding
  `RedirectUri = "/"`.
- Add an empirical verification task for `/bff/register`'s use of the non-standard `prompt=register`
  OIDC parameter against the real Keycloak instance (`shoppinessrealm`), plus a documented
  fallback (Keycloak registration endpoint / realm registration-enabled theme setting) if it
  doesn't behave as intended.
- Fix `GET /bff/user` to return real identity claims (id/email/displayName/roles) without
  reversing the cookie-size optimization in `OnTicketReceived` (i.e. the cookie identity keeps
  being stripped of claims) — claims are sourced from elsewhere (raw token inspection via
  `TokenRefreshService`, `/userinfo` call, or a pre-strip claim cache at sign-in). **Highest
  severity item** — this currently breaks all claim-based frontend logic (`claimGuard` can never
  pass).
- Define and implement a distinguishable signal (response code/header/body shape) that
  `TokenRefreshService` / the proxied-request pipeline can surface to the SPA when the *refresh
  token itself* is expired/revoked at Keycloak, distinct from an ordinary 401. Frontend
  consumption (toast + redirect) is explicitly out of scope for this change.
- Add JWT Bearer validation (`AddAuthentication().AddJwtBearer(...)`, same Keycloak
  issuer/audience the Gateway validates against) and `.RequireAuthorization()` wiring to
  `Shoppiness.ProductsService`, `Shoppiness.StocksService`, and `Shoppiness.PaymentsService`.
  `PaymentsService` has no endpoints mapped yet, so its scope here is auth-pipeline readiness
  (config + middleware), not protecting real endpoints.
- Correct and complete `bff-antiforgery-wiring`: flip the capability's inverted requirement so
  antiforgery validation is required (not exempt) on mutating (`POST`/`PUT`/`PATCH`/`DELETE`)
  YARP-proxied requests, with a bypass for machine-to-machine callers that already carry an
  `Authorization: Bearer` header (reusing the exact Bearer-detection check already in the YARP
  request transform). Replace the broken/mis-scoped route-group WIP in `Program.cs`
  (`UseAntiforgery()` called on a `RouteGroupBuilder`, which does not compile, targeting a
  `/api` prefix that doesn't match the real `/products-api`, `/stocks-api`, `/payments-api`
  paths) with custom middleware registered ahead of `app.MapReverseProxy()`. Harden the
  antiforgery tracking cookie (`options.Cookie.SameSite = Strict`,
  `options.Cookie.SecurePolicy = Always`) to match the auth cookie's posture. Remove the dead
  `"AntiforgeryRequired": "true"` YARP route metadata from all three proxy routes in
  `appsettings.json` and `appsettings.Development.json` (grepped: nothing reads
  `route.Config.Metadata["AntiforgeryRequired"]` — enforcement is via middleware, not metadata).
  Fold `XSRF-TOKEN` cookie issuance into the `/bff/user` handler (via
  `IAntiforgery.GetAndStoreTokens`), removing the standalone `GET /api/antiforgery/token`
  endpoint — the SPA already calls `GET /bff/user` once per session (immediately after login, per
  `bff-user-claims`), so this avoids a redundant round trip. Document that the SPA's reliance on
  that existing call for CSRF-token issuance remains frontend/Angular work tracked in the sibling
  `frontend-bff-auth` change, out of scope here.

## Capabilities

### New Capabilities
- `challenge-scheme-routing`: Challenge dispatch (`DefaultChallengeScheme`) consults the same
  Bearer-vs-Cookie policy scheme ("smart") that Authenticate dispatch already uses, so failed
  authorization returns 401 to API/Bearer callers and a Keycloak redirect to cookie/browser
  callers, on every route (Gateway-native and YARP-proxied).
- `bff-cors-cleanup`: Removes the orphaned/non-functional CORS middleware call and its dead
  config, documenting why CORS is architecturally unnecessary in this same-origin BFF setup.
- `bff-logout-redirect`: `GET /bff/logout` honors a validated `redirectUrl` query parameter
  instead of always redirecting to `/`.
- `bff-registration-flow`: `GET /bff/register`'s `prompt=register` behavior is empirically
  verified against Keycloak, with a documented fallback if unsupported.
- `bff-user-claims`: `GET /bff/user` returns real, correct identity claims for authenticated
  callers without reversing the cookie-claim-stripping optimization.
- `refresh-expiry-signal`: The Gateway surfaces a distinguishable signal to the SPA when the
  refresh token itself is expired/revoked (silent refresh permanently failed), separate from a
  normal 401.
- `downstream-jwt-validation`: ProductsService, StocksService, and PaymentsService validate
  forwarded Bearer tokens via JWT Bearer authentication and enforce `.RequireAuthorization()` on
  protected endpoints. (Corrects/supersedes the same-named, never-implemented capability
  description in `openspec/changes/gateway-bff-auth/specs/downstream-jwt-validation/spec.md`.)
- `bff-antiforgery-wiring`: Corrects this change's own inverted requirement — antiforgery
  validation IS required (not exempt) for mutating YARP-proxied downstream requests, bypassed
  only for machine-to-machine Bearer callers — and completes the wiring: custom middleware
  enforcing that validation ahead of `app.MapReverseProxy()` (replacing broken/uncompiled route-
  group WIP), a hardened antiforgery cookie matching the auth cookie's posture, and removal of
  the dead `AntiforgeryRequired` YARP route metadata that no code ever read.

### Modified Capabilities
_None._ No capability has been archived to `openspec/specs/` yet (`gateway-bff-auth` is complete
but unarchived), so there is no existing baseline spec to delta against — all eight capabilities
above are authored fresh, informed by (and correcting) `gateway-bff-auth`'s design/spec drift
where relevant.

## Impact

- **Affected code:** `backend/src/Gateway.Api/Program.cs`,
  `backend/src/Gateway.Api/Endpoints.cs`,
  `backend/src/Gateway.Api/Extensions/AuthenticationExtension.cs`,
  `backend/src/Gateway.Api/Services/TokenRefreshService.cs`,
  `backend/src/Gateway.Api/appsettings.json`, `backend/src/Gateway.Api/appsettings.Development.json`,
  `backend/src/Shoppiness.ProductsService/Program.cs` (+ `appsettings.json`),
  `backend/src/Shoppiness.StocksService/Program.cs` (+ `appsettings.json`),
  `backend/src/Shoppiness.PaymentsService/Program.cs` (+ `appsettings.json`).
- **Dependencies:** downstream JWT validation adds `Microsoft.AspNetCore.Authentication.JwtBearer`
  (already used by `Gateway.Api`) to the three downstream service projects.
- **Systems:** Keycloak (`shoppinessrealm`) is the empirical verification target for items 1 and
  4; no Keycloak-side config changes are in scope unless verification proves `prompt=register`
  doesn't work, in which case a fallback is designed (not necessarily implemented) here.
- **Out of scope:** `web-client/`, `openspec/changes/frontend-auth/`,
  `openspec/changes/frontend-bff-auth/`. Frontend consumption of the refresh-expiry signal and of
  real `/bff/user` claims, and the `csrf-interceptor` wiring (including the SPA's reliance on its
  existing `GET /bff/user` call after login for `XSRF-TOKEN` issuance, now that the standalone
  antiforgery-token endpoint is removed), remain tracked in `frontend-bff-auth`.
