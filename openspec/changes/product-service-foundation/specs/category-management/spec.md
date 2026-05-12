## ADDED Requirements

### Requirement: Create a category
The system SHALL allow creating a new category with a name, optional description, and optional parent category. A category with no parent is a root category.

#### Scenario: Create root category
- **WHEN** a POST request is sent to `/categories` with a valid name and no parentCategoryId
- **THEN** the system SHALL persist the category with `IsActive = true`, `CreatedAt` and `UpdatedAt` set to the current UTC time, and return `201 Created` with the created category

#### Scenario: Create subcategory
- **WHEN** a POST request is sent to `/categories` with a valid name and an existing `parentCategoryId`
- **THEN** the system SHALL persist the category linked to its parent and return `201 Created`

#### Scenario: Create category with non-existent parent
- **WHEN** a POST request is sent to `/categories` with a `parentCategoryId` that does not exist
- **THEN** the system SHALL return `404 Not Found`

#### Scenario: Create category with missing name
- **WHEN** a POST request is sent to `/categories` with an empty or missing name
- **THEN** the system SHALL return `400 Bad Request` with validation errors

### Requirement: Get a category by ID
The system SHALL return a single category including its parent reference and subcategories list.

#### Scenario: Get existing category
- **WHEN** a GET request is sent to `/categories/{id}` for an existing, active category
- **THEN** the system SHALL return `200 OK` with the category, its `parentCategoryId`, and its direct subcategories

#### Scenario: Get non-existent category
- **WHEN** a GET request is sent to `/categories/{id}` for an ID that does not exist or is inactive
- **THEN** the system SHALL return `404 Not Found`

### Requirement: List categories
The system SHALL return a flat list of active categories, optionally filtered by parent.

#### Scenario: List all root categories
- **WHEN** a GET request is sent to `/categories` with no filter
- **THEN** the system SHALL return `200 OK` with all active categories

#### Scenario: List subcategories of a parent
- **WHEN** a GET request is sent to `/categories?parentCategoryId={id}`
- **THEN** the system SHALL return only direct children of the specified parent

### Requirement: Update a category
The system SHALL allow updating a category's name, description, and parent.

#### Scenario: Update category name
- **WHEN** a PUT request is sent to `/categories/{id}` with a new name
- **THEN** the system SHALL persist the update, set `UpdatedAt` to current UTC time, and return `200 OK`

#### Scenario: Update non-existent category
- **WHEN** a PUT request is sent to `/categories/{id}` for an ID that does not exist
- **THEN** the system SHALL return `404 Not Found`

### Requirement: Soft-delete a category
The system SHALL soft-delete a category by setting `IsActive = false`. Deleting a category with active children SHALL be rejected.

#### Scenario: Delete leaf category
- **WHEN** a DELETE request is sent to `/categories/{id}` for a category with no active subcategories
- **THEN** the system SHALL set `IsActive = false` and return `204 No Content`

#### Scenario: Delete category with children
- **WHEN** a DELETE request is sent to `/categories/{id}` for a category that has active subcategories
- **THEN** the system SHALL return `409 Conflict` with a descriptive error message
