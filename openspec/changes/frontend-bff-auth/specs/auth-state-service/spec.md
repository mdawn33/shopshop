## ADDED Requirements

### Requirement: Session state is exposed as signals derived from `GET /bff/user`
The `Auth` service SHALL expose the current session state as Angular signals — `isAuthenticated`
(computed boolean) and `currentUser` (read-only signal of the raw claims array, or `null` when
unauthenticated) — derived from the response of `GET {apiGatewayUrl}/bff/user`. No component or
guard SHALL call `GET /bff/user` directly; all consumers read state through `Auth`'s signals or
call `checkSession()` to refresh them.

#### Scenario: Successful session hydration
- **WHEN** `checkSession()` is called and `GET /bff/user` resolves with a non-empty claims array
- **THEN** `currentUser()` returns that claims array and `isAuthenticated()` returns `true`

#### Scenario: Failed or anonymous session hydration
- **WHEN** `checkSession()` is called and `GET /bff/user` errors (e.g. `401`) or the request
  otherwise fails
- **THEN** `currentUser()` is set to `null` and `isAuthenticated()` returns `false`; the error is
  swallowed and `checkSession()` resolves (does not throw) so callers can safely chain on it

### Requirement: `checkSession()` is idempotent and side-effect-scoped to signal updates
`checkSession()` SHALL be safely callable multiple times (e.g. once from `APP_INITIALIZER` at boot, then again from a guard), only updating the `Auth` service's internal signals — it SHALL NOT trigger navigation, redirects, or throw. Redirect decisions belong to guards and interceptors, not to `Auth` itself.

#### Scenario: Guard re-checks session after initial boot hydration
- **WHEN** a route guard calls `checkSession()` after `APP_INITIALIZER` has already hydrated
  state once
- **THEN** the signals are updated to reflect the latest server state and no navigation occurs
  as a side effect of the call itself

### Requirement: Session state is hydrated once at application boot
The application SHALL call `checkSession()` during bootstrap (via `provideAppInitializer` in
`app.config.ts`) and block initial rendering until it resolves, so that `isAuthenticated()`
reflects the real session state before any component or guard reads it.

#### Scenario: App boots with an active session cookie
- **WHEN** the browser has a valid `__Host-Shoppiness_bff` cookie and the app bootstraps
- **THEN** `APP_INITIALIZER` resolves `checkSession()` before the router activates the first
  route, so shell chrome (e.g. nav login/logout state) renders correctly on first paint

#### Scenario: App boots with no session
- **WHEN** the browser has no session cookie (or an expired one) and the app bootstraps
- **THEN** `APP_INITIALIZER` still resolves (does not hang or throw) with `isAuthenticated()`
  returning `false`

### Requirement: `hasClaim` performs local, synchronous claim lookups only
`Auth.hasClaim(claimType, expectedValue)` SHALL check only the currently-hydrated claims signal
(no network call) and return `false` when no session is hydrated.

#### Scenario: Claim present in hydrated session
- **WHEN** `hasClaim('role', 'customer')` is called and the hydrated claims include a claim with
  type `role` and value `customer`
- **THEN** it returns `true`

#### Scenario: No session hydrated yet
- **WHEN** `hasClaim(...)` is called before any successful `checkSession()` call
- **THEN** it returns `false` without making a network request
