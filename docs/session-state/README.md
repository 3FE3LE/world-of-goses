# Session state

Generated evidence of where the game actually was at the start of a session,
and what it looked like. Nothing here is hand-written.

| File | Written by | Committed |
| --- | --- | --- |
| `STATE.txt` | `tools/New-SessionSnapshot.ps1` | Yes, overwritten each session |
| `<date>-macro-1280x720.png` | the same script, via `tools/Capture-VisualMatrix.ps1` | Yes, one new file per session that commits |

## Why it exists

`docs/CURRENT_STATUS.md` and `docs/ai/CURRENT_DEVELOPMENT_STATE.md` are written
by hand, so they drift: on 2026-08-03 they claimed 728 and 721 passing tests
against a real 730, and 761 template IDs against a real 804. A hand-maintained
number is a claim; this directory holds the measurement. When the two disagree,
the measurement wins and the prose gets corrected.

The screenshot exists for the same reason in the visual dimension. A clean
build and a green suite say nothing about whether the city still renders, and
this project has already shipped states that booted fine and looked wrong.

## STATE.txt

Overwritten on every session start. Do not hand-edit it and do not cite it as
design intent — it only reports what it measured.

Fields it cannot verify say **"not measured this session"** rather than
repeating the previous run's value. That is the point of the file: a stale
number restated with confidence is worse than an admitted gap.

## Screenshots

One `1280×720` frame per session that produces a commit, dated, never
overwritten — the history is meant to be scrubbed through. Around 50 KB each
at pixel-art densities.

The matching `1920×1080` frame and the capture manifest stay in
`%TEMP%\wog-session-<date>\` as review artifacts, per
`docs/VISUAL_REGRESSION.md`. Committing both resolutions would double permanent
history growth in a repository with no Git LFS to prove nothing the baseline
does not already prove.

The frame shows the live save as a fixture under `WOG_VISUAL_CAPTURE=1`, so it
reflects whatever in-world hour that save is sitting at — a night frame is not
a regression. No persistence write happens during capture.

## Running it

```powershell
# Session start. Git and source only, no dotnet, no Godot, under a second.
pwsh ./tools/New-SessionSnapshot.ps1 -Mode Fast

# Before the session's first commit. Measures everything and captures.
pwsh ./tools/New-SessionSnapshot.ps1 -Mode Full

# Same, on a machine with no interactive desktop.
pwsh ./tools/New-SessionSnapshot.ps1 -Mode Full -SkipCapture
```

`-Mode Fast` runs automatically through the `SessionStart` hook in
`.claude/settings.json`. Codex has no equivalent hook, so there the rule in
`AGENTS.md` §3 is the only trigger.

The capture needs a real Godot window; the headless dummy renderer cannot
produce one. On a desktop where the harness reports a `50×50` client — a known
intermittent failure documented in `docs/VISUAL_REGRESSION.md` — the script
records the failure in `STATE.txt` and continues. It never aborts a session.
