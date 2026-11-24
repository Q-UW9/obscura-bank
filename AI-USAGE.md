# AI usage in Obscura Bank

This project is built with help from Claude (Anthropic), used as a tutor and pair programmer in Claude Code. This file says openly how it is used, and lists every project file that the AI writes **directly**, as opposed to code I type or paste myself after reading it.

## Why this split

Obscura Bank is a Web Technologies course project, and it has two learning goals. They are ranked:

1. **C# / .NET first.** The API (ASP.NET Core minimal APIs, Npgsql + Dapper, xUnit) is the main thing I am here to learn, together with the web security behind each feature.
2. **TypeScript / React second.** I already know them, but the client still carries security logic (session handling, redirects, validation feedback), so I need to understand every line.
3. **CSS last.** The styling serves no learning goal and carries no security logic. Having the AI write it ("vibecoding" it) saves time for the first two goals.

## Who writes what

| Area | Written by | How |
|---|---|---|
| C# (`server/`) | Me | The AI explains the concept and proposes code in the chat. I type or paste it myself, reading every line, and the AI reviews what I wrote. |
| TypeScript / TSX (`client/src/**/*.ts(x)`) | Me | Same as C#. |
| SQL migrations (`db/migrations/`) | Me | Same as C#. |
| Setup and config (Docker, `dotnet`, `npm`, `appsettings`, `vite.config.ts`, git) | Me | The AI guides me step by step. I run every command myself. |
| **CSS** (`client/src/**/*.css`) | **AI, directly** | Written into the files by the AI. I review the result in the browser. |
| **Docs** (this file, concept notes, handoff notes) | **AI, on request** | Drafted by the AI when I ask, then read and corrected by me. |

The architecture decisions (stack, data model, scope) are made by me in a separate chat and recorded before any code is written.

## Files written directly by the AI

Newest first. "Rewritten" means the AI replaced the whole file.

| Date | File | What |
|---|---|---|
| 24.11.2026 | `docs/handoff/2026-11-24-dev-script.md` | Handoff note to the Overview Chat: proposal for a dev quick-command script and how to integrate it |
| 22.11.2026 | `client/src/index.css` | Edited: the shell fills the viewport (`min-height` on `.shell`, `flex: 1` on `.body`), so login and register share one frame height; `scrollbar-gutter: stable` on `html` |
| 22.11.2026 | `docs/handoff/2026-11-22-claude-code-frontend.md` | Handoff note to the architecture chat: decisions from the E2 frontend session |
| 22.11.2026 | `AI-USAGE.md` | This file |
| 22.11.2026 | `client/src/index.css` | Rewritten: the Vite template theme replaced by the Obscura look ported from the proposal page (tokens, page frame, panels, buttons, forms, notices) |

Docs written earlier with the AI on request: the concept notes in `docs/notes/` and the feature map `docs/progress.json` (updated through a tool during sessions).

## Rules for adding to this list

- Every file the AI writes or rewrites directly gets a row here in the same change, with the date and a one-line summary.
- Code the AI proposes in the chat that I type or paste myself is not listed file by file. It is covered by the table above.
