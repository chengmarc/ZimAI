// The words on the Store's images (shots.html, logo.html), one set per language, as JSON. The listing text makes the same
// claims, in ../localization/, and the shots' lines come from docs/POSITIONING.md; change them together.
const CAPTIONS = {
  "en": {
    "tagline": "Unlimited AI for Small Jobs",
    "facts": [
      "No limits",
      "Works offline",
      "No account"
    ],
    "gpu": "Dedicated GPU Preferred",
    "price": "$0",
    "priceText": "No usage limit, no credits, no plan to upgrade",
    "wall": {
      "head": "Usage limit",
      "title": "You've reached your usage limit",
      "used": "<b>100%</b> used",
      "resets": "Resets at 4:00 PM",
      "upgrade": "Upgrade plan",
      "wait": "Wait 4 h 12 m"
    },
    "shots": {
      "limit": {
        "title": "Hit your limit? <span class=\"brand\">Keep going.</span>",
        "text": "When your AI stops halfway through, ZimAI picks up the job, on your own PC, with no limit of its own."
      },
      "chat": {
        "title": "Resets at 4 PM? <span class=\"brand\">Not here.</span>",
        "text": "Emails, formulas, rewrites, quick questions: ask as often as you like. Your chats stay on your PC."
      },
      "home": {
        "title": "Your AI runs out. <span class=\"brand\">Your PC didn't.</span>",
        "text": "It runs on your own PC: your GeForce, or just the CPU. ZimAI checks it before downloading, then fits the model to it."
      },
      "terminal": {
        "title": "The AI that never says <span class=\"brand\">come back later.</span>",
        "text": "For developers, a terminal that works in your projects. It reads, runs and edits on your PC, asking first."
      }
    }
  }
};
