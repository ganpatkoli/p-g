import PlayersTable from "@/components/PlayersTable";
import { PageHead } from "@/components/ui";
import { getPlayers } from "@/lib/api";

export default async function Players() {
  return (<><PageHead title="Players" sub="Search, review flags and check balances." /><PlayersTable players={await getPlayers()} /></>);
}
