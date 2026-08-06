# Changelog

Narrative history of what the connected game gained, lost or reshaped, one
entry per increment. It answers *how we got here*; three neighbours answer
other questions and must not be duplicated into this file:

- `docs/CURRENT_STATUS.md` — what the code does **today** and what is approved
  next.
- `docs/ai/CURRENT_DEVELOPMENT_STATE.md` — the do-not-regress inventory.
- `docs/session-state/STATE.txt` — the generated, machine-measured baseline of
  the session in progress.

**Contract.** Every session that produces a commit adds or extends the entry
for its increment, in the same commit. An entry states what a player can now
do that they could not before, the schema range it crossed, and the measured
baseline — not a list of touched files, which `git log` already owns.

Entries dated before 2026-08-03 were reconstructed from commit subjects when
this file was introduced. They are deliberately thin: their detail was never
written down at the time, and inventing it now would be fabrication. Read
their commits for the real content.

---

## El onboarding cabe en la pantalla y la forma reconstruida se presenta como ficha

**2026-08-06** · schema v31 (sin cambio)

La primera pantalla que ve un jugador nuevo estaba rota por abajo. El paso de
identidad medía unos 775 px contra un presupuesto de 656, y como el contenedor
no tenía ningún hijo que se expandiera verticalmente ni un `ScrollContainer`,
Godot no recortaba ni desplazaba: el pie con `Atrás` y `Conservar este nombre`
simplemente salía de la pantalla. El jugador podía nombrar a su fundador y no
tener forma visible de confirmarlo.

Alrededor de ese fallo había desperdicio: cuatro botones de opción forzados a
66 px cuando su altura natural son 34, suelos de 92 px y 52 px en dos etiquetas
que a menudo no mostraban nada, y un selector de presentación corporal heredado
de un helper con `ExpandFill` que, anidado en una columna expansiva, daba dos
botones de ~510 px — el 81 % del ancho para una elección binaria.

Ahora el onboarding **cabe por construcción** en los tres pasos, medido contra
las cadenas más largas de ambos catálogos: 506 px en las preguntas, 567 en el
nombrado y 510 en la ficha. Dos espaciadores expansivos absorben la holgura, y
las filas vacías se ocultan en vez de vaciarse, porque una etiqueta vacía sigue
reclamando su altura mínima y su separación.

Lo que el jugador gana además de poder terminar: la elección seleccionada se
reconoce por un glifo de confirmación y no solo por el color, y las cuatro
opciones comparten un `ButtonGroup`, así que teclado y mando las recorren como
un único control. La presentación corporal es ahora un control compacto de
304 px. Y la forma reconstruida se lee en **su propia pantalla**: el bloque de
once líneas que se apretaba bajo el campo de nombre es una ficha con linaje,
afinidad, expresión física y los tres ejes del Cubo como barras de dos polos,
cada una con sus dos enteros impresos. Siguiendo la biblia (§Pantalla final del
onboarding), la ficha deja de nombrar familias de arma: "arma preferida" está en
su lista de *no mostrar* y el código llevaba tiempo desviado de ese contrato.

Lo que el jugador todavía ve mal: la barra de estado y la fila de acciones de la
ciudad siguen visibles a través del velo astral traslúcido en los dos últimos
pasos. Es anterior a este cambio —`HeroAccessButton` ya se oculta durante el
onboarding, `CityStatusPanel` y `MacroActions` nunca lo hicieron— y queda como
el único defecto de composición sin firmar en la matriz visual.

Baseline medido: build 0 errores / 0 advertencias; tests 913 pasando, 1 omitido;
918 IDs de plantilla y 283 claves de runtime; capturas de `astral-start`,
`astral-ground`, `astral-identity` y el nuevo `astral-founder-card` a 1280×720 y
1920×1080.

---

## La primera noche del fundador deja de ser un bloqueo sin explicación

**2026-08-05** · schema v30 → v31

Un playtest se detuvo al minuto uno: talar un árbol pedía un hacha primitiva y
nada decía cómo obtenerla. El hacha existía —receta, gate, incluso botón— pero
enterrada en el panel de detalle de un refugio que todavía no existía, al final
de una cadena que nadie había explicado. El único tutorial del juego, tres
tarjetas modales, pedía "1 wood de los Forest plots" cuando la primera obra
cuesta tres ramas y dos piedras, y describía chips de la barra de estado
eliminados hacía semanas.

Lo que faltaba no era un sistema: era una causa aprendida. Este increment pone
en pie el estado de dominio de una **primera noche jugable** entre las `00:00` y
las `06:00`, donde un espíritu de fuego enseñará por qué la materia del suelo
importa. Un mundo nuevo ya nacía en el tick 0, que es Día 1 `00:00` y es noche,
así que la secuencia no necesita mover el reloj ni una segunda escena de
despertar. Nueve etapas avanzan **solo** por hechos del mundo —un módulo
terminado, un nodo de diálogo cerrado— nunca por un temporizador: nadie puede
perder el tutorial por leer despacio.

Tres decisiones sostienen eso. El tick **nunca se congela**, porque congelarlo
pararía la construcción y crearía la circularidad de no poder cumplir el hito
que el reloj detenido mide; en su lugar el amanecer es la transición de etapa, la
hora mostrada se estanca en `05:59` en vez de saltar al concluir, y la noche
difiere el calendario entero — ni ración ni frontera de día mientras corre, lo
que protege al jugador lento que cruce el tick 1200. La posición del espíritu
**no se persiste**: se deriva de la etapa, como los anclajes de edificios
derivan de su placement, porque la ciudad no guarda coordenadas visuales
autoritativas. Y el Bedroll gana por fin significado mecánico: sin él la noche
se niega a dormir, cuando hasta ahora era solo coste, trabajo y prerrequisito del
Canopy.

El camino se abrió retirando cinco defectos que la noche habría exhibido de
inmediato. El peor: al terminar el Campfire el fundador quedaba comprometido con
una obra sin trabajo activo, así que dejaba de estar disponible y el botón de
recolectar se apagaba con "fundador no disponible" — exactamente cuando toca
recoger la fibra del refugio. Solo el Canopy liberaba contribuidores; ahora lo
hace cualquier módulo, y autorizar el siguiente re-moviliza al fundador, que es
lo que la cadena venía sosteniendo por accidente. Dos superficies mentían al leer
el reloj crudo en vez de la regla de jornada, y durante toda la apertura la
interfaz anunciaba trabajo detenido mientras el fundador construía. Una API
pública drenaba madera sin comprobar el hacha. Y el reinicio suave escribía en
disco una ciudad sin recursos de suelo, cuyo Campfire era impagable.

Los saves existentes entran con la noche **ya concluida**: esas ciudades pasaron
su apertura y meterlas en la secuencia las atraparía tras hitos que no pueden
volver a cumplir. No hay regalos ni retro-tutorial.

Lo que el jugador todavía no ve: el espíritu, sus diálogos y el motivo de la
primera expedición. Quedan como fases 2 a 4 en `TO_DO.md` §3. El contrato que
las gobierna está en `docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md`, y mantiene
separados los tres niveles de guía — noche autoral, directivas derivadas del
estado real, y Camino de solo lectura — sin fusionarlos en una lista de misiones.

Baseline medido: build 0 errores / 0 advertencias; tests 879 pasando, 1 omitido
(el snapshot JSON conocido); boot headless correcto; 852 IDs de plantilla y 295
claves de runtime; 442 checks de agent context.

---

## La primera noche del fundador se vuelve jugable y motiva la primera expedición

**2026-08-06** · schema v31 (sin bump) · EG-5

Tras la introducción del estado de dominio en schema v31, la apertura
EG-A0 deja de ser una lista de pasos: la noche del fundador es ahora
una secuencia autoral de 00:00 a 06:00, guiada por un espíritu de fuego
que enseña por qué importan las ramas, las piedras y la fibra. El
jugador ya no tiene que adivinar la cadena (fogata → refugio →
primera salida); el espíritu explica la causa y la noche avanza
módulo a módulo.

### Connected

- **Diálogo del espíritu** (`Domain/FireSpiritDialogueCatalog.cs`):
  seis nodos principales (`Manifested`, `SpiritArrived`,
  `CampfireBuilt`, `ShelterBuilt`, `OtherLightTold`, `Sleeping`) con
  ocho variantes de cuerpo por linaje (48 claves `firstnight.*`).
  Las cantidades del diálogo se interpolan en runtime desde
  `FoundingSiteRules.InputsFor(module)`, de modo que un cambio de
  receta no puede volver a desfasar la guía.
- **Banda de diálogo no modal** (`FirstNightDialogueStrip.cs`) en
  `OverlayLayers.Tutorial = 50`. El strip captura clicks sólo en su
  rectángulo inferior; el resto del mundo sigue jugable mientras el
  jugador lee.
- **Espíritu como entidad visual** (`FireSpiritVisual.cs`): un anillo
  de 16 puntos y un glyph triangular, posición derivada del stage,
  nunca persistida. Antes de `CampfireBuilt` junto al fundador;
  después, sobre la fogata.
- **Brasas post-amanecer** (`FirstNightEmbers.cs`): un pequeño
  cuadrilátero naranja translúcido queda sobre la fogata cuando
  `Stage == Concluded` y `SpiritDeparted` está en el chronicle.
- **Comentario contextual** (`FirstNightContextCommentary.cs`):
  cuando el fundador recoge `Branches`/`SmallStone`/`PlantFiber`
  cerca del fogón o refugio durante la noche, el espíritu comenta
  efímeramente vía `Notifier`.
- **Event causal al amanecer**: nuevo `WorldEventKind.SpiritDeparted`
  se emite una sola vez en el cruce `Sleeping` → `Concluded`, y se
  persiste como evento significativo.
- **Motivación de la primera expedición**:
  `ResourceOpportunityKind.SpiritTrailSearch` con la misma curva de
  retorno que `FallenWoodSearch` (4 / 6 / 8 Wood) y duración 180
  ticks. El botón se desbloquea sólo cuando `SpiritDeparted` está en
  el log, vía `ExpeditionPlanningSnapshot.SpiritTrailUnlocked`.
- **Documentación canónica**: `docs/19_FIRST_NIGHT_AND_FIRE_SPIRIT.md`
  pasa de "Propuesta canónica" a "Aceptada" (DEC-0014). El bloque
  "First night" se añade a `CROSS_DOMAIN_INVARIANTS.md` y la ruta
  "First night / fire spirit" se añade a `CONTEXT_MAP.md`. Cuatro
  fixtures nuevos en la matriz visual.

### Schema

**No schema bump.** `SpiritTrailSearch` es un nuevo
`ResourceOpportunityKind` que se serializa como string
(`Enum.TryParse` ya es tolerante para valores nuevos en saves
previos). `SpiritDeparted` aparece sólo en saves que ejecuten la
secuencia completa de la noche; `WorldEventRetention.IsSignificant`
lo acepta sin migración adicional. El estado de noche en sí
(`FirstNightSave`) ya estaba en v31 desde el incremento anterior.

### Verified

- Build 0 / 0 (warnings 0).
- Tests 911 / 912 (1 omitido conocido de `VerticalLoopPersistenceTests`).
- Localization: 907 IDs plantilla, 295 claves de runtime, ningún
  dígito literal bajo el prefijo `firstnight.*` en EN ni ES.
- Agent context: 442 / 442 checks.
- `messages.pot` regenerado limpio.

## Primer circuito vertical de combate automático y expediciones

**2026-08-05**

Tres ciudadanos persistentes ya pueden equiparse, salir, pelear dos veces
automáticamente, elegir una ruta y volver con consecuencias escritas sobre las
mismas personas. El circuito completo del roadmap —`Citizen` → preparación →
estadísticas derivadas → técnicas con coeficiente físico y elemental → combate →
decisión de ruta → segundo encuentro → destino → regreso → persistencia— se
resuelve entero dentro del dominio, sin escena, sin `_Process` y de forma
reproducible desde una semilla.

Una técnica convierte potencia de canal en acción mediante dos coeficientes cuya
suma es un presupuesto fijo. Esa única invariante es la que obliga a que una
evolución **redistribuya** en lugar de regalar poder: subir el lado físico cuesta
exactamente lo que baja el elemental, y se revalida al aplicarla. El contenido son
tres grupos de módulos que se combinan —cuatro familias de arma, dos expresiones
físicas, las seis afinidades—, nunca una habilidad por combinación: doce
definiciones, no cuarenta y ocho. Stunning y Knockdown son funcionales; Knockdown
altera exposición y disponibilidad, no posición, porque este combate no tiene
dónde moverse.

La competencia gana por fin su curva de experiencia, centralizada y configurable,
con nivel derivado de la experiencia acumulada, techo de aprendizaje y la
penalización de familia extranjera al 10 % aplicada **al aprendizaje**, nunca al
resultado de la técnica. `Survival` entra como competencia profesional, exenta de
esa penalización. El `ConditionFactor` se deriva de causas persistentes —vida,
fatiga, heridas— y expone esas causas para la telemetría en lugar de asignarse a
mano.

La potencia de canal sigue llamándose potencia. Ningún campo de la telemetría la
llama daño, y ninguna estadística persistente nueva de daño físico o elemental
existe.

Deuda reconocida: el `Expedition` de EG-4 sigue siendo un temporizador de recursos
independiente; unificarlo con esta corrida de combate es el siguiente paso, no
parte de este slice. La presentación es un panel de depuración, no una pantalla de
preparación, y no hay economía de equipamiento todavía.

Baseline medido: build con 0 errores y 0 warnings; 858 pruebas aprobadas, 0
fallidas, 1 omitida (859 total); arranque headless limpio; 442 checks de contexto
aprobados y 0 fallidos; 856 IDs de localización y 337 claves runtime.

---

## La fogata se puede autorizar, construir y guardar; el Cubo llega al panel de ciudadanos

**2026-08-05**

Tres defectos de EG-5 quedaban entre el jugador y su primera fogata.

Autorizar la fogata fuera del horario laboral asignaba al fundador y no
construía nada. El campamento fundacional debe ignorar la jornada 08:00–16:00
—no hay refugio al que volver y así lo declaraba `CURRENT_STATUS.md`—, pero la
excepción sólo estaba aplicada en la simulación por tick. La movilización al
asignar, la confirmación de llegada, la frontera día/noche y la reanudación de
órdenes permanentes seguían consultando el reloj crudo. El resultado: el
fundador quedaba **en casa**, la obra se quedaba en `WorkersInTransit` y ningún
error se registraba en ninguna parte. La regla pasa a tener un único dueño,
`CityWorld.IsLaborTime()`, y las cinco compuertas la leen; ahora el fundador
sale hacia la obra a cualquier hora, la llegada no se revierte y cruzar las
16:00 no lo retira del sitio. Que la jornada deba empezar con el Ayuntamiento en
lugar del refugio queda como pregunta de producto, sin decidir aquí.

Y una obra detenida ya dice por qué. La tira de estado mostraba `Obra 0/180` sin
más: el motivo existía en el dominio (`ConstructionStopCause`) y el panel de
construcción lo describía, pero la única superficie siempre visible no lo leía
—los edificios sí exponían el suyo, las obras no—. Ahora el chip añade el motivo
y su tooltip lo explica: falta elegir módulo, contribuyente agotado, en camino,
faltan materiales, nadie contribuye. Con eso a la vista se diagnostica el caso
que parecía una obra muerta: la fogata **estaba construida** y el sitio esperaba
que el jugador eligiera el módulo siguiente, imposible de pagar con una rama en
el inventario. El juego no estaba roto; estaba mudo.

Y elegir ese módulo ya es posible de verdad. La interfaz existía —los botones de
petate, depósito y copa— pero desactivada y sin decir por qué: el coste vivía en
el snapshot y ninguna etiqueta lo mostraba. Ahora cada opción se lista con lo que
pide y lo que hay (`Petate: 2 ramas (disponible: 1) + 3 fibra vegetal
(disponible: 0)`), y el tooltip del botón detalla lo que falta. La carga del
fundador pasa a estar plegada por defecto: desplegada llenaba el cuerpo del panel
y empujaba fuera de vista la fase, el estado y justamente esos costes. Sigue a un
clic y su cabecera ya indica cuántos tipos se transportan.

La expresión física deja de ser invisible. El onboarding la decide junto con la
afinidad elemental, pero sólo la afinidad se mostraba: ni el perfil del héroe, ni
la carta de llegada del fundador, ni la carta final del onboarding la nombraban.
Ahora las tres la muestran, con las dos familias de arma que implica y una nota
de que las naturales entrenan a ritmo completo y el resto a una décima parte. La
carta de llegada además cumple por fin lo que DEC-0013 le exige —afinidad y los
tres ejes del Cubo— en una versión compacta de dos líneas, acorde a los dos
segundos que dura en pantalla. No hay pantalla de equipamiento porque no hay
equipamiento: `SetEquipmentLoadout` sólo se invoca desde pruebas y no existe
catálogo de armas. Lo que se muestra son afinidades de aprendizaje, no un
inventario, y el texto lo dice.

Talar un nodo y construir sobre el suelo que dejaba rompía el guardado. El
dominio ya trataba una unidad agotada como suelo libre —oculta su sprite y
`FrontageState` devuelve `Available`—, pero el validador de guardado contaba
toda posición autorizada como bloqueada para siempre. Las cuatro compuertas de
emplazamiento aceptaban la obra y el guardado posterior lanzaba; `TrySaveNow`
convertía la excepción en un aviso, así que la ciudad seguía viva **sin
guardar** hasta que el jugador cerraba y perdía el emplazamiento. Ahora el
validador filtra las unidades sin reserva y coincide con el dominio: el jugador
puede construir donde despejó, y la partida persiste. El esquema no cambia
—sigue en `v30`— porque la forma del JSON es la misma y la regla sólo **amplía**
lo que valida; los saves que hoy se rechazan vuelven a cargar. Como consecuencia,
devolver carga al suelo ya no puede resucitar un recurso bajo un edificio: el
parche elige la unidad elegible menos abastecida y lo que rechaza regresa al
inventario, de modo que ninguna carga se destruye.

Seleccionar al fundador en el panel de ciudadanos lanzaba
`ArgumentOutOfRangeException`. DEC-0013 dejó al fundador sin afinidades
profesionales y el panel seguía indexándolas por posición. En lugar de tapar el
hueco, el panel ahora muestra la identidad que el ciudadano sí tiene: naturaleza
de combate —afinidad, **expresión física** y las dos familias de arma naturales
que implica— y los tres pares del Cubo con su firma de linaje. La expresión
física no se veía en ninguna pantalla hasta ahora. El detalle de prospectos del
Ayuntamiento comparte el mismo bloque, lo que además retira una llamada que
lanzaba con un estilo de combate vacío. Las estadísticas derivadas siguen fuera:
exigen arma equipada y condición resuelta, y ninguna tiene origen todavía.

La firma de linaje viajaba como clave dinámica y no existía en ningún catálogo,
así que la build inglesa la mostraba en español. Ya está traducida, y una prueba
fija las ocho firmas en ambos catálogos porque el validador no puede ver una
clave que se construye en tiempo de ejecución.

Baseline medido: build con 0 errores y 0 warnings; 816 pruebas aprobadas, 0
fallidas, 1 omitida (817 total); arranque headless limpio; 442 checks de
contexto aprobados y 0 fallidos; 856 IDs de localización y 337 claves runtime.
La referencia rota a los capítulos de linaje en `CONTEXT_MAP.md`, heredada del
incremento anterior, quedó corregida para que la validación vuelva a 0 fallos.

---

## Cubo Kovari y primera derivación auditable de estadísticas

**2026-08-04**

El onboarding del fundador ahora conserva su linaje canónico, afinidad
elemental, memoria narrativa y perfil del Cubo Kovari. Al terminar, el jugador
ve los tres pares del Cubo como tendencias narrativas —sin porcentajes planos—
junto con el linaje y la afinidad. El scoring histórico de linaje continúa
decidiendo el resultado mientras el cubo se calcula en paralelo en modo sombra.

Cada `Citizen` dispone además de naturaleza de combate inmutable, competencia
por familia de arma, canales del arma, apoyos temporales de las cinco piezas de
armadura y condición resuelta. La capa de dominio puede solicitar bajo demanda
potencias física y elemental, vida, defensas, mitigaciones, regeneración,
curación y stats de tempo con un desglose auditable; equipar o retirar objetos
no muta el Cubo persistido. Afinidad y expresión física describen la
manifestación, pero no multiplican los canales.

El esquema cruza `v28 -> v29 -> v30`: primero incorpora el resultado canónico
del onboarding y después las fuentes persistentes de estadísticas. Saves
antiguos reconstruyen el Cubo desde el vértice 60/40 y conservan su afinidad;
la ausencia se normaliza a Silencio sin repetir el onboarding. Un Citizen sano
recibe condición neutral; uno herido queda explícitamente sin resolver para no
inventar una regla futura entre heridas y condición.

Baseline medido: build con 0 errores y 0 warnings; 794 pruebas aprobadas, 0
fallidas, 1 omitida (795 total); arranque headless limpio; 814 IDs de
localización y 329 claves runtime. La validación de contexto conserva 432
checks aprobados y 9 fallidos por referencias/mirrors ya desincronizados. La
captura se omitió tras reproducir un bloqueo del pipe de salida de Godot; el
snapshot Full se completó con `-SkipCapture`.

---

## Session state and changelog contract

**2026-08-03**

Infrastructure, not gameplay. `docs/session-state/` now holds a generated
`STATE.txt` and a dated `1280×720` frame of the city, and this file exists.

The problem it solves: `CURRENT_STATUS.md` and
`docs/ai/CURRENT_DEVELOPMENT_STATE.md` are written by hand and had drifted to
728 and 721 passing tests against a real 730, and to 761 template IDs against a
real 804. Both were corrected against the measurement in the same change.

`tools/New-SessionSnapshot.ps1 -Mode Fast` runs from a `SessionStart` hook and
reads git and source only, so it cannot delay a session start. `-Mode Full`
measures build, tests, headless boot, agent context and catalogs, and drives
the existing visual harness for the screenshot; it is what runs before a
session's first commit. Neither mode can abort a session: a failing probe is
recorded as a failing probe and the rest still run. Unverified fields say "not
measured this session" instead of restating the previous run.

Rule added to `CLAUDE.md` §3 / §5.1 and `AGENTS.md` §3 / §5.1 — the hook covers
Claude Code, the written rule is the only trigger under Codex.

---

## Author guard: no AI agent may appear as a contributor

**2026-08-03**

Nine commits carried a `Co-Authored-By: Claude <noreply@anthropic.com>`
trailer; three of them additionally had Claude as the **author and
committer**, which would have surfaced Claude as a GitHub contributor with
its own avatar. The remote `origin` was configured but had no branches, so
rewriting history was cheap; that window will not stay open after the first
push.

`git filter-branch` with `--all` was run once:

- `noreply@anthropic.com` is reassigned to the repository owner
  (`3l33f3@gmail.com`) wherever it appeared as `GIT_AUTHOR_*` or
  `GIT_COMMITTER_*`.
- `Co-Authored-By:` and AI-domain `Signed-off-by:` trailers are stripped
  from every message. Other body text is left alone.
- Original commit dates and content are preserved (`git diff
  refs/original/refs/heads/main HEAD --stat` is empty).

Prose in `CLAUDE.md` and `AGENTS.md` had carried the rule already. Prose
failed; prose alone is a request, not a guard. The repository now carries
`.githooks/commit-msg`, which rejects:

- Any `Co-Authored-By:` or `Signed-off-by:` trailer.
- Any `Generated with …` notice naming an AI agent.
- The robot marker `🤖`.
- Any author or committer identity whose email or display name matches an
  AI agent (anthropic.com, openai.com, GitHub-managed copilot addresses,
  or names like `Claude` / `Codex` / `Copilot`).

`tools/Install-AuthorGuardHook.ps1` points `core.hooksPath` at
`.githooks`, idempotent and safe to re-run. The snapshot script runs it on
every `-Mode Full` and reports the resulting state on its `Author guard`
line. The override `git commit --no-verify` exists; using it requires a
written reason in the final report.

The full pre-rewrite history is preserved in
`%TEMP%\wog-authorship-backup\pre-authorship-rewrite.bundle` for the day
something needs to be cross-checked, and `git reflog` still points to the
pre-rewrite `refs/original/*` copies until they expire.

---

## EG-4 — resource expeditions on a dynamic frontage grid

**2026-08-03 · `2d949f6c`**

### Connected

- The Campfire and the Cache each expose one finite Food and Wood opportunity.
  Dispatch reserves supply, opportunity and bounded return capacity; completion
  depletes it; cancellation and retreat release it.
- Mature-tree Wood requires the durable Primitive Axe, crafted at the Shelter
  from 1 Branch + 1 Small Stone and kept in its tool set. The first forestry
  capability is a made object instead of a free verb.
- Gathering rejects full storage before movement or drain, and treats a repeated
  request for an exhausted unit idempotently.
- Resource quantities left the status bar. They progress contextually from
  founder cargo in Construction, through the Founding Cache, to the Shelter's
  collapsible inventory.

### Reshaped

- The fixed nine-lot parcel partition became continuous frontage rows. A
  resource unit occupies only its own frontage cell instead of claiming the
  surrounding 3×3 lot; buildings reserve explicit column intervals guarded by
  persisted corridors; resources and constructions share one obstacle-footprint
  contract, of which trees are one case.
- Fresh cities expose three horizontal available parcels. No locked frontier is
  rendered or reconnoitred while expansion and its terrarium boundary language
  stay under design. Legacy parcel records are preserved.

### Schema

v24 → v28, one migration per seam.

| Version | Migration |
| --- | --- |
| v25 | Continuous frontage rows and persisted protected corridors. |
| v26 | Deterministic resource-unit positions that do not claim whole lots. |
| v27 | Finite Food/Wood opportunities, their expedition reservation and bounded return capacity. |
| v28 | The durable tool set, without granting tools to migrated saves. |

### Baseline

`dotnet build` 0 errors / 0 warnings · `dotnet test` 730 passed, 1 skipped ·
headless boot clean · agent context 437 checks · schema v28.

### Direction

Recorded in `docs/world-of-goses-design-bible/12_DYNAMIC_FRONTAGE_PLOTS_AND_CORRIDORS.md`,
which supersedes the rigid nine-lot partition previously described in chapter 03.

---

## Doc consolidation — Ravatha, Cubo Kovari y onboarding

**2026-08-04**

Documentation only. No code, schema, tests, build, baseline or
catalog numbers change in this commit; it captures the consolidation of
22 delivered docs (the `ravatha_lore_package`, the
`RAVATHA_LINEAGE_SYSTEM_GUIDELINES` and the
`KOVARI_CUBE_ONBOARDING_INTEGRATION_GUIDELINE`) into the canonical
design bible.

### Connected

- `bible/13_KOVARI_CUBE.md` — single source of truth for the cube
  mechanics: geometry, the three axes (Cuerpo/Vínculo,
  Estabilidad/Impulso, Dominio/Alcance) with their six canonical stat
  names and cultural aliases, the eight lineage vertices, the six
  elemental affinities (Tierra, Éter, Agua, Fuego, Neutra/Silencio,
  Aire) as independent cube faces, derived stats with explicit
  breakdown, equipment as channel-and-demand (Weight, Demand,
  MaxIntegrity, CurrentCondition, ElementalResonance,
  ElementalTolerance, WearProfile), shadow-mode coexistence with the
  current lineage scoring, migration and fallback rules.
- `bible/14-21_LINEAGES_*.md` — one chapter per lineage (Ardhen,
  Eirune, Kovari, Myrven, Vaelun, Orveth, Caelith, Theryn), each with
  §1 Cultura, §2 Sistema jugable, §3 Firma sistémica and
  §4 Vértice del Cubo. The eight line signatures are canonized:
  Anclaje, Corola, Reconfiguración, Rumbo, Custodia, Adaptación,
  Resonancia, Síntesis.
- `bible/06_LINEAGES.md` rewritten as a one-table index that links to
  the eight lineage chapters and to `bible/13_KOVARI_CUBE.md`.
- `bible/07_ONBOARDING_AND_FOUNDER.md` Result section reduced to
  `FounderOnboardingResult { Lineage, ElementalAffinity, CubeProfile,
  NarrativeMemory }`; the prologue's seven scenes (Before the Sky,
  Interference, Separation, Sky of Ravatha, Descent, Impact, Wait)
  are added as canonical narrative sequence.
- Agent and skill routing updated to point at the bible: the
  `lineages-and-cultures`, `narrative-lore` and `citizens-rpg`
  skills, the `narrative-lore` agent, and `docs/ai/CONTEXT_MAP.md`
  routes `Onboarding`, `Founder`, `Lineages` and `Narrative`.

### Reshaped

- The three delivered packages are no longer canonical. They live
  under `docs/_archive/ravatha-source-2026-08-04/` as a historical
  source, including the two `.zip` originals. The README in the
  archive maps every archived file to its bible destination.
- `DEC-0013` is added to `docs/ai/DECISION_LOG.md` and records:
  onboarding output is the cube profile only (no Traits,
  WeaponPreferences, ProfessionalAffinities, CombatStyle,
  PoliticalOrientation, SpiritualPosture, LeadershipStyle or
  RiskProfile); six canonical stat names; 60/40 base + ±8 onboarding
  range; six elemental affinities as cube faces; equipment is channel
  not power; eight line signatures; lore + systems consolidated into
  bible/13-21 with the original packages archived.

### Schema

None. No `WorldSave` version bump; no persisted field changes. The
migration and fallback rules in `bible/13_KOVARI_CUBE.md` are
forward-looking and apply when the cube schema is introduced.

### Baseline

Unchanged from EG-4. Build, tests, headless boot, agent-context
validation, schema version and locale catalogs are not modified by
this commit.

---

## Reconstructed history

Thin entries, recovered from commit subjects only. See each commit for content.

| Date | Commit | Subject |
| --- | --- | --- |
| 2026-07-31 | `9fc2542c` | Implement early-game resource and cultivation progression |
| 2026-07-31 | `124df29a` | Discard VS-5 and ship EG-1 resource seam |
| 2026-07-30 | `6e11a5a7` | Record why the splash working copies stay untracked |
| 2026-07-30 | `d2881bb4` | Track the AI-generated splash art as redraw reference |
| 2026-07-30 | `0fd7b55c` | Add EG-0 opening measurement, ambient day/night tint and the splash hero view |
| 2026-07-30 | `fc0bf57f` | Re-spread the Ardhen/Orveth/Vaelun accents and derive splash palettes |
| 2026-07-29 | `86db1355` | Stabilize persistent first playable loop |
| 2026-07-29 | `f2d066c8` | Add agent-context infrastructure for Codex and Claude Code |
| 2026-07-28 | `13525c96` | Advance VS-1/VS-2 vertical slice and fix macro-view/pathfinding bugs |
| 2026-07-28 | `41b7699c` | Stabilize the first playable city loop |
| 2026-07-27 | `23791ca0` | Complete localization sweep, real navmesh routing, biome terrain, ambient citizens, expedition FSM, and real frame profiling |
| 2026-07-26 | `1a5774af` | Migrate macro city view to pseudo-3D street perspective |
| 2026-07-26 | `d7eec26c` | Stabilize terrain and localization foundations |
| 2026-07-24 | `d0fd51d3` | Add astral founder flow and polish city UI |
| 2026-07-23 | `91268ddb` | Add persistent parcel resource gameplay |
| 2026-07-23 | `89c70981` | Integrate precomposed appearance variants across 192 bundles |
| 2026-07-22 | `b409d248` | Split status bar into PlayPause + Speed, forest gatherability, auto-release workers |

Earlier than 2026-07-22, `git log` is the only record.
