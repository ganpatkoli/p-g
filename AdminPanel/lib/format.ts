export const n = (v: number) => v.toLocaleString("en-US");
export const signed = (v: number) => (v > 0 ? "+" : v < 0 ? "−" : "") + n(Math.abs(v));
export function ago(iso: string): string {
  const mins = Math.round((Date.UTC(2026, 9, 2, 9, 30) - new Date(iso).getTime()) / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins} min ago`;
  if (mins < 1440) return `${Math.round(mins / 60)} h ago`;
  return `${Math.round(mins / 1440)} d ago`;
}
export const dur = (s: number) => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, "0")}`;
