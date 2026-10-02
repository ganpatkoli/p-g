import "./globals.css";
import type { Metadata } from "next";
import Shell from "@/components/Shell";
import { getPlayers } from "@/lib/api";

export const metadata: Metadata = { title: "Coin Pool Admin" };

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const flagged = (await getPlayers()).filter((p) => p.status === "flagged").length;
  return (
    <html lang="en"><body><Shell flagged={flagged}>{children}</Shell></body></html>
  );
}
