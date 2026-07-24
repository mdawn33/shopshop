## ADDED Requirements

### Requirement: GET /bff/user returns real identity claims for authenticated callers
`GET /bff/user` SHALL return `200 OK` with a non-empty, accurate set of identity claims
(at minimum: user id, email, display name, and role claims) for any caller with a valid
session, sourced independently of the stripped cookie identity.

#### Scenario: Authenticated caller receives real claims
- **WHEN** `GET /bff/user` is called with a valid session cookie
- **THEN** the response is `200 OK`
- **THEN** the response body contains at least one claim entry with `Type` corresponding to
  the user's id (`sub`) and at least one with `Type` corresponding to email
- **THEN** the response body is not an empty array

#### Scenario: Role claims are included when present
- **WHEN** the authenticated user has one or more realm roles assigned in Keycloak
- **THEN** the response includes one claim entry per role, using the configured
  `RoleClaimType`

### Requirement: Claim sourcing does not reverse the cookie-size optimization
The fix to `GET /bff/user` SHALL NOT cause `OnTicketReceived` to stop stripping claims from the
cookie's `ClaimsIdentity`. Claims returned by `GET /bff/user` SHALL be derived from a source
other than `context.User.Claims` (e.g. the access token retrieved via
`TokenRefreshService.GetValidTokenAsync`).

#### Scenario: Cookie identity remains stripped after the fix
- **WHEN** a user completes OIDC sign-in after this change is implemented
- **THEN** `context.User.Claims` (the cookie-backed identity) remains empty, as before this
  change
- **THEN** `GET /bff/user` still returns real claims despite the cookie identity being empty

### Requirement: Unauthenticated callers receive 401 with no claims
`GET /bff/user` SHALL continue returning `401 Unauthorized` for callers with no valid session,
and SHALL NOT attempt to derive claims from a missing or unrefreshable token.

#### Scenario: No session cookie present
- **WHEN** `GET /bff/user` is called with no session cookie
- **THEN** the response is `401 Unauthorized`

#### Scenario: Session cookie present but refresh has permanently failed
- **WHEN** `GET /bff/user` is called with a session cookie whose refresh token has been
  rejected by Keycloak (expired/revoked)
- **THEN** the response is `401 Unauthorized`
- **THEN** the response also carries the distinguishable session-expiry signal defined by the
  `refresh-expiry-signal` capability
