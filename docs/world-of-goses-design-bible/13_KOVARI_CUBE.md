# El Cubo Kovari — sistema de predisposiciones y stats

## Estado

Capítulo canónico del bible que define el sistema mecánico del Cubo
Kovari: las predisposiciones continuas del ciudadano, los stats
derivados, la relación con el equipamiento, la afinidad elemental y el
modo sombra de coexistencia con el scoring actual de linaje.

Fuente original consolidada: `KOVARI_CUBE_ONBOARDING_INTEGRATION_GUIDELINE.md`,
`RAVATHA_LINEAGE_SYSTEM_GUIDELINES/08_KOVARI_CUBE_STATS_AND_BUILDS_GUIDELINE.md`
y `ravatha_lore_package/10_KOVARI_CUBE_STATS_SYSTEM.md` (archivados en
`docs/_archive/ravatha-source-2026-08-04/`).

## Principio

> El ciudadano produce la capacidad. El equipo define cómo esa capacidad
> puede expresarse, cuánto exige y cuánto tiempo resiste.

El Cubo Kovari es el **lenguaje cultural** que las culturas Kovari han
construido para describir predisposiciones corporales y capacidades
continuas. No es un poder exclusivo de los Kovari: una persona de
cualquier linaje puede aprenderlo, enseñarlo o aplicarlo. Los otros
linajes simplemente han desarrollado lenguajes distintos para describir
las mismas realidades (la Brújula de Vaelun para orientación, el Relicario
de Orveth para custodia de valor, el Ciclo de Caelith para conocimiento,
etc.).

El Cubo Kovari existe en el juego porque:

1. Da una estructura coherente a los ocho linajes.
2. Diferencia afinidades sin convertirlas en clases.
3. Proporciona bonificaciones pequeñas y legibles.
4. Sirve como lenguaje compartido entre ciudad, expediciones y
   desarrollo individual.

---

## Geometría

Un cubo posee:

- **8 vértices** → los ocho linajes.
- **12 aristas** → combinaciones intermedias.
- **6 caras** → las seis afinidades elementales.
- **3 ejes** → las tres predisposiciones continuas.

> **Corrección geométrica importante.** Los ocho linajes se representan
> como **vértices**, no como aristas. El modelo se atribuye culturalmente
> a los Kovari porque lo construyeron, conservaron o popularizaron como
> mecanismo. Eso no significa que los Kovari sean propietarios de las
> fuerzas que describe.

---

## Los tres ejes

Cada eje es una pareja complementaria que suma `100` en el perfil
inicial recomendado. Los nombres canónicos son los visibles al jugador;
los nombres técnicos pueden mantenerse neutrales aunque la UI los
presente con la versión cultural.

| Eje | Polo A | Polo B |
| --- | --- | --- |
| I | **Cuerpo** (alias cultural: Sustancia) | **Vínculo** (alias cultural: Relación) |
| II | **Estabilidad** (alias cultural: Contención) | **Impulso** (alias cultural: Proyección) |
| III | **Dominio** (alias cultural: Concentración) | **Alcance** (alias cultural: Distribución) |

### Eje I — Cuerpo ↔ Vínculo

**Pregunta cultural:** ¿la acción se apoya primero en el cuerpo y la
materia, o en los vínculos entre personas y sistemas?

#### Cuerpo

Representa capacidad material, fuerza aplicada, tolerancia al esfuerzo
físico, uso corporal de herramientas, carga, contacto directo con el
entorno y resistencia estructural.

Puede contribuir a: daño físico, vida máxima, defensa física, capacidad
de carga, manejo de peso, sostenimiento de escudos, resistencia a la
fatiga física, salud base, potencia de acciones corporales.

#### Vínculo

Representa canalización elemental, interacción entre sistemas,
sincronización, transmisión de efectos, capacidad de afectar o asistir a
otros, relación entre conciencia, materia y afinidad.

Puede contribuir a: daño elemental, resistencia elemental, potencia de
curación, potencia de escudos, duración de mejoras, control de
resonancia elemental, efectos compartidos, moral, coordinación, eficacia
de apoyo.

### Eje II — Estabilidad ↔ Impulso

**Pregunta cultural:** ¿la acción busca conservar y estabilizar, o
intervenir y provocar un cambio?

#### Estabilidad

Representa conservación del rendimiento, recuperación, control de la
fatiga, resistencia a interrupciones, continuidad durante
enfrentamientos largos, regulación del cuerpo, prevención.

Puede contribuir a: reducción de daño, regeneración de vida, resistencia
a estados, resistencia a interrupción, recuperación de fatiga,
eficiencia durante expediciones largas, mantenimiento de postura y
guardia, defensa, retirada ordenada.

#### Impulso

Representa frecuencia de acción, iniciativa, velocidad de respuesta,
aceleración, activación de habilidades, capacidad de intervenir
rápidamente.

Puede contribuir a: velocidad de ataque, velocidad de lanzamiento,
recuperación de habilidades, reducción de enfriamiento, iniciativa,
esquiva, velocidad de reacción, frecuencia de acciones, potencia de
habilidades activas.

### Eje III — Dominio ↔ Alcance

**Pregunta cultural:** ¿la capacidad se reúne en un foco especializado o
se extiende a través de una red?

#### Dominio

Representa precisión, control técnico, aprovechamiento eficiente de una
herramienta, especialización, concentración de una acción, ejecución
sobre un objetivo concreto.

Puede contribuir a: precisión, probabilidad crítica, penetración, daño
crítico, eficiencia de manejo, conservación del filo o la tensión
mediante técnica, rendimiento contra un objetivo único.

#### Alcance

Representa extensión espacial, propagación, cobertura, coordinación entre
posiciones, aprovechamiento de distancia, capacidad de afectar varios
objetivos.

Puede contribuir a: distancia de ataque, área de efecto, cantidad de
objetivos, propagación elemental, cobertura de formación, rendimiento con
armas de asta o proyectiles, efectos grupales, transporte, difusión
tecnológica.

---

## Los ocho vértices

| Linaje | Eje I | Eje II | Eje III | Lectura |
| --- | --- | --- | --- | --- |
| Ardhen | Cuerpo | Estabilidad | Dominio | materia estable reunida en un punto de carga |
| Eirune | Cuerpo | Estabilidad | Alcance | vida preservada mediante redes |
| Kovari | Cuerpo | Impulso | Dominio | intervención técnica precisa |
| Vaelun | Cuerpo | Impulso | Alcance | movimiento material a través de rutas |
| Orveth | Vínculo | Estabilidad | Dominio | confianza y valor custodiados en acuerdos |
| Myrven | Vínculo | Estabilidad | Alcance | identidad sostenida por contextos y representaciones |
| Theryn | Vínculo | Impulso | Dominio | intensidad colectiva enfocada |
| Caelith | Vínculo | Impulso | Alcance | conocimiento conectado y aplicado en redes |

> **El vértice define configuración corporal, no clase.** Un ciudadano
> puede cruzar el centro de cualquier eje mediante experiencia sin
> cambiar de linaje. Un linaje Ardhen puede llegar a Cuerpo 48 / Vínculo
> 52 sin dejar de ser Ardhen.

---

## Las seis caras — afinidad elemental

Las afinidades elementales son **independientes del linaje** y
corresponden a las **seis caras** del cubo. Un mismo linaje puede
resonar con cualquier cara; un mismo vértice admite cualquier afinidad.

| Cara | Afinidad | Notas |
| --- | --- | --- |
| Cuerpo | **Tierra** | |
| Vínculo | **Éter** | |
| Estabilidad | **Agua** | |
| Impulso | **Fuego** | |
| Dominio | **Neutra** o **Silencio** | puede recibir un nombre cultural sin cambiar el identificador técnico |
| Alcance | **Aire** | |

### Regla

La afinidad **no selecciona** el linaje y el linaje **no fuerza** la
afinidad. Son válidos, entre muchos otros:

- Ardhen de Aire
- Eirune de Fuego
- Vaelun de Tierra
- Kovari sin afinidad
- Caelith de Agua

La pregunta elemental durante el onboarding sigue siendo una señal
fuerte, pero no la única contribución al cálculo.

---

## Stats derivados y desglose explícito

El Cubo no sustituye la hoja detallada de combate. Sus seis valores
alimentan estadísticas concretas y visibles. Toda estadística importante
debe poder mostrar **su desglose por fuente**, no resumirse en un único
número opaco como `Power`.

### Ofensiva

- `PhysicalDamage`, `ElementalDamage`
- `AttackSpeed`, `CastSpeed`, `CooldownReduction`
- `CriticalChance`, `CriticalDamage`, `Accuracy`
- `PhysicalPenetration`, `ElementalPenetration`

### Defensa

- `MaxHealth`, `PhysicalDefense`, `ElementalResistance`
- `DamageReduction`, `PhysicalDodge`, `ElementalDodge`
- `HealthRegeneration`, `HealingReceived`
- `InterruptionResistance`, `StatusResistance`

### Utilidad

- `HealingPower`, `ShieldPower`
- `BuffDuration`, `DebuffDuration`
- `AttackRange`, `AreaRadius`, `TargetCount`
- `ThreatGeneration`

> No todas necesitan existir en el mismo slice. La arquitectura debe
> **permitirlas** sin resumirlas en una estadística opaca.

### Matriz de contribuciones

| Polo | Estadísticas favorecidas |
| --- | --- |
| Cuerpo | daño físico, vida, defensa física, carga, manejo de peso |
| Vínculo | daño elemental, resistencia elemental, curación, escudos, resonancia |
| Estabilidad | reducción de daño, regeneración, resistencias, fatiga sostenida |
| Impulso | velocidad de ataque, lanzamiento, enfriamiento, iniciativa, esquiva |
| Dominio | precisión, crítico, penetración, técnica, objetivo único |
| Alcance | distancia, área, objetivos, propagación, cobertura |

### Transparencia — ejemplo de desglose

```text
Velocidad de ataque: 0,91 ataques/s

  Impulso del ciudadano        +0,08
  Dominio con la familia       +0,03
  Peso del arma                -0,04
  Fatiga actual                -0,02
  Condición del arma           -0,03
  Postura                      +0,08
```

La complejidad no debe ocultarse. Debe explicarse.

---

## Equipamiento — canal y exigencia, no poder

El equipamiento **no otorga ataque base ni velocidad estándar**. El
ciudadano es la fuente de poder; el arma canaliza, exige y se desgasta.

### Fórmula de rendimiento

```text
rendimiento efectivo =
    capacidad del ciudadano
  × eficiencia de manejo
  × condición del equipo
  × afinidad
```

El ciudadano aporta: fuerza, técnica, afinidad, velocidad, resistencia,
experiencia, competencia.

El arma determina: qué atributos puede canalizar, cuánto esfuerzo exige,
cuánto peso debe manejarse, cuánto desgaste soporta, cómo responde a la
afinidad elemental.

### Propiedades mínimas del equipo

| Propiedad | Función |
| --- | --- |
| `Weight` | masa transportada, acelerada y detenida; afecta fatiga, velocidad, carga y recuperación |
| `Demand` | esfuerzo técnico y físico requerido para usar la pieza correctamente |
| `MaxIntegrity` | cantidad de deterioro acumulado antes de quedar inutilizable |
| `CurrentCondition` | porcentaje de funcionamiento conservado |
| `ElementalResonance` | eficiencia con que transmite cada afinidad |
| `ElementalTolerance` | carga elemental que soporta antes de degradarse aceleradamente |
| `WearProfile` | qué partes o propiedades pierden rendimiento |

### Familias iniciales sugeridas

Pesada a dos manos, una mano equilibrada, armas dobles, dagas, lanza o
asta, arco, arma y escudo, lanza y escudo. Estas familias **no son
clases** y pueden ampliarse; se documentan como vocabularios de ejemplo.

### Fatiga y desgaste

Separar:

- **Fatiga del ciudadano**: peso, exigencia, frecuencia, postura,
  duración, heridas, falta de entrenamiento.
- **Desgaste del equipo**: impactos, bloqueos, material golpeado, uso,
  afinidad, técnica deficiente, ambiente, calidad.

---

## Afinidad elemental en combate

La afinidad pertenece al **ciudadano**. El equipo responde mediante
Resonancia y Tolerancia.

```text
efecto elemental =
    potencia elemental personal
  × resonancia
  × condición
  × control técnico
```

El equipo **no contiene** el poder elemental como una batería
independiente.

### Riesgo material por afinidad

- **Fuego**: temperatura y deformación.
- **Agua**: corrosión, humedad y pérdida de tensión.
- **Tierra**: presión, vibración y fractura.
- **Aire**: torsión, vibración y desalineación.
- **Éter**: interferencia e inestabilidad.
- **Neutra**: desgaste físico sin carga elemental.

---

## Rasgos y competencias

Los rasgos mecánicos se adquieren durante la vida del ciudadano, no
durante el onboarding. Cada rasgo debe incluir:

- condición de activación,
- efecto numérico explícito,
- fuente identificable,
- duración o permanencia,
- reglas de evolución o pérdida.

### Ejemplo válido

```text
Temerario
Fuente: sobrevivió a tres expediciones con retirada tardía.
Efecto: +8 % velocidad de ataque por debajo de 40 % de vida.
Coste: -5 % reducción de daño en el mismo estado.
```

Las competencias provienen de:

- entrenamiento, experiencia, mentores, instituciones, práctica,
  heridas, decisiones.

El onboarding **no** afirma preferencia por armas que el fundador jamás
ha utilizado. Las preferencias aparecen por uso acumulado, maestría,
resultados positivos, mentores, lesiones, familiaridad y decisiones del
jugador.

---

## Combate automático

La build puede contener:

```text
ciudadano
equipo
afinidad
habilidades
postura
posición
prioridades automáticas
condición de retirada
```

Las posturas deben modificar estadísticas y prioridades explícitas:

```text
Agresiva
+15 % velocidad de ataque
+10 % daño efectivo
-8 % reducción de daño
Prioridad: objetivo con menor vida
```

No se utilizan descripciones tácticas imposibles de observar o medir.

---

## Bonificación inicial — modo sombra

> El algoritmo actual de linaje permanece como fuente de verdad durante
> la primera integración. **No reemplazarlo por el Cubo en el mismo
> refactor.**

### Reglas

1. Calcular en paralelo:
   - `CurrentLineageResult` (algoritmo actual, sin cambios)
   - `CubeVertexCandidate` (perfil continuo del cubo)

2. Persistir o registrar la comparación durante desarrollo y pruebas.
   El candidato del cubo **no modifica** el resultado mostrado.

3. Después de calcular el linaje actual, usar su vértice como inclinación
   base del cubo:

   ```text
   Polo del linaje: 60
   Polo opuesto:    40
   ```

4. Las respuestas personales pueden desplazar cada pareja dentro de un
   margen moderado:

   ```text
   ±8 puntos por eje
   ```

5. El cubo sólo podrá determinar directamente el linaje cuando:

   - exista paridad demostrada con el algoritmo actual,
   - los casos dorados mantengan su resultado,
   - los empates sean deterministas,
   - se haya probado una muestra amplia de secuencias,
   - el cambio tenga una ventaja real.

### Ejemplo

```text
Base Ardhen (vértice canónico)
  Cuerpo        60 / Vínculo        40
  Estabilidad   60 / Impulso        40
  Dominio       60 / Alcance        40

Matices del onboarding
  Cuerpo        -4
  Estabilidad   +3
  Dominio       -7

Resultado
  Cuerpo        56 / Vínculo        44
  Estabilidad   63 / Impulso        37
  Dominio       53 / Alcance        47
```

El resultado conserva el vértice Ardhen sin convertir a todos los Ardhen
en copias estadísticas.

### Scoring en el código

Mantener sistemas independientes para:

- `LineageScoring`
- `ElementScoring`
- `CubeScoring`
- `NarrativeMemory`

No codificar contribuciones dentro de la vista. Cada respuesta puede
contribuir simultáneamente a uno o varios linajes, a uno o varios polos
del cubo, a una afinidad elemental, y a un eco narrativo.

Cuando el jugador cambie una respuesta, recalcular desde cero:
limpiar acumuladores, recorrer respuestas seleccionadas, aplicar
contribuciones, calcular resultados. **No restar manualmente**
contribuciones anteriores.

Los IDs persistidos son `question_id` y `answer_id`, no índices visuales.

---

## Migración y fallback

### Datos antiguos

Los perfiles existentes pueden contener:

- `Traits`, `WeaponPreferences`, `ProfessionalAffinities`,
  `CombatStyle`, `RiskProfile`, `LeadershipStyle`,
  `PoliticalOrientation`, `SpiritualPosture`.

No eliminar campos persistidos abruptamente si existen partidas o
serialización activa.

### Estrategia

1. Marcar campos como obsoletos en dominio o DTO.
2. Dejar de generarlos en nuevas sesiones.
3. Mantener lectura compatible durante una versión de migración.
4. Generar el Cubo a partir de respuestas guardadas cuando existan.
5. Si no existen respuestas, generarlo desde el vértice del linaje con
   valores base `60/40`. **Para ciudadanos no fundadores posteriores a
   `DEC-0019`**, el vértice se desplaza `±8` por eje con FNV-1a
   (`CubeScoring.GenerateOrdinaryProfile(lineage, seed)`); el rango del
   sobre y el invariante de pareja son los mismos que el onboarding, y
   el resultado sigue siendo determinista por `(linaje, id)`.
6. Eliminar los campos obsoletos únicamente después de migrar guardados
   y pruebas.

### Fallback

```text
Sin respuestas históricas
  → fundador: usar vértice del linaje, asignar 60/40 por eje, conservar afinidad
  → ciudadano corriente: usar CubeScoring.GenerateOrdinaryProfile(lineage, id)
    para sembrar ±8 por eje con FNV-1a
```

Nunca volver a ejecutar el onboarding automáticamente sobre una partida
válida.

---

## Evolución posterior del `CubeProfile`

El `CubeProfile` debe poder cambiar mediante:

- entrenamiento, salud, edad, heridas, educación, experiencia,
  rasgos adquiridos, efectos temporales, decisiones extraordinarias.

El linaje permanece. Las coordenadas evolucionan.

---

## Guardarraíles

- **No** convertir el Cubo en seis clases disfrazadas.
- **No** aplicar penalizaciones raciales permanentes.
- **No** bloquear armas, profesiones, elementos o roles.
- **No** asignar preferencias de armas durante el onboarding.
- **No** convertir señales narrativas en rasgos mecánicos vacíos.
- **No** ocultar estadísticas derivadas detrás de un único valor de
  poder.
- **No** permitir que el equipamiento otorgue poder base independiente
  del ciudadano.
- **No** mostrar números del Cubo durante las doce elecciones.
- **No** asociar obligatoriamente un linaje con una afinidad elemental.
- **No** convertir las coordenadas iniciales en valores inmutables.
- **No** utilizar el Cubo para determinar la política futura de la
  ciudad.

---

## Criterios de aceptación

La integración del Cubo se considera correcta cuando:

1. El scoring actual de linaje se conserva hasta demostrar equivalencia.
2. El onboarding produce sólo `CubeProfile` (sin aptitudes, política,
   espiritualidad, riesgo, liderazgo, armas ni combate como output).
3. Las estadísticas derivadas son explícitas y trazables al cubo.
4. Cada estadística importante tiene desglose por fuente.
5. El ciudadano es la fuente de poder; el arma canaliza, exige y se
   desgasta.
6. El equipamiento no otorga ataque base ni velocidad base
   independientemente del ciudadano.
7. Peso y exigencia son propiedades diferentes.
8. Fatiga y desgaste son sistemas separados.
9. La afinidad pertenece al ciudadano.
10. Cualquier linaje puede desarrollar cualquier build.
11. Los rasgos nacen de experiencia real, no del onboarding.
12. El sistema soporta combate automático profundo sin ocultar sus
    números.
13. El cubo admite Tierra, Agua, Fuego, Aire, Éter y afinidad neutra sin
    ligarlas a un linaje.
14. Existen pruebas doradas, modo sombra y migración segura.
15. Ninguna decisión del onboarding prescribe el destino de la ciudad o
    del fundador.

---

## Regla final

> El onboarding no decide qué hará el fundador. Decide qué cuerpo pudo
> sostener su conciencia, con qué fuerza de Ravatha resuena y desde qué
> coordenadas comenzará a aprender.

El linaje define el vértice inicial. La afinidad define la cara
elemental con la que resuena. El Cubo describe sus predisposiciones.
**La vida posterior construye todo lo demás.**
