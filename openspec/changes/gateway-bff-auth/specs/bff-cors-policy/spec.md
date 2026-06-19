## ADDED Requirements

### Requirement: Gateway registers a named CORS policy that allows the Angular origin with credentials
The Gateway.Api SHALL register a named CORS policy `"BffCorsPolicy"` that calls `AllowCredentials()`, sets `WithOrigins(...)` from the `Cors:AllowedOrigins` configuration array, and allows the headers and methods required by the Angular client.

#### Scenario: CORS headers present on preflight from Angular origin
- **WHEN** the browser sends an `OPTIONS` preflight request from `http://localhost:4200` with `Access-Control-Request-Method: POST`
- **THEN** the response includes `Access-Control-Allow-Origin: http://localhost:4200`
- **THEN** the response includes `Access-Control-Allow-Credentials: true`
- **THEN** the response status is `204 No Content`

#### Scenario: CORS headers present on actual request from Angular origin
- **WHEN** `POST /auth/login` is sent from `http://localhost:4200` with `withCredentials: true`
- **THEN** the response includes `Access-Control-Allow-Origin: http://localhost:4200`
- **THEN** the response includes `Access-Control-Allow-Credentials: true`

#### Scenario: CORS policy does not allow arbitrary origins
- **WHEN** a request arrives from an origin not listed in `Cors:AllowedOrigins`
- **THEN** the response does NOT include `Access-Control-Allow-Origin`

### Requirement: CORS policy is applied before authentication middleware
The `UseCors("BffCorsPolicy")` call in `Program.cs` SHALL appear before `UseAuthentication()` and `UseAuthorization()` in the middleware pipeline.

#### Scenario: CORS headers are present even on 401 responses
- **WHEN** an unauthenticated request from the Angular origin receives a `401 Unauthorized`
- **THEN** the response still includes `Access-Control-Allow-Origin` and `Access-Control-Allow-Credentials`
- **THEN** the Angular client can read the response body to handle the error

### Requirement: Allowed CORS origins are configurable per environment
The `Cors:AllowedOrigins` key in `appsettings.json` SHALL be an array of strings. In `appsettings.Development.json` it SHALL include `"http://localhost:4200"`. In `appsettings.json` (production template) the array SHALL be empty by default with a comment indicating it must be set before deployment.

#### Scenario: Development environment allows localhost Angular origin
- **WHEN** the application runs with `ASPNETCORE_ENVIRONMENT=Development`
- **THEN** `http://localhost:4200` is in the allowed origins list
