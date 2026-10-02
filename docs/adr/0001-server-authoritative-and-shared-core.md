# ADR 0001: Server-authoritative play with shared deterministic physics/rules core
Status: Accepted as default (D1 = yes, adopted so Phases 2-5 could proceed; revisit if you disagree)

Context: Clients can be modified; rewards and results must be trustworthy.
Decision: Server simulates every shot; clients submit intent only. Physics and rules live in an engine-agnostic C# library
used by both Unity and the server to guarantee identical results.
Consequences: Server CPU cost per shot (cheap, short sims); need fixed-timestep deterministic code; client animation replays server result.
