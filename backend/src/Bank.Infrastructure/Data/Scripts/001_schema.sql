-- 001_schema.sql: Initial Core Banking Schema
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ASP.NET Core Identity Tables (snake_case)
CREATE TABLE IF NOT EXISTS users (
    id UUID PRIMARY KEY,
    user_name VARCHAR(256),
    normalized_user_name VARCHAR(256),
    email VARCHAR(256),
    normalized_email VARCHAR(256),
    email_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    password_hash TEXT,
    security_stamp TEXT,
    concurrency_stamp TEXT,
    phone_number VARCHAR(32),
    phone_number_confirmed BOOLEAN NOT NULL DEFAULT FALSE,
    two_factor_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    lockout_end TIMESTAMPTZ,
    lockout_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    access_failed_count INT NOT NULL DEFAULT 0,
    customer_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMPTZ
);

CREATE TABLE IF NOT EXISTS roles (
    id UUID PRIMARY KEY,
    name VARCHAR(256),
    normalized_name VARCHAR(256),
    concurrency_stamp TEXT
);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS user_claims (
    id SERIAL PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    claim_type TEXT,
    claim_value TEXT
);

CREATE TABLE IF NOT EXISTS user_logins (
    login_provider VARCHAR(128) NOT NULL,
    provider_key VARCHAR(128) NOT NULL,
    provider_display_name TEXT,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    PRIMARY KEY (login_provider, provider_key)
);

CREATE TABLE IF NOT EXISTS role_claims (
    id SERIAL PRIMARY KEY,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    claim_type TEXT,
    claim_value TEXT
);

CREATE TABLE IF NOT EXISTS user_tokens (
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    login_provider VARCHAR(128) NOT NULL,
    name VARCHAR(128) NOT NULL,
    value TEXT,
    PRIMARY KEY (user_id, login_provider, name)
);

-- Customers
CREATE TABLE IF NOT EXISTS customers (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    customer_no VARCHAR(32) NOT NULL,
    full_name VARCHAR(256) NOT NULL,
    date_of_birth DATE NOT NULL,
    phone VARCHAR(32) NOT NULL,
    address_json JSONB NOT NULL DEFAULT '{}',
    status VARCHAR(32) NOT NULL,
    kyc_status VARCHAR(32) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    CONSTRAINT uq_customers_user_id UNIQUE (user_id),
    CONSTRAINT uq_customers_customer_no UNIQUE (customer_no)
);

-- Accounts
CREATE TABLE IF NOT EXISTS accounts (
    id UUID PRIMARY KEY,
    account_number VARCHAR(13) NOT NULL,
    customer_id UUID REFERENCES customers(id) ON DELETE RESTRICT,
    kind VARCHAR(32) NOT NULL,
    product_type VARCHAR(32) NOT NULL,
    ledger_class VARCHAR(32) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    status VARCHAR(32) NOT NULL,
    balance NUMERIC(19,4) NOT NULL DEFAULT 0.0000,
    held_amount NUMERIC(19,4) NOT NULL DEFAULT 0.0000,
    overdraft_limit NUMERIC(19,4) NOT NULL DEFAULT 0.0000,
    min_balance NUMERIC(19,4) NOT NULL DEFAULT 0.0000,
    daily_debit_limit NUMERIC(19,4),
    last_entry_seq BIGINT NOT NULL DEFAULT 0,
    opened_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    closed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ,
    version BIGINT DEFAULT 0,
    CONSTRAINT uq_accounts_account_number UNIQUE (account_number)
);

-- Transactions (append-only)
CREATE TABLE IF NOT EXISTS transactions (
    id UUID PRIMARY KEY,
    reference VARCHAR(64) NOT NULL,
    type VARCHAR(32) NOT NULL,
    status VARCHAR(32) NOT NULL,
    amount NUMERIC(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    description VARCHAR(500) NOT NULL,
    channel VARCHAR(50) NOT NULL,
    initiated_by_user_id VARCHAR(128) NOT NULL,
    on_behalf_of_admin BOOLEAN NOT NULL DEFAULT FALSE,
    idempotency_key_id UUID,
    reverses_transaction_id UUID REFERENCES transactions(id) ON DELETE RESTRICT,
    metadata_json JSONB,
    posted_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    value_date DATE NOT NULL,
    CONSTRAINT uq_transactions_reference UNIQUE (reference),
    CONSTRAINT uq_transactions_reverses_transaction_id UNIQUE (reverses_transaction_id)
);

-- Ledger Entries (append-only)
CREATE TABLE IF NOT EXISTS ledger_entries (
    id UUID PRIMARY KEY,
    transaction_id UUID NOT NULL REFERENCES transactions(id) ON DELETE RESTRICT,
    account_id UUID NOT NULL REFERENCES accounts(id) ON DELETE RESTRICT,
    direction VARCHAR(16) NOT NULL,
    amount NUMERIC(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    account_seq BIGINT NOT NULL,
    balance_after NUMERIC(19,4) NOT NULL,
    posted_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT ck_ledger_entries_amount_positive CHECK (amount > 0),
    CONSTRAINT uq_ledger_entries_account_seq UNIQUE (account_id, account_seq)
);

-- Idempotency Keys
CREATE TABLE IF NOT EXISTS idempotency_keys (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL,
    key VARCHAR(64) NOT NULL,
    endpoint VARCHAR(256) NOT NULL,
    request_hash VARCHAR(128) NOT NULL,
    status VARCHAR(32) NOT NULL,
    response_status INT,
    response_body_json JSONB,
    transaction_id UUID,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMPTZ NOT NULL,
    CONSTRAINT uq_idempotency_keys_user_key UNIQUE (user_id, key)
);

-- Refresh Tokens
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id UUID PRIMARY KEY,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    token_hash VARCHAR(64) NOT NULL,
    family_id UUID NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_ip VARCHAR(45),
    user_agent VARCHAR(512),
    revoked_at TIMESTAMPTZ,
    revoked_by_ip VARCHAR(45),
    replaced_by_id UUID REFERENCES refresh_tokens(id) ON DELETE RESTRICT,
    CONSTRAINT uq_refresh_tokens_token_hash UNIQUE (token_hash)
);

-- Audit Logs (append-only)
CREATE TABLE IF NOT EXISTS audit_logs (
    id UUID PRIMARY KEY,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    actor_user_id UUID,
    actor_role VARCHAR(64),
    actor_ip VARCHAR(45),
    user_agent VARCHAR(512),
    correlation_id VARCHAR(64),
    action VARCHAR(64) NOT NULL,
    entity_type VARCHAR(64) NOT NULL,
    entity_id VARCHAR(128),
    outcome VARCHAR(32) NOT NULL,
    before JSONB,
    after JSONB,
    metadata JSONB,
    prev_hash VARCHAR(128),
    row_hash VARCHAR(128)
);

-- Outbox Messages
CREATE TABLE IF NOT EXISTS outbox_messages (
    id UUID PRIMARY KEY,
    type VARCHAR(128) NOT NULL,
    payload_json JSONB NOT NULL,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    processed_at TIMESTAMPTZ,
    attempts INT NOT NULL DEFAULT 0,
    last_error TEXT
);

-- Reconciliation Runs & Findings
CREATE TABLE IF NOT EXISTS reconciliation_runs (
    id UUID PRIMARY KEY,
    started_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    finished_at TIMESTAMPTZ,
    status VARCHAR(32) NOT NULL,
    accounts_checked INT NOT NULL DEFAULT 0,
    discrepancies_found INT NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS reconciliation_findings (
    id UUID PRIMARY KEY,
    run_id UUID NOT NULL REFERENCES reconciliation_runs(id) ON DELETE CASCADE,
    account_id UUID NOT NULL REFERENCES accounts(id) ON DELETE RESTRICT,
    expected_balance NUMERIC(19,4) NOT NULL,
    actual_balance NUMERIC(19,4) NOT NULL,
    currency VARCHAR(3) NOT NULL,
    severity VARCHAR(32) NOT NULL,
    description VARCHAR(500) NOT NULL,
    resolved_at TIMESTAMPTZ
);

-- Indexes
CREATE INDEX IF NOT EXISTS ix_accounts_customer_id ON accounts(customer_id);
CREATE INDEX IF NOT EXISTS ix_transactions_posted_at ON transactions(posted_at DESC);
CREATE INDEX IF NOT EXISTS ix_ledger_entries_transaction_id ON ledger_entries(transaction_id);
CREATE INDEX IF NOT EXISTS ix_ledger_entries_account_posted_id ON ledger_entries(account_id, posted_at DESC, id DESC);
CREATE INDEX IF NOT EXISTS ix_idempotency_keys_expires_at ON idempotency_keys(expires_at);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_family_id ON refresh_tokens(family_id);
CREATE INDEX IF NOT EXISTS ix_refresh_tokens_user_id ON refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS ix_audit_logs_occurred_at ON audit_logs(occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_logs_entity ON audit_logs(entity_type, entity_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_logs_actor ON audit_logs(actor_user_id, occurred_at DESC);
CREATE INDEX IF NOT EXISTS ix_audit_logs_correlation_id ON audit_logs(correlation_id);
CREATE INDEX IF NOT EXISTS ix_outbox_unprocessed ON outbox_messages(occurred_at) WHERE processed_at IS NULL;
