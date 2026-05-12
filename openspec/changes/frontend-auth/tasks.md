## 1. Environment and Configuration

- [ ] 1.1 Add `authServiceUrl: string` to the `AppEnvironment` interface in `src/environments/environment.model.ts`
- [ ] 1.2 Add `authServiceUrl: ''` to `src/environments/environment.ts` (production placeholder)
- [ ] 1.3 Add `authServiceUrl: 'http://localhost:5050'` to `src/environments/environment.development.ts`

## 2. AuthService Upgrade

> Depends on: 1.1 (authServiceUrl must exist before Auth can reference it)

- [ ] 2.1 Remove the `_token` signal and the public `token` readonly from `src/app/core/services/auth.ts`
- [ ] 2.2 Remove the `defaultUser` constructor workaround and initialise the private `user` signal to `null`
- [ ] 2.3 Inject `HttpClient` and `environment` into `Auth`; define the five BFF endpoint URLs as private constants derived from `environment.authServiceUrl`
- [ ] 2.4 Implement `login(email: string, password: string): Promise<void>` — POST `/login`, set `currentUser` from `response.user`, return void
- [ ] 2.5 Implement `register(email: string, password: string, displayName: string): Promise<void>` — POST `/register`, set `currentUser` from `response.user`, return void
- [ ] 2.6 Implement `logout(): Promise<void>` — POST `/logout`, set `currentUser` to `null`
- [ ] 2.7 Add public `refreshInProgress = false` property to `Auth`
- [ ] 2.8 Implement `refreshToken(): Promise<void>` — set `refreshInProgress = true`, POST `/refresh`, update `currentUser`, set `refreshInProgress = false`; ensure `refreshInProgress` is reset to `false` in the error path too
- [ ] 2.9 Implement `rehydrate(): Promise<void>` — GET `/user`, set `currentUser` from response on success; catch all errors, set `currentUser` to `null`, resolve (never reject)

## 3. APP_INITIALIZER for Rehydration

> Depends on: 2.9 (rehydrate() must exist)

- [ ] 3.1 In `src/app/app.config.ts`, import `APP_INITIALIZER` from `@angular/core`
- [ ] 3.2 Add a provider using `APP_INITIALIZER` that injects `Auth` and returns a factory function calling `auth.rehydrate()`; set `multi: true`

## 4. Interceptor Upgrades

> Depends on: 2.7–2.8 (refreshInProgress and refreshToken must exist for ErrorInterceptor)

- [ ] 4.1 Rewrite `src/app/core/interceptors/auth.ts` — remove the Bearer-header logic; clone every request with `withCredentials: true` and call `next(clonedReq)`
- [ ] 4.2 In `src/app/core/interceptors/error.ts`, add a module-level `BehaviorSubject<boolean | null>` named `refreshComplete$` to act as the retry gate
- [ ] 4.3 Upgrade the 401 branch in `errorInterceptor`: if `auth.refreshInProgress` is `true`, subscribe to `refreshComplete$`, wait for a `true` emission, then retry the original request; if `false`, set `auth.refreshInProgress = true`, call `auth.refreshToken()`, emit `true` on success and retry, or emit an error and call `auth.logout()` then navigate to `/auth/login` on failure
- [ ] 4.4 Ensure `auth.refreshInProgress` is always set back to `false` after the refresh resolves or rejects in the error interceptor

## 5. ToastService

- [ ] 5.1 Create `src/app/core/services/toast.ts` — define the `Toast` interface `{ id: string, message: string, type: 'success' | 'error' | 'info', timestamp: number }`
- [ ] 5.2 Implement `ToastService` as `providedIn: 'root'` with a private `WritableSignal<Toast[]>` initialised to `[]`
- [ ] 5.3 Implement `show(message: string, type: 'success' | 'error' | 'info'): void` — generates a unique id (e.g. `crypto.randomUUID()`), appends the toast to the signal array
- [ ] 5.4 Implement `dismiss(id: string): void` — filters out the toast with the matching id from the signal array
- [ ] 5.5 Expose a `readonly toasts` signal from the array for use by `ToastComponent`

## 6. ToastComponent

> Depends on: 5.1–5.5

- [ ] 6.1 Create `src/app/shared/components/toast/toast.ts` — standalone `ToastComponent` with `ChangeDetectionStrategy.OnPush`
- [ ] 6.2 Inject `ToastService` and expose `toasts` computed signal in the component
- [ ] 6.3 Write the template using `@for` to render each toast; apply a CSS class per `type` (`toast--success`, `toast--error`, `toast--info`); include a dismiss button calling `toast.dismiss(t.id)`
- [ ] 6.4 On each new toast appearance, schedule a `setTimeout` of 5000ms to call `toast.dismiss(id)`; store timer references and clear them in `ngOnDestroy` to prevent memory leaks
- [ ] 6.5 Add `ToastComponent` to the imports array of `AppComponent` and add `<app-toast />` (or the component's selector) to the `AppComponent` template

## 7. LoginComponent

> Depends on: 2.4 (login()), 5.3 (ToastService.show())

- [ ] 7.1 Create `src/app/features/auth/login/login.ts` — standalone `LoginComponent` with `ChangeDetectionStrategy.OnPush`
- [ ] 7.2 Inject `FormBuilder`, `Auth`, `Router`, `ActivatedRoute`, and `ToastService`
- [ ] 7.3 Build a reactive form with `email` (Validators.required + Validators.email) and `password` (Validators.required + Validators.minLength(8))
- [ ] 7.4 Write the template: email input, password input, inline validation error messages (shown after touched or submitted), submit button (disabled when form invalid), and a `routerLink` to `/auth/register`
- [ ] 7.5 Implement `onSubmit()`: if form invalid, mark all fields as touched and return; call `Auth.login(email, password)`; on success, read `returnUrl` from `route.snapshot.queryParams['returnUrl']`, decode it, navigate to it or fall back to `/`; on error, call `ToastService.show(error.message, 'error')`
- [ ] 7.6 Add `LoginComponent` to `ReactiveFormsModule` and `RouterModule` imports (or import individual directives as required by Angular 21 standalone conventions)

## 8. RegisterComponent

> Depends on: 2.5 (register()), 5.3 (ToastService.show())

- [ ] 8.1 Create `src/app/features/auth/register/register.ts` — standalone `RegisterComponent` with `ChangeDetectionStrategy.OnPush`
- [ ] 8.2 Inject `FormBuilder`, `Auth`, `Router`, and `ToastService`
- [ ] 8.3 Build a reactive form with `email` (Validators.required + Validators.email), `password` (Validators.required + Validators.minLength(8)), and `displayName` (Validators.required)
- [ ] 8.4 Write the template: email input, password input, displayName input, inline validation error messages (shown after touched or submitted), submit button (disabled when form invalid), and a `routerLink` to `/auth/login`
- [ ] 8.5 Implement `onSubmit()`: if form invalid, mark all fields as touched and return; call `Auth.register(email, password, displayName)`; on success, navigate to `/`; on error, call `ToastService.show(error.message, 'error')`
- [ ] 8.6 Add `RegisterComponent` to `ReactiveFormsModule` and `RouterModule` imports (or import individual directives as required by Angular 21 standalone conventions)

## 9. Route Wiring

> Depends on: 7.1 (LoginComponent), 8.1 (RegisterComponent)

- [ ] 9.1 Update `src/app/features/auth/auth.routes.ts` — add `{ path: 'login', loadComponent: () => import('./login/login').then(m => m.LoginComponent) }`
- [ ] 9.2 Add `{ path: 'register', loadComponent: () => import('./register/register').then(m => m.RegisterComponent) }` to the same routes array

## 10. Smoke Verification

> Depends on: all previous tasks

- [ ] 10.1 Run `ng build` and confirm zero TypeScript compilation errors
- [ ] 10.2 Run `ng serve`, navigate to `/auth/login`, and confirm the form renders without console errors
- [ ] 10.3 Navigate to a protected route while unauthenticated and confirm redirection to `/auth/login?returnUrl=...`
- [ ] 10.4 Submit the login form with an invalid email and confirm the inline error message appears without a toast
- [ ] 10.5 Submit the login form with valid credentials (using a running BFF or a mock) and confirm `currentUser` is set and the router navigates to the `returnUrl`
- [ ] 10.6 Hard-reload the page while authenticated and confirm the user is not redirected to login (rehydration via `APP_INITIALIZER` works)
- [ ] 10.7 Confirm that a forced 401 response triggers a refresh attempt and retries the original request, not a direct logout
