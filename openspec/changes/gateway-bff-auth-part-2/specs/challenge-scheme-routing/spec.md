## ADDED Requirements

### Requirement: Challenge dispatch follows the Bearer-vs-Cookie policy scheme
`Gateway.Api` SHALL set `DefaultChallengeScheme` to the `"smart"` policy scheme (the same policy
scheme already used for `DefaultAuthenticateScheme`) instead of hardcoding
`OpenIdConnectDefaults.AuthenticationScheme`, so that a failed `.RequireAuthorization()` on any
route routes its Challenge through the same Bearer-header inspection Authenticate already uses.

#### Scenario: API-style caller with a Bearer header fails authorization
- **WHEN** a request carrying `Authorization: Bearer <token>` fails `.RequireAuthorization()`
  (missing, expired, or invalid token) on any route enforcing authorization
- **THEN** the response is `401 Unauthorized`
- **THEN** no `Location` header redirecting to Keycloak is present

#### Scenario: Browser-style caller with no Bearer header fails authorization
- **WHEN** a request with no `Authorization` header (cookie-based or fully anonymous) fails
  `.RequireAuthorization()` on any route enforcing authorization
- **THEN** the response is `302 Found`
- **THEN** the `Location` header points to Keycloak's OIDC authorize endpoint

### Requirement: Challenge routing applies to every route enforcing authorization, not only Gateway-native endpoints
The `"smart"`-based Challenge dispatch SHALL govern every route where authorization can fail,
including YARP-proxied routes carrying `"AuthorizationPolicy": "default"` in
`appsettings.json`/`appsettings.Development.json` (`products-route`, `stocks-route`,
`payments-route`) and Gateway-native endpoints calling `.RequireAuthorization()` (`/bff/refresh`).
(The standalone `GET /api/antiforgery/token` endpoint previously listed here as an example no
longer exists — see `bff-antiforgery-wiring` — its token-issuance responsibility is folded into
`GET /bff/user`.)

#### Scenario: Proxied route with no credentials
- **WHEN** an unauthenticated browser-style request is made to a YARP-proxied route (e.g.
  `/products-api/products`)
- **THEN** the Gateway's own authorization check fails before the request reaches the
  downstream cluster
- **THEN** the response follows the same Bearer-vs-Cookie Challenge dispatch rule as any other
  protected route (302 for cookie-style, 401 for Bearer-style)

### Requirement: Challenge scheme fix is empirically verified before being considered complete
This capability SHALL NOT be considered complete until its behavior has been confirmed against
a running Gateway instance with a reachable Keycloak. A source-code trace alone SHALL NOT be
treated as sufficient verification.

#### Scenario: Manual verification against a live instance
- **WHEN** a protected route is called with no credentials against a running Gateway + Keycloak
- **THEN** the observed response code and headers are recorded and match the two scenarios
  above
- **THEN** any mismatch is treated as a defect in this capability's implementation, not a
  documentation update to match observed behavior
