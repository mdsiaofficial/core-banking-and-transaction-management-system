-- 003_seed_chart_of_accounts.sql: Chart of Accounts & Base Roles Seed

-- Roles
INSERT INTO roles (id, name, normalized_name, concurrency_stamp)
VALUES
    ('018f0000-0000-7000-8000-000000000001', 'Admin', 'ADMIN', gen_random_uuid()::text),
    ('018f0000-0000-7000-8000-000000000002', 'Customer', 'CUSTOMER', gen_random_uuid()::text),
    ('018f0000-0000-7000-8000-000000000003', 'Auditor', 'AUDITOR', gen_random_uuid()::text)
ON CONFLICT (id) DO NOTHING;

-- Chart of Accounts (Internal Bank Accounts)
INSERT INTO accounts (
    id, account_number, customer_id, kind, product_type, ledger_class, currency, status,
    balance, held_amount, overdraft_limit, min_balance, daily_debit_limit, last_entry_seq,
    opened_at, created_at
) VALUES
    -- 1000 Cash / Vault (Settlement) - Asset
    (
        '018f0000-0000-7000-8000-000000001000',
        '1000000000008',
        NULL,
        'Internal',
        'Cash',
        'Asset',
        'BDT',
        'Active',
        100000000.0000,
        0.0000,
        0.0000,
        0.0000,
        NULL,
        0,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    ),
    -- 2000 Customer Deposits (Control) - Liability
    (
        '018f0000-0000-7000-8000-000000002000',
        '2000000000006',
        NULL,
        'Internal',
        'CustomerDeposits',
        'Liability',
        'BDT',
        'Active',
        0.0000,
        0.0000,
        0.0000,
        0.0000,
        NULL,
        0,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    ),
    -- 4000 Fee Income - Income
    (
        '018f0000-0000-7000-8000-000000004000',
        '4000000000002',
        NULL,
        'Internal',
        'FeeIncome',
        'Income',
        'BDT',
        'Active',
        0.0000,
        0.0000,
        0.0000,
        0.0000,
        NULL,
        0,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    ),
    -- 9000 Suspense / Adjustments - Equity
    (
        '018f0000-0000-7000-8000-000000009000',
        '9000000000001',
        NULL,
        'Internal',
        'Suspense',
        'Equity',
        'BDT',
        'Active',
        0.0000,
        0.0000,
        0.0000,
        0.0000,
        NULL,
        0,
        CURRENT_TIMESTAMP,
        CURRENT_TIMESTAMP
    )
ON CONFLICT (account_number) DO NOTHING;
