# Integración del Cubo Kovari en el onboarding

## Estado

Propuesta de refactor funcional para el onboarding del héroe fundador de **World of Goses**.

Este documento no reemplaza la secuencia narrativa actual. Define cómo transformar sus resultados en un perfil mecánico útil, explícito y compatible con el sistema de ciudadanos, linajes, afinidades elementales, combate automático y progresión posterior.

---

# 1. Objetivo

Integrar el Cubo Kovari como perfil inicial del fundador sin perder la predictibilidad actual del cálculo de linaje.

El onboarding debe responder únicamente:

```text
¿Qué cuerpo logró sostener la conciencia?
¿Con qué afinidad elemental resuena?
¿Desde qué predisposiciones comienza a desarrollarse?
¿Qué recuerdos astrales conserva?
```

No debe decidir:

```text
Arma preferida
Profesión futura
Clase de combate
Rol permanente
Doctrina política
Postura espiritual definitiva
Forma de gobierno
Destino de la ciudad
```

La secuencia actual ya determina correctamente el linaje en la mayoría de los casos. Ese comportamiento debe preservarse antes de intentar sustituirlo.

---

# 2. Principio central

> El onboarding determina la forma inicial del fundador, no su build final.

El resultado mecánico se reduce a tres componentes:

```text
Linaje
Afinidad elemental
Perfil del Cubo Kovari
```

El resultado narrativo conserva:

```text
Respuestas seleccionadas
Palabra que creyó escuchar
Detalle preservado
Ecos narrativos de la caída
Origen astral
Relación con el punto de impacto
```

Los rasgos, preferencias, competencias, profesiones y estilos de combate deben aparecer posteriormente como consecuencia de la vida del ciudadano.

---

# 3. El Cubo Kovari

## Geometría

Un cubo posee:

- 8 vértices;
- 12 aristas;
- 6 caras;
- 3 ejes.

Los ocho linajes ocupan los ocho vértices.

Las seis afinidades elementales pueden representarse mediante las seis caras.

Los tres ejes describen predisposiciones continuas del ciudadano.

---

# 4. Los tres ejes

Cada eje contiene dos polos complementarios.

No son barras independientes. Cada pareja debe representar una misma distribución.

Ejemplo:

```text
Cuerpo 64 / Vínculo 36
```

La suma recomendada de cada pareja es `100`.

## Eje I: Cuerpo ↔ Vínculo

### Cuerpo

Representa:

- capacidad material;
- fuerza aplicada;
- tolerancia al esfuerzo físico;
- uso corporal de herramientas;
- carga;
- contacto directo con el entorno;
- resistencia estructural del cuerpo.

Puede contribuir posteriormente a:

- daño físico;
- vida máxima;
- defensa física;
- capacidad de carga;
- manejo de peso;
- capacidad de sostener escudos;
- resistencia a fatiga física.

### Vínculo

Representa:

- canalización elemental;
- interacción entre sistemas;
- sincronización;
- transmisión de efectos;
- capacidad de afectar o asistir a otros;
- relación entre conciencia, materia y afinidad.

Puede contribuir posteriormente a:

- daño elemental;
- resistencia elemental;
- potencia de curación;
- potencia de escudos;
- duración de mejoras;
- control de resonancia elemental;
- efectos compartidos.

## Eje II: Estabilidad ↔ Impulso

### Estabilidad

Representa:

- conservación del rendimiento;
- recuperación;
- control de fatiga;
- resistencia a interrupciones;
- continuidad durante enfrentamientos largos;
- regulación del cuerpo.

Puede contribuir posteriormente a:

- reducción de daño;
- regeneración de vida;
- resistencia a estados;
- resistencia a interrupción;
- recuperación de fatiga;
- eficiencia durante expediciones largas;
- mantenimiento de postura y guardia.

### Impulso

Representa:

- frecuencia de acción;
- iniciativa;
- velocidad de respuesta;
- aceleración;
- activación de habilidades;
- capacidad de intervenir rápidamente.

Puede contribuir posteriormente a:

- velocidad de ataque;
- velocidad de lanzamiento;
- recuperación de habilidades;
- reducción de enfriamiento;
- iniciativa;
- esquiva;
- velocidad de reacción.

## Eje III: Dominio ↔ Alcance

### Dominio

Representa:

- precisión;
- control técnico;
- aprovechamiento eficiente de una herramienta;
- especialización;
- concentración de una acción;
- ejecución sobre un objetivo concreto.

Puede contribuir posteriormente a:

- precisión;
- probabilidad crítica;
- penetración;
- daño crítico;
- eficiencia de manejo;
- conservación del filo, tensión o alineación mediante técnica;
- rendimiento contra un objetivo.

### Alcance

Representa:

- extensión espacial;
- propagación;
- cobertura;
- coordinación entre posiciones;
- aprovechamiento de distancia;
- capacidad de afectar varios objetivos.

Puede contribuir posteriormente a:

- distancia de ataque;
- área de efecto;
- cantidad de objetivos;
- propagación elemental;
- cobertura de formación;
- rendimiento con armas de asta o proyectiles;
- efectos grupales.

---

# 5. Los ocho vértices

| Linaje | Eje I | Eje II | Eje III |
|---|---|---|---|
| Ardhen | Cuerpo | Estabilidad | Dominio |
| Eirune | Cuerpo | Estabilidad | Alcance |
| Kovari | Cuerpo | Impulso | Dominio |
| Vaelun | Cuerpo | Impulso | Alcance |
| Orveth | Vínculo | Estabilidad | Dominio |
| Myrven | Vínculo | Estabilidad | Alcance |
| Theryn | Vínculo | Impulso | Dominio |
| Caelith | Vínculo | Impulso | Alcance |

Esta tabla define el vértice cultural y corporal del linaje.

No determina una clase.

Un ciudadano puede desarrollar valores que crucen el centro de cualquier eje sin cambiar de linaje.

Ejemplo:

```text
Linaje corporal: Ardhen

Cuerpo 48 / Vínculo 52
Estabilidad 43 / Impulso 57
Dominio 46 / Alcance 54
```

El ciudadano continúa siendo Ardhen aunque su vida lo haya llevado hacia coordenadas cercanas a Caelith.

---

# 6. Afinidades elementales como caras

Las afinidades permanecen independientes del linaje.

| Cara del Cubo | Afinidad técnica |
|---|---|
| Cuerpo | Earth |
| Vínculo | Aether |
| Estabilidad | Water |
| Impulso | Fire |
| Dominio | None / Neutral |
| Alcance | Air |

Correspondencia recomendada para UI en español:

```text
Tierra
Éter
Agua
Fuego
Neutra
Aire
```

`None` puede recibir posteriormente un nombre cultural, como **Silencio**, sin cambiar el identificador técnico ni el scoring existente.

## Regla

La afinidad no selecciona el linaje y el linaje no fuerza la afinidad.

Son válidos, entre muchos otros:

```text
Ardhen de Aire
Eirune de Fuego
Vaelun de Tierra
Kovari sin afinidad
Caelith de Agua
```

La pregunta elemental actual puede seguir siendo la señal más fuerte, pero no debe convertirse obligatoriamente en la única contribución.

---

# 7. Nuevo contrato de salida

El resultado del onboarding debe dejar de producir como datos mecánicos:

```text
WeaponPreferences
ProfessionalAffinities
CombatStyle
PoliticalOrientation
SpiritualPosture
LeadershipStyle
RiskProfile
Traits
```

Estos conceptos pueden conservarse solo cuando exista un sistema consumidor real.

Las respuestas originales deben guardarse mediante IDs estables para que futuros sistemas puedan reinterpretarlas sin repetir el onboarding.

## Resultado recomendado

```csharp
public sealed record FounderOnboardingResult(
    LineageId Lineage,
    ElementalAffinity ElementalAffinity,
    FounderCubeProfile CubeProfile,
    FounderNarrativeMemory NarrativeMemory
);

public sealed record FounderCubeProfile(
    int Body,
    int Bond,
    int Stability,
    int Impulse,
    int Mastery,
    int Reach
);

public sealed record FounderNarrativeMemory(
    IReadOnlyList<string> AnswerIds,
    string? BelievedFinalWordId,
    string? PreservedDetailId,
    IReadOnlyList<string> EchoIds
);
```

Los nombres exactos deben respetar las convenciones actuales del repositorio.

No crear una entidad separada del tipo `FounderEntity`.

El resultado debe integrarse en el `Citizen` fundador.

---

# 8. Rasgos y ecos narrativos

## Eliminar rasgos decorativos

Palabras como:

```text
Protector
Observador
Adaptable
Decidido
Reflexivo
```

no deben persistirse como rasgos mecánicos si no tienen:

- condición de activación;
- efecto numérico explícito;
- fuente identificable;
- duración o permanencia;
- reglas de evolución o pérdida.

## Sustitución

Las señales de capacidad se convierten en contribuciones al Cubo.

Las decisiones memorables se convierten en `NarrativeEchoes`.

Ejemplo:

```text
Intentó conservar la dirección de la fractura.
Creyó escuchar “Regresa”.
Protegió la claridad de su conciencia durante el impacto.
```

Estos ecos pueden alimentar:

- prólogo personalizado;
- recuerdos;
- diálogos;
- encuentros con la conciencia perdida;
- eventos asociados a la caída.

## Rasgos reales

Los rasgos mecánicos se adquieren posteriormente mediante:

- experiencias repetidas;
- heridas;
- decisiones;
- entrenamiento;
- relaciones;
- traumas;
- éxitos;
- fracasos;
- envejecimiento;
- instituciones.

Ejemplo válido:

```text
Temerario
Fuente: sobrevivió a tres expediciones con retirada tardía.
Efecto: +8 % velocidad de ataque por debajo de 40 % de vida.
Coste: -5 % reducción de daño en el mismo estado.
```

---

# 9. Preferencias de armas

El onboarding no debe asignar preferencias de armas.

El fundador todavía no ha usado armas mortales y no puede tener una preferencia formada por experiencia.

El Cubo puede hacer ciertas familias inicialmente más compatibles, pero no favoritas.

Ejemplo:

```text
Cuerpo alto
Estabilidad alta
Dominio medio
```

Esto puede permitir un buen desempeño inicial con equipamiento pesado, pero no significa:

```text
Arma preferida: martillo
```

Las preferencias deben aparecer posteriormente por:

- uso acumulado;
- maestría;
- resultados positivos;
- mentores;
- lesiones;
- familiaridad;
- decisiones del jugador.

---

# 10. Relación con equipamiento

El equipamiento no otorga ataque base ni velocidad base.

El ciudadano aporta:

```text
fuerza
técnica
afinidad
velocidad
resistencia
experiencia
```

El arma determina:

```text
qué atributos puede canalizar
cuánto esfuerzo exige
cuánto peso debe manejarse
cuánto desgaste soporta
cómo responde a la afinidad elemental
```

## Propiedades mínimas del equipo

```text
Weight
Demand
MaxIntegrity
CurrentCondition
ElementalResonance
ElementalTolerance
```

El Cubo puede intervenir en la compatibilidad:

| Propiedad | Atributos relevantes |
|---|---|
| Manejo de peso | Cuerpo + Estabilidad |
| Frecuencia de uso | Impulso + Estabilidad |
| Técnica | Dominio |
| Distancia o cobertura | Alcance |
| Canalización elemental | Vínculo |
| Resistencia a sobrecarga | Vínculo + Estabilidad |

El arma no reemplaza las estadísticas del ciudadano.

Actúa como canal, exigencia y límite material.

---

# 11. Estadísticas derivadas explícitas

El Cubo no sustituye la hoja detallada de combate.

Sus seis valores alimentan estadísticas concretas y visibles.

## Ofensiva

```text
PhysicalDamage
ElementalDamage
AttackSpeed
CastSpeed
CooldownReduction
CriticalChance
CriticalDamage
Accuracy
PhysicalPenetration
ElementalPenetration
```

## Defensa

```text
MaxHealth
PhysicalDefense
ElementalResistance
DamageReduction
PhysicalDodge
ElementalDodge
HealthRegeneration
HealingReceived
InterruptionResistance
StatusResistance
```

## Utilidad

```text
HealingPower
ShieldPower
BuffDuration
DebuffDuration
AttackRange
AreaRadius
TargetCount
ThreatGeneration
```

No todas deben implementarse durante el mismo slice.

La arquitectura debe permitirlas sin resumirlas en una estadística opaca como `Power`.

## Matriz de contribuciones

| Cubo | Estadísticas favorecidas |
|---|---|
| Cuerpo | daño físico, vida, defensa física, carga, manejo de peso |
| Vínculo | daño elemental, resistencia elemental, curación, escudos, resonancia |
| Estabilidad | reducción de daño, regeneración, resistencias, fatiga sostenida |
| Impulso | velocidad de ataque, lanzamiento, enfriamiento, iniciativa, esquiva |
| Dominio | precisión, crítico, penetración, técnica, objetivo único |
| Alcance | distancia, área, objetivos, propagación, cobertura |

## Transparencia

Cada estadística final debe poder mostrar su desglose.

Ejemplo:

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

# 12. Estrategia de scoring

## Separación obligatoria

Mantener sistemas independientes para:

```text
LineageScoring
ElementScoring
CubeScoring
NarrativeMemory
```

No codificar contribuciones dentro de la vista.

## Contribución conceptual

```csharp
public sealed record ScoreContribution(
    FounderScoreAxis Axis,
    string ValueId,
    int Weight
);
```

Cada respuesta puede contribuir simultáneamente a:

- uno o varios linajes;
- uno o varios polos del Cubo;
- una afinidad elemental;
- un eco narrativo.

## Recalcular desde cero

Cuando el jugador cambie una respuesta:

```text
limpiar acumuladores
recorrer respuestas seleccionadas
aplicar contribuciones
calcular resultados
```

No restar manualmente contribuciones anteriores.

## IDs estables

Guardar:

```text
question_id
answer_id
```

No usar índices visuales como identidad persistente.

---

# 13. Conservación de la predictibilidad del linaje

## Regla principal

El algoritmo actual de linaje permanece como fuente de verdad durante la primera integración.

No reemplazarlo por el Cubo en el mismo refactor.

## Modo sombra

Calcular en paralelo:

```text
CurrentLineageResult
CubeVertexCandidate
```

Persistir o registrar la comparación durante desarrollo y pruebas.

El candidato del Cubo no modifica el resultado mostrado.

## Anclaje del perfil

Después de calcular el linaje actual, utilizar su vértice como inclinación base.

Valor recomendado inicial:

```text
Polo del linaje: 60
Polo opuesto: 40
```

Las respuestas personales pueden desplazar cada pareja dentro de un margen moderado.

Rango recomendado durante onboarding:

```text
±8 puntos por eje
```

Ejemplo:

```text
Base Ardhen
Cuerpo 60 / Vínculo 40
Estabilidad 60 / Impulso 40
Dominio 60 / Alcance 40

Matices del onboarding
Cuerpo -4
Estabilidad +3
Dominio -7

Resultado
Cuerpo 56 / Vínculo 44
Estabilidad 63 / Impulso 37
Dominio 53 / Alcance 47
```

El resultado conserva el vértice Ardhen sin convertir a todos los Ardhen en copias estadísticas.

## Reemplazo futuro opcional

Solo considerar que el Cubo determine directamente el linaje cuando:

- exista paridad demostrada con el algoritmo actual;
- los casos dorados mantengan su resultado;
- los empates sean deterministas;
- se haya probado una muestra amplia de secuencias;
- el cambio tenga una ventaja real.

No existe obligación de sustituir el algoritmo actual si ambos pueden coexistir correctamente.

---

# 14. Traducción de señales actuales al Cubo

Las etiquetas existentes pueden mapearse de forma orientativa.

## Cuerpo

```text
peso
esfuerzo
manual
obra
resistencia corporal
protección física
sostener
```

## Vínculo

```text
apego
reciprocidad
empatía
memoria compartida
protección de otros
relación invisible
cohesión
```

## Estabilidad

```text
autocontrol
paciencia
continuidad
preservación
responsabilidad
control
resistencia al cambio
```

## Impulso

```text
riesgo
audacia
transformación
reinvención
iniciativa
asalto directo
aceptación del cambio
```

## Dominio

```text
precisión
observación
análisis
patrón
detalle
claridad
control local
```

## Alcance

```text
orientación
camino
movilidad
propagación
adaptabilidad
relación espacial
regreso
```

Estas equivalencias deben configurarse en contenido, no enterrarse en código de presentación.

---

# 15. Pantalla final del onboarding

## Mostrar

```text
Nombre
Presentación corporal
Sprite
Linaje
Afinidad elemental
Tres ejes del Cubo
Resumen narrativo breve
```

## No mostrar

```text
Arma preferida
Profesión recomendada
Clase
Rol de expedición
Ideología
Destino político
Rasgos sin mecánica
```

## Ejemplo

```text
AREL

Linaje corporal
ARDHEN

Afinidad
AIRE

PERFIL DE ENCARNACIÓN

Cuerpo       56 / 44 Vínculo
Estabilidad  63 / 37 Impulso
Dominio      53 / 47 Alcance
```

Resumen posible:

> La nueva forma responde con facilidad al esfuerzo material, conserva su rendimiento bajo presión y concentra sus acciones en objetivos precisos. El Aire resuena en ella sin determinar el camino que elegirá después de la caída.

El resumen debe describir el perfil, no prometer una profesión o estilo de juego.

---

# 16. Integración con Citizen

El fundador se crea como un `Citizen` normal con metadata excepcional.

## Datos persistentes sugeridos

```text
LineageId
ElementalAffinity
CubeProfile
Origin
History
Tags
Recognitions
FounderMetadata
FallSiteRelation
OnboardingAnswerIds
NarrativeMemory
```

## No crear

```text
FounderEntity
HeroEntity
AstralCitizen
```

La excepcionalidad pertenece a metadata e historia, no a una jerarquía paralela de entidades.

## Evolución posterior

El `CubeProfile` debe poder cambiar mediante:

- entrenamiento;
- salud;
- edad;
- heridas;
- educación;
- experiencia;
- rasgos adquiridos;
- efectos temporales;
- decisiones extraordinarias.

El linaje permanece.

Las coordenadas evolucionan.

---

# 17. Compatibilidad y migración

## Datos antiguos

Los perfiles existentes pueden contener:

```text
Traits
WeaponPreferences
ProfessionalAffinities
CombatStyle
RiskProfile
LeadershipStyle
PoliticalOrientation
SpiritualPosture
```

No eliminar campos persistidos abruptamente si existen partidas o serialización activa.

## Estrategia

1. Marcar campos como obsoletos en dominio o DTO.
2. Dejar de generarlos en nuevas sesiones.
3. Mantener lectura compatible durante una versión de migración.
4. Generar el Cubo a partir de respuestas guardadas cuando existan.
5. Si no existen respuestas, generarlo desde el vértice del linaje con valores base `60/40`.
6. Eliminar los campos obsoletos únicamente después de migrar guardados y pruebas.

## Fallback

```text
Sin respuestas históricas
→ usar vértice del linaje
→ asignar 60/40 por eje
→ conservar afinidad existente
```

Nunca volver a ejecutar onboarding automáticamente sobre una partida válida.

---

# 18. Plan de implementación

## Fase 0: Congelar comportamiento actual

- Crear casos dorados del scoring de linaje.
- Crear casos dorados de afinidad elemental.
- Registrar secuencias conocidas por cada linaje.
- Registrar empates y desempates actuales.

## Fase 1: Modelo del Cubo

- Añadir tipos de dominio.
- Añadir scoring configurable por polos.
- Calcular el Cubo en paralelo.
- No cambiar todavía la pantalla final.

## Fase 2: Modo sombra

- Comparar candidato del Cubo con linaje actual.
- Registrar discrepancias.
- Ajustar contribuciones sin modificar producción.

## Fase 3: Nuevo resultado

- Sustituir rasgos, armas y profesiones por el perfil del Cubo.
- Mostrar los tres pares.
- Conservar linaje y afinidad actuales.
- Conservar memorias narrativas.

## Fase 4: Integración Citizen

- Persistir `CubeProfile`.
- Añadir migración y fallback.
- Garantizar creación única del fundador.

## Fase 5: Estadísticas derivadas

- Conectar los ejes con estadísticas concretas.
- Mostrar desgloses.
- Evitar fórmulas finales hasta definir combate y balance.

## Fase 6: Limpieza

- Retirar resultados obsoletos.
- Eliminar código muerto.
- Actualizar documentación.
- Mantener compatibilidad de guardado según versión.

---

# 19. Pruebas mínimas

## Scoring

1. Las secuencias doradas mantienen el linaje actual.
2. La afinidad elemental se conserva.
3. Cada perfil del Cubo suma `100` por pareja.
4. El perfil permanece dentro de los límites configurados.
5. Dos secuencias del mismo linaje pueden producir perfiles diferentes.
6. El Cubo no cambia el linaje durante modo sombra.
7. Los empates mantienen el desempate actual.
8. Volver y cambiar respuestas recalcula todo desde cero.

## Resultado

9. No aparecen preferencias de armas.
10. No aparecen afinidades profesionales.
11. No aparecen rasgos sin efecto.
12. Se muestran linaje, elemento y tres ejes.
13. El resumen no prescribe gameplay futuro.

## Persistencia

14. El fundador se crea una sola vez.
15. El Cubo se guarda dentro de `Citizen`.
16. Un guardado antiguo obtiene fallback válido.
17. Un fallo de guardado no duplica al fundador.
18. Las respuestas se conservan mediante IDs estables.

## Dominio

19. La vista no contiene pesos de scoring.
20. El contenido no depende de nodos Godot.
21. El Cubo puede probarse sin cargar escenas.
22. La afinidad elemental permanece independiente del linaje.

---

# 20. Guardarraíles

- No reemplazar el scoring de linaje que ya funciona sin pruebas de paridad.
- No convertir el Cubo en seis clases disfrazadas.
- No aplicar penalizaciones raciales permanentes.
- No bloquear armas, profesiones, elementos o roles.
- No asignar preferencias de armas durante el onboarding.
- No convertir señales narrativas en rasgos mecánicos vacíos.
- No ocultar estadísticas derivadas detrás de un único valor de poder.
- No permitir que el equipamiento otorgue poder base independiente del ciudadano.
- No mostrar números del Cubo durante las doce elecciones.
- No asociar obligatoriamente un linaje con una afinidad elemental.
- No convertir las coordenadas iniciales en valores inmutables.
- No utilizar el Cubo para determinar la política futura de la ciudad.

---

# 21. Criterios de aceptación

La integración se considera correcta cuando:

1. La secuencia narrativa de doce elecciones permanece funcional.
2. El linaje continúa siendo tan predecible como antes.
3. La afinidad elemental conserva su resultado actual.
4. El onboarding produce un perfil continuo del Cubo.
5. Cada eje se muestra como una pareja complementaria.
6. Las preferencias de armas desaparecen del resultado.
7. Las afinidades profesionales desaparecen del resultado.
8. Los rasgos decorativos se sustituyen por Cubo o ecos narrativos.
9. El fundador se crea como `Citizen`.
10. El Cubo queda persistido y puede evolucionar después.
11. Las estadísticas de combate pueden rastrear sus contribuciones al Cubo.
12. El equipamiento sigue funcionando como canal y exigencia, no como fuente autónoma de poder.
13. El sistema admite Tierra, Agua, Fuego, Aire, Éter y afinidad neutra sin ligarlas a un linaje.
14. Existen pruebas doradas, modo sombra y migración segura.
15. Ninguna decisión del onboarding prescribe el destino de la ciudad o del fundador.

---

# 22. Regla final

> El onboarding no decide qué hará el fundador. Decide qué cuerpo pudo sostener su conciencia, con qué fuerza de Ravatha resuena y desde qué coordenadas comenzará a aprender.

El linaje define el vértice inicial.

La afinidad define la cara elemental con la que resuena.

El Cubo describe sus predisposiciones.

La vida posterior construye todo lo demás.
