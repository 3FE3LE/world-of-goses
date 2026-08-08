# Agent dispatch

> Inference layer between natural-language requests and the agent / skill
> catalogue. **Use this when the user describes a problem without naming an
> agent or skill.** Loaded automatically by Claude Code and Codex because
> the root `AGENTS.md` / `CLAUDE.md` reference it.
>
> This is not a replacement for [`CONTEXT_MAP.md`](CONTEXT_MAP.md). Use
> `CONTEXT_MAP.md` to find the canonical docs and code; use this file to
> pick the right agent from a free-form prompt.
>
> Routing contract: every agent lists its **primary** and **conditional**
> skills under `.agents/agents/<id>/AGENT.md`. The local capability
> adapter layer (godot-dotnet, godot-presentation, godot-persistence,
> dotnet-testing, dotnet-diagnostics, repo-navigation) is loaded
> conditionally and never duplicates a project-domain skill. Engine API
> specifics are delegated to the verified upstream provider installed by
> `Install-GodotDotNetSkills.ps1`; see
> [`SKILL_MIGRATION.md`](SKILL_MIGRATION.md).

---

## 1. How to read this file

1. Find the row in §2 or §3 that best matches the request.
2. Read the row's **Primary agent** and **Required skills**.
3. If the row's **Cross-domain** column is non-empty, also load those
   skills.
4. Open [`CONTEXT_MAP.md`](CONTEXT_MAP.md) for the matching route to get
   the canonical docs and code.

If several rows match, pick the **most specific** one. If none matches,
use `gameplay-integrator` and add the missing row to this file.

---

## 2. Direct keyword dispatch

When the prompt contains these words or close synonyms, route accordingly.
Words are listed in both Spanish and English because the project docs are
in English but conversations often happen in Spanish.

### `citizens-rpg`

| Español | English |
| --- | --- |
| ciudadano, ciudadana, habitantes, héroes, heroína | citizen, citizens, inhabitant, hero, heroine |
| herida, herido, recuperación, convalecencia | injury, wound, recovery, convalescence |
| compromiso, disponibilidad, asignación, rol | commitment, availability, assignment, role |
| competencia, aptitud, experiencia, perfil | competency, aptitude, experience, profile |
| reclutamiento, migrante | recruitment, migrant |
| cansancio, fatiga (estamina) | stamina, fatigue |

### `city-simulation`

| Español | English |
| --- | --- |
| edificio, construcción, obra, plano | building, construction, project, plan, blueprint |
| producción, rendimiento, parar, paro, parada | production, output, stop, halt, blocker |
| consumo, almacén, inventario, reserva | consumption, storage, inventory, reserve, stockpile |
| upkeep, mantenimiento, presión | upkeep, maintenance, pressure, drain |
| granja, cantera, refugio, horno, taller | farm, quarry, shelter, smithy, workshop |
| parcela, solar, ruta, crecimiento | parcel, plot, route, growth |

### `expeditions-territory`

| Español | English |
| --- | --- |
| expedición, exploración, equipo, partida | expedition, exploration, team, dispatch |
| encuentro, combate, retirada, emboscada | encounter, combat, retreat, ambush |
| botín, carga,损失 (perdida), baja, herido de expedición | loot, cargo, loss, casualty, expedition injury |
| territorio, parcela, ruta, desbloqueo | territory, parcel, route, unlock |

### `narrative-lore`

| Español | English |
| --- | --- |
| diálogo, texto, copy, narración, lore | dialogue, copy, narration, lore, voice |
| chronicle, evento, fundacional, fundador | chronicle, event, foundational, founder |
| nombre, descripción, voz, tono | name, description, voice, tone |
| Rabata, Burgoses, linaje, cultura | Rabata, Burgoses, lineage, culture |

### `lineages-and-cultures` (cross-cutting, no owning agent)

| Español | English |
| --- | --- |
| linaje, cultura, afinidad, Ardhen, Eirune, Kovari, Myrven, Vaelun, Orveth, Caelith, Theryn | lineage, culture, affinity, eight lineages |

### `technical-foundation`

| Español | English |
| --- | --- |
| guardado, persistencia, save, snapshot, schema, versión, migración | save, persistence, snapshot, schema, version, migration |
| offline, catch-up, simulación, determinismo | offline, catch-up, simulation, determinism |
| rendimiento, frame, memoria, perfilado | performance, frame, memory, profiling |
| tests, xUnit, regresión | tests, xUnit, regression |
| arquitectura, capa, dominio, presentación | architecture, layer, domain, presentation |
| Godot, escena, nodo, .NET, C# | Godot, scene, node, .NET, C# |

### `presentation-experience`

| Español | English |
| --- | --- |
| UI, panel, botón, tooltip, HUD | UI, panel, button, tooltip, HUD |
| pixel art, sprite, animación, atlas | pixel art, sprite, animation, atlas |
| audio, música, sonido, bus | audio, music, sound, bus |
| tipografía, icono, fuente | typography, icon, font |
| accesibilidad, color | accessibility, color |
| cámara, movimiento, perspectiva | camera, motion, perspective |

### `quality-guardian`

| Español | English |
| --- | --- |
| revisar, revisión,评审, evaluador | review, reviewer, audit, regression |
| ¿esto respeta las invariantes? | does this respect the invariants? |
| ¿se debilita la identidad del juego? | does this weaken the game identity? |

### `gameplay-integrator`

| Español | English |
| --- | --- |
| cruzar, varios, juntos, decidir, integrar | cross, multiple, together, integrate |
| afectar dos o más pilares | affects two or more pillars |
| no sé qué agente usar | I don't know which agent to use |
| ¿qué agente debería...? | which agent should…? |

---

## 3. Symptom-based dispatch

When the user describes a **symptom** instead of a system, infer the agent
from the symptom.

| Symptom | Likely agent | Why |
| --- | --- | --- |
| "A citizen is doing two things at once" | `citizens-rpg` | exclusivity is owned by the citizen commitment model |
| "Production stops but the panel doesn't say why" | `city-simulation` | stop causes live with the production system |
| "Resources vanish on expedition return" | `expeditions-territory` + `citizens-rpg` | survival consequences cross both |
| "I cannot save and reload and get the same world" | `technical-foundation` | determinism + persistence |
| "The art direction looks too generic for our game" | `presentation-experience` | pixel art and identity are theirs |
| "Two lineages feel like the same thing" | `lineages-and-cultures` | cross-cutting cultural identity |
| "Founder is overpowered / mandatory" | `narrative-lore` + `citizens-rpg` | founder narrative vs founder entity |
| "I added a new building and the city becomes a generic builder" | `city-simulation` + `quality-guardian` | drift toward generic builder |
| "Auto-save corrupts after closing the window" | `technical-foundation` | persistence atomicity |
| "The chronicle shows the same line ten times" | `narrative-lore` + `technical-foundation` | event retention |
| "Build is clean but tests are flaky" | `technical-foundation` | test infrastructure |
| "A new feature feels like a minigame" | `gameplay-integrator` | identity drift, cross-domain |
| "Citizens are interchangeable numbers" | `citizens-rpg` + `quality-guardian` | identity erosion |
| "The UI changes layout per lineage" | `presentation-experience` + `lineages-and-cultures` | shared UI invariant |

---

## 4. Cross-domain signals

These phrases mean **more than one agent** is in play. The first row is
always the primary; the rest are consultants.

| Phrase | Primary | Consultants |
| --- | --- | --- |
| "a wound changes how the city can produce" | `citizens-rpg` | `city-simulation`, `technical-foundation` |
| "an expedition unlocks a building" | `expeditions-territory` | `city-simulation`, `citizens-rpg`, `technical-foundation` |
| "a chronicle event changes the save format" | `narrative-lore` | `technical-foundation` |
| "a per-lineage UI bonus" | `presentation-experience` | `lineages-and-cultures`, `citizens-rpg` |
| "the founder affects production for years" | `narrative-lore` | `citizens-rpg`, `city-simulation` |
| "a new save version requires migrating chronicles" | `technical-foundation` | `narrative-lore` |

---

## 5. Self-check questions for each agent

When you (the agent) read a prompt, ask yourself these questions before
acting. If you answer **yes** to one of another agent's questions, hand
the task over.

### `citizens-rpg` — am I the right agent?

- Does the request mention a person, citizen, hero, or wound?
- Does it touch `Citizen`, `Role`, `CompetencyEntry`, `CitizenVitalStatus`,
  `CitizenCommitment`, `CitizenAssignmentService`?
- Does it ask "can a citizen do X and Y at the same time?"

If yes, I'm in. If the request is also about city buildings, expeditions,
or save format, also consult `city-simulation`, `expeditions-territory`,
or `technical-foundation`.

### `city-simulation` — am I the right agent?

- Does the request mention a building, recipe, parcel, or production?
- Does it touch `Building`, `Recipes`, `CityInventory`,
  `CityResourceLedger`, `Construction*`, `NaturalResourcePatch`?
- Does it ask "why is production paused" or "the building is not doing
  anything"?

If yes, I'm in. If the request is about a citizen's personal consequences
or about persistence, also consult the appropriate agent.

### `expeditions-territory` — am I the right agent?

- Does the request mention an expedition, team, encounter, parcel unlock,
  or territory?
- Does it touch `Expedition*`, `CityParcel`, `ParcelGrid`?
- Does it ask about retreat posture, return, or loadout?

If yes, I'm in. **Mandatory:** also consult `citizens-rpg`, `city-simulation`,
and `technical-foundation`.

### `narrative-lore` — am I the right agent?

- Does the request ask for a name, dialogue, voice, tone, event text, or
  chronicle entry?
- Does it touch `Dialogue`, `FounderNarrative*`, `WorldEventLog`,
  `game/locale/`?

If yes, I'm in. I may **not** invent mechanics or bonuses. If the change
implies a mechanical effect, route to the owning domain agent.

### `lineages-and-cultures` — am I relevant?

- Does the request mention any of the eight lineages?
- Does it propose a per-lineage UI, audio, sprite, or mechanical variation?
- Does it touch `LineageDefinition`, `LineageThemeRegistry`,
  `CharacterVisualRegistry`?

If yes, I am **consulted** by whichever domain owns the change. There is
no lineage agent.

### `technical-foundation` — am I the right agent?

- Does the request mention a save, schema version, migration, offline
  catch-up, determinism, performance, or tests?
- Does it touch any file under `game/scripts/Domain/`?
- Does it change the domain/presentation boundary?

If yes, I'm in. For persistence changes, treat the schema version bump
and the round-trip test as part of the change.

### `presentation-experience` — am I the right agent?

- Does the request mention a scene, UI panel, sprite, animation, audio,
  icon, font, accessibility, or feedback?
- Does it touch `game/scenes/`, `game/assets/`, `game/scripts/Ui/`,
  `game/scripts/visual/`?

If yes, I'm in. I render state; I do not decide rules.

### `quality-guardian` — am I the right agent?

- Has the change already been implemented and needs review?
- Is the question about identity erosion, invariant violations, or
  regression?

If yes, I'm in. I am **read-only** by frontmatter. I do not implement
the change I review.

### `gameplay-integrator` — am I the right agent?

- Does the request touch two or more pillars?
- Does it change progression, the active slice, or a foundational decision?
- Is the owning agent unclear?

If yes, I'm in. I coordinate; I do not own a domain.

---

## 6. Anti-patterns in dispatch

Things to avoid when interpreting a prompt:

- **Do not route by the name of a single class.** "I changed `Citizen.cs`"
  is still a city task if the change was about how citizens interact with
  construction. Route by **intent**, not by file name.
- **Do not skip the cross-domain check.** A request about a citizen
  building a building is still two domains. Load both skills.
- **Do not invent routes.** If no row in §2 or §3 matches, use
  `gameplay-integrator` and add the missing pattern to this file as part
  of the change.
- **Do not assume language.** The project docs are in English but the
  conversation may be in Spanish. Look at §2 and §3 for both.
- **Do not promote an agent from "consultant" to "writer" without
  justification.** A consultant's job is to review; only the primary
  agent writes. The single-writer rule in
  [`AGENT_COLLABORATION_PROTOCOL.md`](AGENT_COLLABORATION_PROTOCOL.md)
  applies.

---

## 7. Phrasings the user might use and the inferred agent

Examples (not exhaustive). These are the kind of messages you might see
in a chat without the user naming an agent.

- *"the quarry workers are tired and the production stopped"* →
  `city-simulation` (production stop cause) and `citizens-rpg` (stamina /
  commitment).
- *"after a long expedition the survivor came back wounded and there is
  no one to treat them"* → `citizens-rpg` (wound model) and
  `city-simulation` (Shelter recovery) and `expeditions-territory`
  (return consequence).
- *"we added a new building and now the city feels like a regular
  builder"* → `city-simulation` and `quality-guardian`.
- *"the player cannot tell why production is paused"* →
  `presentation-experience` (missing UI) and `city-simulation` (stop
  cause).
- *"this feature feels like a minigame we bolted on"* → `gameplay-integrator`.
- *"we want each lineage to feel mechanically distinct in combat"* →
  refuse via `lineages-and-cultures` + `quality-guardian` (lineages must
  not become classes; see DEC-0002).
- *"the founder feels mandatory and we cannot lose them"* →
  `citizens-rpg` and `narrative-lore` (founder must not be eternal; see
  bible/07).
- *"the save file is huge and slow to load"* → `technical-foundation`.

---

## 8. When none of this is enough

Stop and ask the user when:

- Two rows match and you cannot tell which is more specific.
- A request would change a decision in
  [`DECISION_LOG.md`](DECISION_LOG.md) (e.g. the wound / stamina
  question, DEC-0011).
- A request would invalidate saves without a migration strategy.
- A request would remove or replace a central system.

The full escalation rules are in
[`AGENT_COLLABORATION_PROTOCOL.md`](AGENT_COLLABORATION_PROTOCOL.md) §8.