## ADDED Requirements

### Requirement: AuthService exposes signal-based auth state
`AuthService` (`core/services/auth.service.ts`) SHALL be a `providedIn: 'root'` service that exposes a `currentUser` readonly signal of type `User | null` and an `isAuthenticated` computed signal of type `boolean`.

#### Scenario: isAuthenticated is true when currentUser is set
- **WHEN** the internal `currentUser` signal holds a non-null `User` object
- **THEN** `isAuthenticated()` SHALL return `true`

#### Scenario: isAuthenticated is false when currentUser is null
- **WHEN** the internal `currentUser` signal holds `null`
- **THEN** `isAuthenticated()` SHALL return `false`

### Requirement: AuthService initial state is unauthenticated
On construction, `AuthService` SHALL initialize `currentUser` to `null`.

#### Scenario: Fresh service instance is unauthenticated
- **WHEN** `AuthService` is first injected into any consumer
- **THEN** `isAuthenticated()` SHALL return `false` and `currentUser()` SHALL return `null`

### Requirement: `User` model is declared in `core/models/user.model.ts`
A `User` interface SHALL be declared with at minimum: `id: string`, `email: string`, `displayName: string`.

#### Scenario: User model is importable by any feature
- **WHEN** a feature or component imports `User` from `core/models/user.model.ts`
- **THEN** TypeScript SHALL resolve the type without error

### Requirement: `login()` and `logout()` simulate auth state changes; `register()` remains unimplemented
`AuthService` SHALL declare:
- `login(email: string, password: string): Promise<void>` — resolves successfully and sets `currentUser` to a hardcoded mock `User` object (`{ id: 'mock-user-id', email: email, displayName: 'Mock User' }`). Does not make any HTTP call.
- `logout(): Promise<void>` — resolves successfully and sets `currentUser` to `null`. Does not make any HTTP call.
- `register(email: string, password: string, displayName: string): Promise<void>` — logs a warning and returns a rejected promise with message `'Not implemented'`.

#### Scenario: Calling login() resolves and sets currentUser
- **WHEN** `authService.login('a@b.com', 'pass')` is called
- **THEN** the returned promise SHALL resolve
- **AND** `authService.currentUser()` SHALL return a `User` object with `email` equal to `'a@b.com'`
- **AND** `authService.isAuthenticated()` SHALL return `true`

#### Scenario: Calling logout() resolves and clears currentUser
- **WHEN** `authService.logout()` is called after a prior login
- **THEN** the returned promise SHALL resolve
- **AND** `authService.currentUser()` SHALL return `null`
- **AND** `authService.isAuthenticated()` SHALL return `false`

#### Scenario: Calling register() rejects with Not implemented
- **WHEN** `authService.register('a@b.com', 'pass', 'Alice')` is called
- **THEN** the returned promise SHALL reject with an error whose message is `'Not implemented'`

### Requirement: AuthService uses `inject()` for dependencies
`AuthService` SHALL use the `inject()` function for any internal dependencies, not constructor injection.

#### Scenario: No constructor parameters are present
- **WHEN** reviewing `auth.service.ts`
- **THEN** the class constructor SHALL have no parameters
