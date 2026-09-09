-- Runs once, only on first initialization of an empty Postgres data volume
-- (docker-entrypoint-initdb.d). Creates the dedicated database Keycloak uses
-- when it runs in production mode (`start`). In `aspire run` Aspire creates this
-- database itself; this script only covers the published/deployed compose path.
SELECT 'CREATE DATABASE keycloak'
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = 'keycloak')\gexec
