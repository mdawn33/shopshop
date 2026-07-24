## ADDED Requirements

### Requirement: `ToastService` exposes a minimal signal-based queue
`ToastService` SHALL be a root-provided, signal-based service exposing a method
`show(message: string, variant?: 'info' | 'error')` that appends a toast entry to an internal
signal, and a way for `ToastComponent` to read and dismiss entries. It SHALL NOT depend on any
third-party notification library.

#### Scenario: A toast is requested
- **WHEN** `toastService.show('Your session has expired')` is called
- **THEN** a new toast entry becomes visible in the signal that `ToastComponent` reads, with the
  given message and default variant

### Requirement: `ToastComponent` renders active toasts and auto-dismisses
`ToastComponent` SHALL be mounted once at the application shell root (outside routed content) and
SHALL render all currently active toast entries from `ToastService`. Each toast SHALL
auto-dismiss after a fixed duration (e.g. a few seconds) and SHALL also be dismissible manually.

#### Scenario: Toast auto-dismisses
- **WHEN** a toast has been visible for its configured duration with no manual dismissal
- **THEN** it is removed from the active toast list automatically

#### Scenario: Toast is manually dismissed
- **WHEN** the user activates the toast's dismiss control before the auto-dismiss duration
  elapses
- **THEN** it is removed immediately

### Requirement: Toast usage in this change is scoped to session/auth feedback only
This change SHALL introduce exactly one consumer of `ToastService`: the session-expired message
shown by `auth-error-handling` before redirecting to `/bff/login`. No form-validation, success, or
other general-purpose toast usage SHALL be added as part of this change.

#### Scenario: Session-expired notice is the only toast triggered by this change's code
- **WHEN** reviewing all calls to `toastService.show(...)` introduced by this change
- **THEN** the only call site is the session-expired path in `errorInterceptor`
