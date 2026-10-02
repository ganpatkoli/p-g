// Single data-access seam. Replace the bodies with fetch() calls to PoolGame.Api when it exists.
// The panel never writes balances directly: coin changes go through audited server commands.
import * as mock from "./mock";
import type { AuditEntry, EconomyDay, Kpis, LedgerEntry, MatchRow, MatchTier, Player } from "./types";

export const usingSampleData = !process.env.POOLGAME_API_URL;

export async function getKpis(): Promise<Kpis> { return mock.kpis; }
export async function getEconomy(): Promise<EconomyDay[]> { return mock.economy; }
export async function getPlayers(): Promise<Player[]> { return mock.players; }
export async function getLedger(): Promise<LedgerEntry[]> { return mock.ledger; }
export async function getTiers(): Promise<MatchTier[]> { return mock.tiers; }
export async function getMatches(): Promise<MatchRow[]> { return mock.matches; }
export async function getAudit(): Promise<AuditEntry[]> { return mock.audit; }
