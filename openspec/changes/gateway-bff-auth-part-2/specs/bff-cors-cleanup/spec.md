## ADDED Requirements

### Requirement: Gateway pipeline does not reference CORS middleware
`Gateway.Api`'s HTTP request pipeline SHALL NOT call `app.UseCors(...)`, and
`Program.cs` SHALL NOT contain a commented-out or active `AddCors(...)` registration for a
policy the pipeline references. CORS is architecturally unnecessary because the
`angular-spa-fallback` YARP route proxies the Angular dev server through the Gateway itself,
making every browser-to-Gateway interaction same-origin.

#### Scenario: Pipeline builds without an unregistered CORS policy reference
- **WHEN** the Gateway application starts
- **THEN** no middleware in the pipeline references a CORS policy name that has no matching
  `AddCors` registration

#### Scenario: Cross-origin request behavior is unchanged by removal
- **WHEN** the Angular dev server (proxied through `angular-spa-fallback`) makes a request to
  any Gateway route
- **THEN** the request is same-origin (target origin `https://localhost:5001`, matching the
  page's own origin) and requires no CORS headers to succeed

### Requirement: Dead CORS-related configuration is removed
The `BFF:FrontendOrigin` configuration key SHALL be removed from `appsettings.json` and
`appsettings.Development.json` once confirmed to have no other consumer besides the removed
CORS wiring.

#### Scenario: FrontendOrigin has no remaining reference
- **WHEN** `appsettings.json` and `appsettings.Development.json` are inspected after this
  change
- **THEN** neither file contains a `BFF:FrontendOrigin` key
- **THEN** no `.cs` file in `Gateway.Api` reads `config["BFF:FrontendOrigin"]` (active or
  commented-out)
