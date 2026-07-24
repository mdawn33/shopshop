## ADDED Requirements

### Requirement: `authGuard` enforces that a session exists, without regard to claims
`authGuard` SHALL activate a route only when the current user is authenticated. It SHALL first
check `auth.isAuthenticated()`; if `false`, it SHALL call `auth.checkSession()` and re-check. If
still unauthenticated, it SHALL call `auth.login(state.url)` (redirecting to Keycloak with a
return URL) and deny activation. `authGuard` SHALL NOT be a no-op stub (`return true`) in
production routing.

#### Scenario: Authenticated user navigates to a guarded route
- **WHEN** `isAuthenticated()` is already `true` and the user navigates to a route with
  `canActivate: [authGuard]`
- **THEN** the guard returns `true` synchronously without calling `checkSession()` again

#### Scenario: Unauthenticated user navigates to a guarded route
- **WHEN** `isAuthenticated()` is `false` and the user navigates to a route with
  `canActivate: [authGuard]`
- **THEN** the guard calls `checkSession()`; if it resolves `false`, the guard calls
  `auth.login(state.url)` and returns `false`, preventing activation

#### Scenario: Session becomes valid during the guard's check
- **WHEN** `isAuthenticated()` is initially `false` but `checkSession()` resolves `true` (e.g. a
  cookie exists but local signals hadn't been hydrated yet)
- **THEN** the guard returns `true` and activation proceeds without a redirect

### Requirement: `claimGuard` enforces both authentication and a specific claim
`claimGuard(claimType, allowedValue)` SHALL first ensure the user is authenticated (via
`isAuthenticated()`, falling back to `checkSession()` as in `authGuard`), redirecting to
`auth.login(state.url)` on failure. If authenticated, it SHALL check
`auth.hasClaim(claimType, allowedValue)`; on failure, it SHALL navigate to `/unauthorized` and
deny activation. This guard SHALL be used for account-protected routes specifically (e.g.
`/checkout`, `/orders`), not as a general authentication gate — general auth-only routes use
`authGuard`.

#### Scenario: Authenticated user with the required claim
- **WHEN** the user is authenticated and `hasClaim('role', 'customer')` returns `true` on a
  route guarded by `claimGuard('role', 'customer')`
- **THEN** the guard returns `true`

#### Scenario: Authenticated user missing the required claim
- **WHEN** the user is authenticated but `hasClaim('role', 'customer')` returns `false`
- **THEN** the guard navigates to `/unauthorized` and returns `false`

#### Scenario: Unauthenticated user reaches a claim-guarded route
- **WHEN** the user is not authenticated and navigates to a route guarded by `claimGuard`
- **THEN** the guard calls `auth.login(state.url)` and returns `false` (does not send them to
  `/unauthorized` — that page is for authenticated-but-unauthorized users, not anonymous ones)

### Requirement: An `/unauthorized` route exists as the landing page for claim failures
The application SHALL register a top-level `/unauthorized` route rendering a minimal
"not authorized" page. This route SHALL be reachable and SHALL NOT itself be guarded (an
already-authenticated-but-unauthorized user must be able to land on it without a redirect loop).

#### Scenario: Claim-guarded navigation is denied
- **WHEN** `claimGuard` denies activation due to a missing claim
- **THEN** the router navigates to `/unauthorized` and that route renders successfully
