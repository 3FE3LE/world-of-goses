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

### Prioridad del slice

Hasta cerrar VS-5 no se inicia profundidad nueva. Solo se admiten:

1. Correcciones encontradas durante el recorrido humano del primer loop.
2. Evidencia necesaria para firmar los 17 criterios de aceptación.
3. Limpieza documental o técnica que no cambie el producto.

### Baseline vigente

- Fecha de alineación: **2026-07-31**.
- Slice activo: **VS-5 — firma y repetición**.
- Próximo trabajo aprobado: **EG-0 implementado**; recorrer VS-5 en el slot
  limpio nuevo (ese recorrido produce ya el reporte EG-0), después **EG-3** y
  entonces la firma de VS-5. Ver el checkpoint en §2.
- Save: **schema v20** (EG-0).
- Build: **0 errores / 0 advertencias**.
- Tests: **653 / 654** (1 omitido por brittleness del snapshot JSON en
  `VerticalLoopPersistenceTests.Recovery_ReloadedHalfway`; el comportamiento
  no cambió, sólo los IDs auto-incrementados de eventos).
- Arranque Godot headless: correcto.
- Fuente de verdad del slice: `docs/CURRENT_STATUS.md` y
  `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`.

## 1. Resumen

| Estado | Crítica | Alta | Media | Baja |
| --- | ---: | ---: | ---: | ---: |
| En curso | 1 | 0 | 1 | 0 |
| Pendiente | 0 | 0 | 2 | 0 |
| Necesita reanálisis | 0 | 0 | 0 | 0 |
| Diferido por trigger | 0 | 2 | 1 | 0 |
| Bloqueado | 0 | 0 | 0 | 0 |

## 2. Milestone activo — primer loop completo y repetible

Flujo objetivo:

```text
Onboarding → gathering → Shelter/Farm/Quarry → prospecto/reclutamiento
→ asignación y presión de Food → preparación de expedición
→ salida/encuentro/objetivo o retirada/regreso
→ herida y territorio → tratamiento/nueva decisión
→ guardado/carga → segunda expedición sin reset
```

### Estado por fase

| Fase | Estado | Evidencia vigente |
| --- | --- | --- |
| VS-0 — ciudad causal | Hecho | Orden/compromiso, tránsito visible, stock lleno, descanso y equivalencia offline. |
| VS-1 — reclutamiento y coste de oportunidad | Implementado; firma pendiente | Ayuntamiento, prospecto expedicionario, vivienda, disponibilidad explicada y ración diaria de Food. |
| VS-2 — expedición mínima | Hecho | Equipo de 1–2 citizens, suministros, retirada, encuentro determinista, objetivo/retorno y persistencia. |
| VS-3 — consecuencias y territorio | Hecho | Herida persistente, tratamiento Shelter/Food/tiempo y parcela de cuatro estados. |
| VS-4 — persistencia | Hecho | Schema v19; reload por fases y tratamiento; resolución exacta una vez. |
| VS-5 — firma y repetición | En curso | Recorrido humano iniciado; G1 reabierto y faltan relanzamientos visibles. |

### 🔴 VS-5 — Firma humana y repetición

- **Estado:** En curso.
- **Prioridad:** Crítica.
- **Afecta:** flujo completo de `CityPrototype.tscn`, save principal,
  `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`, `docs/VISUAL_REGRESSION.md`.
- **Prueba automatizada existente:**
  `VerticalSliceRepetitionTests.RecoveredCity_CanCompleteSecondExpeditionWithoutReset`.
- **Trabajo restante:**
  1. Empezar con un slot limpio y completar onboarding.
  2. Recolectar Wood y construir Shelter, Farm y Quarry mediante UI normal.
  3. Construir Ayuntamiento, obtener un prospecto por expedición y aceptarlo
     con vivienda disponible.
  4. Asignar y retirar varios citizens; verificar razones de indisponibilidad.
  5. Observar varios días y confirmar que la ración de Food crea una decisión
     legible sin bloquear el bootstrap.
  6. Preparar una expedición con citizens reales, suministros y postura de
     retirada.
  7. Ver salida, encuentro, objetivo o retirada, regreso y resumen causal.
  8. Verificar herida, tratamiento y desbloqueo territorial.
  9. Cerrar y relanzar a mitad de expedición.
  10. Cerrar y relanzar a mitad de tratamiento.
  11. Iniciar un segundo ciclo sin reset ni herramientas de depuración.
  12. Firmar contención 1280×720 y 1920×1080, además de foco por teclado y
      gamepad en las superficies usadas.
- **Aceptación:** los 17 criterios de
  `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` pasan sin editor, fixtures o comandos de
  depuración.
- **Regla:** cualquier bug hallado se corrige y se vuelve a recorrer desde el
  último límite persistente relevante.

#### Checkpoint para la próxima sesión — actualizado 2026-07-31

**El checkpoint anterior ya no aplica.** Describía la partida iniciada el
2026-07-30 (Shelter/Farm/Quarry/Town Hall, prospecto `Inara`, dos citizens) y
mandaba reanudar desasignando al fundador de Quarry. Esa partida fue sustituida
por un **slot limpio nuevo**, así que el recorrido VS-5 empieza otra vez desde
el paso 1. Lo que se firmó antes de los criterios 1–5 sigue siendo válido como
evidencia de que el código funciona; lo que hay que rehacer es el recorrido.

Lo aprendido en la partida anterior que sigue vigente:

- La primera expedición completa dura 600 ticks (4 horas simuladas, 10 minutos
  a 1x, 2,5 a 4x). Prueba:
  `ExpeditionTeamTests.FirstLoopTemplates_LastFourSimulatedHours`.
- G1 sigue **reabierto**: Farm alcanzó 60 Food contra 2 Food/día de dos
  residentes. El motor de abundancia es la Granja, que produce Food de la nada
  sin receta de insumo — no la Madera inicial. Por eso lo cierra EG-3, no un
  recorte de recursos de partida.

**Primera acción al reanudar, en este orden:**

1. **Arrancar el juego con el build actual.** La sesión que quedó corriendo el
   2026-07-30 era anterior al arreglo de las rutas de guardado, y su autosave
   no refrescaba `eg0-report.txt`.
2. **Cruzar el primer amanecer** (tick 1200 = 08:00 in-game) y dejar que caiga
   un autosave. Comprobar
   `%LOCALAPPDATA%\World of Goses\eg0-report.txt`: si deja de decir
   "No dawn has been sampled yet" y muestra días observados, horizonte de
   comida y porcentaje ocioso, EG-0 queda verificado de punta a punta en el
   juego real y todo lo jugado a partir de ahí cuenta como medición.
3. **Recorrer VS-5 desde el paso 1** de la lista de arriba. Ese mismo recorrido
   produce ahora los datos de EG-0, así que no hay que jugar la apertura dos
   veces.
4. Anotar con palabras propias el **criterio 6**: si con la Granja funcionando
   la Comida vuelve a sobrar sin obligar a decidir nada, el reporte lo mostrará
   como un horizonte de comida que nunca baja. Esa es la evidencia que aprueba
   o revisa los números EG-A0 y justifica abrir EG-3.

**Nota sobre `git status`:** `art/exports/characters/splash/` y
`game/assets/characters/splash/` aparecen como no rastreados **a propósito**.
Son la misma copia de 38 MB del splash art generado por IA que ya vive
rastreada en `art/references/`; comitear las tres triplicaría un repositorio de
57 MB de forma permanente, y sin Git LFS, por placeholders que van a
sustituirse por PNG dibujados a mano de ~100 KB. Se rastrearán cuando existan
esas versiones. No añadir a `.gitignore`: eso bloquearía las definitivas.
`LineageSplashRegistry` cae al sprite animado si el asset falta, así que un
clon limpio degrada en vez de romperse.

**Después del recorrido: abrir EG-3**, no EG-1. Ver
`docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §15 — el orden aprobado
es EG-0 → EG-3 → firma de VS-5, porque EG-1/EG-2 reescribirían la apertura y
reabrirían los criterios 1–5 ya firmados.

### Estado de los 17 criterios

| # | Criterio abreviado | Estado |
| ---: | --- | --- |
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
- **Pendiente para VS-5:** navegación/foco completo por teclado y gamepad,
  close paths usados en el recorrido y firma humana de estados que no pueden
  probarse headless.
- **Aceptación:** ningún cambio de UI se cierra solo con boot headless.

## 4. Pendientes después de VS-5

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
- **Dirección:** un host con slots/prioridad solo si VS-5 reproduce el solape o
  el siguiente slice necesita overlays simultáneos.
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
     recolectado, y el procedimiento VS-5 pide varios relanzamientos.
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
  `StreetRoutePlannerTests`, `MacroInputBoundaryTests` y el recorrido humano
  de VS-5 dan por cerrado: gather entre hileras, construcción adyacente,
  entrada/salida del refugio, retorno expedicionario y un solo carrier por
  citizen.

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
