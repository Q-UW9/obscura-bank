-- 0003: server-side sessions (E2)
-- The cookie holds a random token
-- Only SHA-256(token) is stored, so a stolen db yields no usable sessions.
-- All timestamps come from the API's TimeProvider, so there are deliberately no
-- DEFAULT now() values.
CREATE TABLE sessions (
    id              bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id         bigint      NOT NULL REFERENCES users (id) ON DELETE CASCADE,
    token_hash      bytea       NOT NULL,
    created_at      timestamptz NOT NULL,
    last_seen_at    timestamptz NOT NULL,
    expires_at      timestamptz NOT NULL,

    CONSTRAINT sessions_token_hash_unique UNIQUE (token_hash),
    CONSTRAINT sessions_token_hash_length CHECK (octet_length(token_hash) = 32),
    CONSTRAINT sessions_expiry_after_creation CHECK (expires_at > created_at)
);

-- Foreign keys are not indexed automatically in PostgreSQL.
-- Needed for "delete all sessions of a user"
-- (ON DELETE CASCADE, and later a 'sign out everywhere')
CREATE INDEX sessions_user_id_idx ON sessions (user_id);