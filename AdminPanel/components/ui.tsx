import { usingSampleData } from "@/lib/api";

export function PageHead({ title, sub, children }: { title: string; sub?: string; children?: React.ReactNode }) {
  return (
    <div className="head">
      <div><h1>{title}</h1>{sub ? <p className="sub">{sub}</p> : null}</div>
      <div className="tools">{usingSampleData ? <span className="sample">Sample data</span> : null}{children}</div>
    </div>
  );
}

const TONE: Record<string, string> = {
  active: "ok", flagged: "warn", banned: "bad", settled: "ok", in_progress: "info", abandoned: "muted", under_review: "warn",
};
export function Pill({ value }: { value: string }) {
  return <span className={`pill ${TONE[value] ?? "muted"}`}>{value.replace("_", " ")}</span>;
}
