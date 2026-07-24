## ADDED Requirements

### Requirement: `errorInterceptor` handles explicit `401` responses as session-expired
`errorInterceptor` SHALL, on receiving an `HttpErrorResponse` with `status === 401` from any request, call `auth.checkSession()` to re-sync local session state. If `checkSession()` resolves `false` (confirmed unauthenticated), it SHALL show a toast via `ToastService` with a session-expired message and then call `auth.login(currentUrl)`, in that order, before rethrowing the wrapped `AppHttpError` to the original caller. If `checkSession()` resolves `true` (the `401` was request-specific, not a session-wide failure), it SHALL skip the toast/redirect and simply rethrow the wrapped error.

#### Scenario: A proxied call returns a bare 401 and the session truly expired
- **WHEN** any HTTP request receives a `401` response and the subsequent `checkSession()` call
  resolves `false`
- **THEN** the interceptor shows a "Your session has expired" toast, then redirects via
  `auth.login(state.url)`, then rethrows the error

#### Scenario: A 401 occurs but the session is still valid on re-check
- **WHEN** a request receives a `401` but `checkSession()` immediately after resolves `true`
- **THEN** the interceptor does not toast or redirect; it rethrows the wrapped `AppHttpError` for
  the calling code to handle

### Requirement: `errorInterceptor` does not redirect on `403`
When `errorInterceptor` receives a `403` (authenticated but not authorized), it SHALL NOT call
`auth.login()` or navigate anywhere. It SHALL rethrow the wrapped `AppHttpError` unchanged, since
authorization failures are expected to be prevented proactively by `claimGuard` and any residual
`403` is a signal for the calling feature to handle contextually (e.g. an inline message), not a
global auth-state problem.

#### Scenario: An authorized-but-insufficient-claim request returns 403
- **WHEN** a request receives a `403` response
- **THEN** the interceptor rethrows the wrapped error without toasting or redirecting

### Requirement: 401 handling does not depend on the Gateway's Challenge-vs-401 fix landing, but its practical coverage is currently limited by it
This capability's `401` branch (see above) SHALL be implemented against the documented, intended
contract — a bare `401` on authorization failure — regardless of whether the Gateway's
`DefaultChallengeScheme = "smart"` fix (tracked separately as `gateway-challenge-fix`, not part
of this change) has shipped. This spec explicitly documents that today, before that fix lands,
the Gateway redirects (`302` to Keycloak) rather than returning `401` for *all* callers on every
proxied route (`AuthorizationPolicy: "default"` on products/stocks/payments routes) and on
Gateway-native protected endpoints, so in practice the `401` branch above fires only for the
subset of failures that already surface as a clean `401` today. The interceptor SHALL NOT attempt
to infer "unauthenticated" from `status === 0` or from a redirected/opaque response as a
workaround for the pre-fix state — those signals are too ambiguous (CORS failures, DNS failures,
offline, ad-blockers) to safely trigger a global session-expired redirect.

#### Scenario: Pre-fix state — a proxied call fails auth and the Gateway redirects instead of 401ing
- **WHEN** a proxied request fails Gateway-level authorization before `gateway-challenge-fix`
  ships, and the browser either follows the redirect (surfacing as a non-JSON/HTML response) or
  the request is blocked as an opaque/CORS-failed response (`status === 0`)
- **THEN** the interceptor does NOT treat this as a confirmed session-expiry event and does NOT
  toast or redirect on the basis of `status === 0` alone; it rethrows the wrapped error as-is

#### Scenario: Post-fix state — the same failure now returns a bare 401
- **WHEN** `gateway-challenge-fix` has shipped and the same proxied request now fails with a
  clean `401` (because the caller is cookie/XHR-based, not a browser navigation)
- **THEN** the interceptor's existing `401` branch (see above) handles it exactly as specified,
  with no code change required in this capability

### Requirement: Toast is shown before redirect, not instead of it
`errorInterceptor` SHALL show the session-expired toast synchronously before calling `auth.login()` whenever the session-expired redirect path (per the first requirement above) is triggered, so the user has a chance to read it even though the subsequent full-page navigation will unmount the toast shortly after.

#### Scenario: Confirmed session expiry
- **WHEN** the interceptor determines the session has truly expired
- **THEN** `ToastService.show(...)` is called immediately before `window.location.href` is set
  by `auth.login()`, giving the toast a brief window to render before navigation
