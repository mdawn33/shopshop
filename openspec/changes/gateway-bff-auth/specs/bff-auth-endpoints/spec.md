## ADDED Requirements

### Requirement: POST /auth/login exchanges credentials for a session cookie
The BFF SHALL expose `POST /auth/login` that accepts `{ email: string, password: string }`, calls Keycloak's token endpoint with `grant_type=password`, and on success calls `SignInAsync` to issue an HttpOnly cookie, then returns `200 OK` with `{ user: { id, email, displayName } }`.

#### Scenario: Successful login issues cookie and returns user
- **WHEN** `POST /auth/login` is sent with valid `{ email, password }`
- **THEN** the BFF calls the Keycloak token endpoint with `grant_type=password`
- **THEN** `HttpContext.SignInAsync` is called with a principal containing `access_token`, `refresh_token`, and user identity claims
- **THEN** the response is `200 OK` with body `{ user: { id, email, displayName } }`
- **THEN** the response sets an HttpOnly auth cookie

#### Scenario: Invalid credentials returns 401
- **WHEN** `POST /auth/login` is sent with incorrect credentials
- **THEN** Keycloak returns a non-2xx response
- **THEN** the BFF returns `401 Unauthorized` with no cookie set

#### Scenario: Missing required fields returns 400
- **WHEN** `POST /auth/login` is sent with a missing `email` or `password`
- **THEN** the BFF returns `400 Bad Request` with validation error details before calling Keycloak

### Requirement: POST /auth/register creates a Keycloak user and issues a session cookie
The BFF SHALL expose `POST /auth/register` that accepts `{ email: string, password: string, displayName: string }`, calls the Keycloak Admin REST API to create the user using a service-account token, then calls the Keycloak token endpoint (`grant_type=password`) to authenticate the new user, calls `SignInAsync`, and returns `201 Created` with `{ user: { id, email, displayName } }`.

#### Scenario: Successful registration creates user, issues cookie, returns user
- **WHEN** `POST /auth/register` is sent with valid `{ email, password, displayName }`
- **THEN** the BFF obtains a service-account token via `client_credentials` grant
- **THEN** the BFF calls `POST /admin/realms/{realm}/users` with the user data
- **THEN** the BFF logs in the new user via `grant_type=password` and calls `SignInAsync`
- **THEN** the response is `201 Created` with body `{ user: { id, email, displayName } }`
- **THEN** the response sets an HttpOnly auth cookie

#### Scenario: Duplicate email returns 409
- **WHEN** `POST /auth/register` is sent with an email that already exists in Keycloak
- **THEN** the Keycloak Admin API returns `409 Conflict`
- **THEN** the BFF returns `409 Conflict` with no cookie set

#### Scenario: Missing required fields returns 400
- **WHEN** `POST /auth/register` is sent with a missing `email`, `password`, or `displayName`
- **THEN** the BFF returns `400 Bad Request` before calling Keycloak

### Requirement: POST /auth/logout clears the session cookie
The BFF SHALL expose `POST /auth/logout` that calls `HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme)` and returns `204 No Content`.

#### Scenario: Logout clears the cookie
- **WHEN** `POST /auth/logout` is called while the user has a valid session cookie
- **THEN** `SignOutAsync` is called
- **THEN** the response is `204 No Content`
- **THEN** the response includes a `Set-Cookie` header that expires the auth cookie

#### Scenario: Logout on unauthenticated request is a no-op
- **WHEN** `POST /auth/logout` is called with no session cookie present
- **THEN** the BFF returns `204 No Content` without error

### Requirement: POST /auth/refresh rotates the session cookie using the Keycloak refresh token
The BFF SHALL expose `POST /auth/refresh` that reads the `refresh_token` claim from the current cookie principal, calls the Keycloak token endpoint with `grant_type=refresh_token`, calls `SignInAsync` with a new principal containing the updated tokens, and returns `200 OK` with `{ user: { id, email, displayName } }`.

#### Scenario: Successful refresh rotates cookie and returns updated user
- **WHEN** `POST /auth/refresh` is called with a valid session cookie containing a non-expired refresh token
- **THEN** the BFF sends `grant_type=refresh_token` to the Keycloak token endpoint
- **THEN** `SignInAsync` is called with the new access and refresh tokens
- **THEN** the response is `200 OK` with body `{ user: { id, email, displayName } }`
- **THEN** the response sets an updated HttpOnly auth cookie

#### Scenario: Expired or invalid refresh token returns 401
- **WHEN** `POST /auth/refresh` is called and Keycloak rejects the refresh token
- **THEN** `SignOutAsync` is called to clear the stale cookie
- **THEN** the BFF returns `401 Unauthorized`

#### Scenario: No session cookie returns 401
- **WHEN** `POST /auth/refresh` is called with no session cookie
- **THEN** the BFF returns `401 Unauthorized`

### Requirement: GET /auth/user returns the current user from cookie claims
The BFF SHALL expose `GET /auth/user` that reads `ClaimTypes.NameIdentifier`, `ClaimTypes.Email`, and `"display_name"` from `HttpContext.User` and returns `200 OK` with `{ user: { id, email, displayName } }`. The endpoint SHALL require authentication.

#### Scenario: Authenticated request returns user
- **WHEN** `GET /auth/user` is called with a valid session cookie
- **THEN** the response is `200 OK` with body `{ user: { id, email, displayName } }` derived from cookie claims

#### Scenario: Unauthenticated request returns 401
- **WHEN** `GET /auth/user` is called with no session cookie or an expired cookie
- **THEN** the response is `401 Unauthorized`

### Requirement: All BFF auth endpoints are exempt from YARP proxying
The `/auth/*` routes SHALL be registered as minimal API endpoints in `Program.cs` before `MapReverseProxy()` so they are handled by the Gateway's own middleware stack and never forwarded to a downstream service.

#### Scenario: Auth endpoints are not proxied
- **WHEN** a request is sent to `/auth/login`
- **THEN** YARP does not forward the request to any upstream cluster
- **THEN** the Gateway's own handler processes the request
