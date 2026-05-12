## ADDED Requirements

### Requirement: Auth service performs real HTTP login
The `Auth` service SHALL call `POST /login` on the BFF with `{ email, password }` and update the `currentUser` signal from the response body on success. The method signature SHALL be `login(email: string, password: string): Promise<void>`.

#### Scenario: Successful login sets currentUser
- **WHEN** `login(email, password)` is called with valid credentials
- **THEN** `POST /login` is sent to `authServiceUrl` with `{ email, password }`
- **THEN** `currentUser()` returns the `User` object from the response
- **THEN** the returned promise resolves

#### Scenario: Failed login propagates error
- **WHEN** `login(email, password)` is called and the BFF returns a non-2xx response
- **THEN** `currentUser()` remains unchanged
- **THEN** the returned promise rejects with an `AppHttpError`

### Requirement: Auth service performs real HTTP register
The `Auth` service SHALL call `POST /register` on the BFF with `{ email, password, displayName }` and update the `currentUser` signal on success. The method signature SHALL be `register(email: string, password: string, displayName: string): Promise<void>`.

#### Scenario: Successful register sets currentUser
- **WHEN** `register(email, password, displayName)` is called with valid data
- **THEN** `POST /register` is sent to `authServiceUrl` with `{ email, password, displayName }`
- **THEN** `currentUser()` returns the `User` object from the response
- **THEN** the returned promise resolves

#### Scenario: Failed register propagates error
- **WHEN** `register(email, password, displayName)` is called and the BFF returns a non-2xx response
- **THEN** `currentUser()` remains unchanged
- **THEN** the returned promise rejects with an `AppHttpError`

### Requirement: Auth service performs real HTTP logout
The `Auth` service SHALL call `POST /logout` on the BFF and clear `currentUser` to `null` after the call completes. The method signature SHALL be `logout(): Promise<void>`.

#### Scenario: Successful logout clears currentUser
- **WHEN** `logout()` is called while a user is authenticated
- **THEN** `POST /logout` is sent to `authServiceUrl`
- **THEN** `currentUser()` returns `null`
- **THEN** `isAuthenticated()` returns `false`
- **THEN** the returned promise resolves

### Requirement: Auth service exposes refreshToken with race guard flag
The `Auth` service SHALL expose a `refreshToken(): Promise<void>` method that calls `POST /refresh` on the BFF and updates `currentUser` from the response. The service SHALL also expose a public `refreshInProgress: boolean` flag, which is `true` while a refresh call is in flight and `false` otherwise.

#### Scenario: Successful refresh updates currentUser
- **WHEN** `refreshToken()` is called
- **THEN** `refreshInProgress` is `true` for the duration of the call
- **THEN** `POST /refresh` is sent to `authServiceUrl`
- **THEN** on success, `currentUser()` is updated from the response
- **THEN** `refreshInProgress` is set to `false`

#### Scenario: Failed refresh clears refreshInProgress
- **WHEN** `refreshToken()` is called and the BFF returns a non-2xx response
- **THEN** `refreshInProgress` is set to `false`
- **THEN** the returned promise rejects

### Requirement: Auth service rehydrates currentUser on app init
The `Auth` service SHALL expose a `rehydrate(): Promise<void>` method that calls `GET /user` on the BFF. If the response is successful, `currentUser` SHALL be set from the response body. If the request fails for any reason, `currentUser` SHALL remain `null` and the promise SHALL resolve (not reject).

#### Scenario: Valid session rehydrates user
- **WHEN** `rehydrate()` is called and the browser has a valid session cookie
- **THEN** `GET /user` is sent to `authServiceUrl`
- **THEN** `currentUser()` returns the `User` object from the response

#### Scenario: No session leaves currentUser null
- **WHEN** `rehydrate()` is called and the browser has no valid session cookie
- **THEN** `GET /user` is sent and returns 401
- **THEN** `currentUser()` remains `null`
- **THEN** the promise resolves without rejecting

### Requirement: Auth service removes _token signal
The `Auth` service SHALL NOT expose a `_token` signal or a public `token` signal. The token is managed exclusively by the BFF via HttpOnly cookie and is not accessible to JavaScript.

#### Scenario: No token signal accessible
- **WHEN** a consumer inspects the `Auth` service
- **THEN** no `token` or `_token` property is present on the service

### Requirement: APP_INITIALIZER calls rehydrate before first render
The application configuration SHALL register an `APP_INITIALIZER` that calls `Auth.rehydrate()` and completes before the first route activates.

#### Scenario: App init waits for rehydration
- **WHEN** the Angular application bootstraps
- **THEN** `GET /user` is called before any route guard evaluates
- **THEN** `AuthGuard` reads the correct `isAuthenticated()` state on first navigation
