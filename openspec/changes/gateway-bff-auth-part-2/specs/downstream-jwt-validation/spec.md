## ADDED Requirements

> This capability corrects and supersedes the plan previously described in
> `openspec/changes/gateway-bff-auth/specs/downstream-jwt-validation/spec.md`, which was never
> implemented and referenced a non-existent `AddKeycloakJwtBearer(...)` extension method. This
> spec uses the plain `Microsoft.AspNetCore.Authentication.JwtBearer` package, matching
> `Gateway.Api`'s own working implementation.

### Requirement: ProductsService validates forwarded Keycloak JWTs
`Shoppiness.ProductsService` SHALL register JWT Bearer authentication via
`AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)`, configured with
the same Keycloak `Authority`/`MetadataAddress`, `ValidIssuer`, and `Audience` values
`Gateway.Api` uses. `app.UseAuthentication()` and `app.UseAuthorization()` SHALL be called
before endpoint mapping.

#### Scenario: Valid forwarded token is accepted
- **WHEN** a request arrives at ProductsService with `Authorization: Bearer <valid_token>`
  forwarded by the Gateway
- **THEN** `HttpContext.User.Identity.IsAuthenticated` is `true`
- **THEN** an endpoint requiring authorization executes normally

#### Scenario: Missing or invalid token is rejected on protected endpoints
- **WHEN** a request arrives at a ProductsService endpoint that calls `.RequireAuthorization()`
  without an `Authorization` header, or with an expired/invalid token
- **THEN** the response is `401 Unauthorized`
- **THEN** the endpoint handler does not execute

#### Scenario: Existing RequireAuthorization endpoint no longer fails at startup/request time
- **WHEN** `POST /categories` (which already calls `.RequireAuthorization()`) is called with a
  valid forwarded token, after this change is implemented
- **THEN** the request is processed normally, with no `InvalidOperationException` about a
  missing authentication scheme

### Requirement: StocksService validates forwarded Keycloak JWTs
`Shoppiness.StocksService` SHALL register JWT Bearer authentication identically in shape to
ProductsService's, and SHALL apply `.RequireAuthorization()` to its write endpoints
(`InitializeStock`, `AddStock`, `RemoveStock`).

#### Scenario: Valid forwarded token is accepted on a write endpoint
- **WHEN** `POST` to `AddStock` or `RemoveStock` arrives with a valid forwarded Bearer token
- **THEN** the request is processed normally

#### Scenario: Missing token is rejected on a write endpoint
- **WHEN** `POST` to `AddStock`, `RemoveStock`, or `InitializeStock` arrives with no
  `Authorization` header
- **THEN** the response is `401 Unauthorized`

### Requirement: PaymentsService has authentication pipeline readiness, not endpoint enforcement
`Shoppiness.PaymentsService` SHALL register JWT Bearer authentication and middleware
identically in shape to the other two services (config keys, `AddAuthentication`,
`UseAuthentication`, `UseAuthorization`), even though it has no mapped endpoints yet. This
requirement covers configuration and pipeline readiness only — no scenario describing a
protected endpoint's runtime behavior applies until real endpoints exist.

#### Scenario: Pipeline is registered without a mapped protected endpoint
- **WHEN** `Shoppiness.PaymentsService` starts
- **THEN** JWT Bearer authentication middleware is registered and configured against the same
  Keycloak realm/audience as the other services
- **THEN** no endpoint currently exists to exercise a 401/200 scenario against — this is
  expected and not a gap in this requirement

### Requirement: Downstream JWT validation configuration is environment-aware
Each of the three services SHALL read Keycloak metadata from `Authentication:MetadataAddress`,
`Authentication:ValidIssuer`, and `Authentication:Audience` in its own `appsettings.json`,
mirroring the `Gateway.Api` configuration pattern. In development, `RequireHttpsMetadata` SHALL
be `false`.

#### Scenario: Development environment allows HTTP metadata
- **WHEN** any of the three services runs with `ASPNETCORE_ENVIRONMENT=Development`
- **THEN** Keycloak metadata discovery succeeds without a TLS-related failure

#### Scenario: Audience validation is enforced
- **WHEN** a JWT with an `aud` claim different from the configured `Authentication:Audience` is
  presented to a protected endpoint
- **THEN** the service returns `401 Unauthorized`
