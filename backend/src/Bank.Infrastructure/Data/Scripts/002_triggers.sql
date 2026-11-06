-- 002_triggers.sql: PostgreSQL Integrity Triggers and Check Constraints

-- 1. Immutability trigger function
CREATE OR REPLACE FUNCTION prevent_immutable_modification()
RETURNS TRIGGER AS $$
BEGIN
    RAISE EXCEPTION 'UPDATE and DELETE operations are not permitted on table % (append-only ledger/audit).', TG_TABLE_NAME
    USING ERRCODE = '55000';
END;
$$ LANGUAGE plpgsql;

-- Apply immutability triggers
DROP TRIGGER IF EXISTS trg_transactions_immutable ON transactions;
CREATE TRIGGER trg_transactions_immutable
BEFORE UPDATE OR DELETE ON transactions
FOR EACH ROW
EXECUTE FUNCTION prevent_immutable_modification();

DROP TRIGGER IF EXISTS trg_ledger_entries_immutable ON ledger_entries;
CREATE TRIGGER trg_ledger_entries_immutable
BEFORE UPDATE OR DELETE ON ledger_entries
FOR EACH ROW
EXECUTE FUNCTION prevent_immutable_modification();

DROP TRIGGER IF EXISTS trg_audit_logs_immutable ON audit_logs;
CREATE TRIGGER trg_audit_logs_immutable
BEFORE UPDATE OR DELETE ON audit_logs
FOR EACH ROW
EXECUTE FUNCTION prevent_immutable_modification();

-- 2. Balanced journal check function (DEFERRABLE INITIALLY DEFERRED constraint trigger)
CREATE OR REPLACE FUNCTION check_transaction_balanced()
RETURNS TRIGGER AS $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        SELECT transaction_id, currency,
               COUNT(*) AS entry_count,
               COALESCE(SUM(CASE WHEN direction = 'Debit' THEN amount ELSE 0 END), 0) AS total_debits,
               COALESCE(SUM(CASE WHEN direction = 'Credit' THEN amount ELSE 0 END), 0) AS total_credits
        FROM ledger_entries
        WHERE transaction_id = NEW.transaction_id
        GROUP BY transaction_id, currency
    LOOP
        IF r.entry_count < 2 THEN
            RAISE EXCEPTION 'Transaction % must have at least 2 ledger entries, found %.', r.transaction_id, r.entry_count
            USING ERRCODE = '23514';
        END IF;

        IF r.total_debits <> r.total_credits THEN
            RAISE EXCEPTION 'Transaction % is unbalanced for currency %: total debits (%) <> total credits (%). Difference: %',
                r.transaction_id, r.currency, r.total_debits, r.total_credits, ABS(r.total_debits - r.total_credits)
            USING ERRCODE = '23514';
        END IF;
    END LOOP;

    RETURN NULL;
END;
$$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_ledger_entries_balanced ON ledger_entries;
CREATE CONSTRAINT TRIGGER trg_ledger_entries_balanced
AFTER INSERT ON ledger_entries
DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW
EXECUTE FUNCTION check_transaction_balanced();

-- 3. Overdraft check constraint on accounts
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_accounts_overdraft'
    ) THEN
        ALTER TABLE accounts
        ADD CONSTRAINT ck_accounts_overdraft
        CHECK (kind <> 'Customer' OR balance + overdraft_limit >= 0);
    END IF;
END $$;
