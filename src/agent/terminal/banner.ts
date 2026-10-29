// The banner at the top of a session: the title line.
import { BOLD, GOLD, ROSE, RST, VIOLET } from "./theme.ts";

export function banner(root: string): string {
  return `\n${BOLD}${GOLD}Agent 2B${RST} · ${ROSE}Local Agent${RST} · ${VIOLET}${root}${RST}`;
}
