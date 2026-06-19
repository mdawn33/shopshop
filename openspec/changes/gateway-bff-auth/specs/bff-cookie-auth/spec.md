## ADDED Requirements

### Requirement: Gateway registers Cookie authentication scheme
The Gateway.Api SHALL register `CookieAuthenticationDefaults.AuthenticationScheme` via `AddCookie()` with `DefaultAuthenticateScheme` set to `CookieAuthenticationDefaults.AuthenticationScheme`.

#### Scenario: Cookie scheme is the default authenticate scheme
- **WHEN** a request arrives at the Gateway with a valid auth cookie
- **THEN** `HttpContext.User` is populated from the cookie claims before any endpoint handler runs

#### Scenario: JwtBearer scheme is still registered
- **WHEN** the application starts
- **THEN** both `Cookies` and `Bearer` schemes are registered without conflict

### Requirement: Cookie is HttpOnly, Secure, SameSite=Lax, Path=/
The auth cookie issued by `SignInAsync` SHALL be configured with `HttpOnly = true`, `Secure = true` in non-development environments, `SameSite = Lax`, and `Path = "/"`.

#### Scenario: Cookie flags in production
- **WHEN** `SignInAsync` is called in a non-development environment
- **THEN** the `Set-Cookie` response header includes `HttpOnly`, `Secure`, `SameSite=Lax`, and `Path=/`

#### Scenario: Secure flag omitted in development
- **WHEN** `SignInAsync` is called in a development environment
- **THEN** the `Set-Cookie` response header includes `HttpOnly` and `SameSite=Lax` but NOT `Secure`, allowing `http://localhost`

### Requirement: Cookie stores access token and refresh token as claims
After a successful Keycloak token exchange the BFF SHALL call `SignInAsync` with a `ClaimsPrincipal` that contains the following claims:
- `ClaimTypes.NameIdentifier` — Keycloak `sub` value
- `ClaimTypes.Email` — decoded `email` claim from the Keycloak JWT
- `"display_name"` — decoded `name` or `preferred_username` from the Keycloak JWT
- `"access_token"` — raw Keycloak access token string
- `"refresh_token"` — raw Keycloak refresh token string

#### Scenario: Claims populated after login
- **WHEN** `POST /auth/login` succeeds and `SignInAsync` is called
- **THEN** `HttpContext.User.FindFirstValue("access_token")` returns the non-null Keycloak access token on the next request

#### Scenario: Claims cleared after logout
- **WHEN** `POST /auth/logout` calls `SignOutAsync`
- **THEN** the auth cookie is deleted from the response
- **THEN** `HttpContext.User.Identity.IsAuthenticated` is `false` on subsequent requests

### Requirement: Cookie expiry aligns with Keycloak access token TTL
The cookie `ExpireTimeSpan` SHALL be configurable via `BffCookie:ExpireMinutes` in `appsettings.json` and SHALL default to 60 minutes with sliding expiration disabled.

#### Scenario: Cookie expires after configured duration
- **WHEN** a cookie is issued and `BffCookie:ExpireMinutes` is 60
- **THEN** the `Set-Cookie` header includes an `Expires` value approximately 60 minutes in the future
