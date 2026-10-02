# Architecture

## 1. Principles
Clean Architecture, SOLID, dependency inversion, server-authoritative gameplay, data-driven tuning,
event-driven communication, no giant `GameManager`. Dependencies point inward only.

Hard rules:
- Client never decides balance, rewards, purchases, match results or inventory ownership.
- No economy logic in UI. No networking in physics classes.
- Tuning values (physics, tiers, rewards) live in config (ScriptableObjects on client, DB/config on server), never hardcoded.
- Virtual currency only; no cash-out path exists anywhere.

## 2. System overview
```
 Unity Client ──HTTPS REST──▶ PoolGame.Api ──▶ PoolGame.Application ──▶ PoolGame.Domain
      │                            │                    │                      ▲
      └──WebSocket (match)─────────┘                    ▼                      │
                                              PoolGame.Infrastructure ─────────┘
                                              (PostgreSQL, Redis)
 Next.js Admin ──HTTPS──▶ PoolGame.Api (admin policy)
```
Match flow: Menu → Tier → Validate wallet → Reserve entry → Matchmaking → Match created →
Play → Server validation → Settlement (once) → Reward → XP → Rating → History.

## 3. Client (Unity 6, URP, Input System, Addressables)
Assemblies (one asmdef per folder under `Assets/_Game/Scripts`), dependency direction:
```
Core  ◀── Physics, Rules, Input, Camera, Audio, Analytics   (pure/engine-light)
Core  ◀── Gameplay (composes Physics+Rules+Input+Camera)
Core  ◀── Networking, Economy*, Progression*, Inventory* (*thin API clients/view-models only)
Core  ◀── AI (uses Physics sim + Rules)
UI    ──▶ view-models/interfaces only; never calls Economy rules
```
- **Physics** and **Rules** have no UnityEngine dependency where possible → EditMode testable and
  shareable with the server (see §6).
- Config via ScriptableObjects: `PhysicsConfig`, `TableConfig`, `BallConfig`, `CueConfig`, `AIDifficultyConfig`.
- Events via a small typed event bus in `Core`; no singletons holding game state.
- Economy/Progression/Inventory on the client are display caches populated from server responses.

## 4. Server (ASP.NET Core)
| Project | Responsibility | References |
|---|---|---|
| `PoolGame.Domain` | Entities, value objects, economy/match rules, pool rules + physics sim | none |
| `PoolGame.Application` | Use cases, commands/queries, ports (repository/clock/lock interfaces) | Domain |
| `PoolGame.Infrastructure` | PostgreSQL (EF Core/Npgsql), Redis, adapters | Application, Domain |
| `PoolGame.Api` | Endpoints, auth middleware, validation, contracts, WebSocket match hub | Application, Infrastructure (composition root only) |
| `PoolGame.Tests` | Unit + integration (Testcontainers for Postgres/Redis) | all |

## 5. Economy design
- **Append-only `WalletTransactions` ledger**: TransactionId, PlayerId, Currency, Amount, Type,
  PreviousBalance, NewBalance, Timestamp, Source, MatchId?, IdempotencyKey (unique).
- Balance on `Wallets` is a derived, row-locked projection updated in the **same DB transaction** as the ledger insert;
  CHECK (balance >= 0). Reconciliation job verifies sum(ledger) = balance.
- Idempotency: unique `(PlayerId, IdempotencyKey)`; retries return the original result.
- Race safety: single DB transaction + `SELECT … FOR UPDATE` (or optimistic row version); Redis locks only as an optimization.
- Entry fee flow: `Reserve` (hold) → on settlement `Capture`/`Release` + reward credit. Settlement guarded by
  unique `MatchSettlements.MatchId` + state machine (`Created→InProgress→Settling→Settled`) so it runs exactly once.
- `MatchTier` config: EntryFee, Reward, MinimumLevel, RatingRange, DailyLimit, Enabled, RequiredUnlock (stored in DB, admin-editable, no hardcoded values).
- Currencies: `Coin` (soft) and `Gem` (premium, virtual). Neither is cash-convertible.

## 6. Authoritative multiplayer
- Client sends **intent only**: `SubmitShot{matchId, seq, aimAngle, power, spin(x,y), cueElevation, clientTs}`.
- Server validates: turn ownership, sequence number (replay protection), ranges, rate, match state.
- Server runs the **deterministic physics sim** (C# port of the shared pool-physics core; fixed timestep, no floats from
  platform-dependent APIs where avoidable) → resulting ball state, pocketed balls, fouls via Rules Engine → broadcasts
  `ShotResult{finalBallStates, events}`. Client replays animation from server result; client-side prediction is cosmetic only.
- Every action persisted to `MatchActions` (matchId, seq, playerId, payload, hash chain) → traceable/replayable.
- Reconnect: server holds match state in memory + Redis snapshot; client re-requests `MatchSnapshot`. Disconnect/AFK timers
  → forfeit rules via Domain match rules.
- Shared code strategy (open decision D1): physics/rules core as a plain C# netstandard2.1 library consumed by both Unity and server.

## 7. Data stores
- **PostgreSQL** (source of truth): Users, PlayerProfiles, Wallets, WalletTransactions, Matches, MatchPlayers, MatchActions,
  MatchSettlements, Inventories, InventoryItems, Cues, Missions, Achievements, Tournaments, TournamentPlayers, LeaderboardEntries.
  Indexes: ledger `(PlayerId, Timestamp)`, unique idempotency, `MatchActions (MatchId, Seq)`, leaderboard `(Season, Score desc)`.
- **Redis**: matchmaking queues (sorted sets by rating/tier), sessions, rate limiting, presence, short-lived cache, distributed locks. Never wallet source of truth.

## 8. Security
JWT access + rotating refresh tokens; per-user/IP rate limiting (Redis); idempotency keys on all money-moving endpoints;
server-side validation of every input; impossible-state / speed checks on shots; audit log for admin and economy actions;
secrets via environment / secret manager (never in git; `.env.example` only); TLS everywhere; admin behind separate role policy.

## 9. Testing strategy
Unity Test Framework (EditMode: physics, rules, fouls, AI evaluation; PlayMode: flow). Server: xUnit unit tests for Domain/Application;
integration tests (real Postgres/Redis via containers) for wallet, ledger idempotency, concurrency, settlement-once, matchmaking,
reconnection, tournament, security. Critical economy paths require integration tests.

## 10. Environments
Docker Compose (dev): postgres, redis, api, (admin). Production: containers, externally managed secrets.

## 11. Open decisions (need your input)
- **D1** Share physics/rules between Unity and server as one netstandard2.1 library? (recommended: yes, for exact determinism)
- **D2** Realtime transport: raw WebSocket vs SignalR vs UDP (e.g. LiteNetLib)? (recommended: SignalR/WebSocket – turn-based shots tolerate latency)
- **D3** Auth providers: guest + email, Google/Apple sign-in? (Apple needed on iOS if other social logins exist)
- **D4** Premium currency: only virtual/earnable, or also IAP-purchased? IAP needs store-receipt validation (later phase) and compliance review.
- **D5** Legal: coin-staked matches may be treated as gambling in some regions even with virtual coins; recommend legal review before launch.
