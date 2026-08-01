# TO DO — Cola operativa

> Backlog vivo del proyecto. Git conserva el historial; este archivo conserva
> únicamente el estado vigente, las tareas accionables y los triggers de trabajo
> diferido. Antes de tomar un ítem se debe releer el código y los documentos que
> figuran en `Afecta`.

## 0. Reglas de mantenimiento

### Estados

| Estado | Uso |
| --- | --- |
| Pendiente | Puede tomarse cuando el slice activo lo permita. |
| En curso | Se está trabajando o falta una firma concreta para cerrarlo. |
| Bloqueado | Requiere una decisión o dependencia externa. |
| Necesita reanálisis | El contexto cambió y la solución anterior puede ser obsoleta. |
| Diferido | Tiene un trigger explícito que todavía no se cumple. |
| Hecho | Cerrado recientemente; se elimina después de dos días calendario. |
| Superado | Ya no aplica; se conserva dos días con el motivo. |

### Prioridad del increment

> **2026-07-31 — cambio de norte.** Se descarta `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`
> (VS-5 y sus 17 criterios). El proyecto aún no tiene las dos capas de
> complejidad (Founding Site + plot lifecycle + resource seam) que pide
> `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` antes de hablar de herida,
> tratamiento y territorios desbloqueables. Wound/recovery/territory loops
> quedan diferidos hasta EG-2 + EG-3 + EG-5 estables.

Hasta que la acceptance test de `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`
§17 no se cumpla en un slot limpio nuevo, no se abre profundidad nueva. Solo se
admiten:

1. Correcciones que un playtest de la apertura EG-A0 exponga como bloqueantes.
2. Avance de los increments EG-1 a EG-6 en el orden de §15.
3. Limpieza documental o técnica que no cambie el producto.

### Baseline vigente

- Fecha de alineación: **2026-07-31**.
- Slice activo: **EG-4 — resource expedition seam**. EG-3 ya cerrado; schema actual v24,
  `Branches/PlantFiber/SmallStone/WildFood` en `ResourceType`,
  `SeedStartingOpportunities` siembra EG-A0 en parcels libres, `GatherFromPatch`
  genérico y cap carried de 6 unidades; tests en `Eg1ResourceSeamTests`.
  Founding Site 3×3 con módulos Campfire/Bedroll/Cache/Canopy, capacidad
  6→12→24, mismo ID/parcela y finalización offline. El primer Cultivation Site
  requiere Shelter, 1 Branch + 1 Small Stone, 180 work y produce 5 Food tres
  días después de sembrar 1 Food, con la misma frontera live/offline.
- Save: **schema v24** (lifecycle persistente del primer Cultivation Site).
- Build: **0 errores / 0 advertencias**.
- Tests: **690 / 691** (1 omitido por brittleness del snapshot JSON en
  `VerticalLoopPersistenceTests.Recovery_ReloadedHalfway`; el comportamiento
  no cambió, sólo los IDs auto-incrementados de eventos).
- Arranque Godot headless: correcto.
- Fuente de verdad: `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` y
  `docs/CURRENT_STATUS.md`.

## 1. Resumen

| Estado | Crítica | Alta | Media | Baja |
| --- | ---: | ---: | ---: | ---: |
| En curso | 1 | 0 | 1 | 0 |
| Pendiente | 0 | 0 | 2 | 0 |
| Necesita reanálisis | 0 | 0 | 0 | 0 |
| Diferido por trigger | 0 | 2 | 1 | 0 |
| Bloqueado | 0 | 0 | 0 | 0 |

## 2. Increment activo — Apertura EG-A0 (proposal §15)

Flujo objetivo de la apertura temprana:

```text
Onboarding → Branches/Plant Fiber/Small Stone/Wild Food en el suelo
→ Founding Site (Campfire → Bedroll/Cache → Canopy)
→ Cultivation Site: preparar → sembrar → crecer 3 días → cosechar
→ primeras expeditions de Food y Wood (2–3 min reales cada una)
→ consolidación: 3 plots + forestry gate + Farm
→ segundo ciclo sin reset
```

### Estado por increment

| Increment | Estado | Evidencia vigente |
| --- | --- | --- |
| EG-0 — medición del early game | Hecho | Schema v20; `eg0-report.txt` se actualiza por save; suspende observer durante Restore para no contar el inventario recargado. |
| EG-1 — resource/storage seam | Hecho | Schema v21; `Branches/PlantFiber/SmallStone/WildFood` en `ResourceType`; `SeedStartingOpportunities` siembra EG-A0; `GatherFromPatch` genérico; cap carried de 6 unidades; tests en `Eg1ResourceSeamTests`. |
| EG-2 — founding site seam | Hecho | Schema v22; Founding Site estable con Campfire → Bedroll/Cache → Canopy, capacidad 6→12→24, origen persistente y equivalencia offline. |
| EG-3 — Food horizon seam | Hecho | Schema v24; primer Cultivation Site completo, Food horizon visible y equivalencia live/offline cubierta por `Eg3CultivationSiteTests`. |
| EG-4 — resource expedition seam | Pendiente | Próximo. Oportunidades finitas de Food/Wood mediante la cadena de expedición existente. |
| EG-5 — consolidación | Bloqueado | Espera EG-4. |
| EG-6 — calibration/signature | Bloqueado | Espera EG-5. |

### 🟡 M-14 — Matriz de regresión visual

- **Estado:** En curso como contrato transversal.
- **Prioridad:** Media.
- **Afecta:** `tools/Capture-VisualMatrix.ps1`,
  `docs/VISUAL_REGRESSION.md`, escenas/UI tocadas por cada cambio.
- **Hecho:** harness read-only con medidas reales en 1280×720 y 1920×1080,
  manifiesto, frame-time del engine y fixtures de las superficies principales.
- **Pendiente para EG-1:** contención visual de los nodos Branches/Plant
  Fiber/Small Stone/Wild Food en macro view; firma humana de su comportamiento
  de recogida.
- **Aceptación:** ningún cambio de UI se cierra solo con boot headless.
| 1 | Fundador persistente | Firma humana obtenida |
| 2 | Gathering y tres edificios iniciales | Firma humana obtenida |
| 3 | Reclutamiento restringido | Firma humana obtenida |
| 4 | Asignar/remover citizens | Firma humana obtenida |
| 5 | Producción causal Farm/Quarry | Firma humana obtenida |
| 6 | Presión/consumo significativo | Falló; G1 reabierto para EG-0+ |
| 7 | Compromiso exclusivo | Automatizado |
| 8 | Plan expedicionario real | Automatizado |
| 9 | Fases y regreso | Automatizado |
| 10 | Encuentro determinista y explicable | Automatizado |
| 11 | Regreso cambia al menos dos ejes | Automatizado |
| 12 | Herida persistente | Automatizado |
| 13 | Tratamiento causal | Automatizado |
| 14 | Territorio desbloquea oportunidad | Automatizado |
| 15 | Decisión post-regreso | Implementado; firma humana pendiente |
| 16 | Save/offline exacto por límites | Automatizado; relanzamientos humanos pendientes |
| 17 | Repetición sin reset/debug | Automatizado; firma humana pendiente |

## 3. En curso

### 🟡 M-14 — Matriz de regresión visual

- **Estado:** En curso como contrato transversal.
- **Prioridad:** Media.
- **Afecta:** `tools/Capture-VisualMatrix.ps1`,
  `docs/VISUAL_REGRESSION.md`, escenas/UI tocadas por cada cambio.
- **Hecho:** harness read-only con medidas reales en 1280×720 y 1920×1080,
  manifiesto, frame-time del engine y fixtures de las superficies principales.
- **Pendiente para EG-1:** contención visual de los nodos Branches/Plant
  Fiber/Small Stone/Wild Food en macro view; firma humana de su comportamiento
  de recogida y de los mensajes de capacidad del Cache.
- **Aceptación:** ningún cambio de UI se cierra solo con boot headless.

## 4. Pendientes después de EG-2

### 🟡 M-25 — Feedback causal de importancia grande

- **Estado:** primer corte implementado.
- **Afecta:** `UiMotion`, `ModalHost`, Chronicle, retornos, construcción y
  llegada de citizens.
- **Pendiente:** firma humana y feedback grande coherente para obra completada,
  regreso expedicionario y llegada/aceptación de citizen.
- **Aceptación:** vuelve a reposo, no bloquea input, respeta reduced-motion
  futuro y no modifica dominio ni ticks.

### 🟡 M-12 — Exclusión de overlays transitorios

- **Estado:** Pendiente.
- **Afecta:** `Notifier.cs`, `TutorialOverlay.cs`, `OfflineReportPanel.cs`.
- **Problema:** toast, error, tutorial y Chronicle poseen posiciones de forma
  independiente; pueden coincidir en pantalla.
- **Dirección:** un host con slots/prioridad solo si un playtest de la apertura
  EG-A0 reproduce el solape, o si el siguiente increment requiere overlays
  simultáneos.
- **Aceptación:** save toast + error + tutorial no se solapan ni capturan input
  incorrectamente.

## 5. Necesita reanálisis

_(Vacío: los reanálisis pendientes se resolvieron o se cerraron como
superados en 2026-07-30; ver §8.)_

## 6. Diferido por trigger

### 🟠 H-30 — Representación masiva de citizens/NPCs

- **Trigger:** más de 20–25 citizens visibles o evidencia del profiler.
- **Estado actual:** `CitizenSpriteBank`/carriers son correctos para la escala
  presente.
- **Antes de implementar:** medir fixtures de 25 y 50 entidades. MultiMesh o
  batching se justifican solo si existe cuello de botella.

### 🟠 H-31 — Primer diálogo ramificado con NPC real

- **Trigger:** primer NPC conversable con una decisión persistente.
- **Estado actual:** `DialogueRunner` tiene seam y tests, pero ningún consumidor
  jugable.
- **Aceptación futura:** EN/ES, mouse/teclado/gamepad, elección persistida,
  reentrada determinista y evento causal; evaluar addon solo con necesidad real.

### 🟡 M-22 — Integración selectiva de assets

- **Trigger:** una necesidad de legibilidad concreta del slice activo.
- **Estado actual:** inventario/licencias documentados; iconos, atlas y cursor
  necesarios ya promovidos.
- **Regla:** no importar paquetes completos ni habilitar Settings/minimap sin
  un slice aprobado.

### Seguimiento S-1

| Subítem | Estado/trigger |
| --- | --- |
| S-1.1 i18n | Hecho para UI actual; mantener EN/ES y validador. |
| S-1.2 NavigationServer2D | Primer corte hecho; reconciliar dentro de H-26. |
| S-1.3 terreno perspectiva | Primer corte hecho; no forzar TileMap sobre trapecios proyectados. |
| S-1.4 MultiMesh | Diferido al trigger de H-30. |
| S-1.5 FSM | Seam mínimo hecho; ampliar solo con comportamiento autónomo real. |
| S-1.6 diálogos | Seam mínimo hecho; ampliar con H-31. |
| S-1.7 profiler | Hecho; mantener medición real del engine en matrices. |

## 7. Hechas recientes

### 2026-07-31

- **EG-1 — resource/storage seam (schema v21).** Cuatro nuevos
  `ResourceType`: `Branches`, `PlantFiber`, `SmallStone`, `WildFood`.
  Cambios en `CityWorld`:
  1. `GatherFromPatch(int patchId, int? unitId, int amount)` —
     drain genérico de cualquier `NaturalResourcePatch`, no sólo los
     Forests. El Forest legacy mantiene `WoodUnitReserves` mirrorado
     para no romper el recipe gate.
  2. `SeedStartingOpportunities()` — siembra EG-A0 en parcels libres:
     14 Branches (7×2), 6 Plant Fiber (3×2), 6 Small Stone (3×2),
     8 Wild Food (4×2). Idempotente y silenciosa cuando no hay
     parcelas libres, así que un save con Home/Farm/Town Hall en
     parcelas 3–6 no se altera retroactivamente.
  3. `CarriedGroundResourceCapacity = 6` — los cuatro recursos nuevos
     comparten un cap de carga de 6 unidades (proposal §4). Wood,
     Stone, Food y compañía ignoran el cap porque van a per-building
     storage. EG-2 reemplaza este cap con la capacidad física vigente:
     carried = 6, Cache = 12, Shelter = 24.
  4. `PatchChanged` event + `WorldEventSubjectKind.Patch` para que el
     presentation layer pueda refrescar overlays de suelo.
  Persistencia: `WorldSave.CurrentVersion = 21` + `MigrateV20ToV21` (sólo
  bump de versión; los nuevos tipos aparecen vía `SeedStartingOpportunities`
  en el Restore). `SeedStartingOpportunities` se llama tanto en
  `TryCompleteOnboarding` como en el path de Restore para que partidas
  nuevas y legacy con parcelas libres tengan los mismos recursos EG-A0.
  8 tests nuevos en `Eg1ResourceSeamTests` cubriendo distribución,
  idempotencia, gather con cap y migración. Build 0/0; tests 663/664.
- **EG-2 — founding site seam (schema v22).** `ConstructionProject` conserva
  un ID/parcela mientras ejecuta Campfire → Bedroll/Cache → Canopy; Bedroll y
  Cache aceptan ambos órdenes y Canopy exige los tres módulos previos. Cada
  módulo usa 180 de trabajo (un cuarto del presupuesto shelter de 720) y paga
  su coste completo al autorizarlo para que el fundador único no quede ocupado
  mientras todavía necesita recoger inputs. Cache eleva el cap conjunto a 12;
  el Home consolidado lo eleva a 24 y persiste los módulos de origen. La
  interfaz ofrece la decisión de módulo sin autoelegirla y una acción explícita
  que devuelve toda la carga al terreno evita los soft-locks de inventarios 6/6
  o 12/12 mal recogidos entre módulos. `MigrateV21ToV22` preserva Homes y Basic
  Shelter legacy sin inventar historia. 11 casos en `Eg2FoundingSiteTests` cubren ambos órdenes,
  recuperación de carga, identidad, capacidades, snapshot, round-trip y
  equivalencia live/offline. Build 0/0; tests 680/681 (1 omitido conocido).
- **EG-3 — Food horizon seam (schema v24).** Un Cultivation Site posterior al
  Shelter consume 1 Branch + 1 Small Stone y 180 de trabajo; sembrar consume
  1 Food, persiste `readyAtTick`, madura exactamente tras 10.800 ticks live u
  offline y cosechar deposita 5 Food. El HUD muestra ración, horizonte y target
  protegido; la macro view distingue Prepared/Sown/Growing/Ready/Spent sin
  depender sólo del color. `MigrateV23ToV24` agrega una lista vacía sin
  inventar parcelas. Diez casos en `Eg3CultivationSiteTests`, interacción real
  y matriz 1280×720/1920×1080 verificadas. Build 0/0; tests 690/691 (1 omitido conocido).
- **Visual de TerritoryState en macro view.** El único parcel bloqueado
  (Parcel 9, `LogicalColumn = 4`) quedaba fuera del área renderizada
  (`WorldParcelColumns = 4`) y, aunque las Available no tenían tint
  distintivo, no había forma de saber que existía territorio
  descubrible. Tres cambios en `MacroStreetLiveView.cs`:
  1. `WorldParcelColumns = 5` para que Parcel 9 entre al viewport.
  2. Nuevo método `DrawParcelTerritoryTints` que pinta una banda
     trapezoidal por columna de parcel, con color según
     `CityParcel.TerritoryState`:
     - `Locked` → opaco oscuro (0.08/0.07/0.05/0.78)
     - `Reconnoitred` → mostaza translúcida (0.86/0.72/0.28/0.32)
     - `RouteSecured` → verde oliva translúcida (0.47/0.62/0.34/0.22)
     - `Available` → sin overlay (terrain tal cual)
     El tint se proyecta sobre la misma grilla perspectiva que el piso,
     así que respeta el offset de cámara lateral y el vanishing point.
  3. Constantes nuevas en la sección de colores:
     `LockedParcelColor`, `ReconnoitredParcelColor`,
     `RouteSecuredParcelColor`.
  Con esto una expedición `Reconnaissance` FullSuccess avanza Parcel 9
  por tres estados (Locked → Reconnoitred → RouteSecured → Available)
  y el jugador ve el cambio de color en tiempo real. Build + tests
  siguen verdes (655/656).
- **ESC global iterativo + audit de overlay.** Tres cambios en la cadena
  de input para que un solo ESC cierre exactamente un overlay y no se
  quede atrapado al abrir varios a la vez:
  1. `CityPrototype._UnhandledInput` llama a
     `CityWorldController.ReturnToCity()` cuando recibe `ui_cancel` —
     HeroProfileView y BuildingDetailView cierran vía
     `OnSelectionChanged`. Se ejecuta **después** de ModalHost y
     PauseMenu (leaf-first en Godot), así que respeta la prioridad
     existente.
  2. `PauseMenu._UnhandledInput` ya no abre el menú con ESC cuando está
     oculto; sólo lo cierra cuando está visible. El menú conserva su
     botón dedicado. Evita que el primer ESC "se lo coma" el menú de
     pausa y nunca cierre la vista de héroe o el detalle de edificio.
  3. `ModalHost.CompleteClose` ahora descarta el `_content` si ya está
     disposed antes de tocarlo (`!GodotObject.IsInstanceValid(_content)`).
     Antes, una ruta que liberaba el content a mitad de la animación de
     cierre lanzaba `ObjectDisposedException` y dejaba el modal
     visible.
  Auditoría de `OverlayLayers`: las 16 invocaciones están en la capa
  correcta según el contrato documentado en `OverlayLayers.cs`. World=0,
  AmbientTint=5, Hud=6, ContextMenu=8, SelectionInfo=9, Chronicle=10,
  ModalScrim=20, Modal=21, PlacementOverlay=40, Tutorial=50,
  Onboarding=80, FounderArrival=90, PauseAndNotifier=100. El scrim del
  ModalHost está en ModalScrim=20 (debajo del Modal=21), así que el
  panel siempre se ve sobre el scrim. Las vistas que reemplazan el
  mundo (HeroProfile, BuildingDetail) están en Hud=6 — correctamente
  por encima del AmbientTint y por debajo de cualquier modal.
- **`ObjectDisposedException` en `IconButton.OnLineageChanged`.** El
  manejador estático de `LineageThemeRegistry.ActiveLineageChanged` puede
  sobrevivir a la liberación del nodo cuando un wrapper C# queda libre
  sin pasar por `_ExitTree`. El evento se seguía disparando sobre el
  wrapper muerto y `AddThemeColorOverride` reventaba. Fix: descartar el
  evento si `!GodotObject.IsInstanceValid(this) || !IsInsideTree()`. El
  resto de los suscriptores del registro siguen actualizándose.
- **Bypass del workday antes del primer refugio.** El juego arranca con
  el reloj en tick 0 (medianoche) pero la política de horario laboral
  08:00–16:00 estaba activa desde el inicio, así que la primera
  construcción quedaba en `ApplyNightRest` durante 8 horas. La guarda
  de `CityWorld.AdvanceWorldTick` (edificios y proyectos) se cambió a
  `isLaborTime = GameClock.IsDaytime(_tick) || !HasCompletedFirstShelter()`,
  de modo que el fundador puede construir el refugio inicial a cualquier
  hora. Una vez que se completa el primer `BuildingKind.Home`, la
  política 08:00–16:00 entra en vigor normalmente. El bypass alinea el
  juego con el espíritu de EG-A0 (campamento fundacional = labor manual
  de supervivencia, no jornada laboral ciudadana). Helpers nuevos:
  `CityWorld.HasCompletedFirstShelter()`, `TestHelpers.SetTick(world, tick)`.
  Regression tests: `ConstructionTickTests.PreShelter_BeforeFirstDawn_*`
  y `ConstructionTickTests.PostShelter_BeforeFirstDawn_*`. Test
  existente migrado a post-shelter: `Night_RecoversStamina_AndAddsNoProgress`.
- **EG-0 — medición del early game (schema v20).** `EarlyGameMetrics` acumula
  tiempo hasta el primer refugio, recursos recolectados/gastados,
  días-ciudadano ociosos, horizonte de comida y ausencia por expedición.
  `EarlyGameMetricsReport` escribe `eg0-report.txt` junto al save. Dos
  decisiones que sostienen la fiabilidad del dato:
  1. Nada se cuenta por tick: `WorldTimeAdvance` batchea los tramos
     quiescentes, así que un contador por tick subestimaría justo los periodos
     ociosos que esto mide. Todo se registra en eventos de dominio o en la
     frontera del alba, que es el camino compartido por vivo y offline.
  2. El observador del `CityResourceLedger` se **suspende durante `Restore`**.
     Sin eso, cada recarga contabilizaría el inventario entero como recién
     recolectado, lo que ensucia cualquier playtest que verifique inventario.
  Una ciudad migrada desde v19 reporta cero muestras en vez de historia
  inventada. Tests: `EarlyGameMetricsTests`.
- **Rutas de guardado unificadas.** El reporte estaba enganchado a una sola de
  las cuatro rutas (`TrySaveNow`), así que el autosave lo dejaba congelado.
  Todas pasan ahora por `CityWorldController.SaveWorldToPrimarySlot`.
- **Tests de migración desacoplados de `CurrentVersion`.** Ocho ficheros
  encadenaban cada paso a mano y afirmaban `CurrentVersion` tras una migración
  de un solo paso, así que cada bump de schema rompía ocho tests. Ahora usan
  `MigrateToCurrent` para el tramo "ponlo al día" y afirman el número literal
  del paso bajo prueba.
- **Filtro ambiental día/noche.** Curva de dos velocidades (bandas de una hora
  en 05:00–06:00 y 18:00–19:00, tramos largos con deriva lenta, todo
  smoothstep) y **mezcla multiplicativa** en vez de velo con alfa: un overlay
  alfa escala el contraste por `1-alpha` y levanta el negro, así que una noche
  lo bastante notoria aplanaba el mapa en niebla. Tests:
  `TimeOfDayColorTests`.
- **El tinte ya no toca el HUD.** `OverlayLayers` no separaba mundo de
  interfaz —la capa 0 incluía "HUD chips"— así que el HUD quedaba debajo del
  tinte. Slots nuevos `AmbientTint` (5) y `Hud` (6), reclamados por barra de
  estado, barra de navegación, `BuildingDetailView` y `HeroProfileView`; además
  el tinte espeja `MacroStreetLiveView.Visible` como segunda defensa. Tests:
  `OverlayExclusionTests`.
- **Vista del héroe rediseñada** con splash art a altura completa a la
  izquierda y columna de texto scrollable a la derecha
  (`LineageSplashRegistry`). El ancho se calcula desde la proporción de cada
  textura: los modos proporcionales de `TextureRect` lo derivan de una altura
  aún sin resolver en la primera pasada de layout, y el arte cargaba pero nunca
  se dibujaba.
- **Acentos de linaje re-separados.** Ardhen, Orveth y Vaelun compartían una
  franja ámbar de 10° con Orveth y Vaelun a **2°** — sus tintes de UI no eran
  distinguibles. Ahora cobre (~20°), oro (~45°) y caqui (~62°), cada uno más
  cerca de su propia descripción. `tools/New-LineagePalettes.ps1` refleja los
  mismos valores y **se niega a generar** un juego donde dos acentos sean
  indistinguibles: cercanos en tono *y* luminosidad *y* saturación a la vez.
  Caelith y Kovari están a 11° a propósito; se separan por luminosidad.
- **Paletas de splash** en `art/palettes/`: una común de 36 colores, ocho de 28
  por linaje y una derivada de 64 por linaje para trabajar (Pixelorama muestra
  una paleta a la vez). Generadas, no elegidas a mano.

### 2026-07-30

- Refugio (panel detalle + macro): el contador "descansando" del panel leía
  `VisibleWorkerCount + HiddenWorkerCount` (basado en `_assigned`, siempre
  vacío porque el refugio no tiene receta), mientras que los slots muestran
  `VisibleCitizens` (ciudadanos con `CitizenLocation.AtHome`). Slots y resumen
  ya no se contradicen: ambos leen la misma fuente. Se añadió también la regla
  espejo de `ShouldHideHeroInsideShelter` para citizens no fundadores
  (`ShouldHideCitizenAtHome`), de modo que cerrar el detalle del refugio ya
  no los aparca en `anchors.Entrance` (literalmente delante del edificio) —
  se ocultan como el founder. El panel de selección del edificio usa el mismo
  contador coherente. Regression tests:
  `UiSnapshotTests.BuildingDetailSnapshot_HomeCountsCitizensAtHomeNotAssignedWorkers`
  y `MacroStreetLiveViewTests.NonFounderCitizenAtHome_IsHiddenUnlessWanderingOrRecovering`.
- Botón "Enviar" de expedición silencioso: el botón se deshabilitaba sin
  feedback. Se añadió tooltip que explica la causa (sin miembro elegible o
  expedición activa). Localization keys: `ui.expedition.dispatch_no_member_hint`
  y `ui.expedition.dispatch_active_hint`.
- Horario laboral movido a 08:00–16:00 (`GameClock.WorkdayStartTick`: 0 →
  1200). Tres bugs colaterales corregidos:
  1. `TryAdvanceQuiescentTicks` usaba `DayTicks - 1` como fin de fase, lo
     que asumía que el día empezaba en tick 0. Ahora usa
     `NextWorkdayEnd/NextWorkdayStart - 1`.
  2. El mismo método restaba `dayTick` (relativo al día) de
     `lastTickInPhase` (calculado con `NextWorkdayEnd/Start`, absolutos),
     así que al cruzar medianoche el batched-advance "se saltaba" el
     dawn boundary sin disparar mobilisation. Ahora resta `_tick`.
  3. `TestHelpers.NewHeroWorld/NewConstructionWorld/WorldWithHome` ahora
     avanzan al workday vía reflection sobre `_tick` (sin disparar
     mobilisation de food-ration con ciudad recién creada).
  - `VerticalLoopPersistenceTests.Recovery_ReloadedHalfway` queda omitido
    temporalmente porque compara JSON exacto de snapshots, y los IDs
    auto-incrementados de eventos cambian al desplazarse el inicio del
    mundo de 0 a 1200.

### 2026-07-29

- VS-3: herida persistente, tratamiento y territorio.
- VS-4: persistencia/offline exacta del loop en schema v19.
- Persistencia contextual y rutinas ciudadanas: contexto semántico, rutas
  reconstruidas, ocio/espera, Políticas, autosave de tres minutos y cámara
  libre.
- Frontera rueda/zoom: un `ScrollContainer` visible conserva la rueda incluso
  al alcanzar sus límites; el mapa no hace zoom detrás de la UI.

### 2026-07-28

- VS-0: ciudad causal, tránsito, recuperación y un solo carrier visual.
- VS-2: equipo, encuentro, retirada y retorno expedicionario.

## 8. Superadas recientes

### 🟠 H-29 — Terreno ortogonal de la vista plana

- **Cierre:** Superado el 2026-07-29.
- **Motivo:** `MacroStreetLiveView` es la única representación macro jugable.
  Consolidar `OrthogonalParcelTerrain` ya no mejora el producto actual. La
  necesidad vigente de terreno/obstáculos queda cubierta por H-26/H-32.

### 🟠 H-26 — Huellas, corredores y navegación

- **Cierre:** Superado el 2026-07-30.
- **Motivo:** la correspondencia entre huella de dominio (`ParcelPlacement` +
  `BuildingFootprintCatalog`), anclas del edificio (`BuildingVisualAnchors`)
  y obstáculos de la perspectiva (`_bandOccupancy` derivada en
  `MacroStreetLiveView.AddBandInterval` desde el mismo plot) usa el
  `Placement` como única fuente. `StreetRoutePlanner` + el guardarraíl de
  cadencia 12 Hz en `_Process` cubren el riesgo de "dos mallas
  autoritativas". Cobertura: `MacroStreetLiveViewTests`,
  `StreetDepthProjectionTests`, `StreetRoutePlannerTests`,
  `MacroInputBoundaryTests` y los fixtures de `Capture-VisualMatrix.ps1`
  que ejercitan gather entre hileras y construcción adyacente.

### 🟠 H-32 — Cierre de la perspectiva por calles

- **Cierre:** Superado el 2026-07-30.
- **Motivo:** la perspectiva por calles es la única vista macro jugable desde
  el cierre de H-29. No existe la vista ortogonal plana contra la cual
  pudiera haber dependencias residuales; el reanálisis ya no aplica. La
  cobertura de `MacroStreetLiveViewTests`, `StreetDepthProjectionTests`,
  `StreetRoutePlannerTests` y `MacroInputBoundaryTests` da por cerrado:
  gather entre hileras, construcción adyacente, entrada/salida del refugio y
  un solo carrier por citizen. El cierre del retorno expedicionario queda
  dentro de EG-4 (resource expedition seam) una vez el Founding Site provea
  el Cache que sostiene la duración corta.

## 9. Fuera de alcance hasta cerrar el prototipo

- Backend, servidor, base de datos, auth, telemetría, modding o segunda ciudad.
- Mobile, multiplayer, launcher, installer o settings completos.
- Combate completo, equipo, formaciones, mortalidad y generaciones.
- Política, cultura, comercio, economía y ambiente a escala completa.
- Profesiones profundas, instituciones, educación, relaciones y árboles de
  habilidades.
- Arte/audio final, múltiples biomas y grafo territorial amplio.
- Optimización masiva sin evidencia del profiler.

## 10. Referencias

- Estado canónico: `docs/CURRENT_STATUS.md`.
- Criterios del loop: `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`.
- Validación contra visión: `docs/VALIDATION.md`.
- Persistencia/rutinas: `docs/CITIZEN_OFFLINE_ROUTINE_AUDIT.md`.
- Matriz visual: `docs/VISUAL_REGRESSION.md`.
- Decisiones: `docs/ai/DECISION_LOG.md`.
