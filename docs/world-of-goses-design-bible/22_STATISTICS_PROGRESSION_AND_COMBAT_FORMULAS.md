# Sistema de estadísticas, progresión y fórmulas de combate

## Estado del documento

Versión inicial canónica para prototipo: `v0.1`.

Este documento consolida las decisiones vigentes sobre:

- Cubo Kovari.
- Naturaleza de combate del `Citizen`.
- Competencias y progresión.
- Armas y equipamiento.
- Salud, condición y apoyo de ciudad.
- Cuatro familias de estadísticas derivadas.
- Curvas, límites y casos de referencia.

Los coeficientes numéricos de esta versión son valores iniciales de balance. Deben vivir en configuración y podrán ajustarse mediante pruebas sin modificar la arquitectura del dominio.

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

### 2.4 Correspondencia conceptual de las seis caras

| Cara | Afinidad elemental | Expresión física |
|---|---|---|
| Body | Earth | Fracture |
| Bond | Aether | Poisoning |
| Stability | Water | Paralysis |
| Impulse | Fire | Stunning |
| Domain | Silence | Bleeding |
| Reach | Air | Knockdown |

En `v0.1`, esta correspondencia también determina la expresión física a
partir de la afinidad elemental al crear o migrar un `Citizen`:

```text
Earth   → Fracture
Aether  → Poisoning
Water   → Paralysis
Fire    → Stunning
Silence → Bleeding
Air     → Knockdown
```

Se persiste la afinidad como fuente inmutable y se deriva la expresión
física, evitando dos campos capaces de contradecirse. Esta relación no es un
multiplicador y no obliga a que un `Citizen` con afinidad Fire tenga Impulse
alto ni a que uno con expresión Knockdown tenga Reach alto.

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
- Una familia natural aprende al `100 %` de eficiencia.
- Una familia extranjera aprende inicialmente al `10 %` de eficiencia.
- La penalización extranjera afecta adquisición de experiencia, no aplica una reducción directa al daño.
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

Masa, integridad y desgaste pueden guardarse en los objetos, pero no participan todavía en estas fórmulas hasta contar con una especificación propia.

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

La curva es configurable y provisional. Ninguna fórmula debe codificarla fuera de la configuración central.

### 4.5 Salud y condición

`ConditionFactor` representa el estado actual del `Citizen`, no su identidad.

Rango inicial:

```text
0.50  condición grave
0.75  herido o enfermo
1.00  saludable
1.05  condición excepcional
```

La forma de derivar este factor desde heridas, enfermedades, fatiga y hambre queda pendiente. La primera implementación puede recibirlo como un valor ya resuelto.

### 4.6 Apoyo de ciudad

`CitySupportFactor` representa el apoyo operativo que recibe el `Citizen` por infraestructura, servicios y entorno urbano.

Rango inicial:

```text
0.90  entorno desfavorable
1.00  condiciones ordinarias
1.10  apoyo excelente
```

La forma de derivarlo desde edificios, políticas, salud pública, alimentación o vivienda queda pendiente. La primera implementación puede recibirlo como un valor ya resuelto.

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

Los caps son obligatorios. El equipo y la progresión acercan al límite, pero no lo superan.

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

---

## 11. Modelo de dominio sugerido

```csharp
public readonly record struct CubePair(double Left, double Right);

public sealed record CubeProfile(
    CubePair BodyBond,
    CubePair StabilityImpulse,
    CubePair DomainReach
);

public sealed record CombatNature(
    ElementalAffinity PrimaryAffinity,
    PhysicalExpression PhysicalExpression
);

public sealed record WeaponChannelProfile(
    WeaponFamily Family,
    double PhysicalTransfer,
    double ElementalResonance
);

public sealed record GearSupportProfile(
    double Body,
    double Bond,
    double Stability,
    double Impulse,
    double Domain,
    double Reach
);

public sealed record StatCalculationContext(
    int ApplicableSkillLevel,
    double ConditionFactor,
    double CitySupportFactor
);
```

Los tipos concretos pueden adaptarse a la arquitectura existente. El dominio no debe depender de nodos Godot, escenas, frame rate, input ni rutas de assets.

---

## 12. Desglose para UI

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

## 13. Guardarraíles

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

---

## 14. Pendientes

- Curva definitiva de experiencia por competencia.
- Coste de subir del nivel 0 al 20.
- Coeficientes físicos y elementales de técnicas.
- Crítico: multiplicador de daño y reglas de activación.
- Penetración física y elemental.
- Resistencias a estados físicos y elementales.
- Fórmulas de masa, integridad y desgaste.
- Derivación concreta de `ConditionFactor`.
- Derivación concreta de `CitySupportFactor`.
- Reglas de armaduras, escudos y requisitos de equipo.
- Interacción con envejecimiento.
- Escalado del bestiario y dificultad de expediciones.
