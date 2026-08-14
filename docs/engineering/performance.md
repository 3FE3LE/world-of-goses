# Performance Budgets

> Frame-time targets per scenario. Measured with the engine's own
> `Performance.Monitor.TimeProcess` (real per-frame process cost, not a
> host-side proxy — see "Hooks in the capture harness" below) via the
> autoprofile pass in `tools/Capture-VisualMatrix.ps1`. The harness
> writes `frame-time.json` next to the manifest; a frame that exceeds
> **2× the budget** logs a `Write-Warning`, not a failure — the
> screenshot the harness exists to produce must never be lost to a perf
> regression.

## Sustained target

**60 fps = 16.67 ms / frame** in all scenarios below. Budgets are
written as `< N ms` to leave headroom for the rest of the frame
(input, audio, OS).

## Frame budgets by scenario

| Scenario                                | Frame budget | Spike budget (1×) |
|-----------------------------------------|--------------|-------------------|
| Idle, 1×, 0 buildings, 0 citizens       | < 4 ms       | < 8 ms            |
| Idle, 1×, 1 building, 1 citizen         | < 8 ms       | < 16 ms           |
| Idle, 1×, 10 buildings, 10 citizens      | < 12 ms      | < 24 ms           |
| Idle, 4×, 10 buildings, 10 citizens      | < 20 ms      | < 40 ms           |
| Active onboarding, 1×, 0 buildings       | < 8 ms       | < 16 ms           |
| Active construction modal, 1×           | < 10 ms      | < 20 ms           |

The "1×" / "4×" suffix refers to the simulation speed multiplier. At
4× the controller calls `AdvanceWorldTick` four times per real-world
second; each tick exercises the domain, so the budget reflects the
multiplier.

## Allocation budget

The idle manager should **not allocate during steady-state ticks**.
That is: between two world ticks, the GC heap should be flat.
**Not implemented in the harness** — no memory profiling code exists
in `tools/Capture-VisualMatrix.ps1` or the game today (an earlier
version of this doc claimed a `Mono.GetTotalMemory()` 2 % assertion
that was never built). Verified
manually during interactive play until a real hook is added.

A growing heap during steady state is a red flag. Common offenders:
- New lambdas or tuples per tick.
- String concatenation that bypasses `StringBuilder` (rare in this
  project — most text is event keys).
- LINQ over `ICollection` enumerables that re-allocate.

## When a budget is breached

- A frame exceeds **2× the documented budget** in the autoprofile
  output → open a GitHub issue with the manifest.
- The GC heap grows noticeably during manual interactive testing →
  open a GitHub issue (no automated check
  yet — see "Allocation budget" above).
- The "10 buildings, 10 citizens" budget breaches 12 ms → migrate to
  `MultiMeshInstance2D` or `TileMap` earlier than
  the original trigger.

## Territory extent budget

A fresh city currently projects three horizontal parcels and 23 finite natural
resource units. This is the opening composition, not the complete envelope.
The current target maximum is 8×9 parcels; it remains provisional until dense
building/resource layouts and 25/50 visible-citizen fixtures prove that the
simultaneous presentation load is viable.

Expansion remains mechanically suspended until those measurements and its
causal acquisition rules exist. Persisted off-screen parcels need not be
rendered: the bounded 8×9 “digital terrarium” uses a moving depth window,
provided the visible set can be brought inside the budgets above.

`terrarium-8x9-window` is the current presentation-only extent probe: eight
parcel rows by nine columns, 72 parcels total, with only a thirteen-street
camera window rendered at once. It does not alter fresh worlds or saves. The
older 16/21-row probes remain historical evidence for rejecting full-territory
rendering.

Three windowed runs on 2026-08-03 ranged from `6.117` to `40.115 ms` process
time per frame with empty added rows; the direct-viewport capture run remained
between `28.665` and `40.115 ms`. Even the best sample exceeds the `< 4 ms`
empty-city budget, while the worst exceeds its `8 ms` spike budget several
times over.
Drawing the entire semantic territory every frame is therefore rejected at
this extent. A large final city requires visible-street culling/LOD before
dense buildings or citizens are added; this does not argue for reducing the
semantic territory itself.

The final 8×9 windowed capture measured `15.201 ms` while rendering thirteen
street bands. That is a material improvement over drawing all 48 streets, but
it still breaches the empty-city budget. Lateral culling or batching remains
required before this envelope becomes production territory.

## Hooks in the capture harness

`tools/Capture-VisualMatrix.ps1` measures frame time on the
`--wog-visual-capture` mode. The real chain (reworked 2026-07-27
after an audit found the prior version measured PowerShell host
`Start-Sleep` interval drift — blind to any real stall inside the
Godot process itself):

1. While `WOG_VISUAL_CAPTURE=1`, `CityWorldController._Process` calls
   `Performance.GetMonitor(Performance.Monitor.TimeProcess)` — the
   engine's own real per-frame cost — every frame and prints it as
   `[WOG-FRAME-TIME] <ms, invariant culture>` (capped at 300 samples,
   ~5 s at 60 fps, so a long-lived capture window can't grow the log
   unbounded).
2. The harness waits for the screenshot to complete (startup + any
   clicks), then a short extra 500 ms, so real frames have accumulated
   in the log regardless of whether `NormalizedClicks` was used.
3. It tails the log's last 30 `[WOG-FRAME-TIME]` lines, parses them
   with `double.TryParse(..., InvariantCulture)`, and writes them to
   `frame-time.json` next to the manifest.
4. `Write-Warning` (not a terminating failure) if the max sample
   exceeds 2× the scenario's spike budget (40 ms), or if zero samples
   parsed (e.g. an older game build without the print, or a genuinely
   empty log).

**Real bug found and fixed while building this:** the game formatted
the printed value with the OS locale's own decimal separator (`F3`
without a culture), so on a comma-decimal locale (e.g. `es-*`) every
sample silently failed to parse and `frame-time.json` came back
empty — caught by actually running a capture, not by reading the code.
Fixed by formatting with `CultureInfo.InvariantCulture` at the source.

The script does **not** enforce the GC budget (Godot's memory API
requires a debug build and a connected debugger). The GC budget is
verified manually during interactive play until a CI hook is added.
