"use client";
import Link from "next/link";
import { usePathname } from "next/navigation";

const NAV = [
  { href: "/", label: "Overview" }, { href: "/players", label: "Players" }, { href: "/matches", label: "Matches" },
  { href: "/ledger", label: "Wallet ledger" }, { href: "/tiers", label: "Match tiers" }, { href: "/audit", label: "Audit log" },
];

export default function Shell({ children, flagged }: { children: React.ReactNode; flagged: number }) {
  const path = usePathname();
  return (
    <div className="app">
      <aside className="side">
        <div className="brand"><i /><div>Coin Pool<small>ADMIN</small></div></div>
        <nav className="nav" aria-label="Main">
          {NAV.map((i) => {
            const active = i.href === "/" ? path === "/" : path.startsWith(i.href);
            return (
              <Link key={i.href} href={i.href} aria-current={active ? "page" : undefined}>
                {i.label}{i.href === "/players" && flagged > 0 ? <span className="badge">{flagged}</span> : null}
              </Link>
            );
          })}
        </nav>
        <div className="who">Signed in as<br /><b>ops@poolgame</b> · role: Operations</div>
      </aside>
      <main className="main">{children}</main>
    </div>
  );
}
