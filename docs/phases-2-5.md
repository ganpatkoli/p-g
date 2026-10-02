# Phases 2-5: client foundation, physics, cue/aim/spin, rules

## What exists
**Shared core (`Shared/PoolGame.Core`)** - pure C#, deterministic, 60 xUnit tests.
- `Physics/` `PhysicsConfig`, `TableConfig` (all tuning, serialisable, validated), `PhysicsWorld` (fixed 1/480 s step),
  `ShotSimulator`, `CueStrike`, `AimPredictor`, `BallPlacement`, `Rack`, `TrajectoryRecorder/Player`, `ShotValidator`.
- `Rules/` `IRuleSet`, `EightBallRules`, `NineBallRules`, `ShotSummary`, `RulesState`, `ShotOutcome`. No UI or engine dependency.

**Unity client (`Client/`)** - Unity 6 project skeleton. Open `Client/` in Unity Hub, run `PoolGame > Setup URP`, set
*Player > Active Input Handling* to *Input System Package (New)* or *Both*, press Play in any empty scene.
`GameBootstrap` (composition root) builds a local practice table. Controls: pointer aims, wheel / `+` `-` power,
arrows move the cue-tip spin point (`R` resets), click or Space shoots, `C` toggles camera.

## Physics model
- SI units. Ball has linear velocity and 3-axis angular velocity.
- Cloth: sliding friction until contact-point slip is zero (then rolling), rolling resistance, side-spin decay.
- Cue strike: impulse at height b gives w = 2.5 v b / R^2 (b = 0.4R rolls immediately; b = 0 is a stun shot).
  Draw/follow/stop/stun emerge from sliding friction, they are not special cases. Squirt is configurable.
- Ball-ball: restitution + Coulomb friction (spin transfer / throw). Cushion: restitution + Coulomb friction (spin changes rebound).
- Pockets: open mouths between cushion segments, jaw posts (rim collisions), capture radius.
- Verified by analytic tests (rolling and slide-then-roll distances, momentum transfer, cushion restitution), plus behaviour tests
  (draw/follow/stun, English, pocketing, determinism, break validity).

## Rules implemented
8-Ball: open table, group assignment, legal first contact, no-rail foul, scratch, ball-in-hand, break legality (4 rails or pot),
8 early/scratch loss, legal 8 win, 8 re-spot on break. 9-Ball: lowest ball first, 9 wins on any legal shot, 9 re-spot on foul.
Not implemented (decisions needed): re-rack options, push-out, three-foul rule, called shots.

## Known limits / not verified
- Unity code was compile-checked only against a UnityEngine stub; it has **not** been run in the Unity Editor.
  Expect small API/visual fixes on first open (rolling-rotation sign, camera feel, materials).
- Unity generates `.meta` files on first open; commit them.
- Local resolver runs physics on the client for practice. In multiplayer (Phase 10) `IShotResolver` is replaced by a server-backed one.
- Physics tuning values are plausible defaults, not calibrated against a real table.
