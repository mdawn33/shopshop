## ADDED Requirements

### Requirement: `AppEnvironment` interface declares all microservice URLs
A TypeScript interface `AppEnvironment` SHALL be declared in `src/environments/environment.model.ts` with the following properties: `production: boolean`, `productServiceUrl: string`, `stockServiceUrl: string`, `paymentServiceUrl: string`.

#### Scenario: All URL properties are present and typed as string
- **WHEN** a developer inspects `AppEnvironment`
- **THEN** TypeScript SHALL surface a compile error if any URL property is missing or assigned a non-string value

### Requirement: Production environment file exports a typed `environment` constant
`src/environments/environment.ts` SHALL export a constant named `environment` typed as `AppEnvironment` with `production: true` and placeholder URL values for each microservice.

#### Scenario: Importing environment in production mode resolves correct URLs
- **WHEN** the Angular CLI builds with the `production` configuration
- **THEN** the imported `environment` constant SHALL have `production === true`

### Requirement: Development environment file exports a typed `environment` constant
`src/environments/environment.development.ts` SHALL export a constant named `environment` typed as `AppEnvironment` with `production: false` and `localhost` base URLs for each microservice (e.g., `http://localhost:5001`).

#### Scenario: Importing environment in development mode resolves localhost URLs
- **WHEN** the Angular CLI builds with the `development` configuration (via `fileReplacements`)
- **THEN** the imported `environment` constant SHALL have `production === false` and each service URL pointing to `localhost`

### Requirement: `angular.json` `fileReplacements` maps development environment
The `development` build configuration in `angular.json` SHALL contain a `fileReplacements` entry that replaces `src/environments/environment.ts` with `src/environments/environment.development.ts`.

#### Scenario: Development serve uses development environment
- **WHEN** the developer runs `ng serve`
- **THEN** the bundled application SHALL use the localhost URLs from `environment.development.ts`
