import { PageHead, Pill } from "@/components/ui";
import { getEconomy, getKpis, getMatches, getPlayers } from "@/lib/api";
import { ago, n } from "@/lib/format";

export default async function Overview() {
  const [k, eco, matches, players] = await Promise.all([getKpis(), getEconomy(), getMatches(), getPlayers()]);
  const max = Math.max(...eco.flatMap((d) => [d.minted, d.sunk]));
  const live = matches.filter((m) => m.status === "in_progress");
  const review = matches.filter((m) => m.status === "under_review");
  const flagged = players.filter((p) => p.status === "flagged");
  return (
    <>
      <PageHead title="Overview" sub="Virtual coin economy and match health. Coins have no cash value." />
      <div className="grid kpis">
        <div className="card kpi"><div className="l">Daily active players</div><div className="v">{n(k.dau)}</div><div className="n">last 24 hours</div></div>
        <div className="card kpi"><div className="l">Matches today</div><div className="v">{n(k.matchesToday)}</div><div className="n">{live.length} in progress now</div></div>
        <div className="card kpi"><div className="l">Coins in circulation</div><div className="v" title={n(k.coinsInCirculation)}>{new Intl.NumberFormat("en-US",{notation:"compact",maximumFractionDigits:1}).format(k.coinsInCirculation)}</div><div className="n">sum of wallet balances</div></div>
        <div className="card kpi alert"><div className="l">Flagged players</div><div className="v">{k.flaggedPlayers}</div><div className="n">awaiting review</div></div>
        <div className="card kpi"><div className="l">Double settlements</div><div className="v">{k.settlementOnceViolations}</div><div className="n">must stay at 0</div></div>
      </div>
      <div className="grid two">
        <section className="card">
          <h2>Coins minted vs sunk, 7 days</h2>
          <div className="bars" role="img" aria-label="Daily coins minted versus sunk">
            {eco.map((d) => (
              <div className="day" key={d.day}>
                <div className="pair">
                  <div className="b m" style={{ height: `${(d.minted / max) * 100}%` }} title={`Minted ${n(d.minted)}`} />
                  <div className="b s" style={{ height: `${(d.sunk / max) * 100}%` }} title={`Sunk ${n(d.sunk)}`} />
                </div>
                <span>{d.day}</span>
              </div>
            ))}
          </div>
          <div className="legend"><span><i style={{ background: "var(--brass)" }} />Minted (rewards, bonuses)</span><span><i style={{ background: "#7b8794" }} />Sunk (entry fees, shop)</span></div>
        </section>
        <section className="card">
          <h2>Needs attention</h2>
          <div className="list">
            {review.map((m) => (<div className="row" key={m.id}><div><b>{m.id}</b> {m.players.join(" vs ")}<small>{m.flag}</small></div><Pill value={m.status} /></div>))}
            {flagged.map((p) => (<div className="row" key={p.id}><div><b>{p.name}</b><small>{p.id} · last seen {ago(p.lastSeen)}</small></div><Pill value="flagged" /></div>))}
          </div>
        </section>
      </div>
      <section className="card">
        <h2>Live matches</h2>
        <div className="tablewrap"><table><thead><tr><th>Match</th><th>Tier</th><th>Players</th><th className="num">Pot</th><th>Started</th></tr></thead><tbody>
          {live.map((m) => (<tr key={m.id}><td className="mono">{m.id}</td><td>{m.tier}</td><td>{m.players.join(" vs ")}</td><td className="num">{n(m.reward)}</td><td>{ago(m.at)}</td></tr>))}
        </tbody></table></div>
      </section>
    </>
  );
}
