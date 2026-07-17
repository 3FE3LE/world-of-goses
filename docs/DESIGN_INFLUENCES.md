# Design Influences

> An internal document. It records the project's acknowledged
> inspirations so that design decisions remain traceable, and it
> states the boundary between inspiration and original intellectual
> property. Public-facing names, art, lore, and implementations must
> remain independently created.

---

## 1. Acknowledged inspiration

The project is intentionally and visibly inspired by the systemic
fantasy of **Wakfu and the World of Twelve**, particularly its broad
class diversity, ecological themes, profession-based identity, and the
way character archetypes influence how players interact with the world.

This inspiration is intentional. It informs the breadth of the design
space, the emphasis on systemic interactions, the professional
identity of citizens, and the way environmental choices cascade
through the world.

It is not a license. The final game must remain an original
intellectual property.

## 2. What may be used as conceptual inspiration

The following are part of the project's conceptual vocabulary and may
appear in internal documents, design notes, and discussions:

- The general fantasy of a class / archetype system with broad
  diversity.
- The general fantasy of profession-based identity.
- The general fantasy of an ecological theme that interacts with
  player choices.
- The general fantasy of an environmental alignment expressed through
  accumulated actions rather than a one-time choice.
- The general fantasy of automatic expedition-style gameplay with
  configuration instead of direct control.
- The general fantasy of a single persistent settlement.

## 3. What must remain original

The following must not be copied or reproduced from the source of
inspiration, in any form, in any artifact (code, art, lore, UI,
documentation, naming, marketing, or otherwise):

- Existing class or race names.
- Character silhouettes.
- Costumes.
- Class symbols.
- Spell names.
- Exact spell kits.
- Lore.
- Religions.
- Nations.
- Locations.
- Interface designs.
- Artwork.
- Animations.
- Music.
- Dialogue.
- Exact numerical systems.

Internal documents may reference the original inspirations to
communicate design intent. Public-facing names, art, lore, and
implementations must be independently created.

## 4. The 18-lineage design space

The long-term design may contemplate approximately **18 distinct
lineages or archetypal peoples**, inspired by the breadth and clarity
of the source's class roster.

These must not be direct renamed copies.

Each lineage should be independently defined through:

- Biology or magical nature.
- Cultural tendencies.
- Professional affinities.
- Environmental relationship.
- Social organization.
- Architecture.
- Clothing.
- Animation language.
- Strengths.
- Vulnerabilities.
- Potential internal diversity.

A lineage creates tendencies and opportunities, not absolute
restrictions. Members of the same lineage may become miners, doctors,
farmers, engineers, politicians, artisans, adventurers, or any other
profession. Their lineage may affect aptitude, learning conditions,
cultural expectations, or passive interactions, but it must not
permanently lock them into one profession.

The number 18 is a target, not a commitment. The roster may evolve as
the design matures.

## 5. The three MVP lineages

For the MVP, only three original lineages are designed. Their working
inspirations are:

1. A physically driven, direct, action-oriented lineage inspired by
   the gameplay fantasy associated with the Iop archetype.
2. A biologically or magically restorative lineage inspired by the
   gameplay fantasy associated with the Eniripsa archetype.
3. A tactical, explosive, deceptive, or engineering-oriented lineage
   inspired by the gameplay fantasy associated with the Rogue /
   Roublard archetype.

These references are internal design shorthand only. The actual MVP
lineages must receive:

- Original working names.
- Original visual silhouettes.
- Original cultures.
- Original professional affinities.
- Original abilities.
- Original architecture.
- Original strengths and weaknesses.
- Original environmental relationships.

Direct parody names, slightly altered spellings, or transliterations
of the source names are not acceptable as final names.

### 5.1 Martial lineage (MVP)

**Working inspiration:** physically driven, direct, action-oriented
(Iop archetype).

**Potential tendencies:**

- Physical work.
- Construction.
- Mining.
- Security.
- Expedition leadership.
- Heavy equipment.
- Strong action-oriented culture.

**Possible weaknesses:**

- Greater resource consumption.
- Conflict escalation.
- Lower initial interest in administrative or medical specialization
  unless the city deliberately develops those areas.

### 5.2 Restorative lineage (MVP)

**Working inspiration:** biologically or magically restorative
(Eniripsa archetype).

**Potential tendencies:**

- Healthcare.
- Agriculture.
- Biological research.
- Public sanitation.
- Rehabilitation.
- Environmental restoration.
- Support roles during expeditions.

**Possible weaknesses:**

- Higher demand for specialized ingredients.
- Greater personnel investment in long-term care.
- Lower immediate military or industrial output unless deliberately
  developed.

### 5.3 Tactical-engineering lineage (MVP)

**Working inspiration:** tactical, explosive, deceptive, or
engineering-oriented (Rogue / Roublard archetype).

**Potential tendencies:**

- Engineering.
- Traps.
- Logistics.
- Demolition.
- Manufacturing.
- Automation.
- Tactical expedition roles.
- Rapid infrastructure deployment.

**Possible weaknesses:**

- Accident risk.
- High material consumption.
- Pollution or environmental damage.
- Dependence on complex supply chains.

These are tendencies, not class restrictions. A member of the martial
lineage may become an exceptional doctor. A member of the restorative
lineage may become a soldier. A member of the tactical-engineering
lineage may become a farmer.

Their performance depends on aptitude, education, professional
experience, institutions, equipment, and personal history — exactly as
described in `GAME_VISION.md` for any citizen.

## 6. Founder influence

At the beginning of a city, the player selects:

- The founding lineage.
- The initial embodied hero.
- The founder's primary profession or calling.
- Initial environmental and cultural conditions.

The founder should influence the early city through:

- Teaching.
- Cultural prestige.
- Available knowledge.
- Professional imitation.
- Initial institutions.
- Recruitment preferences.
- Early production priorities.
- The probability that other citizens pursue related professions.

This is **not** a global numerical bonus that automatically converts
citizens into copies of the founder.

Example, using the restorative lineage as a working name:

```
Founder profession: healer

Possible early consequences:
- Greater social respect for medical work.
- Earlier appearance of apprentices.
- Basic medical knowledge becomes easier to transmit.
- Medical supplies receive higher default priority.
- Migrant healers may find the city more attractive.
```

The founder influences history but does not permanently dictate it.

## 7. Environmental alignment

The city should have a systemic environmental alignment inspired by
the conceptual opposition between the regenerative and extractive
forces present in the source material. The terms `Wakfu` and `Stasis`
must not be used as final public-facing names. Original terminology
will be created later.

The alignment is not a simplistic good-versus-evil meter. It reflects
how the civilization interacts with living systems and material
extraction.

### 7.1 Regenerative tendencies

Actions that may move the city toward a regenerative alignment:

- Replanting forests.
- Maintaining agricultural fertility.
- Preserving water systems.
- Protecting animal populations.
- Restoring damaged territory.
- Recycling materials.
- Producing without exhausting renewal rates.
- Establishing sustainable settlements.

### 7.2 Extractive tendencies

Actions that may move the city toward an extractive or destructive
alignment:

- Excessive logging.
- Exhausting mines without restoration.
- Raiding other settlements.
- Destroying infrastructure.
- Polluting water.
- Overhunting.
- Burning territory.
- Prioritizing immediate extraction over regeneration.

### 7.3 Neither alignment is automatically a win or loss

A regenerative city may gain advantages such as faster biological
recovery, more reliable agriculture, wildlife migration, better
long-term resource renewal, improved public health, and access to
nature-based discoveries. It may also face disadvantages such as
slower extraction, higher land-management costs, restrictions on
rapid expansion, and lower short-term industrial output.

An extractive city may gain advantages such as faster resource
acquisition, stronger short-term industrial expansion, efficient
military supply, greater capacity for rapid construction, and access
to destructive technologies or doctrines. It may also face
consequences such as resource depletion, environmental instability,
reduced agricultural reliability, hostile migration patterns,
public-health problems, and dependence on raids or territorial
expansion.

The player determines whether the resulting model is sustainable
through the systems they construct.

## 8. Systemic influence of alignment

Environmental alignment may influence:

- Agriculture.
- Resource regeneration.
- Architecture.
- Available materials.
- Creature behavior.
- Migration.
- Public health.
- Political factions.
- Cultural values.
- Research.
- Expedition opportunities.
- City appearance.
- Ambient effects.
- Music and soundscape.
- Available institutional paths.

The alignment emerges from accumulated actions and policies rather
than from selecting a permanent faction at character creation.

## 9. Naming discipline

Provisional names, including the project name, exist to make the
design concrete. The process for promoting a provisional name to a
public-facing name is:

1. The provisional name is documented in an internal document.
2. The name is reviewed against the originality rules in §3.
3. The name is either confirmed as original or replaced.
4. The public-facing artifact is updated to use the confirmed name.

Until a name is confirmed, treat it as an internal placeholder. Do
not present provisional names as final shipping terminology in
public-facing artifacts.
