# Decision records

> Kept only where a **current** rule is counterintuitive and the reason is not
> visible in the code or in the canon. A decision that merely restates a rule
> already documented under `docs/systems/` or `docs/world/` was removed: the
> rule survives where it belongs, and `git log` keeps the rest.
>
> These are not a queue and not a status log. Superseded sequencing decisions
> (which increment lands first) were dropped entirely — that job is GitHub
> Issues'.


## DEC-0002 — Lineages are not classes or professions

**Accepted** 2026-07-29 · citizens, narrative, presentation, city

The eight lineages are cultural identities. They do not block professions, do
not guarantee competence, do not replace experience, and must never become
automatic production multipliers. Every profession admits eight approaches.

*Why it is restated here:* lineage is the most natural place in this design to
accidentally put a bonus, and a bonus is the one thing it may never carry.
Lineage may influence flavour, learning *context* and visual/audio identity —
nothing else. There is no lineage agent; the `lineages-and-cultures` skill is
consulted by whichever domain is changing.

## DEC-0011 — A wound is not depleted stamina

**Accepted** 2026-07-29 · citizens, expeditions, city, persistence

A persistent wound is a subsystem separate from stamina, so it cannot be
expressed as a low stamina value. The two interact — a wound caps usable
stamina and blocks expedition participation — but **ordinary rest never cures
it**. Treatment requires a Basic Shelter, time and an explicit resource cost.

*Why it is not obvious:* a rested but wounded citizen looks recovered in every
short-term reading. Collapsing the two axes would make the injury disappear the
moment they slept, which is exactly the no-instant-healing rule the game is
built on.

## DEC-0013 — The onboarding produces a cube and nothing else

**Accepted** 2026-08-04 · onboarding, citizens, lineage, narrative, combat, persistence

1. **The output is reduced.** The onboarding produces `LineageId`,
   `ElementalAffinity`, `FounderCubeProfile` and `FounderNarrativeMemory`.
   It must **not** produce `WeaponPreferences`, `ProfessionalAffinities`,
   `CombatStyle`, `PoliticalOrientation`, `SpiritualPosture`,
   `LeadershipStyle`, `RiskProfile` or `Traits`. Those are earned during a
   citizen's life.
2. **Canonical face names** are `Body`, `Bond`, `Stability`, `Impulse`,
   `Domain`, `Reach`. Cultural aliases (Sustancia, Relación, Contención,
   Proyección, Concentración, Distribución) may appear in lore copy but are
   never the technical identifiers.
3. **The anchoring is `60/40` per axis at the lineage vertex, with `±8` of
   onboarding variation.** This bound is load-bearing well beyond the
   onboarding — see DEC-0018.
4. **Six affinities are the six faces, independent of lineage.** Element does
   not select lineage and lineage does not force element.
5. **Equipment is a channel and a demand, not a source of power.** It must not
   grant base attack or base speed independently of the citizen.
6. **Eight lineage signatures** are visible one-liners for each vertex —
   Anclaje, Corola, Reconfiguración, Rumbo, Custodia, Adaptación, Resonancia,
   Síntesis. Each is a small interaction, never a class definition.

*Why it needed a decision:* three earlier documents described three different
scoring shapes, and the natural instinct at the end of a personality test is to
hand the player a build. Removing the extra fields is what keeps the founder a
person with predispositions instead of a class selection.

## DEC-0014 — The first night is authored, and the clock never freezes

**Accepted** 2026-08-06 · narrative, citizens, city, presentation

The period from the founder's arrival at `00:00` to dawn is a bounded authored
sequence: six linear dialogue nodes, one body variant per lineage, advancing
only on a closed dialogue node or a completed module — **never on the clock**.

Three consequences that a reader would otherwise get wrong:

- **`_tick` is never frozen.** Freezing it would stop construction, which
  depends on the tick interval, and create the circularity of "you cannot meet
  the milestone because the clock that measures it is stopped". Instead the
  *displayed* hour stalls at `05:59` and the **calendar is deferred**: no Food
  ration and no day/night boundary while the night runs.
- **The spirit's position is not persisted.** It derives from the stage, the
  way building anchors derive from placement. A temporary apparition is not a
  reason to start storing authoritative visual coordinates.
- **Quantities are never baked into dialogue text.** Every visible number is
  interpolated at runtime from the recipe, so a recipe change cannot make the
  guidance lie.

`DialogueRunner.RunAsync` is deliberately unused here: a coroutine holding its
position across `await` cannot survive save/restore. The night persists its
current node id instead.

## DEC-0015 — Slate is the neutral surface; warm tones mean state

**Accepted** 2026-08-06 · presentation

Neutral chrome — buttons, panels, inputs, the status strip — is the dark slate
9-slice. Gold and warm tones are reserved for **state**: focus ring, elevated
border, stabilised fragment pips. Green keeps only its success semantic and red
only its destructive one.

*Why it needed a decision:* the yellow surface that used to govern almost every
button was never chosen. It was the sum of two defaults — the button variation
most controls use pointed at a yellow 9-slice, and the lineage fallback pointed
at the same file while the active lineage id started as `"default"`, which was
not a key of the lineage dictionary. Every panel built before a hero existed
resolved through that fallback and, because most consumers apply the stylebox
once in `_Ready`, stayed yellow for the whole session.

Consequence worth keeping: `"default"` resolves explicitly to the neutral
surface and is deliberately **not** an entry in the per-lineage dictionary — it
is not a lineage, and the available-lineages list must keep returning exactly
eight.

## DEC-0016 — The fire spirit speaks from a balloon in the world

**Accepted** 2026-08-06 · presentation, narrative

The first night's dialogue is a speech balloon anchored over the spirit, not a
band at the bottom of the screen. The whole balloon is the confirm affordance.
It renders on the diegetic world-dialogue layer: above the ambient tint, below
the persistent HUD and below modals, and clicks outside it still reach the
world.

*Why the earlier design did not survive contact:* the bottom strip was
specified before anything rendered it — the whole sequence was inert behind a
mis-resolved `NodePath`, so it was never observed running. When it finally
rendered it inherited the yellow panel fallback, sat on the viewport's bottom
edge, and gave the words no visible speaker: the player read a caption bar
while the character teaching them stood elsewhere on screen.

Consequence: the balloon follows the spirit every frame, and the anchor is
re-derived per frame because the macro view projects its streets by hand rather
than through a camera transform. Re-parenting would not help.

## DEC-0017 — The biome is where the founder fell, not what the lineage is

**Accepted** 2026-08-06 · presentation

Each city draws its ground from one of eight biomes, keyed to the founder's
lineage. **Presentation only**: no resource, yield, recipe, rate or rule
differs by biome, and nothing about it is persisted.

*Why the framing matters:* keyed-to-lineage looks like a lineage trait, which
would make lineages destiny. It is the opposite claim — the land did not change
because of who founded it; the founder arrived somewhere. Terrain art must
never become simulation state.

## DEC-0018 — The Cube decides the expression, and a weapon family is learned in tiers

**Accepted** 2026-08-07 · citizens, combat

`PhysicalExpression` derives from the **highest face of the persisted cube**,
not from the elemental affinity. The two are independent axes.

Weapon experience is acquired at one of three rates: `100 %` for the two
families of the citizen's own expression, `50 %` for the four their lineage's
vertex also reaches, `10 %` for the remaining six. The tier scales
**experience acquisition only** — never damage, accuracy, channel transfer,
cooldown, defence or technique coefficients.

*Why it is not obvious:*

- The derivation was a **correction**. The statistics chapter publishes one
  table with three columns — face, affinity, expression — and an earlier
  implementation read it as a chain, deriving expression from affinity. That
  collapsed two independent axes into one and erased six of the thirty-six
  combinations the technique model assumes.
- **Existing saves load with a different expression than before.** Nothing on
  disk changed; the obsolete derivation simply no longer exists. No schema bump
  was needed because the expression was never a stored field.
- Each lineage admits exactly three expressions, enforced by geometry rather
  than by a blacklist: under `60/40` with the `±8` cap a favoured face stays in
  `52–68`. That cap is therefore load-bearing — widening it past `10` breaks
  the property.
- A level reached through a foreign family is worth exactly what any other
  level of that number is worth. The tier is a qualitative *learning context*,
  never a bonus.

## DEC-0019 — An ordinary citizen's cube is derived from their id, not stored

**Accepted** 2026-08-07 · citizens, persistence

Every non-founder's cube is a pure function of `(lineage, id)`: the lineage
vertex shifted `±8` per axis with FNV-1a, applied through the same clamp and
pair invariant the onboarding uses. The name index deliberately uses a
different mix of the same seed, so two migrants of one lineage are distinct
people rather than statistical copies.

*Why it is not obvious:* without this, a citizen with no onboarding sat on the
bare `60/60/60` vertex — a three-way tie — so **every** non-founder of a Body
lineage was `Fracture` and every one of a Bond lineage was `Poisoning`: two of
six expressions in play across a whole population.

**Cities saved before this date keep their uniform population.** The migration
fills the vertex only when the cube is null; variation appears only in migrants
who arrive afterwards. That is deliberate — regenerating existing people would
rewrite player history.

## DEC-0022 — The opening Spirit Trail yields a discovery, not a resource

**Accepted** 2026-08-10 · expeditions, narrative, persistence

The first `SpiritTrailSearch` is Founder-only, lasts about four world hours,
requires **no supply** and resolves as a non-material `Discovery`. Its
encounter begins at the named half-hour milestone; victory continues to a
physical trail manifestation, and only arrival back in the city completes the
expedition.

*Why it is not obvious:* the obvious implementation of a long route is to make
it cost food and pay resources, and that is what it used to do (`1 Food →
Wood`). A route whose purpose is narrative must not be balanced as a harvesting
trip, and its final material reward is deliberately left open rather than
filled with a placeholder conversion. `SupplyRequirement.None` and
`ExpeditionReward.Discovery` are real contracts, not zero-valued resources.

Combat rules are versioned per expedition, so a replay already in flight keeps
the rules it started with while new routes use the current ones.

## DEC-0023 — World time is the only authority that ends a journey

**Accepted** 2026-08-11 · citizens, presentation, persistence

A citizen's journey ends when the world tick reaches their arrival tick, and at
no other moment. Presentation draws the journey and has no vote: the macro view
paces its route across the domain's own arrival window, so the drawn arrival
lands on the tick the domain chose, speed multipliers scale it for free, and a
dropped frame catches up on the next.

*Why it needed reversing:* the previous design was deliberate and documented —
live play required the view's route completion so elapsed ticks could not start
production before the visible arrival, while offline catch-up completed the same
journey on elapsed ticks because no sprite existed. That is two semantic
authorities for one fact, and it meant an animation that never ran could hold a
citizen in transit indefinitely, with their workplace reporting
`WorkersInTransit` forever.

---

## Infrastructure

### DEC-I001 — `.agents/` is the canonical agent-context root

**Accepted** 2026-07-29

Canonical skills live in `.agents/skills/<id>/SKILL.md` and agents in
`.agents/agents/<id>/AGENT.md`. `.claude/` and `.codex/` hold generated
mirrors only: edit canonical files, then run `scripts/Sync-AgentContext.ps1`.
Introducing a second root would have created two mechanisms to synchronise.

### DEC-I002 — Codex agent adapters are delivered as Codex skills

**Accepted** 2026-07-29

Agent personas reach Codex as skills at `.codex/skills/agent-<id>/SKILL.md`.
Codex CLI has no sub-agent concept and no `.codex/agents/` directory, but it
does discover project-level skills — verified empirically with a probe skill.
The `agent-` prefix prevents collision with the domain skills of the same name.

### DEC-I003 — Mirroring is copy-based, not symlink-based

**Accepted** 2026-07-29

`Sync-AgentContext.ps1` copies content; symlinks are opt-in. `core.symlinks`
is `false` in this environment and the tracked mirror entries are regular
files, so copies are the committed, portable form. Symlinks in a working tree
are a local convenience a fresh clone would not reproduce. Mirrors are
committed, and `Validate-AgentContext.ps1` fails if they drift.
