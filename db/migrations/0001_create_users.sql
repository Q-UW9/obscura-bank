-- 0001: create users table (E1)
-- id is an identity column; IDs never appear in URLs, E8 ownership checks protect access
-- email is normalized in C# (trim + lowercase); the CHECK rejects anything that skipped that
-- password_hash holds the full Argon2id PHC string (algorithm, params, salt, hash)
-- role is limited to 'user' / 'admin' by a CHECK, not a separate table
-- lockout, session and 2FA columns come later with their own migrations
CREATE TABLE users (
    id              bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    email           text        NOT NULL,
    password_hash   text        NOT NULL,
    role            text        NOT NULL DEFAULT 'user',
    is_frozen       boolean     NOT NULL DEFAULT false,
    created_at      timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT users_email_unique       UNIQUE (email),
    CONSTRAINT users_email_normalized   CHECK (email = lower(btrim(email))),
    CONSTRAINT users_role_valid         CHECK (role IN ('user', 'admin'))
)