# Changelog

Narrative history of what the connected game gained, lost or reshaped, one
entry per increment. It answers *how we got here*; three neighbours answer
other questions and must not be duplicated into this file:

- `docs/CURRENT_STATUS.md` — what the code does **today** and what is approved
  next.
- `docs/ai/CURRENT_DEVELOPMENT_STATE.md` — the do-not-regress inventory.
- `docs/session-state/STATE.txt` — the generated, machine-measured baseline of
  the session in progress.

**Contract.** Every session that produces a commit adds or extends the entry
for its increment, in the same commit. An entry states what a player can now
do that they could not before, the schema range it crossed, and the measured
baseline — not a list of touched files, which `git log` already owns.

Entries dated before 2026-08-03 were reconstructed from commit subjects when
this file was introduced. They are deliberately thin: their detail was never
written down at the time, and inventing it now would be fabrication. Read
their commits for the real content.

---

## Session state and changelog contract

**2026-08-03**

Infrastructure, not gameplay. `docs/session-state/` now holds a generated
`STATE.txt` and a dated `1280×720` frame of the city, and this file exists.

The problem it solves: `CURRENT_STATUS.md` and
`docs/ai/CURRENT_DEVELOPMENT_STATE.md` are written by hand and had drifted to
728 and 721 passing tests against a real 730, and to 761 template IDs against a
real 804. Both were corrected against the measurement in the same change.

`tools/New-SessionSnapshot.ps1 -Mode Fast` runs from a `SessionStart` hook and
reads git and source only, so it cannot delay a session start. `-Mode Full`
measures build, tests, headless boot, agent context and catalogs, and drives
the existing visual harness for the screenshot; it is what runs before a
session's first commit. Neither mode can abort a session: a failing probe is
recorded as a failing probe and the rest still run. Unverified fields say "not
measured this session" instead of restating the previous run.

Rule added to `CLAUDE.md` §3 / §5.1 and `AGENTS.md` §3 / §5.1 — the hook covers
Claude Code, the written rule is the only trigger under Codex.

---

## Author guard: no AI agent may appear as a contributor

**2026-08-03**

Nine commits carried a `Co-Authored-By: Claude <noreply@anthropic.com>`
trailer; three of them additionally had Claude as the **author and
committer**, which would have surfaced Claude as a GitHub contributor with
its own avatar. The remote `origin` was configured but had no branches, so
rewriting history was cheap; that window will not stay open after the first
push.

`git filter-branch` with `--all` was run once:

- `noreply@anthropic.com` is reassigned to the repository owner
  (`3l33f3@gmail.com`) wherever it appeared as `GIT_AUTHOR_*` or
  `GIT_COMMITTER_*`.
- `Co-Authored-By:` and AI-domain `Signed-off-by:` trailers are stripped
  from every message. Other body text is left alone.
- Original commit dates and content are preserved (`git diff
  refs/original/refs/heads/main HEAD --stat` is empty).

Prose in `CLAUDE.md` and `AGENTS.md` had carried the rule already. Prose
failed; prose alone is a request, not a guard. The repository now carries
`.githooks/commit-msg`, which rejects:

- Any `Co-Authored-By:` or `Signed-off-by:` trailer.
- Any `Generated with …` notice naming an AI agent.
- The robot marker `🤖`.
- Any author or committer identity whose email or display name matches an
  AI agent (anthropic.com, openai.com, GitHub-managed copilot addresses,
  or names like `Claude` / `Codex` / `Copilot`).

`tools/Install-AuthorGuardHook.ps1` points `core.hooksPath` at
`.githooks`, idempotent and safe to re-run. The snapshot script runs it on
every `-Mode Full` and reports the resulting state on its `Author guard`
line. The override `git commit --no-verify` exists; using it requires a
written reason in the final report.

The full pre-rewrite history is preserved in
`%TEMP%\wog-authorship-backup\pre-authorship-rewrite.bundle` for the day
something needs to be cross-checked, and `git reflog` still points to the
pre-rewrite `refs/original/*` copies until they expire.

---

## EG-4 — resource expeditions on a dynamic frontage grid

**2026-08-03 · `2d949f6c`**

### Connected

- The Campfire and the Cache each expose one finite Food and Wood opportunity.
  Dispatch reserves supply, opportunity and bounded return capacity; completion
  depletes it; cancellation and retreat release it.
- Mature-tree Wood requires the durable Primitive Axe, crafted at the Shelter
  from 1 Branch + 1 Small Stone and kept in its tool set. The first forestry
  capability is a made object instead of a free verb.
- Gathering rejects full storage before movement or drain, and treats a repeated
  request for an exhausted unit idempotently.
- Resource quantities left the status bar. They progress contextually from
  founder cargo in Construction, through the Founding Cache, to the Shelter's
  collapsible inventory.

### Reshaped

- The fixed nine-lot parcel partition became continuous frontage rows. A
  resource unit occupies only its own frontage cell instead of claiming the
  surrounding 3×3 lot; buildings reserve explicit column intervals guarded by
  persisted corridors; resources and constructions share one obstacle-footprint
  contract, of which trees are one case.
- Fresh cities expose three horizontal available parcels. No locked frontier is
  rendered or reconnoitred while expansion and its terrarium boundary language
  stay under design. Legacy parcel records are preserved.

### Schema

v24 → v28, one migration per seam.

| Version | Migration |
| --- | --- |
| v25 | Continuous frontage rows and persisted protected corridors. |
| v26 | Deterministic resource-unit positions that do not claim whole lots. |
| v27 | Finite Food/Wood opportunities, their expedition reservation and bounded return capacity. |
| v28 | The durable tool set, without granting tools to migrated saves. |

### Baseline

`dotnet build` 0 errors / 0 warnings · `dotnet test` 730 passed, 1 skipped ·
headless boot clean · agent context 437 checks · schema v28.

### Direction

Recorded in `docs/world-of-goses-design-bible/12_DYNAMIC_FRONTAGE_PLOTS_AND_CORRIDORS.md`,
which supersedes the rigid nine-lot partition previously described in chapter 03.

---

## Reconstructed history

Thin entries, recovered from commit subjects only. See each commit for content.

| Date | Commit | Subject |
| --- | --- | --- |
| 2026-07-31 | `9fc2542c` | Implement early-game resource and cultivation progression |
| 2026-07-31 | `124df29a` | Discard VS-5 and ship EG-1 resource seam |
| 2026-07-30 | `6e11a5a7` | Record why the splash working copies stay untracked |
| 2026-07-30 | `d2881bb4` | Track the AI-generated splash art as redraw reference |
| 2026-07-30 | `0fd7b55c` | Add EG-0 opening measurement, ambient day/night tint and the splash hero view |
| 2026-07-30 | `fc0bf57f` | Re-spread the Ardhen/Orveth/Vaelun accents and derive splash palettes |
| 2026-07-29 | `86db1355` | Stabilize persistent first playable loop |
| 2026-07-29 | `f2d066c8` | Add agent-context infrastructure for Codex and Claude Code |
| 2026-07-28 | `13525c96` | Advance VS-1/VS-2 vertical slice and fix macro-view/pathfinding bugs |
| 2026-07-28 | `41b7699c` | Stabilize the first playable city loop |
| 2026-07-27 | `23791ca0` | Complete localization sweep, real navmesh routing, biome terrain, ambient citizens, expedition FSM, and real frame profiling |
| 2026-07-26 | `1a5774af` | Migrate macro city view to pseudo-3D street perspective |
| 2026-07-26 | `d7eec26c` | Stabilize terrain and localization foundations |
| 2026-07-24 | `d0fd51d3` | Add astral founder flow and polish city UI |
| 2026-07-23 | `91268ddb` | Add persistent parcel resource gameplay |
| 2026-07-23 | `89c70981` | Integrate precomposed appearance variants across 192 bundles |
| 2026-07-22 | `b409d248` | Split status bar into PlayPause + Speed, forest gatherability, auto-release workers |

Earlier than 2026-07-22, `git log` is the only record.
