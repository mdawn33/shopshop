## ADDED Requirements

### Requirement: AuthInterceptor attaches withCredentials to every request
The `AuthInterceptor` SHALL clone every outgoing `HttpRequest` and set `withCredentials: true` before passing it to the next handler. It SHALL NOT read any JS-accessible token or set an `Authorization` header.

#### Scenario: withCredentials attached on every request
- **WHEN** any `HttpRequest` passes through the interceptor chain
- **THEN** the cloned request has `withCredentials: true`
- **THEN** no `Authorization` header is added

#### Scenario: withCredentials applies regardless of auth state
- **WHEN** `AuthInterceptor` processes a request while `currentUser()` is `null`
- **THEN** the request still has `withCredentials: true`
- **WHEN** `AuthInterceptor` processes a request while `currentUser()` is a valid user
- **THEN** the request still has `withCredentials: true`

### Requirement: ErrorInterceptor handles 401 with refresh then retry
When a request receives a 401 response, the `ErrorInterceptor` SHALL attempt to refresh the session exactly once before retrying the original request. It SHALL use `Auth.refreshInProgress` to prevent concurrent refresh calls.

#### Scenario: Single 401 triggers refresh and retries request
- **WHEN** a request receives a 401 response
- **THEN** `Auth.refreshInProgress` is `false`
- **THEN** `Auth.refreshToken()` is called
- **THEN** on refresh success, the original request is retried
- **THEN** the retried request's response is returned to the caller

#### Scenario: Concurrent 401s queue behind single refresh
- **WHEN** request A receives a 401 and sets `Auth.refreshInProgress = true`
- **WHEN** request B receives a 401 while `Auth.refreshInProgress` is `true`
- **THEN** request B waits without calling `refreshToken()` again
- **THEN** when the refresh triggered by request A completes, request B retries

#### Scenario: Refresh failure triggers logout and redirect
- **WHEN** a 401 triggers a refresh call and the refresh also fails
- **THEN** `Auth.refreshInProgress` is set to `false`
- **THEN** `Auth.logout()` is called
- **THEN** the application navigates to `/auth/login`

#### Scenario: Non-401 errors are mapped and rethrown
- **WHEN** a request receives a non-401 HTTP error response
- **THEN** the error is mapped to an `AppHttpError` with `status`, `message`, and `body`
- **THEN** the `AppHttpError` is rethrown via `throwError`
- **THEN** no refresh is attempted
