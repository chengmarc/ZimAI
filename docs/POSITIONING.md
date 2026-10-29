# Positioning: what ZimAI is, and for whom

Decided 2026-10-28. Read this before changing the README, the Store listing, the screenshots or the demo, and before
adding a feature: if it doesn't serve this, it needs a reason.

## The decision

> **A free, unlimited AI on your own PC for the small jobs: when your cloud AI hits its limit, or the job isn't
> worth it.**

**Slogan: "Unlimited AI for small jobs."** Second line: "Out of credits? Not here." "Small jobs" was chosen over
"odd jobs", "chores" and "loose ends" (each narrower or less plain), and without "the", as a headline.

ZimAI is **advertised as AI, not as an agent**. The agent is how it does the work, not what it's sold as.

ZimAI **sits beside** Claude, Cursor, Codex and the like; it doesn't compete with them. Nobody has to choose ZimAI over
them. They use it for the jobs those are too limited, or too costly, for.

## Who it's for

Developers who already use a cloud AI, paid or free, and run into its limits. The pitch is in their words: rate
limits, credits, "not worth Opus".

## The job it's hired for

Small, repetitive work. Before AI it was tedious; now it isn't complicated enough to spend a frontier model's quota
on:

- **The commit message after a big rework, when the quota is gone.** This is the defining scenario and the demo.
- "Where is X used?", "rename these", "what does this error mean?", ".gitignore this", a quick grep or script.

Being less capable than a cloud model is fine for these jobs. Having no limit, no cost, no account and no upload is
what matters.

## What follows from it

**Product**, in this order:

1. A one-shot mode: `zim "question"` answers and exits, so a small job doesn't need a session.
2. `zim commit`: drafts a message from the staged diff in the repo's own style (`git log`), and asks y / n / edit
   before it commits. It must handle a diff larger than the context: `git diff --stat` first, then files in chunks.
3. Test both on real diffs (this repo's history) before any copy claims them.

**Marketing**: the framing may lead the product, but no copy names a feature before it's built and demoed (the
one-shot mode, `zim commit`).

- The README, the Store listing, the screenshots, the box art and the app lead with the slogan (done 2026-10-28).
- The demo and the first screenshot should show the commit scenario, once it's been tested with gpt-oss-20b. Until
  then the copy only says you can ask ZimAI for a commit message, which the agent does today through Git.
- Supporting facts, never the headline: it runs on the gaming PC you have (or the CPU), it's private, it works
  offline, it fits on a flash drive.

## Framings considered and set aside

| Framing                                                             | Why not the lead                                                                                                                          |
| ------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| A local coding agent to rival the cloud ones                        | A 20B model loses on capability, and cloud agents have free tiers. Nobody would pick it.                                                  |
| For organizations whose code can't leave the building               | They don't give every employee a GPU PC; they buy one server and serve a bigger model to everyone. A different product.                   |
| AI for gamers and modders                                           | Narrow, and it came from the context of the work at hand, not from the users. Gaming PCs stay a supporting fact.                          |
| An engine for developers to ship local AI in their apps (game NPCs) | Those jobs need a ~4B model, not gpt-oss-20b. It would be a different product.                                                            |
| A teaching kit: an agent you can read in an afternoon               | Might work: it's small, free to run, and the 2026-10-23 deletion is a ready-made safety lesson. A possible second audience, not the lead. |
| Offline and portable ("AI in your flash drive")                     | Clever, but it names no person and no problem. Kept as a feature.                                                                         |
| AI on a USB stick, sold as a device                                 | Hardware and logistics for a niche.                                                                                                       |
| A helper for a non-technical family's PC                            | Trust is the whole product there, and a 20B model deleting files is the risk.                                                             |

## Guardrails against drift

- Don't lead with "agent", and don't compare ZimAI to cloud tools on capability.
- Don't claim what hasn't been demoed with gpt-oss-20b on real work.
- Don't widen the audience in the headline: one person, one problem.
- A new feature should make a small job faster or simpler. If it only makes ZimAI more capable at large jobs, it's off
  this course.
