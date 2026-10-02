// Sample data used until PoolGame.Api exists (Phase 7+). Deterministic so screenshots and tests are stable.
import type { AuditEntry, EconomyDay, Kpis, LedgerEntry, MatchRow, MatchTier, Player, TxType } from "./types";

let seed = 20261002;
const rnd = () => { seed = (seed * 1664525 + 1013904223) % 4294967296; return seed / 4294967296; };
const pick = <T,>(a: T[]) => a[Math.floor(rnd() * a.length)];
const NOW = Date.UTC(2026, 9, 2, 9, 30);
const ago = (min: number) => new Date(NOW - min * 60000).toISOString();

const NAMES = ["Arjun","Meera","Rohan","Kavya","Vikram","Ananya","Dev","Ishaan","Sneha","Kabir","Tara","Aarav","Nisha","Rahul","Diya","Yash","Pooja","Karan","Riya","Neel"];

export const players: Player[] = NAMES.map((name, i) => {
  const matches = 20 + Math.floor(rnd() * 400);
  return {
    id: `p_${(1001 + i).toString(36)}`, name, level: 3 + Math.floor(rnd() * 48), rating: 900 + Math.floor(rnd() * 900),
    coins: Math.floor(rnd() * 42000), gems: Math.floor(rnd() * 120),
    status: i === 6 ? "flagged" : i === 13 ? "banned" : i === 3 ? "flagged" : "active",
    lastSeen: ago(Math.floor(rnd() * 3000)), matches, winRate: Math.round((0.38 + rnd() * 0.3) * 100),
  };
});

export const tiers: MatchTier[] = [
  { id: "t1", name: "Rookie Room", entryFee: 100, reward: 190, minimumLevel: 1, ratingMin: 0, ratingMax: 1200, dailyLimit: 50, enabled: true, requiredUnlock: "none" },
  { id: "t2", name: "Club Table", entryFee: 500, reward: 950, minimumLevel: 8, ratingMin: 800, ratingMax: 1500, dailyLimit: 30, enabled: true, requiredUnlock: "none" },
  { id: "t3", name: "High Rollers", entryFee: 2500, reward: 4750, minimumLevel: 20, ratingMin: 1200, ratingMax: 2000, dailyLimit: 15, enabled: true, requiredUnlock: "club_pass" },
  { id: "t4", name: "Grandmaster", entryFee: 10000, reward: 19000, minimumLevel: 35, ratingMin: 1600, ratingMax: 3000, dailyLimit: 5, enabled: false, requiredUnlock: "master_badge" },
];

const TYPES: { t: TxType; src: string; sign: 1 | -1; amt: () => number }[] = [
  { t: "entry_reserve", src: "matchmaking", sign: -1, amt: () => pick([100, 500, 2500]) },
  { t: "match_reward", src: "settlement", sign: 1, amt: () => pick([190, 950, 4750]) },
  { t: "entry_release", src: "matchmaking_cancel", sign: 1, amt: () => pick([100, 500]) },
  { t: "daily_login", src: "login_streak", sign: 1, amt: () => pick([50, 100, 150, 250]) },
  { t: "mission_reward", src: "mission", sign: 1, amt: () => pick([200, 300, 500]) },
  { t: "shop_purchase", src: "shop", sign: -1, amt: () => pick([800, 1500, 3000]) },
];
export const ledger: LedgerEntry[] = (() => {
  const bal = new Map(players.map((p) => [p.id, 5000 + Math.floor(rnd() * 20000)]));
  const out: LedgerEntry[] = [];
  for (let i = 0; i < 60; i++) {
    const p = pick(players), spec = pick(TYPES), amount = spec.amt() * spec.sign;
    const prev = bal.get(p.id)!, next = Math.max(0, prev + amount);
    bal.set(p.id, next);
    out.push({ id: `tx_${(48211 + i).toString(36)}`, playerId: p.id, playerName: p.name, type: spec.t, amount: next - prev, previous: prev, next, source: spec.src, matchId: spec.t.startsWith("match") || spec.t.startsWith("entry") ? `m_${(9100 + Math.floor(rnd() * 400)).toString(36)}` : undefined, at: ago(i * 7 + Math.floor(rnd() * 5)) });
  }
  out.push({ id: "tx_adm01", playerId: players[3].id, playerName: players[3].name, type: "admin_adjustment", amount: -2500, previous: 9800, next: 7300, source: "admin:support", at: ago(95) });
  return out.sort((a, b) => b.at.localeCompare(a.at));
})();

export const matches: MatchRow[] = Array.from({ length: 28 }, (_, i) => {
  const a = pick(players), b = pick(players.filter((p) => p.id !== a.id)), tier = pick(tiers.slice(0, 3));
  const status = i < 3 ? "in_progress" : i === 7 ? "under_review" : i === 12 ? "abandoned" : "settled";
  return { id: `m_${(9500 - i).toString(36)}`, tier: tier.name, players: [a.name, b.name], status, winner: status === "settled" ? (rnd() > .5 ? a.name : b.name) : undefined,
    durationSec: 240 + Math.floor(rnd() * 900), reward: tier.reward, at: ago(i * 11 + 2), flag: status === "under_review" ? "Shot speed above server limit" : undefined };
});

export const audit: AuditEntry[] = [
  { id: "a_19", admin: "support@poolgame", action: "Adjust balance", target: `${players[3].name} (${players[3].id})`, reason: "Refund for stuck match m_2zk", at: ago(95) },
  { id: "a_18", admin: "ops@poolgame", action: "Disable tier", target: "Grandmaster", reason: "Rebalancing reward curve", at: ago(410) },
  { id: "a_17", admin: "trust@poolgame", action: "Ban player", target: `${players[13].name} (${players[13].id})`, reason: "Automated play pattern, 3 confirmed matches", at: ago(900) },
  { id: "a_16", admin: "trust@poolgame", action: "Flag player", target: `${players[6].name} (${players[6].id})`, reason: "Impossible ball state in match m_2x0", at: ago(1320) },
  { id: "a_15", admin: "ops@poolgame", action: "Edit tier", target: "Club Table", reason: "Daily limit 40 to 30", at: ago(2600) },
];

export const economy: EconomyDay[] = ["Sep 26","Sep 27","Sep 28","Sep 29","Sep 30","Oct 1","Oct 2"].map((day, i) => ({ day, minted: 410000 + Math.floor(rnd() * 90000) + i * 8000, sunk: 360000 + Math.floor(rnd() * 90000) + i * 11000 }));

export const kpis: Kpis = { dau: 12840, matchesToday: 31920, coinsInCirculation: players.reduce((s, p) => s + p.coins, 0) * 410, flaggedPlayers: players.filter((p) => p.status === "flagged").length, settlementOnceViolations: 0 };
