-- Postgres init script. Runs once when the data volume is first created.
-- The main `giwu` database is created by POSTGRES_DB; this script adds the
-- separate database Hangfire uses for its job queue tables.

CREATE DATABASE giwu_hangfire;
