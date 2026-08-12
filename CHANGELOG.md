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

---

## Exit gate A0–A12: la verificación deja de contar una historia que el
## artefacto no sostiene

**2026-08-12** · arquitectura · sin cambio de schema (v34) · 1340 pruebas
superadas, 0 omitidas sobre 1340, compilación forzada con 0 avisos

El arco A0–A12 terminó con el build en verde, 1323 pruebas pasando y todas
las guardas de arquitectura sostenidas — mientras el juego no arrancaba. Esa
regresión (la recursión de `_Ready` de A9) la encontró una persona abriendo
el juego, no la tubería, porque nada en la tubería lo abría nunca. Este
cierre elimina esa clase de hueco: los sitios donde la verificación afirmaba
más de lo que medía.

**El jugador gana un juego que no puede volver a shippear sin arrancar.**
`tools/Test-GodotBoot.ps1` lanza la escena principal real en headless, con
el arnés de captura apagado, y falla ante una salida anormal — nombrando
`STATUS_STACK_OVERFLOW`, que es como murió A9 —, ante `ERROR`/`SCRIPT
ERROR`, ante una excepción gestionada sin capturar, y ante una escena que
nunca llega a su arranque. CI lo corre después de las pruebas y
`New-SessionSnapshot -Mode Full` corre ese mismo script, no un segundo
parecido. Probado reintroduciendo la recursión de A9: la guarda se pone
roja y nombra el fallo exacto.

**Y una línea de diálogo que estaba en inglés en la build inglesa.**
`ExpeditionPanel` pedía a `en.po` el msgid `"Wood"`, que no existe: el
error de despacho por falta de suministros mostraba el nombre crudo del
enum. Salió al escribir la guarda que A12 dejó anotada como *pendiente*.
`ResourceTypeLocalizer` pierde su fallback por nombre de enum — un
`ResourceType` sin mapear ahora es un fallo de test, no una cadena sin
traducir en producción — y `GenderIdLocalizer` cubre la familia hermana.

Lo demás es honestidad medida:

- **La cobertura offline era ficción.** `VerticalLoopPersistenceTests` tenía
  `if (offline) world.AdvanceWorldTick(); else world.AdvanceWorldTick();`:
  ambas ramas idénticas, así que `WorldTimeAdvance` nunca se ejecutaba y la
  suite comparaba el camino canónico consigo mismo. Ahora el tramo offline
  pasa por el catch-up real y **afirma que hubo ticks agrupados**, lo único
  que un avance en vivo no puede producir. La comparación pasó de igualdad
  byte a byte del save —que comparaba IDs de evento autoincrementales— a
  equivalencia semántica sobre la crónica duradera. La prueba de
  recuperación que estaba `Skip`-eada vuelve a correr (GitHub #1).
- **El baseline de build no puede mentir.** Compilaba incremental sobre un
  árbol caliente, no recompilaba nada y anotaba «0 errores, 0 avisos». Ahora
  es `--no-incremental`, y CI además usa `-warnaserror`. Los 22 avisos que
  una compilación forzada sí exponía están arreglados de raíz: entre ellos
  una desreferencia nula realmente alcanzable detrás de 16 de ellos, y un
  CS2002 por doble compilación en Persistence.
- **Un fotograma dorado podía ser de otra aplicación.** La captura usaba
  `CopyFromScreen` sobre el rectángulo de la ventana —una copia del
  escritorio, no del juego— y una corrida llegó a fotografiar una ventana de
  Chrome/Meet y archivarla como válida. Ahora la escribe el motor desde su
  propio viewport; sin fallback (GitHub #2).
- **`WorldRestoreState` documentaba una arquitectura que no corría.** Cero
  referencias durante todo A7–A12. Eliminado; `ARCHITECTURE.md` describe el
  flujo de restauración que sí se ejecuta, y explica por qué completarlo
  habría sido un tercer modelo de objetos paralelo sin mejorar la propiedad
  de dependencias.
- **La lista de excepciones contaba tres que ya no existían.**
  `PresentationDirectWorldAccess` queda vacía. Las lecturas de producción
  que aún alcanzaban el agregado por el seam de propiedad
  (`AvailableConstructionLots`, el snapshot de depuración, el reinicio
  conservando fundador) son casos de uso del session; `controller.World` ya
  no existe, así que reintroducirlo no compila.

## A8–A12: la frontera pasa a ser del compilador, no de la disciplina

**2026-08-12** · arquitectura · sin cambio de schema · 1323 pruebas superadas
y 1 omitida sobre 1324

Cierra el arco A0–A12. `CityGameSession` (Application) pasa a ser el
dueño de `CityWorld`: el controller ya no tiene un `CityWorld` propio,
sólo un `_session`, y el acceso que queda para los fixtures de
regresión visual es `internal CityWorld World { get; }` alcanzado vía
`InternalsVisibleTo("World of Goses")`. Las escenas de producción no
tocan el agregado: cada lectura va por un snapshot inmutable y cada
escritura por un método del session. Las cuatro dependencias entre
ensamblados (Domain sin nada, Application sobre Domain, Persistence
sobre Domain, Godot sobre los tres) las impone `ProjectReference`, así
que una regresión que añada Godot al dominio o JSON a Application ya no
pasa revisión: no compila. `ArchitectureBoundaryTests` (+519 líneas,
con `ArchitectureBoundaryAllowlist` como registro explícito de las
excepciones que quedan) congela lo que los `ProjectReference` no pueden
expresar.

A9 mata el seam por reflexión de la primera noche. `FirstNightScene`
leía las anclas del mundo con `HasMethod` + `Node.Call` en su propio
`_Process`, una vez por frame; ahora `MacroStreetLiveView` emite
`WorldDialogueAnchorsChanged` cuando la cámara o la proyección se
mueven y la escena refresca sólo entonces. Se fue el `_Process`
completo y con él el string mágico del nombre del método.

A10 saca la regresión visual del prototipo a `game/scripts/Testing/`:
`VisualRegressionHarness` decide si la captura está activa y
`VisualFixtureCatalog` conoce los 17 fixtures por nombre, en vez de que
el nombre viva repartido entre el script de PowerShell y ~50 métodos de
`CityPrototype`. A11 vuelve a authorizar paneles en `.tscn` y A12
introduce `ResourceTypeLocalizer`, con lo que ninguna clave i18n se
deriva ya de `enum.ToString()` — el dominio no conoce claves PO, igual
que desde A7 no conoce IDs de save.

Lo que el jugador NO nota: nada. Mismo schema 34, mismos saves, misma
pantalla. Lo que el próximo implementador sí nota está en
`docs/ARCHITECTURE_HARDENING_REPORT.md` §15: ya no puede añadir reglas
de gameplay al controller, pasar `CityWorld` a una vista, meter Godot
en el dominio, ni derivar un ID de save o una clave i18n del nombre de
un enum. El §10 del mismo informe lista la deuda que se conserva a
propósito, cada línea con su issue de GitHub (#5–#9).

**Sin schema.** No hay migration; la persistencia no cambia.

---

## A7: IDs estables y contrato semántico de restore

**2026-08-12** · arquitectura · sin cambio de schema · 1310 pruebas superadas
y 1 omitida sobre 1311

Lo que NO se nota, y es el grueso del trabajo: cada enum persistido
del juego — `ResourceType`, `BuildingKind`, `ToolKind`, `FirstNightStage`,
`ParcelTerritoryState`, `BuildingOrientation`, `ConstructionKind`,
`CitizenOrigin`, `CitizenCommitmentKind`, `CitizenVitalStatus`,
`CitizenLocation`, `WoundSeverity`, `ExpeditionStatus`, `ExpeditionPhase`,
`ExpeditionEncounterOutcome`, `ExpeditionRetreatPosture`,
`ExpeditionRewardKind`, `ResourceOpportunityKind`, `ResourceOpportunityState`,
`CultivationPlotState`, `GenderId`, `ElementalAffinityId` y el resto de los
combat/equipment IDs — tiene ahora un mapper explícito de IDs persistidos
en `src/WorldofGoses.Persistence/Ids/`. Cada mapper expone dos
operaciones: `ToId(value)` produce la cadena wire estable;
`TryParse(id, out value)` la decodifica. Antes, cada uno de esos
enums viajaba por la frontera Domain→JSON con un `Enum.ToString()`,
lo cual significaba que renombrar `ResourceType.Wood` a
`ResourceType.WoodLog` cambiaba silenciosamente el protocolo del
save. Ahora ese cambio exige tocar el mapper (un solo punto) y, si
no se actualiza el `TryParse` para aceptar el ID viejo, los saves
históricos rompen en build, no en runtime.

A7 también introduce el tipo `WorldRestoreState` en Domain — un
contrato semántico, engine-free, que NO es el JSON schema. Es un
record con `RestoredX` records anidados, todos con tipos del
dominio (`BuildingKind`, `ResourceType`, etc.) en vez de strings. Es
la puerta que A8 usará para que el applier deje de recibir
`WorldSave` directamente: el pipeline será `WorldSave → WorldSaveMapper
→ WorldRestoreState → CityWorld.ApplyRestoreState(state)`. Hoy el
mapper existe como contrato (puede ser consumido por tests y por
herramientas de migración), pero el applier sigue operando sobre
`WorldSave` por compatibilidad con las ~680 líneas de A6. La
introducción del mapper no rompe el ciclo: el wire ID del JSON sigue
siendo byte-idéntico, lo verifican 96 contract tests nuevos.

Los 96 contract tests de `StableSaveIdContractTests` congelan:
- los IDs wire de cada valor persistido (`Theory` por enum family);
- la parseabilidad inversa (que `TryParse(id)` rondea al valor original);
- la regla de que ningún archivo del capture/restore pipeline
  (`WorldPersistence.cs`, `WorldSaveApplier.cs`) puede contener un
  `Enum.ToString()` directo sobre un enum persistido — un regex
  busca el patrón y falla el build si lo encuentra. Eso es lo que
  cazó cuatro regresiones durante el propio refactor (en
  `ParcelTerritoryState.Available/Locked.ToString()` y
  `ResourceOpportunityState.Available.ToString()`).

Lo que el jugador NO nota: nada cambia. Carga de saves v34 sigue
funcionando. Lo que el próximo implementador de un feature sí nota:
si añade un nuevo enum persistido, tiene que crear su mapper (un
archivo de ~30 líneas) antes de empezar a usarlo; si renombra un
valor persistido, tiene que actualizar el mapper y (si no quiere
romper saves viejos) añadir un paso de migración. El contrato es
explícito en cada mapper y en el guardrail de tests.

**Sin schema.** No hay migration; no cambia la persistencia. Los
saves existentes cargan byte-idéntico.

---

## A6: Persistence sale de Domain

**2026-08-12** · arquitectura · sin cambio de schema · 1214 pruebas superadas
y 1 omitida sobre 1215

Lo que NO se nota, y es el grueso del trabajo: la persistencia deja de
vivir dentro de Domain. El nuevo assembly `src/WorldofGoses.Persistence`
(`Microsoft.NET.Sdk`, sin Godot) absorbe los 27 DTOs `*Save.cs`,
`WorldSave.cs` con su `CurrentVersion = 34`, `WorldPersistence.cs`
con el serializador JSON, las migraciones v2→v34, la validación, el
mapeador Capture/Restore y la lógica de slot atómico con `.bak`
sidecar. El espacio de nombres pasa de `WorldofGoses.Domain.Persistence`
a `WorldofGoses.Persistence`. Las fuentes se mudaron vía `git mv` desde
`game/scripts/Domain/Persistence/` para preservar la historia; los
companion `.uid` de Godot viajan con cada `.cs`.

La flecha de dependencia permitida es la única posible:
**Persistence → Domain**. Domain nunca referencia Persistence, lo cual
queda blindado por
`ArchitectureBoundaryTests.Layer_DoesNotReferencePersistenceAssembly`
tanto para Domain como para Application (que tampoco debe empezar a
depender de él en este slice). El guardrail previo
`EngineFreeProject_DoesNotReferenceGodot` se extendió para cubrir
también a Persistence: añadir `using Godot` ahí es un error de
compilación, no un comentario de revisión. La migration path
existente (v2→v34) corre byte-idéntica; los roundtrip tests cargan
saves de cada versión sin cambio.

El método público `CityWorld.Restore(WorldSave save)` no puede vivir
en Domain ahora — Domain ya no ve `WorldSave`. La orquestación se
mudó a `WorldSaveApplier.ApplyTo(CityWorld world, WorldSave save)` en
Persistence, y `WorldPersistence.ApplyTo` / `WorldPersistence.FromSave`
sirven como facade para no ensuciar cada call site. Para que el
applier pueda poblar el mundo sin duplicar lógica,
`CityWorld` expone por `internal` los campos y helpers que ya usaba
la restore original (`_citizens`, `_buildings`, `_projects`,
`RegisterBuilding`, `RegisterNaturalResourcePatch`, `EnsureFoundingParcels`,
`MobiliseForDay`, `MobiliseForNight`, `NaturalResourceOccupiesFrontageCell`,
`FindFirstAvailablePlacement`, `ResourcePositionIndex`,
`SetPendingProspectForRestore`, `CreateMigrantProfile`,
`ToExpeditionOutcome`). El csproj de Domain gana el exclude
`**/Persistence/**` para que los archivos viejos no compilen dos
veces durante el movimiento; el csproj de Game y el de Tests añaden
`ProjectReference` a `WorldofGoses.Persistence`. La seam
`InternalsVisibleTo("WorldofGoses.Persistence")` en Domain.asmInfo es
lo que permite al appler ver esos internals sin filtrar la API
pública.

Lo que el jugador NO nota: nada cambia en pantalla, en el formato de
save, en la velocidad de carga, ni en la semántica de ninguna
migración. El mundo se serializa exactamente igual que antes; la
v34 sigue siendo v34. Lo que el próximo implementador de un feature
sí nota: ya no puede abrir un save DTO desde Domain sin que el build
falle — el boundary `Domain → Persistence` está cerrado por test.

**Sin schema.** No hay migration; no cambia la persistencia.

---

## A5-A: la capa Application deja de significar sólo "snapshots"

**2026-08-12** · arquitectura · sin cambio de schema · 1211 pruebas superadas
y 1 omitida sobre 1212

Lo que NO se nota, y es el grueso del trabajo: `WorldofGoses.Application`
deja de ser el cubo donde viven los read models y pasa a ser un ensamblado
con dos responsabilidades. Sigue conteniendo los snapshots inmutables
(`CityStatusSnapshot`, `ConstructionSnapshot`, `BuildingDetailSnapshot`,
`CityMacroSnapshot`, etc., más `MacroStreetLiveViewState`/`MacroCitizenSnapshot`
movidos a `game/scripts/Application/`), pero ahora también expone un único
**facade de casos de uso engine-free**, `CityGameSession`. Cada método
público del session es un caso de uso: autorizar construcción, asignar un
ciudadano a un edificio o a un proyecto, cosechar un sitio de cultivo,
fabricar una herramienta, despachar o cancelar una expedición, sembrar la
primera noche, avanzar el reloj del mundo, sembrar los bosques iniciales.

`CityWorldController` ya no orquesta esas operaciones directamente. Sigue
siendo el dueño temporal de `CityWorld` (la persistencia aún recibe la
instancia y será abordada en A6-A7), pero cada wrapper público que toca
estado de juego delega en `_session.<Método>` y sólo después traduce el
resultado a `EmitSignal(...)` y a `SaveNow()` cuando corresponde. Los
resultados son los `*Result` que ya existían en el dominio
(`AssignmentResult`, `ToolCraftResult`, `CultivationActionResult`,
`ExpeditionStartResult`, `ConstructionAuthorizationResult`,
`HeroCreationResult`, `HeroIncorporationResult`, `WoundRecoveryResult`).
No se introdujo una nueva jerarquía de resultados; se reutilizaron los
outcome types que ya describían cada modo de fallo posible.

El session no expone `Execute(Action<CityWorld>)`, `GetWorld()` ni
`WithWorld(...)`. El facade recibe el `CityWorld` por constructor (sin DI
container, sin service locator, sin MediatR, sin `ICommandHandler<T>`) y
todo el código de Presentation llega al dominio únicamente a través de sus
métodos. Los únicos `_world.` que sobreviven en el controller son las
costuras legítimas: la persistencia (`_world.Restore(...)`,
`WorldPersistence.SaveToSlot(_world, ...)`), las suscripciones a eventos
del dominio (`_world.BuildingChanged += ...`), los `internal` fixture
seam para los visual regression (`SeedFixtureWorld`,
`RestoreFixtureWorld`, `AdvanceWorldTickForFixture`, etc.), y los chequeos
de validez de selección (`_world.GetBuilding(id) is null`).

El guardrail nuevo es `UseCaseDelegationTests`. Lista cada método de caso
de uso del controller y verifica que el cuerpo llama a
`_session.<Nombre>`, no a `_world.<Nombre>`. Cuando se añada un comando de
gameplay nuevo al controller sin pasar por el session, el build falla
antes de llegar a revisión. No detecta arquitectura mediante nombres
privados: enumera únicamente los wrappers públicos que ya sabemos que son
casos de uso.

Lo que el jugador NO nota: nada cambia en pantalla. Lo que el próximo
implementador de un feature sí nota: ya no puede añadir un comando nuevo
directamente al controller — tiene que crear el método correspondiente en
`CityGameSession` y delegar.

**Sin schema.** No hay migration; no cambia la persistencia.

---

## A5: las capas dejan de ser una convención y pasan a ser ensamblados

**2026-08-11** · arquitectura · sin cambio de schema · 1209 pruebas superadas
y 1 omitida sobre 1210

Lo que se nota: en el panel de ciudad, con el juego en español y una
construcción en curso, "3 días", "Activa", "60%" y todas las cantidades de
recursos se dibujaban con la última letra partida por la mitad. Ya no. La
barra de scroll vertical de Godot no reserva ancho: se dibuja **encima** del
borde derecho del contenido, y esa columna alinea todo a la derecha. Hacían
falta dos condiciones a la vez —que el panel fuese lo bastante alto para
scrollear y que el valor llegase al borde— y por eso sobrevivió tanto: el
fixture de ciudad ociosa cabe y se ve perfecto, y en inglés las etiquetas son
más cortas. Cada fixture existente mostraba una condición, ninguno las dos.

Lo que NO se nota, y es el grueso del trabajo: `Domain` y `Application` son
ahora ensamblados propios (`src/WorldofGoses.Domain`,
`src/WorldofGoses.Application`), proyectos `Microsoft.NET.Sdk` sin referencia
a GodotSharp. `using Godot` en cualquiera de los dos es un **error de
compilación**, comprobado añadiendo uno y viendo fallar el build. Antes la
regla la sostenía un test que hacía grep de las fuentes: detecta el error
honesto y nada más. Las fuentes no se movieron de sitio salvo los 15
read models, que pasan a `game/scripts/Application/`.

Levantar la frontera destapó seis miembros del dominio a los que la
presentación llegaba de tapadillo, invisibles mientras un solo ensamblado
hacía que `internal` no significara nada. Ninguno se abrió en bloque:
`IsLaborTime`, `RegisterCitizen` y `SustainWound` pasan a `public` porque son
consultas y comandos legítimos; `Gather` sigue `internal` —su documentación
dice que nadie fuera del dominio drena un parche, y el controlador lo estaba
haciendo en bucle— y el bucle se mudó dentro de `CityWorld`; el setter de
`ConstructionProject.Progress` sigue `internal`, así que en juego real una
obra sólo avanza por el tick, y los fixtures usan
`SeedProgressForFixture`, que clampa; `ConcludeFirstNightForFixtures` pasa a
`public` y la intención que llevaba su `internal` se mudó a un test que fija
los sitios de llamada, que es más estricto que lo anterior.

**Sin schema.** No hay migration; no cambia la persistencia. Godot arranca en
headless y carga el mundo del slot; los dos ensamblados nuevos se despliegan
junto al del juego.

---

## A4: el macro view empieza a dejar de ser una mega-clase

**2026-08-11** · arquitectura · sin cambio de schema · 1206 pruebas superadas
y 1 omitida sobre 1207

Lo que se nota: la macro view ya no declara en línea 41 constantes, 11 colores
y 3 rutas de escena — viven en `MacroViewConstants`, su única fuente de
verdad. Las 8 funciones puras de proyección viven en `MacroProjectionHelpers`. La
geometría de obstáculos vive en `MacroObstacleGeometry`. La
construcción de texto de selección de ciudadano vive en
`MacroSelectionTextBuilder`. Los records `PlotBox` / `TreeBox` viven en
`MacroStreetRenderer`; `PlacementLotBox` / `PlacementCellBox` viven en
`PlacementPresenter`. `MacroHitRects` y `MacroPlotLookup` son las costuras
de records-bag entre renderer, interaction y journey. El renderer owns el
band-layer stack, la Painter abstraction, la caché de texturas de edificio,
el terreno gastado, la biome del suelo, las listas de plots/trees, los
estados de citizen, el territory map, el band occupancy, y el world
envelope. `MacroInteractionController` owns selection + hover state +
`WorldStatusBubble` + cursor. `CitizenJourneyPresenter` owns el founder
state + el journey dictionary + `StreetNavigationServerPlanner`. `MacroCameraController`
owns zoom + free/follow + lateral/depth anchor + pan + transition timing +
building-entry push. `PlacementPresenter` owns el estado de placement: el
flag activo, el tipo elegido, los lotes y celdas proyectados, y el lote
bajo el cursor o seleccionado. La macro view pasa de **4917 a 4223
líneas** (Δ –694, –14.1%).

Lo que NO se nota: lo movido son constantes, helpers puros, records,
costuras, estado de rendering, estado de interaction, estado de journey,
estado de camera, estado de placement, los `Draw*` methods del placement,
los de los obstacles (`DrawPlacementLots`, `ProjectPlacementFootprint`,
`DrawParcelTerritoryTints`, `DrawSteppedTintTrapezoid`,
`DrawSteppedPlacementFootprint`, `DrawSteppedPlacementEdge`,
`DrawNaturalResourceUnit`, `DrawStorageFullBadge`) y `DrawCultivationSite`.
Los métodos de interaction (TryClick, TryRightClick, UpdateWorldHover,
OpenGatherMenu, OpenCultivationMenu, etc.) siguen embebidos en la vista.
El comportamiento (píxeles, autoridad A2, frontera dominio/presentación,
scheduling) no cambia. `PacedRouteSteps` y `ReconstructRouteProgress`
siguen paces contra la ventana del dominio.
`ArchitectureBoundaryTests.Presentation_DoesNotConfirmCitizenArrival`
sigue con la allowlist vacía. Los `internal static` que las pruebas
tocan siguen siendo accesibles a través de forwarders de una línea.

Los 12 archivos colaboradores suman **~2.2k líneas**, cada uno con su
única responsabilidad: renderer, interaction, journey, camera, placement,
y los helpers puros (constants, projection, obstacle geometry, selection
text) más las dos costuras de records-bag (hit rects, plot lookup).

**Sin schema.** No hay migration; no cambia la persistencia.

El enrutamiento de clicks también se unifica: el orden de prioridad
(árbol → ciudadano → edificio) estaba escrito dos veces, una en
`TryClick` y otra en `TryRightClick`, de modo que los dos botones podían
separarse en silencio y el click derecho actuar sobre algo distinto de lo
que el izquierdo habría seleccionado en el mismo píxel. Ahora ambos
resuelven el objetivo con un único `MacroInteractionController.HitTest`,
y sólo difieren en qué hacen con él. La vista conserva el "qué hacer"
(inspector, menús, entrada a edificio); el controller sólo responde
"qué hay ahí".

**Verificado con clicks reales, no leyendo código.** Los fixtures
`construction-placement`, `construction-placement-hover-invalid` y
`construction-placement-confirm-click` se capturaron contra este árbol y
contra HEAD en un worktree limpio: los frames coinciden, y el click de
puntero real sigue autorizando la construcción (Farm en curso, madera
7→5, dock primario restaurado). El refactor movió la búsqueda del lote
más cercano —duplicada literalmente en la ruta de hover y en la de
click— a un único `PlacementPresenter.TryFindNearestLot`.

El enrutamiento se verificó igual, inyectando clicks reales del sistema
operativo sobre el mundo: click izquierdo en un árbol lo selecciona
("Wood · 8 units of wood remain", con el sprite del árbol clicado);
click derecho sobre el mismo píxel lo selecciona y además abre el menú de
recolección; y en un píxel donde se solapan un recurso y un ciudadano
gana el recurso, que es el orden de prioridad de siempre.

---

## A2: el viaje de un ciudadano lo termina el reloj, no el sprite

**2026-08-11** · arquitectura · schema v34 (sin cambio) · 1206 pruebas superadas
y 1 omitida sobre 1207

Lo que se nota: un ciudadano llega a su puesto porque ha pasado el tiempo, no
porque una animación lo haya dicho. Cerrar la vista macro, un tirón de frames o
una ruta que el planificador no sabe resolver ya no pueden dejar a nadie
caminando para siempre ni congelar un edificio en `WorkersInTransit`.

Había dos autoridades para un mismo hecho. En vivo, el viaje solo terminaba
cuando `MacroStreetLiveView` avisaba de que su sprite había llegado; sin el
juego abierto, el mismo viaje terminaba por ticks transcurridos. Era una
decisión consciente y documentada — evitaba que el tiempo simulado escondiera a
un caminante o arrancara la producción antes de su llegada visible — pero
contradice tres reglas que están por encima de una nota de arquitectura: *live y
offline usan las mismas reglas de dominio*, *las escenas no deciden reglas*, y
*el dominio gana sobre su representación visual*.

Ahora existe **un solo tick**. `AdvanceOfflineWorldTick` desaparece, y
`CompleteDueTravel` dentro de `AdvanceWorldTick` es el único sitio donde un
viaje acaba: cuando el reloj alcanza `TravelArrivalTick`. Los comandos
`ConfirmCitizenArrivedAtAssignment` y `ConfirmCitizenArrivedHome` se eliminan
del dominio y del controller, y una prueba de límite con lista vacía impide que
vuelvan.

**Sin migración.** Todo lo que el contrato necesita se persiste desde v19:
localización lógica, tick de salida, dirección, compromiso y orden permanente.
El tick de llegada se deriva, no se guarda, para que la duración siga siendo una
regla y no un valor que pueda desviarse de ella. Una partida guardada con
alguien atascado `InTransit` se cura sola en el primer tick tras cargar.

La vista sigue dibujando el viaje, pero ahora lo reparte sobre la ventana del
dominio con el mismo cálculo que ya usaba para reanudar un viaje a medias tras
cargar: llegada dibujada y hecho coinciden, 2x y 4x aceleran solos, y restaurar
y caminar dejan de ser dos caminos de código. El paso sigue siendo discreto;
solo cambia su ritmo, que pasa a salir de `AbstractTravelTicks` (30) en vez de
la cadencia de render.

Tres defectos latentes caen de paso: la lista blanca de compromisos excluía
`None`, así que quien volvía a casa tras perder su compromiso no llegaba nunca;
el batching offline no tenía término de viaje y podía saltarse una llegada; y el
camino offline nunca emitía `BuildingChanged` al llegar, cosa que el comando
borrado sí hacía.

Desviación de comportamiento a la vista: quien llega a casa con una orden
permanente vuelve a salir en el mismo tick — visible ahora que los viajes
terminan de verdad. Y un fundador que llega al refugio cena, cosa que antes no
ocurría porque se quedaba congelado en mitad de la calle.

---

## A3: el HUD persistente deja de reconstruirse en cada tick

**2026-08-11** · arquitectura · sin cambio de schema

Lo que se nota: abrir el macro con un mundo activo ya no paga un
rebuild completo del status bar, el city summary y la crónica por cada
tick de simulación. Las superficies persistentes ahora actualizan sus
hijos en sitio; solo `ExpeditionPanel` (modal oculto por defecto) y los
widgets estructurales raros reconstruyen. El comportamiento visual y
las pruebas siguen verdes.

El status bar reemplaza `QueueFree` + `new` por un `ApplySnapshot` que
muta los labels, iconos y tooltips de los chips de recurso. La crónica
mantiene un pool de filas persistentes y las oculta o las reusa por
identidad. `ExpeditionPanel` cierra un leak de lambdas en
`_controller.WorldTickAdvanced` que sobrevivía al `_ExitTree`, y
agrega un guard `if (!Visible) return` para que el modal oculto no
procese ticks. Un cache compartido path→Texture2D corta las llamadas a
`ResourceLoader.Load` que la barra, los docks y la crónica hacían en
hot paths.

---

## A1: la frontera Presentation → Domain queda cerrada por el controller

**2026-08-11** · arquitectura · sin cambio de schema

Lo que se nota: ningún nodo de presentación toca ya `CityWorld` ni sostiene
una entidad viva del dominio. El controller expone solo *snapshots* y
*queries* inmutables; los mutadores del agregado (`Ledger`, `Patch`)
viven ahora `internal`, de modo que solo `CityWorld` puede ejecutarlos.
Las pruebas del límite pasan; los únicos accesos directos que quedan en
`Presentation` son tres ficheros documentados como deuda A2 (la animación
de llegada del fundador, el panel de debug de combate, y el guion de
fixtures visuales). Behaviour y UX idénticos.

---

## El rail deja de negociar su columna: un solo cuerpo visible

**2026-08-10** · schema v34 (sin cambio) · presentación

Lo que se nota: abrir y cerrar el Registro ya no vacía la lista de
expediciones. `VER` y las tarjetas vuelven juntos, porque ya no hay dos
hermanos peleando por la misma columna.

El defecto era estructural, no un fallo de refresco. `_scroll` y
`ChroniclePanel` eran hermanos `ExpandFill` del mismo `VBoxContainer`, y Godot
tenía que repartir una altura entre dos aspirantes cuyos mínimos cambiaban al
plegarse. `VER` sobrevivía por estar en un hermano `ShrinkBegin` con mínimo
natural; la lista de tarjetas, el único nodo negociable, no. Medido: el cuerpo
quedaba en **2 px** con tarjetas de 25 px — visibles, maquetadas en ninguna
parte, dibujadas nunca. El código lo compensaba invalidando y reordenando tres
ancestros (`UpdateMinimumSize`, `QueueSort`, `ResetSize`) tras cada toggle.

Ahora existe `AccordionHost`, reutilizable y ajeno a expediciones y crónica:
varios cuerpos, un único hueco compartido, exactamente uno visible. Los dos
headers viven en la columna del rail y siguen siempre a la vista; los cuerpos
se intercambian debajo. Como un `Container` de Godot excluye del mínimo a los
hijos invisibles, no queda reparto que perder: desaparecen `RequestRailRelayout`
y las tres llamadas de invalidación, y el `MaxHeight` de 360 px del cuerpo de la
crónica —piso y techo a la vez, y la causa directa del ahogo— se elimina.

Las pruebas que fijaban el parche como contrato se reescribieron para custodiar
el invariante nuevo: un solo `ExpandFill`, ambos headers fuera del host, un solo
cuerpo visible, y ausencia de `QueueSort`/`ResetSize`/`UpdateMinimumSize` en el
rail. Su reaparición es la señal de que la guerra civil volvió.

**Cuatro sesiones de fixtures rojos tenían una causa concreta.** El montaje
descartaba en silencio el fallo de `StartExpedition`, así que la captura salía
con código 0 y escribía un PNG de la pantalla equivocada. Al hacerlo hablar:
`outcome=MemberUnavailable, unavailableReason=Wounded`. El Founder estaba
**herido** por el combate que entregamos dos commits antes. Los diagnósticos
anteriores —«fuera de horario», «madurez del save»— eran incorrectos y quedan
corregidos. Los fixtures del rail siembran ahora un mundo propio, ya pasada la
primera noche, así que no dependen del slot 0.

El fixture del round-trip tampoco sabía ver este bug: comprobaba
`IsVisibleInTree()`, que es exactamente lo que el defecto dejaba intacto. Ahora
exige que el cuerpo mida al menos lo que mide su primera tarjeta. Verificado por
A/B: **1102 px** con la estructura nueva, **2 px** con la anterior.

Baseline: build 0/0; 1185 pruebas superadas y 1 omitida sobre 1186, sin cambio
de recuento; catálogos y schema v34 intactos.

**Deuda que sigue abierta**, registrada en M-14 y no presentada como resuelta:
`expedition-rail-rail-protagonist` y `expedition-rail-phase-focus` fallan
también con la estructura anterior (A/B hecho), así que son previos y no
regresiones. Y las fixtures de click en ventana siguen sin ser fiables: el
helper escala la posición por `DisplayServer.WindowGetSize()`, de modo que la
ventana bootstrap de 50×50 documentada manda el click lejos del objetivo. La
vía fiable es headless; el frame en ventana sí confirma visualmente tarjetas y
`VER` juntos.

---

## El Spirit Trail ya es la primera expedición completa

**2026-08-10** · schema v33 → v34 · expedición/combate/apertura

Tras `SpiritDeparted`, el Founder puede partir inmediatamente sin Cache ni
Food. El Spirit Trail dura cuatro horas de mundo, inicia su encuentro tras
media hora, enfrenta al mismo CombatSession melee+ranged visible o en segundo
plano, continúa después de la victoria hasta una manifestación física del
rastro y muestra el regreso antes de devolver al Citizen a la ciudad. No entrega
Wood ni otra recompensa inventada: el resultado actual es `Discovery`.

El Founder desarmado usa Basic Attack y Skill 1 mediante un baseline de sesión
explícito que no crea equipo ni experiencia de arma. Un arma real futura toma
precedencia. Schema v34 elimina el arma sintética y la reserva Food exactas de
v33, y guarda la versión de reglas del encuentro para que un combate legacy en
curso no cambie al cargar. Save/load y offline preservan cooldown, comandos,
posición lógica, outcome, llegada al objetivo y consecuencias exact-once.

Baseline al cierre: build 0/0; 1185 pruebas superadas y 1 omitida sobre 1186;
catálogos válidos con 1050 IDs de plantilla y 324 claves de runtime; cadena de
migración `33 => MigrateV33ToV34` registrada.

Los once fixtures de viaje/combate/objetivo/regreso produjeron 22 frames
locales a 1280×720 y 1920×1080 **contra el slot 0 que existía en esa máquina en
ese momento**; capturas, logs y frame-time quedan fuera de Git. Esa firma no es
reproducible bajo demanda: revalidados aquí sobre un slot 0 más temprano
(tick 11509), `expedition-live-early` y `expedition-live-travel` fallan, y
`expedition-rail-chronicle-roundtrip` sigue fallando, mientras que los fixtures
que no dependen de una expedición activa —`expedition-rail-empty`,
`expedition-components-default`— pasan limpios. La causa sigue siendo la
registrada en M-14: `StartExpedition` no arranca y `CityPrototype` descarta el
fallo en silencio (`if (!started.IsSuccess) return;`), de modo que la captura
sale con exit 0 y escribe la pantalla equivocada. El diagnóstico anterior de
«fuera de horario» era incompleto: esta tanda falló a hora laboral
(tick 13043 → 2243 dentro de la ventana 1200–2400), así que el disparador es el
estado del save, no sólo la hora. Mientras el fixture no siembre su propio
estado, ninguna firma visual de expedición debe darse por vigente en otra
máquina o en otro slot.

---

## El combate lateral ya ocupa espacio real

**2026-08-10** · schema v33 (sin cambio) · combate/presentación

El encuentro observable ahora simula una línea autoritativa dentro de la misma
`CombatSession`: Citizens y enemigos avanzan hacia su target hasta entrar en
`AttackRange`, se plantan y atacan. No existe rama para retroceder, de modo que
un ranged alcanzado por melee permanece en su sitio salvo que un impacto lo
desplace. Impulse atacante y Stability defensora gobiernan un primer knockback
determinista; velocidades, rangos y radios de cuerpo viven en dominio y el
replay save/offline reconstruye el mismo resultado sin persistir `Node.Position`.

`ExpeditionStage` dejó de dibujar combatientes estáticos y reutiliza un
`CombatantView` por participante. Los placeholders muestran facing, HP,
avance, Basic Attack, Skill, impacto, knockback y derrota a partir de snapshots
y eventos; sus tweens redondean coordenadas visuales y nunca calculan daño.
No se añadió movimiento manual, kiting, hitboxes de gameplay, Chains, Traits,
formación, boss ni reloj/velocidad privados.

El rail derecho también recupera su contenido inicial: abrir Chronicle pliega
las expediciones y cerrarlo vuelve a mostrar cards y `VER`.

Baseline medido al cierre: build 0 errores / 0 advertencias; 1168 pruebas
superadas y 1 omitida sobre 1169, incluidas 13 de `CombatSpatialTests`; schema
v33 sin migración; agent context 474/474 y catálogos 1049/324. El arranque
headless conserva el fallo ambiental Windows `-1073741819` ya conocido.

**Los fixtures visuales de expedición no pudieron ejecutarse en este cierre.**
`expedition-live-early` y `expedition-rail-chronicle-roundtrip` fallan ambos
con la misma causa raíz: `StartExpedition` no arranca en el slot cargado y
`CityPrototype` la descarta en silencio (`if (!started.IsSuccess) return;`),
así que no hay card, no hay `VER` y la captura escribe un PNG de la vista macro
con `EXPEDICIONES · 0`. El save se lee con `--wog-visual-capture`, que desactiva
escrituras, pero la progresión offline aplicada al cargar desplaza la hora del
mundo con el tiempo real: la captura que hoy funcionó a media jornada falla a
`Día 2 · 18:56 · Fuera de horario`, con el héroe en casa. Los fixtures no son
herméticos y fallan de forma silenciosa —exit 0 y PNG de la pantalla
equivocada—, que es precisamente la forma de fallo que una matriz visual no
debe tener. El movimiento espacial queda respaldado por pruebas de dominio, no
por firma visual, y el presupuesto de frame de la vista en vivo sigue sin
medirse desde que se conectó la sesión.

---

## El primer encuentro ya avanza como una sesión observable

**2026-08-10** · schema v32 → v33 · combate/expedición

El Spirit Trail del Founder crea ahora una `CombatSession` propiedad de la
ciudad y asociada al `ExpeditionId`. Cada tick mundial avanza un paso del mismo
motor determinista que `ResolveToEnd`; volver a la ciudad o reabrir `VER` no
detiene ni recrea el encuentro. Basic Attack continúa automáticamente, AUTO
gobierna solo el gasto de la Active Skill y los inputs 1–4 convergen con click y
focus en un único comando por slot. Salud, enemigos, cooldown, AUTO y outcome
se proyectan desde estado real; RETIRADA permanece deshabilitada y no se añadió
movimiento, Chains ni un reloj privado.

Hasta que el onboarding materialice el arma elegida, el primer Spirit Trail
asigna al Founder desarmado una familia provisional determinista entre las
cuatro ya soportadas y la persiste en su loadout. Schema v33 conserva el paso
lógico y el historial reproducible de comandos AUTO/manual para reconstruir la
misma sesión después de cargar.

El alcance observable queda deliberadamente estrecho: solo
`SpiritTrailSearch`, con Founder como único integrante. Reconocimientos y
sorties materiales equipados conservan su resolver agregado anterior. Durante
una sesión activa el catch-up offline usa los mismos ticks canónicos que el
juego en vivo, y los comandos aceptados de AUTO/Skill dejan el save marcado
para que cerrar inmediatamente tampoco los pierda.

Los saves v32 también cruzan este cambio sin quedar atrapados: un Spirit Trail
Founder-only todavía outbound —o en una frontera Encounter aún no resuelta—
recibe la misma arma provisional determinista al migrar. Las fases posteriores
preservan su loadout histórico; una dupla legacy no canónica no se invalida y
termina mediante el resolver agregado anterior.

Baseline medido: build 0/0, 1154 pruebas superadas y 1 omitida sobre 1155,
36 pruebas de migración verdes, `WorldSave.CurrentVersion = 33` con la cadena
`32 => MigrateV32ToV33` registrada y `save.Version != 32` como guarda.

Regresión de rendimiento **abierta y no resuelta**: `expedition-live-early`
mide 51–55 ms de `Performance.Monitor.TimeProcess`, por encima del presupuesto
de pico de 40 ms, frente a 18,7 ms del mismo fixture antes de conectar la
sesión de combate. No es ruido de máquina: en la misma tanda
`macro-hud-default` midió 6–18 ms, así que el coste es propio de la vista en
vivo. El fixture captura y la revisión visual pasa; el presupuesto de frame
no. Queda registrado como deuda, no como verificación superada.

---

## La expedición activa ya puede abrir una perspectiva lateral estructural

**2026-08-10** · schema v32 (sin cambio) · presentación

Una expedición activa ofrece ahora `VER` desde el rail de ciudad y abre una
única `ExpeditionLiveView` dentro del `GameUiShell`. La barra global permanece
con el mismo reloj y Speed 1x/2x/4x; la transición oculta las superficies macro
sin reconstruirlas y Volver/ESC regresan a la ciudad sin pausar, cambiar
velocidad ni resolver la expedición.

La vista proyecta ruta lineal, fase, integrante real, salud/aguante disponibles
y progreso. Compone cuatro puestos de escuadra y cuatro Active Skills, y reserva
AUTO/RETIRADA como controles deshabilitados. El stage lateral es estático: el
fixture muestra Founder y dos amenazas, pero todavía no existen movimiento,
hitboxes, daño, combate espacial ni relojes privados. Cuando el dominio no
publica dificultad o enemigos, la UI lo declara desconocido en vez de inventar
loot o telemetría.

La convergencia de layout posterior usa la referencia local de Expedition HUD
solo como autoridad geométrica: el battlefield ocupa el centro, las columnas
de información enmarcan sus lados y escuadrón, cuatro Skills octogonales y
AUTO/RETIRADA se reparten la franja inferior sin salir del canvas. Los Skills
crecen mediante nuevas dimensiones lógicas —sin `Scale`— y sus ocho anchors de
Trait permanecen ligados a los lados. No se añadió gameplay ni telemetría.

Verificación de cierre: build limpio, familias HUD/snapshots/controller/
expediciones verdes, catálogos EN/ES válidos y `expedition-live-early`
capturado e inspeccionado sobre el canvas 1280×720; el runtime 1920×1080
conserva esa misma composición lógica. Un click real sobre `VER` abre la vista
y ESC vuelve a la ciudad sin modificar Speed ni resolver la expedición.

---

## La futura expedición en vivo ya tiene componentes visuales reutilizables

**2026-08-10** · schema v32 (sin cambio) · presentación

Todavía no existe `ExpeditionLiveView` ni se ejecuta combate. Este incremento
construye y valida solamente su gramática reusable: cuatro puestos visuales de
escuadra, cuatro Active Skills y un Skill Slot octogonal real con estados
Empty/Locked/Ready/Cooldown/Disabled. El primer fixture muestra Founder + tres
puestos bloqueados y Skill 1 lista + tres skills bloqueadas; Locked usa `[X]`
y texto, y Cooldown muestra tiempo restante y progreso.

Los ocho lados del octágono reservan anchors invisibles independientes para
Traits futuros, sin `TraitDefinition`, tooltip ni evolución. La UI anticipa
cuatro Citizens sin cambiar `ExpeditionRequest.MaxTeamSize`, y reutiliza la
paleta, tipografía y cromo `Hud*` existentes. No reaparecen
`SimulationControls`, `PlayPauseButton` ni una pausa global.

Baseline medido: build 0/0, 1118 pruebas superadas y 1 omitida sobre 1119 (+4
sobre el incremento anterior), catálogos válidos con 1012 IDs de plantilla y 301
claves de runtime. Las tres fixtures del showcase capturan a 1280×720 y
1920×1080; las de teclado y gamepad salen idénticas byte a byte, que es
exactamente el ciclo de foco horizontal compartido que exige
`VISUAL_REGRESSION.md`. Las etiquetas de andamiaje del showcase siguen en
inglés a propósito: son texto de depuración, no superficie de jugador.

---

## La apertura prioriza el primer combate visual del Founder

**2026-08-10** · schema v32 (sin cambio) · dirección de producto

Este incremento no implementa gameplay. Realinea el siguiente vertical: tras
onboarding, primera noche y `SpiritDeparted`, el Spirit Trail debe llevar al
Founder a un encuentro lateral automático dentro de unos cinco minutos,
continuar al objetivo y regresar mientras la ciudad sigue avanzando. La regla
que bloqueaba toda profundidad de combate hasta cerrar EG-5/EG-6 queda
superada solo para ese vertical; la consolidación agrícola se conserva como
EG-5C y el combate amplio permanece diferido.

Quedan fijados un único reloj sin pausa, velocidades 1x/2x/4x que no cambian al
abrir la vista expedicionaria, Basic Attack automática, vanguardia futura de
cuatro con Founder-only en la primera salida, cuatro skills octogonales con
solo Skill 1 conectada, movimiento hasta `AttackRange` sin kiting y knockback
modulado por Stability/Impulse. Traits, Chains, carroza, `SPACE`, formación
avanzada y Skills 2–4 no entran todavía. Spirit Trail deja de significar
`1 Food → Wood`; dura unas cuatro horas de mundo, no consume Food por existir y
mantiene abierta su recompensa material.

Baseline de apertura: build 0/0, 1114 pruebas superadas y 1 omitida sobre 1115,
schema v32, 474 checks de contexto y catálogos EN/ES válidos. El boot headless
del snapshot falló con `-1073741819` (0xC0000005) y la captura no se produjo por
cliente 50×50; ninguno se presenta como verificación exitosa. El fallo de boot
no reproduce: cinco ejecuciones posteriores de
`--headless --path game --quit-after 3` salieron con código 0, así que se
registra como caída de teardown intermitente, no como regresión.

---

## El mundo ya no se puede pausar, y los controles sueltos vuelven a la barra de estado

**2026-08-10** · schema v32 (sin cambio) · presentación

Lo que se nota: la superficie flotante de abajo a la derecha
(`SimulationControls`, con su botón de play/pausa) ha desaparecido. El
control de velocidad vive ahora en el *utility cluster* del borde
derecho de la barra de estado, junto a Cámara y Menú, compartiendo el
mismo cromo de dock en vez de flotar solo en una esquina. Sigue
ciclando las tres velocidades (1× → 2× → 4× → 1×) con el mismo
apilado de iconos de play, ahora centrado con márgenes simétricos.

La pausa se ha eliminado como concepto, no sólo como botón:
`SpeedChoice.Paused` ya no existe y el jugador sólo puede acelerar el
mundo. Es lo coherente con una ciudad que avanza con el juego cerrado
— un botón que detiene un mundo que de todos modos sigue avanzando
mientras no miras prometía un control que nunca tuvo.

ESC recupera el gesto estándar de «tecla atrás abre el menú»: con la
vista macro activa lo abre, con el menú abierto lo cierra (o cierra
antes la confirmación de reinicio), y con un perfil de héroe o un
detalle de edificio en pantalla deliberadamente no consume el evento
para que `CityPrototype` pueda devolver al jugador a la ciudad. El
menú, además, vuelve a abrirse: su botón estaba suscrito a `Toggle`
dos veces —una aquí y otra desde la vista macro—, así que cada clic lo
abría y lo cerraba en el mismo fotograma.

Baseline medido: build correcto, 1114 pruebas superadas y 1 omitida
sobre 1115, `HudCompositionTests` en 51/51. Los másters de audio
(`audio/`, 4 GB de `.wav` y `.pkf`) y las capturas de regresión visual
(`docs/session-state/captures/`) pasan a estar ignorados: el
repositorio no tiene LFS configurado y esos binarios no se diffean.

---

## La barra superior distingue cada recurso y ya no se queda callada cuando hay más de los que caben

**2026-08-09** · schema v32 (sin cambio) · presentación

Lo que se nota: las nueve `ResourceType` que pueden llegar a aparecer
juntas en la barra superior tienen ahora siluetas distintas — Piedra no
es Piedra Pequeña, Comida no es Comida Silvestre, Madera no es Ramas,
Pociones es un frasco deliberado. Cuando el catálogo crezca y la barra
no quepa, la fila ya no se recorta en silencio: enseña un chip `+N`
cuyo tooltip lista los recursos ocultos con su cantidad exacta. Las
cantidades que cruzan los mil se abrevian (`1.2K`, `18.4K`, `1.1M`) en
la fila y se quedan exactas en el tooltip — la decisión de despachar
una expedición no se toma a ciegas.

### Estructural (S)

- Las prioridades de la barra superior y del panel lateral de la ciudad
  vienen de un único `ResourcePriority.Sequence` — antes cada superficie
  tenía su propia copia, lista a separarse en cuanto alguien añadiera
  un recurso.
- El tope visible (`MaxVisibleResourceChips = 5`) y la condición
  asociada (añadir el chip `+N` cuando hay más) viven como constante
  documentada en `CityStatusPanel`, no como un cálculo de layout en
  `_Process`.
- `CompactNumber.Format` aplica en la barra y en los tooltips;
  `FormatExact` reserva el separador de miles para los tooltips.
  Los tipos numéricos del dominio siguen siendo `int`.

### Iteración visual (V)

- Las siluetas nuevas de `ResourceIcon` se evalúan a 16 px dentro de la
  celda de 24 px que ya reservaba la barra. Coordenadas enteras, sin
  antialiasing, y curvar (tronco, manzana, frasco) se hace apilando
  rectángulos, no con curvas reales.
- La paleta por recurso se queda en la zona del material: gris para
  piedra, marrón para madera, verde para fibra, rojo para manzana, gris
  azulado para hierro, violeta para pociones. La modulación por acento
  de linaje se aplica encima como antes.
- El catálogo del showcase expone los nueve `ResourceType` con icono,
  nombre, cantidad pequeña y cantidad grande, así las colisiones de
  silueta son visibles de un vistazo y se puede verificar el formateador
  compacto en la misma captura.

---

## El panel de expediciones se dobla y deja de ahogar la pantalla, y sus piezas ya sirven a la futura pantalla de expediciones

**2026-08-09** · schema v32 (sin cambio) · presentación

Lo que se nota: la columna derecha del HUD macro, donde vive la lista de
expediciones en curso y la crónica, ahora se cierra como cualquier otro
panel del HUD. Cuando está plegada deja sólo el encabezado `EXPEDICIONES ·
N` con un chevron — la ciudad se queda con el mundo visible sin perder
el indicador de que hay algo pasando. Cada tarjeta de expedición ya no
imprime la lista entera de miembros (`Aster, Lira, ...`) en su cuerpo;
los nombres sobreviven sólo dentro del tooltip del botón "detalles". La
fase (`Outbound`, `Encounter`, etc.) ya no es una línea de texto suelta
sino un chip con glifo a la izquierda y la palabra localizada a la
derecha, igual que las superficies que ya reutilizan `HudBadge` y los
nuevos `HudStateBadge`. La hoja se cierra con el mismo gesto que el panel
de la ciudad; el estado del pliegue es efímero (no se guarda).

### Estructural (S)

- El rail pasa a ser un `VBoxContainer` con un `CollapsiblePanelHeader`
  en la parte superior; el cuerpo plegable contiene las tarjetas y la
  crónica. La crónica mantiene su propio pliegue interno
  (compact/full) — no se fusiona con el pliegue del rail, sino que se
  oculta junto con él cuando el rail se cierra.
- La cabecera del pliegue carga el formato `ui.expedition_rail.header`
  con un conteo: expediciones activas cuando hay alguna, filas
  compactadas de la crónica cuando la ciudad está en calma.
- El pliegue del rail es efímero: nada se serializa en `WorldSave`, ni
  en `EditorPrefs` ni en `ConfigFile`. La preferencia del jugador
  pertenece a la sesión, no a la partida guardada.
- `ExpeditionIcon.Leading(item)` resuelve el glifo de identidad a partir
  del `SupplyResource` del snapshot, reusando `ResourceIcon` (mismo
  mapeo que la barra superior y el panel de la ciudad). No se inventa
  ningún `biomeId` ni se accede al `RewardResource` — el snapshot no
  lo expone, y ampliarlo tocaría el dominio.
- `HudStateBadge` se añade como widget reutilizable (no sólo para
  expediciones) con la única responsabilidad de llevar glifo + etiqueta.
  Su mapa `IconFor(ExpeditionPhase)` vive como fuente única que usan
  la tarjeta, el showcase y los tests. `Outbound` y `Resolved` comparten
  marca de verificación a propósito; `Returning` y `Retreating`
  comparten flecha — la palabra localizada distingue las dos.

### Iteración visual (V)

- Tarjeta compacta: la identidad (recurso + nombre) sustituye al
  `Leaf` genérico. La fase pasa de `Label "HudCaption"` a `HudStateBadge`
  con glifo. Los miembros se eliminan del cuerpo y pasan al tooltip del
  botón "detalles" con la clave `ui.expedition_rail.members_tooltip`.
- Se conserva `StatChip.HudIconValue` para tiempo restante y suministros,
  `HudProgressBar` para el avance, y el botón cancelar cuando
  `CanCancel` lo permite.
- El showcase (`HudComponentShowcase`) gana una sección `STATE BADGE`
  con las seis fases, y un bloque `EXPEDITION REUSE PATTERNS` con cinco
  composiciones sólo visuales: `ExpeditionMemberCard`, `RouteNode`,
  `DecisionOption`, `BestiarySummaryCard`, `RewardItemCard`. Cada una
  está construida con `HudCard` + `HudMetricRow` + `HudProgressBar` +
  `IconButton` + `ResourceIcon` — ningún widget nuevo dedicado a
  expediciones. La composición prueba que `DecisionTray` no hace falta:
  las decisiones de 2-4 vías se cubren con `IconButton` + `HudButton`
  / `HudButtonSelected` que ya usa el panel de planificación.

### Tests

- Nuevos guardas estructurales (`HudCompositionTests`): el rail usa
  `CollapsiblePanelHeader`, no persiste el pliegue, la tarjeta compacta
  no renderiza los miembros en línea y usa `HudStateBadge`, y el mapa
  de fases cubre los seis valores del enum.
- Tests dedicados (`HudStateBadgePhaseMapTests`): cada `ExpeditionPhase`
  resuelve a un `res://` no vacío; `Returning` y `Retreating` comparten
  glifo a propósito; `Outbound` y `Returning` no se colapsan.
- Catálogo de localización: dos claves nuevas (`ui.expedition_rail.header`,
  `ui.expedition_rail.members_tooltip`) en EN y ES.
- Suite completa: 1111 superadas, 1 omitida (pre-existente), 0 fallidas.

---

## El panel lateral de la ciudad lee el estado real y la ciudad habla tu idioma al instante

**2026-08-09** · schema v32 (sin cambio) · presentación

Lo que se nota: la columna izquierda del HUD macro ya no se queda en
"población 3/3" como única cifra. Ahora muestra cinco renglones útiles
bajo `ESTADO` — horizonte de comida, ciudadanos trabajando, en casa,
próxima cosecha y jornada activa/pau­sada — y dos de ellos llevan una
advertencia visual cuando el dominio ya la justifica (comida por agotarse,
cose que llega después de que se acabe la comida). La lista de recursos
aparece en el orden que importa leerla — primero lo que alimenta, luego lo
que construye, al final lo demás. Y si el jugador cambia el idioma en
medio de la partida, el panel se reescribe sin esperar al próximo tick
de simulación.

### Estructural (S)

- La sección de identidad, la barra de progreso de vivienda y el orden de
  renderizado (identidad → estado → recursos → construcción) se conservan.
- `ConstructionQueueItem` ya no mezcla cadenas crudas en inglés con
  `UiText.Get` — cada causa de parada pasa por la clave correspondiente.
- El panel se suscribe a `LocaleChanged` y se da de baja en `_ExitTree`;
  el panel deja de depender de que llegue un evento de simulación para
  reescribir el idioma.
- El panel no añade nuevos dominios: las cifras siguen siendo
  `CityStatusSnapshot` — `FoodHorizonDays`, `CitizensAtWork`,
  `CitizensAtHome`, `TicksUntilFirstHarvest`, `IsLaborTime`,
  `HousingCapacity`. Cero métrica inventada.

### Iteración visual (V)

- Sección `ESTADO` añadida con cinco renglones `HudMetricRow` + una barra
  de vivienda al final. Las etiquetas y formatos van por
  `ui.city_summary.*` y `UiText.Format`.
- Recursos re-secuenciados por supervivencia → construcción → resto
  (`ResourceSequence`, orden estático).
- Las advertencias sólo se pintan cuando el dominio ya da la regla:
  `FoodHorizonDays < 1` y `TicksUntilFirstHarvest > FoodHorizonDays *
  TicksPerInGameDay`. La barra de vivienda no lleva glifo (no hay regla
  defendible).

### Tests

- Nuevos guardas estructurales (`HudCompositionTests`): el panel lee los
  cinco campos autorizados, los umbrales de advertencia sólo cubren los
  dos casos definidos por el dominio, la secuencia de recursos antepone
  comida, el panel se suscribe a `LocaleChanged`, y `ConstructionQueueItem`
  no conserva ninguna cadena cruda en inglés. Más una invariante de
  catálogo: cada clave nueva existe en ambos `.po` (EN y ES).
- Suite completa: 1068 superadas, 1 omitida (pre-existente), 0 fallidas.

### Capturas

- `$env:TEMP\wog-city-summary\city-summary-en-1280x720.png` y
  `…-1920x1080.png` (panel en inglés, sin y con obra activa).
- `…-city-summary-es-…` (panel en español, glifos y orden preservados).
- `…-city-summary-housing-full-…` (vivienda al 100%).
- `…-city-summary-no-construction-…` (sección de construcción vacía).
- Pendiente re-toma humana para `city-summary-low-food` (la captura
  automatizada compite con la progresión offline de la ranura
  persistente; el código de advertencia está verificado por test y
  disparo manual).

---

Entries dated before 2026-08-03 were reconstructed from commit subjects when
this file was introduced. They are deliberately thin: their detail was never
written down at the time, and inventing it now would be fabrication. Read
their commits for the real content.

---

## El dock primario deja de ser un strip de iconos y se separa la cámara del reloj

**2026-08-09** · schema v32 (sin cambio) · presentación

Lo que el jugador nota son tres cosas. La primera: la franja inferior ya no
tiene seis cuadrados anónimos — ahora lee cinco destinos con su nombre debajo
del icono (Héroe, Obra, Exp., Norma, Gente), igual que en la referencia
Proposal 06 que se guardó al inicio de la fundación del HUD. La segunda: el
botón de pausa ya no vive entre esos cinco destinos — vive, junto con el
selector de modo de cámara, en un pequeño cluster de iconos en el borde
derecho de la barra superior, donde nunca se confunde con un destino de
navegación. La tercera: el control de simulación de la esquina inferior
derecha se quedó en lo suyo, sólo pausa y velocidad — la cámara se fue al
cluster de arriba, donde el resto de las utilidades globales viven.

El bible ya decía `etiquetado`; la implementación anterior vivía en tensión
con esa línea. Este pase cierra esa distancia sin tocar el render del mundo,
la lógica de la cámara, `PlayPauseButton` o `SpeedButton`. La composición
macro sigue midiendo 240 px a la izquierda, 236 px a la derecha y 520×60 para
el dock primario etiquetado en el lienzo lógico fijo de 1280×720.

### Estructural (S)

- `GameUiShell`, `CityStatusPanel`, `ActionDock`, `ContextInspector`,
  `ChroniclePanel`, `ExpeditionRail`, `CitySummaryPanel`, `ModalHost`,
  `OverlayLayers`, `Tokens`, `IconButton`, `PlayPauseButton`, `SpeedButton`
  y `PauseMenu` (sólo cambia el valor por defecto de `OpenButtonPath`)
  intactos.
- `PrimaryNavDock` ↔ `ActionDock` siguen siendo mutuamente exclusivos en la
  misma zona inferior central.
- Cada superficie HUD sigue siendo dueña de su puntero
  (`MouseFilterEnum.Stop`); el ciclo de foco horizontal explícito se mantiene.
- La regla "el dock no puede ensancharse con la resolución" (UI_PATTERNS §11)
  sigue vigente: el nuevo tamaño vive en el lienzo lógico 1280, igual que
  antes.
- Los assets `art/references/` siguen siendo referencia, no contrato.

### Iteración visual (V)

- `PrimaryNavDock.custom_minimum_size` 300×52 → 520×60 (re-afirmar sólo tras
  visto bueno humano en ambos tamaños oficiales).
- Ancho por botón 40 → 88 (`PerButtonWidth` en el script, no literal).
- `SimulationControls.custom_minimum_size` 184×40 → 76×32.
- Nuevo `UtilityCluster` dentro de `CityStatusPanel` después de la población,
  antes del chip de guardado: dos `IconButton` 40×40 sólo icono con
  tooltips.

### Tests

- Relajados a rangos y a constantes con nombre: `Vector2(300, 52)` →
  rango `[480, 560] × [56, 72]`; `dockRect.Size != new Vector2(300, 52)` →
  `dockRect.Size != PrimaryNavDockSize` (constante en `CityPrototype.cs`).
- Mantenidos: superficies autoradas, propiedad de puntero, exclusión mutua,
  MacroActions/NavigationRail ausentes, aislamiento de flechas en el mundo,
  controles de simulación autorados y `FocusNeighborLeft/Right` cíclicos.
- Nuevos guards estructurales para el cambio: el dock ya no posee
  `MenuButton`, `CityStatusPanel` expone `UtilityCluster` con `CameraButton`
  y `MenuButton`, `PauseMenu.OpenButtonPath` apunta al nuevo menú, y
  `SimulationControls` ya no construye un `IconButton` de cámara.

### Verificación

- Build limpio (`dotnet build` → 0 errores, 0 advertencias).
- Suite completa: 1062 superadas, 1 omitida (pre-existente), 0 fallidas.
- Capturas antes/después en `$env:TEMP\wog-hud-convergence-{before,after}`
  a 1280×720 y 1920×1080.
- Pendiente: visto bueno humano sobre las dimensiones finales del dock
  etiquetado y el cluster de utilidades.

---

## La infraestructura de agentes deja de pagar release por cualquier cosa

### Modos y riesgo

Tres modos ahora — `SURGICAL`, `FEATURE`, `RELEASE` — y tres niveles de
riesgo — `LOW`, `MEDIUM`, `HIGH`. Un cambio cosmético ya no exige Full snapshot,
ni la matriz visual completa, ni una revisión de Quality Guardian con todo el
catálogo de invariantes. Un cambio de schema sigue exigiéndolo todo. El
escalado es ahora proporcional al riesgo, no al tamaño del commit.

Las decisiones viven en:

- `docs/ai/WORKFLOW_MODES.md`
- `docs/ai/RISK_MODEL.md`
- `docs/ai/DOMAIN_CONSULTATION.md` — leer estado existente no activa el dominio
- `docs/ai/DOCUMENTATION_IMPACT_GATE.md` — un doc sólo se abre si su contrato
  cambió
- `tools/Get-VerificationPlan.ps1` — recomendación determinista, sin LLM
- `docs/ai/AGENT_WORKFLOW_REFACTOR_REPORT.md` — métricas antes/después y el
  motivo de cada decisión

### Verificación proporcional

`Validate-AgentContext.ps1` ya no corre en cada commit: sólo cuando el diff
toca `.agents/`, `.claude/`, `.codex/`, `AGENTS.md`, `CLAUDE.md`, `docs/ai/`,
`scripts/`, `tools/`, o el instalador. Lo mismo para la validación de
localización y la captura visual. La matriz visual completa se reserva para
`RELEASE` o un cambio arquitectónico de presentación.

### Skills fuera del slice, borradas

Seis skills que el slice actual (2D pixel art, Godot 4 + C#/.NET,
single-player) nunca pidió:

- `godot-3d-essentials` — sin contenido 3D
- `godot-multiplayer` — single-player
- `game-ai` — los ciudadanos son personales, no FSM/A\*
- `godot-gdscript` — C#/.NET sólo, política local
- `godot-2d-movement` — sin avatar de jugador
- `router` — motor fijo, detección no aporta

Quitar estas seis (más seis archivos de `references/` y los espejos huérfanos
en `.claude/skills/` y `.codex/skills/`) baja la corpus siempre-cargado de 40 a
34 skills. `skills-lock.json`, `Install-GodotDotNetSkills.ps1` y el §11 del
validador se actualizaron al nuevo conjunto.

### Quality Guardian

Tres profundidades — `PRESENTATION_REVIEW`, `DOMAIN_REVIEW`, `SYSTEM_REVIEW` —
y una sola revisión por `FEATURE`. Antes corría una revisión completa después
de cada subtarea; ahora corre una vez, a la profundidad del riesgo.

### Lo que se quedó como estaba

- `DomainBoundaryTests` sigue activo — sin `using Godot` en
  `game/scripts/Domain/`.
- El `AuthorGuardHook` sigue activo.
- La separación dominio / presentación, dominio / motor, sigue intacta.
- `CityPrototype.cs` se queda con su 1,956 líneas: el dispatch de fixtures y
  el seam runtime están demasiado entrelazados para extraerlos sin poner en
  riesgo la matriz visual. El motivo queda en
  `AGENT_WORKFLOW_REFACTOR_REPORT.md` §12.

### Measured baseline

- `dotnet build`: 0 advertencias, 0 errores.
- `dotnet test`: 1058 superadas, 0 fallidas, 1 omitida (era 1015 / 0 / 1; las
  +43 vienen de los tests del refactor y de trabajo previo no medido).
- `Validate-AgentContext.ps1`: 474 / 474 checks.
- `Sync-AgentContext.ps1`: 34 skills, 8 agents, 0 errores.
- Ningún cambio al schema, a gameplay, a balance, a lore, a ciudad,
  expediciones o ciudadanos. Esta entrada es de infraestructura.

---

**2026-08-08** · schema v32 (sin cambio) · EG-5

Tres arreglos que sólo existen porque la entrada anterior puso superficies
permanentes donde antes no había ninguna. Ese es el patrón que vale la pena
notar: enmarcar la ciudad no rompió nada por sí solo, pero invalidó tres
supuestos que se habían escrito cuando la pantalla estaba vacía.

### Connected

- **Un hueco propio para el diálogo diegético.** El globo de la primera noche, el
  espíritu y las brasas pasan de `OverlayLayers.Tutorial` a
  `OverlayLayers.WorldDialogue`: por encima del tinte ambiental, por debajo de
  todo HUD persistente y de los modales. La decisión original —`DECISION_LOG.md`,
  la enmienda queda anotada bajo ella en vez de reescribirla— se tomó cuando no
  existían `CitySummaryPanel` ni `ExpeditionRail`, así que "por encima del HUD"
  era inofensivo; con las rails puestas, el diálogo las tapaba. El globo además
  se acota al corredor central, de modo que ninguna de las dos rails queda
  cubierta y la línea completa se sigue leyendo. Los clics fuera del globo siguen
  llegando al mundo.
- **Una flecha deja de hacer dos cosas a la vez.** Con la vista macro sin
  obstruir, las flechas físicas se consumen antes del reparto de foco de GUI, así
  que una sola pulsación ya no puede desplazar la cámara *y* mover el anillo de
  foco del dock al mismo tiempo. El D-pad del mando sigue siendo la vía explícita
  de navegación por foco, y con un modal o un menú contextual abierto sus
  controles recuperan la propiedad normal del teclado.
- **La ruta Construcción → Héroe espera a que el modal se cierre.** Un clic real
  en *View Hero* pide el cierre del modal antes de seleccionar el perfil a
  pantalla completa, de modo que el panel y su scrim ya no sobreviven a la
  transición por encima de la pantalla nueva.

### Verified

Build 0 errores / 0 advertencias. Tests **1044 / 1045** (1 omitido conocido,
previo), 3 más que la entrada anterior. Arranque headless limpio. Contexto de
agentes 517/517. Localización 976 plantillas y 288 claves de runtime. Esquema sin
tocar en v32.

Cuatro filas nuevas del matrix, todas con entrada real y no con llamadas a
método: `macro-arrow-focus-isolation` y `pause-arrow-focus` inyectan teclas
físicas —Derecha mueve la cámara sin cambiar el foco del dock; con Pausa abierta,
Abajo mueve Resume → Settings y la cámara no se mueve—, `construction-hero-route`
usa un puntero real, y `firstnight-manifested` comprueba que el diálogo queda
detrás de ambas rails. Firmado a 1280×720 y 1920×1080.

**No verificado:** el harness reportó picos aislados de arranque y de frame por
encima de los 40 ms en los fixtures de diálogo y flechas. Esta pasada **no hace
ninguna afirmación de rendimiento**; queda como medición pendiente, no como
resultado.

## La fundación del HUD deja de ser un escaparate y se convierte en la pantalla

**2026-08-08** · schema v32 (sin cambio) · EG-5

La entrada anterior dejó una fundación que nada consumía. Ahora la consume el
juego entero: las cuatro superficies que aquel trabajo tenía explícitamente fuera
de alcance existen, y las pantallas conectadas —Construcción, Expediciones,
Políticas, Ciudadanos, Pausa— pasan a compartir su tipografía y sus roles de
botón en vez de mantener cada una su propio vocabulario.

Lo que un jugador ve: la ciudad enmarcada por superficies estables en vez de
rodeada de controles sueltos. Arriba una barra de 40 px de borde a borde con
marca, contexto de linaje/día/hora, un marcador de recursos sólo-icono respaldado
por el ledger, y población/capacidad reales. A la izquierda `CitySummaryPanel`
(240 px), a la derecha `ExpeditionRail` (236 px). Abajo al centro un
`PrimaryNavDock` de 300×52 sólo-icono que **cede su zona** al `ActionDock` de
480×72 durante la colocación, y abajo a la derecha `SimulationControls` con
play/pausa, velocidad y modo de cámara.

### Connected

- **Dos superficies desaparecen en vez de acumularse.** `NavigationRail` se borra
  entera a favor de `PrimaryNavDock`, y `OfflineReportPanel` a favor de un
  `ChroniclePanel` único embebido en el rail: modo compacto con cuatro filas
  significativas, modo expandido que reemplaza el resumen de expediciones dentro
  del mismo rail y añade el resumen offline, los bloqueadores agrupados y hasta
  80 eventos compactados. `ChronicleEventProjection` queda como **única** regla
  de filtrado y compactación, de modo que no hay una segunda superficie de log
  que pueda discrepar de la primera.
- **Las primitivas de la fundación se usan tal cual.** `ConstructionQueueItem` y
  `ExpeditionCompactCard` son composiciones sobre ellas, no un segundo sistema de
  marcos: el estado bloqueado se escribe como texto localizado y tooltip, nunca
  sólo por color.
- **Dos iconos promovidos por un estado real.** `backpack.svg` para Expediciones
  y `clipboard-note.svg` para Políticas — el dock sólo-icono no puede repetir un
  glifo para acciones distintas. `game/assets/ui/icons/24/` pasa a 33 SVG. Los
  seis iconos ya promovidos que traían `currentColor` se reescriben a relleno
  blanco: `currentColor` importa como negro y un tinte multiplicativo no puede
  aclararlo, así que el HUD no podía teñirlos.
- **El rail y el dock reclaman su propio input**, rueda incluida en los límites
  de scroll, y la cadena de foco vertical alcanza detalles, cancelar válido y el
  toggle del Chronicle.

### Verified

Build 0 errores / 0 advertencias. Tests **1041 / 1042** (1 omitido conocido,
previo), 26 más que la entrada anterior. Arranque headless limpio. Contexto de
agentes 517/517. Catálogos de localización válidos: 976 identificadores de
plantilla y 288 claves de runtime. Esquema sin tocar en v32.

`UI_AUDIT.md` §4 registra la firma humana del 2026-08-08 a 1280×720 y 1920×1080
para el Chronicle embebido, el dock de 300×52 y las cinco pantallas conectadas.

**No verificado en esta sesión:** las capturas de esa firma viven en `%TEMP%` como
artefactos de revisión y no se volvieron a generar aquí; lo medido arriba es
build, tests, arranque, contexto y localización, no una segunda pasada visual.

## El HUD gana una escala propia, y un borde de un solo píxel

**2026-08-08** · schema v32 (sin cambio) · EG-5

La propuesta 06 —`art/references/Proposal 06 — minimalist workstation.png`— pone
en pantalla el doble de filas que la escala actual permite. Medida contra el
viewport lógico de 1280×720, la diferencia no está en la paleta: sus rellenos
(luminancia 8, 12 y 20) ya coinciden casi exactamente con los `panel_elevated` y
`panel_card` que el proyecto tiene. Está en dos cosas — **el borde es de 1 px** y
el texto baja a 14–20.

Esto es sólo la fundación. Ninguna superficie del HUD la consume todavía, y eso
es deliberado: `TopStatusBar`, `CitySummaryPanel`, `NavDock` y `ExpeditionRail`
quedan fuera del alcance.

### Connected

- **Un solo tile sostiene todo el cromo del HUD.** Medí el grosor de marco de
  todos los tiles huecos del pack: los rectangulares van de 3 a 6 px, y el único
  artefacto de **1 px** es `Small tiles/Thin outline/tile_0069`, un contorno
  redondeado de 10×10. De él salen **7 composites** —`hud_surface`, `hud_inset`,
  `hud_card`, `hud_button`, `hud_button_selected`, `hud_button_danger`,
  `hud_badge`— que se diferencian sólo por el relleno, que es exactamente lo que
  hace la referencia. Hover, pressed y disabled reutilizan esos PNG con
  `modulate_color`; `hud_dock.tres` es `hud_surface.png` con otro padding.
- **Corrección registrada.** Este trabajo empezó asumiendo que ningún recurso del
  pack podía dar 1 px y que haría falta `StyleBoxFlat`. Era falso: `tile_0069` y
  `tile_0092` estaban señalados como candidatos pero sin medir. Al medirlos, la
  decisión se invirtió y todo marco del HUD es un asset Kenney real. `StyleBoxFlat`
  sobrevive sólo en el **relleno** de progreso, y `StyleBoxLine` en el separador —
  ambos nombrados uno a uno en el test, para que un tercero tenga que ser una
  edición visible.
- **Dos composites promovidos y retirados.** `hud_card` y `hud_dock` se hornearon
  primero desde `tile_0019` (3 px) y `tile_0018` (4 px) y se pusieron al lado del
  de 1 px en el escaparate. A 1920×1080 los remaches de esquina de `tile_0019` se
  leían como artefactos y `tile_0018` doblaba su propio borde: ambos se
  reconstruyeron sobre `tile_0069` y los PNG perdedores se borraron en vez de
  quedarse sin consumidor.
- **Perfil tipográfico aislado.** `HudBrand` 20, `HudHeader` 18, `HudLabel` 16,
  `HudBody` 16, `HudNumeric` 16, `HudCaption` **14** — el primer tamaño por debajo
  del suelo de 16 que tenía el proyecto. Ninguna variación de pantalla se tocó: el
  diff del tema son 121 inserciones y cero borrados, y
  `ScreenVariations_AreUnchangedByTheHudProfile` falla si alguien edita `BodyText`
  creyendo que edita `HudBody`.
- **Seis primitivas `[GlobalClass]`**: `HudSectionHeader`, `HudMetricRow`,
  `HudResourceRow`, `HudProgressBar`, `HudBadge` y `CollapsiblePanelHeader`.
- **`HudIconValue` no existe, a propósito.** Era `StatChip` con el hueco cambiado:
  misma celda de icono de 24 px, misma altura, y la variación de etiqueta ya era
  un parámetro. El hueco pasó a ser parámetro y `StatChip.HudIconValue(...)` nombra
  el rol. El escaparate existe para cazar justo eso, y cazó éste.
- **El linaje no repinta el HUD.** Los assets de linaje son marcos de 6–8 px y el
  painter normaliza márgenes a 14/12, más del doble de lo que cabe en una fila de
  24 px: pintarlos sobre `HudSurface` cambiaría *minimum sizes*, que los
  invariantes prohíben a un tema de linaje. La identidad llega por `IconAccent`.

### Verified

Build 0 errores / 0 advertencias. Tests **1015 / 1016** (1 omitido conocido,
previo a este cambio), de los cuales **34 nuevos**; comprobé que los guardias
fallan de verdad mutando el tema en dos sitios antes de revertir. Arranque
headless limpio. Contexto de agentes 448/448. Importaciones de fuentes pixel
válidas. Capturas reales de `HudComponentShowcase.tscn` a **1280×720 y
1920×1080**: los 14 px se leyeron a 3× sobre la captura de 720p —píxeles sólidos,
sin franja gris— que es la única evidencia que vale para bajar del suelo
anterior. El bloque de advertencia se comprobó **desaturado a escala de grises**:
el signo `[!]`, el glifo, el `-9` y el `94%` sobreviven todos sin color.

**No verificado:** nada consume la fundación todavía, así que no hay prueba de
que resista una composición real de HUD. Y las filas son de **24 px donde la
referencia usa 22**: los Pixelarticons son SVG de rejilla 24 estricta con trazos
de una unidad, y re-rasterizarlos a 0.667 deja cada borde en coordenada
fraccionaria. Cerrar esos 2 px necesita iconos dibujados en rejilla menor, que es
trabajo de arte y no una constante de layout.

## Un solo pack de UI en el juego, y la medida real de lo que ese pack puede dar

**2026-08-07** · schema v32 (sin cambio) · EG-5

`ButtonWarning` venía del kit viejo `art/Kenney/`: un tile de 16×16 escalado 3×
a 48×48, con un borde tres veces más gordo que la pizarra nativa de 32×32 que
usan `ButtonText` y `ButtonPrimary` justo al lado. La guía de iconografía prohíbe
explícitamente *"mezclar componentes de varios paquetes de UI sin ajustar
previamente su estilo"*, y esa mezcla vivía dentro de una misma familia de
botones. El rojo destructivo y el verde de progreso pasan al pack Pixel
Adventure, nativos, y con eso **`game/assets/ui/kenney/` deja de tener un solo
consumidor y se borra entero** — los cinco archivos muertos que arrastraba
(`ancient_brown`, `ancient_grey`, `grey`, `grey_pressed`, `green_pressed`) se van
con él en vez de podarse uno a uno.

Lo que un jugador ve: el botón destructivo ya no desentona junto a los demás. Lo
que el repositorio gana es más grande que eso, y es una medición.

### Connected

- **7 de 504 tiles importados.** `Tiles/Small tiles/Thick outline/tile_0071` →
  `red`, `tile_0070` → `red_outlined` (el contorno blanco que el pack ya trae
  como estado resaltado, ahora el `hover`), `tile_0075` → `green`.
  `red_pressed` reutiliza `red.png` con `modulate_color`, así que no se promovió
  ningún asset para un cambio de brillo. `content_margin` sigue siendo 16/4: no
  se movió ninguna métrica de layout.
- **`tools/New-KenneyContactSheet.ps1`.** El pack no trae nombres semánticos —
  todo es `tile_NNNN.png`, sin XML— así que un tile sólo se puede identificar
  mirándolo. El script compone los tiles con vecino más cercano y rotula cada
  uno con su índice sobre la propia rejilla del pack, de modo que
  `índice = fila * columnas + columna` se sostiene y una promoción puede citar un
  índice real en lugar de una suposición.
- **Los tiles Large son 9-slice; los Small no.** `slate_raised_dark.png` es
  1020/1024 opaco y llena su lienzo, luego `texture_margin = 8` es correcto. Los
  Small son sprites de ~10×10 centrados en un lienzo de 16×16 con 3 px de
  relleno transparente: con `texture_margin = 4` el corte parte el borde y deja
  el anillo de luz interior dentro del centro que se tilea, y eso se ve como una
  rejilla de puntos repetida. El valor correcto es **6**, que apoya el centro
  sobre el interior uniforme de 4×4. Verificado a 1280×720 y 1920×1080.
- **El pack no tiene ningún tile oscuro.** El centro opaco más oscuro de los 91
  tiles Large es luminancia **114**; `StyleBoxFlat_panel` es 17 y
  `StyleBoxFlat_panel_elevated` es 11. Las piezas de pizarra tienen 4–6 tonos
  distintos, así que oscurecer una con `modulate_color` hasta el valor del
  proyecto comprime su rango tonal a ~7/255 — indistinguible de un relleno
  plano. **Este pack puede dar botones, chips, casillas y widgets pequeños;
  no puede dar las superficies oscuras de panel de este juego.** Por eso
  `OverlayPanel`, `Panel`, `PanelCard`, `ScrollContainer` y `StatusStrip` siguen
  siendo `StyleBoxFlat`, ahora con la justificación concreta escrita en
  `ASSET_INVENTORY.md` en lugar de por omisión.
- **`PanelAction`, `PanelIdle` y `PanelWarning` fuera del tema.** Las tres
  resolvían al *mismo* stylebox que `PanelCard` y ninguna escena ni script las
  referenciaba: prometían una distinción que el tema no entregaba, de forma que
  un panel `PanelWarning` se veía exactamente igual que una tarjeta neutra.
- **`UI_PATTERNS.md` §5 decía algo falso.** Afirmaba que el tema registra un tipo
  base `Button` con valores por defecto; no existe, y un `Button` sin variación
  cae a la fuente y los styleboxes grises del motor. Queda corregido y anotado:
  registrarlo es una red de seguridad real, pero también da márgenes de
  contenido nuevos a cualquier botón sin anotar —un cambio de tamaño— así que
  pertenece a un pase que pueda revisar las superficies afectadas.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido del
snapshot JSON de `VerticalLoopPersistenceTests`, previo a este cambio). Arranque
headless limpio. Contexto de agentes 448/448. Localización válida: 922
identificadores de plantilla y 283 claves de runtime, sin claves nuevas.
Importaciones de fuentes pixel válidas. Capturas reales a **1280×720 y
1920×1080** de `pause-menu-reset` (el rojo nuevo junto al botón pizarra, con el
anillo de foco dorado), `expedition-idle` y `construction-scroll`.

**No verificado:** el relleno verde de `ProgressBar` no se vio en vivo — ningún
fixture del estado inicial muestra una barra con progreso. Su geometría es la
misma que la del rojo, ya comprobada, pero queda como comprobación manual, junto
con el aplastamiento de los cortes cuando la razón de llenado es menor que los
márgenes.

## ESC durante la colocación, comprobado con una tecla de verdad

**2026-08-07** · schema v32 (sin cambio) · EG-5

El dock contextual se metió entre el jugador y el mundo, y sus botones pueden tomar
el foco. Que ESC siguiera cancelando la colocación estaba en el código
—`_placementActive && ui_cancel` en `_UnhandledInput`— pero **leer el código no es
verificar**, y la matriz visual no tenía ninguna fila que ejerciera una tecla.

La fixture `construction-placement-escape` entra en modo colocación e inyecta un
`ui_cancel` real por `Input.ParseInputEvent`, no llamando a `CancelPlacement`. La
captura confirma el recorrido entero: el dock desaparece, los solares de colocación
se limpian, vuelve el menú de construcción y el botón del rail muestra su glifo de
cierre.

Se envía pulsación **y** liberación: `IsActionPressed` solo dispara en el flanco, y
una acción encallada se filtraría a la siguiente fixture.

### Connected

- **Ocho guardas nuevas de composición del HUD** (`HudCompositionTests`), que
  afirman la escena y no los píxeles: las propiedades que merece la pena proteger son
  justo las que una captura esconde. Un rail que vuelve a `mouse_filter = 2` se ve
  igual mientras deja pasar sus clics al mundo; un inspector que pierde
  `grow_vertical = 0` se dibuja bien hasta que su texto salta a una segunda línea.
  **Cada guarda se comprobó rompiendo lo que guarda**: inyectar `mouse_filter = 2` en
  el rail y borrar `grow_vertical = 0` del inspector hizo fallar exactamente esas dos
  y ninguna más. Una guarda que no puede fallar no es una guarda.
- Cubren además superficies que ninguna fixture alcanza: `AssignmentPanel` y
  `ProductionPanel` se ocultan para hogares y Ayuntamiento.

### Verified

Build 0 errores / 0 advertencias. Tests **981 / 982** (1 omitido conocido), desde 973.
Arranque headless limpio. Contexto de agentes 448/448. Captura de la fixture nueva a
1280×720.

## El chip del reloj deja de recortar, y el espaciado empieza a tener nombres

**2026-08-07** · schema v32 (sin cambio) · EG-5

Subir el icono de chip de 12 a sus 24 px reales en la entrada anterior arrastraba
una consecuencia que no era visible: `ClockChipWidth` valía `180f` con el
presupuesto calculado para un icono de 12, y su envoltorio tiene `ClipContents =
true`. Con el icono real, "Día 99 · 23:59" habría perdido los últimos dígitos **sin
ningún error visible**. Ahora se deriva del propio token —`168f +
Tokens.IconInline`— de modo que el presupuesto no puede volver a desincronizarse del
icono.

Y el espaciado empieza a tener vocabulario. Un recuento de las 71 llamadas literales
a `AddThemeConstantOverride` da 2, 4, 6, 8, 10, 12, 16, 18, 20, 22, 24 y 28: **no es
una escala, es un reparto casi continuo**, con el 18 sentado de forma incómoda entre
16 y 20. Nombrarlos es seguro y convierte un futuro re-escalado en una edición por
token; colapsarlos a un solo paso mueve métricas en superficies que ninguna fixture
dibuja, y eso pertenece a su propio pase con el escaparate abierto.

### Connected

- **Cinco tokens nuevos** —`SpacingRelaxed` 10, `SpacingComfortable` 12,
  `SpacingWide` 16, `SpacingSection` 20, `SpacingBlock` 24— y barrido de
  `ConstructionPanel`, `OfflineReportPanel` y `PoliciesPanel`. Se eligieron esos tres
  porque **tienen fixture**: el barrido se puede demostrar, no solo afirmar.
- El comentario de `Tokens` deja escrito que la escala todavía no es un ritmo, para
  que nadie la lea como si lo fuera.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido).
Arranque headless limpio. `construction-scroll` recapturado y comparado píxel a
píxel con la captura previa al barrido: **0 píxeles distintos** en los 316 800 de la
región del panel. El barrido cambia el espaciado interno; que el contenido interno
sea idéntico es la prueba de que no movió ninguna métrica.

## Un escaparate de componentes, y el icono que llevaba tiempo pisando su etiqueta

**2026-08-07** · schema v32 (sin cambio) · EG-5

La verificación era el punto débil de todo este trabajo. Varias primitivas solo se
alcanzan desde estados de juego estrechos: `AssignmentPanel` y `ProductionPanel` se
ocultan para hogares y para el Ayuntamiento
(`Visible = !isHome && !isTownHall`), así que **ninguna fixture de la matriz visual
las dibuja** y un cambio en su superficie no se podía ver. `ComponentShowcase.tscn`
pone todas las primitivas compartidas en una pantalla, revisable a las dos medidas
oficiales cuando haga falta.

Se amortizó en la primera captura.

### Connected

- **`Tokens.IconInline` pasa de 12 a 24, y `ChipHeight` de 20 a 24.** 12 reservaba
  la mitad del espacio que el glifo ocupaba de verdad:
  `TextureRect.StretchModeEnum.Keep` dibuja la textura a su tamaño natural por
  pequeño que sea el rect, y los iconos se importan a `svg/scale=1.0` sobre origen
  24×24. Resultado: **cada icono de chip se desbordaba a la derecha sobre su propia
  etiqueta y hacia abajo sobre el chip siguiente** — visible en la barra de estado
  desde antes de esta sesión, con la luna encajada contra "Día 1 · 05:59". La
  alternativa, escalar arte de píxel de 24 a 12, es un 0.5× que se come los trazos
  de un píxel; reservar el tamaño real cuesta 12 px de ancho por chip y conserva los
  glifos intactos.
- **La última columna del escaparate compone una ficha de expedición sin nada más
  que primitivas de ciudad** — `StatChip`, `ActionButton`, `PanelCard`. Es la prueba
  de reutilización en miniatura: si una pantalla de expedición necesita widgets
  propios, ahí es donde aparece primero.
- **Una fila nueva en la matriz visual**, con la instrucción de revisarla antes de
  tocar `Ui/Tokens.cs`, un `StatChip` o una variación de panel.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido).
Arranque headless limpio. Contexto de agentes 448/448. Escaparate y `macro-current`
capturados a 1280×720 tras el arreglo: los iconos quedan separados de su etiqueta y
la luna de la barra de estado ya no pisa la fecha.

## El linaje llega al panel por el tema, no rodeándolo

**2026-08-07** · schema v32 (sin cambio) · EG-5

Catorce superficies pedían su cromo de linaje llamando
`AddThemeStyleboxOverride("panel", LineageThemeRegistry.GetStyleBox(...))` sobre sí
mismas. Eso costaba tres cosas: un override local gana al tema, así que
`default_theme.tres` **no era** la autoridad visual que dice ser; casi todas lo
aplicaban una vez en `_Ready` y nunca más, de modo que cambiar de linaje a media
sesión las dejaba mostrando el anterior; y eran catorce oportunidades de hacerlo
distinto.

El motivo por el que *tenían* que sobrescribir es más aburrido de lo que parece: el
tema registraba `Panel` —el control `Panel`— pero **nunca registró
`PanelContainer`**, que es lo que estas superficies son en realidad. Un
`PanelContainer` pelado caía al stylebox gris del motor, y sobrescribir era la
única forma de parecerse al proyecto. Ya está registrado.

### Connected

- **`Ui/LineageThemePainter`.** Escribe la superficie del linaje activo en
  `PanelContainer`, `Panel` y `PanelCard` del tema del proyecto cuando el linaje
  cambia. `AssignmentPanel` y `ProductionPanel` dejan de sobrescribir y de
  suscribirse a `LineageChanged`: reciben exactamente el mismo stylebox que antes,
  ahora por el tema.
- **Solo los ocho linajes reales reciben marco de linaje.** Pedirle al registro
  cualquier otra cosa devuelve su *fallback*, `slate_raised_dark` —la textura de
  botón elevado, último recurso para que ninguna superficie quede sin estilo—. No
  es una superficie de tarjeta, y pintarla sobre `PanelCard` sustituía el composite
  autoral por pizarra de medio tono. **Medido, no supuesto:** movía el marco del
  rail de `(158,135,92)` a `(161,192,202)`. Con el linaje `default` el composite se
  conserva intacto.
- **Los márgenes de contenido se normalizan, no se heredan.** Los assets de linaje
  traen 8/7 y la tarjeta neutra 14/12. Un tema de linaje puede cambiar paleta,
  bordes, esquinas y rellenos; los invariantes le prohíben cambiar tamaños mínimos,
  y el padding es layout.

Quedan tres overrides del patrón, a propósito: `ConstructionPanel` es un
`OverlayPanel` y quitárselo cambiaría cómo se leen los modales; `CityStatusPanel`
muta los márgenes de contenido para la HUD compacta.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido).
Arranque headless limpio. Contexto de agentes 448/448. Localización 922/283. La
ciudad guardada es Vaelun, así que la captura de `macro-current` muestra el rail con
el marco de Vaelun llegado por el tema — que es justamente la función.

## Las acciones de colocación dejan de flotar sobre el mundo

**2026-08-07** · schema v32 (sin cambio) · EG-5

El modo de colocación repartía su interfaz por la pantalla: la instrucción era un
`Label` anclado **arriba del todo**, y los botones Confirmar/Cancelar un
`HBoxContainer` abajo construido con un `new Button` crudo y **sin superficie
ninguna**, de modo que las acciones flotaban directamente sobre el terreno. Dos
nodos, dos banderas de visibilidad, y la instrucción tan lejos de sus propios
botones como permitía el viewport.

`Ui/ActionDock.cs` los reúne en una bandeja inferior centrada con cromo real: la
instrucción y sus acciones en una sola superficie con un solo `Visible`. No es una
barra permanente — nadie la muestra salvo un modo que tenga algo que ofrecer, y la
ciudad queda despejada en cuanto ese modo termina. Esa misma forma es la que
necesitará una expedición para su bandeja de despacho o retirada.

### Connected

- **Construir los hijos al primer acceso, no solo en `_Ready`.** Segunda vez que
  aparece la misma trampa: el macro view precede al dock en la escena y etiqueta
  sus acciones desde su propio `_Ready`, que corre antes, así que construir solo en
  `_Ready` le entregaba botones nulos y **rompía el arranque**. `EnsureBuilt` es
  idempotente, con la misma forma que `CityStatusPanel.EnsureBuilt`. Queda escrito
  en `UI_PATTERNS.md` como regla: toda superficie compartida que una pantalla toque
  desde su `_Ready` debe resolverse al acceder.
- **`_placementInstruction.Visible` + `_placementFooter.Visible`** se reducen a
  `_actionDock.Show()` / `.Hide()`.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido).
Arranque headless limpio. Fixture `construction-placement` capturado a **1280×720 y
1920×1080**: la bandeja aparece abajo al centro con la instrucción, "Confirmar
ubicación" correctamente deshabilitado y "Cancelar", y el rail se oculta durante el
modo de colocación.

## El panel de selección deja de recolocarse cada frame

**2026-08-07** · schema v32 (sin cambio) · EG-5

`SelectionInfoPanel` lo construía el macro view en runtime y **se recolocaba en
`_Process` en cada frame mientras estaba visible**. El sondeo no era gratuito ni
arbitrario: una colocación de una sola vez competía con el asentamiento del tamaño
mínimo del contenedor de Godot y calculaba durante un instante un panel
absurdamente alto. La solución no era colocar mejor, era **dejar de calcular la
posición**.

Pasa a `Ui/ContextInspector.cs`, declarado en la escena y anclado abajo a la
izquierda con `grow_vertical = Begin`: queda fijado al borde inferior y crece hacia
arriba cuando su texto se parte. Sin callback por frame y sin carrera. La regla
general que deja escrita: **si un widget se recoloca en `_Process`, normalmente los
anclajes están mal.**

El nombre cambia porque el papel se amplía: `ShowSelection` toma un trío
icono/título/detalle, así que árboles, edificios y ciudadanos comparten una
superficie, y una pantalla de expedición podrá apuntar a la misma con un nodo de
ruta o una entrada de bestiario.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido).
Arranque headless limpio. Contexto de agentes 448/448. Localización 922/283.
Verificado con un clic real sobre un árbol del mundo: el inspector aparece anclado
y completo a **1280×720 y 1920×1080**, sin `_Process`.

## La navegación deja de cruzar la pantalla y se recoge en un rail

**2026-08-07** · schema v32 (sin cambio) · EG-5

`MacroActions` era una franja de 42 px de borde a borde justo bajo la barra de
estado: gastaba el ancho completo del viewport para siete botones y partía el
mundo en horizontal. Pasa a ser `NavigationRail`, un grupo vertical arriba a la
izquierda que se ajusta a su propio contenido.

Lo que un jugador ve: la ciudad recupera la franja superior entera y el mundo se
lee de un borde al otro. La navegación sigue permanentemente visible, ahora en
una esquina, con el marco de bisel del composite.

### Connected

- **`Ui/NavigationRail.cs`.** Posee su propia estructura y devuelve los botones
  como propiedades tipadas, así que `MacroStreetLiveView` —4576 líneas— guarda
  **una** ruta al rail en lugar de cinco rutas literales del tipo
  `"../MacroActions/Actions/PoliciesButton"`. Decidir qué abre cada botón sigue
  siendo del macro view: el rail es cromo, y el cromo no sabe qué es una pantalla.
- **Resolver los hijos al acceder, no en `_Ready`.** El macro view precede al rail
  en `CityPrototype.tscn` y Godot inicializa los hermanos en orden de árbol, así
  que cachear los botones en el `_Ready` del rail devolvía null y **rompía el
  arranque**. Reordenar la escena habría arreglado ese consumidor y habría dejado
  la trampa puesta para el siguiente.
- **El rail no se ensancha a mayor resolución, y no puede.** `project.godot` usa
  `stretch/mode=canvas_items` sobre una base 16:9 de 1280×720, de modo que
  1920×1080 es **el mismo** viewport lógico dibujado a 1.5×:
  `GetVisibleRect().Size.X` vale 1280 en las dos medidas oficiales de revisión. No
  hay espacio extra al que expandirse. Se quitó el código responsive que lo
  intentaba en vez de dejarlo como adorno muerto.
- **`users.svg` y `camera.svg` promovidos.** El rail dibujaba el héroe, el censo y
  el modo de cámara con el mismo `user.svg`. Con etiquetas de texto se toleraba;
  al recoger el rail a solo iconos, tres acciones sin relación quedaban como el
  mismo glifo repetido. Son navegación genérica, que la guía de iconografía asigna
  a Pixelarticons, así que el modelo de tres capas funciona como está previsto.
- **`IconButton.ShowLabel`.** Colapsa el botón a su icono conservando
  `ButtonText`. Vive en `IconButton` y no en el rail porque `SetIconAndLabel`
  tiene otros consumidores —el macro view reescribe los botones de construcción y
  cámara según su estado— y aplicar el colapso desde fuera habría hecho que la
  siguiente de esas escrituras restaurara el texto en silencio.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido).
Arranque headless limpio. Contexto de agentes 448/448. Localización 922/283.
Importaciones de fuentes pixel válidas.

**Verificado con clics reales**, no leyendo código: un clic en el botón de
construcción del rail abre el panel *y* el segundo botón cambia a su glifo de
cierre **sin** recuperar la etiqueta, que es la prueba de que `ShowLabel` aguanta
las mutaciones del macro view; un clic en un árbol del mundo sigue seleccionándolo
y poblando el panel de selección; el menú de pausa sigue encontrando su botón tras
cambiar la ruta del nodo. Capturas a 1280×720 y 1920×1080.

**Pendiente de esta fase:** el `ContextInspector` y el `ActionDock` del plan no
existen todavía. `SelectionInfoPanel` sigue construyéndose en runtime y sondeando
`_Process`, y el footer de colocación sigue siendo un `new Button` sin superficie.

## Los paneles ganan marco autoral sin ceder la paleta

**2026-08-07** · schema v32 (sin cambio) · EG-5

La entrada anterior midió que el pack no tiene ningún tile oscuro y dejó los
paneles en `StyleBoxFlat` con esa justificación. Queda una salida que la medición
no cerraba: el pack **sí** trae tiles cuyo centro es completamente transparente
(`tile_0008`, `tile_0009`, `tile_0019`, `tile_0032` en el set Large). Si el
relleno del proyecto se hornea dentro de ese marco, el resultado conserva el
marco de píxel autoral *y* la paleta — algo que ni el tile crudo ni un
`modulate_color` podían dar.

Lo que un jugador ve: el borde de los paneles pasa de una línea plana a un bisel
de 3–4 tonos con el ornamento de esquina del pack, y el interior sigue siendo el
mismo casi-negro de antes.

### Connected

- **`tools/New-CompositeStylebox.ps1`.** Toma un tile de marco hueco, inunda el
  interior *encerrado* con un color de relleno y remapea los tonos del marco sobre
  una rampa del proyecto. Dos reglas hacen que el resultado sea exacto y no
  aproximado: el relleno parte del centro y se detiene en el marco, así que los
  píxeles transparentes de fuera de una esquina redondeada siguen transparentes y
  el tile conserva su silueta; y el remapeo es uno a uno por luminancia, de modo
  que la rampa debe traer exactamente tantos colores como tonos tenga el tile —
  un desajuste es un error, no un ajuste silencioso al más cercano.
- **`panel_card` y `panel_elevated`.** `Panel`/`PanelCard` toman `tile_0008` con
  relleno `14,17,23,246` y rampa tostada; `OverlayPanel` toma `tile_0009` con
  relleno `9,11,16,251` y rampa dorada. Ambos rellenos y ambos tonos de borde son
  los valores que ya llevaba el `StyleBoxFlat` anterior, así que **la paleta no se
  movió**, y el dorado sigue reservado al panel elevado como decidió el pase del
  2026-08-06. `content_margin` sigue en 14/12 y 18/16: tampoco se movió ninguna
  métrica.
- **Cada PNG generado lleva su `.recipe.json`** con el tile de origen, el relleno,
  el mapeo de tonos y el recuento de píxeles interiores, así que el asset es
  reproducible desde el repositorio. Es el mismo patrón que ya siguen los paneles
  de linaje generados bajo `game/assets/ui/lineages/<linaje>/panel/`.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido).
Arranque headless limpio. Contexto de agentes 448/448. Localización 922/283, sin
claves nuevas. Capturas reales a 1280×720 y 1920×1080 de `expedition-idle`,
`construction-scroll` y `shelter-resources`, más un recorte a 6× de la esquina
del modal para leer el bisel.

**Dos consecuencias que no son ganancia neta y quedan anotadas:**
`OverlayPanel` **pierde su sombra** — `StyleBoxTexture` no tiene `shadow_size` y
la caja plana llevaba una difusa de 8 px; para un proyecto cuyos invariantes
piden píxel puro sin antialiasing la sombra difusa ya era discutible, pero es un
cambio real. Y **el composite no llega a los paneles que sobrescriben el tema**:
17 de las 24 llamadas a `AddThemeStyleboxOverride` del repositorio aplican
`LineageThemeRegistry.GetStyleBox("panel")` directamente sobre el
`PanelContainer`, y un override local gana al tema —
`ResourceInventoryPanel` en la vista del refugio es el caso visible. Unificar el
tematizado por linaje con el tema, en vez de sobrescribirlo, es el trabajo
arquitectónico que queda.

## La cara Dominio del cubo deja de llamarse Mastery y los migrantes dejan de ser gemelos

**2026-08-07** · schema v31 → v32 · EG-5

El Cubo Kovari arrastraba `Mastery` como nombre de su tercera cara —en disco,
en los DTOs, en la UI— y el nombre se reservaba para los tiers de aprendizaje
de familias de arma que están a punto de entrar en dominio. Si el cubo y los
tiers hubieran coincidido leyendo `Mastery`, dos cosas distintas habrían
compartido nombre justo cuando la segunda empezara a existir. La cara pasa a
`Dominio` antes de que llegue la colisión.

Y bajo `60/60/60` con desempate `Body` primero, **todo ciudadano no fundador
era Fracture si su linaje era Body y Poisoning si era Bond**: las seis
expresiones físicas, dos alcanzables. Migrar después no servía: ese es el M-29
que `DEC-0018` dejó abierto.

### Connected

- **`CubeScoring.GenerateOrdinaryProfile(lineage, seed)`.** FNV-1a por eje
  sobre el vértice del linaje, delta en `[-8, +8]`, aplicado vía
  `ApplyContribution` — el mismo clamp y el mismo invariante de pareja del
  onboarding. La misma constante vive en
  `NaturalResourceLayoutPlanner.StableScore` y `CityWorld.StableAppearanceSeed`,
  así que el vocabulario de hashes del repositorio es uno solo. Un mismo
  `(linaje, seed)` produce siempre el mismo cubo; dos seeds distintos
  producen cubos distintos; cada linaje alcanza exactamente sus tres
  expresiones en un barrido y ninguna de las opuestas.
- **`CitizenProfile.TryCreate` con cubo explícito.** Nuevo overload
  opcional: `cubeProfile` por defecto sigue siendo el vértice, así
  `TestHelpers.NewProfile()` no se mueve y ningún test heredado cambia su
  cubo. Sólo `CreateMigrantProfile` lo pasa.
- **Descorrelación nombre / linaje.** El índice de nombre usaba
  `(seed - 2) % 8` y el de linaje `seed % 8`: misma longitud, mismo
  desfase, así que el id 2 siempre era Inara y siempre del mismo linaje.
  Ahora `CityWorld.MigrantNameForSeed(seed) = MigrantNames[(seed * 11 + 3) % 8]`,
  descorrelado en fase y reutilizable desde el test.
- **Rename `Mastery` → `Domain` en código, disco y UI.** El ctor,
  la propiedad y el `nameof` del mensaje de validación de
  `FounderCubeProfile` pasan al nombre canónico. `CubeScoring.MasteryValueId`
  pasa a `DomainValueId` con valor `"Domain"`, `WithMastery` a `WithDomain`,
  las ramas `"mastery"` del switch a `"domain"`. Las 16 referencias del
  `FounderNarrativeCatalog` siguen al renombrado. Los cuatro sitios de
  `WorldPersistence` que escribían `cube.Mastery` (captura y restauración
  en `MigrateV28ToV29` y `MigrateV29ToV30`) leen y escriben `Domain` desde
  la fuente canónica. `CitizenNatureText.cs` deja de imprimir el literal
  inglés `"Mastery"` y la ficha de fundación, el perfil del héroe y el
  panel de ciudadanos leen `cube.Domain`. El comentario obsoleto de
  `CubeExpression.cs` que documentaba el alias se quita: ya no hay alias.

### Schema

**v31 → v32, un paso.** El campo en disco del cubo se llama `Domain`. La
clave vieja `Mastery` se conserva una versión como puente nullable
(`FounderCubeProfileSave.Mastery`) para que un save v31 deserializado por
el código nuevo no pierda el cubo del fundador — el dato se pierde al
deserializar, antes de que la migración corra, así que el puente vive en
el DTO, no en el migrador. `MigrateV31ToV32` recorre los ciudadanos,
copia el puente a `Domain` y lo deja en `null`. Una partida que pasó por
`MigrateV29ToV30` antes del rename sigue funcionando: esa migración lee
`savedCube.Mastery ?? savedCube.Domain` (puente nullable, fallback al
campo canónico) y reconstruye la `FounderCubeProfile` que usaba el código
nuevo. **El cubo del fundador es idéntico antes y después.**

### Decisión

`DEC-0019` cierra M-29 y fija el rename. `DEC-0018` anota la resolución.

### Reshaped

- `DEC-0018` (Cube decides physical expression) gana la nota de que
  `M-29` quedó resuelto por `DEC-0019` el 2026-08-07.
- `13_KOVARI_CUBE.md` § *Estrategia y fallback* deja de afirmar
  "60/40 por eje" para todo no fundador: el nuevo fallback llama a
  `GenerateOrdinaryProfile(lineage, id)` y mantiene el invariante del
  sobre.
- `16_LINEAGES_KOVARI.md` § *Familias de armas y el vértice Kovari*
  se marca **superada 2026-08-07**: la sugerencia de armas por vértice
  quedó descartada cuando `DEC-0018` derivó el tier del ciudadano del
  cubo persistido y `DEC-0019` introdujo la variación `±8` que rompe el
  empate de tres bandas. Una tabla por vértice sería una lista
  redundante con el atlas de `bible/22_STATISTICS_PROGRESSION_AND_COMBAT_FORMULAS.md`.
- `07_ONBOARDING_AND_FOUNDER.md` snippet del `FounderOnboardingResult`
  cambia el parámetro `int Mastery` por `int Domain`.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido
del snapshot JSON de `VerticalLoopPersistenceTests`). Localización: el
`msgstr` bajo `msgid "Dominio"` pasa de `"Mastery"` a `"Domain"`; la
clave no cambia, ninguna traducción queda vacía, ningún marcador ni
placeholder se mueve. La carga con captura manual de un save v31 escrito
con la clave `"Mastery"` migra limpiamente al campo `Domain` con el cubo
idéntico y el puente `null`. Agent context: 448 / 448. Arranque
headless limpio.

**La fixture `migrant-cube` fotografiaba al fundador.** Se capturó y se
leyó la imagen: decía `1/0 ciudadanos alojados · 0 no héroes`. El mundo
de la fixture no tiene Shelter, así que `AvailableHousing == 0`,
`TryAcceptPendingProspect()` devolvía `AtCapacity` y un `else` silencioso
seleccionaba al fundador — cuyo cubo es el vértice desnudo, es decir
exactamente lo que este cambio existe para mover. La fixture leía como
prueba de lo contrario de lo que probaba. Ahora monta su mundo con el
idioma de `ShowShelterResourcesForVisualRegression` (Shelter para
alojamiento, Town Hall para hospedar, prospecto hospedado y aceptado por
las APIs reales), **no tiene fallback** y cada precondición que falle
emite `GD.PushError`. La captura verificada muestra `2/3 alojados · 1 no
héroes`, con `Tovan` (Kovari) seleccionado y expresión física
**Sangrado** — no Fractura, que es lo que daría el vértice.

Dos correcciones menores en el mismo paso: el baseline de localización
del `TO_DO` afirmaba `908 / 296` contra los `922 / 283` medidos —la
medición manda, §5.1— y `MigrantNameForSeed` indexaba con aritmética
`unchecked` con signo, de modo que un seed lo bastante grande daba un
índice negativo; ahora mezcla sin signo.

Lo que queda pendiente: un jugador con una partida guardada antes del
rename verá **exactamente la misma ciudad** al cargar — M-29 ya no se
introduce retroactivamente en una ciudad existente, sólo en los
migrantes que lleguen a partir de ahora. Esa consecuencia ya estaba
anunciada en la propia nota de M-29.

---

## La expresión física deja de ser la afinidad con otro nombre

**2026-08-07** · schema v31 (sin cambio) · EG-5

Un fundador de Fuego era siempre Aturdimiento y aprendía siempre Maza y Orbe.
No era una regla de diseño: era un atajo. La biblia publica **una tabla de tres
columnas** —cara del Cubo, afinidad elemental, expresión física— y la
implementación la leyó como una cadena, derivando la expresión de la afinidad.
Dos ejes independientes colapsados en uno, y treinta de las treinta y seis
combinaciones que el propio roadmap describe perdidas.

Ahora la expresión sale de la **cara más alta del `CubeProfile`** y la afinidad
va por su cuenta. Un Ardhen puede ser Fractura con Fuego, Parálisis con Aire o
Sangrado con Éter. La derivación es función pura del cubo persistido: el mismo
cubo responde siempre lo mismo y no se guarda ninguna copia que pueda
contradecirlo.

Cada linaje admite exactamente tres expresiones y cada expresión pertenece a
exactamente cuatro linajes. Eso no se impone con una lista de exclusión: bajo
`60/40` con el tope `±8` una cara favorecida vive en `52–68` y su opuesta en
`32–48`, así que la más alta es siempre una de las tres favorecidas. Los
empates se resuelven por el orden canónico explícito `Body, Bond, Stability,
Impulse, Domain, Reach`, nunca por orden de enum, diccionario o carga.

Sobre esa base, **aprender un arma tiene tres velocidades** en vez de dos
(`DEC-0018`): `100 %` para las dos familias de la propia expresión, `50 %` para
las cuatro de las otras dos expresiones que el vértice del linaje alcanza, y
`10 %` para las seis restantes. El nivel escala **sólo la adquisición de
experiencia**: quien llega a Espada 20 con una familia extranjera tiene Espada
20, y pelea igual que cualquiera. La dificultad estaba en llegar.

**Sin migración.** La expresión nunca se persistió y todo ciudadano ya guardaba
su cubo desde v30, así que cambiar la derivación bastó. Consecuencia real y
buscada: una partida existente **carga con expresiones distintas** a las de
ayer. Nada en disco cambia; el valor obsoleto no sobrevive porque nunca se
guardó.

De paso, dos incoherencias que el corte destapó: `EnemyCatalog` construía la
naturaleza desde la afinidad ignorando el `Expression` que la definición ya
traía, y un test llamaba "natural" a un arma que en realidad era familiar de
linaje —seguía verde porque `0.50 > 0.10 × 2`—.

---

## La biblia recupera sus capítulos huérfanos

**2026-08-07** · schema v31 (sin cambio)

**Un jugador no nota nada.** Esta entrada existe porque el contrato la pide, no
porque el juego haya cambiado: no se tocó dominio, escena ni guardado. Lo que
cambió es dónde vive el canon, que es lo que decide si la próxima sesión lo
encuentra.

Cuatro documentos estaban numerados como capítulos de la biblia pero vivían en
la raíz de `docs/`, y tres de esos números ya estaban ocupados dentro de la
biblia. La colisión más cara: `19_FIRST_NIGHT_AND_FIRE_SPIRIT` — aceptado como
canónico el día anterior bajo DEC-0014 — competía con `19_LINEAGES_ORVETH`.
Ahora las afinidades elementales ocupan el **11**, el hueco que la biblia había
dejado; las estadísticas y fórmulas de combate pasan a **22**; la primera noche
pasa a **23**, y es el único capítulo que el código cita por nombre, así que el
renombrado arrastró catorce archivos de `game/` y `tests/`. El cuarto,
`13_EXPEDITIONS_AND_COMBAT_INTEGRATION_ROADMAP`, **no** entró en la biblia:
ordena trabajo por dependencias, dice cuándo y no qué, y pierde el número. El
número de capítulo queda declarado como identidad estable: nunca se reutiliza
ni se reordena.

La causa de que los cuatro sobrevivieran años en la raíz era que `docs/README.md`
no los mencionaba, junto con otros seis documentos. El índice ahora está
completo y la regla de autoridad 7 lo declara: un documento que existe y no está
indexado es invisible. `scripts/docs/classify.ps1` la hace cumplir, y falla
también cuando un documento no está clasificado o cuando el registro nombra un
archivo que ya no existe. Ese registro, `scripts/docs/classification.json`, es
la fase 2 de la migración y está escrito a mano a propósito: decidir si un
bloque es canon, especificación, roadmap o backlog es un juicio que se revisa
línea por línea, no un patrón que se infiere. 265 documentos clasificados, y
cuatro quedan marcados `split` o `merge` como propuestas sin programar —
`ARCHITECTURE.md` y el capítulo 10 mezclan descripción con roadmap,
`UI_AUDIT.md` mezcla checklist con historial, y `CURRENT_DEVELOPMENT_STATE.md`
es el tercer documento que responde qué está construido.

De paso, los cinco enlaces rotos que el inventario de la fase 1 había
reportado resultaron ser fantasmas: el extractor leía `MIGRATIONS[v](data)`
dentro de un bloque de código como si fuera un enlace Markdown. El inventario
ahora ignora el código antes de buscar enlaces: 129 enlaces, **0 rotos**.

### Las cuatro propuestas, resueltas — y dos de ellas no eran ciertas

El detector de marcadores buscaba subcadenas. Contaba «deferred disposal» como
trabajo diferido, «future attachments» como plan, y encontraba `owed` dentro de
`allowed` y de `reflowed`. Sobre esos conteos se acusó a `docs/ARCHITECTURE.md`
de mezclar arquitectura con roadmap. Con coincidencia por palabra completa el
repositorio pasa de 124 documentos con backlog a 84, y los 14 que le quedan a
`ARCHITECTURE.md` son todos el adjetivo «future» dentro de prosa descriptiva.
**No se divide**: describe el código que existe, de principio a fin. Su defecto
real es otro y ahora tiene ficha propia (`M-28`): el §8 narra migraciones hasta
la v28 mientras el código va por la v31.

El capítulo 10 sí lo era, y su propio título lo admitía. Se quedó el canon
—stack, separación de capas, reglas de simulación, pixel perfect, cámara
caminable, guardarraíles— y se archivó el plan. El mapa de escenas que
proponía describía `scenes/city/`, `scenes/buildings/MineDetailView.tscn`,
`scenes/gardens/`: hoy el proyecto tiene diecisiete `.tscn` y ninguno se llama
así. Presentarlo como canon lo convertía en una instrucción equivocada. La
secuencia de quince pasos se archivó en vez de reubicarse en un roadmap vivo,
porque el orden vigente es EG-0→EG-6 y dos secuencias canónicas compitiendo son
peores que un plan viejo. Tres «preguntas abiertas» que en realidad eran trabajo
con dueño bajaron a `TO_DO.md` como `M-27`. El archivo perdió `AND_ROADMAP` del
nombre; el número de capítulo no cambió.

`UI_AUDIT.md` mezclaba un checklist reutilizable con el registro de lo firmado.
El checklist y la regla de cierre se fueron a `VISUAL_REGRESSION.md`, que ya
era dueño del contrato de firma humana; el audit se queda con estado, evidencia,
deuda de presentación e historial. Sus veintiséis marcadores sí eran señal real
—casillas sin marcar— y leían como un backlog sin dueño. De paso desaparece el
encuadre en VS-5, descartado el 2026-07-31.

`CURRENT_DEVELOPMENT_STATE.md` no se fusionó entero: sólo se le quitó lo que
duplicaba. Su tabla de cabecera afirmaba 730 pruebas y esquema v28 contra 914 y
v31 medidos. Un número copiado a mano es un número que va a estar mal, así que
se borró en vez de corregirse; ahora el archivo no contiene ninguno y remite a
`STATE.txt`. Lo que queda —el inventario de no-regresión— no lo tiene ningún
otro documento.

Y una cosa que el corte destapó y era peor que todo lo anterior: cuatro skills
canónicos mandaban a leer `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`, borrado hace una
semana, y a usar su tabla de huecos `G0`–`G7` como criterios de aceptación. Un
agente que cargara `vertical-slice-validation` recibía cuatro punteros a la
nada. Ahora apuntan al proposal §3, §15 y §17, que es donde viven los criterios.

Baseline medido: build 0 errores / 0 avisos, **914 pruebas pasan** (1 omitida),
arranque headless limpio, 448 verificaciones de contexto de agentes, 922
identificadores de plantilla y 283 claves en runtime.

## Profundidad real en el macro y suelo por bioma

**2026-08-06** · schema v31 (sin cambio)

Un ciudadano se dibujaba **siempre** por delante de todo el mundo, incluso de un
árbol más cercano a cámara. No era un fallo de ordenación sino dos ejes
distintos: la vista pintaba terreno, edificios y árboles con comandos inmediatos
en su propio `_Draw()`, mientras los ciudadanos son nodos hijos — y en Godot un
`CanvasItem` emite lo suyo antes de dibujar a sus hijos. Nunca se comparaban.

Ahora los obstáculos viven en una capa por calle cuyo `ZIndex` sale de la
**misma función** que usan los ciudadanos. El suelo se queda en el padre, que es
lo correcto: el terreno siempre va detrás. La vista entera baja a
`OverlayLayers.WorldDepthBase` para que ese rango de índices no adelante al HUD.

Y el suelo deja de rotar `street % 3` entre hierba, tierra y piedra — el motivo
de que leyera como bandas arbitrarias. Cada ciudad dibuja el **bioma del sitio
donde cayó su fundador** (`DEC-0017`), ocho paletas, una por linaje, y solo
paleta: ninguna receta, tasa ni recurso cambia, así que el linaje sigue sin
conferir ventaja.

Tres cosas que solo se vieron capturando: al dar lienzo a los helpers cambié las
firmas y no los cuerpos, y **todos los árboles desaparecieron** aunque compilaba;
los nuevos z de banda adelantaron a la Crónica; y el primer hash de variante
producía **rayas horizontales**, porque `tileIndex * 3` se anula con tres
variantes y la elección colapsaba sobre la fila.

Lo que no entra: la dispersión de flores. Los tiles con flores del pack son
piezas de autotile y cada una arrastra una esquina del material vecino, así que
al repetirlas se ve el corte. Hace falta arte de props con fondo transparente.

Sobre eso, la lección que costó tres intentos: la hoja tiene **dos cosas que
parecen relleno**. El bloque de muestras sólidas (cols 5-9, filas 0-1) sí es
plano. Los bloques de *parche* de color son autotiles de 5×3 cuyas columnas 0-1
son las cuatro esquinas interiores —cada una con la muesca del material de
debajo— y cuyas columnas 2-4 son un blob redondeado de 3×3. **Solo el centro
del blob es apto para repetir.** Al tomar un id nuevo hay que renderizar su
bloque entero, no el tile suelto: la muesca es invisible a un solo tile y
evidente en cuanto se repite en una banda.

**El grano del movimiento se afina sin dejar de ser escalonado.** La locomoción
pasa de 8 px a 12 Hz a **4 px a 24 Hz**: misma velocidad efectiva (96 px/s, y se
deriva de las dos constantes, así que no puede desincronizarse) con la mitad de
salto visual. Hubo que duplicar los contadores de paso del paneo en profundidad
y del zoom de entrada a edificio, que avanzan una fracción fija por tick y si no
se habrían acelerado al doble — "suavizar el desplazamiento" habría vuelto la
cámara más brusca. Y el peldaño de las escaleras trapezoidales del terreno baja
de 4 px a **2 px**. En ningún caso se introduce interpolación: el movimiento
sigue siendo discreto y los bordes siguen encajados en rejilla de píxel entero.

Los ocho biomas se pueden revisar sin rejugar el onboarding, con el fixture
`biome-<linaje>`. Hacía falta porque el linaje del fundador lo *infiere* el
scorer, no se elige. Y de paso los fixtures dejan de construir su fundador con
`CitizenProfile.TryCreate` y una lista inventada de aptitudes, profesiones y
rasgos: un fundador no empieza con nada de eso —`CreateFounder` pasa arrays
vacíos— así que una ciudad de prueba ya no nace pre-cualificada.

Baseline medido: build 0 errores / 0 advertencias; tests 914 pasando, 1 omitido;
boot headless correcto; 922 IDs de plantilla y 283 claves de runtime; agent
context 448/448.

---

## El tiempo deja de poder detenerse

**2026-08-06** · schema v31 (sin cambio)

La ciudad avanza con el juego cerrado. Un botón que congelaba el reloj discutía
con esa premisa, y abrir el menú ESC era además una forma de comprar tiempo
gratis. Fuera los dos: el control de play/pausa desaparece de la barra de estado
y el menú de la ciudad ya no congela la simulación al abrirse — el mundo sigue
corriendo detrás del scrim.

Queda el **multiplicador de velocidad**, que es el control que sí tiene sentido:
quien quiera que la ciudad se asiente la baja de ritmo en vez de detenerla. La
biblia reserva sitio en la barra para "tiempo, velocidad, alertas y acciones
globales", así que la velocidad sigue cumpliendo ese contrato.

Consecuencia que hay que saber: **desaparece el autoguardado al pausar.** Se
disparaba en la transición corriendo→pausado, y sin pausa esa rama no se alcanza.
El autoguardado periódico de tres minutos y el de cierre siguen intactos;
`ARCHITECTURE.md` y `CURRENT_STATUS.md` lo prometían "on close/pause" y ya no.

El ejemplo canónico de `-NormalizedClicks` en la matriz visual apuntaba
justamente al botón borrado. Se sustituyó, y se dejó anotada la lección: unas
coordenadas solo son tan duraderas como el control al que apuntan — al quitar el
botón, ese clic habría caído en panel vacío y la captura habría "pasado" igual.

---

## Identidad visual: fuera el amarillo, sprites en el mundo y el espíritu con voz propia

**2026-08-06** · schema v31 (sin cambio)

Casi todos los botones del juego eran amarillos, y nadie lo había decidido. Era
la suma de dos valores por defecto: `ButtonText` —la variación que usa el 80 %
de los botones— apuntaba a `kenney/9-slice/yellow.tres`, y el fallback de
`LineageThemeRegistry` apuntaba al mismo archivo mientras el linaje activo
arrancaba como `"default"`, **un id que no era clave del diccionario de
linajes**. Todo panel construido antes de que exista un héroe caía por ahí; y
como la mayoría de consumidores aplican el stylebox una sola vez en `_Ready`,
se quedaban amarillos el resto de la sesión.

Ahora la superficie neutra es **pizarra oscura**, promocionada del pack CC0
*UI Pack – Pixel Adventure* de Kenney: cuatro tiles de 32×32, con hover y
disabled resueltos por `modulate` sobre los mismos PNG para no promocionar
assets que no aportan forma nueva. El dorado queda solo para estado —foco,
borde de panel elevado, fragmentos estabilizados—; el verde conserva su
semántico de éxito y el rojo el destructivo. El texto de botón pasa a crema
sobre la superficie oscura. **Ninguna métrica de layout se movió**: los
`content_margin` siguen siendo 16/4, y las capturas del onboarding antes y
después lo confirman. La biblia permite que un reskin cambie paleta, bordes y
rellenos, pero no tamaños mínimos ni jerarquía.

Las acciones se eligen ahora **por rol** y no por apariencia:
`PrimaryActionButton`, `SecondaryActionButton` y `DangerActionButton`. Ese es
el motivo de que este cambio de piel haya sido una edición del tema y no una
auditoría de treinta puntos de construcción.

De paso cae un defecto de capas que este trabajo destapó: `FirstNightScene`
alojaba la noche en un `CanvasLayer = 50`, pero `OverlayLayers` es un catálogo
de `ZIndex` y un `CanvasLayer` gana a cualquier `ZIndex`. El espíritu y su
banda se dibujaban por encima del onboarding (80), del menú de pausa (100) y
del Notifier. Ahora son raíces de canvas normales en
`OverlayLayers.Tutorial`, y la captura de pausa lo demuestra: el espíritu
queda bajo el scrim.

**El mundo deja de ser rectángulos de color.** Ramas, fibra vegetal, piedra
pequeña y comida silvestre eran cuatro `DrawRect` planos con colores a pelo, que
a distancia de macro leían todos como el mismo cuadrado. Ahora son sprites del
atlas roguelike: matorral seco, brote verde, escombro gris y arbusto con bayas.
Las coordenadas del atlas vivían triplicadas —con la fila y la columna
re-escritas como literales justo debajo de las constantes que ya las
contenían— y ahora están en un único `TerrainAtlas`. La verificación importó:
el primer tile que elegí para la piedra resultó ser una pila de lingotes de
plata que renderizaba azul, exactamente la clase de error que el repo ya tenía
registrada con los tiles de agua en vez de árboles.

**Los cursores son de verdad contextuales.** El puntero era un SVG de 24 px que
leía blando contra el pixel art, y el cursor "interactivo" era esa misma flecha
reteñida: al pasar por un botón salía una flecha más clara, no una mano. Ahora
son dos glifos distintos del pack de cursores, teñidos por linaje como manda la
biblia (recolorear por tokens, sin deformar geometría).

**El espíritu de fuego habla desde el mundo.** Era un anillo de dieciséis
puntos con un triángulo, clavado a la pantalla porque su posición solo se
recalculaba al cambiar de etapa. Ahora es una llama que parpadea en la cadencia
de 12 Hz del proyecto y se re-proyecta cada frame, y su voz es una **burbuja
anclada sobre él** en lugar de una banda al pie: la banda nunca se había visto
en ejecución hasta ayer, y al verse resultó ser una barra de subtítulos con el
personaje que enseña en otra parte de la pantalla. `DEC-0016` supersede a
`DEC-0014` §3 y explica por qué. La burbuja entera es el confirmar; no hay
botón compitiendo. Y las superficies de la noche se ocultan fuera del macro,
porque estaban dibujándose sobre el panel de una vista de edificio.

**Las vistas de detalle tienen suelo.** Eran una textura de panel repetida y
teñida por el acento del linaje; ahora son tiles de terreno reales dibujados
ortogonalmente a escala entera ×4 (16 → 64, el `BaseUnit` del proyecto), sin el
`Modulate` que enturbiaba el color.

**Y el espíritu por fin enseña.** Se callaba tras dos frases: las dos etapas en
las que la noche espera a que construyas devuelven `null` del catálogo a
propósito, y nada llenaba ese hueco, así que el "tutorial orgánico" no llegaba
a enseñar nada. Ahora la burbuja muestra una directiva —"Necesitas 3 ramas y 2
piedras pequeñas para levantar una hoguera"— con las cantidades **interpoladas
de `FoundingSiteRules.InputsFor`**, nunca escritas a mano, como exige
`DEC-0014` §4: si cambia la receta, el tutorial no puede mentir.

Tres correcciones más de la misma sesión de prueba: la burbuja se anclaba al
borde superior cuando no cabía arriba y caía **encima** del hablante, tapando
al espíritu — ahora vuelca debajo y la cola gira para seguir apuntándole; la
burbuja seguía visible al abrir Construcción, porque ningún modal cambia
`Selection` y vigilar solo la selección no bastaba (ahora también escucha al
`ModalHost`); y el cursor mostraba un hacha para cualquier recurso, cuando el
pack tiene herramientas distintas — ahora hacha para madera, pico para piedra y
mano para lo que se recoge del suelo.

**Y el mundo recupera su escala.** El sprite del habitante en el macro estaba
al 25 %, una decisión tomada cuando el terreno era provisional y bastaba con
que una persona "se notara"; contra el terreno acabado leía como un enano.
Ahora está al 50 %, una fracción limpia para que el muestreo nearest siga
cayendo en píxeles enteros. Y los árboles medían lo mismo que un arbusto de
bayas — no por venir de packs distintos, que ya vienen del mismo atlas, sino
porque todos los recursos usaban el mismo rect cuadrado. El árbol pasa a su
forma de dos tiles: la copa crece hacia arriba fuera del rect y el tronco
conserva la línea de suelo, así que la huella de la parcela y el área de clic
no cambian.

Dos arreglos más del segundo playtest: la burbuja de apertura dibujaba una cola
apuntando al suelo vacío, porque en `Manifested` el espíritu todavía no está
presente — la narración ahora va sin cola, y solo las etapas con espíritu tienen
hablante. Y las brasas dejaron de ser un cuadrilátero de alambre que un
playtester confundió con un sigilo de linaje: ahora son el sprite de hoguera del
atlas, teñido hacia ceniza.

**El espíritu deja de narrarse a sí mismo.** Los seis nodos del catálogo
declaraban `SpeakerId = "fire_spirit"`, incluidos los cinco escritos como
narración en tercera persona *sobre* él; el globo se los atribuía, así que el
espíritu decía cosas como "El espíritu se detiene, sorprendido". Ahora existe un
hablante `narrator` y solo `shelter_built` se pronuncia en voz alta. La
narración se pinta centrada, sin cola y un tono más apagada; el habla conserva
el nivel `DialogText` y la cola sobre el espíritu. El test que existía afirmaba
la atribución errónea y se corrigió; otro nuevo vigila que la noche conserve
**las dos** voces, para que la separación no se colapse en silencio.

Lo que sigue pendiente: el espíritu es una llama autoral, no un sprite — no hay
ninguna llama exenta en los tres packs y recortarla de un brasero sería editar
a mano un PNG exportado, que el pipeline prohíbe. Y cinco de los seis diálogos
autorales están escritos como **narración en tercera persona sobre** el espíritu
("El espíritu se detiene, sorprendido"), no como habla suya: la banda inferior
original los presentaba bien, pero un globo de diálogo los atribuye a alguien
que no los dice. Clasificar la voz nodo a nodo son 48 claves y es decisión de
`narrative-lore`, no de presentación.

Baseline medido: build 0 errores / 0 advertencias; tests 913 pasando, 1
omitido; 918 IDs de plantilla y 283 claves de runtime; specimen tipográfico con
`TitleCropColorCount = 2` (sin franja gris); capturas de `astral-start`,
`firstnight-manifested`, `pause-menu`, `migrant`, `policies`, `offline-report`
y `macro-current` a 1280×720 y 1920×1080.

---

## README alineado con EG-5

**2026-08-06** · schema v31 (sin cambio)

El `README.md` llevaba tiempo describiendo una realidad que el repo
ya no es: todavía hablaba de "complete hero-onboarding profile for
one citizen and zero buildings, followed by an explicitly authorised
Basic Shelter construction project" y citaba 232 / 345 tests como
si fueran la línea base actual. La primera obra ya no es un Basic
Shelter libre — el fundador entra con la noche autoral (espíritu de
fuego, módulo a módulo, `00:00` → `06:00`) y el primer edificio
crece dentro del Founding Site como Campfire → Bedroll/Cache →
Canopy. El Cubo Kovari, el slice vertical de combate, las
expediciones de recursos sobre la rejilla dinámica, el catálogo
`firstnight.*` y la SpiritTrailSearch ya están conectados; un
visitante que se quede en el `README` se queda con la versión vieja.

Este increment no toca gameplay, schema ni catálogo. Solo reescribe
las cuatro secciones que el repo había superado:

- El **status header** ahora declara EG-5, la apertura EG-A0
  (onboarding Kovari, primera noche, Founding Site, Cultivation
  Site, SpiritTrail/FallenWood, combate determinista, reclutamiento
  por Town Hall) y la baseline medida hoy (schema v31, **913
  passing**, 1 omitido).
- La sección **13 (First prototype scope)** ya no enumera el
  Basic Shelter libre como primer proyecto, sino la cadena de la
  noche autoral y el ciclo del Founding Site, junto con el Cubo
  Kovari, las expediciones de recursos y el slice de combate.
- La sección **14 (Short initial roadmap)** incorpora la secuencia
  `EG-0 → EG-6` definida por el proposal §15 y separa lo cerrado
  de lo activo (EG-5) y lo pendiente (EG-6).
- La sección **15 (Founding hero and first night)** deja de
  presentar al Basic Shelter como primera decisión de obra y
  apunta a `docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md` y al
  acceptance test EG-A0 del proposal §17.

Lo demás del archivo (game vision, pilares, stack, plataforma,
flujo de arte, convenciones, licencia, contribución) sigue
vigente.

Baseline medido en este increment: `dotnet build` 0 errores /
0 advertencias (reconfirmado por el snapshot Full del commit);
tests 913 / 914 (1 omitido); agent context 448 / 448; 918 IDs de
plantilla y 283 claves de runtime.

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

Dos filas se reservan en vez de ocultarse: la consecuencia inmediata durante las
preguntas y el aviso de validación durante el nombrado. Son las dos que aparecen
y desaparecen *como respuesta a que el jugador actúe en esa misma pantalla*, así
que ocultarlas era correcto para el espacio y erróneo para la sensación —
elegir una opción reajustaba la columna y movía el texto que se estaba leyendo.
Comprobado con dos capturas idénticas salvo el clic: solo difieren el contador,
el fragmento encendido, el foco, el botón elegido, la línea de consecuencia y el
pie; título y narrativa son idénticos píxel a píxel a 1280×720 y a 1920×1080.

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
las gobierna está en `docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`, y mantiene
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
- **Documentación canónica**: `docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`
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
