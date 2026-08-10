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

## DEC-0014: First night authored — fire spirit dialogue, expedition motive, post-dawn separation

**Status:** Accepted
**Date:** 2026-08-06

**Decision:**
The post-manifestation period (`docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`)
is a bounded, authored sequence running from the founder's arrival at
tick 0 (`00:00`) to dawn. Concretely:

1. **Six main-dialogue nodes carry the spirit's voice.** Each node has
   one body variant per `LineageId` (eight × six = 48 keys), so the
   spirit reacts without branching the route or exposing internal
   labels. The route itself is strictly linear: `Choices` is empty
   and `Next` is `null` on every node, and the only advance is
   `CityWorld.TryCloseFirstNightDialogue`.

2. **`DialogueRunner.RunAsync` stays untouched.** The first NPC slice
   (backlog H-31) will add `JsonDialogueRunner`. The first night
   persists `FirstNightState.CurrentDialogueNodeId` instead, because a
   coroutine holding its position across `await` cannot survive a
   save/restore without breaking invariant 13 of the doc.

3. **`OverlayLayers.Tutorial = 50` is the night's surface.** The
   slot was reserved by deleting `TutorialOverlay` on 2026-08-05 and
   is documented as the authored-guidance layer. The non-modal strip
   `FirstNightDialogueStrip` lives there with `MouseFilter.Stop` only
   on the bottom strip — clicks outside the strip fall through to
   the world.

4. **Quantities are never baked into body keys.** Every visible
   number (the campfire's branches and stone, the shelter's branches
   and fibre) is interpolated at runtime from
   `FoundingSiteRules.InputsFor(module)`, so a recipe change cannot
   leave the night describing a world that no longer exists. The
   `FirstNightDialogueNoLiteralDigitsTests` guard enforces this.

5. **The Bedroll gains its first mechanical meaning.** A founder with
   no Bedroll (or `Home`) cannot fall asleep at `OtherLightTold` —
   `HasRestingPlace()` is the gate. The Bedroll stops being only
   cost/work and starts being "where sleep is possible".

6. **`SpiritTrailSearch` is the post-dawn expedition motive.** *(Its
   Food-funded Wood-return implementation is superseded by DEC-0020; this
   paragraph remains historical context.)* A
   new `ResourceOpportunityKind` value carries the same return curve
   as `FallenWoodSearch` but rewards `Wood` (fire-blackened remnants).
   The button surfaces in the expedition panel only after the
   `WorldEventKind.SpiritDeparted` event lands in the log. No schema
   bump — the kind serialises as a string and `Enum.TryParse`
   tolerates the new value in legacy saves.

7. **The three levels of post-dawn guidance stay separated.** The
   first night is authored and finite. After dawn:
   (a) **Derived directives** explain needs, causes and impediments
       from the real city state — never from a static list that can
       drift out of step.
   (b) **The Camino** is a read-only conceptual map of the
       settlement's progression; it grants no rewards and creates no
       arbitrary unlocks.
   (c) **The first night itself is not repeated** and never appears
       in the Camino or the directives. Concretely: no "follow-up
       mission", no modal "tutorial replay", no list of steps the
       player can check off.

**Reason:**
A playtest of the opening (`TO_DO.md` §3 2026-08-05 entry) showed the
previous three-card modal tutorial lying about recipe costs and
requiring an axe the player could not yet obtain. The proposal
section §6 forbids a permanent mission list and §442 (see
`EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`) forbids a chain of
modal tutorials. The first-night sequence is the only shape that
explains the cause (why these resources matter) without breaking
either ban. The `SpiritTrailSearch` is the same shape: a real
expedition with a real reward, gated on a real event, that exists
because the night produced it.

**Affected domains:** narrative, presentation, citizens, city, expeditions,
persistence.

**Consequences:**

- `Domain/FirstNightState.cs`, `FirstNightStage.cs`, `FirstNightRules.cs`
  already exist as the persistent seam; this decision adds the
  authored content on top, not a parallel state machine.
- `Domain/FireSpiritDialogueCatalog.cs` becomes the only source of
  body keys for the night; `FounderNarrativeCatalog` patterns are
  reused, `DialogueRunner.RunAsync` is not.
- The Expedition panel exposes a third objective button
  (`SpiritButton`) which is hidden by default and only appears when
  `ExpeditionPlanningSnapshot.SpiritTrailUnlocked == true`.
- `WorldEventKind.SpiritDeparted` is a new enum value, added to
  `WorldEventRetention.IsSignificant` so it survives save/load and
  drives the embers primitive in `FirstNightScene`.
- `docs/CURRENT_STATUS.md` and
  `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` now describe
  the opening as entering through the first night, not as a
  separate recipe-and-resources tutorial.

**Documents affected:**
`docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md` (Propuesta → Aceptada),
`docs/CURRENT_STATUS.md`, `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`,
`docs/ai/CROSS_DOMAIN_INVARIANTS.md` (first-night invariants block),
`docs/ai/CONTEXT_MAP.md` (first-night route + tools placeholder),
`docs/VISUAL_REGRESSION.md` (four new fixtures).

**Code affected:**
`game/scripts/Domain/FireSpiritDialogueCatalog.cs`,
`game/scripts/FirstNightDialogueStrip.cs`, `game/scripts/FireSpiritVisual.cs`,
`game/scripts/FirstNightEmbers.cs`, `game/scripts/FirstNightScene.cs`,
`game/scripts/FirstNightContextCommentary.cs`,
`game/scripts/CityWorldController.cs` (`FirstNightStageChanged` signal +
`TryOpenFirstNightDialogue` / `TryCloseFirstNightDialogue` wrappers),
`game/scripts/IconPaths.cs` (`Fire`),
`game/scripts/ExpeditionPanel.cs` (`SpiritTrailObjectiveButtonPath`,
third `ConfigureObjectiveButton`),
`game/scripts/ExpeditionPlanningSnapshot.cs` (`SpiritTrailUnlocked`),
`game/scenes/Components/ExpeditionPanel.tscn` (`SpiritButton`),
`game/locale/en.po` + `game/locale/es.po` (48 body + 4 context + 2
button + 1 tooltip keys), `game/assets/ui/icons/24/fire.svg` (asset
promotion).

---

## DEC-0015: Slate is the neutral UI surface; the warm accent is for state only

**Status:** Accepted
**Date:** 2026-08-06

**Decision:**
The project's neutral chrome — buttons, panels, input fields, the status strip
— is the **dark slate** 9-slice promoted from Kenney's CC0 *UI Pack – Pixel
Adventure*. Gold and warm tones are reserved for **state**: the focus ring,
the elevated-panel border, the stabilised fragment pips. Green keeps only its
success semantic (`ProgressBar/fill`) and red only its destructive one
(`ButtonWarning`).

**Why this needed a decision at all:**
The yellow surface that governed almost every button was never chosen. It was
the sum of two defaults: `ButtonText` — the variation ~80 % of buttons use —
was mapped to `kenney/9-slice/yellow.tres`, and
`LineageThemeRegistry.DefaultPanelStyleboxPath` pointed at the same file while
the active lineage started as `"default"`, an id that was **not a key** of the
lineage dictionary. Every panel built before a hero existed therefore resolved
through that fallback, and since most consumers apply the stylebox once in
`_Ready` and never refresh, they stayed yellow for the whole session. No
`DEC-` had ever recorded it, so nothing flagged it as a choice.

**Consequences:**
- `"default"` resolves explicitly to the neutral surface and is deliberately
  **not** an entry in `StyleboxByLineage`: it is not a lineage, and
  `AvailableLineages` must keep returning exactly eight.
- Button text is cream on the dark surface and near-black on the light one.
  Layout metrics are untouched — `content_margin` stays 16/4 — because the
  bible allows a re-skin to change palette, borders and fills but **not**
  minimum sizes, hierarchy or semantics.
- Actions are chosen by role through `Ui/ActionButton.cs`
  (`PrimaryActionButton`, `SecondaryActionButton`, `DangerActionButton`), so a
  future re-skin stays one edit to the theme rather than an audit of every
  call site.
- Per-lineage skins still re-palette on top of this; the neutral surface is
  what shows when no lineage applies, not a replacement for them.

---

## DEC-0016: The fire spirit speaks from a balloon in the world

**Status:** Accepted
**Date:** 2026-08-06
**Supersedes:** `DEC-0014` §3 (the non-modal bottom strip)

**Decision:**
The first night's dialogue is a **speech balloon anchored over the spirit**,
not a band at the bottom of the screen. The whole balloon is the confirm
affordance — clicking it advances — so there is no separate button.
`FirstNightDialogueStrip` is removed. `OverlayLayers.Tutorial = 50` remains the
night's layer, and clicks outside the balloon still fall through to the world.

**Presentation amendment (2026-08-08):** the balloon, spirit and embers now use
`OverlayLayers.WorldDialogue`, a diegetic slot above the ambient tint and below
the persistent HUD. Proposal-06 added permanent city and expedition rails after
this decision; playtesting showed the old tutorial layer let world dialogue
cover those management surfaces. Modals still outrank and hide the night, while
clicks outside the balloon continue to reach the world.

**Why the earlier decision did not survive contact:**
The strip was specified before anything rendered it. The whole sequence was
inert behind a mis-resolved `NodePath` (`CityPrototype` passed
`"CityWorldController"` for a node that is a *sibling*), so `DEC-0014` §3 was
never observed running. When it finally rendered it showed three problems the
spec could not have anticipated: it inherited the yellow panel fallback and
printed cream text on yellow, its band sat on the viewport's bottom edge, and
the words had no visible speaker — the player read a caption bar while the
character who was teaching them stood elsewhere on screen.

**Consequences:**
- The balloon follows the spirit every frame. `MacroStreetLiveView` projects
  its streets by hand rather than through a camera transform, so the anchor is
  re-derived in `_Process`; re-parenting would not help.
- The night's surfaces hide whenever the player is not on the macro view and
  sit below persistent HUD/modal chrome while remaining above the ambient tint.
- The confirm hint (`Continue` / `Give in to sleep`) is a quiet label inside
  the balloon, keeping the existing `firstnight.*` catalogue keys.
- The fire spirit itself is still a placeholder, now shaped as a flame rather
  than a ring and glyph. **None of the three Kenney packs ships a
  free-standing flame** — the nearest art is a hearth or brazier carrying its
  own stonework — and cropping one would mean hand-editing an exported PNG,
  which `docs/ART_PIPELINE.md` §10 forbids. Real spirit art is still owed.

---

## DEC-0017: The city's ground is the site the founder fell on

**Status:** Accepted
**Date:** 2026-08-06

**Decision:**
Each city draws its ground from a **biome**, and which biome is keyed to the
founder's lineage. Eight biomes, one per lineage, defined in
`Ui/TerrainAtlas.GroundBiome` as a short list of seam-free fill tiles plus the
tile a trodden path wears down to.

**This is presentation only.** No resource, yield, recipe, rate or rule differs
by biome. A lineage still confers no advantage, so `DEC-0002` — lineages are not
classes and not destiny — holds. The framing matters: the biome is not a trait
of the lineage, it is **the place the astral fall deposited the founder**. The
land does not change because of who founded it; the founder arrived somewhere.
The standing rule in the macro view still applies verbatim: *terrain art must
never become simulation state*, and nothing here is persisted.

**Why it needed a decision:**
`docs/CURRENT_STATUS.md` §8 and `docs/VALIDATION.md` both listed *"multiple
biomes"* as explicitly outside the current slice. Eight of them is exactly that,
so the deferral is lifted deliberately rather than by drift. It also reopens a
signed visual decision: `docs/VISUAL_REGRESSION.md` records that two ground
palettes were rejected — one for excessive saturation, one as "too
architectural" — before the current olive-ground was accepted.

**Consequences:**
- The ground stopped cycling `street % 3` across Grass/Dirt/Stone, which is what
  made it read as arbitrary bands.
- Per-tile variation is a spatial hash over the biome's fill list. The previous
  expression degenerated for three variants and produced flat horizontal
  stripes; variant selection now lives in `TerrainAtlas.VariantIndex`.
- **Flower-strewn ground was investigated and rejected for now.** The pack's
  flower tiles belong to autotile sets and each carries a corner of the
  neighbouring material, so repeating one across a band shows the cut. Scatter
  needs transparent-background props, not fills.

---

## DEC-0018: The Cube decides the physical expression, and weapons are learned in three tiers

**Status:** Accepted
**Date:** 2026-08-07

**Decision:**
`PhysicalExpression` derives from the **highest face of the persisted
`CubeProfile`**, not from the elemental affinity. The affinity keeps coming from
the onboarding's elemental signal and the two are independent axes.

Weapon competency experience is acquired at one of three rates:

| Tier | Families | XP efficiency |
|---|---|---|
| Natural | the two of the citizen's own expression | `100 %` |
| Lineage-familiar | the four of the other two expressions their lineage's vertex reaches | `50 %` |
| Foreign | the remaining six | `10 %` |

The tier scales **experience acquisition only**. It never touches damage,
accuracy, `PhysicalTransfer`, `ElementalResonance`, cooldown, defence or
technique coefficients.

**Why it needed a decision:**
Two separate reasons.

The derivation was a *correction*, not a choice: `22_…` §2.4 publishes one table
with three columns — face, affinity, expression — and the implementation read it
as a chain, deriving the expression from the affinity. That collapsed two
independent axes into one and left six of the thirty-six combinations the same
chapter's technique model assumes (`EXPEDITIONS_AND_COMBAT_INTEGRATION_ROADMAP.md`
§2: six expression trees × six affinity trees).

The **middle tier is new canon**. No document described it: the bible knew only
`100 %` natural and `10 %` foreign, in `22_…` §3 and the roadmap §Fase 1. The
hard rule is to never invent a rule absent from the docs, so it is recorded here
rather than slipped into a table.

**Consequences:**
- Nothing new is persisted and no migration exists. `PhysicalExpression` was
  never stored; every citizen already persists a `CubeProfile` since v30, so
  changing the derivation was sufficient and `WorldSave.CurrentVersion` stays 31.
- **Existing saves load with different expressions than before.** A Kovari+Fire
  founder read as Stunning and now reads as Fracture. Nothing on disk changed;
  the obsolete value simply no longer exists to be recomputed.
- Each lineage admits exactly three expressions and each expression belongs to
  exactly four lineages. This is not enforced by a blacklist: under `60/40` with
  the `±8` cap a favoured face stays in `52–68` and its opposite in `32–48`, so
  the highest face is always favoured. `DEC-0013` §3 is therefore load-bearing —
  widening `±8` past `10` would break the property.
- Ties are resolved by the explicit order `Body, Bond, Stability, Impulse,
  Domain, Reach`, never by enum, dictionary, insertion or load order.
- **A citizen with no onboarding sits on the bare vertex `60/60/60`, which is a
  three-way tie.** Every non-founder of a Body lineage is therefore Fracture and
  every one of a Bond lineage is Poisoning: two of the six expressions in play.
  Accepted knowingly and tracked as `M-29`; varying an ordinary citizen's cube is
  a separate decision about citizen generation. **`M-29` is resolved by
  `DEC-0019` (2026-08-07).**
- `DEC-0002` still holds: the lineage confers no advantage. The tier is a
  qualitative *learning context* — what your people know — and never a
  production or combat bonus.

---

## DEC-0019: Ordinary citizens get a deterministic cube seeded from their id

**Status:** Accepted
**Date:** 2026-08-07

**Decision:**

`CubeScoring.GenerateOrdinaryProfile(LineageId lineage, int seed)` produces
the cube of every non-founder citizen as a pure function of `(lineage, seed)`.
The lineage vertex is shifted by `±8` per axis using FNV-1a — the same
stable hash the repo already uses for layout and appearance seeds (see
`NaturalResourceLayoutPlanner.StableScore`, `CityWorld.StableAppearanceSeed`)
— and applied through `ApplyContribution`, so the existing `±8` clamp and
pair invariant are the same code path the onboarding already exercises.
The cube and its seeded `name → lineage` cycle live together: the name index
deliberately uses a different mix of the seed
(`MigrantNames[(seed * 11 + 3) % 8]`) so two migrants of one lineage are
statistically distinct people, not statistical copies.

The third cube face used to be `Mastery` everywhere — on disk, in the
DTOs and in the JSON key — and that name was about to collide with the
weapon-family mastery tiers (see `WeaponLearningAffinity`, `WeaponLearning`).
`DEC-0013` §2 already named the face `Dominio`; the rename removes the
alias, frees the canonical mechanical name for the cube, and reserves the
English form for the upcoming skill-tier system. The on-disk field
`Mastery` is preserved one schema bump as a nullable bridge
(`FounderCubeProfileSave.Mastery`) so a v31 save loads without losing its
founder's cube; `MigrateV31ToV32` copies the legacy value into the new
`Domain` field and clears the bridge, and new code never writes it.

**Reason:**

The M-29 gap named in `DEC-0018` only became visible after the
expression derivation was corrected: every Body lineage read Fracture and
every Bond lineage read Poisoning because their `±0` vertex was always a
three-way tie. Two migrants of the same lineage were statistically the
same person even before that. A `±8` shift keyed off the citizen id is
the smallest move that produces six reachable expressions, stays inside
the same envelope the onboarding already commits to, and adds no
production or combat consequence. The on-disk rename exists to prevent
the inevitable collision between the cube's third face and the
weapon-family mastery tier before either side acquires readers.

**Affected domains:** citizens, combat, persistence, narrative,
presentation.

**Consequences:**

- `WorldSave.CurrentVersion` rises to `32`. `MigrateV31ToV32` walks every
  citizen, copies `cube.Mastery` into `cube.Domain` and nulls the bridge.
- `CubeScoring.MasteryValueId` becomes `CubeScoring.DomainValueId`; the
  switch arm `"mastery"` becomes `"domain"`; `WithMastery` becomes
  `WithDomain`. The founder narrative catalog and every `Recalculate` /
  `ApplyContribution` test fixture follow.
- A pre-v32 save keeps the founder's cube exactly: `MigrateV31ToV32` is
  lossless because the bridge carries the legacy value through
  deserialization.
- An existing city keeps its population uniform: the v29→v30 path
  already fills missing cubes with the lineage vertex, and the
  generation code only runs for *new* migrants. Migrated saves see
  variation only for citizens who arrived after the rename — the same
  conservative posture `M-29`'s note already declared.
- The English face name `Mastery` is reserved for the weapon-family
  mastery tier; until that tier lands, no field or document in the
  domain may use it.

**Documents affected:** `docs/world-of-goses-design-bible/13_KOVARI_CUBE.md`
§ *Estrategia y fallback*; `docs/world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md`
snippet for the canonical face list; `docs/world-of-goses-design-bible/16_LINEAGES_KOVARI.md`
weapons-per-vertex paragraph; `docs/ai/DECISION_LOG.md`; `docs/CURRENT_STATUS.md`;
`TO_DO.md` (closes `M-29`).

**Code affected:** `game/scripts/Domain/CubeScoring.cs`,
`FounderCubeProfile.cs`, `FounderNarrativeCatalog.cs`,
`game/scripts/Domain/Persistence/FounderCubeProfileSave.cs` (adds the
bridge), `game/scripts/Domain/Persistence/WorldPersistence.cs`
(`MigrateV31ToV32`, four legacy capture/restore sites), `game/scripts/Ui/CitizenNatureText.cs`,
`game/scripts/Ui/FounderCardPanel.cs`, `game/scripts/HeroProfileView.cs`,
`game/locale/en.po` (msgstr `"Mastery"` → `"Domain"`).

---

## DEC-0020: The first visual combat belongs in the opening Spirit Trail

**Status:** Accepted
**Date:** 2026-08-10
**Supersedes:** `DEC-0014` §6 only where it defines `SpiritTrailSearch` as a
Food-funded Wood reward; the authored-night, `SpiritDeparted` gate and
post-dawn separation remain accepted.

**Decision:**

The first visual combat must appear within approximately the first five minutes
of gameplay in this end-to-end flow:

```text
Astral onboarding → first night → dawn / SpiritDeparted
→ Spirit Trail available → Founder's first expedition
→ first visual encounter → continuation to the objective → city return
```

The integration is constrained as follows:

1. Expeditions remain automatic. There is no manual movement control.
2. Presentation is lateral and structurally inspired by *Taskbar Hero*, without
   copying assets, UI, characters or content.
3. There is one world clock. City, travel and combat advance in parallel. The
   world cannot be paused; its only current global speeds are 1x / 2x / 4x.
   Entering or leaving `ExpeditionLiveView` never changes speed.
4. Basic Attack is automatic. The future vanguard ceiling is four `Citizen`.
   The first expedition uses only the Founder; slots 2–4 stay visible and
   locked.
5. Four octagonal Active Skill slots are visible. Only Skill 1 is initially
   connected, through `expedition_skill_1`; the other planned actions are
   `expedition_skill_2`, `expedition_skill_3` and `expedition_skill_4`.
   Each octagon must preserve eight sides capable of hosting a future Trait.
6. A ranged combatant does not kite. Combatants advance only to enter
   `AttackRange` and do not retreat voluntarily after they can attack.
   Knockback may displace them; `Stability` reduces displacement and `Impulse`
   may increase the displacement produced.
7. The first Spirit Trail lasts approximately four world hours. That duration
   does not consume Food merely because the expedition exists.
   `SpiritTrailSearch` follows the spirit and advances the opening narrative;
   it no longer means `1 Food → Wood`. Its definitive material reward is open.
8. Traits, Chains, carriage, `SPACE`, advanced formation and functional Skill
   slots 2–4 remain explicitly out of scope for this increment.

**Reason:**

The existing combat domain and the existing early-game expedition are separate
implementation islands. Waiting for the former EG-5 consolidation and EG-6
signature before integrating them leaves the opening without its first visible
RPG consequence. The new priority validates the project's RPG-city-builder-idle
identity early: the player sees the Founder leave, fight automatically while
the city keeps living, continue toward a narrative objective, and return.

**Affected domains:** expeditions, citizens, city, narrative, presentation,
simulation, input.

**Consequences:**

- The earlier rule that blocked combat depth until EG-5/EG-6 closed is
  reanalysed and superseded. The exception is narrow: only the end-to-end first
  visual encounter moves forward; broad combat depth remains deferred.
- The current `SpiritTrailSearch` implementation (180 ticks, 1 Food, Wood
  4/6/8) is now documented technical debt, not product canon.
- The current 1–2-member `Expedition` and the debug-only combat run remain facts
  of the HEAD, not the target team contract. No schema or gameplay changes are
  made by this documentation increment.
- Implementation must land a thin end-to-end vertical before adding Traits,
  Chains, carriage, `SPACE`, advanced formation, more active skills, procedural
  routes or a wider combat roster.

**Documents affected:** bible/05, bible/10,
`docs/EXPEDITIONS_AND_COMBAT_INTEGRATION_ROADMAP.md`,
`docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`,
`docs/CURRENT_STATUS.md`, `docs/PRODUCT_DIRECTION.md`,
`docs/VALIDATION.md`, `docs/UI_PATTERNS.md`,
`docs/ai/CROSS_DOMAIN_INVARIANTS.md`,
`docs/ai/CURRENT_DEVELOPMENT_STATE.md`, `TO_DO.md`.

**Code affected:** none in this increment. Named future seams include
`CityWorld`, `Expedition`, `ExpeditionRequest`, `ResourceExpeditionRules`,
`CombatEncounter`, `ExpeditionPanel`, `ExpeditionRail` and a new
`ExpeditionLiveView` presentation surface.

---

### DEC-0021: Observable combat sessions and provisional first weapon

**Status:** Accepted
**Date:** 2026-08-10

**Decision:**

The first Spirit Trail encounter is an incremental `CombatSession` owned by
`CityWorld` and keyed by `ExpeditionId`, never by `ExpeditionLiveView`. The
single world tick advances combat. Basic Attack is intrinsic and automatic;
AUTO only authorizes automatic spending of the member's one Active Skill, and
manual activation uses the same technique/target/status/cooldown/log pipeline.
Closing or reopening the view does not affect the session, speed or expedition.

Until the planned astral onboarding lets the player choose one of the two
weapons associated with their physical expression, a Founder who starts the
first Spirit Trail unarmed receives one deterministic provisional family from
the four already implemented (Spear, Staff, Mace or Orb). It is written to the
existing persistent `EquipmentLoadout`. This automatic choice is temporary and
must be replaced, not layered beside, the future onboarding choice.

**Consequences:**

- Schema v33 persists the active session's logical step and replayable
  AUTO/manual command history; deterministic replay reconstructs RNG, health,
  cooldowns and `CombatLog` through one engine.
- V32 grandfathering equips an unarmed Founder-only Spirit Trail in Outbound,
  or at an unresolved Encounter boundary, deterministically before its first
  observable encounter. A post-encounter legacy expedition preserves its
  existing loadout and outcome. A legacy Spirit
  Trail with a non-canonical team is not invalidated or rewritten: it completes
  through the previous aggregate encounter resolver.
- `ResolveToEnd` remains available but consumes incremental `Advance`.
- Observable combat is deliberately limited to the Founder-only
  `SpiritTrailSearch` in this slice. Armed reconnaissance and material sorties
  keep their prior aggregate resolver until a later explicit product increment.
- Offline catch-up steps every world tick while that session is active, preserving
  the same HP, cooldown, log and outcome as equivalent live world time.
- `expedition_skill_1` and the octagon's `Activated` signal converge on
  `TryActivateMemberSkill(0)`; slots 2–4 remain legal no-ops.
- No movement, Chains, Traits, combat clock, expedition speed or manual retreat
  is introduced.
- The provisional neutral Basic Attack coefficient split and first weapon are
  slice balance/content debt, not the final onboarding contract.

**Scope clarification (2026-08-10):** the statement above about movement records
the boundary of the observable-session increment, not a permanent prohibition on
spatial combat. The subsequent lateral-combat increment implements the already
accepted DEC-0020 rules for automatic approach, AttackRange, no kiting and
Impulse/Stability knockback. It does not introduce manual movement, a separate
clock or a combat-specific speed.

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
