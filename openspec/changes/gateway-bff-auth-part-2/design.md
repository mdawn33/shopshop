## Context

`gateway-bff-auth` shipped the BFF/OIDC skeleton (cookie auth, OIDC redirect login, YARP token
forwarding, `TokenRefreshService`) and is marked complete (30/30 tasks) but was never audited
against running/real behavior — `products-api` isn't runnable locally yet, so several decisions
in that change were made from a code trace only. A follow-up audit
(`openspec/temporal-discussion.md`, sections 7 and 11) re-read every relevant file
(`Program.cs`, `Endpoints.cs`, `AuthenticationExtension.cs`, `TokenRefreshService.cs`,
both `appsettings*.json`, and the three downstream services' `Program.cs`) and found eight
concrete gaps. This design covers the technical approach for each. All eight are backend-only,
confined to `backend/src/Gateway.Api` plus targeted additions to
`Shoppiness.ProductsService`, `Shoppiness.StocksService`, and `Shoppiness.PaymentsService`
(item 7 only).

Two constraints shape every decision below:
- **No running Keycloak-backed environment was used to derive these decisions.** Items 1 and 4
  are explicitly flagged for empirical verification as part of this change, not assumed correct
  from the trace alone.
- **Cookie-size optimization is a hard constraint, not a suggestion.** `OnTicketReceived`
  stripping all claims from the cookie identity exists specifically to avoid the
  "Request Header Or Cookie Too Large" failure mode (see comment in
  `AuthenticationExtension.cs` citing this). Item 5's fix must not reverse it.

## Goals / Non-Goals

**Goals:**
- Make Challenge dispatch (302 vs 401) match the already-correct Authenticate dispatch logic,
  across every route that enforces authorization, not just Gateway-native endpoints.
- Remove dead/non-functional config (CORS, `AntiforgeryRequired` metadata, `BFF:FrontendOrigin`)
  so the codebase reflects only what's actually wired.
- Close the two severity-blocking gaps: `/bff/user` returning real claims, and downstream
  services actually validating the tokens the Gateway forwards to them.
- Give the SPA a way to distinguish "your session is truly over" from an ordinary 401.
- Leave clear, explicit empirical-verification and fallback-design notes for the two items
  (Challenge scheme, `prompt=register`) that were decided without a running instance.

**Non-Goals:**
- No frontend code changes (`web-client/`) — consumption of the claims fix, the refresh-expiry
  signal, and CSRF wiring is `frontend-bff-auth`'s job.
- No change to `openspec/changes/gateway-bff-auth/` itself (proposal/design/tasks are left as
  historical record; only its `downstream-jwt-validation` spec content is superseded here,
  in a new change).
- No Keycloak realm/admin-console configuration changes are performed as part of this change
  unless item 4's verification proves `prompt=register` doesn't work, in which case a fallback
  is *designed* here but its actual implementation may be scoped as a follow-up if it requires
  realm-level changes outside this codebase.
- PaymentsService does not get new business endpoints in this change — item 7's scope there is
  limited to authentication pipeline readiness (see D7).

## Decisions

### D1: Challenge dispatch — `DefaultChallengeScheme = "smart"`

Change `AuthenticationExtension.cs`'s `AddAuthentication(options => ...)` block so
`options.DefaultChallengeScheme = "smart"` (the existing `AddPolicyScheme("smart", ...)`
registration, currently used only for `DefaultAuthenticateScheme`), instead of the hardcoded
`OpenIdConnectDefaults.AuthenticationScheme` literal.

Because `"smart"`'s `ForwardDefaultSelector` inspects the `Authorization` header
(`Bearer ...` → JwtBearer, else → Cookie), this makes Challenge dispatch consult the same
logic Authenticate dispatch already uses correctly. JwtBearer's default challenge behavior is
already a bare `401` (no code change needed there); Cookie's default challenge behavior is
already a redirect (no `OnRedirectToLogin` override needed, matching the existing intent
documented in the commented-out block).

This affects every route carrying `"AuthorizationPolicy": "default"` in `appsettings.json`
(`products-route`, `stocks-route`, `payments-route`) as well as the Gateway-native
`.RequireAuthorization()` endpoint (`/bff/refresh`) — not a narrow fix. (The standalone
`GET /api/antiforgery/token` endpoint referenced in earlier drafts of this document no longer
exists — see D8 — its token-issuance responsibility is folded into `/bff/user`.)

**Empirical verification required** (tasks.md captures this as its own task, separate from the
code change): with the Gateway running and Keycloak reachable, `curl -i` a protected route with
no cookie and no `Authorization` header (expect `401`), then with a fabricated/garbage
`Authorization: Bearer x` header (expect `401`, not a crash), then with neither header via a
browser-simulated request (expect `302` to the Keycloak authorize URL). If results don't match,
this decision needs revisiting before the fix is considered closed.

**Alternatives considered:**
- Always-401-on-Challenge (drop OIDC auto-redirect, let the SPA decide when to navigate to
  `/bff/login`) — rejected; user confirmed the Bearer-vs-Cookie distinction on Challenge is the
  desired behavior, not a simplification target (`temporal-discussion.md` 7.4, option A).
- Per-route policy overrides (keep `DefaultChallengeScheme = OIDC` but add a second
  `.RequireAuthorization("apiOnly")` policy with an explicit `AuthenticationSchemes = [JwtBearer]`
  on API routes) — rejected; more config surface than wiring the scheme selector once, and
  doesn't naturally cover future routes the way `DefaultChallengeScheme` does.

### D2: Remove CORS entirely (not "fix" it)

Delete `app.UseCors("AngularDevPolicy")` from `Program.cs`, delete the commented-out
`AddCors(...)` block above it, and remove the `BFF:FrontendOrigin` key from both
`appsettings.json` and `appsettings.Development.json` (confirmed via grep: its only other
reference anywhere in the project is a commented-out line in `Endpoints.cs`'s login handler —
no live consumer).

This is corrected framing versus the original assumption in
`openspec/temporal-discussion.md` section 6.2 ("the `AddCors` registration is missing, should be
restored") — the actual right fix is the opposite. The `angular-spa-fallback` YARP route already
proxies `http://localhost:4200/` through the Gateway itself (`appsettings.json`
`Clusters.angular-cluster`), so the browser only ever talks to one origin
(`https://localhost:5001`). CORS headers are meaningless in a true same-origin setup; keeping
`UseCors` referencing an unregistered policy name is a latent defect regardless (unverified
whether it currently throws at startup or silently no-ops, since the app hasn't been run with
this configuration empirically — removing it eliminates the ambiguity either way).

**Alternatives considered:**
- Register `AddCors("AngularDevPolicy", ...)` to match the `UseCors` call — rejected per
  corrected framing above; would reintroduce an unnecessary attack surface (CORS is an opt-in
  relaxation of the same-origin policy, not something to add defensively when same-origin
  already holds).

### D3: `GET /bff/logout` honors `redirectUrl`

Change the logout handler's `Results.SignOut(...)` call to build `RedirectUri` the same way
`/bff/login` builds its path: validate `redirectUrl` with `UrlHelpers.IsLocalUrl(redirectUrl)`,
falling back to `"/"` when absent or non-local. Reuse `UrlHelpers.IsLocalUrl` as-is — no new
validation helper.

The existing `// TODO: Handle the error when user is not authenticated or cookie is not
provided` is **not addressed in this change** — `Results.SignOut` against the Cookie + OIDC
schemes is safe to call even when unauthenticated (Cookie sign-out on an absent cookie is a
no-op; OIDC's remote sign-out still redirects to Keycloak's end-session endpoint). The
open concern behind that TODO — no `id_token_hint` is passed on sign-out (the
`OnRedirectToIdentityProviderForSignOut` event that would attach it is commented out in
`AuthenticationExtension.cs`), so Keycloak may show its own "are you sure you want to sign out"
confirmation screen — is a separate, pre-existing UX gap not called out in the section 11 audit
that produced this change's scope. Documented here as an explicit **follow-up**, not silently
dropped.

### D4: `/bff/register`'s `prompt=register` — verify, with a documented fallback

`prompt=register` is not one of the four OIDC-spec-defined prompt values (`none`, `login`,
`consent`, `select_account`). No code change is prescribed here beyond adding an empirical
verification task against the real `shoppinessrealm` Keycloak instance: hit `/bff/register`
with a browser/curl-with-redirect-follow and confirm Keycloak's hosted theme lands on the
registration form, not the login form.

**If verification fails, fallback (documented now, implemented only if needed):**
1. **Keycloak's dedicated registration endpoint** — construct the challenge against
   `{realm}/protocol/openid-connect/registrations` instead of the standard
   `{realm}/protocol/openid-connect/auth` endpoint for this one flow. Requires either
   overriding `OnRedirectToIdentityProvider` to rewrite `context.ProtocolMessage.IssuerAddress`
   only when a request-scoped flag (e.g. a query string marker checked in the event) indicates
   the register flow, or issuing a second named OIDC scheme configured with that endpoint.
2. **Realm-level "User registration" toggle** — if enabled in Keycloak's realm login settings,
   the standard hosted login page shows its own "Register" link; `/bff/register` would then
   just be `/bff/login` in effect (drop the `prompt` parameter). Weaker UX (extra click) but
   requires no endpoint-swapping logic.

Option 1 is preferred if verification fails, since it preserves the current one-click UX;
option 2 is the fallback-of-the-fallback if Keycloak's registrations endpoint proves unreliable
in this realm's configuration.

### D5: `GET /bff/user` returns real claims without reversing the cookie-size fix

**Chosen approach:** derive claims from the access token itself, not the cookie identity.

`OnTicketReceived` only removes `.Token.id_token` and `.Token.token_type` from
`AuthenticationProperties.Items`, plus every claim from `ClaimsIdentity` — it does **not**
remove `.Token.access_token`, `.Token.refresh_token`, or `.Token.expires_at`. The access token
(a Keycloak-issued JWT carrying `sub`, `email`, `preferred_username`/`name`, and realm role
claims) is therefore still available via `context.GetTokenAsync("access_token")` — this is
exactly what `TokenRefreshService.GetValidTokenAsync` already reads and returns.

The fix: `/bff/user`'s handler resolves `TokenRefreshService` and calls
`GetValidTokenAsync(context)` (the same call the YARP transform makes) to get a guaranteed-fresh
access token — this has the side benefit of proactively refreshing if the current token is
within 30 seconds of expiry, same as any proxied call would. The token is then parsed
(`JsonWebTokenHandler().ReadJsonWebToken(token)`, from the `Microsoft.IdentityModel.JsonWebTokens`
namespace already imported in `AuthenticationExtension.cs`) **without a full re-validation
pass** (this token was already validated at OIDC sign-in / previous refresh cycles and lives
only in a `HttpOnly`, `Secure`, `SameSite=Strict` server-side cookie — the same trust level
`TokenRefreshService` already implicitly extends it when forwarding it downstream). The handler
maps an explicit allow-list of claims (`sub` → id, `email`, `preferred_username`/`name` →
displayName, the configured `RoleClaimType` values → roles) into the response, preserving the
existing `{ Type, Value }[]` shape the frontend (`core/services/auth.ts`, per
`temporal-discussion.md` section 5) already expects and parses as `Claim[]`.

If `GetValidTokenAsync` returns `null` — no session, or refresh failed — the endpoint returns
`401` as it already does today for the "not authenticated" branch (`context.User.Identity?.IsAuthenticated
!= true`). If the *specific* reason is a failed refresh (session was real but Keycloak rejected
the refresh), the same distinguishable signal introduced in D6 is attached to this response too,
so `/bff/user` and proxied calls behave consistently for the SPA.

**Alternatives considered:**
- **Call Keycloak's `/userinfo` endpoint on every `/bff/user` request** — rejected as the
  primary approach; adds a network round-trip and a new external failure mode to a
  frequently-called endpoint (session hydration on every app load) for data the access token
  already carries. Kept as a documented fallback if the access token turns out to be missing
  claims the frontend needs (e.g. if `GetClaimsFromUserInfoEndpoint = true` at sign-in pulls in
  claims not present in the token itself) — verify claim parity during implementation.
- **Cache a curated claim set separately at sign-in, before stripping** (e.g. a small JSON blob
  under a custom key in `AuthenticationProperties.Items`) — rejected; re-adds bytes to the
  cookie (defeats the original optimization's intent, even if smaller than the original
  full-claims payload), and creates a second source of truth that must be kept in sync across
  every `SignInAsync` call in `TokenRefreshService.RefreshTokensAsync` (easy to forget on future
  changes). Deriving from the access token has no such sync burden — the token itself is
  refreshed by the exact same code path already.

### D6: Refresh-token-expiry signal — a response header, not a status code or body shape

`TokenRefreshService.RefreshTokensAsync` currently returns `null` from two different situations
that need to be distinguished:
1. There's no `refresh_token` on the ticket at all (never authenticated, or a very old session
   predating `offline_access` scope) — normal "not authenticated" case.
2. A `refresh_token` exists, but Keycloak's token endpoint rejects it (expired, revoked, or the
   session was ended out-of-band) — the "your session is truly over" case this item targets.

**Decision:** widen `RefreshTokensAsync`'s return to distinguish these two outcomes internally
(e.g. return `null` for case 1, and for case 2 have the caller — `GetValidTokenAsync` — record
that the refresh attempt was made and failed against a real refresh token). In the YARP request
transform (`Program.cs`) and in the `/bff/user` handler (D5), when this "refresh attempted and
rejected" outcome occurs, set a dedicated response header —
**`X-Shoppiness-Session-Expired: true`** — on `transformContext.HttpContext.Response.Headers`
(mutable pre-flight on a request transform, so it rides along on whatever status code the
eventual response carries, whether that's a Gateway-native `401` or a downstream service's own
`401` once item 7 ships) or directly on the `/bff/user` response.

**Alternatives considered:**
- **A distinct HTTP status code** (e.g. `419`, used informally by some frameworks for "session
  expired") — rejected; non-standard status codes on a *proxied* response path risk being
  altered or stripped by intermediate infrastructure (load balancers, some HTTP client
  libraries normalize unrecognized 4xx codes), and this change doesn't control what status code
  downstream services choose to return once they validate JWTs independently (item 7).
- **A dedicated response body shape** (e.g. a specific `ProblemDetails.type` URI) — rejected as
  the primary mechanism for proxied calls specifically; the Gateway doesn't rewrite proxied
  response bodies today (only request transforms are wired — see `Program.cs`'s
  `AddTransforms`), so guaranteeing a specific body shape across arbitrary downstream JSON
  payloads would require adding response-body transforms, a larger change than this item
  warrants. A body shape remains reasonable for Gateway-native-only responses (`/bff/user`) and
  is used there in addition to the header, for convenience — but the header is the one signal
  guaranteed present on every path, including proxied ones.

### D7: Downstream JWT validation — mirror the Gateway's own JwtBearer wiring

Add `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` (the plain
`Microsoft.AspNetCore.Authentication.JwtBearer` package already referenced by `Gateway.Api`'s
`.csproj`) to each of `Shoppiness.ProductsService`, `Shoppiness.StocksService`, and
`Shoppiness.PaymentsService`, configured against the same `Authentication:MetadataAddress`,
`Authentication:ValidIssuer`, and `Authentication:Audience` keys already present in
`Gateway.Api/appsettings.json` — add these keys to each downstream service's own
`appsettings.json` (currently absent from all three; each service currently has no
`Authentication` section at all). Add `app.UseAuthentication()` / `app.UseAuthorization()` to
each `Program.cs`, positioned before endpoint mapping.

This **corrects and supersedes** the plan in
`openspec/changes/gateway-bff-auth/specs/downstream-jwt-validation/spec.md`, which referenced a
non-existent `AddKeycloakJwtBearer(...)` extension method — no `Keycloak.AuthServices` (or
similar) package is referenced anywhere in the solution (confirmed via grep across all
`.csproj` files); the plain `AddJwtBearer` call, identical in shape to what `Gateway.Api` already
does successfully, is the correct and available approach.

**Per-service scope is not uniform:**
- **ProductsService** — has real endpoints already calling `.RequireAuthorization()`
  (`Features/Categories/Create.cs`) with **no authentication configured at all today** — this is
  a latent defect independent of this audit: calling `.RequireAuthorization()` with no
  authentication scheme registered throws at request time. This item's fix resolves that
  incidentally. Other endpoints (Products, Purchase) should be reviewed during implementation
  for whether they need the same `.RequireAuthorization()` treatment — call out any gaps found
  as implementation-time findings, not assumed already-decided here.
- **StocksService** — has real endpoints (`GetStockLevel`, `InitializeStock`, `AddStock`,
  `RemoveStock`), none currently call `.RequireAuthorization()`. This item adds the
  authentication *pipeline*; whether each endpoint should require authorization is a
  per-endpoint judgment call for the implementer/tasks, informed by whether these are
  read (`GetStockLevel`) vs. write (the other three) operations — writes should default to
  requiring authorization; call out any left unauthorized.
- **PaymentsService** — has **zero endpoints mapped**. This item's scope here is limited to
  **pipeline readiness**: register the authentication services, config keys, and middleware, so
  that when real endpoints are added in a future change they only need `.RequireAuthorization()`
  added, not the whole pipeline re-derived. No scenario describing "a protected endpoint returns
  401" can be written against real PaymentsService endpoints yet — the spec for this service
  is necessarily narrower (config + pipeline wiring only) than for the other two.

**Alternatives considered:**
- Add a shared `Shared.ServiceBus`/`SharedContracts`-style shared authentication extension
  project so all three services (and the Gateway) consume one `AddKeycloakJwtBearer(...)`
  helper — appealing for DRY, but out of scope for this change (introduces a new shared
  project, a bigger structural change than a bug-fix change should carry); noted as a
  reasonable follow-up once the duplicated config proves annoying in practice.

### D8: Antiforgery — required for mutating YARP-proxied routes, with an M2M Bearer bypass

**This corrects the previous framing of this decision**, which stated antiforgery was
"config cleanup only, no runtime logic change" and that CSRF protection was "never required for
YARP-proxied downstream calls." That was backwards: CSRF protection is meaningless if it only
guards Gateway-native endpoints (which currently have zero mutating routes) while the actual
state-changing traffic — product, stock, and payment mutations — flows through YARP unchecked
(see proposal.md's "Why" section for the corrected requirement this decision now implements).

**Decision:** antiforgery validation SHALL be required on mutating (`POST`/`PUT`/`PATCH`/`DELETE`)
requests to the YARP-proxied `products-route`, `stocks-route`, and `payments-route`, in addition
to any Gateway-native mutating endpoints, **except** when the request already carries a
machine-to-machine `Authorization: Bearer` header — those bypass antiforgery entirely, since
they are not cookie/browser-driven and carry no CSRF exposure. The bypass reuses the exact
Bearer-detection predicate already implemented in the YARP `AddRequestTransform` lambda in
`Program.cs` (an `Authorization` header present and starting with `Bearer `), extracted into a
small shared helper so the transform's check and the antiforgery check cannot drift apart.

**Implementation shape:**
- Custom middleware (`app.Use(async (context, next) => ...)`) registered after
  `app.UseAuthorization()` and before `app.MapReverseProxy()` — not `app.UseAntiforgery()` plus
  endpoint metadata/conventions (see D9 for why that approach doesn't work here).
- The middleware calls `next()` immediately for non-mutating methods (`GET`/`HEAD`/`OPTIONS`),
  calls `next()` immediately when the Bearer-bypass predicate is true, and otherwise resolves
  `IAntiforgery` and calls `ValidateRequestAsync`, short-circuiting with `400 Bad Request` (no
  downstream call made) on `AntiforgeryValidationException`.
- This replaces the broken, uncommitted WIP found in `Program.cs` — a `MapGroup("/api")` with
  `.RequireAuthorization()` and `.UseAntiforgery()` chained onto it. `UseAntiforgery()` is an
  `IApplicationBuilder` extension, not a valid `RouteGroupBuilder`/`IEndpointConventionBuilder`
  extension, so this does not compile; it also targeted the wrong URL prefix (`/api`), while the
  real proxied paths are `/products-api/...`, `/stocks-api/...`, `/payments-api/...` with no
  `/api` prefix. That WIP is removed outright, not repaired.
- The `Metadata` block cleanup is unchanged from the prior framing: remove the dead
  `"AntiforgeryRequired": "true"` entry from `products-route`, `stocks-route`, and
  `payments-route` in both `appsettings.json` and `appsettings.Development.json` (confirmed via
  grep: `route.Config.Metadata["AntiforgeryRequired"]` is never read anywhere in the codebase).
  This remains correct under the corrected framing too — enforcement is performed by the
  middleware above, not by route metadata, so the metadata was always dead weight regardless of
  which direction the requirement pointed.
- The antiforgery tracking cookie itself needs hardening: `AddAntiforgery(...)` today only sets
  `HeaderName`, leaving `options.Cookie` on framework defaults (`SameSite=Lax`,
  `SecurePolicy=SameAsRequest`). Add `options.Cookie.SameSite = SameSiteMode.Strict` and
  `options.Cookie.SecurePolicy = CookieSecurePolicy.Always` so it matches the posture already
  chosen for the `__Host-Shoppiness_bff` auth cookie. The cookie stays `HttpOnly = true`; the
  separate `XSRF-TOKEN` cookie, now written as a side effect of the `/bff/user` handler calling
  `IAntiforgery.GetAndStoreTokens` (see tasks.md 2.10), is unaffected and stays `HttpOnly = false` (it must
  remain readable by client-side script to be echoed into the `X-XSRF-TOKEN` header).

**Why this is load-bearing now, not just defense-in-depth:** today's deployment is genuinely
same-origin — the `angular-spa-fallback` YARP route proxies the Angular dev server through the
Gateway itself (D2), so the browser only ever talks to `https://localhost:5001`, and both the
auth cookie and the now-hardened antiforgery cookie carry `SameSite=Strict`. That already gives
substantial CSRF protection against genuinely cross-site requests today. But that protection is
a property of the *current* same-origin topology, not of the antiforgery wiring itself — it
would silently stop applying if the SPA's origin ever diverged from the Gateway's (e.g. served
from a separate host/CDN rather than proxied by `angular-spa-fallback`), without anyone
necessarily revisiting this decision at that point. Explicit application-level antiforgery
validation doesn't depend on that topology holding, which is why it is implemented as real
enforcement now rather than treated as unnecessary on the grounds that `SameSite=Strict` already
covers it.

Token issuance for the `XSRF-TOKEN` cookie is a real, load-bearing dependency of this design —
the SPA must obtain it once per session (e.g. immediately after login) before its first mutating
request. Rather than a standalone endpoint, this is now folded into the `/bff/user` handler (see
tasks.md 2.10): the SPA already must call `GET /bff/user` immediately after login (per `bff-user-claims`),
so piggybacking `XSRF-TOKEN` issuance onto that existing call avoids a redundant round trip. That
frontend reliance on `/bff/user` for both claims and CSRF-token issuance is out of scope for this
change and tracked in `frontend-bff-auth`.

**Side note — middleware-based token issuance considered, deferred:**

An alternative to folding `XSRF-TOKEN` issuance into `/bff/user` (see above) is a dedicated
middleware that issues/refreshes the cookie on any authenticated request lacking a valid one,
rather than tying issuance to a single endpoint.

- **Endpoint-based (chosen):** minimal, localized change; zero overhead on other requests;
  matches the stated goal of piggybacking on the SPA's existing post-login call to `/bff/user`.
  Weakness: mixes a cross-cutting security concern into a claims-returning endpoint, and token
  freshness depends entirely on `/bff/user` being re-called — if the antiforgery token/cookie
  expires mid-session and the SPA doesn't call `/bff/user` again (e.g. a long-lived tab with no
  reload), mutating requests start failing `400` until something re-triggers `/bff/user`.
- **Middleware-based (rejected for now):** decouples issuance from any specific endpoint,
  self-heals expired tokens on the next authenticated request regardless of route, and is
  architecturally symmetric with the D8/D9 enforcement middleware. Weakness: broader blast
  radius (runs across Gateway-native, YARP-proxied, and fallback routes alike), needs an explicit
  "skip if a valid cookie already exists" guard to avoid re-issuing/re-encrypting on every
  request, and requires careful ordering relative to the existing enforcement middleware and
  `app.UseAntiforgery()`.

**Follow-up (deferred, not addressed in this change):** the endpoint-based approach has a known
gap — token expiration is not proactively handled if `/bff/user` isn't re-called within a
session. A future change should address this, either by switching to the middleware-based
approach above, or by having the frontend's `csrf-interceptor` detect a `400` from antiforgery
validation and transparently re-call `/bff/user` before retrying the mutating request once.
Tracked here as a deferred design consideration, not a task in this change's `tasks.md`.

**Alternatives considered:**
- Keep antiforgery scoped to Gateway-native endpoints only, exempting all YARP-proxied calls
  (the original, now-corrected framing) — rejected as backwards; nearly all real state-changing
  traffic in this system flows through YARP-proxied routes, while Gateway-native endpoints
  currently have zero mutating routes at all, so the original framing left CSRF protection
  covering nothing that actually mutates state.
- Rely on `SameSite=Strict` cookie behavior alone as sufficient CSRF protection, and skip
  explicit antiforgery wiring for proxied routes — rejected as the sole mechanism; see the
  "load-bearing now" rationale above. Kept as complementary context/defense-in-depth, not a
  substitute for explicit validation.
- Enforce antiforgery inside the YARP `AddRequestTransform` lambda itself, alongside the
  existing Bearer-detection logic, instead of separate middleware — rejected; request
  transforms run after routing has already matched a destination cluster, so a rejection there
  would need to short-circuit deep inside YARP's forwarding pipeline instead of before it
  engages at all. A preceding middleware is the standard ASP.NET Core mechanism for "reject
  before this subsystem runs" and keeps the transform focused on request mutation, not
  authorization decisions.

### D9: Antiforgery enforcement via custom middleware, not endpoint metadata/conventions

**Decision:** implement the D8 antiforgery check as custom middleware (`app.Use(...)`)
registered ahead of `app.MapReverseProxy()`, rather than via `app.UseAntiforgery()` combined
with endpoint metadata or an opt-in convention (e.g. a hypothetical `RequireAntiforgeryValidation()`
mirroring `RequireAuthorization()`).

**Rationale:**
1. `MapReverseProxy()`-mapped endpoints never carry `IAntiforgeryMetadata`, and ASP.NET Core's
   antiforgery middleware only validates endpoints that carry that metadata. This .NET version
   exposes no public opt-in convention analogous to `RequireAuthorization()` for antiforgery —
   only a public opt-out, `DisableAntiforgery()`, exists (for exempting individual endpoints
   from a blanket requirement, not for opting specific ones in). There is no supported way to
   attach an antiforgery-required convention to YARP's dynamically-generated proxy endpoints.
2. Even if such a convention existed, endpoint metadata is evaluated once, per endpoint, at
   endpoint-build time — it is static and cannot express "except when *this specific request*
   already carries an `Authorization: Bearer` header," which is the M2M bypass D8 requires. That
   bypass is a per-request, runtime condition, not a per-endpoint, static one. A metadata-based
   approach would still need a second, request-scoped mechanism layered on top to implement the
   bypass, at which point the metadata layer adds indirection without functional benefit over a
   single middleware that already has access to the full `HttpContext` for every request.
3. Custom middleware naturally composes all three checks this decision needs — the mutating-method
   skip, the Bearer-bypass, and the `ValidateRequestAsync` call — in one place ahead of the proxy,
   with no dependency on YARP internals or on a future antiforgery/YARP integration that does not
   currently exist.

**Alternatives considered:**
- `app.UseAntiforgery()` + a hypothetical `RequireAntiforgeryValidation()` endpoint convention —
  rejected; does not exist in this .NET version (only the `DisableAntiforgery()` opt-out exists),
  and even if it did, it could not express the per-request Bearer-bypass condition (rationale 2
  above).
- Route-group-scoped `UseAntiforgery()` — the discovered broken WIP
  (`app.MapGroup("/api")...UseAntiforgery()`) — rejected; does not compile, since
  `UseAntiforgery()` is an `IApplicationBuilder` extension, not a
  `RouteGroupBuilder`/`IEndpointConventionBuilder` extension; it also targeted the wrong URL
  prefix (`/api` instead of the real `/products-api`, `/stocks-api`, `/payments-api` paths).
- Enforcing the check inside the YARP request transform instead of separate middleware —
  rejected for the same reason noted under D8's alternatives: transforms run after routing has
  matched a cluster, which is the wrong place to reject a request outright.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| D1's fix was chosen from a code trace only, no running instance | tasks.md includes an explicit empirical-verification task (curl against a live Gateway + Keycloak) before this item is considered done, not just implemented |
| D4's `prompt=register` may not be honored by Keycloak's hosted theme | tasks.md includes an explicit empirical-verification task; D4 documents two fallback approaches to implement if verification fails |
| D5 (claims from access token) assumes the access token carries every claim the frontend needs | Verify claim parity against `GetClaimsFromUserInfoEndpoint = true`'s actual claim set during implementation; `/userinfo` fallback documented if a gap is found |
| D7 changes downstream services' request-handling behavior for the first time (previously anything was accessible with no auth check) | This is the intended fix, not a regression, but implementation must confirm no currently-working manual/integration test relies on unauthenticated access to these endpoints |
| D7's ProductsService finding (existing `.RequireAuthorization()` with no auth pipeline registered) suggests that endpoint may already throw at request time today | Flag this explicitly as a pre-existing defect this change happens to fix, not a new risk introduced by this change |
| D6's header-based signal is a new contract the frontend must be updated to read | Out of scope here by design (frontend consumption belongs to `frontend-bff-auth`), but the exact header name (`X-Shoppiness-Session-Expired`) must be communicated/kept in sync with that change |
| D2's CORS removal assumes no other, currently-unknown consumer of cross-origin requests exists | Grepped confirmed no other route in `appsettings*.json` needs cross-origin access; the Angular dev server is proxied same-origin by design |
| D8's antiforgery enforcement is new runtime behavior on every mutating proxied request, not just config cleanup | tasks.md includes manual verification steps (400 without a token, success with a valid token, bypass with a Bearer header, no effect on GET); the M2M bypass predicate is shared with the YARP transform's existing check (D8/D9) so the two cannot drift apart |

## Migration Plan

Backend-only, no data migrations. Suggested implementation/rollout order (elaborated in
tasks.md): config/cleanup items first (D2 — low risk, no behavior dependencies; D8/D9 —
introduces real enforcement logic and cookie hardening, but is independent of the other
decisions and safe to land early), then D1
(challenge scheme — affects every protected route, verify early since later items depend on
auth actually working end-to-end), then D3/D4 (small, independent endpoint fixes), then D5
(claims fix — depends on D1 being correct so `/bff/user` is reachable in the right shape),
then D6 (refresh-expiry signal — depends on D5's `TokenRefreshService` call pattern), then D7
(downstream validation — largest scope, independent of the Gateway-side items but should land
after D1 so the Gateway's own auth boundary is confirmed sound first). No rollback complexity
beyond standard git revert per item; no shared/external state changes except Keycloak
realm config, which is only touched if D4's fallback requires it (not assumed necessary).

## Open Questions

- Does D1's empirical verification match the predicted behavior? Blocks closing item 1.
- Does D4's empirical verification confirm `prompt=register` works as-is? Blocks needing the
  documented fallback.
- Does the access token (D5) actually carry `email` and a display-name-suitable claim given
  current Keycloak client scope configuration (`Scope.Add("roles-only")` is notably not the
  standard `profile`/`email` scopes) — **this needs verification during implementation**; if
  `email`/name claims are absent from the access token because only `openid`, `roles-only`, and
  `offline_access` scopes are requested, D5's primary approach may need the `/userinfo` fallback
  after all, or the `Scope` list may need `profile`/`email` added (a small, separate follow-on
  decision to make during implementation, not pre-decided here).
- Should PaymentsService (D7) get a placeholder authenticated health/ping endpoint in this
  change to prove the pipeline works end-to-end, given it has no real endpoints yet? Left as an
  implementation-time judgment call, default leaning "no" (adding a throwaway endpoint just to
  test config is itself scope creep) unless the empirical verification step for D7 needs it to
  produce a pass/fail signal.
</content>
