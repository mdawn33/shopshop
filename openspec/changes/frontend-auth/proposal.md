## Why

The `frontend-foundation` change established stub-only implementations for `AuthService`, `AuthInterceptor`, and `ErrorInterceptor` — none of them make real HTTP calls, so the application cannot authenticate users, maintain sessions across reloads, or recover transparently from expired tokens. These stubs are now the only thing blocking real user-facing auth flows.

## What Changes

- Replace all stub methods in `AuthService` (`login`, `register`, `logout`) with real HTTP calls to the BFF, and add `refreshToken()` with a `refreshInProgress` flag for race-condition-safe token refresh
- Remove the unused `_token` signal from `AuthService`; the token now lives in an HttpOnly cookie managed by the BFF
- Add `authServiceUrl` to the `AppEnvironment` interface and both environment files
- Upgrade `AuthInterceptor` to attach `withCredentials: true` on every outgoing request instead of reading a JS-accessible token
- Upgrade `ErrorInterceptor` 401 handler with a queue-based refresh-then-retry flow to prevent the refresh race condition
- Add `APP_INITIALIZER` that calls `GET /user` on startup to rehydrate the `currentUser` signal before any route activates
- Add `ToastService` (signal-based) and `ToastComponent` (auto-dismiss) as shared UI infrastructure for displaying auth feedback
- Add `LoginComponent` at `/auth/login` with client-side validation and `returnUrl` redirect logic
- Add `RegisterComponent` at `/auth/register` with client-side validation
- Wire `LoginComponent` and `RegisterComponent` into the `auth` feature route array

## Capabilities

### New Capabilities

- `auth-service-real`: Full HTTP-backed `AuthService` with login, register, logout, refresh, and rehydration via `APP_INITIALIZER`
- `auth-interceptors`: Upgraded `AuthInterceptor` (`withCredentials: true`) and `ErrorInterceptor` (401 → refresh → retry with race guard)
- `toast-infrastructure`: Signal-based `ToastService` and auto-dismissing `ToastComponent` for global user feedback
- `login-ui`: `LoginComponent` with form validation, error display via `ToastService`, and `returnUrl` post-login redirect
- `register-ui`: `RegisterComponent` with form validation and error display via `ToastService`

### Modified Capabilities

- `environment-config`: Adding `authServiceUrl: string` to the `AppEnvironment` interface

## Impact

- **Modified file:** `src/environments/environment.model.ts` — add `authServiceUrl`
- **Modified file:** `src/environments/environment.ts` — add `authServiceUrl` placeholder
- **Modified file:** `src/environments/environment.development.ts` — add `authServiceUrl` dev URL
- **Modified file:** `src/app/core/services/auth.ts` — replace stubs with real HTTP, remove `_token`, add `refreshToken()` and `refreshInProgress`
- **Modified file:** `src/app/core/interceptors/auth.ts` — attach `withCredentials: true` instead of `Bearer` header
- **Modified file:** `src/app/core/interceptors/error.ts` — upgrade 401 path with refresh + retry queue
- **Modified file:** `src/app/app.config.ts` — register `APP_INITIALIZER`
- **Modified file:** `src/app/features/auth/auth.routes.ts` — wire `LoginComponent` and `RegisterComponent`
- **New file:** `src/app/core/services/toast.ts` — `ToastService`
- **New file:** `src/app/shared/components/toast/toast.ts` — `ToastComponent`
- **Modified file:** `src/app/app.ts` (AppComponent) — add `ToastComponent` to template
- **New file:** `src/app/features/auth/login/login.ts` — `LoginComponent`
- **New file:** `src/app/features/auth/register/register.ts` — `RegisterComponent`
- **Dependencies:** No new npm packages required
