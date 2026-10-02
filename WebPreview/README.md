# Web preview: Coin Pool 8 Ball

`coin-pool.html` is a single-file, mobile-first (portrait and landscape, touch aim, vibration) playable version of the game (not the Unity build). It ports the shared physics core to JavaScript and adds:
guest/account sign-in, virtual coin wallet with an append-only ledger, data-driven match tiers, five bot levels
(candidate generation, geometry check, physics simulation, evaluation, selection), XP/level, Elo rating, daily streak, history and leaderboard.

Open the file in any browser to play as a guest (progress in localStorage). Account sign-in and the shared leaderboard only work when the page is
published as a claude.ai Artifact (platform identity + its `db`). This is NOT the production auth system: the real game needs the ASP.NET
backend (register/login with email and password, JWT, server-side wallet and settlement; roadmap phases 7-12).
