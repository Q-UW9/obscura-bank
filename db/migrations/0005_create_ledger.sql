-- 0005: the ledger - E3
-- Money only moves as a transfer:
-- a group of entries that sums to zero
-- Append-only:
-- rows are never updated or deleted, corrections are new reversing transfers
CREATE TABLE transfers (
    id          bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    kind        text        NOT NULL,
    created_at  timestamptz NOT NULL,

    CONSTRAINT transfers_kind_valid CHECK (kind in ('issue', 'transfer'))
);

CREATE TABLE ledger_entries (
    id          bigint      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    transfer_id bigint      NOT NULL REFERENCES transfers (id) ON DELETE RESTRICT,
    account_id  bigint      NOT NULL REFERENCES accounts (id) ON DELETE RESTRICT,
    -- integer piconero:
    -- negative = debit
    -- positive = credit
    amount     bigint      NOT NULL, 

    CONSTRAINT ledger_entries_amount_nonzero CHECK (amount <> 0)
);

-- Balance = SUM per account
-- E5 history in id order
CREATE INDEX ledger_entries_account_id_idx ON ledger_entries (account_id, id);
CREATE INDEX ledger_entries_transfer_id_idx ON ledger_entries (transfer_id);

-- Sum to zero:
-- checked at COMMIT, so the entries of one transfer can be inserted one by one
CREATE FUNCTION ledger_check_transfer_balanced() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    IF (SELECT SUM(amount) FROM ledger_entries WHERE transfer_id = NEW.transfer_id) <> 0 THEN
        RAISE EXCEPTION 'transfer % does not sum to zero', NEW.transfer_id
            USING ERRCODE = 'check_violation';
    END IF;
    RETURN NULL;
END $$;

CREATE CONSTRAINT TRIGGER ledger_entries_balanced
    AFTER INSERT ON ledger_entries
    DEFERRABLE INITIALLY DEFERRED
    FOR EACH ROW EXECUTE FUNCTION ledger_check_transfer_balanced();

-- Append-only:
-- any UPDATE or DELETE on the ledger is an error
CREATE FUNCTION ledger_reject_change() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION '% is append-only - % is not allowed', TG_TABLE_NAME, TG_OP;
END $$;

CREATE TRIGGER ledger_entries_append_only
    BEFORE UPDATE OR DELETE ON ledger_entries
    FOR EACH ROW EXECUTE FUNCTION ledger_reject_change();

CREATE TRIGGER transfers_append_only
    BEFORE UPDATE OR DELETE ON transfers
    FOR EACH ROW EXECUTE FUNCTION ledger_reject_change();