import { PageHead } from "@/components/ui";
import { getLedger } from "@/lib/api";
import { ago, n, signed } from "@/lib/format";

export default async function Ledger() {
  const rows = await getLedger();
  return (
    <>
      <PageHead title="Wallet ledger" sub="Append-only. Each row stores the previous and new balance. Nothing here can be edited or deleted." />
      <section className="card"><div className="tablewrap"><table>
        <thead><tr><th>Transaction</th><th>Player</th><th>Type</th><th className="num">Amount</th><th className="num">Previous</th><th className="num">New</th><th>Source</th><th>Match</th><th>When</th></tr></thead>
        <tbody>{rows.map((t) => (
          <tr key={t.id}><td className="mono">{t.id}</td><td>{t.playerName}</td><td>{t.type.replace(/_/g, " ")}</td>
            <td className={`num ${t.amount >= 0 ? "pos" : "neg"}`}>{signed(t.amount)}</td><td className="num">{n(t.previous)}</td><td className="num">{n(t.next)}</td>
            <td>{t.source}</td><td className="mono">{t.matchId ?? ""}</td><td>{ago(t.at)}</td></tr>))}
        </tbody></table></div></section>
    </>
  );
}
