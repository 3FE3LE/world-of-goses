# Game Vision

> The design fantasy of World of Goses, written before any system is
> implemented. This document is meant to keep the team honest about what
> we are building — and what we are not.

---

## 1. Main fantasy

A single living city that grows because of decisions made by a player who
is not always present. The world does not wait. The world does not pause.
The world executes what the player has authorized and waits for the next
decision that only the player can make.

The fantasy is not "I control everything." The fantasy is "I shape a
society that I can leave and return to without losing it."

## 2. Single-city concept

The player controls **one** persistent city.

The player does not manage multiple cities. The player does not receive
bonuses for restarting. To begin another story, the player must delete
the current city or use another account. The only progression transferred
between playthroughs is the knowledge acquired by the player.

This is a deliberate constraint. It is the foundation of every other
design decision in this document.

## 3. Absence without artificial penalties

The world continues advancing while the game is closed. Player absence
must not apply artificial penalties. The city does not "decay" because
the player logged off. It does not gain a hidden advantage because the
player returned at the perfect moment.

What the world executes while the player is away:

- Previously authorized orders.
- Configured policies.
- Production chains.
- Medical treatments.
- Approved construction.
- Active expeditions.
- Inventory replenishment.
- Citizen training and accumulated experience.

What the world does **not** do while the player is away: make sovereign
decisions that belong to the player, unless the player has explicitly
delegated specific authority through institutions or protocols.

## 4. Two gameplay pillars

### 4.1 City development

A multi-dimensional evaluation of the city, **not** a single overall level.

Development is measured across independent dimensions, including:

- Age and historical continuity.
- Cultural development.
- Political development.
- Economic development.
- Geographic development.
- Demographic complexity.
- Professional coverage.
- Knowledge redundancy.
- Institutional capacity.
- Generational transmission of experience.

Having one thousand soldiers does not automatically turn a population into
an advanced city. The dimensions are independent on purpose.

Buildings are not unlocked through an arbitrary level. They require real
conditions. A hospital requires medical knowledge, available personnel,
supplies, infrastructure, administration, economic capacity, and a
political decision. A society may develop in different ways. It may
become agricultural, academic, mercantile, industrial, nomadic, military,
raider-based, or an emergent combination. The game does not impose a
single correct model of development.

### 4.2 Expeditions

Expeditions are automatic. There is no direct combat control.

The player configures:

- Members.
- Roles.
- Positioning.
- Target priorities.
- Automatic skill usage.
- Retreat policy.
- Equipment.
- Supplies.
- Route.
- Objective.
- Survival priorities.

Expeditions may be used to:

- Explore.
- Expand territory.
- Contact villages, cities, and factions.
- Recruit or attract migrants.
- Respond to threats.
- Discover knowledge.
- Obtain material samples.
- Discover exploitable resources.
- Negotiate access to technologies.
- Obtain blueprints.
- Learn about policies, institutions, and economic models.
- Discover medical methods, water systems, or architectural practices.
- Establish diplomatic relations.
- Generate historical opportunities for the city.

Expeditions are not an infinite source of loot. There are no randomly
dropped legendary weapons. Equipment depends on available materials,
technological capacity, known designs, artisan experience, manufacturing
quality, mass-production capacity, and city logistics.

## 5. Citizens

Each citizen is capable of developing multiple competencies.

The system must distinguish between:

- Natural aptitudes.
- Physical or mental statistics.
- Current profession.
- Previous professions.
- Competencies.
- Contextual experience.
- Training.
- Knowledge.
- Personal history.
- Culture.
- Species or race.
- Health condition.
- Relationships.
- Potential.

Two citizens with the same statistics do not necessarily have the same
performance. A citizen who developed their abilities through years of
mining, expeditions, teaching, and real experiences should be more
efficient than another citizen with equivalent statistics but no
practical experience.

Experience affects production, quality, safety, waste, tool usage,
teaching ability, reaction to problems, and specialization.

Any citizen may become an adventurer or hero if their environment,
education, experiences, mentors, and opportunities develop that
potential. Heroes do not appear only because of randomness.

## 6. Combat, defeat, and healthcare

Combat is automatic and conceptually similar to an idle battler.

Defeat during an expedition activates an organic teleportation
mechanism that transports only the character. The character returns
without weapons, armor, tools, clothing, supplies, or transported
resources — while preserving all wounds, diseases, and physical or
psychological consequences.

Godot should represent the return appropriately, such as immediately
covering the character with a medical blanket. Explicit nudity is not
required.

Lost equipment does not return automatically. The city must manufacture
and store replacements.

Wounds do not heal instantly. They require time, medical personnel,
beds, medicine, treatment, rehabilitation, and infrastructure. Medical
personnel assigned to a patient cannot simultaneously care for other
citizens at full capacity.

Citizens may die inside the city due to systemic causes such as natural
disasters, disease, insecurity, lack of medical attention, social
crises, and other clearly explainable chains of consequences. Death does
not depend on an invisible random roll without understandable causality.

## 7. Production and storage

Production follows configurable chains and policies.

If an expedition uses four armor sets out of a target stock of eight to
twelve sets, four inventory spaces become available. The production
chain may manufacture replacements, consuming real materials. Production
stops when the maximum is reached. If materials, workers, tools,
transport, or storage space are missing, production must stop because of
that specific cause.

Production is not infinite. Each system may depend on local capacity,
transport, storage, minimum stock, maximum stock, priority, materials,
personnel, and maintenance.

Time does not magically reduce efficiency. Efficiency changes because of
internal causes such as experience gained, missing materials, full
storage, broken tools, sick workers, new technologies, logistics
saturation, demand changes, or damaged infrastructure. A well-configured
city may improve while the player is absent.

## 8. Persistent time

The world continues advancing while the game is closed. The architecture
must support:

- Saving world state.
- Saving the timestamp of the last update.
- Calculating elapsed time.
- Processing changes through discrete events.
- Avoiding simulation of every individual second.
- Generating a causal report of what happened.

Example causal report:

```
08:00 Game closes.
10:00 One armor set is completed.
11:30 Coal runs out.
13:00 An expedition returns.
13:05 The hospital reaches critical capacity.
16:00 The player returns.
```

The exact time scale has not been decided yet. The project does not
hardcode a relationship between real time and world time.

## 9. Design principles

1. **One city. One story.** No meta-progression between cities. No
   bonuses for restarting.
2. **No artificial penalties for absence.** The world continues. It does
   not punish the player for being away.
3. **No sovereign decisions without authorization.** The world only
   executes what the player has authorized, configured, or delegated.
4. **No single overall level.** Development is multi-dimensional.
5. **No arbitrary unlocks.** Buildings require real conditions.
6. **No random loot.** Equipment is produced, not found.
7. **No invisible death.** Death has explainable causes.
8. **No instant healing.** Wounds require treatment.
9. **No magic-string efficiency.** Changes have internal causes.
10. **No single correct model of development.** Agricultural, academic,
    mercantile, industrial, nomadic, military, raider-based, or
    emergent combinations are all valid paths.
11. **Causality over randomness.** Every consequence must trace back to
    a real chain of events.
12. **Composition over inheritance.** Code is structured by parts that
    combine, not by deep class hierarchies.
13. **Domain is not presentation.** The simulation does not depend on
    sprites, cameras, or animations.
14. **Originality.** All current names — including the project name —
    are provisional. Inspirations inform design. Outputs are original.

These principles are constraints on future design decisions, not
aspirations. A system that violates one of them should be redesigned,
not waved through.
