## ADDED Requirements

### Requirement: ToastService manages a signal-based toast list
The `ToastService` SHALL be a `providedIn: 'root'` service that holds a `WritableSignal<Toast[]>` where `Toast` is `{ id: string, message: string, type: 'success' | 'error' | 'info', timestamp: number }`. It SHALL expose a `show(message: string, type: 'success' | 'error' | 'info'): void` method that appends a new toast to the list with a generated unique `id` and the current timestamp. It SHALL expose a `dismiss(id: string): void` method that removes the matching toast from the list.

#### Scenario: show() appends a toast
- **WHEN** `ToastService.show('Login failed', 'error')` is called
- **THEN** the signal array contains a new entry with `message: 'Login failed'`, `type: 'error'`, a non-empty `id`, and a `timestamp`

#### Scenario: dismiss() removes the matching toast
- **WHEN** `ToastService.dismiss(id)` is called with an existing toast id
- **THEN** the toast with that `id` is removed from the signal array

#### Scenario: dismiss() with unknown id is a no-op
- **WHEN** `ToastService.dismiss('nonexistent-id')` is called
- **THEN** the signal array is unchanged

### Requirement: ToastComponent renders the toast list and auto-dismisses
The `ToastComponent` SHALL be a standalone component in `shared/components/toast/` that injects `ToastService` and renders the current list of toasts. Each toast SHALL display its `message` and apply a visual class for its `type`. Each toast SHALL be automatically dismissed 5 seconds after it appears. The component SHALL apply `ChangeDetectionStrategy.OnPush`.

#### Scenario: Toast renders with correct type class
- **WHEN** a toast with `type: 'error'` is in the list
- **THEN** the rendered element has a CSS class indicating error type

#### Scenario: Toast auto-dismisses after 5 seconds
- **WHEN** a toast is added to the list
- **THEN** after 5000ms, `ToastService.dismiss(id)` is called for that toast
- **THEN** the toast is no longer rendered

#### Scenario: ToastComponent is globally visible via AppComponent
- **WHEN** `AppComponent` is rendered
- **THEN** `ToastComponent` is present in the template and receives all toasts
