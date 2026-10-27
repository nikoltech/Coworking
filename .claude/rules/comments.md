---
paths:
  - "**/*.{cs,csx}"
  - "**/*.{csproj,props,targets,slnx,dcproj,config,xml,resx}"
  - "**/*.{yml,yaml}"
  - "**/Dockerfile*"
  - "**/*.hbs"
  - "**/*.{js,jsx,ts,tsx}"
  - "**/*.{sh,ps1}"
---

# Comments

- Before writing one, try in order: rename, extract, restructure. Comment only if all three fail.
- Delete any comment that restates adjacent code at the same level of abstraction. Keep it if it
  adds a level: intent, contract, invariant above — or a non-obvious detail below.
- Worth writing: why not the obvious approach, workarounds (link the issue), invariants the types
  can't express, business rules, spec references, code that looks wrong but is correct.
- Never: narrate the implementation, log changes ("was X, now Y"), leave commented-out code,
  write TODO without a ticket, or add comments to code you didn't change.
- Section markers: only in long functions with genuinely distinct phases. Name the phase
  ("fast path: cached"), not the category ("Helpers"). No ====, no boxes, no ALL CAPS — a blank
  line and one lowercase line is enough. If every block needs one, split the function instead.
- Docstrings: public API only. One sentence starting with the name. Contract, not implementation.
  No params that restate name and type.
- Match the comment density of the file you are editing. Default is no comment.
