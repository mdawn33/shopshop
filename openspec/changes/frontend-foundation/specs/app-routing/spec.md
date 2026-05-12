## ADDED Requirements

### Requirement: Top-level routes are lazy-loaded per feature
The application SHALL define a top-level route array in `src/app/app.routes.ts` where each feature area (`auth`, `catalog`, `product`, `cart`, `checkout`, `orders`) is a separate lazy-loaded route using `loadChildren`.

#### Scenario: Feature route loads its shell
- **WHEN** the user navigates to a feature path (e.g., `/catalog`)
- **THEN** the router SHALL dynamically import the corresponding `<feature>.routes.ts` file without including it in the initial bundle

#### Scenario: Root path redirects to catalog
- **WHEN** the user navigates to `/`
- **THEN** the router SHALL redirect to `/catalog` with `pathMatch: 'full'`

#### Scenario: Unknown path shows 404
- **WHEN** the user navigates to a path that matches no defined route
- **THEN** the router SHALL activate a catch-all route (`**`) that renders a `NotFoundComponent`

### Requirement: Route paths use kebab-case
The application SHALL define all route paths in kebab-case (e.g., `/checkout`, `/orders`).

#### Scenario: Multi-word paths are kebab-case
- **WHEN** a route path contains multiple words
- **THEN** it SHALL use hyphens as separators (e.g., `not-found`, not `notFound`)

### Requirement: `NotFoundComponent` is a standalone component
The catch-all route SHALL use a standalone `NotFoundComponent` loaded via `loadComponent`.

#### Scenario: 404 component is not included in the initial bundle
- **WHEN** the user visits a valid route
- **THEN** the `NotFoundComponent` module SHALL NOT be part of the initial JavaScript bundle
