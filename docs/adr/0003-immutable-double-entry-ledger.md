# ADR 0003: Immutable Double-Entry Ledger and Deferred Constraint Triggers

## Status
Accepted

## Context
A financial core system requires strict debit/credit balance equality for all journal transactions and permanent immutability of financial records.

## Decision
1. **Append-Only Immutability**: `transactions` and `ledger_entries` are immutable. PostgreSQL `BEFORE UPDATE OR DELETE` triggers raise an unrecoverable exception if modification or deletion is attempted.
2. **Deferred Constraint Trigger**: Every transaction must have at least 2 ledger entries and satisfy `SUM(debits) = SUM(credits)` per currency. This invariant is enforced at the database level via a `DEFERRABLE INITIALLY DEFERRED` constraint trigger on transaction commit.
3. **Compensating Reversals**: Reversals do not delete or modify posted transactions; they post a full compensating journal entry referencing the original transaction.

## Consequences
- Guaranteed accounting invariants enforced at the database level independently of application bugs.
- Full auditability compliant with regulatory financial standards.
