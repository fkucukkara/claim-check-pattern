---
name: "PR Review Summary"
description: "Generate a PR review summary from the current branch diff versus main; use for git diff main...HEAD, what changed, and why changed summaries."
argument-hint: "Optionally provide extra review context or constraints"
agent: "agent"
---

Review the current branch against `main`, equivalent to `git diff main...HEAD`, and produce a concise PR summary.

Use available repository context and diff tooling to inspect the changes. If the branch diff is unavailable, explain what is missing and ask the user for the minimum extra context needed instead of guessing.

Requirements:

- Ground every statement in the observed diff.
- Be concise, but specific.
- Reference the most important files, symbols, or behaviors that changed.
- Do not invent intent. If the reason for a change is not explicit, say `Why is not explicit in the diff` and provide only a clearly labeled inference when it is strongly supported by the code.
- Prefer plain English over implementation trivia.

Return exactly this structure:

## Title

One short PR title in sentence case.

## What was Changed?

- Bullet list of the key code, config, API, schema, test, or documentation changes.
- Mention the affected files or components where helpful.

## Why was Changed?

- Bullet list describing the purpose or motivation supported by the diff.
- If intent is uncertain, say so explicitly instead of over-claiming.

Quality bar:

- Focus on reviewer-relevant changes, not every line edit.
- Call out breaking changes, behavior changes, or operational/configuration impact when present.
- If the diff is tiny, keep the summary tiny.