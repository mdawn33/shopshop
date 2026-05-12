## Why

The Shoppiness web-client (`web-client/`) is an empty Angular 21 scaffold with no routing, no HTTP infrastructure, and no feature organization. Without a navigational skeleton, interceptor layer, and auth-state foundation, no frontend feature can be built — every subsequent feature change would be blocked or would duplicate this setup ad-hoc.

## What Changes

- Add top-level lazy-loaded routing with shells for `auth`, `catalog`, `product`, `cart`, `checkout`, and `orders`, plus a default redirect and a 404 catch-all
- Add `core/interceptors/`: `AuthInterceptor`, `ErrorInterceptor`, and `LoadingInterceptor` using the functional `HttpInterceptorFn` API
- Add `core/guards/auth.guard.ts` to protect authenticated routes
- Add `core/services/auth.service.ts` with signal-based `currentUser` and `isAuthenticated` state; `login()`, `logout()`, `register()` methods stubbed for future implementation
- Add `src/environments/environment.ts` and `environment.development.ts` with a typed `AppEnvironment` interface carrying per-service API base URLs
- Add empty feature entry-point route files under `features/<name>/<name>.routes.ts` for each of the six features
- Add empty placeholder files under `shared/components/`, `shared/pipes/`, and `shared/directives/`
- Wire interceptors and HTTP client in `app.config.ts` using `provideHttpClient(withInterceptors([...]))`

## Capabilities

### New Capabilities

- `app-routing`: Top-level route tree with lazy-loaded feature shells, default redirect, and 404 catch-all
- `http-interceptors`: Functional interceptor stack — auth token attachment, global error handling, in-flight loading tracking
- `auth-guard`: Route guard that redirects unauthenticated users to the auth feature
- `auth-service`: Signal-based auth state singleton; stubbed login/logout/register methods
- `environment-config`: Typed `AppEnvironment` interface and per-environment files with microservice API base URLs
- `feature-shells`: Lazy-load entry-point route files for `auth`, `catalog`, `product`, `cart`, `checkout`, `orders`
- `shared-scaffold`: Empty `shared/components/`, `shared/pipes/`, `shared/directives/` structure ready for cross-feature UI primitives

### Modified Capabilities

## Impact

- **Modified file:** `src/app/app.routes.ts` — replaces empty routes with full lazy-loaded tree
- **Modified file:** `src/app/app.config.ts` — adds `provideHttpClient(withInterceptors([...]))` and registers interceptors
- **New files:** `src/app/core/interceptors/auth.interceptor.ts`, `error.interceptor.ts`, `loading.interceptor.ts`
- **New file:** `src/app/core/guards/auth.guard.ts`
- **New file:** `src/app/core/services/auth.service.ts`
- **New files:** `src/environments/environment.ts`, `src/environments/environment.development.ts`
- **New files:** `src/app/features/<name>/<name>.routes.ts` × 6
- **New files:** `src/app/shared/components/.gitkeep`, `shared/pipes/.gitkeep`, `shared/directives/.gitkeep`
- **Dependencies:** No new npm packages — all capabilities use Angular 21 built-ins
