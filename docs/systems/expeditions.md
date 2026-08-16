# Expediciones

## Qué es

Una expedición es un compromiso de la ciudad: personas reales salen, tardan
tiempo de mundo, se encuentran con algo, alcanzan un objetivo y **regresan**.
No es un temporizador que convierte tiempo en recursos, y no es una escena
aparte del mundo: la ciudad sigue avanzando mientras ocurre.

## Qué problema jugable resuelve

Es la mitad exterior del juego. Abre rutas, revela recursos, contacta
poblaciones, habilita territorio y produce las consecuencias personales
—heridas, experiencia, pérdida de carga— que la ciudad tiene que absorber.
El jugador la configura; no la pilota.

## Autoridad

| Concepto | Autoridad |
| --- | --- |
| Plan, equipo, suministro, postura, objetivo | `ExpeditionRequest` |
| Estado y fases | `Expedition` (`ExpeditionPhase`, `ExpeditionStatus`) |
| Avance del viaje | `Travel.PositionX`, derivado del reloj de mundo |
| Encuentro | `CombatSession` propiedad de `CityWorld` |
| Consecuencias sobre personas | `CombatExpeditionService` (único escritor) |
| Reglas de oportunidad/ruta | `ResourceExpeditionRules` |
| Presentación del camino | `ExpeditionPathRenderer` + `ExpeditionPathComposition` + `ExpeditionPathCamera` |
| Vista | `ExpeditionLiveView`, `ExpeditionStage`, `ExpeditionRail` |

## Cadena de fases

```text
Outbound → Encounter → Objective | Retreating → Returning → Resolved
```

La expedición no termina al alcanzar el objetivo: el regreso es parte del
contrato y puede encontrar enemigos, rutas modificadas, escasez, heridos y
pérdida de carga.

## Un solo reloj

Existe **un** reloj de mundo. Ciudad, viaje y combate avanzan sobre esa misma
línea temporal, en paralelo.

- El mundo no se puede pausar. Las velocidades globales son `1x`, `2x` y `4x`.
- Abrir o cerrar `ExpeditionLiveView` es un cambio de presentación: no crea
  una segunda simulación, no pausa nada y conserva la velocidad seleccionada.
- Un menú o modal puede capturar input y cubrir la escena, pero nunca congela
  el dominio.
- La progresión offline usa el mismo reloj y las mismas transiciones de
  dominio; no existe un reloj expedicionario separado.

## Gramática espacial

Ciudad y expedición comparten **la misma gramática de bandas de profundidad**:
una proyección 2D que simula perspectiva pseudo-3D con bandas trapezoidales no
uniformes. No es 2.5D ni 3D. El contrato completo, con sus invariantes
registradas, está en
[`../engineering/architecture.md`](../engineering/architecture.md) §10.

Lo que importa a nivel de sistema:

- **Comparten vocabulario, no instancias.** `MacroStreetRenderer` sigue siendo
  urbano; `ExpeditionPathRenderer` no sabe qué es un solar, un edificio, la
  navegación ni el territorio. Ninguno recibe un booleano de configuración del
  tipo `isExpedition`.
- **El gameplay es 1D.** `Travel.PositionX` durante el viaje y
  `Combatant.PositionX` durante el encuentro son las únicas posiciones
  autoritativas. La profundidad visual es estado de presentación.
- **El scroll del mundo es derivado.** El desplazamiento del camino y el
  parallax por capas son funciones de esas posiciones; nunca se persiste un
  offset paralelo ni se conduce con un reloj propio.
- **El camino es visualmente infinito por reciclado.** Un anillo acotado de
  chunks (`ExpeditionPathChunkPool`: 7 chunks de 256 unidades, con el foco en
  el central) se recicla al desplazarse, de modo que la memoria queda acotada
  y **ningún chunk entra en persistencia**.
- **La party mantiene un foco estable.** Durante el viaje el grupo se queda
  cerca de un punto focal de la banda jugable y es el mundo el que se
  desplaza bajo él.
- **Viaje y encuentro ocurren sobre el mismo stage.** No hay campo de batalla
  lateral ni stage de reserva; la única diferencia entre viaje y encuentro es
  la política de cámara (`FollowTravel` frente a `FrameEncounter`).
- **Una sola banda jugable, y los consumidores la preguntan.** Terreno, party,
  enemigos, objetivo y decorado resuelven su fila a través del renderer. Se
  derivó dos veces una vez, y el resultado fue un camino en el horizonte con la
  party caminando al lado.

El decorado por bioma y el parallax son presentación derivada del mismo offset:
no crean estado mecánico nuevo.

## Preparación

El jugador configura miembros, formación, posiciones, roles, habilidades
automáticas, prioridades, equipo, suministros, retirada, protección de heridos
y objetivo.

- Sólo participan ciudadanos **incorporados explícitamente como héroes**. No
  existe un segundo tipo de trabajador expedicionario anónimo.
- La vanguardia futura admite como máximo **cuatro `Citizen`**. La superficie
  muestra cuatro slots; el primer Spirit Trail es Founder-only y los slots 2–4
  se ven bloqueados, para enseñar la capacidad sin inventar miembros.
  `ExpeditionRequest.MaxTeamSize` sigue aceptando 1–2 miembros: es estado de
  implementación, no el contrato objetivo.
- La superficie de habilidades muestra cuatro octágonos. Sólo
  `expedition_skill_1` está conectada; `expedition_skill_2`–`4` son no-ops
  legales y bloqueadas. Cada octágono conserva sus ocho lados porque cada lado
  alojará un Trait; esa reserva geométrica no autoriza a implementar Traits.
- `RETIRADA` está presente y deshabilitada.

## Combate

No hay control directo de movimiento. El resultado depende de equipo,
competencias, linaje, experiencia, formación, salud, recursos, decisiones y
entorno, y es determinista a partir de una semilla y del estado persistido.

- **Basic Attack es automática** y no ocupa un input de habilidad.
- **El desplazamiento es automático.** Un combatiente avanza sólo hasta entrar
  en su `AttackRange` y, una vez puede atacar, no retrocede para conservar
  rango: **un combatiente a distancia no kitea**.
- **Knockback** puede desplazar a un combatiente ya en rango, pero **sólo lo
  produce un golpe que aplica `Knockdown`**: es la consecuencia de una expresión
  física, no de cualquier impacto. `Stability` reduce ese desplazamiento,
  `Impulse` puede aumentarlo y la proporción física del golpe lo escala. El
  empujón menor de un impacto sólido es una reacción de impacto de presentación
  y no toca el dominio. Ver
  [`statistics-and-combat.md`](statistics-and-combat.md) §2.3. Los coeficientes
  son provisionales y viven en `CombatBalanceConfig`, en dominio determinista,
  nunca en la animación.
- **Un golpe puede fallar.** El objetivo tira su evasión —mezclada por la
  proporción física de la técnica— y un golpe evadido no ocurre: no critica, no
  se mitiga, no aplica expresión y no desplaza.
- **Una expresión física puede ser rechazada.** `ControlPower` del atacante
  contra `ControlResistance` del objetivo. Nunca es seguro y nunca es
  imposible. Ver [`statistics-and-combat.md`](statistics-and-combat.md) §8.4.
- El motor no depende de nodos, escenas, animaciones ni frame rate:
  `CombatDebugPanel` y `ExpeditionLiveView` **observan** la misma sesión sin
  poseerla.
- La sesión activa persiste su paso lógico y el historial reproducible de
  comandos AUTO/manual, así que un save a mitad de encuentro se reanuda sin
  volver a tirar el resultado.

No forman parte del combate actual: Traits, Chains, carroza, la acción en
`SPACE`, formación avanzada y Skills 2–4 funcionales. La profundidad de
técnicas, crítico, penetración y resistencias está trazada en
[#38](https://github.com/3FE3LE/world-of-goses/issues/38) y el escalado de
bestiario en [#43](https://github.com/3FE3LE/world-of-goses/issues/43).

## El primer Spirit Trail

Es la primera expedición de la partida y la que enseña el pilar completo,
dentro de los primeros cinco minutos aproximados de gameplay:

```text
onboarding astral → primera noche → amanecer / SpiritDeparted
→ Spirit Trail disponible → primera expedición del Founder
→ primer encuentro visual → continuación hacia el objetivo → regreso
```

Contrato de `ResourceOpportunityKind.SpiritTrailSearch`:

- Se desbloquea cuando `WorldEventKind.SpiritDeparted` está en la crónica, es
  decir cuando la primera noche concluyó. **Está exento** del gate
  Campfire + Cache que sí conservan las salidas materiales de recursos.
- Dura aproximadamente **cuatro horas de mundo** y **no consume Food** por
  existir: una ruta larga no es lo mismo que una ruta caza.
- Su resultado es `Discovery`, no un recurso material. La recompensa material
  definitiva permanece abierta y no debe inventarse una conversión provisional
  para llenar ese vacío. En particular ya no representa `1 Food → Wood`.
- Dispara su encuentro tras media hora de mundo, continúa hasta una traza
  física del espíritu y regresa visiblemente a la ciudad.
- Es Founder-only.
- Lleva un suelo estadístico de tutorial que es **propiedad de la ruta, no de
  la party**: protege al fundador tanto si llega armado como si no.

### Vivacidad del progreso

Un fracaso en esa primera salida sólo registra una herida persistente si la
ciudad ya puede tratarla: un refugio completado más el coste del tratamiento en
Food no reservada (`WoundRules.CanCityCarryWound`). El Spirit Trail regresa
antes de que exista cualquiera de las dos cosas, así que la regla ordinaria
dejaba al único fundador herido, indisponible para recolectar, construir y
salir, y sin forma de alcanzar el refugio ni la comida que el tratamiento
necesita. El fracaso sigue costando tiempo, salud, condición y recompensa. En
cuanto la ciudad está equipada, todas las reglas ordinarias vuelven a aplicar,
coste de tratamiento incluido.

## Oportunidades materiales

Campfire + Cache habilitan una oportunidad finita de Food y una de Wood. El
despacho reserva atómicamente la oportunidad, el suministro y la capacidad de
retorno acotada; la cancelación o la retirada las liberan y un objetivo
completado las agota. Repetirlas indefinidamente no puede sostener el
crecimiento: más fuentes exigen reconocimiento, seguridad de ruta o acceso
territorial.

Toda oportunidad tiene identidad de ruta o celda, reserva restante, ventana de
disponibilidad y requisito de acceso, y todo eso se persiste. Un objetivo sin
esas propiedades es un botón de recursos disfrazado.

## Derrota y equipo

Los habitantes vivos regresan sin equipo y con sus heridas; la ciudad debe
tratarlos. El equipo se fabrica en la ciudad, tiene calidad según planos,
materiales y producción, y puede perderse. La carga sólo regresa si el grupo
logra transportarla.

## Cómo se manifiesta para el jugador

- **`ExpeditionRail`**, persistente a la derecha, proyecta fase, miembros,
  suministro y tiempos reales de las expediciones activas, más las cuatro
  entradas significativas más recientes de la crónica. No muestra cola porque
  no existe cola.
- **`ExpeditionLiveView`** ocupa la pantalla cuando el jugador elige `VER`
  sobre una expedición activa: oculta el macro, ambos resúmenes laterales y los
  docks sin liberarlos, y conserva la barra de estado global. Proyecta ruta,
  miembros reales, salud/stamina, posiciones, HP y feedback de
  ataque/skill/impacto/knockback reutilizando `CombatantView`.
- **`ExpeditionPanel`** conserva la planificación y el despacho.
- Los sprites de expedición son los más detallados del juego; hoy siguen siendo
  provisionales.

## Territorio

Una expedición aumenta conocimiento, seguridad y acceso. Al completarse, el
trabajo civil puede operar por rutas seguras sin representación expedicionaria
detallada. Una ciudad nueva expone tres parcelas horizontales disponibles y
**no** renderiza frontera bloqueada ni selecciona objetivo territorial: el
borde del terrario y la adquisición causal del sobre objetivo se diseñan en
[#35](https://github.com/3FE3LE/world-of-goses/issues/35). Los registros de
parcela de saves antiguos se conservan.

## Inspiración

La referencia estructural para la legibilidad del combate automático dentro de
una franja persistente es *Taskbar Hero*. Aplica a la legibilidad, no al tipo
de stage, y no se copian assets, interfaz, personajes, nombres ni contenido.

## Animaciones prioritarias

```text
idle · walk · run · attack_basic · skill · support · heal
hit · injured · downed · carry · teleport · victory · retreat
```
