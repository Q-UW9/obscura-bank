-- 0004: accounts - E3
-- One account per user, created together with the user
-- One system *treasury* account, owned by no-one, that issues funds, allowed to go negative
-- No balance column: a balance is always SUM(ledger_entries.amount) from 0005
CREATE TABLE accounts (
    id          bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id     bigint      NULL REFERENCES users (id) ON DELETE RESTRICT,
    kind        text        NOT NULL,
    created_at  timestamptz NOT NULL DEFAULT now(),

    CONSTRAINT  accounts_kind_valid     CHECK (kind in ('user', 'treasury')),
    CONSTRAINT  accounts_user_id_unique UNIQUE (user_id),
    CONSTRAINT  accounts_kind_owner     CHECK ((kind = 'user') = (user_id IS NOT NULL))
);

-- At most one treasury: the index only covers rows where kind = 'treasury'
CREATE UNIQUE INDEX accounts_one_treasury ON accounts (kind) WHERE kind = 'treasury';

-- The treasury exists in every database: structural data, not a seed
INSERT INTO accounts (kind) VALUES ('treasury');

-- Backfill: users created before this migration (E1/E2 dev data) get their account now
INSERT INTO accounts (user_id, kind) SELECT id, 'user' FROM users ORDER BY id;