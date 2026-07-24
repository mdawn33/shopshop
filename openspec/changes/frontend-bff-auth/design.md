## Context

The Gateway (`backend/src/Gateway.Api`) implements a BFF pattern: Angular never sees Keycloak or
tokens directly. Login/register/logout are full-page redirects to `/bff/login`, `/bff/register`,
`/bff/logout`; session state is an `__Host-Shoppiness_bff` cookie (`SameSite=Strict`, 15 minute
expiry); token refresh happens entirely server-side inside YARP's `TokenRefreshService` on every
proxied request — the frontend never calls `/bff/refresh` (it exists but is dead code). Session
claims are read via `GET /bff/user`. CSRF protection (`X-XSRF-TOKEN`, fetched from
`GET /api/antiforgery/token`) applies only to Gateway-native POST endpoints, never to proxied
downstream calls (products/stock/payment APIs go through YARP, which doesn't require it).

`openspec/changes/frontend-auth/` was written against an earlier, since-abandoned backend design
(ROPC-style credential POSTs, frontend-orchestrated refresh) and is superseded by this change.
Frontend code has already independently drifted toward the real BFF model — `Auth`
(`core/services/auth.ts`) already does redirect-based login/register/logout and
`GET /bff/user` hydration; `authInterceptor` already just attaches `withCredentials: true`. What
remains unfinished: `authGuard`'s real logic is commented out behind a `return true` stub,
`APP_INITIALIZER` rehydration is commented out, `errorInterceptor` does no 401/403 handling, no
`ToastService` exists, and no `/unauthorized` route exists.

A known backend defect blocks part of this change from being fully exercised: the Gateway's
`DefaultChallengeScheme` is hardcoded to `OpenIdConnect`, so every failed
`.RequireAuthorization()` — on Gateway-native endpoints and on all three proxied routes
(products/stocks/payments, which carry `"AuthorizationPolicy": "default"`) — currently redirects
to Keycloak (302) instead of returning a bare 401, regardless of caller type. The fix
(`DefaultChallengeScheme = "smart"`) is tracked as a separate `gateway-challenge-fix` change and
is **not** part of this change's scope; `auth-error-handling` is designed to degrade gracefully
in both the current and post-fix states.

## Goals / Non-Goals

**Goals:**
- Finish wiring the frontend auth pieces that are stubbed/commented out, matching the BFF/OIDC
  model that is already ground truth on the backend and already partially reflected in code.
- Give `errorInterceptor` explicit, documented behavior for 401/403 that works today (redirect
  world) and continues to work once `gateway-challenge-fix` ships (bare-401 world for API-style
  callers) — without requiring a second frontend change when that fix lands.
- Introduce minimal, purpose-scoped `ToastService`/`ToastComponent` for session/auth feedback
  only (not a general-purpose notification system).
- Introduce a minimal, forward-looking CSRF interceptor even though it has zero current callers,
  so it doesn't need to be re-derived later; keep it small.

**Non-Goals:**
- No login/register forms, no `login(email, password)` / `register(...)` HTTP methods — Keycloak's
  hosted UI is the login/registration surface.
- No frontend-orchestrated refresh: no `refreshToken()`, no `refreshInProgress` flag, no retry
  queue, no call to `POST /bff/refresh`. Refresh is 100% backend-side.
- Not implementing or blocking on `gateway-challenge-fix` — that is a separate backend change.
  This change documents the dependency and defines interim behavior only.
- Not implementing or blocking on the `GET /bff/user` empty-claims bug fix — assumed fixed;
  no frontend workaround designed for the current `200 []`-while-authenticated behavior.
- No general-purpose toast/notification system for form validation, success messages, etc. —
  scope is session/auth feedback only in this change.

## Decisions

### D1: `authGuard` activates unconditionally protected routes; `claimGuard` handles claim-scoped ones

**Decision:** Un-comment and activate `authGuard`'s real logic (check `isAuthenticated()`, else
`checkSession()`, else redirect via `auth.login(state.url)`), applied to routes that only require
*some* authenticated user (currently `/cart`). Keep `claimGuard(claimType, allowedValue)` for
routes needing a specific claim (currently `/checkout`, `/orders` via `claimGuard('role',
'customer')`), which already has the correct real logic in code today.

**Rationale:** Matches existing route wiring in `app.routes.ts` and the user's stated intent
that `claimGuard` is for account-protected routes specifically, not general auth. No new guard
type needed — both already exist, one just needs its stub removed.

**Alternatives considered:** Collapsing both into one guard with an optional claim parameter —
rejected, would require changing every route's `canActivate` wiring for no behavior gain, and
blurs the "authenticated" vs. "authorized for X" distinction the routes already encode.

### D2: `APP_INITIALIZER` rehydrates session state once at boot, blocking bootstrap

**Decision:** Un-comment `provideAppInitializer` in `app.config.ts` to call
`auth.checkSession()` and block bootstrap until it resolves (success or failure both resolve —
`checkSession()` already swallows errors into `of(false)`).

**Rationale:** Avoids a flash of "logged out" UI before the first `GET /bff/user` round-trip
resolves. `checkSession()` already returns a safely-completing observable, so this cannot hang
bootstrap indefinitely under normal conditions.

**Alternatives considered:** Lazy hydration (call `checkSession()` only when a guard first needs
it) — rejected, causes UI flicker on shell chrome that reads `isAuthenticated()` (e.g. nav
"Login"/"Logout" state) before any guard has run.

### D3: `auth-error-handling` — interim behavior vs. post-`gateway-challenge-fix` behavior

**Decision:** `errorInterceptor` adds a 401 branch:
- **Interim (today, `gateway-challenge-fix` not yet live):** Because the Gateway currently
  redirects (302) rather than 401s on auth failure for *all* callers, and Angular's `HttpClient`
  transparently follows same-origin/CORS-safe redirects, a genuinely-failed proxied call will
  most often surface to Angular as a CORS-blocked opaque failure or a follow-through to
  Keycloak's login page content (status `0` or a non-JSON body), not a clean `401`. Given open
  question D4 in the superseded discussion already rejected treating `status === 0` as
  "unauthenticated" (too many unrelated causes: CORS, DNS, offline, ad-blockers), the interceptor
  does **not** attempt to infer "session expired" from `status === 0` in the interim state. It
  only acts on an explicit `401` when the Gateway does emit one (e.g. today's real
  `AllowAnonymous` 404 vs 401 cases, or once the fix partially lands). This is a deliberate
  narrower-than-ideal interim behavior, not a bug — broadening it further requires the backend
  fix to make `401` reliable first.
- **Post-fix (once `gateway-challenge-fix` ships):** API-style calls that fail authorization
  return a bare `401` reliably. On `401`, the interceptor calls `auth.checkSession()` once to
  re-sync local state (covers the case where the session cookie is still fresh but a specific
  downstream check failed) and, only if that resolves `false` (truly unauthenticated), triggers
  the toast-then-redirect flow described in D5. On `403`, no redirect — `403` means authenticated
  but not authorized, which is `claimGuard`'s job to prevent proactively; the interceptor surfaces
  it as a normal `AppHttpError` for the calling feature to handle (e.g. inline message).
- Both states share one code path with a feature check, not two interceptors — the interim
  behavior is simply "narrower 401 handling," not a different mechanism, so no rewrite is needed
  when the backend fix lands, only removing the caveat comment.

**Rationale:** Avoids designing a frontend workaround for a backend bug (redirect-instead-of-401)
that duplicates effort once the real fix ships, while still shipping usable session-expiry UX
today for the cases that already do 401 correctly.

**Alternatives considered:**
- Full `fetch(..., { redirect: 'manual' })` workaround (option C in prior investigation) to detect
  the 302-to-Keycloak as an `opaqueredirect` and treat it as "unauthenticated" today — rejected;
  Angular's `withFetch()` `HttpClient` backend doesn't expose the `redirect` option through its
  public API, so this would require bypassing the interceptor pipeline for a soon-to-be-obsolete
  workaround. Not worth the complexity given the backend fix is already scheduled.
- Treating `status === 0` as unauthenticated — rejected per D4 in the prior investigation
  (false positives from CORS/DNS/offline/ad-blocker failures).

### D4: `csrf-interceptor` is minimal and only fires for Gateway-native POSTs

**Decision:** A small `csrfInterceptor` that: (a) only activates for requests whose URL matches
the Gateway's own base origin/path prefix (not proxied `/products-api`, `/stocks-api`,
`/payments-api` calls) and whose method is POST/PUT/PATCH/DELETE, (b) fetches
`GET /api/antiforgery/token` (once, cached) and attaches it as `X-XSRF-TOKEN` before forwarding.

**Rationale:** Matches confirmed backend reality: CSRF is scoped to Gateway-native endpoints only,
never proxied downstream calls, and there are currently zero Gateway-native POST endpoints Angular
calls (login/register/logout are GET redirects; refresh is dead). This is forward-looking
infrastructure — kept intentionally thin so it's cheap to have even with no current consumer, and
straightforward to extend once a real Gateway-native POST endpoint exists.

**Alternatives considered:** Skipping this capability entirely until a consumer exists — rejected;
the interceptor is small enough that having it ready now avoids blocking a future PR on
infrastructure, and the proposal explicitly calls out this capability as forward-looking so it
isn't mistaken for dead code later.

### D5: `toast-infrastructure` is net-new and scoped to session/auth feedback only

**Decision:** Add a minimal `ToastService` (signal-based queue of `{ message, variant }`) and a
`ToastComponent` mounted once at the app shell root. First and only consumer in this change: a
"Your session has expired" message shown when `auth-error-handling` (D3) determines the user is
truly unauthenticated (session cookie's underlying refresh token expired/revoked server-side),
displayed briefly before `auth.login()` redirects to `/bff/login`.

**Rationale:** Without a toast, the redirect to Keycloak's login screen looks like an unexplained
kick-out. A brief, dismissable message gives the user context before the page navigates away.
Scoping strictly to auth avoids scope creep into a general notification system that isn't needed
yet and wasn't asked for.

**Alternatives considered:** Third-party toast library — rejected, single use case doesn't
justify a new dependency. General-purpose notification service (form validation, success
messages, etc.) — rejected as out of scope for this change; can be extended later without
breaking this narrow API.

### D6: `environment-config` confirms single-URL model

**Decision:** `environment.model.ts` keeps exactly `apiGatewayUrl`; no `authServiceUrl` or other
per-service URLs are introduced. All BFF endpoints (`/bff/login`, `/bff/register`, `/bff/logout`,
`/bff/user`, `/api/antiforgery/token`) and all proxied API calls go through the single Gateway
origin.

**Rationale:** There is no separate auth service in this architecture — the Gateway *is* the BFF.
This was already true in code before this change; the spec simply codifies it and formally retires
the `authServiceUrl` notion carried over from `frontend-auth`.

**Alternatives considered:** None — this reflects existing, already-correct code.

## Risks / Trade-offs

| Risk | Mitigation |
|---|---|
| `gateway-challenge-fix` ships later than expected, leaving `auth-error-handling`'s interim behavior (narrow 401-only handling) in place longer than intended | Interim behavior is safe (no false-positive redirects) even if long-lived; D3's shared-code-path design means no rewrite is needed when the fix lands, only removing the caveat. |
| `GET /bff/user` empty-claims bug is not actually fixed before this change ships, breaking `hasClaim()`-based `claimGuard` checks | Out of scope per proposal; flagged explicitly as an external backend prerequisite. If it lands unfixed, `claimGuard` will incorrectly treat all authenticated users as claim-less and block access — acceptable as a known, already-flagged blocking bug rather than a silent one. |
| `APP_INITIALIZER` blocking bootstrap on `GET /bff/user` adds a network round-trip to first paint for every user, including anonymous ones browsing the catalog | Accepted trade-off — call is cheap (cookie-based, same-origin) and `checkSession()` already resolves quickly on failure; revisit only if real-world latency becomes a problem. |
| CSRF interceptor ships with no consumer and could bit-rot or be removed by a future cleanup pass before it's used | Proposal and design explicitly document it as intentional forward-looking infra, not dead code, to prevent accidental removal. |
| Toast infrastructure introduced narrowly (auth-only) may need rework if a broader notification need arises later | Minimal signal-based API (`show(message, variant)`) is easy to extend without a breaking change; not a concern for this change's scope. |

## Migration Plan

- No data migration. Purely frontend code changes plus one net-new UI capability (toasts) and
  one net-new route (`/unauthorized`).
- Rollout order: `environment-config` confirmation → `auth-state-service` (APP_INITIALIZER
  wiring) → `session-guard` (unblock `authGuard`) → `toast-infrastructure` → `auth-error-handling`
  (depends on toast) → `csrf-interceptor` (independent, can land any time).
- Rollback: each capability is independently revertible (guard stub can be restored, interceptor
  branches can be removed) since none introduce backend or schema changes.
- No coordination required with `gateway-challenge-fix` deployment timing — this change is
  designed to be safe to ship before, during, or after that backend fix lands.

## Open Questions

- None outstanding for this change. Remaining open items (`gateway-challenge-fix` implementation,
  `GET /bff/user` claims bug fix, `gateway-bff-auth/specs/*` rewrite, `frontend-auth` archival)
  are tracked as separate follow-up work, not open questions blocking this change.
