import { PageHead } from "@/components/ui";
import { getAudit } from "@/lib/api";
import { ago } from "@/lib/format";

export default async function Audit() {
  const rows = await getAudit();
  return (
    <>
      <PageHead title="Audit log" sub="Every admin action with who, what and why." />
      <section className="card"><div className="tablewrap"><table>
        <thead><tr><th>When</th><th>Admin</th><th>Action</th><th>Target</th><th>Reason</th></tr></thead>
        <tbody>{rows.map((a) => (<tr key={a.id}><td>{ago(a.at)}</td><td>{a.admin}</td><td><b>{a.action}</b></td><td>{a.target}</td><td style={{ whiteSpace: "normal" }}>{a.reason}</td></tr>))}</tbody>
      </table></div></section>
    </>
  );
}
