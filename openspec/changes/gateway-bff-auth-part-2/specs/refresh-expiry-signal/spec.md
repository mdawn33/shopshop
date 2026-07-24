## ADDED Requirements

### Requirement: A distinguishable signal marks a permanently failed refresh
The Gateway SHALL attach the `X-Shoppiness-Session-Expired: true` response header to any
response produced after `TokenRefreshService` attempts to refresh an access token using a
`refresh_token` that Keycloak explicitly rejects (expired or revoked). This header SHALL be
distinct from an ordinary `401` caused by a resource-specific authorization failure.

#### Scenario: Refresh token rejected by Keycloak on a proxied call
- **WHEN** a proxied request's access token is expired and the stored `refresh_token` is
  rejected by Keycloak's token endpoint
- **THEN** the response returned to the caller carries `X-Shoppiness-Session-Expired: true`

#### Scenario: Refresh token rejected by Keycloak on GET /bff/user
- **WHEN** `GET /bff/user` is called with a session whose refresh token is rejected by
  Keycloak
- **THEN** the response is `401 Unauthorized`
- **THEN** the response carries `X-Shoppiness-Session-Expired: true`

### Requirement: The signal is not raised for callers who were never authenticated
The `X-Shoppiness-Session-Expired` header SHALL NOT be present on responses to callers with no
prior session at all (no `refresh_token` ever issued), to avoid conflating "never logged in"
with "session expired."

#### Scenario: Fully anonymous caller does not receive the expiry signal
- **WHEN** a request is made with no session cookie and no `Authorization` header
- **THEN** the resulting `401`/`302` response does not carry
  `X-Shoppiness-Session-Expired`

### Requirement: The signal does not depend on downstream services being auth-aware
The signal SHALL be attachable to the response regardless of whether the eventual downstream
service (once `downstream-jwt-validation` ships) independently rejects the request, since it is
set by the Gateway before the request is proxied.

#### Scenario: Signal present even if downstream would have allowed the request
- **WHEN** a refresh-token-rejected request is proxied to a downstream route
- **THEN** `X-Shoppiness-Session-Expired: true` is present on the response headers regardless
  of the downstream service's own response status code
