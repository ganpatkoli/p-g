import { PageHead, Pill } from "@/components/ui";
import { getMatches } from "@/lib/api";
import { ago, dur, n } from "@/lib/format";

export default async function Matches() {
  const rows = await getMatches();
  return (
    <>
      <PageHead title="Matches" sub="Every match has a unique ID and a traceable action log. Settlement runs once per match." />
      <section className="card"><div className="tablewrap"><table>
        <thead><tr><th>Match</th><th>Status</th><th>Tier</th><th>Players</th><th>Winner</th><th className="num">Reward</th><th className="num">Length</th><th>When</th><th>Note</th></tr></thead>
        <tbody>{rows.map((m) => (
          <tr key={m.id}><td className="mono">{m.id}</td><td><Pill value={m.status} /></td><td>{m.tier}</td><td>{m.players.join(" vs ")}</td><td>{m.winner ?? "-"}</td>
            <td className="num">{m.status === "settled" ? n(m.reward) : "-"}</td><td className="num">{dur(m.durationSec)}</td><td>{ago(m.at)}</td><td>{m.flag ?? ""}</td></tr>))}
        </tbody></table></div></section>
    </>
  );
}
