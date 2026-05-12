## 1. Environment Configuration

- [x] 1.1 Create `src/environments/environment.model.ts` — declare and export the `AppEnvironment` interface with `production`, `productServiceUrl`, `stockServiceUrl`, `paymentServiceUrl`
- [x] 1.2 Create `src/environments/environment.ts` — export `environment` typed as `AppEnvironment` with `production: true` and empty-string placeholder URLs
- [x] 1.3 Create `src/environments/environment.development.ts` — export `environment` typed as `AppEnvironment` with `production: false` and `http://localhost:500x` base URLs per service
- [x] 1.4 Verify `angular.json` development configuration has a `fileReplacements` entry swapping `environment.ts` for `environment.development.ts`; add it if missing

## 2. Core Error Type

- [x] 2.1 Create `src/app/core/errors/app-http-error.ts` — declare `AppHttpError` class with `status: number`, `message: string`, `body: unknown` properties and a constructor

## 3. Loading Service

- [x] 3.1 Create `src/app/core/services/loading.ts` — `providedIn: 'root'` service with a private `WritableSignal<number>` counter, public `increment()` and `decrement()` methods, and a public readonly `isLoading` computed signal

## 4. Auth Service

- [x] 4.1 Create `src/app/core/models/user.ts` — declare and export `User` interface with `id: string`, `email: string`, `displayName: string`
- [x] 4.2 Create `src/app/core/services/auth.ts` — `providedIn: 'root'` service named `Auth`; private `WritableSignal<User | null>` initialised to `null`; expose readonly `currentUser` signal; expose `isAuthenticated` as `computed(() => currentUser() !== null)`; stub `login()`, `logout()`, `register()` methods that log a warning and return `Promise.reject(new Error('Not implemented'))`; use `inject()` for any dependencies

## 5. HTTP Interceptors

- [x] 5.1 Create `src/app/core/interceptors/auth.ts` — `HttpInterceptorFn` that reads a token from `Auth` service; clones the request with `Authorization: Bearer <token>` header when token is present; passes the original request unchanged when token is null
- [x] 5.2 Create `src/app/core/interceptors/error.ts` — `HttpInterceptorFn` that catches `HttpErrorResponse` via `catchError`; maps it to `AppHttpError`; calls `console.error` (or a thin `log()` helper); re-throws via `throwError(() => appHttpError)`; passes non-HTTP errors through unchanged
- [x] 5.3 Create `src/app/core/interceptors/loading.ts` — `HttpInterceptorFn` that calls `Loading.increment()` before forwarding the request and calls `Loading.decrement()` inside `finalize()` on the response observable

## 6. Auth Guard

- [x] 6.1 Create `src/app/core/guards/auth.ts` — export `authGuard` as a `CanActivateFn`; inject `Auth` service and `Router`; return `true` when `auth.isAuthenticated()` is `true`; return a `UrlTree` redirecting to `/auth/login?returnUrl=<encodedPath>` when `false`

## 7. App Configuration Wiring

- [x] 7.1 Update `src/app/app.config.ts` — replace `provideHttpClient()` with `provideHttpClient(withInterceptors([authInterceptor, errorInterceptor, loadingInterceptor]))`; import `withInterceptors` from `@angular/common/http`

## 8. Feature Shell Route Files

- [x] 8.1 Create `src/app/features/auth/auth.routes.ts` — export `export default [] as Routes`
- [x] 8.2 Create `src/app/features/catalog/catalog.routes.ts` — export `export default [] as Routes`
- [x] 8.3 Create `src/app/features/product/product.routes.ts` — export `export default [] as Routes`
- [x] 8.4 Create `src/app/features/cart/cart.routes.ts` — export `export default [] as Routes`
- [x] 8.5 Create `src/app/features/checkout/checkout.routes.ts` — export `export default [] as Routes`
- [x] 8.6 Create `src/app/features/orders/orders.routes.ts` — export `export default [] as Routes`

## 9. 404 Component

- [x] 9.1 Create `src/app/features/not-found/not-found.ts` — minimal standalone component named `NotFound` with `ChangeDetectionStrategy.OnPush`; inline template displaying a "404 – Page not found" message and a link back to `/catalog`

## 10. Top-Level Routing

- [x] 10.1 Update `src/app/app.routes.ts` — add a `{ path: '', redirectTo: 'catalog', pathMatch: 'full' }` route
- [x] 10.2 Add lazy-loaded route for `auth` — `{ path: 'auth', loadChildren: () => import('./features/auth/auth.routes').then(m => m.default) }`
- [x] 10.3 Add lazy-loaded route for `catalog` — `{ path: 'catalog', loadChildren: () => import('./features/catalog/catalog.routes').then(m => m.default) }`
- [x] 10.4 Add lazy-loaded route for `product` — `{ path: 'product', loadChildren: () => import('./features/product/product.routes').then(m => m.default) }`
- [x] 10.5 Add lazy-loaded route for `cart` — `{ path: 'cart', loadChildren: () => import('./features/cart/cart.routes').then(m => m.default) }`
- [x] 10.6 Add lazy-loaded route for `checkout` — `{ path: 'checkout', loadChildren: () => import('./features/checkout/checkout.routes').then(m => m.default) }`
- [x] 10.7 Add lazy-loaded route for `orders` — `{ path: 'orders', loadChildren: () => import('./features/orders/orders.routes').then(m => m.default) }`
- [x] 10.8 Add catch-all route — `{ path: '**', loadComponent: () => import('./features/not-found/not-found').then(m => m.NotFound) }`

## 11. Shared Scaffold

- [x] 11.1 Create `src/app/shared/components/.gitkeep`
- [x] 11.2 Create `src/app/shared/pipes/.gitkeep`
- [x] 11.3 Create `src/app/shared/directives/.gitkeep`

## 12. Smoke Test

- [x] 12.1 Run `ng serve` and verify the app compiles without errors
- [x] 12.2 Navigate to `/` in the browser and confirm it redirects to `/catalog` with no console errors
- [x] 12.3 Navigate to `/does-not-exist` and confirm the `NotFoundComponent` renders
- [x] 12.4 Open the Network tab and confirm that feature chunk JS files are loaded lazily on first navigation to each route
