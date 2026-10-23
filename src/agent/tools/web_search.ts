// WebSearch: DuckDuckGo results (title, URL, snippet), with the passages of the top pages that match the query.
// The excerpts put the evidence in front of the model, which otherwise answers from memory: a small model's memory of
// facts is often wrong, and it doesn't reliably go on to read a page. The pages are the search's own results, so
// reading them sends nothing the query didn't (unlike WebFetch, whose URL the model writes, which asks first).
import type { Session } from "./_types.ts";
import { fill } from "../text.ts";
import { decodeEntities, htmlText, reason, webGet } from "./_web.ts";

export const SCHEMA = {
  name: "WebSearch",
  description: "Search the web. Returns titles, URLs, snippets, and the passages of the top pages that match; " +
               "read more of a page with WebFetch.",
  parameters: { type: "object", properties: {
    query: { type: "string" },
  }, required: ["query"] },
};
export const TIP = "look up any fact you'd otherwise answer from memory: versions, flags, APIs, error messages, " +
  "people, anything outside the project. Use a few keywords.";
export const MESSAGES = {
  error: "Error: the search failed ({error}).",
  none: "No results for {query}.",
  blocked: "Error: the search failed: DuckDuckGo is asking for a human check (too many searches from this network). " +
           "Searching won't work for a while; say you couldn't search, rather than answering from memory.",
  again: "You already searched for {query}; its results are above. Search with different words, or read a result with WebFetch.",
  // After the results: the model reads its latest tool result most closely.
  footer: "\n\nAnswer from these results, copying names and numbers exactly, and give the URL. If they don't answer " +
          "it, say so.",
};

const MAX_RESULTS = 8;
const READ = 3;               // top results whose pages are read for excerpts
const EXCERPTS = 2;           // passages per page
const EXCERPT_CHARS = 400;
const READ_MS = 10_000;       // per page; a slow one is skipped
const RESULT = /class="result__a" href="([^"]+)"[^>]*>(.*?)<\/a>(.*?)(?=class="result__a"|$)/gs;
const STOP = new Set(["the", "and", "for", "with", "what", "which", "who", "how", "does", "did", "was", "were", "when",
                      "from", "that", "this", "into", "version", "added", "flag", "option"]);

/** Each conversation's searches so far, to stop the same search running again and again. */
const searched = new WeakMap<Session, Set<string>>();

class Blocked extends Error {}   // DuckDuckGo's bot check instead of results

/** [[title, url, snippet]] from DuckDuckGo's plain-HTML results page. */
async function duckduckgo(query: string, signal: AbortSignal): Promise<[string, string, string][]> {
  const [, , page] = await webGet("https://html.duckduckgo.com/html/?q=" + encodeURIComponent(query).replaceAll("%20", "+"), signal);
  // After many searches from one address it asks to pick the ducks in some pictures, which looks like no results.
  if (!page.includes("result__a") && /bots use DuckDuckGo too/i.test(page)) throw new Blocked();
  const clean = (s: string) => decodeEntities(s.replace(/<[^>]+>/g, "")).trim();
  const out: [string, string, string][] = [];
  for (const [, link, title, rest] of page.matchAll(RESULT)) {
    let href = link;
    const target = href.match(/uddg=([^&]+)/);   // DuckDuckGo wraps results in a redirect link
    if (target) {
      try { href = decodeURIComponent(target[1]); } catch { href = target[1]; }
    }
    if (href.includes("duckduckgo.com/y.js")) continue;   // ads
    const snip = rest.match(/class="result__snippet"[^>]*>(.*?)<\/a>/s);
    out.push([clean(title), href, snip ? clean(snip[1]) : ""]);
    if (out.length >= MAX_RESULTS) break;
  }
  return out;
}

/** The query's words that say what it's about, lower-case: "git rebase --update-refs" -> git, rebase, update-refs. */
function terms(query: string): string[] {
  const words = query.toLowerCase().match(/[\p{L}\p{N}][\p{L}\p{N}._-]*/gu) ?? [];
  return [...new Set(words.filter(w => w.length > 2 && !STOP.has(w)))];
}

/** The page's lines that match the most terms (at least two, or the only one), cut around the first match. */
function excerpts(text: string, words: string[]): string[] {
  const need = Math.min(2, words.length);
  const scored = text.split("\n").map(line => {
    const low = line.toLowerCase();
    return { line, low, score: words.filter(w => low.includes(w)).length };
  }).filter(s => need > 0 && s.score >= need);
  scored.sort((a, b) => b.score - a.score);
  return scored.slice(0, EXCERPTS).map(({ line, low }) => {
    if (line.length <= EXCERPT_CHARS) return line.trim();
    const at = Math.min(...words.map(w => low.indexOf(w)).filter(i => i >= 0));
    const start = Math.max(0, at - EXCERPT_CHARS / 4);
    return (start > 0 ? "…" : "") + line.slice(start, start + EXCERPT_CHARS).trim() + "…";
  });
}

/** A result page's excerpts, or none if it can't be read in time or isn't HTML. */
async function read(url: string, words: string[], signal: AbortSignal): Promise<string[]> {
  try {
    const [, ctype, body] = await webGet(url, AbortSignal.any([signal, AbortSignal.timeout(READ_MS)]));
    return ctype.includes("html") ? excerpts(htmlText(body)[1], words) : [];
  } catch {
    return [];
  }
}

export async function run(ag: Session, args: { query: string }): Promise<string> {
  const key = args.query.toLowerCase().replace(/\s+/g, " ").trim();
  if (!searched.has(ag)) searched.set(ag, new Set());
  if (searched.get(ag)!.has(key)) return fill(MESSAGES.again, { query: args.query });
  searched.get(ag)!.add(key);
  let results: [string, string, string][];
  try {
    results = await duckduckgo(args.query, ag.signal);
  } catch (e) {
    if (ag.signal.aborted) throw e;
    if (e instanceof Blocked) {
      searched.get(ag)!.delete(key);   // a search that didn't happen can be tried again later
      return MESSAGES.blocked;
    }
    return fill(MESSAGES.error, { error: reason(e) });
  }
  if (!results.length) return fill(MESSAGES.none, { query: args.query });
  const words = terms(args.query);
  const found = await Promise.all(results.slice(0, READ).map(([, url]) => read(url, words, ag.signal)));
  ag.signal.throwIfAborted();
  return results.map(([t, u, s], i) => {
    const quotes = (found[i] ?? []).map(q => `\n   > ${q}`).join("");
    return `${i + 1}. ${t}\n   ${u}\n   ${s}${quotes}`;
  }).join("\n\n") + MESSAGES.footer;
}
