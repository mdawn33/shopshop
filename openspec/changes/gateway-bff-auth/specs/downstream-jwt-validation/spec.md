## ADDED Requirements

### Requirement: ProductService validates Keycloak JwtBearer tokens
`Shoppiness.ProductsService` SHALL register JWT Bearer authentication via `AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddKeycloakJwtBearer(...)` using the same Keycloak realm and audience configuration already used by Gateway.Api. `UseAuthentication()` and `UseAuthorization()` SHALL be called in the middleware pipeline before endpoint mapping.

#### Scenario: Valid forwarded token is accepted
- **WHEN** a request arrives at ProductService with `Authorization: Bearer <valid_keycloak_token>`
- **THEN** `HttpContext.User.Identity.IsAuthenticated` is `true`
- **THEN** the endpoint handler executes normally

#### Scenario: Missing or invalid token is rejected
- **WHEN** a request arrives at ProductService without an `Authorization` header or with an expired/invalid token
- **THEN** the response is `401 Unauthorized`
- **THEN** the endpoint handler does not execute

### Requirement: Downstream JWT validation configuration is environment-aware
ProductService SHALL read Keycloak metadata from `Authentication:MetadataAddress`, `Authentication:ValidIssuer`, and `Authentication:Audience` in `appsettings.json`, mirroring the Gateway.Api configuration pattern. In development, `RequireHttpsMetadata` SHALL be `false`.

#### Scenario: Development environment allows HTTP metadata
- **WHEN** ProductService runs with `ASPNETCORE_ENVIRONMENT=Development`
- **THEN** the Keycloak metadata discovery URL is fetched over HTTP without TLS errors

#### Scenario: Audience validation is enforced
- **WHEN** a JWT with a different `aud` claim than `Authentication:Audience` is presented
- **THEN** ProductService returns `401 Unauthorized`
