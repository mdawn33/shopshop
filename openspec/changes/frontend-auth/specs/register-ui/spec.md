## ADDED Requirements

### Requirement: RegisterComponent renders a reactive register form
`RegisterComponent` SHALL be a standalone component at `src/app/features/auth/register/register.ts` using Reactive Forms with `ChangeDetectionStrategy.OnPush`. It SHALL render an `email` field (required, valid email format), a `password` field (required, minimum 8 characters), a `displayName` field (required), a submit button, and a link to `/auth/login`.

#### Scenario: Form renders all required fields
- **WHEN** the user navigates to `/auth/register`
- **THEN** the page displays an email input, a password input, a displayName input, a submit button, and a link to `/auth/login`

#### Scenario: Submit is disabled while form is invalid
- **WHEN** any required field is empty or invalid
- **THEN** the submit button is disabled

### Requirement: RegisterComponent validates fields client-side
The register form SHALL mark fields as invalid and display an error message in the UI when `email` does not match email format, `password` is fewer than 8 characters, or `displayName` is empty, but only after the field has been touched or the form has been submitted.

#### Scenario: Invalid email shows validation error
- **WHEN** the user enters `notanemail` in the email field and blurs the field
- **THEN** an error message indicating invalid email is displayed

#### Scenario: Short password shows validation error
- **WHEN** the user enters fewer than 8 characters in the password field and blurs the field
- **THEN** an error message indicating minimum length is displayed

#### Scenario: Empty displayName shows validation error
- **WHEN** the user clears the displayName field and blurs it
- **THEN** an error message indicating the field is required is displayed

### Requirement: RegisterComponent calls Auth.register on submit
When the form is valid and submitted, `RegisterComponent` SHALL call `Auth.register(email, password, displayName)`.

#### Scenario: Valid form submit calls auth register
- **WHEN** the user fills in a valid email, password, and displayName and clicks submit
- **THEN** `Auth.register(email, password, displayName)` is called with the form values

### Requirement: Successful registration redirects to root
After a successful `Auth.register()` call, `RegisterComponent` SHALL navigate to `/`.

#### Scenario: Successful registration navigates to root
- **WHEN** `Auth.register()` resolves successfully
- **THEN** the router navigates to `/`

### Requirement: Registration errors are shown via ToastService
If `Auth.register()` rejects, `RegisterComponent` SHALL call `ToastService.show(errorMessage, 'error')` and not navigate away.

#### Scenario: Registration failure shows error toast
- **WHEN** `Auth.register()` rejects with an `AppHttpError`
- **THEN** `ToastService.show` is called with a non-empty message and type `'error'`
- **THEN** the router does not navigate
