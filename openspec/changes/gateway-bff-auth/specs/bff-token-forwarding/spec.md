## ADDED Requirements

### Requirement: Gateway injects Authorization Bearer header on all proxied requests
The Gateway.Api SHALL register an `ITransformProvider` implementation (`BffTokenForwardingTransformProvider`) that adds a request transform to every YARP route. The transform SHALL read the `"access_token"` claim from `HttpContext.User` and set the `Authorization` header to `Bearer <access_token>` on the upstream request.

#### Scenario: Authenticated request reaches downstream with Bearer token
- **WHEN** an authenticated request with a valid session cookie is proxied by YARP to a downstream service
- **THEN** the upstream request includes the header `Authorization: Bearer <access_token>`
- **THEN** the downstream service can validate the JWT independently

#### Scenario: Cookie is not forwarded to downstream services
- **WHEN** YARP proxies a request to a downstream service
- **THEN** the `Cookie` header is NOT included in the upstream request
- **THEN** no session cookie is visible to downstream services

#### Scenario: Unauthenticated proxied request does not include Authorization header
- **WHEN** a request with no session cookie is proxied by YARP
- **THEN** `HttpContext.User.Identity.IsAuthenticated` is `false`
- **THEN** no `Authorization` header is added to the upstream request
- **THEN** the downstream service receives the request without a Bearer token (and may reject it with 401)

### Requirement: Token forwarding transform provider applies to all YARP routes
The `BffTokenForwardingTransformProvider` SHALL implement `ITransformProvider` and register its transform in the `Apply(TransformBuilderContext context)` method unconditionally (i.e., it applies to all routes, not a subset). It SHALL be registered via `AddReverseProxy().AddTransforms<BffTokenForwardingTransformProvider>()`.

#### Scenario: Transform applies to products route
- **WHEN** YARP proxies a request matching `/products-api/{**catch-all}`
- **THEN** the Bearer token injection transform is active

#### Scenario: Transform applies to stocks route
- **WHEN** YARP proxies a request matching `/stocks-api/{**catch-all}`
- **THEN** the Bearer token injection transform is active
