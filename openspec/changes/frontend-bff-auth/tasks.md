## 1. `environment-config` — confirm single-URL model

- [ ] 1.1 Review `web-client/src/environments/environment.model.ts` and confirm only
      `apiGatewayUrl` exists (no `authServiceUrl` or other per-service URL). No code change
      expected here; this task is a verification checkpoint.
- [ ] 1.2 Grep the codebase for any remaining reference to `authServiceUrl` and remove/update if
      found.

## 2. `auth-state-service` — session hydration

- [ ] 2.1 Confirm `Auth.checkSession()`, `isAuthenticated`, `currentUser`, and `hasClaim()` in
      `web-client/src/app/core/services/auth.ts` match the spec (they largely already do); remove
      the dead commented-out `checkSession()` block.
- [ ] 2.2 Un-comment and wire the `provideAppInitializer` block in
      `web-client/src/app/app.config.ts` to call `auth.checkSession()` and block bootstrap until
      it resolves.
- [ ] 2.3 Manually verify: reload the app with a valid session cookie and confirm
      `isAuthenticated()` is `true` before the first route activates (no login-state flicker).
- [ ] 2.4 Manually verify: reload the app with no session cookie and confirm bootstrap still
      completes (does not hang).

## 3. `session-guard` — activate real guard logic and `/unauthorized` route

- [ ] 3.1 Remove the `return true` stub in `authGuard`
      (`web-client/src/app/core/guards/auth.ts`) and un-comment/activate its real logic: check
      `isAuthenticated()`, fall back to `checkSession()`, call `auth.login(state.url)` and deny
      activation on failure.
- [ ] 3.2 Review `claimGuard` in the same file against the spec — confirm existing logic already
      matches (authenticate first via `isAuthenticated()`/`checkSession()`, then `hasClaim()`
      check, redirect to `/unauthorized` on claim failure, `auth.login()` on auth failure).
- [ ] 3.3 Create a minimal `UnauthorizedComponent` (standalone, `OnPush`) and register the
      `/unauthorized` route in `web-client/src/app/app.routes.ts` (currently commented out).
- [ ] 3.4 Verify route wiring: `/cart` uses `authGuard`; `/checkout` and `/orders` use
      `claimGuard('role', 'customer')`, matching current `app.routes.ts` intent.
- [ ] 3.5 Manually verify: navigate to `/cart` unauthenticated and confirm redirect to
      `/bff/login` with `returnUrl=/cart`.
- [ ] 3.6 Manually verify: navigate to `/checkout` as an authenticated user lacking the
      `customer` role claim and confirm redirect to `/unauthorized` (no redirect loop).

## 4. `toast-infrastructure` — net-new, auth-scoped

- [ ] 4.1 Create `ToastService` (signal-based queue, `show(message, variant?)` method) under
      `web-client/src/app/core/services/`.
- [ ] 4.2 Create `ToastComponent` (standalone, `OnPush`, inline template if small) under
      `web-client/src/app/shared/` (or equivalent shared UI location), rendering active toasts
      from `ToastService` with auto-dismiss and manual dismiss.
- [ ] 4.3 Mount `ToastComponent` once at the application shell root (`app.ts` template), outside
      routed content.
- [ ] 4.4 Verify no other call sites of `ToastService.show(...)` exist yet besides the one added
      in Section 5 below (scope check).

## 5. `auth-error-handling` — 401/403 handling in `errorInterceptor`

- [ ] 5.1 Add a `401` branch to `errorInterceptor`
      (`web-client/src/app/core/interceptors/error.ts`): on `401`, call `auth.checkSession()`; if
      it resolves `false`, call `toastService.show('Your session has expired')` then
      `auth.login(currentUrl)`, then rethrow the wrapped `AppHttpError`; if it resolves `true`,
      skip the toast/redirect and rethrow.
- [ ] 5.2 Add explicit non-handling for `403` — confirm the interceptor rethrows the wrapped
      error unchanged with no redirect/toast for `403` responses.
- [ ] 5.3 Add a code comment documenting the interim-vs-post-`gateway-challenge-fix` behavior per
      design.md D3, so future readers understand why `status === 0` / redirected responses are
      deliberately not treated as session-expiry signals.
- [ ] 5.4 Manually verify (interim state, current backend): trigger a proxied call while
      unauthenticated and observe actual behavior (redirect/opaque failure) to confirm it does
      not falsely trigger the toast/redirect path.
- [ ] 5.5 Manually verify: trigger a request that returns an explicit `401` (if any such endpoint
      exists today) and confirm the toast + redirect path fires correctly.

## 6. `csrf-interceptor` — reads `XSRF-TOKEN` cookie, no token-fetch endpoint

> Backend note (`gateway-bff-auth-part-2`, D8/D9, tasks 2.10-2.11): the standalone
> `GET /api/antiforgery/token` endpoint referenced in the original version of this section no
> longer exists. The `XSRF-TOKEN` cookie is now set as a side effect of `GET /bff/user`, which
> `Auth.checkSession()` (`web-client/src/app/core/services/auth.ts`) already calls during
> bootstrap (Section 2) — no separate fetch is needed. Antiforgery is also now enforced on
> mutating YARP-proxied routes (`/products-api/*`, `/stocks-api/*`, `/payments-api/*`), not just
> Gateway-native ones, so the interceptor must cover both rather than excluding the proxied paths.

- [ ] 6.1 Create `csrfInterceptor` under `web-client/src/app/core/interceptors/`: matches any
      mutating method (`POST`/`PUT`/`PATCH`/`DELETE`) request — both Gateway-native and
      YARP-proxied (`/products-api/*`, `/stocks-api/*`, `/payments-api/*`) — reads the
      `XSRF-TOKEN` cookie value directly (e.g. via `document.cookie` or Angular's built-in
      `HttpXsrfTokenExtractor`) and attaches it as the `X-XSRF-TOKEN` header. No HTTP call to
      fetch a token — the backend no longer exposes a dedicated antiforgery-token endpoint.
- [ ] 6.2 Register `csrfInterceptor` in the `provideHttpClient(withInterceptors([...]))` chain in
      `web-client/src/app/app.config.ts`, ordered before `errorInterceptor` so CSRF failures are
      still normalized by it.
- [ ] 6.3 Confirm proxied downstream requests (`/products-api/*`, `/stocks-api/*`,
      `/payments-api/*`) are INCLUDED in the CSRF check, not excluded — the backend requires
      antiforgery validation precisely on those mutating proxied routes (`gateway-bff-auth-part-2`
      D8), with the only bypass being machine-to-machine Bearer-token callers, which does not
      apply to the browser SPA.
- [ ] 6.4 Verify the ordering dependency: `Auth.checkSession()` (Section 2) must call
      `GET /bff/user` during bootstrap before any mutating request is issued, since that call is
      what sets the `XSRF-TOKEN` cookie for the session — confirm this holds (it does, per current
      `auth.ts`) and flag it explicitly as a load-bearing dependency, not an incidental detail.
- [ ] 6.5 Add a unit test: verify the header is attached on a synthetic Gateway-native mutating
      request and on a synthetic proxied mutating request (e.g. `POST /products-api/...`) when the
      `XSRF-TOKEN` cookie is present, and confirm it is NOT attached on `GET` requests.

## 7. Cross-cutting verification

- [ ] 7.1 Run the full `web-client` unit test suite and confirm no regressions.
- [ ] 7.2 Run through the primary flows manually end-to-end where the local stack permits: boot
      with/without session, guarded navigation, forced 401 (if reachable), logout.
- [ ] 7.3 Confirm this change makes no edits to `openspec/changes/frontend-auth/` — that
      supersession/archival is explicitly out of scope for this change.
