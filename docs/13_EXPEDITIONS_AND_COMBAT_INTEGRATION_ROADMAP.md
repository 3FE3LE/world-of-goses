# Roadmap de integración de combate y expediciones

## Estado del documento

Versión inicial canónica para prototipo: `v0.1`.

Este documento ordena por dependencias la integración del sistema de combate automático y expediciones de **World of Goses**.

Parte de las decisiones consolidadas en:

```text
01_GAME_VISION.md
02_CORE_GAMEPLAY_PILLARS.md
04_CITIZENS_PROFESSIONS_AND_HEROES.md
05_EXPEDITIONS.md
10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md
11_ELEMENTAL_AFFINITIES_AND_WORLD_INTERACTIONS.md
12_STATISTICS_PROGRESSION_AND_COMBAT_FORMULAS.md
```

El objetivo inmediato no es construir el sistema definitivo, sino demostrar un circuito vertical completo:

```text
Citizen persistente
→ preparación
→ estadísticas derivadas
→ técnicas automáticas
→ combate
→ consecuencias
→ regreso
→ actualización de la ciudad
```

---

## 1. Principios de integración

1. `Citizen` continúa siendo la única entidad persistente de persona.
2. No crear una entidad separada de héroe o combatiente.
3. El dominio de combate no depende de nodos, escenas, animaciones ni frame rate.
4. Las estadísticas derivadas se calculan bajo demanda mediante el sistema ya integrado.
5. La competencia es la principal progresión numérica mutable.
6. La afinidad elemental y la expresión física son inmutables.
7. El arma aporta `PhysicalTransfer` y `ElementalResonance`.
8. Una técnica convierte potencia de canal en una acción concreta mediante coeficientes físico y elemental.
9. El combate es automático, pero su resultado debe responder a preparación, prioridades, equipo, salud y composición.
10. La vida, heridas, equipo, suministros y experiencia sobreviven al encuentro y al regreso.
11. La primera expedición no será procedural.
12. No implementar contenido masivo antes de validar el circuito completo.

---

## 2. Orden de implementación

### Fase 0. Validación del sistema estadístico

#### Objetivo

Confirmar que la implementación actual de estadísticas coincide con `12_STATISTICS_PROGRESSION_AND_COMBAT_FORMULAS.md`.

#### Entregables

- Tests de los seis `Citizen` de referencia.
- Cálculos con y sin equipamiento.
- Validación de caps y retornos decrecientes.
- Desglose auditable de cada stat.
- Confirmación de que `PhysicalChannelPower` y `ElementalChannelPower` no se nombran como daño final.

#### Criterio de salida

Cada resultado debe poder explicar:

```text
CubeFace
+ GearSupport
× WeaponChannel
× SkillFactor
× ConditionFactor
× CitySupportFactor
```

---

### Fase 1. Competencias y curva de experiencia

#### Objetivo

Definir cómo una competencia progresa de nivel `0` a `20` y cómo alimenta `SkillFactor`.

#### Modelo mínimo

```text
CompetencyProgress
├── competencyId
├── level
├── accumulatedXp
├── learningEfficiency
└── learningCeiling
```

#### Reglas iniciales

- Nivel mínimo: `0`.
- Nivel máximo: `20`.
- Familia natural de arma: `100 %` de XP aplicada.
- Familia extranjera: `10 %` de XP aplicada.
- La penalización extranjera afecta aprendizaje, no daño directo.
- Las competencias profesionales no usan la penalización de familias de arma.
- La práctica rutinaria no permite superar indefinidamente el techo de la actividad.
- El progreso offline puede consolidar trabajo ya configurado, pero no inventa oportunidades nuevas.

#### Alcance del primer slice

Implementar únicamente:

```text
Spear
Staff
Mace
Orb
Survival
```

La curva definitiva de XP debe centralizarse en configuración y quedar cubierta por tests.

#### Criterio de salida

Un `Citizen` puede obtener XP, subir de nivel, recalcular sus stats y conservar el progreso al regresar.

---

### Fase 2. Contrato de técnicas y acciones

#### Objetivo

Convertir potencia física y elemental en acciones ejecutables.

#### Contrato mínimo

```text
TechniqueDefinition
├── id
├── source
├── kind
├── requiredWeaponFamily
├── physicalCoefficient
├── elementalCoefficient
├── cooldown
├── activationTime
├── targetRule
├── priorityRule
├── animationTag
└── evolutions
```

#### Fuentes

```text
WeaponFamily
PhysicalExpression
ElementalAffinity
```

Cada árbol modular tendrá inicialmente:

```text
1 active technique
1 passive technique
```

No existen 72 árboles únicos. Existen:

```text
12 árboles de arma
6 árboles de expresión física
6 árboles de afinidad elemental
```

Cada `Citizen` combina un árbol de cada grupo.

#### Resolución base

```text
PhysicalContribution =
    PhysicalChannelPower × PhysicalCoefficient

ElementalContribution =
    ElementalChannelPower × ElementalCoefficient

RawTechniqueResult =
    PhysicalContribution + ElementalContribution
```

La afinidad interpreta el aporte elemental. La expresión física interpreta el aporte físico cuando la técnica lo permite.

#### Progresión de una técnica

Una técnica conserva su identidad y evoluciona mediante hitos, no mediante una colección creciente de clones.

Propuesta inicial:

```text
Nivel 5  → orientación física, elemental o híbrida
Nivel 10 → forma de objetivo
Nivel 15 → ritmo, coste o cooldown
Nivel 20 → transformación de maestría
```

La primera integración puede representar estas evoluciones como datos sin implementar todavía todas sus opciones.

#### Criterio de salida

Una técnica puede resolverse con ambos coeficientes y mostrar su desglose completo.

---

### Fase 3. Estados físicos y manifestaciones elementales

#### Objetivo

Permitir que las técnicas produzcan consecuencias distintas al daño inmediato.

#### Estados físicos del primer slice

```text
Stunning
Knockdown
```

Los siguientes quedan modelados, pero no necesitan comportamiento completo todavía:

```text
Fracture
Bleeding
Poisoning
Paralysis
```

#### Afinidades soportadas por el dominio

```text
Earth
Water
Fire
Air
Aether
Silence
```

Las seis deben existir en datos desde el inicio. Sus comportamientos provisionales pueden ser sencillos, pero no deben implementarse como seis copias cromáticas del mismo daño periódico.

#### Contrato mínimo de estado

```text
StatusEffect
├── id
├── sourceCitizenId
├── targetCitizenId
├── stacks
├── duration
├── threshold
├── appliedAt
└── expirationRule
```

#### Criterio de salida

Una técnica puede aplicar, acumular, resolver y expirar un estado de manera determinista.

---

### Fase 4. Equipamiento y loadout

#### Objetivo

Formalizar el equipo necesario para preparar una expedición y recalcular al `Citizen`.

#### Ranuras

```text
Weapon
OffHand
Helmet
Chest
Legs
Boots
Gloves
```

#### Propiedades mínimas del arma

```text
WeaponFamily
PhysicalTransfer
ElementalResonance
Mass
Integrity
Wear
```

#### Propiedades mínimas de armadura

```text
CubeSupport
Mass
Integrity
Wear
```

#### Restricciones

- `ElementalResonance` es universal.
- No crear resonancias específicas por elemento.
- El equipo no modifica permanentemente el Cubo.
- El loadout debe poder reemplazarse sin perder identidad o competencias.
- El sistema debe permitir pérdida y desgaste posteriores sin acoplarlos todavía a cada encuentro.

#### Criterio de salida

Equipar o retirar una pieza recalcula correctamente los stats y conserva el desglose de fuentes.

---

### Fase 5. Salud, heridas y `ConditionFactor`

#### Objetivo

Derivar la condición actual de causas persistentes y conectar combate con ciudad.

#### Fuentes iniciales

```text
CurrentHealth
Fatigue
Injuries
Diseases
Hunger
Recovery
```

La primera versión puede implementar solo:

```text
CurrentHealth
Fatigue
Injuries
```

#### Heridas iniciales

```text
Contusion
OpenWound
TemporaryIncapacitation
```

#### Reglas

- La vida no se restaura automáticamente entre encuentros.
- Las heridas persisten al regresar.
- Un `Citizen` incapacitado no desaparece del dominio.
- La UI debe mostrar causas individuales y el `ConditionFactor` resultante.
- La curación durante combate no elimina automáticamente una herida persistente.

#### Criterio de salida

Un `Citizen` puede salir saludable, regresar herido y quedar temporalmente indisponible.

---

### Fase 6. Motor de combate automático

#### Objetivo

Resolver un encuentro completo sin control directo de movimiento.

#### Componentes de dominio sugeridos

```text
CombatEncounter
CombatantState
ActionScheduler
TargetResolver
TechniqueResolver
StatusResolver
DamageResolver
AutoCastController
CombatLog
```

Los nombres deben adaptarse a la arquitectura real del repositorio.

#### Configuración mínima del jugador

```text
position
techniquePriority
preferredTarget
useCondition
retreatRule
```

#### Condiciones automáticas iniciales

```text
UseWhenReady
UseAgainstTwoOrMoreEnemies
UseWhenAllyBelowHalfHealth
UseToInterrupt
ReserveForPrimaryTarget
```

#### Requisitos técnicos

- Resolución determinista cuando se usa la misma semilla y configuración.
- El dominio no depende de `_Process`.
- Puede ejecutarse por pasos discretos o timeline lógico.
- Debe producir un `CombatLog` auditable.
- La presentación consume eventos o snapshots del dominio.

#### Criterio de salida

Tres `Citizen` pueden combatir automáticamente contra enemigos provisionales hasta victoria, retirada o incapacitación.

---

### Fase 7. Expedición vertical mínima

#### Objetivo

Integrar preparación, navegación fija, combates, destino y regreso.

#### Flujo

```text
City
→ Preparation
→ Departure
→ Segment A
→ Encounter A
→ RouteDecision
→ Segment B
→ Encounter B
→ Destination
→ Return
→ ExpeditionResult
```

#### Alcance

```text
Grupo:                         3 Citizens
Familias de arma:              Spear, Staff, Mace, Orb
Expresiones físicas completas: Stunning, Knockdown
Afinidades en dominio:         6
Estados físicos funcionales:   2
Encuentros:                    2
Decisión de ruta:              1
Destino:                       1
Regreso persistente:           sí
Proceduralidad:                no
```

#### Preparación

```text
members
positions
loadouts
supplies
techniquePriorities
retreatRule
objective
```

#### Navegación

La primera ruta es fija y ofrece una sola decisión:

```text
SafeRoute
→ encuentro previsible

ShortRoute
→ mayor riesgo y mejor recompensa
```

No llamar a esta estructura roguelike como definición canónica. Es una expedición ramificada con consecuencias persistentes.

#### Resultado

```text
ExpeditionResult
├── survivors
├── injuries
├── equipmentState
├── consumedSupplies
├── acquiredResources
├── gainedExperience
├── discoveredRouteState
└── combatLogs
```

#### Criterio de salida

El circuito funciona de extremo a extremo usando los mismos `Citizen` persistentes de la ciudad.

---

## 3. Fases posteriores al vertical slice

Estas fases forman parte del roadmap general, pero no bloquean la primera expedición jugable.

### Fase 8. Ciudad de apoyo

Fusiona:

```text
profesiones
herramientas
producción
instituciones
CitySupportFactor
recuperación
abastecimiento
```

Primeras funciones necesarias:

```text
Recolector
Constructor
Artesano
Sanador
Investigador
```

Son asignaciones y competencias, no clases permanentes.

El `CitySupportFactor` debe derivarse de servicios concretos y no ser una bonificación global arbitraria.

### Fase 9. Envejecimiento y transferencia de conocimiento

Debe definir:

```text
edad
etapas vitales
retiro
mortalidad
maestros
aprendices
instituciones
conocimiento preservado
```

Principio:

```text
La competencia pertenece a la persona.
El conocimiento puede sobrevivir en la institución.
```

### Fase 10. Bestiario y escalado de dificultad

El bestiario definitivo se diseña después de validar técnicas, defensas, estados, recuperación y economía de equipo.

La dificultad debe surgir de composición, comportamiento, entorno, rutas y objetivos, no solo de inflar vida y daño.

---

## 4. Telemetría obligatoria

Durante el prototipo debe existir una vista o log de depuración que muestre:

```text
PhysicalChannelPower
ElementalChannelPower
PhysicalCoefficient
ElementalCoefficient
PhysicalContribution
ElementalContribution
RawTechniqueResult
PhysicalMitigation
ElementalMitigation
CriticalResult
FinalResult
AppliedStatuses
ConditionFactor changes
```

La telemetría puede ser provisional, pero el dominio debe producir los datos necesarios sin depender de la UI.

---

## 5. Guardarraíles

1. No crear una entidad de combate separada del `Citizen` persistente.
2. No implementar 72 árboles de habilidades independientes.
3. No crear niveles para afinidad elemental o expresión física.
4. No crear resonancias específicas por elemento.
5. No introducir nuevas estadísticas sin documentarlas.
6. No llamar daño final a las potencias de canal.
7. No duplicar experiencia por arma, expresión, afinidad y habilidad en la primera versión.
8. No implementar proceduralidad antes de validar la expedición fija.
9. No diseñar un bestiario completo contra coeficientes todavía inestables.
10. No conectar el dominio directamente a escenas, nodos o assets.
11. No simular cada `Citizen` urbano en `_Process`.
12. No ocultar números mágicos dentro de resolvers o escenas.

---

## 6. Definición de terminado del primer vertical slice

El slice se considera completo cuando:

1. Se seleccionan tres `Citizen` persistentes.
2. Se configuran posición, arma, equipo, técnicas y retirada.
3. Sus stats se calculan mediante el sistema existente.
4. Las técnicas usan coeficientes físico y elemental.
5. El combate automático resuelve cooldowns, objetivos y estados.
6. La expedición presenta dos encuentros y una decisión de ruta.
7. La vida, fatiga, heridas, experiencia y equipo conservan consecuencias.
8. El grupo alcanza el destino o regresa derrotado.
9. El resultado actualiza el estado persistente de la ciudad.
10. Cada cálculo relevante puede inspeccionarse mediante telemetría o pruebas.

