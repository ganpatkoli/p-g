# Advanced Coin Pool Game

3D multiplayer Pool/Billiards game (Unity 6 client, ASP.NET Core authoritative server).

**Virtual coins only.** Coins have no cash value and can never be converted to real money.
No cash-out, no real-money wagering.

## Repository layout
| Path | Purpose |
|---|---|
| `Client/` | Unity 6 (URP) project — see `docs/architecture.md` §3 |
| `Server/` | ASP.NET Core solution (Api / Application / Domain / Infrastructure / Tests) |
| `AdminPanel/` | Next.js + TypeScript admin (Phase 18) |
| `infra/` | Docker Compose, env templates (Phase 7) |
| `docs/` | Architecture, ADRs, roadmap |

## Status
Phase 1 (Architecture) — complete. See `docs/roadmap.md`.
