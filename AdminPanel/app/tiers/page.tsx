import TierEditor from "@/components/TierEditor";
import { PageHead } from "@/components/ui";
import { getTiers } from "@/lib/api";

export default async function Tiers() {
  return (
    <>
      <PageHead title="Match tiers" sub="Entry fee, reward and eligibility per tier. Values live in the database, never in client code." />
      <TierEditor initial={await getTiers()} />
    </>
  );
}
