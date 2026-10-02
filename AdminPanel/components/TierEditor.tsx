"use client";
import { useState } from "react";
import type { MatchTier } from "@/lib/types";
import { n } from "@/lib/format";

export default function TierEditor({ initial }: { initial: MatchTier[] }) {
  const [tiers, setTiers] = useState(initial);
  const [dirty, setDirty] = useState<Set<string>>(new Set());
  const [saved, setSaved] = useState("");

  const edit = (id: string, patch: Partial<MatchTier>) => {
    setTiers((ts) => ts.map((t) => (t.id === id ? { ...t, ...patch } : t)));
    setDirty((d) => new Set(d).add(id));
    setSaved("");
  };
  const num = (v: string) => Math.max(0, Number(v) || 0);
  const invalid = (t: MatchTier) => t.reward <= t.entryFee || t.ratingMin > t.ratingMax;
  const anyInvalid = tiers.some(invalid);

  return (
    <>
      <div className="grid" style={{ gridTemplateColumns: "repeat(auto-fit,minmax(340px,1fr))" }}>
        {tiers.map((t) => (
          <section className="card tier" key={t.id} aria-label={t.name}>
            <div className="top">
              <div><b style={{ fontSize: 16 }}>{t.name}</b><div className="note">Pot after fee: {n(t.reward - t.entryFee)} coins {dirty.has(t.id) ? "· unsaved" : ""}</div></div>
              <label className="switch"><input type="checkbox" checked={t.enabled} onChange={(e) => edit(t.id, { enabled: e.target.checked })} />{t.enabled ? "Enabled" : "Disabled"}</label>
            </div>
            <div className="form">
              <label>Entry fee<input type="number" min={0} value={t.entryFee} onChange={(e) => edit(t.id, { entryFee: num(e.target.value) })} /></label>
              <label>Reward<input type="number" min={0} value={t.reward} onChange={(e) => edit(t.id, { reward: num(e.target.value) })} /></label>
              <label>Minimum level<input type="number" min={1} value={t.minimumLevel} onChange={(e) => edit(t.id, { minimumLevel: num(e.target.value) })} /></label>
              <label>Daily limit<input type="number" min={0} value={t.dailyLimit} onChange={(e) => edit(t.id, { dailyLimit: num(e.target.value) })} /></label>
              <label>Rating from<input type="number" min={0} value={t.ratingMin} onChange={(e) => edit(t.id, { ratingMin: num(e.target.value) })} /></label>
              <label>Rating to<input type="number" min={0} value={t.ratingMax} onChange={(e) => edit(t.id, { ratingMax: num(e.target.value) })} /></label>
              <label>Required unlock
                <select value={t.requiredUnlock} onChange={(e) => edit(t.id, { requiredUnlock: e.target.value })}>
                  <option value="none">None</option><option value="club_pass">Club pass</option><option value="master_badge">Master badge</option>
                </select></label>
            </div>
            {invalid(t) ? <div className="pill bad" style={{ alignSelf: "flex-start" }}>Reward must exceed the entry fee and rating range must be valid</div> : null}
          </section>
        ))}
      </div>
      <div className="tools">
        <button className="btn primary" disabled={dirty.size === 0 || anyInvalid} onClick={() => { setSaved(`${dirty.size} tier${dirty.size > 1 ? "s" : ""} validated. Saving needs the server API (Phase 7+); this preview does not persist.`); setDirty(new Set()); }}>Save changes</button>
        <button className="btn" disabled={dirty.size === 0} onClick={() => { setTiers(initial); setDirty(new Set()); setSaved(""); }}>Discard</button>
        {saved ? <span className="note" role="status">{saved}</span> : null}
      </div>
    </>
  );
}
