## ADDED Requirements

### Requirement: AuthGuard blocks unauthenticated access to protected routes
`AuthGuard` (`core/guards/auth.guard.ts`) SHALL be a `CanActivateFn` that allows navigation only when `AuthService.isAuthenticated()` returns `true`.

#### Scenario: Authenticated user can activate a protected route
- **WHEN** a user whose `AuthService.isAuthenticated()` signal returns `true` navigates to a guarded route
- **THEN** the guard SHALL return `true` and allow the navigation to proceed

#### Scenario: Unauthenticated user is redirected to login
- **WHEN** a user whose `AuthService.isAuthenticated()` signal returns `false` navigates to a guarded route
- **THEN** the guard SHALL return a `UrlTree` that redirects to `/auth/login`

### Requirement: AuthGuard uses the functional API
`AuthGuard` SHALL be exported as a plain `CanActivateFn` function, not as an `Injectable` class.

#### Scenario: Guard is usable directly in a route definition
- **WHEN** a route definition sets `canActivate: [authGuard]`
- **THEN** the Angular router SHALL invoke the function without any `Injectable` or `provide` setup beyond what is already in `app.config.ts`

### Requirement: Redirect preserves the intended URL as a query parameter
When redirecting an unauthenticated user, the guard SHALL include a `returnUrl` query parameter with the originally requested path.

#### Scenario: Return URL is preserved after redirect
- **WHEN** an unauthenticated user navigates to `/orders`
- **THEN** the guard SHALL redirect to `/auth/login?returnUrl=%2Forders`
