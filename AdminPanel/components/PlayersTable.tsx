"use client";
import { useMemo, useState } from "react";
import type { Player } from "@/lib/types";
import { ago, n } from "@/lib/format";

const TONE = { active: "ok", flagged: "warn", banned: "bad" } as const;

export default function PlayersTable({ players }: { players: Player[] }) {
  const [q, setQ] = useState("");
  const [status, setStatus] = useState("all");
  const rows = useMemo(
    () => players.filter((p) => (status === "all" || p.status === status) && (p.name + p.id).toLowerCase().includes(q.toLowerCase())),
    [players, q, status]
  );
  return (
    <section className="card">
      <div className="tools" style={{ marginBottom: 12 }}>
        <input type="text" placeholder="Search name or ID" aria-label="Search players" value={q} onChange={(e) => setQ(e.target.value)} />
        <select aria-label="Filter by status" value={status} onChange={(e) => setStatus(e.target.value)}>
          <option value="all">All statuses</option><option value="active">Active</option><option value="flagged">Flagged</option><option value="banned">Banned</option>
        </select>
        <span className="note" style={{ alignSelf: "center" }}>{rows.length} of {players.length}</span>
      </div>
      <div className="tablewrap"><table>
        <thead><tr><th>Player</th><th>Status</th><th className="num">Level</th><th className="num">Rating</th><th className="num">Coins</th><th className="num">Gems</th><th className="num">Matches</th><th className="num">Win %</th><th>Last seen</th></tr></thead>
        <tbody>{rows.map((p) => (
          <tr key={p.id}>
            <td><b>{p.name}</b> <span className="mono" style={{ fontFamily: "monospace", color: "var(--muted)", fontSize: 12 }}>{p.id}</span></td>
            <td><span className={`pill ${TONE[p.status]}`}>{p.status}</span></td>
            <td className="num">{p.level}</td><td className="num">{n(p.rating)}</td><td className="num">{n(p.coins)}</td><td className="num">{p.gems}</td>
            <td className="num">{n(p.matches)}</td><td className="num">{p.winRate}</td><td>{ago(p.lastSeen)}</td>
          </tr>))}
        </tbody>
      </table></div>
      <p className="note">Balances are read-only here. Changes go through an audited adjustment on the server, which writes a ledger entry.</p>
    </section>
  );
}
