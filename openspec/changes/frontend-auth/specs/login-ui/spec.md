## ADDED Requirements

### Requirement: LoginComponent renders a reactive login form
`LoginComponent` SHALL be a standalone component at `src/app/features/auth/login/login.ts` using Reactive Forms with `ChangeDetectionStrategy.OnPush`. It SHALL render an `email` field (required, valid email format) and a `password` field (required, minimum 8 characters), a submit button, and a link to `/auth/register`.

#### Scenario: Form renders all required fields
- **WHEN** the user navigates to `/auth/login`
- **THEN** the page displays an email input, a password input, a submit button, and a link to `/auth/register`

#### Scenario: Submit is disabled while form is invalid
- **WHEN** the email or password field is empty or invalid
- **THEN** the submit button is disabled

### Requirement: LoginComponent validates fields client-side
The login form SHALL mark fields as invalid and display an error message in the UI when `email` does not match email format or `password` is fewer than 8 characters, but only after the field has been touched or the form has been submitted.

#### Scenario: Invalid email shows validation error
- **WHEN** the user enters `notanemail` in the email field and blurs the field
- **THEN** an error message indicating invalid email is displayed

#### Scenario: Short password shows validation error
- **WHEN** the user enters fewer than 8 characters in the password field and blurs the field
- **THEN** an error message indicating minimum length is displayed

### Requirement: LoginComponent calls Auth.login on submit
When the form is valid and submitted, `LoginComponent` SHALL call `Auth.login(email, password)`.

#### Scenario: Valid form submit calls auth login
- **WHEN** the user fills in a valid email and password and clicks submit
- **THEN** `Auth.login(email, password)` is called with the form values

### Requirement: Successful login redirects to returnUrl
After a successful `Auth.login()` call, `LoginComponent` SHALL read the `returnUrl` query parameter from the current route. If present, it SHALL navigate to that URL. If absent, it SHALL navigate to `/`.

#### Scenario: returnUrl query param is used after login
- **WHEN** the user logs in successfully and the route has `?returnUrl=%2Fcart`
- **THEN** the router navigates to `/cart`

#### Scenario: Missing returnUrl defaults to root
- **WHEN** the user logs in successfully and there is no `returnUrl` query param
- **THEN** the router navigates to `/`

### Requirement: Login errors are shown via ToastService
If `Auth.login()` rejects, `LoginComponent` SHALL call `ToastService.show(errorMessage, 'error')` and not navigate away.

#### Scenario: Login failure shows error toast
- **WHEN** `Auth.login()` rejects with an `AppHttpError`
- **THEN** `ToastService.show` is called with a non-empty message and type `'error'`
- **THEN** the router does not navigate
