-- Runs once, the first time the container starts with an empty volume.
-- The xUnit tests use this separate database so they can wipe it freely.
CREATE DATABASE obscura_test OWNER obscura;
