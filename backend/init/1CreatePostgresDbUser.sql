-- Create the database first (must exist before grants below)
CREATE DATABASE "ShoppinessPgDb";

-- Create application user
CREATE USER shoppinessdbuser WITH PASSWORD 'Trey776!';

-- Grant connection to database
GRANT CONNECT ON DATABASE "ProductPgDb" TO shoppinessdbuser;

-- Make user the owner of the database (IMPORTANT)
ALTER DATABASE "ShoppinessPgDb" OWNER TO shoppinessdbuser;

-- Switch to the database
\c ShoppinessPgDb;

-- Make user owner of schema (VERY IMPORTANT for EF)
ALTER SCHEMA public OWNER TO shoppinessdbuser;

-- Allow schema usage
GRANT USAGE ON SCHEMA public TO shoppinessdbuser;

-- Allow creating objects (tables, indexes, etc.)
GRANT CREATE ON SCHEMA public TO shoppinessdbuser;

-- Grant full privileges on existing tables
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO shoppinessdbuser;

-- Grant full privileges on sequences
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO shoppinessdbuser;

-- Grant full privileges on functions
GRANT ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public TO shoppinessdbuser;

-- Ensure future objects automatically grant permissions
ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON TABLES TO shoppinessdbuser;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON SEQUENCES TO shoppinessdbuser;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
GRANT ALL PRIVILEGES ON FUNCTIONS TO shoppinessdbuser;