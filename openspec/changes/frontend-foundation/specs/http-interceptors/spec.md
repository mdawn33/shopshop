## ADDED Requirements

### Requirement: AuthInterceptor attaches Bearer token to outgoing requests
The `AuthInterceptor` (`core/interceptors/auth.interceptor.ts`) SHALL be an `HttpInterceptorFn` that reads the current auth token from `AuthService` and attaches it as an `Authorization: Bearer <token>` header on every outgoing HTTP request when a token is present.

#### Scenario: Authenticated request gets Authorization header
- **WHEN** an HTTP request is dispatched and `AuthService` holds a non-null token
- **THEN** the interceptor SHALL clone the request and set the `Authorization` header to `Bearer <token>` before passing it to the next handler

#### Scenario: Unauthenticated request is passed through unchanged
- **WHEN** an HTTP request is dispatched and `AuthService` holds a null token
- **THEN** the interceptor SHALL pass the original request to the next handler without modification

### Requirement: ErrorInterceptor maps HTTP errors to typed `AppHttpError`
The `ErrorInterceptor` (`core/interceptors/error.interceptor.ts`) SHALL be an `HttpInterceptorFn` that catches any `HttpErrorResponse`, maps it to an `AppHttpError` object, logs it, and re-throws using `throwError(() => appHttpError)`.

#### Scenario: 4xx error is mapped and re-thrown
- **WHEN** the server responds with a 4xx status code
- **THEN** the interceptor SHALL catch the `HttpErrorResponse`, construct an `AppHttpError` with the status code and message, log it, and re-throw it as an `AppHttpError`

#### Scenario: 5xx error is mapped and re-thrown
- **WHEN** the server responds with a 5xx status code
- **THEN** the interceptor SHALL catch the `HttpErrorResponse`, construct an `AppHttpError`, log it, and re-throw it

#### Scenario: Non-HTTP errors are passed through
- **WHEN** an error that is not an `HttpErrorResponse` occurs in the pipeline
- **THEN** the interceptor SHALL re-throw the original error without mapping

### Requirement: LoadingInterceptor tracks in-flight request count
The `LoadingInterceptor` (`core/interceptors/loading.interceptor.ts`) SHALL be an `HttpInterceptorFn` that increments a shared `LoadingService` counter when a request starts and decrements it when the request completes (success, error, or cancellation).

#### Scenario: Loading state becomes true when a request is in flight
- **WHEN** the interceptor dispatches an HTTP request
- **THEN** `LoadingService.isLoading()` SHALL return `true` for the duration of that request

#### Scenario: Loading state returns to false when all requests complete
- **WHEN** all in-flight requests have completed
- **THEN** `LoadingService.isLoading()` SHALL return `false`

#### Scenario: Counter is decremented even on error
- **WHEN** a request fails with an HTTP error
- **THEN** the interceptor SHALL still decrement the counter via `finalize`

### Requirement: Interceptors are registered with `withInterceptors`
All three interceptors SHALL be registered in `app.config.ts` via `provideHttpClient(withInterceptors([authInterceptor, errorInterceptor, loadingInterceptor]))`.

#### Scenario: No class-based HTTP_INTERCEPTORS token is used
- **WHEN** reviewing `app.config.ts`
- **THEN** the file SHALL NOT contain `HTTP_INTERCEPTORS` or `withInterceptorsFromDi()`

### Requirement: `AppHttpError` is a typed class
An `AppHttpError` class SHALL be declared in `core/errors/app-http-error.ts` with properties: `status: number`, `message: string`, `body: unknown`.

#### Scenario: AppHttpError carries status and message
- **WHEN** an `HttpErrorResponse` with status 404 and `error.message = 'Not found'` is caught
- **THEN** the resulting `AppHttpError` SHALL have `status === 404` and `message === 'Not found'`
