# Shared

`PoolGame.Core` is the engine-agnostic C# library (physics + rules) used by **both** the Unity client and the server,
so a shot gives identical results on both sides (ADR 0001).

| Folder | What |
|---|---|
| `PoolGame.Core/` | Source + `package.json` + `asmdef`. Unity references it as a local package (`Client/Packages/manifest.json`). |
| `PoolGame.Core.Lib/` | `netstandard2.1` csproj compiling the same sources. The server will reference this. |
| `PoolGame.Core.Tests/` | xUnit tests: `dotnet test Shared/PoolGame.Core.Tests` |

Constraints: C# 9 only, no UnityEngine, no I/O, no `System.Random`/clock (determinism), doubles only.
