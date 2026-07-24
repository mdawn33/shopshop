## ADDED Requirements

### Requirement: Logout honors a validated redirectUrl
`GET /bff/logout` SHALL accept an optional `redirectUrl` query parameter and use it as the
post-logout `RedirectUri` when it passes the same local-URL validation
(`UrlHelpers.IsLocalUrl`) already used by `GET /bff/login`'s `returnUrl` parameter. When
`redirectUrl` is absent or fails validation, the endpoint SHALL fall back to `/`.

#### Scenario: Valid local redirectUrl is honored
- **WHEN** `GET /bff/logout?redirectUrl=/orders` is called
- **THEN** the sign-out flow's `RedirectUri` is `/orders`

#### Scenario: Missing redirectUrl falls back to root
- **WHEN** `GET /bff/logout` is called with no `redirectUrl` parameter
- **THEN** the sign-out flow's `RedirectUri` is `/`

#### Scenario: Non-local redirectUrl is rejected
- **WHEN** `GET /bff/logout?redirectUrl=https://evil.example.com` is called
- **THEN** `UrlHelpers.IsLocalUrl` rejects the value
- **THEN** the sign-out flow's `RedirectUri` is `/`, not the rejected value

### Requirement: Sign-out succeeds regardless of prior authentication state
`GET /bff/logout` SHALL complete its sign-out flow (clearing the local cookie and triggering
Keycloak's remote sign-out) without erroring when called by a caller with no valid session
cookie.

#### Scenario: Logout called with no session cookie
- **WHEN** `GET /bff/logout` is called with no `__Host-Shoppiness_bff` cookie present
- **THEN** the endpoint does not throw or return a 5xx error
- **THEN** the sign-out flow still redirects per the `redirectUrl`/fallback rule above
