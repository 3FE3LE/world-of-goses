# Decision log

> Versioned record of decisions that constrain future work.
>
> This log **records** decisions established elsewhere; it does not create
> them. A design decision may only be listed as `Accepted` when it is
> explicitly stated in the design bible or another canonical document, with a
> citation. Anything else is `Proposed` or `Open`.

Source shorthand: `bible/NN` = `docs/world-of-goses-design-bible/NN_*.md`.

---

## DEC-0001: `Citizen` is the only personal entity

**Status:** Accepted
**Date:** 2026-07-29 (recorded; decision predates this log)

**Decision:**
A single `Citizen` entity represents every person in the game. Hero, miner,
medic, artisan, leader, and adventurer are assignments, competencies, ranks,
memberships, recognitions, or history attached to that entity.

**Reason:**
bible/04: "No crear entidades o subclases separadas para héroe, minero, médico,
artesano, líder o aventurero." Reinforced by bible/10 guard-rail "No separar
héroes y habitantes."

**Affected domains:** citizens, expeditions, city, persistence, presentation.

**Consequences:**
Profession and hero state accumulate rather than replace. Any feature that
would introduce a parallel person type must be redesigned.

**Documents affected:** bible/04, bible/10.
**Code affected:** `game/scripts/Domain/Citizen.cs`, `Role.cs`, `CompetencyEntry.cs`.

---

## DEC-0002: Lineages are not classes or professions

**Status:** Accepted
**Date:** 2026-07-29 (recorded)

**Decision:**
The eight lineages (Ardhen, Eirune, Kovari, Myrven, Vaelun, Orveth, Caelith,
Theryn) are cultural identities. They do not block professions, do not
guarantee competence, do not replace experience, and must not become automatic
production multipliers.

**Reason:**
bible/06: "No son profesiones ni clases de combate." bible/04 lists the
prohibited effects explicitly. bible/10 guard-rail: "No convertir linajes en
clases profesionales."

**Affected domains:** citizens, narrative, presentation, city.

**Consequences:**
Lineage may influence flavor, learning speed, and visual/audio identity, but
every profession admits eight approaches. There is no lineage agent; the
`lineages-and-cultures` skill is consulted by whichever domain is changing.

**Documents affected:** bible/04, bible/06, `docs/LINEAGE_DESIGN_MATRIX.md`.
**Code affected:** `game/scripts/Domain/LineageDefinition.cs`, `LineageId.cs`.

---

## DEC-0003: The domain does not depend on Godot

**Status:** Accepted
**Date:** 2026-07-29 (recorded)

**Decision:**
Domain and simulation code is plain C# with no dependency on nodes, sprites,
animations, cameras, frame rate, input, or asset paths. Presentation reads
domain state and renders it.

**Reason:**
bible/10: "El dominio no depende de nodos, sprites, animaciones, cámaras, frame
rate, input ni rutas de assets." bible/01 principle 13.
`docs/PRODUCT_DIRECTION.md`: "Keep the simulation deterministic and independent
of Godot."

**Affected domains:** all.

**Consequences:**
`game/scripts/Domain/` must contain no `using Godot` and no `res://` paths.
This is enforced by `DomainBoundaryTests`.

**Documents affected:** bible/10, `docs/ARCHITECTURE.md`.
**Code affected:** `game/scripts/Domain/**`, `tests/WorldofGoses.Tests/DomainBoundaryTests.cs`.

---

## DEC-0004: Production is causal

**Status:** Accepted
**Date:** 2026-07-29 (recorded)

**Decision:**
A building does not produce merely by existing. Output depends on accessible
resource, workers, competence, tools, materials, energy, health, logistics,
storage, policy, and risk. Every blocker surfaces as a visible stop cause.

**Reason:**
bible/02 pillar 4: "Un edificio no produce por existir." bible/01 principle 9:
"Sin eficiencia mágica."

**Affected domains:** city, citizens, presentation.

**Consequences:**
New production must add causes, not flat rates. Stop causes are part of the
feature, not an afterthought.

**Documents affected:** bible/02.
**Code affected:** `BuildingProductionCalculator.cs`, `ProductionStopCause.cs`, `Recipes.cs`.

---

## DEC-0005: An expedition includes the return leg

**Status:** Accepted
**Date:** 2026-07-29 (recorded)

**Decision:**
An expedition is outbound, objective, and return. It does not end on reaching
the objective; it must return or trigger emergency return. Survivors come back
without equipment and carrying their wounds.

**Reason:**
bible/05: "La expedición no termina visualmente al alcanzar el objetivo. Debe
regresar o activar retorno de emergencia." and "Los habitantes vivos regresan
sin equipo y con sus heridas."

**Affected domains:** expeditions, citizens, city, persistence.

**Consequences:**
Any expedition feature must model the return. A one-way timer that yields
resources is not an expedition.

**Documents affected:** bible/05.
**Code affected:** `Expedition.cs`, `ExpeditionPhase.cs`, `ExpeditionStatus.cs`.

---

## DEC-0006: One city, no meta-progression

**Status:** Accepted
**Date:** 2026-07-29 (recorded)

**Decision:**
The city is the long-term protagonist. One game is one city. There is no
prestige mechanic rewarding its destruction and restart, and no bonus carried
between cities. The city continues operating while the game is closed, and is
evaluated across multiple independent axes rather than one level.

**Reason:**
bible/01: "La ciudad es la protagonista de largo plazo." and "Una partida
representa una ciudad. No hay prestigio que recompense destruirla y reiniciar."

**Affected domains:** all.

**Documents affected:** bible/01, bible/03, `docs/PRODUCT_DIRECTION.md`.

---

## DEC-0007: Local structured save before any backend

**Status:** Accepted
**Date:** 2026-07-29 (recorded)

**Decision:**
Persistence is local, structured, schema-versioned JSON with migrations,
snapshots, and an event log. No backend, database, or network component until a
validated need exists.

**Reason:**
bible/10: "Primera opción: guardado local estructurado, versionado de esquema,
migraciones, snapshots... Postgres no se justifica para el primer prototipo.
Backend externo solo cuando exista una necesidad validada."

**Affected domains:** persistence, all state-owning domains.

**Documents affected:** bible/10, `docs/ARCHITECTURE.md`.
**Code affected:** `game/scripts/Domain/Persistence/**`.

---

## DEC-0008: 2D pixel art with integer scaling and nearest filtering

**Status:** Accepted
**Date:** 2026-07-29 (recorded)

**Decision:**
Pure 2D pixel art at a logical resolution of 1280 x 720. Integer scale, nearest
filter, integer positions, no antialiasing on sprites and pixel-art UI. Not
2.5D as the primary direction.

**Reason:**
bible/08: "Pixel art 2D puro... Escala entera. Filtro nearest." bible/10 repeats
the pixel-perfect rules.

**Affected domains:** presentation, art pipeline.

**Documents affected:** bible/08, bible/10, `docs/ART_PIPELINE.md`.

---

## DEC-0009: Emergent history, not a mandatory linear campaign

**Status:** Accepted
**Date:** 2026-07-29 (recorded)

**Decision:**
The game's backbone is emergent history produced by the city and its citizens,
not a scripted linear campaign. The world still needs lore, cultures, and
memory, but does not require a mandatory final villain.

**Reason:**
bible/01: "No se plantea una campaña lineal como columna vertebral." and "El
mundo necesita lore, culturas y memoria, aunque no necesite un villano final
obligatorio."

**Affected domains:** narrative, city, citizens.

**Documents affected:** bible/01.

---

## DEC-0010: No instant healing; wounds require treatment

**Status:** Accepted
**Date:** 2026-07-29 (recorded)

**Decision:**
There is no general instant healing. A wounded person requires beds, staff,
medicine, time, and rehabilitation. Expedition survivors return carrying their
injuries and the city must treat them.

**Reason:**
bible/01 principle 8: "Sin curación instantánea. Las heridas requieren
tratamiento." bible/02 pillar 6. bible/05.

**Affected domains:** citizens, city, expeditions.

**Documents affected:** bible/01, bible/02, bible/05.

---

## DEC-0011: Persistent wound model distinct from stamina

**Status:** Accepted
**Date:** 2026-07-29

**Decision:**
Model persistent wounds as a subsystem separate from the existing stamina
model, so that a wound is not expressible as depleted stamina. The two systems
may still interact: a wound can cap usable stamina and restrict work or
expeditions, but ordinary stamina recovery never cures the wound. Treatment
requires Basic Shelter, time, and an explicit resource cost.

**Reason:**
The user explicitly approved this relation on 2026-07-29. It preserves the
bible's durable-health/no-instant-healing rule while keeping the existing
short-term exertion system legible. A rested but wounded citizen therefore
remains wounded; the injury still matters because it reduces their effective
stamina ceiling and blocks expedition participation until treatment completes.

**Affected domains:** citizens, expeditions, city, persistence.

---

## DEC-0012: Player-facing time and official visual resolutions

**Status:** Accepted
**Date:** 2026-07-29

**Decision:**
Simulation ticks remain an internal domain/persistence unit and must never be
shown as player-facing copy or a UI unit. Presentation converts durations into
world days, hours, and minutes through one shared formatter. The official
visual-regression matrix uses 1280×720 and 1920×1080; unusual aspect ratios are
targeted exploratory checks when a layout change puts them at risk.

**Reason:**
The user explicitly found raw ticks neither measurable nor referential in the
interface and chose 720p plus 1080p as the useful review pair. This keeps the
logical pixel-art baseline and a common full-HD target without making every UI
change pay for a third routine capture.

**Affected domains:** presentation, localization, validation tooling.

---

## DEC-0013: Onboarding output, canonical cube axes, elemental affinities

**Status:** Accepted
**Date:** 2026-08-04

**Decision:**

1. **Onboarding output is reduced to a `CubeProfile`.** The onboarding
   produces only `LineageId`, `ElementalAffinity`, `FounderCubeProfile`
   (six continuous stats), and `FounderNarrativeMemory`. It must **not**
   produce `WeaponPreferences`, `ProfessionalAffinities`, `CombatStyle`,
   `PoliticalOrientation`, `SpiritualPosture`, `LeadershipStyle`,
   `RiskProfile`, or `Traits`. These fields are eliminated from the
   output. Traits and competencies are acquired during the citizen's
   life (see `bible/04` § *Cinco capas de competencia*).

2. **Canonical cube stat names** are `Cuerpo`, `Vínculo`, `Estabilidad`,
   `Impulso`, `Dominio`, `Alcance`. Cultural aliases (`Sustancia`,
   `Relación`, `Contención`, `Proyección`, `Concentración`,
   `Distribución`) may appear in lore copy but are not the technical
   identifiers.

3. **Initial bonus anchoring** is `60/40` per cube axis at the lineage
   vertex, with `±8` per axis as the onboarding variation range
   (≈52–68). The cube is calculated in **shadow mode** in parallel with
   the existing lineage scoring; the existing scoring remains the
   source of truth until parity is demonstrated. The cube never
   replaces the current algorithm without explicit parity evidence.

4. **Six elemental affinities** are defined as the six cube faces,
   independent of lineage: `Tierra`, `Éter`, `Agua`, `Fuego`,
   `Neutra/Silencio`, `Aire`. Element does not select lineage and
   lineage does not force element.

5. **Equipment is a channel and a demand, not a source of power.** The
   citizen produces the capacity; equipment channels it, demands
   effort, and wears. Equipment must not grant attack base or speed
   base independently of the citizen.

6. **Eight line signatures** are canonized as visible one-liners
   expressing each lineage vertex: Ardhen = Anclaje, Eirune = Corola,
   Kovari = Reconfiguración, Vaelun = Rumbo, Orveth = Custodia,
   Myrven = Adaptación, Theryn = Resonancia, Caelith = Síntesis.
   Each is a small interaction, never a class definition.

7. **Lineage lore and lineage system guidelines** are consolidated into
   `bible/14-21_LINEAGES_*.md` (one chapter per lineage) and the
   shared cube mechanics into `bible/13_KOVARI_CUBE.md`. The original
   `docs/ravatha_lore_package/`, `docs/RAVATHA_LINEAGE_SYSTEM_GUIDELINES/`
   and `docs/KOVARI_CUBE_ONBOARDING_INTEGRATION_GUIDELINE.md` are
   archived under `docs/_archive/ravatha-source-2026-08-04/` for
   traceability. They are no longer canonical.

**Reason:**
The user explicitly chose this model on 2026-08-04 during the Ravatha
documentation consolidation session. It aligns with `DEC-0002`
(lineages are not classes), with `bible/04` § *Reglas de datos y
balance* (no flat bonuses), and with `bible/06` § *No son profesiones
ni clases de combate*. It removes contradictory scoring shapes
described in three different places (the cube doc, the cube stats
system doc, and the cube guideline) and gives the cube a stable
integration path via shadow mode.

**Affected domains:** onboarding, citizens, lineage, narrative,
presentation, combat, persistence.

**Consequences:**
`FounderNarrativeResult` and `FounderNarrativeScorer` must drop the
fields listed above. The persisted founder record stores the cube
profile and the narrative memory, not the eliminated fields. The
`Profile` snapshot rendered by `FounderArrivalSequence` must show
only lineage, affinity, three cube axes and the narrative summary;
not weapons, professions, politics, posture, risk or leadership
style. Lineage UI themes continue to change palette/borders/fills
only; the cube panel is functionally shared across lineages and
constrained to a numerate breakdown (see
`bible/13_KOVARI_CUBE.md` § *Estadísticas derivadas y desglose
explícito*).

**Documents affected:** `bible/06_LINEAGES.md` (rewritten as index),
`bible/07_ONBOARDING_AND_FOUNDER.md` (rewritten Result section +
prologue expansion), new `bible/13_KOVARI_CUBE.md`, new
`bible/14-21_LINEAGES_*.md`, `docs/_archive/ravatha-source-2026-08-04/`.

**Code affected:** `game/scripts/Domain/FounderNarrativeCatalog.cs`,
`FounderNarrativeScorer.cs`, `FounderNarrativeSession.cs`,
`FounderNarrativeModels.cs`, `HeroCreationRequest.cs`,
`HeroCreationResult.cs`, `CitizenProfile.cs`. Migration via
`WorldSave` schema bump — see `bible/13_KOVARI_CUBE.md` § *Migración
y fallback*.

---

## Infrastructure decisions

These concern the agent architecture itself, not game design.

### DEC-I001: `.agents/` is the canonical agent-context root

**Status:** Accepted
**Date:** 2026-07-29

**Decision:**
Canonical skills live in `.agents/skills/<id>/SKILL.md` and canonical agent
definitions in `.agents/agents/<id>/AGENT.md`. Tool-specific directories
(`.claude/`, `.codex/`) contain generated or mirrored copies only.

**Reason:**
The repository already used `.agents/skills/` as the canonical root with
mirrors in `.claude/skills/`, established by `Install-GodotDotNetSkills.ps1`
and tracked by `skills-lock.json`. Introducing a second root (`.ai/`) would
have created two mechanisms to synchronize.

**Consequences:**
Edit canonical files only, then run `scripts/Sync-AgentContext.ps1`.
Never hand-edit files under `.claude/agents/`, `.claude/skills/`, or
`.codex/skills/`.

---

### DEC-I002: Codex agent adapters are delivered as Codex skills

**Status:** Accepted
**Date:** 2026-07-29

**Decision:**
Agent personas are exposed to Codex as skills at
`.codex/skills/agent-<id>/SKILL.md`, prefixed with `agent-`.

**Reason:**
Codex CLI 0.145.0 has no sub-agent concept and no `.codex/agents/` directory.
It does discover project-level skills: verified empirically by placing a probe
skill at `.codex/skills/<name>/SKILL.md` and confirming Codex listed it. The
`agent-` prefix prevents collision with the domain skills of the same name.

**Consequences:**
If Codex later ships a native agent format, add a generator branch to
`scripts/Sync-AgentContext.ps1`; the canonical definitions do not change.

---

### DEC-I003: Mirroring is copy-based, not symlink-based

**Status:** Accepted
**Date:** 2026-07-29

**Decision:**
`scripts/Sync-AgentContext.ps1` copies content by default. Symlinks are opt-in
via `-UseSymlinks`.

**Reason:**
`git config core.symlinks` is `false` in this environment and the existing
`.claude/skills/*` entries are tracked in git as regular files (mode `100644`),
not symlinks (`120000`). The committed, portable form is therefore copies. The
symlinks present in this working tree are a local convenience that a fresh
clone would not reproduce.

**Consequences:**
Mirrors are committed. `scripts/Validate-AgentContext.ps1` verifies they match
the canonical source and fails if they have drifted.
