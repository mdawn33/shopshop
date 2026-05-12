## Context

The `web-client/` Angular 21 application was generated from the CLI scaffold and currently contains only the default `AppComponent`, an empty route array, and a bare `provideHttpClient()` call in `app.config.ts`. There is no feature folder structure, no HTTP interceptor layer, no auth-state management, and no environment configuration.

All six planned features (auth, catalog, product, cart, checkout, orders) need a shared navigational skeleton, a consistent HTTP pipeline, and a single source of truth for auth state before any of them can be developed. This design establishes those foundations without implementing any feature logic.

## Goals / Non-Goals

**Goals:**
- Define a top-level route tree that lazy-loads each feature shell
- Establish three functional interceptors covering token attachment, error normalization, and loading-state tracking
- Provide a signal-based `AuthService` singleton with stubbed methods that features can depend on immediately
- Introduce a typed `AppEnvironment` interface so microservice URLs are never magic strings
- Scaffold empty feature and shared folder structures to enforce the no-cross-feature-import rule from day one

**Non-Goals:**
- Implementing any real authentication flow (login form, token refresh, session persistence, JWT handling, or real HTTP calls to an auth backend). Mock state transitions that simulate auth responses are in scope.
- Implementing any feature UI (catalog, cart, checkout, etc.)
- Server-side rendering or pre-rendering setup
- State management beyond Angular signals (no NgRx, no Akita)
- Unit tests for stub methods (covered in a dedicated test-suite change)

## Decisions

### D1: Functional interceptors via `HttpInterceptorFn`

All interceptors use the functional API (`HttpInterceptorFn`) and are registered with `provideHttpClient(withInterceptors([authInterceptor, errorInterceptor, loadingInterceptor]))` in `app.config.ts`.

**Rationale:** Angular 21 deprecates class-based interceptors with `HTTP_INTERCEPTORS`. The functional API integrates directly with the standalone bootstrap model, removes the need for `Injectable` boilerplate, and is tree-shakable.

**Alternative considered:** Class-based interceptors with `HTTP_INTERCEPTORS` multi-provider. Rejected — deprecated in Angular 15+, incompatible with `provideHttpClient` without the `withInterceptorsFromDi()` adapter, and adds unnecessary ceremony.

### D2: Auth state in `AuthService` using Angular signals

`AuthService` exposes two public readonly signals — `currentUser` (type `User | null`) and `isAuthenticated` (a `computed()` derived from `currentUser`). Internal state is a private `WritableSignal<User | null>`.

**Rationale:** Signals give components fine-grained reactivity without RxJS subscription management. A single service in `core/` is the only permitted source of auth truth; features read it via `inject(AuthService)`.

**Alternative considered:** BehaviorSubject + async pipe. Rejected — adds RxJS ceremony and subscription lifecycle concerns in components where signals are simpler and already the Angular 21 default.

### D3: `AppEnvironment` typed interface for API URLs

A TypeScript interface `AppEnvironment` is declared in `src/environments/environment.model.ts` and imported by both `environment.ts` (production) and `environment.development.ts`. Each file exports a single `environment` constant typed as `AppEnvironment`.

```ts
export interface AppEnvironment {
  production: boolean;
  productServiceUrl: string;
  stockServiceUrl: string;
  paymentServiceUrl: string;
}
```

**Rationale:** Prevents silent typos on URL keys, enables IDE auto-complete across all services, and makes it immediately obvious when a new microservice URL needs adding.

**Alternative considered:** Untyped object literal. Rejected — no compile-time safety; URL key misspellings become runtime 404s.

### D4: `ErrorInterceptor` re-throws typed errors

`ErrorInterceptor` catches `HttpErrorResponse`, maps to a domain `AppHttpError` (status code + message + optional body), logs via `console.error` (swappable for a logger service later), and re-throws using `throwError(() => appHttpError)`.

**Rationale:** Features should catch `AppHttpError`, not raw `HttpErrorResponse`. Centralising the mapping means features never import `@angular/common/http` error types.

**Alternative considered:** Swallowing errors and returning a fallback value. Rejected — hides failures from feature-level error boundaries.

### D5: `LoadingInterceptor` exposes a signal, not a subject

A `LoadingService` singleton in `core/services/loading.service.ts` holds a private `WritableSignal<number>` (count of in-flight requests). `LoadingInterceptor` increments on request start and decrements on response/error via RxJS `finalize`. `LoadingService` exposes a `computed` boolean `isLoading`.

**Rationale:** Components can consume `isLoading` directly as a signal without subscribing, consistent with D2.

### D6: Feature isolation enforced by folder structure

Each feature lives under `src/app/features/<name>/`. The only permitted cross-feature dependency is through services in `src/app/core/`. The `shared/` folder holds only reusable UI primitives (components, pipes, directives) with zero business logic.

**Rationale:** Prevents tight coupling between features, which becomes a refactoring burden as the application grows. The rule is enforced by convention at this stage; an ESLint `import/no-relative-packages` rule can enforce it formally in a later change.

### D7: `AuthGuard` as a functional guard

`AuthGuard` is implemented as `CanActivateFn` using `inject(AuthService)`. It reads `authService.isAuthenticated()` (a signal) and redirects to `/auth/login` if false.

**Rationale:** Consistent with D1 (functional API everywhere). No class, no `Injectable`.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| `AuthService` stub methods throw `Error('Not implemented')` — any accidental early call will crash | Methods log a warning and return a rejected promise rather than throwing synchronously; features guard calls behind `isAuthenticated` checks |
| `LoadingInterceptor` count can drift if a request never completes (e.g., browser cancellation) | `finalize` in RxJS handles both completion and error; browser abort also triggers `finalize` via `HttpClient` |
| Environment files are not tree-shaken — wrong file could be bundled in production | Angular CLI `fileReplacements` in `angular.json` already handles this; the typed interface makes mismatches a compile error |
| No error reporting service yet — `ErrorInterceptor` only logs to console | Logger call is extracted behind a thin `log(error)` helper so swapping to a real reporter (Sentry, Datadog) requires changing one line |

## Migration Plan

1. Create `src/environments/environment.model.ts` with `AppEnvironment` interface
2. Create `src/environments/environment.ts` and `environment.development.ts`
3. Verify `angular.json` has `fileReplacements` pointing to `environment.development.ts` for the `development` configuration
4. Create `LoadingService` in `core/services/`
5. Create the three interceptors in `core/interceptors/`
6. Create `AuthService` in `core/services/`
7. Create `AuthGuard` in `core/guards/`
8. Update `app.config.ts` to register interceptors and services
9. Update `app.routes.ts` with the full lazy-loaded route tree
10. Create six `features/<name>/<name>.routes.ts` entry-point files
11. Create `shared/` subdirectory placeholders

Rollback: All changes are additive. Reverting any file returns the app to scaffold state with no data loss.
