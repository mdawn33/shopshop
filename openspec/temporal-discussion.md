# Temporal Discussion — frontend-auth to BFF/OIDC realignment

Status: exploration only. Nothing implemented, no OpenSpec artifacts created/edited yet.
Purpose: resume point for adapting frontend-auth to the real BFF/OIDC backend.

## 1. Why this doc exists

The backend auth approach changed since `openspec/changes/frontend-auth/` was written. It now
uses a **BFF pattern with OIDC redirect to Keycloak** (not manual login/register endpoints, not
frontend-driven token refresh). This session explored the gap between the old spec, a second
existing change (`gateway-bff-auth`), and the actual code, and worked through open questions
with the user. Decision reached: **ditch `frontend-auth`**, create a new superseding change
**`frontend-bff-auth`** once remaining backend questions are resolved.

## 2. Sources of truth found (and how they disagree)

Four different descriptions of the auth flow exist in this repo simultaneously:

| Source | Login mechanism | Endpoints | Cookie | Refresh |
|---|---|---|---|---|
| `openspec/changes/frontend-auth/design.md` + `specs/*` | Angular POSTs `{email,password}` to BFF | `/login`, `/register`, `/logout`, `/refresh`, `/user` (ROPC-style) | HttpOnly, unnamed | Frontend-orchestrated: `refreshToken()`, `refreshInProgress`, retry queue |
| `openspec/changes/gateway-bff-auth/proposal.md` + `design.md` | OIDC redirect (Keycloak hosted login); explicitly rejects the 5 manual endpoints | `/signin-oidc`, `/signout-callback-oidc` only | `__Host-Shoppiness_bff`, `SameSite=Strict`, 10 min (design.md says 10; code says 15 - needs reconciling) | Backend-only, proactive, inside YARP (`TokenRefreshService`) |
| `openspec/changes/gateway-bff-auth/specs/*.md` | Contradicts its own design.md - describes the same ROPC/5-endpoint model as `frontend-auth` | `/auth/*`, tokens stored as claims | `SameSite=Lax`, 60 min | Frontend calls `/auth/refresh` |
| Actual code (ground truth per user decision, see section 4) | OIDC redirect via `Results.Challenge` | `/bff/login`, `/bff/logout`, `/bff/user`, `/bff/register`, `/bff/refresh` (dead), `/api/antiforgery/token` | `__Host-Shoppiness_bff`, `SameSite=Strict`, 15 min | Backend-only via `TokenRefreshService`; frontend never calls `/bff/refresh` |

**Decision: actual backend code is the single source of truth.** `gateway-bff-auth/design.md`
gets corrected where it drifted from code (e.g. 10 to 15 min expiry). `gateway-bff-auth/specs/*`
describe a plan that was abandoned mid-project and need to be rewritten/archived to match reality.

Frontend code has *also* already drifted ahead of `frontend-auth`'s spec (see section 5) - it was
hand-edited outside the OpenSpec workflow and is currently half-finished/commented-out.

## 3. Files read this session (for quick re-navigation)

Backend:
- `backend/src/Gateway.Api/Program.cs`
- `backend/src/Gateway.Api/Endpoints.cs`
- `backend/src/Gateway.Api/Extensions/AuthenticationExtension.cs`
- `backend/src/Gateway.Api/appsettings.Development.json` (ReverseProxy routes/clusters section)

Frontend:
- `web-client/src/app/core/services/auth.ts`
- `web-client/src/app/core/guards/auth.ts`
- `web-client/src/app/core/interceptors/auth.ts`
- `web-client/src/app/core/interceptors/error.ts`
- `web-client/src/app/app.config.ts`
- `web-client/src/app/app.routes.ts`
- `web-client/src/app/app.ts`
- `web-client/src/environments/environment.ts`, `environment.model.ts`, `environment.development.ts`

OpenSpec:
- `openspec/changes/frontend-auth/proposal.md`, `design.md`, `tasks.md`
- `openspec/changes/frontend-auth/specs/auth-service-real/spec.md`
- `openspec/changes/frontend-auth/specs/auth-interceptors/spec.md`
- `openspec/changes/frontend-auth/specs/login-ui/spec.md`
- `openspec/changes/frontend-auth/specs/register-ui/spec.md`
- `openspec/changes/frontend-auth/specs/toast-infrastructure/spec.md`
- `openspec/changes/frontend-auth/specs/environment-config/spec.md`
- `openspec/changes/gateway-bff-auth/proposal.md`, `design.md`, `tasks.md`
- `openspec/changes/gateway-bff-auth/specs/bff-auth-endpoints/spec.md`
- `openspec/changes/gateway-bff-auth/specs/bff-cookie-auth/spec.md`
- `openspec/changes/gateway-bff-auth/specs/bff-cors-policy/spec.md`
- `openspec/changes/gateway-bff-auth/specs/bff-token-forwarding/spec.md`
- `openspec/changes/gateway-bff-auth/specs/downstream-jwt-validation/spec.md`

`openspec list --json` at session start:
- `gateway-bff-auth`: 30/30 tasks, status "complete", not archived
- `frontend-auth`: 0/49 tasks, status "in-progress" (never actually implemented)
- `frontend-foundation`: 35/35, complete (this is what stubbed Auth/AuthInterceptor/ErrorInterceptor originally)

## 4. What frontend-auth gets wrong (assumes direct-IdP / token-handling patterns that no longer apply)

- **design.md D3 (Refresh Race Condition Guard)** and **D4 (APP_INITIALIZER -> GET /user
  rehydrate)** - the `refreshInProgress` flag, `refreshComplete$` Subject queue, and
  frontend-triggered `POST /refresh` have no backend counterpart. Refresh is fully silent,
  server-side, inside YARP's `TokenRefreshService`, on every proxied request. Confirmed dead
  by user: `/bff/refresh` exists but is never called by the frontend.
- **login-ui / register-ui capabilities in full** - LoginComponent/RegisterComponent as
  reactive forms that POST credentials don't fit OIDC. Login/registration is Keycloak's hosted
  UI; Angular's job is just `window.location.href = '/bff/login'` / `/bff/register`. Current
  code already does this correctly (see section 5).
- **auth-service-real spec's method signatures** - `login(email,password)`,
  `register(email,password,displayName)` as promise-returning HTTP calls are gone.
- **environment-config's authServiceUrl** - superseded; one `apiGatewayUrl` now, not four
  per-service URLs (there's no separate auth service).
- **toast-infrastructure** - still a valid pattern, doesn't exist in code yet, needs
  rescoping from "form validation feedback" to "session/auth state feedback".

## 5. Actual frontend code state (already diverged from frontend-auth spec, independently)

- `Auth` service (`core/services/auth.ts`): no `login()/register()/logout()` HTTP calls - all
  three are full-page redirects (`window.location.href` to `/bff/login`, `/bff/register`,
  `/bff/logout`). `checkSession()` calls `GET /bff/user`, expects `Claim[]`, sets a
  `userClaims` signal. No `refreshToken()` method, no `refreshInProgress` - matches the
  "refresh is backend-only" decision already.
- `authInterceptor`: only attaches `withCredentials: true`. Old Bearer-header logic is
  commented out. Matches BFF intent.
- `errorInterceptor`: maps errors to `AppHttpError` and rethrows. No 401 handling, no
  refresh-then-retry, no redirect. Needs new logic per section 7.
- `AuthGuard` (`core/guards/auth.ts`): currently `return true` (disabled for testing only,
  per user - the commented-out real logic below it, which calls `checkSession()` then
  `auth.login(state.url)` on failure, is the intended implementation).
- `claimGuard`: has real logic (`checkSession()` + `hasClaim()` + redirect to `/unauthorized`
  on failure) - per user, this is used specifically for account-protected routes.
- `APP_INITIALIZER` (`provideAppInitializer` in `app.config.ts`): imported but the actual call
  to rehydrate session state is commented out - currently a no-op. Needs wiring.
- No ToastService/ToastComponent exist anywhere in the codebase.
- No `/unauthorized` route/component exists (commented out in `app.routes.ts`, but referenced
  by `claimGuard`).
- `environment.model.ts` already has just `apiGatewayUrl` (not `authServiceUrl`) - matches the
  BFF-collapsed-to-one-gateway reality.
- Auth routes (`features/auth/auth.routes.ts`) is an empty array - no login/register
  components wired (correctly so, since there shouldn't be any per OIDC).

## 6. Concrete backend bugs/gaps found (not spec drift - real defects, independent of which OpenSpec doc is "correct")

1. **GET /bff/user likely returns [] even when authenticated.** `OnTicketReceived` in
   `AuthenticationExtension.cs` strips *all* claims from the cookie identity (to keep the
   cookie small), but `Endpoints.cs`'s `/bff/user` handler does
   `context.User.Claims.Select(...)` - empty claims, but `Identity.IsAuthenticated` stays
   `true` (only depends on `AuthenticationType`, not claim count), so it's `200 OK` with `[]`,
   not 401. Per user decision (open question 3): frontend proposal should assume this gets
   fixed to return correct claims - treated as a backend prerequisite, not something the
   frontend designs around.
2. **`app.UseCors("AngularDevPolicy")` runs in Program.cs but the corresponding
   `AddCors(...)` registration is commented out.** Will throw at startup or produce no CORS
   headers. Live regression vs. the bff-cors-policy spec.
3. **Challenge-vs-401 ambiguity (see section 7 for full deep dive) - now known to affect every
   proxied route**, not just Gateway-native endpoints, because of finding in 7.3.
4. **"AntiforgeryRequired": "true" metadata on all three proxy routes
   (products-route, stocks-route, payments-route) is dead config - RESOLVED, safe to delete.**
   Grepped the whole Gateway.Api project, nothing reads
   `route.Config.Metadata["AntiforgeryRequired"]`. User confirmed (2026-07-22): the antiforgery
   header is scoped to the Gateway's own endpoints only; downstream/proxied services don't need
   it yet. So this metadata is a stale leftover, not an unwired future intent - remove it from
   the three proxy routes in `appsettings.Development.json` (and any other environment configs
   that carry it).
5. **Naming**: none of the three OpenSpec documents match real routes (`/bff/login`,
   `/bff/logout`, `/bff/user`, `/bff/register`, `/bff/refresh`, `/api/antiforgery/token`). Any
   new spec must be written against this actual surface.

## 7. Deep dive: the Challenge-vs-401 problem (open question 5 - NOT yet resolved)

### 7.1 The mechanism

ASP.NET Core asks two different questions at two different times:

1. AUTHENTICATE (app.UseAuthentication(), every request)
   "Who is this?" -> uses DefaultAuthenticateScheme = "smart"
   Bearer header present?  -> JwtBearer populates HttpContext.User
   No Bearer header?       -> Cookie populates HttpContext.User
   This works as intended.

2. CHALLENGE (only fires if .RequireAuthorization() fails)
   "How do we ask them to log in?"
   -> uses DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme, ALWAYS
   -> "smart"'s Bearer-header selector is NEVER consulted here - no ForwardChallenge or
      ForwardDefaultSelector override wired for the Challenge action, and
      DefaultChallengeScheme is a hardcoded literal ("OpenIdConnect"), not "smart".

Grepped the whole Gateway.Api project for `smart`, `X-Requested-With`, `Accept`,
`ForwardChallenge`, `DefaultChallengeScheme` - the only two references to "smart" are the
DefaultAuthenticateScheme assignment and the AddPolicyScheme registration. No code makes a
failed-authorization Challenge route through "smart"'s Bearer-header check.

**Practical consequence (as read from source - not yet empirically verified):** every failed
`.RequireAuthorization()` - whether the caller sent a Bearer token, a cookie, or nothing -
gets challenged via OIDC and 302-redirected to Keycloak, not 401'd.

**User's stated intent:** "the OnRedirectToLogin event was removed because we want to
challenge when the calls come from Angular, and we need to return 401 when it comes from an
API, and that's why I am using the policy scheme 'smart'." This intent is reasonable, but per
the trace above, "smart" as currently wired only governs Authenticate dispatch, not
Challenge dispatch - so the intended behavior likely isn't fully realized yet. This needs
empirical verification before concluding either way (suggested test below).

### 7.2 Suggested verification (not yet run)

    curl -i http://localhost:<gateway-port>/products-api/products   # no cookie, no Bearer

If the trace above is correct: expect 302 to the Keycloak authorize URL. If the user's
mental model already holds in practice: expect a bare 401. This determines whether 7.4's
fix is actually needed.

### 7.3 Scope is bigger than first thought

Originally scoped this as only affecting `GET /api/antiforgery/token` (the one Gateway-native
.RequireAuthorization() endpoint besides the now-dead /bff/refresh). But the YARP route
config in appsettings.Development.json has:

    "products-route":  { ..., "AuthorizationPolicy": "default" },
    "stocks-route":    { ..., "AuthorizationPolicy": "default" },
    "payments-route":  { ..., "AuthorizationPolicy": "default" }

"AuthorizationPolicy": "default" is YARP's reserved keyword for "require an authenticated
user via ASP.NET Core's built-in default policy" - meaning the Gateway enforces auth on
every proxied route already, not just the two Gateway-native endpoints. So if the Challenge
issue in 7.1 is real, it affects every single API call the Angular app makes when the session
is missing/expired - this is the central case, not an edge case. (Also means downstream
services' own JwtBearer validation is a second, redundant layer behind this Gateway-level
check - not necessarily a problem, just worth knowing.)

### 7.4 Candidate fixes discussed

- **A2 - CHOSEN.** Wire `DefaultChallengeScheme = "smart"` so Challenge dispatch consults the
  same Bearer-vs-Cookie policy that Authenticate already uses, instead of the hardcoded
  `OpenIdConnectDefaults.AuthenticationScheme` literal. Preserves the user's original intent:
  Angular (cookie-based, no Bearer header) gets challenged with a redirect to Keycloak's hosted
  login; direct API/service callers (Bearer header present) get JwtBearer's default challenge,
  which is already a bare 401 - no override needed on that side. Cookie's own challenge handler
  keeps its default redirect behavior, since that redirect *is* the desired outcome for
  browser/Angular callers.
- **A (plain) - rejected in favor of A2.** Always-401-on-Challenge (drop the OIDC auto-redirect
  entirely, let Angular decide when to navigate to /bff/login) was the other shape of "A"
  considered. Not chosen - user confirmed the Bearer-vs-Cookie distinction is the desired
  behavior, not a simplification target.
- **B - Frontend guard:** gate any protected Gateway-native call behind
  `auth.isAuthenticated()` (already-hydrated state) before firing. Shrinks the race window,
  doesn't eliminate it. Worth doing regardless, as defense-in-depth, not a fix on its own.
- **C - Frontend fetch(..., { redirect: 'manual' }) workaround:** turns an unfollowed 302
  into a detectable opaqueredirect response instead of a CORS-blocked network error.
  Angular's withFetch() HttpClient backend doesn't obviously expose the redirect option
  through its public API - would need a raw fetch() call outside the normal interceptor
  pipeline for just this case. Not needed now that A2 makes the Gateway itself return 401 to
  API-style/XHR/fetch callers.
- **D - status === 0 treated as "unauthenticated" in ErrorInterceptor:** rejected -
  status: 0 is a generic bucket (CORS failure, DNS failure, offline, ad-blockers); would
  cause false "session expired" redirects for unrelated network problems.

**Status: decided (2026-07-22), not yet implemented.** Empirical verification (7.2) was
skipped per user - `products-api` isn't runnable locally yet. Fix chosen from the code trace in
7.1 alone: wire `DefaultChallengeScheme = "smart"`. This is a **backend prerequisite** for
frontend-bff-auth (see section 10) - needs its own small OpenSpec change (addendum to
gateway-bff-auth, or a tiny standalone change) before/alongside the frontend proposal.

## 8. All resolved open questions (from the original 7 + the "ditch frontend-auth" decision)

1. **Source of truth:** actual backend code, always. gateway-bff-auth/design.md gets
   corrected where stale (e.g. cookie expiry 10->15 min). gateway-bff-auth/specs/* get
   rewritten to match code (currently describe an abandoned ROPC/5-endpoint plan). Login always
   redirects the end user to the IdP's (Keycloak's) hosted login screen. Token refresh is
   backend-only.
2. **POST /bff/refresh is dead code** - frontend never calls it; refresh is fully silent,
   server-side.
3. **GET /bff/user will be fixed to return correct claims** (id/email/displayName) - frontend
   proposal assumes this is already true / will be true, not something to model workarounds
   around. (Backend prerequisite - bug noted in 6.1.)
4. **CSRF (X-XSRF-TOKEN) only needed for Gateway-native POST endpoints, never for proxied
   downstream calls - confirmed again (2026-07-22), and the stale `AntiforgeryRequired`
   metadata on the three proxy routes (6.4) is now confirmed dead/removable, not an unwired
   future intent.** Also noted: currently there are zero Gateway-native POST endpoints Angular
   actually calls (login/register/logout are GET redirects, refresh is dead) - CSRF wiring is
   forward-looking infrastructure with no current consumer.
5. **Challenge-vs-401 (302 redirect) problem - RESOLVED (2026-07-22), fix chosen without
   empirical verification** (curl test in 7.2 skipped - `products-api` not runnable locally
   yet). Fix: wire `DefaultChallengeScheme = "smart"` (variant A2 in 7.4) so Angular
   (cookie-based) gets challenged with a Keycloak redirect and API/Bearer callers get a bare
   401. Treated as a backend prerequisite for frontend-bff-auth - not yet implemented. See
   section 7.
6. **Cookie expiry stays at 15 minutes (the value already in code, not design.md's stale 10).**
   When Keycloak's refresh token itself expires/is revoked (so TokenRefreshService can no
   longer silently refresh), show a toast ("Your session has expired") before redirecting to
   /bff/login.
7. **authGuard's commented-out logic is the real target implementation** - the `return true`
   is a temporary testing stub. Real behavior: check isAuthenticated()/checkSession(), call
   auth.login(state.url) on failure. **claimGuard is used specifically for
   account-protected routes** (not general auth-required routes - that's authGuard's job).
8. **Ditch openspec/changes/frontend-auth/ entirely.** Create a new superseding change,
   working name **frontend-bff-auth**, once remaining questions (mainly section 7) are resolved.

## 9. Proposed capability shape for frontend-bff-auth (BUILT - see openspec/changes/frontend-bff-auth/)

Generated 2026-07-22 by sa-sdd-propose. proposal.md/design.md/specs/tasks.md all created and
`openspec validate frontend-bff-auth --strict` passes. `openspec/changes/frontend-auth/` was
confirmed untouched (empty git diff) - it still needs the archive/delete step in section 10.

design.md landed 6 decisions (D1-D6): guard activation, `APP_INITIALIZER` wiring, the
interim-vs-post-fix `auth-error-handling` behavior (explicitly rejects inferring session-expiry
from `status === 0`, per option D in 7.4), CSRF scoping, toast scoping, single-URL env model.
Plus a risk table and rollout order in tasks.md (7 task groups, dependency-ordered, ending in a
verification step that frontend-auth/ is untouched).

Capability shape as actually built (all seven, unchanged from the sketch below):

- `auth-state-service` - signal-based session state hydrated from GET /bff/user, exposing
  isAuthenticated/currentUser, replacing auth-service-real.
- `auth-redirect-triggers` - thin wrappers around window.location redirects to /bff/login,
  /bff/register, /bff/logout (already mostly implemented in code), replacing
  login-ui/register-ui.
- `session-guard` - real authGuard (general auth) + claimGuard (account-protected routes)
  logic, replacing the current no-op/partial implementation.
- `csrf-interceptor` - fetch-and-attach X-XSRF-TOKEN for Gateway-native POSTs only; currently
  no real consumer, forward-looking infra. Pending resolution of 6.4 contradiction.
- `auth-error-handling` - 401/403 UX rules; depends on section 7 being resolved first, since it
  determines whether ErrorInterceptor can treat all 401s uniformly or needs special-casing.
- `toast-infrastructure` - kept, rescoped to auth/session feedback (e.g. session-expired
  message per open question 6).
- `environment-config` - drop authServiceUrl; confirm apiGatewayUrl is the only URL needed
  (already true in code).

## 10. Next steps

1. ~~Resolve section 7 (Challenge-vs-401)~~ - DONE (2026-07-22). Fix decided: wire
   `DefaultChallengeScheme = "smart"` (7.4, A2). Empirical curl test (7.2) skipped -
   `products-api` not runnable locally yet. Still needs an actual OpenSpec change (small
   addendum to gateway-bff-auth, or its own tiny change) before implementation - not written
   yet.
2. ~~Resolve 6.4 (AntiforgeryRequired metadata contradiction)~~ - DONE (2026-07-22). Confirmed
   dead/removable: antiforgery is Gateway-native-endpoints-only, not needed downstream yet.
3. **DECIDED (2026-07-22): frontend spec first.** Draft frontend-bff-auth now via
   sa-sdd-propose; the auth-error-handling capability (section 9) will document the
   `DefaultChallengeScheme = "smart"` fix (7.4) and the AntiforgeryRequired cleanup (6.4) as an
   explicit unimplemented backend dependency/prerequisite rather than blocking on it. The
   backend fix itself gets its own gateway-challenge-fix change (sa-sdd-propose +
   sdd-backend-implementer), scheduled separately/in parallel.
4. ~~Delegate to sa-sdd-propose to generate frontend-bff-auth's proposal.md/design.md/specs/
   tasks.md~~ - DONE (2026-07-22). See section 9. Validated clean, frontend-auth/ untouched.
5. ~~Draft the separate `gateway-challenge-fix` backend change~~ - SUPERSEDED (2026-07-22).
   Folded into the broader `gateway-bff-auth-part-2` change (see section 11) instead of
   shipping as its own small change, per user decision - avoids two changes touching the same
   Gateway.Api areas in parallel.
6. Correct gateway-bff-auth/design.md (10->15 min expiry, etc.) and rewrite/archive
   gateway-bff-auth/specs/* to match actual code, per decision in 8.1. Not yet started.
7. Delete/archive openspec/changes/frontend-auth/ per decision in 8.8. Not yet started -
   confirmed untouched by the frontend-bff-auth work in step 4.
8. **Next action.** Delegate to sa-sdd-propose to build `gateway-bff-auth-part-2` (see section
   11) - not yet started.

## 11. Gateway.Api security gap analysis (2026-07-22) -> gateway-bff-auth-part-2

Ran a second sa-sdd-explore pass specifically re-verifying Gateway.Api's security surface
against 8 required areas (login, logout, registration, user-info endpoint, refresh, JWT
forwarding, XSRF, cookie management), re-checking code directly rather than trusting this doc's
earlier claims. Two things had changed since section 7's investigation:

- **JWT forwarding to downstream services is now actually implemented** (`Program.cs` YARP
  `AddRequestTransform`, attaches a valid Bearer token from `TokenRefreshService` on every
  proxied request for cookie-based callers). The prior open question ("does this do real work or
  just pass the cookie through?") is resolved: it does real work.
- **`TokenRefreshService` is fully implemented** (30s-buffer expiry check, real refresh POST to
  Keycloak, re-signs the cookie).

Findings, one per required area:

1. **Login** - sound, no gap. Blocked transitively by item 8's CORS issue until that's fixed.
2. **Logout** - works; `redirectUrl` query param accepted but never used (hardcoded to `/`).
   Cosmetic gap - include in part-2 tasks.
3. **Registration** - uses `prompt=register`, which is **not a standard OIDC prompt value**;
   whether Keycloak's hosted theme honors it is unverified against a running instance. Needs
   empirical test - include in part-2 tasks.
4. **`GET /bff/user`** - confirmed live bug (same as section 6.1): `OnTicketReceived` strips all
   claims before persisting the cookie identity, so this always returns `200 []` even when
   authenticated. Breaks all claim-based frontend logic (`claimGuard` can never pass). Include in
   part-2 tasks.
5. **Refresh token** - mechanism itself is solid. Gap: no signal to the SPA when the refresh
   token itself (not just the access token) is expired/revoked at Keycloak - no coordinated
   "session truly over, please log in again" UX exists. Include in part-2 tasks.
6. **JWT forwarding to downstream services** - Gateway-side forwarding works (see above), but
   **none of ProductsService, StocksService, or PaymentsService validate the token at all** -
   zero `AddAuthentication`/`AddJwtBearer`/`RequireAuthorization` in any of the three.
   PaymentsService has no endpoints mapped yet at all. This is the `downstream-jwt-validation`
   capability that gateway-bff-auth/specs/* described but never got built (per section 2/6) -
   confirmed still fully unimplemented. Largest scope item - include in part-2 tasks.
7. **XSRF/CSRF** - antiforgery service is registered (`AddAntiforgery`, token-issuance endpoint
   exists) but completely disconnected from the frontend - `web-client/src` has zero references
   to XSRF/antiforgery anywhere, no interceptor reads/attaches the token. Low urgency only
   because there are currently no state-changing Gateway-native endpoints, but needs real wiring
   before any exist. Also subsumes the already-known dead `AntiforgeryRequired` YARP metadata
   cleanup (6.4). Include in part-2 tasks.
8. **Cookie management** - cookie config itself is solid (`__Host-` prefix, SameSite=Strict,
   HttpOnly, Secure, 15 min absolute expiry, no sliding). Initially flagged as a probable-crash
   bug: `app.UseCors("AngularDevPolicy")` runs unconditionally while the matching `AddCors(...)`
   is commented out. **User correction (2026-07-22, confirmed via appsettings.json review):**
   this isn't a "CORS never got wired up" bug - CORS is genuinely not needed here. The
   `angular-spa-fallback` YARP route proxies the Angular dev server (`http://localhost:4200/`)
   itself through the Gateway, so the browser only ever talks to one origin
   (`https://localhost:5001`) - true same-origin setup by design. **Correct fix: remove the
   orphaned `app.UseCors("AngularDevPolicy")` call entirely** (and the unused
   `AngularDevPolicy`/`BFF:FrontendOrigin` config if nothing else references it), not register
   `AddCors`. Still a real defect (referencing an unregistered policy name is either dead code or
   a startup-time crash risk depending on ASP.NET Core's exact resolution timing - not
   empirically run either way) - include in part-2 tasks, corrected framing.

**Decision (2026-07-22):** the previously-planned standalone `gateway-challenge-fix` change
(section 10, old step 5) is superseded/folded into one comprehensive change,
**`gateway-bff-auth-part-2`**, covering all of: the Challenge-vs-401 fix
(`DefaultChallengeScheme = "smart"`, section 7/7.4/A2), the corrected CORS cleanup (item 8
above), and items 2-7 above (logout redirectUrl, registration prompt verification, `/bff/user`
claims fix, refresh-expiry UX signal, downstream JWT validation in all 3 services, real
XSRF/CSRF wiring including the AntiforgeryRequired metadata removal from 6.4). Rationale: avoids
two changes touching overlapping areas of Gateway.Api in parallel. Not yet built - next action
is delegating to sa-sdd-propose.
