// The proxy for everything the agent fetches: 2B.exe puts the Windows proxy (e.g. Clash) in HTTPS_PROXY /
// HTTP_PROXY, which the commands the agent runs read themselves, but Node only with a flag, so fetch is switched
// over to it here. The model server stays direct (NO_PROXY).
import * as http from "node:http";

export function useProxy(): void {
  if (!process.env.HTTPS_PROXY && !process.env.https_proxy) return;
  process.env.NO_PROXY ??= "127.0.0.1,localhost";
  http.setGlobalProxyFromEnv();
}
