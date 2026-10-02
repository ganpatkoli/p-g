export type PlayerStatus = "active" | "flagged" | "banned";
export interface Player { id: string; name: string; level: number; rating: number; coins: number; gems: number; status: PlayerStatus; lastSeen: string; matches: number; winRate: number; }

export type TxType = "entry_reserve" | "entry_release" | "match_reward" | "mission_reward" | "shop_purchase" | "daily_login" | "admin_adjustment";
export interface LedgerEntry { id: string; playerId: string; playerName: string; type: TxType; amount: number; previous: number; next: number; source: string; matchId?: string; at: string; }

export interface MatchTier { id: string; name: string; entryFee: number; reward: number; minimumLevel: number; ratingMin: number; ratingMax: number; dailyLimit: number; enabled: boolean; requiredUnlock: string; }

export type MatchStatus = "in_progress" | "settled" | "abandoned" | "under_review";
export interface MatchRow { id: string; tier: string; players: [string, string]; status: MatchStatus; winner?: string; durationSec: number; reward: number; at: string; flag?: string; }

export interface AuditEntry { id: string; admin: string; action: string; target: string; reason: string; at: string; }

export interface Kpis { dau: number; matchesToday: number; coinsInCirculation: number; flaggedPlayers: number; settlementOnceViolations: number; }
export interface EconomyDay { day: string; minted: number; sunk: number; }
