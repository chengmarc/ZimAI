// Getting web pages and their readable text, for WebSearch and WebFetch.
// fetch uses the proxy in HTTPS_PROXY, or the Windows proxy (e.g. Clash): see proxy.ts.

const UA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126 Safari/537.36";

/** [final url, content type, decoded text] of a web page (at most 5 MB of it). */
export async function webGet(url: string, signal: AbortSignal): Promise<[string, string, string]> {
  const r = await fetch(url, {
    headers: { "User-Agent": UA, "Accept-Language": "en,*;q=0.5" },
    signal: AbortSignal.any([signal, AbortSignal.timeout(30_000)]),
  });
  if (!r.ok) throw new Error(`HTTP ${r.status} ${r.statusText}`);
  const [type, ...params] = (r.headers.get("content-type") || "text/plain").split(";");
  const charset = params.map(p => p.trim().match(/^charset="?([^"]+)"?$/i)?.[1]).find(Boolean) ?? "utf-8";
  const chunks: Uint8Array[] = [];
  let size = 0;
  for await (const chunk of r.body ?? []) {
    chunks.push(chunk);
    if ((size += chunk.length) >= 5_000_000) break;
  }
  const bytes = Buffer.concat(chunks).subarray(0, 5_000_000);
  let text: string;
  try {
    text = new TextDecoder(charset).decode(bytes);
  } catch {   // a charset Node doesn't know
    text = new TextDecoder("utf-8").decode(bytes);
  }
  return [r.url, type.trim().toLowerCase(), text];
}

/** The short reason a web request failed, e.g. ENOTFOUND. */
export function reason(e: unknown): string {
  const err = e as { cause?: { code?: string; message?: string }; message?: string };
  return err.cause?.code || err.cause?.message || err.message || String(e);
}

const ENTITIES: Record<string, string> = {
  amp: "&", lt: "<", gt: ">", quot: '"', apos: "'", copy: "©", reg: "®", trade: "™",
  hellip: "…", mdash: "—", ndash: "–", lsquo: "‘", rsquo: "’", ldquo: "“", rdquo: "”", laquo: "«", raquo: "»",
  middot: "·", bull: "•", times: "×", divide: "÷", deg: "°", plusmn: "±", micro: "µ", para: "¶", sect: "§",
  euro: "€", pound: "£", yen: "¥", cent: "¢", larr: "←", rarr: "→", uarr: "↑", darr: "↓", harr: "↔",
  // invisible ones, by code
  nbsp: String.fromCharCode(0xa0), shy: String.fromCharCode(0xad), ensp: String.fromCharCode(0x2002),
  emsp: String.fromCharCode(0x2003), thinsp: String.fromCharCode(0x2009), zwnj: String.fromCharCode(0x200c),
  zwj: String.fromCharCode(0x200d),
};

/** Text with HTML character references (&amp; &#39; &#x2014; ...) decoded. */
export function decodeEntities(s: string): string {
  return s.replace(/&(#[xX][0-9a-fA-F]+|#\d+|[a-zA-Z][a-zA-Z0-9]*);?/g, (m, e: string) => {
    if (e[0] !== "#") return ENTITIES[e] ?? m;
    const n = e[1] === "x" || e[1] === "X" ? parseInt(e.slice(2), 16) : parseInt(e.slice(1), 10);
    return n > 0 && n <= 0x10ffff ? String.fromCodePoint(n) : String.fromCharCode(0xfffd);   // the replacement character
  });
}

interface HtmlHandler {
  start(tag: string, attrs: Record<string, string | null>): void;
  end(tag: string): void;
  data(text: string): void;
}

const TAG = /<(\/?)([a-zA-Z][^\s/>]*)((?:[^>"']|"[^"]*"|'[^']*')*)>/y;
const ATTR = /([^\s"'>/=]+)(?:\s*=\s*(?:"([^"]*)"|'([^']*)'|([^\s>]+)))?/g;

/** Feed an HTML page to a handler, like Python's html.parser: lower-case tag names, character references
 *  decoded, script and style contents passed on as raw text, and <x/> reported as a start and an end. */
function parseHtml(src: string, h: HtmlHandler) {
  let i = 0;
  while (i < src.length) {
    const lt = src.indexOf("<", i);
    if (lt < 0) {
      h.data(decodeEntities(src.slice(i)));
      break;
    }
    if (lt > i) h.data(decodeEntities(src.slice(i, lt)));
    if (src.startsWith("<!--", lt)) {
      const e = src.indexOf("-->", lt + 4);
      i = e < 0 ? src.length : e + 3;
      continue;
    }
    if (src[lt + 1] === "!" || src[lt + 1] === "?") {   // doctype, CDATA, processing instruction
      const e = src.indexOf(">", lt);
      i = e < 0 ? src.length : e + 1;
      continue;
    }
    TAG.lastIndex = lt;
    const m = TAG.exec(src);
    if (!m) {   // a lone "<"
      h.data("<");
      i = lt + 1;
      continue;
    }
    i = TAG.lastIndex;
    const [, close, name, rest] = m, tag = name.toLowerCase();
    if (close) {
      h.end(tag);
      continue;
    }
    const attrs: Record<string, string | null> = {};
    for (const a of rest.matchAll(ATTR)) {
      const value = a[2] ?? a[3] ?? a[4];
      attrs[a[1].toLowerCase()] = value === undefined ? null : decodeEntities(value);
    }
    h.start(tag, attrs);
    if (/\/\s*$/.test(rest)) h.end(tag);
    else if (tag === "script" || tag === "style") {
      const closing = new RegExp(`</${tag}`, "ig");
      closing.lastIndex = i;
      const e = closing.exec(src)?.index ?? src.length;
      h.data(src.slice(i, e));
      i = e;
    }
  }
}

const SKIP = new Set(["script", "style", "noscript", "svg", "head", "nav", "footer", "form", "button", "iframe", "template"]);
const SKIP_ROLES = new Set(["navigation", "banner", "contentinfo", "search", "menu", "menubar"]);
const BLOCK = new Set(["p", "div", "br", "li", "tr", "h1", "h2", "h3", "h4", "h5", "h6", "pre", "section", "article",
                       "table", "ul", "ol", "blockquote", "dt", "dd", "hr", "main", "header"]);
const VOID = new Set(["br", "hr", "img", "input", "meta", "link", "area", "base", "col", "embed", "source", "track", "wbr"]);

/** Readable text of an HTML page, roughly as a browser shows it, minus scripts, styles and page chrome. */
class PageText implements HtmlHandler {
  parts: string[] = [];
  skip = 0;
  pre = 0;
  title = "";
  inTitle = false;
  roleTag: string | null = null;   // an element skipped for its role, e.g. <div role="navigation">
  roleDepth = 0;

  start(tag: string, a: Record<string, string | null>) {
    if (this.roleTag) {
      if (tag === this.roleTag) this.roleDepth++;
      return;
    }
    if (!VOID.has(tag) && (SKIP_ROLES.has(a.role ?? "") || a["aria-hidden"] === "true")) {
      this.roleTag = tag;
      this.roleDepth = 1;
      return;
    }
    if (tag === "title") this.inTitle = true;
    if (SKIP.has(tag)) this.skip++;
    else if (BLOCK.has(tag)) this.parts.push("\n");
    if (tag === "pre") this.pre++;
    if (!this.skip && (tag === "h1" || tag === "h2" || tag === "h3")) this.parts.push("#".repeat(Number(tag[1])) + " ");
    if (!this.skip && tag === "li") this.parts.push("- ");
  }

  end(tag: string) {
    if (this.roleTag) {
      if (tag === this.roleTag && !--this.roleDepth) this.roleTag = null;
      return;
    }
    if (tag === "title") this.inTitle = false;
    if (SKIP.has(tag) && this.skip) this.skip--;
    else if (BLOCK.has(tag)) this.parts.push("\n");
    if (tag === "pre" && this.pre) this.pre--;
  }

  data(text: string) {
    if (this.inTitle) this.title += text;
    // Browsers collapse whitespace outside <pre>; inside it, mark each line (\0) to keep its indentation.
    else if (!this.skip && !this.roleTag) this.parts.push(this.pre ? text.replaceAll("\n", "\n\0") : text.replace(/\s+/g, " "));
  }

  text(): string {
    const out: string[] = [];
    for (const l of this.parts.join("").split("\n")) {
      if (l.startsWith("\0")) out.push(l.slice(1).trimEnd());
      else if (!/^[-|»·•¶#\s]+$/.test(l)) out.push(l.trim());   // drop leftovers of menus and heading anchors
    }
    return out.join("\n").replace(/\n{3,}/g, "\n\n").trim();
  }
}

/** [title, readable text] of an HTML page. */
export function htmlText(html: string): [string, string] {
  const p = new PageText();
  parseHtml(html, p);
  return [p.title.trim(), p.text()];
}
