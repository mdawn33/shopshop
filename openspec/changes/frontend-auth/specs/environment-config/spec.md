## MODIFIED Requirements

### Requirement: AppEnvironment interface includes all service URLs
The `AppEnvironment` interface in `src/environments/environment.model.ts` SHALL include a typed property for every BFF/microservice base URL used by the application. Each URL property SHALL be a `string` and SHALL be present in both `environment.ts` and `environment.development.ts`.

#### Scenario: Interface includes authServiceUrl
- **WHEN** a developer imports `AppEnvironment`
- **THEN** the interface has `authServiceUrl: string` in addition to `productServiceUrl`, `stockServiceUrl`, and `paymentServiceUrl`

#### Scenario: Production environment file compiles with authServiceUrl
- **WHEN** `environment.ts` is imported
- **THEN** it exports a constant typed as `AppEnvironment` that includes `authServiceUrl`

#### Scenario: Development environment file includes authServiceUrl
- **WHEN** `environment.development.ts` is imported
- **THEN** it exports a constant with `authServiceUrl` set to a local dev URL (e.g., `http://localhost:5050`)
