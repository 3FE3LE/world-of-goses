# Estadísticas, progresión y fórmulas de combate

## Qué es

El modelo matemático que convierte a una persona —su Cubo, su afinidad, su
equipo, su competencia y su condición— en los números concretos con los que
resuelve un encuentro. Es la referencia para leer cualquier valor que el juego
muestre, y para no inventar una fuente de poder nueva.

## Qué problema jugable resuelve

Hace que el combate automático sea explicable. El jugador no controla el
movimiento, así que la única forma de que el resultado se sienta suyo es que
cada número pueda rastrearse hasta una decisión: a quién envió, con qué, en
qué estado y con cuánta práctica.

## Autoridad

| Concepto | Autoridad |
| --- | --- |
| Coeficientes de balance | `StatisticsBalanceConfig` (combate: `CombatBalanceConfig`) |
| Valor efectivo de una cara | `EffectiveCubeProfile`, con desglose en `CubeFaceCalculation` |
| Cálculo por familia | `OffensiveStatisticsCalculator`, `DefensiveStatisticsCalculator`, `RecoveryStatisticsCalculator`, `TempoStatisticsCalculator` |
| Curvas compartidas | `StatisticsCalculation` |
| Desglose para UI | `StatisticsBreakdown` |
| Canales del arma | `WeaponChannelProfile` |
| Nivel de competencia | `CompetencyProgress`, `CompetencyLevelCurve` |

Los coeficientes numéricos son **valores provisionales y configurables**: viven
en configuración y se ajustan con pruebas sin tocar la arquitectura del
dominio. Ninguna fórmula debe codificarlos fuera de ahí.

---

## 1. Principios

1. `Citizen` es la única entidad persistente de persona.
2. El Cubo Kovari describe capacidades intrínsecas; el equipo no lo modifica permanentemente.
3. Cada pareja del Cubo suma `100`.
4. El perfil inicial ordinario favorecido por linaje parte de `60/40`.
5. El onboarding puede matizar el perfil individual sin cambiar el linaje.
6. La afinidad elemental y la expresión física son inmutables.
7. La afinidad elemental no es un multiplicador: determina la naturaleza de la manifestación elemental.
8. La expresión física no es un multiplicador: determina la consecuencia física natural del `Citizen`.
9. La progresión entrenable vive principalmente en competencias de armas, herramientas, profesiones y técnicas.
10. Todos los stats de combate y utilidad se calculan bajo demanda; no se guardan como fuentes independientes de poder.
11. El equipo aporta canales y apoyos temporales, no reescribe el Cubo.
12. Las fórmulas deben mostrar un desglose auditable en UI y pruebas.

---

## 2. Naturaleza del Citizen

### 2.1 Cubo Kovari

```text
CubeProfile
├── Body / Bond
├── Stability / Impulse
└── Domain / Reach
```

Restricciones:

```text
Body + Bond = 100
Stability + Impulse = 100
Domain + Reach = 100
```

Perfil ordinario favorecido:

```text
60 / 40
```

Perfil neutral:

```text
50 / 50
```

El Cubo no sube de nivel. Sus valores son identidad sistémica del `Citizen` y no se modifican al equipar objetos.

### 2.2 Afinidad elemental

```text
ElementalAffinity
├── Earth
├── Water
├── Fire
├── Air
├── Aether
└── Silence
```

La afinidad:

- se obtiene en la creación del `Citizen`;
- no cambia;
- no tiene nivel;
- no agrega un multiplicador;
- interpreta el resultado del canal elemental.

### 2.3 Expresión física

```text
PhysicalExpression
├── Fracture
├── Poisoning
├── Paralysis
├── Stunning
├── Bleeding
└── Knockdown
```

La expresión física:

- se obtiene en la creación del `Citizen`;
- no cambia;
- no tiene nivel;
- no agrega un multiplicador;
- interpreta el resultado del canal físico cuando la técnica y el objeto lo permiten.

#### Qué hace cada expresión

Las seis son **ofensivas**: ninguna existe sólo para negar el turno del rival.
Se agrupan en tres pares, y dentro de cada par las dos se distinguen por
textura, no por potencia.

| Par | Expresión | Qué hace |
|---|---|---|
| Control | `Stunning` | Impide la acción en curso durante poco tiempo y, mientras dura, abre una **ventana elemental**: el objetivo aturdido no sostiene su resonancia. |
| Control | `Paralysis` | Frena mucho el desplazamiento durante bastante tiempo y, cada paso, tiene **probabilidad** de costar además la acción. Esa probabilidad es lo que impide que sea gratis contra un enemigo a distancia, al que un debuff de movimiento no le quita nada. |
| Desgaste | `Bleeding` | Daño por paso **mitigable como físico**, que acumula por stacks. |
| Desgaste | `Poisoning` | Daño por paso que **ignora la mitigación** y **no acumula**: refrescarlo es la única forma de mantenerlo. A cambio, mientras dura, todo el daño que recibe el objetivo se amplifica. |
| Exposición | `Fracture` | Abre una **ventana física** y, además, cobra vida al objetivo cada vez que golpea, en proporción a cuánto de su propio golpe fue cuerpo. Usar un cuerpo roto duele. Necesita más de una aplicación para prender. |
| Exposición | `Knockdown` | Interrumpe la acción, expone **ambas** ventanas a la vez —tumbado no se guarda nada— y es la **única** expresión que mueve al objetivo. |

`Stunning` y `Fracture` son deliberadamente simétricas: la ventana elemental y
la ventana física. `Bleeding` y `Poisoning` son la misma presión leída al revés:
una acumula y se mitiga, la otra ni acumula ni se mitiga.

Ninguna de las seis prende por el mero hecho de lanzarla: se tira
`ControlPower` del atacante contra `ControlResistance` del objetivo (§8.4). Una
expresión rechazada no aplica estado y, en el caso de `Knockdown`, tampoco
desplaza.

#### Sólo `Knockdown` desplaza

El desplazamiento autoritativo —el que escribe `PositionX` en el dominio— lo
paga únicamente un golpe que aplica `Knockdown`. La distancia sale de `Impulse`
contra `Stability` y se escala además por la **proporción física del golpe**:
una descarga puramente elemental no transfiere momento.

El empujón menor que un impacto sólido *parece* que debería producir es real,
pero es una **reacción de impacto y vive en presentación** (`HitReaction`):
transitoria, decae a cero y por tanto termina exactamente donde el dominio dice
que la figura está. Se dimensiona con la misma razón que el knockback real
—proporción física del golpe × `Impulse` contra `Stability`— para que el
empujón se parezca al golpe que lo causó, pero es **siempre visiblemente menor**
que un derribo, o la expresión perdería lo que la hace valiosa. Un golpe evadido
o absorbido no empuja nada. Nada de esto vuelve al dominio ni aplica estado, y
un encuentro observado y otro resuelto sin mirar dejan a todos en el mismo
sitio.

Antes, cualquier técnica que hiciera daño escribía `PositionX`. El combate
derivaba por el campo con el desgaste ordinario y la distancia de combate
acababa siendo algo que no había elegido ninguno de los dos.

Las duraciones, umbrales y magnitudes son provisionales y viven centralizadas en
`CombatBalanceConfig`, en dominio determinista, nunca en la animación.

### 2.4 Correspondencia conceptual de las seis caras

| Cara | Afinidad elemental | Expresión física |
|---|---|---|
| Body | Earth | Fracture |
| Bond | Aether | Poisoning |
| Stability | Water | Paralysis |
| Impulse | Fire | Stunning |
| Domain | Silence | Bleeding |
| Reach | Air | Knockdown |

**Las dos columnas de la derecha son correspondencias independientes de la
cara, no una cadena.** Leer esta tabla como `afinidad → expresión física` es el
error que la implementación cometió hasta el 2026-08-07: colapsaba los dos ejes
en uno y dejaba seis combinaciones donde el diseño describe treinta y seis.
Ningún `Citizen` deriva su expresión de su elemento.

### 2.5 De dónde sale cada uno

```text
Onboarding
    │
    ├── puntuación de linaje ── Lineage
    │          │
    │          └── vértice 60/40 del Cubo
    │                     +
    │       contribuciones de las doce respuestas (±8 agregado por eje)
    │                     ↓
    │              CubeProfile final
    │                     ↓
    │              cara más alta
    │                     ↓
    │            PhysicalExpression
    │
    └── señal elemental ── PrimaryAffinity
```

Se persiste el `CubeProfile` como fuente inmutable y se deriva la expresión
física de él, evitando dos campos capaces de contradecirse. La derivación es
una función pura del cubo: el mismo cubo responde siempre lo mismo, sea cual
sea la afinidad, y ningún estado adicional se guarda.

**Empates.** Las tres caras favorecidas pueden igualarse — de hecho es el caso
normal de un ciudadano sin onboarding, cuyo cubo es el vértice puro `60/60/60`.
El desempate es determinista y no usa azar, orden de enum, iteración de
diccionario ni orden de carga: gana la primera cara del orden canónico
explícito `Body, Bond, Stability, Impulse, Domain, Reach`.

Bajo `60/40` con el tope de `±8`, una cara favorecida se mueve dentro de
`52–68` y su opuesta dentro de `32–48`. La cara más alta es por tanto siempre
una de las tres favorecidas del linaje, y **cada linaje admite exactamente tres
expresiones físicas**: eso no se impone con una lista de exclusión, sale de las
restricciones del cubo. Cada expresión pertenece así a exactamente cuatro
linajes.

| Linaje | Expresiones alcanzables |
|---|---|
| Ardhen | Fracture · Paralysis · Bleeding |
| Eirune | Fracture · Paralysis · Knockdown |
| Kovari | Fracture · Stunning · Bleeding |
| Vaelun | Fracture · Stunning · Knockdown |
| Orveth | Poisoning · Paralysis · Bleeding |
| Myrven | Poisoning · Paralysis · Knockdown |
| Theryn | Poisoning · Stunning · Bleeding |
| Caelith | Poisoning · Stunning · Knockdown |

Esta relación no es un multiplicador. Un `Citizen` con afinidad Fire no está
obligado a tener Impulse alto, y la expresión no confiere ventaja: sólo dice
qué consecuencia física es natural en él.

---

## 3. Familias naturales de armas

| Expresión física | Familia natural A | Familia natural B |
|---|---|---|
| Stunning | Mace | Orb |
| Bleeding | Sword | Daggers |
| Poisoning | Bow | Darts |
| Paralysis | Whip | Gauntlets |
| Fracture | Hammer | Axe |
| Knockdown | Spear | Staff |

Reglas:

- La expresión física determina dos familias naturales inmutables.
- El arma inicial se elige entre las dos familias naturales.
- Cualquier `Citizen` puede equipar cualquier familia.
- El aprendizaje tiene tres niveles, no dos (`DEC-0018`):

  | Nivel | Qué familia | Eficiencia de XP |
  |---|---|---|
  | Natural | Las dos de la expresión física del propio `Citizen` | `100 %` |
  | Familiar de linaje | Las cuatro de las otras dos expresiones que su linaje alcanza | `50 %` |
  | Extranjera | Las seis de expresiones que su vértice no puede producir | `10 %` |

  Cada `Citizen` ve por tanto `2` naturales, `4` familiares y `6` extranjeras.
  El nivel se deriva de tres piezas —expresión propia, vértice del linaje y la
  tabla de arriba—, nunca de una tabla de armas por linaje.
- El nivel afecta **sólo** la adquisición de experiencia. No reduce daño,
  precisión, `PhysicalTransfer`, `ElementalResonance`, cooldown, velocidad ni
  coeficientes de técnica. Un `Citizen` que alcanza Sword `20` con una familia
  extranjera ha alcanzado Sword `20`: la dificultad estaba en llegar, no en usarlo.
- Entrenar cualquier familia no cambia la expresión física, que es inmutable y
  deriva del Cubo.
- El `Citizen` solo desbloquea técnicas físicas de su expresión.
- El `Citizen` solo desbloquea técnicas elementales de su afinidad.
- Puede aprender técnicas generales de cualquier arma.
- Herramientas y profesiones no usan esta penalización de armas.

---

## 4. Fuentes autorizadas para estadísticas

```text
DerivedStat
├── CubeFace
├── GearSupport
├── WeaponChannel
├── ApplicableCompetency
├── ConditionFactor
└── CitySupportFactor
```

No introducir nuevas fuentes sin documentarlas.

### 4.1 CubeFace

Valor intrínseco de una cara del Cubo.

Rango práctico inicial:

```text
40–60 en perfiles ordinarios
```

### 4.2 GearSupport

Apoyo temporal aportado por:

```text
Helmet
Chest
Legs
Boots
Gloves
```

Cada pieza puede aportar soporte a una o varias caras:

```text
GearBodySupport
GearBondSupport
GearStabilitySupport
GearImpulseSupport
GearDomainSupport
GearReachSupport
```

El valor efectivo usado en cálculos es:

```text
EffectiveFace = CubeFace + TotalGearSupportForFace
```

`EffectiveFace` es derivado. No se persiste dentro de `CubeProfile`.

Límite inicial recomendado del conjunto completo:

```text
hasta +12 sobre una cara principal
```

Masa, integridad y desgaste no participan en estas fórmulas; `PersonalEquipment` sólo modela el arma equipada. Su expansión está trazada en [#39](https://github.com/3FE3LE/world-of-goses/issues/39).

### 4.3 Canales del arma

Cada arma tiene dos coeficientes universales:

```text
PhysicalTransfer
ElementalResonance
```

Rango inicial sugerido:

```text
0.75–1.20
```

- `PhysicalTransfer` multiplica la capacidad expresada mediante Body.
- `ElementalResonance` multiplica la capacidad expresada mediante Bond.
- `ElementalResonance` es universal para Earth, Water, Fire, Air, Aether y Silence.
- No existen resonancias específicas por elemento.
- **Es un intercambio, nunca un total.** Una familia que transfiere bien el
  cuerpo resuena mal, y al revés. Ninguna es mejor que otra.

#### Canales de las doce familias

Las dos familias naturales de una misma expresión se sitúan en lados opuestos
del mismo intercambio. Seis pares son canon —son los perfiles de referencia de
§9.1— y los otros seis se derivan de su pareja canónica dentro de la banda
sancionada arriba:

| Expresión | Familia | Physical | Elemental | | Familia | Physical | Elemental |
|---|---|---:|---:|---|---|---:|---:|
| Stunning | Mace | 1.15 | 0.85 | | **Orb** | **0.75** | **1.20** |
| Bleeding | Sword | 1.10 | 0.90 | | **Daggers** | **1.05** | **0.95** |
| Poisoning | **Bow** | **0.85** | **1.15** | | Darts | 0.80 | 1.20 |
| Paralysis | **Whip** | **0.95** | **1.00** | | Gauntlets | 1.10 | 0.90 |
| Fracture | **Hammer** | **1.20** | **0.75** | | Axe | 1.15 | 0.80 |
| Knockdown | **Spear** | **1.10** | **1.00** | | Staff | 0.85 | 1.15 |

En **negrita**, las canónicas. La tabla vive en `WeaponFamilyChannels`; el arma
inicial que materializa el onboarding es un arma corriente de su familia y toma
estos valores. Antes las doce compartían `1.0 / 1.0`, con lo que elegir familia
cambiaba el catálogo de técnicas y ni un solo número de cuánto se pega.

### 4.4 Competencia aplicable

La competencia representa experiencia con la familia de arma, herramienta o técnica utilizada.

Curva inicial:

```text
SkillFactor(level) = 1 + 0.025 × level
```

Rango:

```text
level 0  → 1.00
level 20 → 1.50
```

La curva es configurable y provisional. Ninguna fórmula debe codificarla fuera de la configuración central. El coste de subir de nivel `0` a `20` está trazado en [#37](https://github.com/3FE3LE/world-of-goses/issues/37).

### 4.5 Salud y condición

`ConditionFactor` representa el estado actual del `Citizen`, no su identidad.

Rango inicial:

```text
0.50  condición grave
0.75  herido o enfermo
1.00  saludable
1.05  condición excepcional
```

Hoy el calculador lo recibe como un valor ya resuelto: derivarlo de la condición real del `Citizen` está trazado en [#40](https://github.com/3FE3LE/world-of-goses/issues/40).

### 4.6 Apoyo de ciudad

`CitySupportFactor` representa el apoyo operativo que recibe el `Citizen` por infraestructura, servicios y entorno urbano.

Rango inicial:

```text
0.90  entorno desfavorable
1.00  condiciones ordinarias
1.10  apoyo excelente
```

Hoy el calculador lo recibe como un valor ya resuelto: derivarlo de infraestructura y servicios reales está trazado en [#41](https://github.com/3FE3LE/world-of-goses/issues/41).

---

## 5. Familia I: potencia y daño directo

### 5.1 Potencia del canal físico

```text
PhysicalChannelPower = clamp(
    EffectiveBody
    × PhysicalTransfer
    × SkillFactor
    × ConditionFactor
    × CitySupportFactor,
    0,
    160
)
```

### 5.2 Potencia del canal elemental

```text
ElementalChannelPower = clamp(
    EffectiveBond
    × ElementalResonance
    × SkillFactor
    × ConditionFactor
    × CitySupportFactor,
    0,
    160
)
```

La afinidad elemental interpreta `ElementalChannelPower`, pero no lo multiplica.

La expresión física interpreta `PhysicalChannelPower`, pero no lo multiplica.

### 5.3 Daño bruto de una técnica

```text
RawDamage =
    PhysicalChannelPower × TechniquePhysicalCoefficient
  + ElementalChannelPower × TechniqueElementalCoefficient
```

Los coeficientes de técnica todavía no están definidos.

Por tanto:

> `PhysicalChannelPower` y `ElementalChannelPower` no son daño final por sí solos.

Ejemplo:

```text
EffectiveBody = 70
PhysicalTransfer = 1.20
SkillFactor = 1.00
ConditionFactor = 1.00
CitySupportFactor = 1.00

PhysicalChannelPower = 70 × 1.20 = 84
```

El `84` es potencia física disponible para la técnica. Si una técnica usa un coeficiente físico `0.80`, aportaría:

```text
84 × 0.80 = 67.20 de daño físico bruto
```

El coeficiente `0.80` es solo ilustrativo y no forma parte todavía del canon.

---

## 6. Familia II: vida, defensa y reducción de daño

### 6.1 Vida máxima

```text
MaxHealth =
    100
  + 1.5 × EffectiveBody
  + 1.0 × EffectiveStability
```

### 6.2 Defensa física

```text
PhysicalDefenseScore =
(
    0.55 × EffectiveStability
  + 0.45 × EffectiveBody
)
× SkillFactor
× ConditionFactor
× CitySupportFactor
```

### 6.3 Defensa elemental

```text
ElementalDefenseScore =
(
    0.55 × EffectiveStability
  + 0.45 × EffectiveBond
)
× SkillFactor
× ConditionFactor
× CitySupportFactor
```

### 6.4 Curva de mitigación específica

```text
SpecificMitigation = min(
    0.70,
    DefenseScore / (DefenseScore + 60)
)
```

La constante `60` controla la velocidad de retornos decrecientes.

### 6.5 Reducción general

```text
GeneralDamageReduction = min(
    0.20,
    0.20 × EffectiveStability / (EffectiveStability + 100)
)
```

### 6.6 Daño recibido

```text
DamageTaken =
RawDamage
× (1 - GeneralDamageReduction)
× (1 - SpecificMitigation)
```

La reducción general y la mitigación específica se componen multiplicativamente; no se suman.

---

## 7. Familia III: regeneración y curación aplicada

Se usa media geométrica para exigir dos fuentes compatibles y evitar que una sola cara domine por completo.

### 7.1 Regeneración de salud

```text
HealthRegenerationPerMinute =
0.12
× sqrt(EffectiveBody × EffectiveStability)
× SkillFactor
× ConditionFactor
× CitySupportFactor
```

### 7.2 Bonificación de curación aplicada

```text
HealingBonusPercent =
0.50
× sqrt(EffectiveBond × EffectiveDomain)
× SkillFactor
× ConditionFactor
× CitySupportFactor
```

```text
HealingAppliedPercent = 100 + HealingBonusPercent
```

Ejemplo:

```text
HealingAppliedPercent = 126
```

significa que una técnica base de `100` puntos cura `126`, antes de reglas específicas de la técnica o del objetivo.

---

## 8. Familia IV: velocidad, crítico, enfriamiento y evasión

Estas estadísticas usan una curva `smoothstep` acotada.

### 8.1 Curva común

```text
t = clamp((Score - 30) / 60, 0, 1)
Curve = 3t² - 2t³
Result = Min + (Max - Min) × Curve
```

El `Score` se calcula antes de la curva:

```text
Score =
BaseScore
× SkillFactor
× ConditionFactor
× CitySupportFactor
```

`BaseScore` puede ser una cara efectiva o el promedio de dos caras efectivas.

### 8.2 Mapeo y caps

| Stat | BaseScore | Mínimo | Máximo |
|---|---|---:|---:|
| AttackSpeed | EffectiveImpulse | 80 % | 140 % |
| CastSpeed | promedio de EffectiveImpulse y EffectiveBond | 80 % | 140 % |
| CooldownReduction | promedio de EffectiveImpulse y EffectiveDomain | 0 % | 40 % |
| CriticalChance | EffectiveDomain | 5 % | 35 % |
| PhysicalEvasion | promedio de EffectiveImpulse y EffectiveReach | 0 % | 30 % |
| ElementalEvasion | promedio de EffectiveBond y EffectiveReach | 0 % | 30 % |
| MovementSpeed | EffectiveReach | 80 % | 130 % |
| ControlPower | promedio de EffectiveDomain y EffectiveBody | 80 % | 140 % |
| ControlResistance | promedio de EffectiveStability y EffectiveBody | 80 % | 140 % |

Los caps son obligatorios. El equipo y la progresión acercan al límite, pero no lo superan.

### 8.3 Evasión

Un golpe evadido **no ocurre**: no critica, no se mitiga, no aplica expresión y
no desplaza. La probabilidad se mezcla por la proporción física de la técnica,
igual que la mitigación, de modo que un golpe híbrido nunca se resuelve contra
la más baja de las dos evasiones.

### 8.4 Control

`ControlPower` y `ControlResistance` **no son probabilidades**: son un par de
multiplicadores opuestos, y por eso comparten la forma de `AttackSpeed` y no la
de `CriticalChance`. De su cociente sale si una expresión física prende:

```text
LandChance = clamp(
    BaseControlLandChance × ControlPower / ControlResistance,
    MinimumControlLandChance,
    MaximumControlLandChance)
```

Tres reglas cierran el modelo:

- **La base es alta.** Controlar es el sentido ofensivo de las seis
  expresiones, así que lo esperable al lanzar una es que funcione;
  `ControlResistance` recorta eso, no lo bloquea.
- **Nunca es imposible.** El suelo garantiza que ninguna cantidad de
  `Stability` vuelve inmune a un combatiente. Un muro que el jugador no puede
  cruzar nunca no es dificultad, es una puerta cerrada.
- **Nunca es seguro.** El techo deja siempre margen a fallar.

`Body` aparece en los dos lados porque aguantar una fractura y provocarla son
el mismo tejido. Las tres constantes viven en `CombatBalanceConfig`.

---

## 9. Seis Citizens de referencia

Todos los casos usan:

```text
SkillLevel = 0
SkillFactor = 1.00
ConditionFactor = 1.00
CitySupportFactor = 1.00
```

Los otros dos ejes no dominantes parten de `50/50`.

### 9.1 Perfiles

| Citizen | Expresión | Afinidad | Cara dominante | Arma | PhysicalTransfer | ElementalResonance |
|---|---|---|---|---|---:|---:|
| Aren | Fracture | Earth | Body 60 | Hammer | 1.20 | 0.75 |
| Seyra | Poisoning | Aether | Bond 60 | Bow | 0.85 | 1.15 |
| Mira | Paralysis | Water | Stability 60 | Whip | 0.95 | 1.00 |
| Tovan | Stunning | Fire | Impulse 60 | Orb | 0.75 | 1.20 |
| Neris | Bleeding | Silence | Domain 60 | Daggers | 1.05 | 0.95 |
| Vael | Knockdown | Air | Reach 60 | Spear | 1.10 | 1.00 |

### 9.2 Valores efectivos tras equipamiento

| Citizen | Body | Bond | Stability | Impulse | Domain | Reach |
|---|---:|---:|---:|---:|---:|---:|
| Aren | 70 | 41 | 54 | 52 | 55 | 51 |
| Seyra | 41 | 70 | 54 | 52 | 54 | 52 |
| Mira | 54 | 52 | 70 | 41 | 52 | 52 |
| Tovan | 52 | 52 | 41 | 70 | 52 | 54 |
| Neris | 52 | 54 | 52 | 51 | 70 | 42 |
| Vael | 53 | 53 | 52 | 53 | 42 | 70 |

### 9.3 Potencia ofensiva

| Citizen | PhysicalChannelPower | ElementalChannelPower |
|---|---:|---:|
| Aren | 84.00 | 30.75 |
| Seyra | 34.85 | 80.50 |
| Mira | 51.30 | 52.00 |
| Tovan | 39.00 | 62.40 |
| Neris | 54.60 | 51.30 |
| Vael | 58.30 | 53.00 |

### 9.4 Vida y defensa

| Citizen | MaxHealth | PhysicalDefense | PhysicalMitigation | ElementalDefense | ElementalMitigation | GeneralReduction |
|---|---:|---:|---:|---:|---:|---:|
| Aren | 259.0 | 61.20 | 50.50 % | 48.15 | 44.52 % | 7.01 % |
| Seyra | 215.5 | 48.15 | 44.52 % | 61.20 | 50.50 % | 7.01 % |
| Mira | 251.0 | 62.80 | 51.14 % | 61.90 | 50.78 % | 8.24 % |
| Tovan | 219.0 | 45.95 | 43.37 % | 45.95 | 43.37 % | 5.82 % |
| Neris | 230.0 | 52.00 | 46.43 % | 52.90 | 46.86 % | 6.84 % |
| Vael | 231.5 | 52.45 | 46.64 % | 52.45 | 46.64 % | 6.84 % |

### 9.5 Utilidad

| Citizen | RegenerationPerMinute | HealingApplied |
|---|---:|---:|
| Aren | 7.38 | 123.74 % |
| Seyra | 5.65 | 130.74 % |
| Mira | 7.38 | 126.00 % |
| Tovan | 5.54 | 126.00 % |
| Neris | 6.24 | 130.74 % |
| Vael | 6.30 | 123.59 % |

### 9.6 Velocidad y probabilidad

| Citizen | AttackSpeed | CastSpeed | CDR | Critical | PhysicalEvasion | ElementalEvasion | MovementSpeed |
|---|---:|---:|---:|---:|---:|---:|---:|
| Aren | 98.28 % | 91.12 % | 13.60 % | 16.28 % | 8.80 % | 5.26 % | 94.09 % |
| Seyra | 98.28 % | 111.50 % | 13.13 % | 15.56 % | 9.14 % | 15.75 % | 95.24 % |
| Mira | 85.31 % | 91.12 % | 7.41 % | 14.14 % | 5.56 % | 9.14 % | 95.24 % |
| Tovan | 124.44 % | 111.50 % | 21.00 % | 14.14 % | 16.50 % | 9.85 % | 97.60 % |
| Neris | 96.90 % | 98.98 % | 20.50 % | 27.22 % | 5.56 % | 6.48 % | 85.20 % |
| Vael | 99.69 % | 99.69 % | 8.22 % | 8.12 % | 16.12 % | 16.12 % | 117.04 % |

---

## 10. Proyección máxima ordinaria

Supuesto:

```text
CubeFace = 60
GearSupport = +12
WeaponChannel = 1.20
SkillLevel = 20
SkillFactor = 1.50
ConditionFactor = 1.05
CitySupportFactor = 1.10
```

Techos aproximados:

| Stat | Techo ordinario inicial |
|---|---:|
| PhysicalChannelPower | 149.69 |
| ElementalChannelPower | 149.69 |
| MaxHealth | cerca de 280 según el segundo eje |
| SpecificMitigation | hasta 70 % por cap |
| GeneralDamageReduction | hasta 20 % por cap |
| HealthRegeneration | cerca de 15/min en perfiles extremos |
| HealingApplied | cerca de 162 % |
| AttackSpeed | 140 % |
| CastSpeed | 140 % |
| CooldownReduction | 40 % |
| CriticalChance | 35 % |
| PhysicalEvasion | 30 % |
| ElementalEvasion | 30 % |
| MovementSpeed | 130 % |

Rango esperado de potencia por canal:

```text
Citizen inicial mal alineado:   25–40
Citizen inicial bien equipado:  60–85
Citizen avanzado:               90–120
Citizen cercano al techo:      130–150
```

Los coeficientes de técnica y las defensas del objetivo determinarán el daño final. El bestiario no debe balancearse usando directamente `PhysicalChannelPower` como si fuera daño infligido.


## 11. Desglose para UI

Todo stat mostrado debe poder explicar su procedencia.

Ejemplo:

```text
POTENCIA FÍSICA: 84.00

Body base                 60.00
Apoyo de equipamiento    +10.00
Body efectivo             70.00
Transferencia del arma    ×1.20
Competencia               ×1.00
Condición                  ×1.00
Apoyo de ciudad            ×1.00
────────────────────────────────
Potencia física            84.00
```

La UI no debe mostrar `84 de daño` hasta que se aplique una técnica y sus coeficientes.

---

## 12. Guardarraíles

- No guardar stats derivados como autoridad persistente.
- No modificar el Cubo al equipar objetos.
- No convertir afinidad o expresión física en niveles.
- No crear `FireDamage`, `WaterDamage` o resonancias separadas por elemento.
- No crear `PhysicalDamage` como competencia independiente.
- No aplicar penalización directa de daño por usar una familia extranjera; la penalización inicial es de aprendizaje.
- No mezclar herramientas profesionales con familias de armas.
- No añadir masa, integridad, desgaste, heridas o hambre a las fórmulas hasta documentar su efecto.
- No fijar coeficientes de técnicas dentro del calculador general.
- No permitir que porcentajes superen sus caps.
- No utilizar `_Process` para recalcular todos los Citizens continuamente.
- Calcular bajo demanda, por eventos o por lotes.

