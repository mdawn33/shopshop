## Why

The backend auth approach changed since `openspec/changes/frontend-auth/` was written: the
Gateway now implements a **BFF pattern with OIDC redirect to Keycloak**, not the manual
ROPC-style login/register/refresh endpoints that `frontend-auth` was designed against. The
frontend code has also already partially drifted toward the real model independently (redirect
based `Auth` service, credentialed HTTP interceptor, commented-out guard logic), so the old spec
no longer matches either the backend contract or the code it's supposed to govern. This proposal
supersedes `frontend-auth` with a spec written against the actual BFF endpoints, cookie, and
refresh model, and finishes wiring the frontend pieces that are still stubbed or commented out
(`authGuard`, `APP_INITIALIZER` rehydration, `errorInterceptor` 401/403 handling, toast
infrastructure, `/unauthorized` route).

## What Changes

- Introduce a signal-based `auth-state-service` capability: session state hydrated from
  `GET /bff/user`, exposing `isAuthenticated`/`currentUser`, wired into `APP_INITIALIZER` so the
  app rehydrates session state on boot instead of the current no-op.
- Introduce `auth-redirect-triggers`: formalize the existing `window.location.href` redirects to
  `/bff/login`, `/bff/register`, `/bff/logout` as the only login/registration mechanism — no
  reactive forms, no credential POSTs. Keycloak's hosted UI owns the login/register screens.
- Introduce `session-guard`: activate the real (currently commented-out) `authGuard` logic for
  general auth-required routes (e.g. `/cart`), and formalize `claimGuard`'s existing logic for
  account-protected routes (e.g. `/checkout`, `/orders`). Wire the `/unauthorized` route that
  `claimGuard` already redirects to but does not yet exist.
- Introduce `csrf-interceptor`: fetch-and-attach `X-XSRF-TOKEN` from `GET /api/antiforgery/token`
  for Gateway-native POST requests only. Scoped thin/forward-looking — no Angular code currently
  calls a Gateway-native POST endpoint.
- Introduce `auth-error-handling`: 401/403 handling in `errorInterceptor` (currently a pure
  passthrough that just wraps errors). Explicitly documents dependency on an **unimplemented
  backend prerequisite** — the Gateway's Challenge-vs-401 fix (`DefaultChallengeScheme = "smart"`,
  tracked separately as `gateway-challenge-fix`) — and defines interim vs. post-fix behavior.
- Introduce `toast-infrastructure`: net-new `ToastService`/`ToastComponent` (does not exist in
  the codebase today), scoped specifically to session/auth feedback — e.g. "Your session has
  expired" when Keycloak's refresh token itself expires and the backend `TokenRefreshService` can
  no longer silently refresh, shown before redirecting to `/bff/login`.
- Modify `environment-config` (as previously specified in `frontend-auth`, never implemented):
  confirm `apiGatewayUrl` is the only URL the frontend needs; drop any notion of a separate
  `authServiceUrl` — there is no separate auth service to point at.
- **BREAKING (spec-level, relative to `frontend-auth`)**: removes the `login-ui`/`register-ui`
  capabilities (no LoginComponent/RegisterComponent, no credential-POST forms) and the
  frontend-orchestrated refresh design (`refreshToken()`, `refreshInProgress` flag, retry queue,
  `POST /bff/refresh` call) entirely — none of these have any backend counterpart.

## Capabilities

### New Capabilities
- `auth-state-service`: signal-based session state (`isAuthenticated`, `currentUser`) hydrated
  from `GET /bff/user`, replacing `auth-service-real` from the superseded `frontend-auth` change.
- `auth-redirect-triggers`: full-page redirect wrappers for login/register/logout against
  `/bff/login`, `/bff/register`, `/bff/logout`, replacing `login-ui`/`register-ui`.
- `session-guard`: `authGuard` (general auth) + `claimGuard` (account-protected routes) route
  guard logic, plus the `/unauthorized` route they depend on.
- `csrf-interceptor`: attaches `X-XSRF-TOKEN` (fetched from `GET /api/antiforgery/token`) to
  Gateway-native POST requests only.
- `auth-error-handling`: 401/403 handling rules in `errorInterceptor`, including explicit
  interim-vs-post-fix behavior tied to the `gateway-challenge-fix` backend prerequisite.
- `toast-infrastructure`: minimal `ToastService`/`ToastComponent` scoped to session/auth
  feedback only (e.g. session-expired notice).

### Modified Capabilities
- `environment-config`: confirms `apiGatewayUrl` as the sole environment URL and drops
  `authServiceUrl` (this capability was specified but never implemented under `frontend-auth`;
  no `openspec/specs/environment-config` exists yet to diff against, so this is effectively a
  fresh spec written against current code, not a requirements delta).

## Impact

- **Affected code**: `web-client/src/app/core/services/auth.ts`,
  `web-client/src/app/core/guards/auth.ts`, `web-client/src/app/core/interceptors/auth.ts`,
  `web-client/src/app/core/interceptors/error.ts`, `web-client/src/app/app.config.ts`,
  `web-client/src/app/app.routes.ts`, `web-client/src/environments/environment.model.ts`; new
  files for `ToastService`/`ToastComponent`, `csrfInterceptor`, and an `/unauthorized` route
  component.
- **Backend dependency (external, not part of this change)**: the Gateway's
  Challenge-vs-401 fix (`DefaultChallengeScheme = "smart"`) is assumed but not yet live; tracked
  as a separate `gateway-challenge-fix` change. `auth-error-handling` must work in both the
  current (redirect-always) and post-fix (bare-401-for-API-callers) states.
- **Backend dependency (external, not part of this change)**: `GET /bff/user` is assumed to be
  fixed to return correct claims (id/email/displayName) rather than the current empty-claims bug;
  this change does not design a workaround for that bug.
- **Supersedes**: `openspec/changes/frontend-auth/` (left untouched for now; archival/removal is
  a separate later step, not part of this change).
- **No database/API contract changes** — this is frontend-only; no new backend endpoints are
  introduced by this change.
