## ADDED Requirements

### Requirement: Login is a full-page redirect to the Gateway's BFF login endpoint
`Auth.login(returnUrl?)` SHALL set `window.location.href` to
`{apiGatewayUrl}/bff/login`, appending `?returnUrl=<encoded returnUrl>` when a `returnUrl` is
provided. It SHALL NOT make an `HttpClient` request and SHALL NOT accept credentials as
parameters — there is no `login(email, password)` method.

#### Scenario: Login triggered with a return URL
- **WHEN** a guard calls `auth.login('/cart')` because the user is unauthenticated
- **THEN** the browser navigates to
  `{apiGatewayUrl}/bff/login?returnUrl=%2Fcart`, handing control to Keycloak's hosted login UI

#### Scenario: Login triggered without a return URL
- **WHEN** `auth.login()` is called with no argument
- **THEN** the browser navigates to `{apiGatewayUrl}/bff/login` with no query string

### Requirement: Registration is a full-page redirect to the Gateway's BFF register endpoint
`Auth.register()` SHALL set `window.location.href` to `{apiGatewayUrl}/bff/register`. It SHALL
NOT accept or POST credential/profile fields — there is no `register(email, password,
displayName)` method; Keycloak's hosted UI collects registration details.

#### Scenario: User initiates registration
- **WHEN** `auth.register()` is called
- **THEN** the browser navigates to `{apiGatewayUrl}/bff/register`

### Requirement: Logout is a full-page redirect to the Gateway's BFF logout endpoint
`Auth.logout()` SHALL set `window.location.href` to `{apiGatewayUrl}/bff/logout`. It SHALL NOT
make an `HttpClient` request. No local signal reset is required beforehand — the subsequent
navigation away from the app makes it moot.

#### Scenario: User initiates logout
- **WHEN** `auth.logout()` is called
- **THEN** the browser navigates to `{apiGatewayUrl}/bff/logout`, which clears the session
  cookie server-side before returning the user to the app

### Requirement: No frontend-orchestrated token refresh
The frontend SHALL NOT implement a `refreshToken()` method, a `refreshInProgress` flag, or a
request retry queue keyed on refresh completion, and SHALL NOT call `POST /bff/refresh` — that
endpoint exists on the backend but is dead code from the frontend's perspective. Token refresh
happens entirely server-side, silently, on every proxied request via the Gateway's
`TokenRefreshService`.

#### Scenario: An access token nears expiry during normal use
- **WHEN** the user makes a proxied API call and the underlying access token is near/past expiry
- **THEN** the Gateway refreshes it server-side before proxying the call; the frontend makes no
  refresh-related request and is not involved in the process
