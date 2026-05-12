## ADDED Requirements

### Requirement: `shared/` folder contains three subdirectories for UI primitives
The directory `src/app/shared/` SHALL contain three subdirectories: `components/`, `pipes/`, and `directives/`.

#### Scenario: Shared subdirectories exist after implementation
- **WHEN** the file system is inspected after implementation
- **THEN** `src/app/shared/components/`, `src/app/shared/pipes/`, and `src/app/shared/directives/` SHALL each exist

### Requirement: Each shared subdirectory has a `.gitkeep` placeholder
Because the directories contain no implementation files at this stage, each SHALL contain a `.gitkeep` file so they are tracked by version control.

#### Scenario: Empty shared directories are tracked in git
- **WHEN** `git status` is run after implementation
- **THEN** the `.gitkeep` files under `shared/components/`, `shared/pipes/`, and `shared/directives/` SHALL appear as tracked new files

### Requirement: Shared components have no business logic
All files placed under `src/app/shared/` SHALL be reusable UI primitives only (components, pipes, directives). Services, models, and business rules SHALL NOT reside in `shared/`.

#### Scenario: No service files exist under shared/
- **WHEN** the `src/app/shared/` directory is statically analyzed
- **THEN** no file with the `.service.ts` suffix SHALL be present
