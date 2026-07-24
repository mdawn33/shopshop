## ADDED Requirements

### Requirement: Antiforgery validation is required for mutating YARP-proxied requests
CSRF/antiforgery protection SHALL be enforced on state-changing requests (`POST`, `PUT`, `PATCH`,
`DELETE`) that `Gateway.Api` proxies via YARP to `products-route`, `stocks-route`, and
`payments-route`, in addition to Gateway-native state-changing endpoints. This corrects the
capability's original framing, which incorrectly stated antiforgery SHALL NOT be required or
referenced for YARP-proxied downstream calls.

#### Scenario: Mutating proxied request without a valid antiforgery token is rejected
- **WHEN** a cookie-authenticated caller sends a `POST`, `PUT`, `PATCH`, or `DELETE` request to a
  YARP-proxied route (e.g. `/products-api/...`) without a valid `X-XSRF-TOKEN` header matching the
  antiforgery cookie
- **THEN** the request is short-circuited before it reaches `MapReverseProxy()`
- **THEN** the response is `400 Bad Request`
- **THEN** no request is forwarded to the downstream service

#### Scenario: Mutating proxied request with a valid antiforgery token proceeds
- **WHEN** a cookie-authenticated caller sends a `POST`, `PUT`, `PATCH`, or `DELETE` request to a
  YARP-proxied route with a valid `X-XSRF-TOKEN` header matching the antiforgery cookie
- **THEN** the request is forwarded downstream, subject to the existing
  authentication/authorization checks

#### Scenario: Non-mutating proxied requests are never antiforgery-checked
- **WHEN** a caller sends a `GET`, `HEAD`, or `OPTIONS` request to any YARP-proxied route
- **THEN** no antiforgery validation is performed, regardless of how the caller authenticated

### Requirement: Machine-to-machine Bearer callers bypass antiforgery validation
Requests to YARP-proxied routes that already carry an `Authorization: Bearer` header SHALL bypass
antiforgery validation entirely. The bypass check SHALL reuse the same Bearer-header detection
already implemented in the YARP request transform in `Program.cs` (an `Authorization` header is
present and its value starts with `Bearer `), not a second, independently-maintained copy of that
logic.

#### Scenario: M2M caller with a Bearer token skips antiforgery validation
- **WHEN** a caller sends a `POST`, `PUT`, `PATCH`, or `DELETE` request to a YARP-proxied route
  with an `Authorization: Bearer <token>` header already present
- **THEN** `IAntiforgery.ValidateRequestAsync` is never invoked for that request
- **THEN** the request proceeds directly to the existing YARP forwarding and
  authentication/authorization pipeline

#### Scenario: The bypass predicate is identical to the transform's Bearer-detection predicate
- **WHEN** the antiforgery-bypass check and the existing YARP request transform's
  "upstream already has a Bearer token" check are compared
- **THEN** they evaluate the same header-presence-and-prefix condition, so no request can be
  classified as "M2M" by one and "browser" by the other

### Requirement: Antiforgery enforcement uses custom middleware, not endpoint metadata
Antiforgery enforcement for YARP-proxied routes SHALL be implemented as custom middleware
registered ahead of `app.MapReverseProxy()`, and SHALL NOT rely on endpoint metadata, a
`RequireAntiforgeryValidation()`-style convention (which does not exist), or a nested route group
calling `UseAntiforgery()` (`UseAntiforgery()` is an `IApplicationBuilder` extension, not a valid
`RouteGroupBuilder`/`IEndpointConventionBuilder` extension, and does not compile in that
position). This is required because `MapReverseProxy()` endpoints never carry
`IAntiforgeryMetadata`, and ASP.NET Core's antiforgery middleware only validates endpoints that
carry that metadata — the framework exposes no public opt-in convention for it, only
`DisableAntiforgery()` (an opt-out for form-binding endpoints).

#### Scenario: app.MapReverseProxy() is mapped exactly once, at its real paths
- **WHEN** `Program.cs` is inspected after this change
- **THEN** `app.MapReverseProxy()` appears exactly once
- **THEN** it serves the existing, unprefixed proxied paths (`/products-api/...`,
  `/stocks-api/...`, `/payments-api/...` per `appsettings.Development.json`)
- **THEN** no route group re-maps or duplicates the reverse proxy under a different prefix
  (e.g. `/api`)

#### Scenario: Antiforgery middleware runs ahead of the reverse proxy for every request
- **WHEN** any request reaches the Gateway
- **THEN** the custom antiforgery-enforcement middleware executes before `MapReverseProxy()`
  resolves and forwards the request
- **THEN** the middleware's mutating-method check, Bearer-bypass check, and
  `ValidateRequestAsync` call all complete before any downstream call is made

### Requirement: Antiforgery cookie is hardened to match the auth cookie's posture
The antiforgery system's own tracking cookie SHALL be configured (via `AddAntiforgery(options =>
options.Cookie ...)`) with `SameSite = SameSiteMode.Strict` and `SecurePolicy =
CookieSecurePolicy.Always`, matching the posture already chosen for the `__Host-Shoppiness_bff`
auth cookie. This cookie SHALL remain `HttpOnly = true`. The separate `XSRF-TOKEN` cookie, now
written as a side effect of the `/bff/user` handler's `IAntiforgery.GetAndStoreTokens` call, is
unaffected by this requirement and remains `HttpOnly = false`.

#### Scenario: Antiforgery tracking cookie is not weaker than the auth cookie
- **WHEN** the `AddAntiforgery` configuration is inspected after this change
- **THEN** `options.Cookie.SameSite` is `SameSiteMode.Strict`
- **THEN** `options.Cookie.SecurePolicy` is `CookieSecurePolicy.Always`
- **THEN** `options.Cookie.HttpOnly` is `true`

#### Scenario: XSRF-TOKEN cookie remains readable by client-side script
- **WHEN** `GET /bff/user` is called
- **THEN** the `XSRF-TOKEN` cookie is still written with `HttpOnly = false`, unchanged by the
  tracking-cookie hardening above

### Requirement: XSRF-TOKEN issuance is folded into the `/bff/user` handler
`GET /bff/user` SHALL call `IAntiforgery.GetAndStoreTokens` (or equivalent) as a side effect of
handling the request, writing the `XSRF-TOKEN` cookie so that a caller who has authenticated and
called `/bff/user` — which the SPA already must do immediately after login, per `bff-user-claims`
— has a valid antiforgery token before issuing its first mutating request, whether that request
targets a Gateway-native endpoint or a YARP-proxied downstream route. The previously separate
`GET /api/antiforgery/token` endpoint SHALL be removed from `Gateway.Api/Endpoints.cs`; there is
no standalone token-issuance endpoint.

#### Scenario: Calling /bff/user issues a usable antiforgery token
- **WHEN** an authenticated caller requests `GET /bff/user`
- **THEN** the response sets an `XSRF-TOKEN` cookie with `HttpOnly = false`
- **THEN** the value of that cookie, echoed back in the `X-XSRF-TOKEN` header, satisfies
  `IAntiforgery.ValidateRequestAsync` for a subsequent mutating request in the same session

#### Scenario: The standalone antiforgery-token endpoint no longer exists
- **WHEN** the `Gateway.Api` codebase is searched for a `GET /api/antiforgery/token` route
  registration after this change
- **THEN** no such route exists in `Endpoints.cs`

### Requirement: Dead AntiforgeryRequired route metadata is removed
The `"AntiforgeryRequired": "true"` metadata entry SHALL be removed from all three proxy routes
in both `appsettings.json` and `appsettings.Development.json`, since no code in `Gateway.Api`
reads `route.Config.Metadata["AntiforgeryRequired"]`. Enforcement is performed by the middleware
described above, not by route metadata.

#### Scenario: No code references the removed metadata key
- **WHEN** the `Gateway.Api` codebase is searched for `AntiforgeryRequired` after this change
- **THEN** no `.cs` file references it
- **THEN** no `appsettings*.json` file under `Gateway.Api` contains the key

### Requirement: Backend antiforgery wiring is a documented dependency for frontend consumption
The `AddAntiforgery()` registration, the hardened cookie configuration, the enforcement middleware, and the `/bff/user` handler's `XSRF-TOKEN` issuance SHALL together be confirmed functional and documented as the dependency the frontend `csrf-interceptor` capability (tracked separately in `frontend-bff-auth`) needs to consume. No further backend changes are required for that frontend work to begin, beyond the SPA's existing requirement to call `GET /bff/user` once per session (e.g. immediately after login, per `bff-user-claims`) before its first mutating request — that call itself is frontend/Angular work and is out of scope for this change.

#### Scenario: No backend changes needed for frontend CSRF wiring to begin
- **WHEN** a future frontend change reads the `XSRF-TOKEN` cookie and attaches the value to an
  `X-XSRF-TOKEN` header on Gateway-native and YARP-proxied mutating requests
- **THEN** no additional backend endpoint, middleware, or configuration change is required beyond
  what already exists after this change
