## ADDED Requirements

### Requirement: CSRF token is attached only to Gateway-native mutating requests
An HTTP interceptor SHALL attach an `X-XSRF-TOKEN` header, sourced from
`GET {apiGatewayUrl}/api/antiforgery/token`, to outgoing `POST`/`PUT`/`PATCH`/`DELETE` requests
whose URL targets a Gateway-native endpoint (i.e. not a proxied downstream call such as
`/products-api/*`, `/stocks-api/*`, `/payments-api/*`). Requests to proxied downstream endpoints
SHALL NOT receive this header — the Gateway's YARP proxy routes do not require it.

#### Scenario: Mutating request to a Gateway-native endpoint
- **WHEN** the app issues a `POST` request to a Gateway-native path (e.g. a future
  `{apiGatewayUrl}/bff/*` or `{apiGatewayUrl}/api/*` mutating endpoint)
- **THEN** the interceptor fetches (or reuses a cached) antiforgery token and attaches it as
  `X-XSRF-TOKEN` before forwarding the request

#### Scenario: Mutating request to a proxied downstream endpoint
- **WHEN** the app issues a `POST` request to a proxied route such as `/products-api/products`
- **THEN** the interceptor does not fetch or attach an `X-XSRF-TOKEN` header

#### Scenario: Non-mutating (GET) request
- **WHEN** the app issues a `GET` request to any endpoint
- **THEN** the interceptor does not fetch or attach `X-XSRF-TOKEN`, regardless of target

### Requirement: The antiforgery token is fetched lazily and cached for reuse
The interceptor SHALL NOT eagerly fetch `GET /api/antiforgery/token` on app boot. It SHALL fetch
it on first need (first qualifying mutating Gateway-native request) and cache the value for reuse
on subsequent qualifying requests within the session, re-fetching only if a request using the
cached token fails due to an invalid/expired token.

#### Scenario: First qualifying request in a session
- **WHEN** the first `POST` to a Gateway-native endpoint occurs in a session with no cached
  token yet
- **THEN** the interceptor fetches `GET /api/antiforgery/token` once, caches it, and attaches it
  before forwarding

#### Scenario: Subsequent qualifying requests reuse the cached token
- **WHEN** a second qualifying `POST` occurs later in the same session
- **THEN** the interceptor attaches the previously cached token without an additional fetch to
  `/api/antiforgery/token`
