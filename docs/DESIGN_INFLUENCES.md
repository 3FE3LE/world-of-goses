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

## 4. The canonical eight-lineage roster

The current canonical roster is **eight original working lineages**: Ardhen, Eirune, Kovari, Myrven, Vaelun, Orveth, Caelith, and Theryn. Their professional affinities and the citizen-profile rules are maintained in [`LINEAGES_AND_PROFESSIONAL_AFFINITIES.md`](LINEAGES_AND_PROFESSIONAL_AFFINITIES.md).

The roster is not a class system. A lineage creates tendencies and opportunities, not absolute restrictions. Members of the same lineage may become miners, doctors, farmers, engineers, politicians, artisans, adventurers, or any other profession. Experience, education, tools, health, institutions, and personal history must matter more than birth over time.

The eight names remain provisional until they pass the originality review in §9. The canonical design document owns the detailed affinities; this document owns the boundary between inspiration and original implementation.

## 5. Retired MVP shorthand

Earlier drafts used three internal shorthand archetypes (martial, restorative, and tactical-engineering) to explore the design space. They are historical notes, not the current roster and not implementation requirements. They must not override the eight canonical lineages or be exposed as public-facing classes.

The old shorthand remains below only as an audit trail. Any future implementation must use original names, cultures, visual silhouettes, professional affinities, strengths, vulnerabilities, and environmental relationships described in the canonical document.

## 6. Founding hero and profile

The current entry point is the complete hero onboarding described in [`LINEAGES_AND_PROFESSIONAL_AFFINITIES.md`](LINEAGES_AND_PROFESSIONAL_AFFINITIES.md). The player chooses a name, one lineage, personal aptitudes, professional affinities, elemental affinity, combat preferences, traits, political orientation, and spiritual posture.

These choices establish the hero's identity and future learning context. They are not a global numerical bonus and do not convert other citizens into copies of the founder. The hero remains an ordinary `Citizen` carrying the `Hero` recognition.

Environmental alignment is separate: it emerges from accumulated city actions and policies rather than from a permanent faction selected at character creation.

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
