// WebFetch: the readable text of a web page, in parts, after asking.
import type { Session } from "./_types.ts";
import { fill } from "../text.ts";
import { int } from "./_args.ts";
import { htmlText, reason, webGet } from "./_web.ts";

const PAGE_CHARS = 10000;   // characters of a page returned per call

export const SCHEMA = {
  name: "WebFetch",
  description: `Fetch a web page and return its readable text, in parts of ${PAGE_CHARS} characters.`,
  parameters: { type: "object", properties: {
    url: { type: "string" },
    offset: { type: "integer", description: "character to start from, for the next part (default 0)" },
  }, required: ["url"] },
};
export const TIP = "read the most relevant search result, or a URL the user gives.";
export const MESSAGES = {
  error: "Error: couldn't fetch {url} ({error}).",
  not_text: "Error: {url} is {type}, not a text page.",
  more: "\n\n... {rest} more characters (use offset={next})",
  end: "(End of page: it has {length} characters in total.)",
};

export async function run(ag: Session, args: { url: string; offset?: number }): Promise<string> {
  let url = args.url;
  if (!/^https?:\/\//.test(url)) url = "https://" + url;
  const denied = await ag.confirm(`fetch ${url}`);
  if (denied) return denied;
  let final: string, ctype: string, body: string;
  try {
    [final, ctype, body] = await webGet(url, ag.signal);
  } catch (e) {
    if (ag.signal.aborted) throw e;
    return fill(MESSAGES.error, { url, error: reason(e) });
  }
  let head: string, text: string;
  if (ctype.includes("html")) {
    const [title, page] = htmlText(body);
    [head, text] = [`Title: ${title}
URL: ${final}

`, page];
  } else if (ctype.startsWith("text/") || ["json", "xml", "javascript"].some(t => ctype.includes(t))) {
    [head, text] = [`URL: ${final}\n\n`, body];
  } else {
    return fill(MESSAGES.not_text, { url, type: ctype });
  }
  const offset = Math.max(0, int(args.offset, 0));
  if (offset >= text.length) return head + fill(MESSAGES.end, { length: text.length });
  const part = text.slice(offset, offset + PAGE_CHARS), rest = text.length - offset - PAGE_CHARS;
  return head + part + (rest > 0 ? fill(MESSAGES.more, { rest, next: offset + PAGE_CHARS }) : "");
}
