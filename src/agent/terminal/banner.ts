// The banner at the top of a session: the title line.
import { BOLD, GOLD, ROSE, RST, VIOLET } from "./theme.ts";

export function banner(root: string): string {
  return `\n${BOLD}${GOLD}ZimAI${RST} · ${ROSE}Local AI${RST} · ${VIOLET}${root}${RST}`;
}
