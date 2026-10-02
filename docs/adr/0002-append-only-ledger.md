# ADR 0002: Append-only wallet ledger with idempotency
Status: Accepted
Decision: All coin movement is an immutable ledger row written in the same DB transaction as the balance update.
Unique idempotency key per player; settlement unique per match. Balance cannot go negative (CHECK + row lock).
Consequences: Full audit trail, safe retries, reconciliation possible.
