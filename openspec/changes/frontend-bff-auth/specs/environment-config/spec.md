## ADDED Requirements

### Requirement: `apiGatewayUrl` is the sole environment URL for auth and API calls
`environment.model.ts` SHALL define exactly one URL property relevant to backend communication,
`apiGatewayUrl`, used for all BFF endpoints (`/bff/login`, `/bff/register`, `/bff/logout`,
`/bff/user`, `/api/antiforgery/token`) and all proxied downstream API calls
(products/stock/payment). No `authServiceUrl` or other per-service base URL SHALL exist, since
there is no separate auth service in this architecture — the Gateway is the BFF.

#### Scenario: Auth service builds a login redirect URL
- **WHEN** `Auth.login()` builds the redirect target
- **THEN** it uses `environment.apiGatewayUrl` as the base, with no reference to any
  `authServiceUrl` or equivalent

#### Scenario: Environment model is inspected for URL properties
- **WHEN** `environment.model.ts` is reviewed
- **THEN** it contains `apiGatewayUrl` and no `authServiceUrl` property
