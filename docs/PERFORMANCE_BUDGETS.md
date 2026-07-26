# Performance Budgets

> Frame-time targets per scenario. Measured with the Godot profiler
> (`--debug` build) and the autoprofile pass in
> `tools/Capture-VisualMatrix.ps1`. The harness writes
> `frame-time.json` next to the manifest; a frame that exceeds **2× the
> budget** fails the capture.

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
That is: between two world ticks, the GC heap should be flat. The
autoprofile harness enables the Godot memory profiler and asserts
`Mono.GetTotalMemory()` does not grow by more than 2 % over a
30-frame window in any scenario above.

A growing heap during steady state is a red flag. Common offenders:
- New lambdas or tuples per tick.
- String concatenation that bypasses `StringBuilder` (rare in this
  project — most text is event keys).
- LINQ over `ICollection` enumerables that re-allocate.

## Trigger to open a performance slice

- A frame exceeds **2× the documented budget** in the autoprofile
  output → open a sub-item of the relevant S-1 sub-item.
- The GC heap grows by more than 2 % over a 30-frame window in any
  scenario → open a sub-item of the relevant S-1 sub-item.
- The "10 buildings, 10 citizens" budget breaches 12 ms → migrate to
  `MultiMeshInstance2D` (S-1.4) or `TileMap` (S-1.3) earlier than
  the original trigger.

## Hooks in the capture harness

`tools/Capture-VisualMatrix.ps1` measures frame time on the
`--wog-visual-capture` mode using a deferred loop. The script:

1. Waits 2 s after the window opens (warm-up, not measured).
2. Samples 30 frames via `Engine.GetFramesPerSecond()` or
   `_process(delta)` cumulative timing.
3. Writes the `delta` array to `frame-time.json` next to the manifest.
4. Exits with code 1 if any frame exceeds 2× the scenario budget.

The script does **not** enforce the GC budget (Godot's memory API
requires a debug build and a connected debugger). The GC budget is
verified manually during interactive play until a CI hook is added.
