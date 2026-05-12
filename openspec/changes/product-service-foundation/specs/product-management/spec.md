## ADDED Requirements

### Requirement: Create a product
The system SHALL allow creating a new product with a name, optional description, base price, and an existing category.

#### Scenario: Create product successfully
- **WHEN** a POST request is sent to `/products` with a valid name, basePrice > 0, and an existing categoryId
- **THEN** the system SHALL persist the product with `IsActive = true`, `CreatedAt` and `UpdatedAt` set to current UTC time, and return `201 Created` with the created product

#### Scenario: Create product with invalid category
- **WHEN** a POST request is sent to `/products` with a `categoryId` that does not exist
- **THEN** the system SHALL return `404 Not Found`

#### Scenario: Create product with invalid price
- **WHEN** a POST request is sent to `/products` with `basePrice <= 0`
- **THEN** the system SHALL return `400 Bad Request` with validation errors

#### Scenario: Create product with missing name
- **WHEN** a POST request is sent to `/products` with an empty or missing name
- **THEN** the system SHALL return `400 Bad Request` with validation errors

### Requirement: Get a product by ID
The system SHALL return a single active product including its category information.

#### Scenario: Get existing product
- **WHEN** a GET request is sent to `/products/{id}` for an existing, active product
- **THEN** the system SHALL return `200 OK` with the product and its category name

#### Scenario: Get non-existent product
- **WHEN** a GET request is sent to `/products/{id}` for an ID that does not exist or is inactive
- **THEN** the system SHALL return `404 Not Found`

### Requirement: List products
The system SHALL return a list of active products, optionally filtered by category.

#### Scenario: List all products
- **WHEN** a GET request is sent to `/products` with no filter
- **THEN** the system SHALL return `200 OK` with all active products

#### Scenario: List products by category
- **WHEN** a GET request is sent to `/products?categoryId={id}`
- **THEN** the system SHALL return only active products belonging to the specified category

### Requirement: Update a product
The system SHALL allow updating a product's name, description, base price, and category.

#### Scenario: Update product successfully
- **WHEN** a PUT request is sent to `/products/{id}` with valid fields
- **THEN** the system SHALL persist the update, set `UpdatedAt` to current UTC time, and return `200 OK`

#### Scenario: Update non-existent product
- **WHEN** a PUT request is sent to `/products/{id}` for an ID that does not exist
- **THEN** the system SHALL return `404 Not Found`

### Requirement: Soft-delete a product
The system SHALL soft-delete a product by setting `IsActive = false`.

#### Scenario: Delete product
- **WHEN** a DELETE request is sent to `/products/{id}` for an existing, active product
- **THEN** the system SHALL set `IsActive = false` and return `204 No Content`

#### Scenario: Delete non-existent product
- **WHEN** a DELETE request is sent to `/products/{id}` for an ID that does not exist
- **THEN** the system SHALL return `404 Not Found`
