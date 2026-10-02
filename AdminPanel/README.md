# Admin panel (Next.js + TypeScript)

Operations UI for Coin Pool: overview, players, matches, wallet ledger (read-only), match tiers, audit log.

## Run
```bash
cd AdminPanel
npm install
npm run dev        # http://localhost:3100
# or: npm run build && npm start
npm run typecheck
```

## Status
UI shell with **sample data** (`lib/mock.ts`). All data access goes through `lib/api.ts`; when `PoolGame.Api` exists
(Phase 7+) replace those functions with authenticated `fetch` calls and set `POOLGAME_API_URL` (see `.env.example`).
Authentication/roles, saving tier edits and audited balance adjustments are server features and are **not** implemented yet.

## Rules this panel follows
- Coins are virtual only. No cash-out or real-money features.
- Wallet ledger is append-only and read-only here. Balances never get edited directly; adjustments will be audited server commands.
- Tier values (EntryFee, Reward, MinimumLevel, RatingRange, DailyLimit, Enabled, RequiredUnlock) are data, not code.
