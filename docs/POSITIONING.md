# Positioning: what ZimAI is, and for whom

Decided 2026-10-28; rebuilt around the usage limit on 2026-10-29. Read this before changing the README, the Store
listing, the screenshots or the demo, and before adding a feature: if it doesn't serve this, it needs a reason.

## The decision

> **A free, unlimited AI on your own PC for the small jobs, for when your AI hits its usage limit mid-task.**

**Slogan: "Unlimited AI for small jobs."** Second line: "Out of credits? Not here." Both stay as the headline of the
README, the Store listing, the box art and the app.

ZimAI is **advertised as AI, not as an agent**. The agent is how it does the work in a terminal, not what it's sold as.

ZimAI **sits beside** the AI people already pay for; it doesn't compete with it. Nobody has to give anything up. They
use ZimAI when theirs has stopped, or for the jobs not worth its limit.

## Who it's for

**Anyone who depends on AI and hits its usage limit**, in a chatbot or in a coding tool. They've handed the small
stuff to AI: emails, rewording, quick questions, commit messages, renames. They don't do it by hand any more. That
dependence is why the limit hurts.

They reach ZimAI two ways, one for each kind of user:

- **Chat**, for everyone: everyday questions, writing and rewording, in the app's own window.
- **Terminal**, for developers: jobs in their files and projects (the commit message, "where is this used?", a rename).

**The audience is limited by the PC.** ZimAI needs an NVIDIA GPU with 6 GB of video memory, or 16 GB of RAM to run on
the CPU, slowly. So the people it can reach are the AI-dependent who own a capable PC: gamers, creators, developers.
The copy says what it needs plainly, and the app checks the PC before anything downloads.

## The moment

Mid-task, a banner: **"You've reached your usage limit. Resets at 4:00 PM."** Everyone who has hit it knows that
sentence. It's the hook: show it, and the reader recognizes themselves before reading anything else.

## The trap: three ways out, all bad

1. **Wait** three to five hours, for a job that takes two minutes.
2. **Pay** for extra usage, or go from the $20 plan to the $200 one, for a single email or commit message.
3. **Do it yourself**, which in theory they could. In practice they don't any more, and that's the point.

## The way out

ZimAI is the fourth option: **keep going**, on your own PC, with nothing that runs out. "Small jobs" stays in the
slogan because it's honest about a 20B model, and the small job is exactly what's left when the limit hits.

## The four screenshots

One line each, in upload order. Each pairs a line with what it shows:

| # | Line                                       | Shows                                                                             |
| - | ------------------------------------------ | --------------------------------------------------------------------------------- |
| 1 | Hit your limit? Keep going.                | the moment: a usage-limit banner, generic, and ZimAI picking up the job           |
| 2 | Resets at 4 PM? Not here.                  | Chat finishing an everyday job (an email, a rewrite) with no clock running        |
| 3 | The AI that never says *come back later*.  | the terminal doing a developer's small job in their project                       |
| 4 | Your AI ran out. Yours didn't.             | the home page: it's on your own PC, checked and ready (the GPUs it runs on)       |

The screenshots follow the app as it is (the 960 × 640 window, the home page without pills).

## How it talks

- **In the customer's words:** "usage limit", "resets in 5 hours", "upgrade", "the $200 plan", "out of credits".
- **Relief, with a little wit.** No crude language, no mocking the customer for depending on AI: we're on their side.
- **Generic about the others.** Never name Claude, ChatGPT, Cursor or their plans, and never copy their interfaces;
  the limit banner in a screenshot is our own drawing of a generic one. Readers recognize it anyway, and it keeps the
  copy clear of trademark trouble and of Store review.

## The jobs, as the copy names them

Small jobs, done many times a day. Before AI they were tedious; now they aren't worth a paid model's limit:

- **Everyone:** reply to this email, reword this paragraph, summarize this, what does this mean, a quick question.
- **Developers:** the commit message after a big rework when the quota is gone, "where is X used?", rename these,
  what does this error mean, a quick grep or script.

Being less capable than a cloud model is fine for these jobs. Having no limit, no cost, no account and no upload is
what matters.

## What follows from it

**Product**, as before, in this order:

1. A one-shot mode: `zim "question"` answers and exits, so a small job doesn't need a session.
2. `zim commit`: drafts a message from the staged diff in the repo's own style (`git log`), and asks y / n / edit
   before it commits. It must handle a diff larger than the context: `git diff --stat` first, then files in chunks.
3. Test both on real diffs (this repo's history) before any copy claims them.

**Marketing**, in this order:

1. The README: the headline as it is, then the moment, the trap and the way out, then Chat and the terminal as the
   two ways in.
2. The Store listing: description, short description, features and keywords. Its category moves from Developer tools
   to one that fits anyone (Productivity), since developers are now one of two audiences.
3. The four screenshots (above), and the demo.

No copy names a feature before it's built and demoed (the one-shot mode, `zim commit`). Until then the copy only says
you can ask ZimAI for a commit message, which the agent does today through Git.

Supporting facts, never the headline: it runs on the gaming PC you have (or the CPU), it's private, it works offline,
it fits on a flash drive.

## Framings considered and set aside

| Framing                                                             | Why not the lead                                                                                                                          |
| ------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------- |
| Developers only (the 2026-10-28 audience)                           | The pain is the usage limit, not the code. Chatbot users hit it too, and Chat already serves them. Developers stay one of two audiences.   |
| A local coding agent to rival the cloud ones                        | A 20B model loses on capability, and cloud agents have free tiers. Nobody would pick it.                                                  |
| For organizations whose code can't leave the building               | They don't give every employee a GPU PC; they buy one server and serve a bigger model to everyone. A different product.                   |
| AI for gamers and modders                                           | Narrow, and it came from the context of the work at hand, not from the users. Gaming PCs stay a supporting fact.                          |
| An engine for developers to ship local AI in their apps (game NPCs) | Those jobs need a ~4B model, not gpt-oss-20b. It would be a different product.                                                            |
| A teaching kit: an agent you can read in an afternoon               | Might work: it's small, free to run, and the 2026-10-23 deletion is a ready-made safety lesson. A possible second audience, not the lead. |
| Offline and portable ("AI in your flash drive")                     | Clever, but it names no person and no problem. Kept as a feature.                                                                         |
| AI on a USB stick, sold as a device                                 | Hardware and logistics for a niche.                                                                                                       |
| A helper for a non-technical family's PC                            | Trust is the whole product there, and a 20B model deleting files is the risk.                                                             |

## Guardrails against drift

- One problem in every headline: the usage limit. The audience is wide; the problem isn't.
- Don't lead with "agent", and don't compare ZimAI to cloud tools on capability.
- Don't claim what hasn't been demoed with gpt-oss-20b on real work.
- Don't name the other products or copy their look.
- A new feature should make a small job faster or simpler. If it only makes ZimAI more capable at large jobs, it's off
  this course.
