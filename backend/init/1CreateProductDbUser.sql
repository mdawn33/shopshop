-- Create the database first (must exist before grants below)
CREATE DATABASE "ProductPgDb";

-- Create application user
CREATE USER productdbuser WITH PASSWORD 'Trey776!';

-- Grant connection to database
GRANT CONNECT ON DATABASE "ProductPgDb" TO productdbuser;

-- Make user the owner of the database (IMPORTANT)
ALTER DATABASE "ProductPgDb" OWNER TO productdbuser;

-- Switch to the database
\c ProductPgDb;

-- Make user owner of schema (VERY IMPORTANT for EF)
ALTER SCHEMA public OWNER TO productdbuser;

-- Allow schema usage
GRANT USAGE ON SCHEMA public TO productdbuser;

-- Allow creating objects (tables, indexes, etc.)
GRANT CREATE ON SCHEMA public TO productdbuser;

-- Grant full privileges on existing tables
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO productdbuser;

-- Grant full privileges on sequences
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO productdbuser;

-- Grant full privileges on functions
GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public TO productdbuser;

-- Ensure future objects automatically grant permissions
ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON TABLES TO productdbuser;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON SEQUENCES TO productdbuser;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON FUNCTIONS TO productdbuser;