## ADDED Requirements

### Requirement: Each feature has a `<feature>.routes.ts` entry-point file
For each of the six features (`auth`, `catalog`, `product`, `cart`, `checkout`, `orders`), a file SHALL exist at `src/app/features/<feature>/<feature>.routes.ts` that exports a `Routes` array as the default export.

#### Scenario: Feature routes file is importable as a lazy-loaded `loadChildren` target
- **WHEN** `app.routes.ts` references `() => import('./features/catalog/catalog.routes').then(m => m.default)`
- **THEN** TypeScript SHALL resolve the import and the Angular router SHALL lazy-load the routes array

#### Scenario: All six feature route files exist
- **WHEN** the file system is inspected after implementation
- **THEN** each of `auth.routes.ts`, `catalog.routes.ts`, `product.routes.ts`, `cart.routes.ts`, `checkout.routes.ts`, and `orders.routes.ts` SHALL exist in their respective feature folders

### Requirement: Feature route files export an empty `Routes` array initially
Each `<feature>.routes.ts` SHALL export `export default [] as Routes` as a placeholder until the feature is implemented.

#### Scenario: Lazy-loading a stub feature shell does not throw
- **WHEN** the router lazy-loads a feature with an empty routes array
- **THEN** no runtime error SHALL occur

### Requirement: Features do not import from each other
No file under `src/app/features/<featureA>/` SHALL contain a relative import that references any file under `src/app/features/<featureB>/` (where `featureA !== featureB`).

#### Scenario: Cross-feature import is absent in all feature files
- **WHEN** all files under `src/app/features/` are statically analyzed
- **THEN** no import path SHALL traverse into a sibling feature directory
