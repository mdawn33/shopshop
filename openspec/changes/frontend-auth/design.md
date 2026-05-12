## Context

The `frontend-foundation` change delivered a stub `Auth` service, a placeholder `AuthInterceptor` that reads a JS-accessible `_token` signal, and an `ErrorInterceptor` that calls `logout()` on any 401 without attempting token refresh. The application cannot authenticate users, maintain sessions, or recover from expired tokens. The `auth` feature route array is empty; there are no login or register UI components.

The auth architecture uses a **BFF (Backend For Frontend)** pattern. The Angular app communicates exclusively with a BFF layer (`authServiceUrl`). The BFF issues and rotates **HttpOnly cookies** — the token is never readable by JavaScript. This eliminates an entire class of XSS token-theft attacks.

Current files of interest:
- `src/app/core/services/auth.ts` — stub `Auth` service with unused `_token` signal
- `src/app/core/interceptors/auth.ts` — reads `_token`, sends `Authorization: Bearer` header
- `src/app/core/interceptors/error.ts` — calls `logout()` on 401, no refresh attempt
- `src/environments/environment.model.ts` — missing `authServiceUrl`
- `src/app/features/auth/auth.routes.ts` — empty route array

## Goals / Non-Goals

**Goals:**
- Replace `Auth` stub methods with real BFF HTTP calls
- Remove `_token` signal (token lives in HttpOnly cookie, not accessible to JS)
- Add `refreshToken()` method and `refreshInProgress` flag to `Auth`
- Add `APP_INITIALIZER` that calls `GET /user` to rehydrate `currentUser` before first render
- Upgrade `AuthInterceptor` to attach `withCredentials: true` on every request
- Upgrade `ErrorInterceptor` to implement 401 → refresh → retry with a queue to prevent the refresh race condition
- Add `ToastService` and `ToastComponent` as shared notification infrastructure
- Implement `LoginComponent` (`/auth/login`) and `RegisterComponent` (`/auth/register`) with client-side validation
- Add `authServiceUrl` to `AppEnvironment` and both environment files

**Non-Goals:**
- Password reset, forgot-password, or email verification flows
- OAuth / social login
- Remember-me / persistent sessions (session lifetime is a BFF concern)
- Multi-tab session synchronization via `BroadcastChannel`
- Unit or integration tests (covered by a separate test-suite change)

## Decisions

### D1: BFF Pattern with HttpOnly Cookies

The Angular app never handles tokens directly. The BFF sets and clears an HttpOnly cookie. The browser includes this cookie automatically on same-site or cross-origin requests when `withCredentials: true` is set.

**Rationale:** Eliminates XSS token theft. JavaScript cannot read, modify, or log the token. The tradeoff is CSRF exposure, which the BFF mitigates via `SameSite=Strict` or a CSRF token header — that is the BFF's responsibility, not the Angular client's.

**Alternative considered:** Storing JWT in `localStorage` or a JS-accessible signal. Rejected — any XSS vulnerability would immediately expose the token.

### D2: AuthInterceptor becomes a withCredentials Attacher

Since the token is an HttpOnly cookie, `AuthInterceptor` no longer needs to read any JS state. Its sole job becomes cloning every outgoing request with `withCredentials: true`.

**Rationale:** Without `withCredentials: true`, browsers do not send cookies on cross-origin requests (CORS). This one-liner interceptor is still the correct place for this concern — it applies globally without every feature explicitly setting it.

**Alternative considered:** Setting `withCredentials: true` per-request in each service call. Rejected — easy to forget, creates bugs that only appear in CORS-enabled environments.

### D3: Refresh Race Condition Guard via Subject Queue

When multiple parallel requests receive 401 simultaneously, all of them should wait for a single refresh call rather than each triggering its own `POST /refresh`. The `ErrorInterceptor` implements this with a `refreshInProgress` flag on `Auth` and a `BehaviorSubject` acting as a gate:

1. First 401 arrives: set `refreshInProgress = true`, call `POST /refresh`
2. Subsequent 401s while refresh is in flight: subscribe to a `refreshComplete$` subject and wait
3. When refresh succeeds: emit on `refreshComplete$`, all waiting requests retry
4. When refresh fails: emit error on `refreshComplete$`, call `logout()`, redirect to `/auth/login`

**Rationale:** Without this guard, N parallel 401s produce N concurrent refresh calls. The first succeeds and rotates the cookie; the remaining N-1 then fail (old cookie is already invalid), cascade-logging the user out. The queue pattern serializes the refresh.

**Alternative considered:** RxJS `shareReplay(1)` on the refresh observable. Works but is harder to reset after the refresh completes; the Subject queue is more explicit and easier to reason about.

### D4: APP_INITIALIZER for Session Rehydration

An `APP_INITIALIZER` factory calls `GET /user` before the application renders. If the cookie is valid, the BFF returns `{ user }` and `Auth.currentUser` is set. If not, `currentUser` stays `null`. Either way, `AuthGuard` sees the correct state before any route activates.

**Rationale:** Without rehydration, a hard reload clears `currentUser` (signals are in-memory), causing `AuthGuard` to redirect authenticated users to login on every page refresh. `APP_INITIALIZER` blocks rendering until the check resolves, preventing the flash.

**Alternative considered:** Lazy rehydration on first `AuthGuard` execution. Rejected — `AuthGuard` runs before the app shell renders, making lazy rehydration essentially the same timing with more complexity.

### D5: ToastService as Signal-Based Array

`ToastService` holds a `WritableSignal<Toast[]>` where each `Toast` is `{ id: string, message: string, type: 'success' | 'error' | 'info', timestamp: number }`. `show()` appends; `dismiss(id)` filters out. `ToastComponent` uses `setTimeout` to auto-dismiss after 5 seconds.

**Rationale:** Signal array keeps the toast list reactive without RxJS. The 5-second auto-dismiss is handled client-side so the BFF does not need to coordinate display timing. The `id` field allows programmatic dismissal before timeout.

**Alternative considered:** RxJS `Subject` + `async` pipe. Rejected — inconsistent with the signals-first convention from `frontend-foundation`.

### D6: Reactive Forms for Login and Register

Both `LoginComponent` and `RegisterComponent` use Angular Reactive Forms with `FormBuilder`. Validation rules: `email` (required + `Validators.email`), `password` (required + `minLength(8)`), `displayName` (required, register only).

**Rationale:** Reactive forms provide programmatic validation control and are easier to unit-test. Template-driven forms with the same validation would require more template logic.

**Alternative considered:** Template-driven forms. Rejected per project convention (`web-client/.claude/CLAUDE.md`: "Prefer Reactive forms instead of Template-driven ones").

## Component / Service Diagram

```
AppComponent
  └─ ToastComponent (shared, always rendered)

AuthGuard
  └─ injects Auth

APP_INITIALIZER
  └─ calls Auth.rehydrate() → GET /user

Auth (core service)
  ├─ login()     → POST /auth/login
  ├─ register()  → POST /auth/register
  ├─ logout()    → POST /auth/logout
  ├─ refreshToken() → POST /auth/refresh
  └─ rehydrate() → GET /auth/user

AuthInterceptor
  └─ attaches withCredentials: true to every request

ErrorInterceptor
  ├─ non-401: map to AppHttpError, rethrow
  └─ 401: check Auth.refreshInProgress
           ├─ true  → queue, wait for refreshComplete$, retry
           └─ false → set refreshInProgress=true, POST /refresh
                       ├─ success → refreshInProgress=false, emit refreshComplete$, retry
                       └─ fail    → refreshInProgress=false, logout(), navigate /auth/login

LoginComponent (features/auth/login/)
  ├─ injects Auth, Router, ActivatedRoute, ToastService
  └─ on success: navigate to returnUrl or '/'

RegisterComponent (features/auth/register/)
  ├─ injects Auth, Router, ToastService
  └─ on success: navigate to '/'
```

## Data Flows

### Login Flow

```
User submits form
  → LoginComponent calls Auth.login(email, password)
    → Auth posts to POST /auth/login
      → BFF validates credentials
        ├─ success: BFF sets HttpOnly cookie, returns { user }
        │   Auth sets currentUser signal
        │   LoginComponent reads returnUrl from query params
        │   Router.navigate(returnUrl || '/')
        └─ error: AppHttpError thrown
            LoginComponent catches, calls ToastService.show(message, 'error')
```

### Reload Rehydration Flow

```
Browser reloads
  → Angular bootstrap starts
    → APP_INITIALIZER fires Auth.rehydrate()
      → GET /auth/user (cookie sent automatically by browser)
        ├─ 200: Auth sets currentUser from response
        └─ 401/error: Auth leaves currentUser as null
    → APP_INITIALIZER resolves (never rejects — errors are caught internally)
  → Router activates routes
    → AuthGuard reads isAuthenticated() (already correct state)
```

### 401 → Refresh → Retry Flow

```
Request A receives 401
  → ErrorInterceptor checks Auth.refreshInProgress
    └─ false: set refreshInProgress=true
       → POST /auth/refresh
         ├─ success:
         │   set refreshInProgress=false
         │   emit true on refreshComplete$
         │   retry Request A with original config
         └─ fail:
             set refreshInProgress=false
             call Auth.logout()
             navigate to /auth/login

Request B receives 401 (while Request A's refresh is in flight)
  → ErrorInterceptor checks Auth.refreshInProgress
    └─ true: subscribe to refreshComplete$
       ├─ on true emit: retry Request B
       └─ on error emit: do nothing (logout already triggered)
```

## BFF API Contract

| Method | Path | Request Body | Response Body | Side Effect |
|--------|------|-------------|---------------|-------------|
| `POST` | `/login` | `{ email: string, password: string }` | `{ user: { id, email, displayName } }` | BFF sets HttpOnly session cookie |
| `POST` | `/register` | `{ email: string, password: string, displayName: string }` | `{ user: { id, email, displayName } }` | BFF sets HttpOnly session cookie |
| `POST` | `/logout` | (none) | (none) | BFF clears HttpOnly cookie |
| `POST` | `/refresh` | (none) | `{ user: { id, email, displayName } }` | BFF rotates HttpOnly cookie |
| `GET` | `/user` | (none) | `{ user: { id, email, displayName } }` | (none) |

All endpoints are relative to `environment.authServiceUrl`. All requests include `withCredentials: true` (set by `AuthInterceptor`).

## isAuthenticated State Machine

```
States: UNKNOWN (before init) → AUTHENTICATED | UNAUTHENTICATED

UNKNOWN
  → APP_INITIALIZER GET /user succeeds  → AUTHENTICATED
  → APP_INITIALIZER GET /user fails     → UNAUTHENTICATED

UNAUTHENTICATED
  → login() succeeds                    → AUTHENTICATED
  → register() succeeds                 → AUTHENTICATED

AUTHENTICATED
  → logout() called                     → UNAUTHENTICATED
  → refresh() fails                     → UNAUTHENTICATED
  → ErrorInterceptor refresh fails      → UNAUTHENTICATED
```

## Files Changed

| File | Status | Notes |
|------|--------|-------|
| `src/environments/environment.model.ts` | Modified | Add `authServiceUrl: string` |
| `src/environments/environment.ts` | Modified | Add `authServiceUrl: ''` |
| `src/environments/environment.development.ts` | Modified | Add `authServiceUrl: 'http://localhost:5050'` |
| `src/app/core/services/auth.ts` | Modified | Replace stubs, remove `_token`, add `refreshToken()`, `rehydrate()`, `refreshInProgress` |
| `src/app/core/interceptors/auth.ts` | Modified | Replace Bearer-header logic with `withCredentials: true` |
| `src/app/core/interceptors/error.ts` | Modified | Add 401 → refresh → retry queue |
| `src/app/app.config.ts` | Modified | Register `APP_INITIALIZER` |
| `src/app/app.ts` | Modified | Add `ToastComponent` to template |
| `src/app/features/auth/auth.routes.ts` | Modified | Wire `LoginComponent` and `RegisterComponent` |
| `src/app/core/services/toast.ts` | New | `ToastService` |
| `src/app/shared/components/toast/toast.ts` | New | `ToastComponent` |
| `src/app/features/auth/login/login.ts` | New | `LoginComponent` |
| `src/app/features/auth/register/register.ts` | New | `RegisterComponent` |

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| `APP_INITIALIZER` blocks first render — slow BFF response delays the app | The initializer catches errors internally and resolves immediately on failure; worst case is one slow network round-trip on cold start |
| `withCredentials: true` on every request requires CORS `Access-Control-Allow-Credentials: true` on all backend services | This is a backend configuration concern; flag it in the BFF setup guide |
| Refresh race condition guard uses a module-level `Subject` — if the module is lazy-loaded, multiple instances could exist | `Auth` is `providedIn: 'root'`; `ErrorInterceptor` injects `Auth` for the `refreshInProgress` flag, ensuring a single instance |
| `ToastComponent` uses `setTimeout` for auto-dismiss — timers don't clean up on component destroy in tests | Auto-dismiss timer is cleared in `ngOnDestroy`; tests can use `fakeAsync` + `tick(5000)` |
| Login/register forms show errors only via toast — inline field errors also improve UX | Client-side validation state is accessible via form control `.errors`; inline errors can be added to the template without changing the service layer |
