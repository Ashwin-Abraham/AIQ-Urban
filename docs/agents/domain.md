# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase.

## Before exploring, read these

This repo uses **multiple contexts** — each AIQ.Urban subproject has its own CONTEXT.md:

- `src/AIQ.Urban.Core/CONTEXT.md` — Core domain model (ModelElements, UrbanModel, pipeline)
- `src/AIQ.Urban.Rhino/CONTEXT.md` — Rhino plugin layer (commands, layer conventions)

There is no root-level `CONTEXT.md` or `CONTEXT-MAP.md` yet. Read the relevant context(s) for the area you're working in.

Also check `docs/adr/` if it exists for system-wide decisions.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront.

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in `CONTEXT.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal — either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/grill-with-docs`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding.
