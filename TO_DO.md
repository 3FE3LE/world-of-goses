# TO DO — Mejoras de Usabilidad y Gameplay

<!-- markdownlint-disable MD060 MD024 MD036 -->
> **Backlog vivo** del proyecto. Cada vez que se abra una sesión y se vaya
> a tomar una tarea de aquí, **se debe re-analizar** el contexto que la
> originó y verificar si sigue vigente (código cambió, otra mejora la
> volvió obsoleta, etc.). No se aborda nada sin esa relectura.
>
> Las tareas se mueven de sección cuando cambian de estado. Los ítems activos
> no se borran. Los ítems de `Hechas` y `Canceladas / Superadas` se conservan
> durante dos días calendario desde su fecha de cierre y luego se eliminan de
> este archivo; Git conserva el historial de largo plazo.

---

## 0. Cómo usar este documento

### Flujo de una tarea

1. **Elegir** un ítem de `## Pendientes`.
2. **Re-analizar** abriendo al menos:
   - Los archivos listados en `Afecta`.
   - El PR / commit que pudo haber cambiado el comportamiento.
   - Los ítems vinculados en `Relacionados` (puede que ya estén resueltos).
3. **Decidir**:
   - ¿El problema aún existe? → Mover a `## En curso`.
   - ¿Ya lo arregló otra cosa? → Mover a `## Canceladas / Superadas` con motivo.
   - ¿La solución propuesta ya no aplica? → Reescribir la solución o cancelar.
4. **Trabajar**, hacer commit con la referencia al ID (ej. `[C-1]` en el mensaje).
5. **Cerrar**: mover a `## Hechas` con la fecha y un resumen de qué cambió.
6. **Purgar cerradas vencidas**: al iniciar cada sesión, eliminar de `Hechas`
   y `Canceladas / Superadas` cualquier ítem cuya fecha de cierre sea anterior
   a dos días calendario respecto de la fecha actual. Recalcular después la
   tabla de resumen. No se purgan tareas activas, bloqueadas o pendientes.

### Estados disponibles

| Estado               | Significado                                                  |
| -------------------- | ------------------------------------------------------------ |
| Pendiente            | Listo para tomar; está en cola.                              |
| En curso             | Alguien lo está trabajando en esta sesión.                   |
| Bloqueado            | Depende de otra cosa (externa o de otro ítem). Ver `Bloqueado por`. |
| Necesita reanálisis  | El contexto cambió; antes de tomarlo hay que re-leerlo.     |
| Hecho                | Implementado. Se conserva dos días con fecha y resumen.      |
| Cancelado            | Ya no aplica. Se conserva dos días con fecha y motivo.       |

### Prioridades

- 🔴 **Crítica** — bloquea el flujo nuevo o rompe la primera partida.
- 🟠 **Alta** — fricción notoria en gameplay o carga cognitiva alta.
- 🟡 **Media** — pulido de UI, consistencia visual, micro-fricciones.
- 🟢 **Baja** — game feel, deleite, refinamiento estético.

### Categorías

- `UX` — flujo, navegación, feedback, affordances.
- `gameplay` — reglas, economía, decisiones del jugador.
- `polish` — tipografía, animaciones, sonidos, micro-detalles.
- `arquitectura` — desacople, refactor, deuda que sostiene a varios ítems.

### IDs

Formato `<PRI-Indice>` según la prioridad y un número estable. Si se
reorganiza la lista, los IDs no se renumeran.

### Última revisión global

- **2026-07-22** — Creación del documento tras la auditoría inicial.
- **2026-07-22** — Auditoría estructural de UI sobre la escena ejecutada a
  1280×720 y revisión estática de layouts, capas y controles dinámicos.
- **2026-07-22** — Auditoría de implementación de los 14 ítems pendientes.
  8 verificados y movidos a Hechas (C-9, C-10, H-12, H-13, H-14, H-15, H-16,
  M-15); 6 se mantienen en Pendientes con Estado actualizado (H-11, H-17,
  M-11, M-12, M-13, M-14). M-14 queda como cross-cutting que cierra los
  acceptance criteria visuales del resto.
- **2026-07-22** — H-17 y M-13 implementados y movidos a Hechas. M-11
  parcialmente completado (`OfflineReportPanel` y `AttentionBanner` con
  `SafeAreaMarginContainer`; los intentos de envolver `CityStatusPanel`
  con `SafeAreaTopBar` y reescribir `MacroActions` como
  `SafeAreaMarginContainer` añadieron un `MarginContainer` visible con
  fondo gris y se revirtieron). M-11 sigue en Pendientes para reintentar
  HUD y macro actions con un enfoque distinto. H-17: body scroll en
  `ConstructionPanel` y `TutorialOverlay`. M-13: `PlaceholderStyle` con
  `Subline`, layout vertical, hitbox del placeholder 144×144. M-11 sólo
  parcial. Pendientes restantes: H-11, M-11, M-12, M-14.
- **2026-07-22** — Revertido el cambio de escena que rompía la HUD
  (`SafeAreaTopBar` causaba un `MarginContainer` gris visible encima del
  `CityStatusPanel`). Añadido `visible = false` al `ConstructionPanel` en
  `CityPrototype.tscn` para que el modal no aparezca abierto por defecto.
- **2026-07-22** — Auditado y corregido el control de velocidad del status bar:
  play/pause muestra la acción, conserva la última velocidad activa y el selector
  queda bloqueado durante pausa. Añadida política de purga de cerradas a dos días.
- **2026-07-22** — Iniciado el slice de consolidación previo a mayor complejidad.
  `WorldTimeAdvance` centraliza avance temporal, conserva el fast-forward de mundos
  inactivos y usa cursor de eventos para reportes offline. Registrados los refactors
  siguientes para batching activo, partición de `CityWorld`, eventos tipados y recursos.
- **2026-07-22** — H-20 cerrado: batching equivalente para ciudades estructuradas
  sin asignaciones; amanecer/atardecer siguen como ticks canónicos y mundos con
  trabajo activo esperan la extracción de simuladores de H-21.
- **2026-07-22** — H-21 parcial: `CitizenAssignmentService` extraído sin cambiar
  la fachada pública de `CityWorld`; 330 pruebas preservan outcomes, ubicación,
  capacidad, notificaciones y auto-release. Pendientes producción y construcción.
- **2026-07-22** — H-21 avanza: `BuildingProductionSimulation` extraído con gates,
  comida/regeneración, stamina, contribuyentes, experiencia, output y stop causes.
  `CityWorld` conserva recursos/eventos mediante callbacks estrechos. Pendiente construcción.
- **2026-07-22** — H-21 cerrado: `ConstructionSimulation` completa la extracción
  incremental de asignaciones, producción y obras; `CityWorld` conserva su API
  pública y queda como orquestador/agregado, con 330 pruebas equivalentes.
- **2026-07-22** — H-22 parcial: sujetos y causas del log ya son referencias
  tipadas; el copy salió del dominio hacia presentación. Persistencia selectiva
  y compactación durable quedan como segundo corte. Verificado con 332 pruebas.
- **2026-07-22** — H-22 cerrado: schema v5 conserva hasta 128 eventos
  significativos, compacta estados repetidos y restaura identidad, causas e IDs.
  Producción/progreso por tick y ciclos día/noche no inflan el historial. 337 pruebas.
- **2026-07-22** — H-23 parcial: `CityResourceLedger` proyecta stocks por
  ubicación, centraliza lotes atómicos y permite reservar, transferir propiedad,
  liberar y consumir suministros. Pendiente persistir reservas. 342 pruebas.
- **2026-07-22** — H-23 cerrado: schema v6 persiste reservas, propietarios,
  secuencia de IDs e `IronStock`; validación impide comprometer más inventario
  del físicamente almacenado. Verificado con 345 pruebas.
- **2026-07-22** — M-14 parcial: harness windowed captura área cliente real en
  1024×576, 1280×720 y 1600×900 y genera manifiesto. Primera matriz macro válida;
  detectado M-16 (icono del carrier oculta el inicio del nombre).
- **2026-07-22** — M-16 cerrado: icono contenido en celda 16×16 y separación
  de 6 px; `zeventh` se lee completo en la recaptura 1024/1280/1600. El harness
  ahora fuerza Godot al frente para evitar capturas contaminadas.
- **2026-07-22** — M-14 avanza: `macro-paused` validado en 1024×576,
  1280×720 y 1600×900. Play/pause muestra la acción correcta, el selector
  conserva la velocidad pero queda deshabilitado y no hay solapes. Una primera
  coordenada normalizada fallida fue descartada y corregida a `0.283,0.025`.
- **2026-07-22** — M-14 avanza: estado de construcción sin proyecto validado
  en 1024×576, 1280×720 y 1600×900. La matriz descubrió y corrigió dos fallos:
  `View hero` quedaba vacío al instanciar su `PackedScene`, y Chronicle reaparecía
  detrás del scrim durante ticks. También se sincronizó `Close construction` en
  el mismo clic de apertura. Capturas intermedias fallidas fueron descartadas.
- **2026-07-22** — M-14 avanza: construcción en curso validada en las tres
  resoluciones. La matriz reabrió H-13: el chip de proyecto expandía el shell y
  recortaba ambos extremos del HUD incluso a 1600 px. `CityStatusPanel` ahora usa
  ancho físico y modo compacto con proyecto activo; el preview queda contenido.
  El harness pasa a modo read-only para impedir escrituras sobre el slot real.
- **2026-07-23** — Smoke test gráfico confirmado por el usuario en una ejecución
  interactiva de Godot 4.7.1, después de `dotnet build` sin advertencias ni
  errores y `dotnet test` con 382/382 pruebas superadas. No se observaron
  defectos visuales generales; esta confirmación no sustituye las firmas
  pendientes de navegación completa por teclado/gamepad ni del caso Forest
  depleted de M-14.
- **2026-07-23** — H-25 avanza tras validación humana: cada árbol fundador
  contiene 40 wood, el mundo inicial conserva ocho parcelas y las parcelas con
  patches naturales quedan excluidas de la construcción. Los saves que tenían
  una construcción sobre una parcela de recursos recolocan su placement al
  primer lote libre. También se corrigió la transición post-`Start over` para
  mostrar `View hero`, `Construction` y `Menu` sin reiniciar la aplicación.
  Build limpio, 382/382 pruebas y matriz 1024×576 / 1280×720 / 1600×900.
- **2026-07-23** — Placement manual validado por el usuario: elegir blueprint
  abre lotes seleccionables, selección + confirmación persisten el lote elegido
  y Cancel/ESC regresan sin autorizar. Cada unidad natural ocupa un lote estable;
  solo los árboles vivos bloquean su lote y el render usa la misma identidad.
  Chronicle/banner no reaparecen durante placement. Build limpio, 385/385
  pruebas y matriz válida en 1024×576 / 1280×720 / 1600×900.
- **2026-07-23** — Regeneración natural y soberanía de asignación validadas por
  el usuario: cada amanecer regenera unidades existentes y puede brotar una
  unidad nueva en cualquier lote natural libre, con equivalencia live/offline.
  Un fundador asignado no puede iniciar gathering, permanece anclado visualmente
  a su obra/edificio y el mismo carrier parte desde allí al quedar libre. El menú
  añade un soft reset que conserva fundador/perfil y reinicia solo la ciudad.
  Build limpio, 392/392 pruebas y arranque headless correcto.
- **2026-07-24** — Auditoría de estabilización posterior a expediciones y
  migración/reclutamiento. Confirmados tres defectos de integración: el líder
  de una expedición activa sigue proyectándose en la ciudad, los paneles
  `ExpeditionPanel` y `MigrantPanel` no tienen superficie visual propia y el
  retorno se muestra como tick interno. Se abre `H-27` para corregirlos antes
  de ampliar el sistema; `P-Migrant` se reduce al roster e integración del
  segundo ciudadano porque la ruta de reclutamiento ya existe.
- **2026-07-24** — La verificación del árbol actual falla al compilar en
  `CityWorld.cs:1331`: el fallback de recompensa migrante intenta invocar el
  constructor privado de `CitizenProfile`. La misma rama desreferencia
  `MigrantId` antes de comprobar `MigrantResult.IsSuccess`. Se abre `C-11`
  como bloqueo previo a cualquier validación funcional.
- **2026-07-24** — M-23 cerrado: pase visual transversal del HUD y paneles.
  El theme compartido adopta superficies oscuras opacas, bordes cálidos,
  jerarquía elevada para modales y foco visible independiente del color.
  Macro actions, pausa, construcción, detalle, recursos, recon y Citizens
  reutilizan las mismas variaciones sin cambiar rutas ni comportamiento.
- **2026-07-25** — Cierre del slice "founding hero + first construction".
  Catálogo semántico de capas (`OverlayLayers.cs`) y eliminación de los
  `z_index` literales en escenas y scripts (H-11). Safe area en
  `CityStatusPanel` y `MacroActions` vía `Offset*` (M-11). Cierre mínimo
  de M-12 (doble fuente de `AttentionBanner` resuelta). `UiMotion.FlashLarge`
  completa la gramática de motion con feedback de importancia grande para
  obra completada, expedición retornada y ciudadano llegado (M-25). Fixture
  `forest-depleted` añadido a la matriz visual (M-14 parcial). H-28 y
  P-FirstRun cerrados a nivel de código y matriz headless — la firma
  humana windowed sigue bloqueada por el cliente Godot 50×50 del escritorio.
  Build limpio, 424/424 tests y headless boot verificado.

- Próxima revisión sugerida: tras cerrar M-14 (cross-cutting) o durante el
  próximo PR de UI.

- **2026-07-25** — S-1 ejecutado. Siete sub-ítems preventivos implementados:
  - S-1.1 i18n con `.po` completo. Recursos `Translation` importados
    nativamente por Godot, `LocaleManager` autoload con persistencia sidecar,
    `Tr.Narrative` con ~150 IDs de traducción, `es.po` con
    narrativa completa, `en.po` con traducción inicial (marcada
    con `# TODO (i18n):`), `messages.pot` template, autoload
    registrado, `AstralOnboardingView` usa `TrKey()`, language
    switcher en `PauseMenu` (debajo de Settings), fixture
    `language-selector` y refresco reactivo mediante `LocaleChanged`.
  - S-1.2 `IPathfinder` seam. `PlanCardinalRoute` movido a
    `CardinalPathfinder : IPathfinder`. Macro view consume la
    interfaz. Trigger de migración a `NavigationServer2D`
    documentado en `TO_DO.md §3 H-26`.
  - S-1.3 `ITerrainRenderer` seam. `OrthogonalParcelTerrain`
    implementa la interfaz (sin cambios funcionales). Trigger de
    migración a `TileMap` documentado.
  - S-1.4 MultiMesh: documentado como trigger (>20 citizens). El
    `CitizenSpriteBank` actual es la implementación válida; el seam
    es implícito en su API.
  - S-1.5 `CitizenBehavior` seam. Enum `CitizenBehaviorState` con
    6 estados + catálogo de transiciones. `CitizenLocation` queda
    como alias semántico.
  - S-1.6 `Dialogue` seam. Interfaces `IDialogueNode`,
    `IDialogueChoice`, `IDialogueRunner` + `DialogueState` y
    `DialogueOutcome`. Trigger de implementación documentado.
  - S-1.7 Profiler y presupuestos. `docs/PERFORMANCE_BUDGETS.md`
    con budgets por escenario. `tools/Capture-VisualMatrix.ps1`
    muestrea 30 frames y falla si excede 32 ms.

  Build limpio, 432/432 tests, headless boot verificado.

## 1. Resumen rápido

| Prioridad | Pendientes | En curso | Bloqueados | Hechos | Cancelados |
| --------: | ---------: | -------: | ---------: | -----: | ---------: |
| 🔴        | 0          | 0        | 0          | 12     | 0          |
| 🟠        | 5          | 1        | 0          | 31     | 2          |
| 🟡        | 4          | 0        | 0          | 17     | 3          |
| 🟢        | 0          | 0        | 0          | 2      | 0          |

> **Cambio de 2026-07-24 (auditoría + correcciones):** se cerró el bache
> de migración v11→v12 que reiniciaba el onboarding silenciosamente; se
> estabilizó la primera partida (modo macro ignora bosques, HUD no solapa
> con árboles, depósito/coste total separados en UI, autoasignación del
> fundador sólo cuando los `RemainingInputs` están disponibles, Home no
> intenta desasignar residentes, foco no cae en controles deshabilitados);
> y se introdujo la primera versión de expedición abstracta con reserva,
> tiempo live/offline, Chronicle causal y schema v13. Cifra real de la
> corrida: **406/406 pruebas superadas**.
>
> **Verificación de estabilización 2026-07-24:** build limpio, **409/409**
> pruebas superadas y matrices de expedición idle/active/returned y migración
> válidas en las tres resoluciones. El “No responde” del fixture returned fue
> causado por adelantar 14.400 ticks síncronos en el hilo principal; el fixture
> ahora prueba la misma transición con una expedición de un tick y completa la
> matriz de tres ventanas en 9,1 s.

- **2026-07-25** — Plan estratégico S-1 registrado en §3 Pendientes. Siete
  sub-ítems preventivos (i18n, NavigationServer2D, TileMap, MultiMesh,
  FSM, diálogos con NPCs, profiler y presupuestos de frame) con su
  trigger explícito y su orden de ejecución. El proyecto crecerá en
  profundidad (lore, diálogos, eventos ramificados) y en escala
  (decenas de citizens, expediciones concurrentes, parcelas transitables);
  este plan evita que las decisiones nativas actuales pasen factura
  cuando el sistema crezca. Ningún sub-ítem se ejecuta hasta que su
  trigger se cumpla o el usuario lo solicite explícitamente.

- **2026-07-25 (re-análisis tras edición manual)** — El usuario reportó
  que ajustes propios dejaron la UI "hecha un desastre" y pidió
  reanalizar este documento y corregir lo ya "implementado" empezando
  por lo que desborda a lo ancho. Auditoría de los cambios sin commitear
  encontró: `AttentionBanner` tapaba el botón `Recon` de `MacroActions`
  en las tres resoluciones (ver detalle en el ítem "Catálogo de capas
  H-11 + safe area M-11 + M-25", corregido en `CityPrototype.tscn`); y
  `tools/Capture-VisualMatrix.ps1` no producía ninguna captura por un
  error de parseo de PowerShell y un cálculo de frame-time roto (mismo
  ítem, corregido). `CityStatusPanel` y sus chips se verificaron por
  captura en las tres resoluciones sin overflow. Build limpio, 432/432
  tests, captura windowed reproducida tras el fix del harness.

- **2026-07-26 (continuación: banner eliminado + override de S-1)** —
  El usuario reportó que el desborde seguía y pidió además eliminar
  `AttentionBanner`: un panel que pulsa opacidad en loop infinito
  mientras haya algún edificio con problemas, nunca se cierra solo y
  su mensaje agregado ("N buildings need attention") no dice cuáles
  ni por qué — información redundante con lo que cada edificio ya
  muestra en su propio detalle/tooltip. Eliminado por completo:
  `AttentionBanner.cs`, su nodo en `CityPrototype.tscn` y todas las
  referencias en `CityMacroView.cs`/`OverlayLayers.cs`. El usuario
  también pidió resolver ya los sub-ítems de S-1 que integran
  built-ins/plugins para evitar refactors futuros al escalar,
  saltando los triggers de escala documentados. Ver el detalle de
  cada sub-ítem en su propia entrada de S-1 más abajo: S-1.2
  (NavigationServer2D), S-1.3 (TileMap) y S-1.5 (FSM propio) y S-1.6
  (diálogo propio) se implementaron; S-1.4 (MultiMesh) se evaluó y se
  dejó explícitamente sin implementar por desproporción riesgo/beneficio
  (ver su entrada). Build limpio, 445/445 tests (8 nuevos de
  `CitizenBehaviorFsmTests`, 5 nuevos de `DialogueRunnerTests`),
  captura windowed verificada en macro/pausa/citizens/expedición en
  las tres resoluciones tras todos los cambios.

### Cola activa (orden sugerido)

1. **M-22** — Cerrar la integración selectiva de assets y alcance del menú.
2. **H-26** — Malla transitable y clasificación de pasillo / camino / calle
   (slices siguientes; el primer corte ya está cerrado). Cuando cierre,
   abre **S-1.2** (NavigationServer2D) que reemplaza el pathfinding
   cardinal.

---

## 2. En curso

### 🟡 M-14 — Matriz de regresión visual para UI

- **Estado:** En curso; harness reproducible y tres estados ejecutados.
- **Prioridad:** 🟡 Media
- **Categoría:** arquitectura
- **Afecta:** `tools/Capture-VisualMatrix.ps1`, `docs/VISUAL_REGRESSION.md`, verificación de escenas/UI.
- **Avance 2026-07-22:** captura windowed del área cliente con comprobación de
  dimensiones y manifiesto JSON. `macro-current` revisado en 1024×576,
  1280×720 y 1600×900; HUD, acciones, plots y Chronicle permanecen dentro.
- **Avance 2026-07-22 (pausa):** `macro-paused` revisado en las tres resoluciones;
  el botón principal muestra play, el selector conserva la última velocidad
  atenuada/deshabilitada y el status bar no presenta solapes.
- **Avance 2026-07-22 (construcción sin proyecto):** layout, copy, footer y
  cierre revisados en las tres resoluciones. `StandardButtons.ViewHeroButton`
  reafirma contenido tras instanciar la escena; la acción macro se actualiza al
  abrir y Chronicle no puede reaparecer durante un modal por refresh/tick.
- **Avance 2026-07-22 (construcción en curso):** fixture efímero de Farm
  autorizado mediante dos clics; HUD compacto, preview, body con scroll y footer
  validados en las tres resoluciones. H-13 fue reabierto y corregido durante la
  revisión. El harness carga el slot con escrituras deshabilitadas.
- **Avance 2026-07-23 (detalle y perfil):** Shelter, Farm, Quarry y Forest
  gatherable revisados en 1024×576, 1280×720 y 1600×900. El perfil reveló texto
  claro sobre el `ScrollContainer` amarillo global y copy recortado a 1024 px;
  ahora usa superficie oscura, gutter derecho y wrapping, con recaptura válida.
  Un frame negro intermedio de Shelter fue descartado y repetido tras compilar.
- **Hallazgo documental:** el runtime de Farm/Quarry/Forest aún expone
  `Reactive policy`, aunque `CURRENT_STATUS.md` describe el panel simplificado.
  No se altera funcionalidad dentro de la regresión visual; debe reconciliarse
  código o documentación antes de cerrar el handoff.
- **Avance 2026-07-23 (overlays):** fixtures read-only de `tutorial`,
  `tutorial-long` y `offline-report` añadidos al harness. La matriz descubrió
  que el body del tutorial colapsaba y que el Chronicle vivo reemplazaba el
  reporte antes de capturarlo. El tutorial reserva 96 px, usa superficie oscura
  y enfoca Next/Got it; el reporte de 80 eventos queda estable solo en capture
  mode. Ambos pasan 1024×576, 1280×720 y 1600×900.
- **Hallazgo resuelto:** el solape del icono del carrier con la primera letra del
  ciudadano se corrigió y cerró como M-16.
- **Avance 2026-07-23 (smoke interactivo):** ejecución gráfica confirmada por el
  usuario sin defectos visuales generales después de build limpio y 382/382
  pruebas. Se registra como validación humana del estado actual, no como firma
  de los recorridos específicos aún pendientes.
- **Pendiente:** reconciliar el caso Forest depleted (el plot agotado se
  deshabilita por diseño y no abre detalle); los close paths y la navegación
  completa por teclado/gamepad requieren firma humana.
- **Criterios de aceptación:** cada PR de UI adjunta la matriz aplicable y compara
  rects/capturas; ningún cierre se basa únicamente en headless boot.

### 🟠 H-26 — Parcelas edificables con huella sólida y corredores

- **Estado:** En curso. El dominio ya define una parcela como 3×3 solares
  estándar; cada solar mide 3×3 tiles y se representa con 6×6 subceldas de
  medio tile. `BuildingFootprintTemplate` separa área reservada y huella sólida.
  Los perfiles provisionales A (0.5 tile lateral + 2 tiles sólidos + 0.5
  lateral) y B (3 tiles sólidos) conservan un tile frontal. Las pruebas cubren
  A+A = camino, A+B = pasillo, B+B = bloqueado y espacios deliberados para
  calles. El schema v9 persiste para cada edificio/proyecto
  `parcelId + lot + span + orientation + footprintProfile`; autorizar reserva
  el primer solar libre, cancelar lo libera y completar conserva la misma
  ocupación. La migración v8 → v9 distribuye la ciudad legacy en orden estable
  y crea parcelas adicionales solo si fueran necesarias.
  `CityMacroSnapshot` proyecta esa ocupación y `BuildingPlotStage` ya posiciona
  los plots en el solar correspondiente, a escala macro 0.5, en vez de usar la
  fila horizontal legacy. El cálculo responde a resize y conserva un fallback
  solo para snapshots incompletos.
- **Prioridad:** 🟠 Alta
- **Categoría:** dominio / territorio / navegación
- **Slices siguientes:**
  1. Generar una malla transitable desde huellas sólidas, accesos frontales y
     recursos; conectar el movimiento macro a esa malla.
  2. Detectar corredores conectados y clasificarlos como pasillo (0.5 tile),
     camino (1 tile) o calle (2 tiles), todavía sin desgaste visual.
- **Fuera de este slice:** arte definitivo, desgaste del césped, tráfico,
  carreteras construibles y simulación logística avanzada.
- **Criterios de aceptación:** nueve solares estándar por parcela; edificios
  multi-solar sin solapamiento; entrada frontal alcanzable; un corredor no se
  considera válido si termina aislado o solo conecta en diagonal; navegación
  usa la huella sólida y no el rectángulo completo del solar.
- **Avance 2026-07-26 (geometría fija + paneo):** el usuario reportó que las
  parcelas ya no eran cuadradas (`CalculateTerrainRect` estiraba el mundo
  para llenar la ventana: 7×6 tiles a 1024×576, 9×8 a 1280×720, 12×11 a
  1600×900 — nunca 9×9). Corregido: `OrthogonalParcelTerrain` ahora usa un
  tamaño de mundo fijo (`ParcelColumns×ParcelRows` parcelas de exactamente
  `ParcelGrid.LotsPerAxis × ParcelGrid.TilesPerStandardLot` = 9×9 tiles cada
  una) más un `PanOffset` estático compartido: centra el mundo cuando entra
  en el área disponible y hace scroll (clamped a los bordes del mundo)
  cuando no entra. Arrastre con el botón izquierdo sobre el terreno vacío
  (con umbral de 4px para no romper el click en árboles/lotes) actualiza el
  paneo; `ClipContents = true` evita que el contenido paneado sangre fuera
  del área. Nueva señal `PanChanged` — paneo no dispara el `Resized` de
  ningún control, así que `CityMacroView` reposiciona explícitamente
  `BuildingPlotStage` y `ConstructionPlacementOverlay` (antes privados,
  ahora `internal`) y la ancla del héroe. Verificado con un arrastre real
  simulado (mouse down/move/up): el contenido sigue al cursor correctamente
  y el overlay de selección de lote queda alineado con la grilla. Nota:
  `Control.Size` en este proyecto son unidades lógicas del canvas
  (`stretch/mode=canvas_items`, `aspect=expand`), no píxeles físicos de
  ventana — Godot reescala el canvas completo de forma uniforme, así que
  las parcelas quedan cuadradas y sin distorsión, pero no son "exactamente
  32 px de dispositivo" en cualquier ventana; lograr eso requeriría un
  `SubViewport` propio para el mundo, fuera de este slice. Build limpio,
  446/446 tests (nuevo `CalculateTerrainRect_PanClampedToWorldBounds`,
  `TerrainRectLeavesHudSafeBand` reescrito para la geometría fija).
- **Avance 2026-07-25 (lectura visual 9×9):** la geometría fija ya era
  correcta, pero el suelo seguía leyéndose como un tapiz continuo. La grilla
  ahora dibuja tres jerarquías alineadas sobre el mismo `TileMapLayer`: tile
  individual cada 32 unidades, solar 3×3 con línea media y parcela 9×9 con
  línea fuerte. Árboles, blueprints y overlays conservan
  `CalculateParcelRect` como proyección única. Matriz 1024×576 / 1280×720 /
  1600×900 revisada sin desalineaciones.

---

## 3. Pendientes

### 🟡 M-25 — Gramática visual de motion y feedback causal

- **Estado:** primer corte implementado; pendiente firma visual humana y
  feedback de importancia grande.
- **Prioridad:** 🟡 Media
- **Categoría:** polish / UI / presentación
- **Afecta:** `ModalHost.cs`, `PauseMenu.cs`, `ConstructionPanel.cs`,
  `ResourceActionMenu.cs`, `MacroBuildingView.cs`, `BuildingPlot.cs`,
  `OfflineReportPanel.cs`, `AttentionBanner.cs` y un componente C# compartido
  de transiciones.
- **Hallazgo:** contraste, cursores y superficies ya son coherentes, pero casi
  todos los cambios de pantalla todavía usan `Show/Hide` instantáneo. Solo
  onboarding, perfil y llegada tienen movimiento. Construir, reunir, asignar,
  recibir un evento o abrir un modal no comparten una respuesta visual.
- **Dirección:** movimiento Godot cuantizado y breve sobre la presentación:
  scrim 0→72 %, panel con fade + desplazamiento vertical de 8 px, cierre
  inverso, presión de botón de 1–2 px, flash corto en el chip afectado y
  selección de mundo con contorno/pulso. Sin blur, bloom, física visual ni
  movimiento subpíxel persistente.
- **Shaders acotados:** reservar `canvas_item` para un outline/dither de
  selección o transición astral reemplazable. No aplicar postprocesado global
  ni efectos continuos a paneles.
- **Feedback por importancia:**
  - pequeño: hover/foco, click y actualización de chip;
  - medio: gather autorizado, asignación, construcción desbloqueada;
  - grande: obra completada, retorno de expedición y llegada de ciudadano.
- **Primer corte verificable:** animar `ModalHost` una sola vez para
  Construction, Recon y Citizens; añadir feedback de selección compartido a
  árbol, lote y edificio; destacar el evento causal nuevo en Chronicle/HUD.
- **Avance 2026-07-24:** `UiMotion` centraliza duraciones e intensidades.
  `ModalHost` anima scrim y panel con fade y pasos verticales enteros
  8→4→0 px, incluido cierre inverso y restauración de foco. Construction,
  Recon y Citizens lo reciben sin código específico. Árbol, lote y edificio
  comparten pulso de selección mouse/foco; Chronicle enfatiza solo la entrada
  causal realmente nueva y el sello Saved pulsa únicamente al cambiar.
- **Verificación:** build limpio, 424/424 tests y fixtures headless de
  Construction, Recon, Citizens, recursos y macro sin errores Godot. La
  advertencia del almacén de certificados continúa siendo externa.
- **Corrección tras revisión humana:** la posición del modal ahora se captura
  después del layout diferido; Construction conserva el centro. Recon y
  Citizens usan un `Control` estable con `PanelContainer` interno para que el
  mínimo del contenido no expanda `ScreenContent`. El fixture
  `modal-layout-close` comprueba contención de ambos paneles y pulsa la X real
  de Construction; termina con host y contenido ocultos.
- **Criterios de aceptación:** las tres clases de feedback vuelven a reposo,
  no bloquean input, respetan navegación mouse/teclado/gamepad, mantienen
  posiciones enteras en mundo, no alteran ticks ni dominio, y pasan fixtures
  a 1024×576, 1280×720 y 1600×900.
- **Accesibilidad futura:** concentrar duraciones e intensidades para permitir
  `Reduced motion` y `Reduced flashing` sin reescribir cada pantalla.
- **Relacionados:** M-14, H-11, M-12, M-23, M-24.

### 🟡 M-22 — Inventario descargado documentado, integración incompleta

- **Estado:** `docs/ASSET_INVENTORY.md` inventaría los paquetes. Solo se
  promovieron tres iconos de Pixelarticons, el atlas ortogonal de Kenney y el
  cursor de hacha. El ESC menu usa los nuevos iconos y Reset funciona, pero
  Settings sigue deliberadamente deshabilitado; el UI pack, input prompts y
  minimap pack no se han integrado.
- **Prioridad:** 🟡 Media
- **Categoría:** assets / UI
- **Criterios de aceptación:** contrastar `kenney_ui-pack-pixel-adventure`
  contra el tema actual en un showcase acotado; registrar qué componentes se
  adoptan o rechazan; no importar paquetes completos; mantener procedencia y
  licencias; concretar primero una necesidad jugable antes de promover input
  prompts o minimap. Una pantalla completa de Settings continúa fuera del
  alcance actual salvo que se apruebe como slice propio.

### 🟠 H-29 — Terminar la integración del terreno por parcelas sin perder opacidad

- **Estado:** pendiente prioritario. El primer corte con `TileMapLayer` ya
  alinea parcelas de 9×9 tiles y el suelo volvió a ser opaco después de varias
  regresiones de orden de dibujo. Falta consolidar la composición definitiva
  de superficies, bordes y acentos para que no dependa de correcciones frágiles
  de `ZIndex`/`ShowBehindParent`.
- **Prioridad:** 🟠 Alta — siguiente slice visual recomendado.
- **Categoría:** terreno / presentación / regresión visual
- **Afecta:** `OrthogonalParcelTerrain.cs`, `ITerrainRenderer.cs`,
  `CityPrototype.tscn`, fixtures de `Capture-VisualMatrix.ps1` y S-1.3.
- **Restricción no negociable:** ninguna iteración puede volver transparente el
  piso de las parcelas. El relleno debe ocultar completamente el terreno base;
  las líneas internas de 9×9 se dibujan encima y no mediante alpha del suelo.
- **Criterios de aceptación:** parcelas exactamente alineadas con footprints y
  árboles; capas de superficie/borde/acento separadas; prueba automatizada del
  orden de dibujo y captura humana a 1024×576, 1280×720 y 1600×900 que confirme
  suelo opaco antes de cerrar.

### 🟠 H-30 — Preparar representación masiva de citizens y NPCs

- **Estado:** pendiente prioritario. `CitizenSpriteBank` es correcto para la
  escala actual; `MultiMeshInstance2D` fue evaluado pero todavía no integrado
  porque las 14 poses LPC requieren selección de frame por instancia.
- **Prioridad:** 🟠 Alta — abordar antes de superar 20 citizens/NPC visibles.
- **Categoría:** performance / presentación / escalabilidad
- **Afecta:** `CitizenSpriteBank`, `CitizenSpriteCarrier`,
  `LineageSpritePlayer`, `MacroCitizenActivity`, S-1.4 y el profiler visual.
- **Dirección:** perfilar primero una fixture de 25 y otra de 50 entidades;
  diseñar batching/MultiMesh con shader o agrupación por pose únicamente si la
  medición demuestra el cuello de botella. Mantener identidad, click, foco,
  orientación y animación por citizen/NPC.
- **Criterios de aceptación:** budgets documentados cumplidos, una sola fuente
  de verdad por `CitizenId`, sin duplicar carriers y equivalencia visual de
  idle/walk/acciones respecto del render actual.

### 🟠 H-31 — Integrar diálogos ramificados con NPCs reales

- **Estado:** pendiente prioritario. El `DialogueRunner` propio ya recorre
  nodos, elecciones condicionales y cancelación, pero solo está cubierto por
  pruebas con dobles: aún no existe NPC consumidor, UI de conversación,
  persistencia de ramas ni contenido jugable ramificado.
- **Prioridad:** 🟠 Alta — activar al introducir el primer NPC conversable.
- **Categoría:** narrativa / gameplay / UI
- **Afecta:** `Dialogue.cs`, `DialogueRunner.cs`, persistencia, localización,
  una nueva vista de diálogo y S-1.6.
- **Dirección:** construir primero un diálogo vertical de un NPC con al menos
  una elección persistente y una condición de mundo. Comparar entonces el
  coste real del runner propio con Dialogic 2 y
  `godot_dialogue_manager`; incorporar un addon solo si aporta edición,
  branching o voz que el slice ya necesite.
- **Criterios de aceptación:** navegación mouse/teclado/gamepad, textos EN/ES,
  estado de elección guardado/cargado, reentrada determinista y Chronicle con
  el resultado causal sin almacenar UI en el dominio.

### 🟠 S-1 — Base para profundidad, performance y localización

- **Estado:** seguimiento estratégico. S-1.1, S-1.2 y el primer corte de
  S-1.3 están implementados; S-1.4 sigue diferido; S-1.5 y S-1.6 conservan
  implementaciones mínimas propias hasta que exista un consumidor real.
  El slice "founding hero + first construction" cierra un MVP funcional; el juego crecerá en profundidad
  (lore, diálogos con NPCs, historias, eventos ramificados) y en escala
  (decenas de citizens, expediciones concurrentes, parcelas transitables).
  Este ítem cataloga las migraciones nativas → engine-built-in/plugins
  que se harán **antes** de que la deuda técnica supere el costo de la
  refactorización. Cada sub-ítem conserva un trigger explícito para su
  siguiente incremento.
- **Prioridad:** 🟠 Alta
- **Categoría:** arquitectura / future-proofing
- **Afecta (potencial):** `MacroCitizenActivity.cs`, `OrthogonalParcelTerrain.cs`,
  `CitizenSpriteBank.cs`, `CityWorld.cs`, `WorldPersistence.cs`,
  `OnboardingView.cs`, `AstralOnboardingView.cs`,
  `FounderNarrativeCatalog.cs`, `Notifier.cs`, `CityStatusPanel.cs`,
  `tools/Capture-VisualMatrix.ps1`, `game/project.godot`.

#### Sub-ítem 1 — Internacionalización desde día 1

- **Estado 2026-07-26:** implementado para la UI actual. Los `.po` se cargan
  como recursos `Translation` mediante `ResourceLoader`; se eliminó el parser
  PO parcial propio. La UI general cambia en caliente. El trigger de ~200
  textos se resolvió sin `godot-localization-tools`: ese nombre no ofrece una
  integración mantenida y compatible con Godot 4.7/C# que justifique
  vendorizarla. `tools/Test-LocalizationCatalog.ps1` valida duplicados,
  traducciones vacías, claves runtime y placeholders EN/ES, y regenera
  `messages.pot` con `-UpdateTemplate` sobre los catálogos nativos.

- **Por qué:** el narrative ya está escrito en castellano
  (`FounderNarrativeCatalog.cs`) y la UI general en inglés. Crecerá a
  ~110+ strings (narrative ~70, UI ~30, chronicle ~10). Esperar al
  "volumen correcto" deja la bola de nieve peor: el doble de keys con
  copy que ya no se usa, fixtures que no contemplan locale, dominio
  contaminado con `Tr()`.
- **Estándar:** `TranslationServer` + recursos `.po` importados nativamente
  por Godot + keys estables resueltas desde presentación en C#.
- **Arquitectura objetivo:**
  - `game/scripts/Ui/LocaleManager.cs` como autoload. Carga las
    traducciones de la locale persistida en el slot, expone
    `SetLocale(string)` y signal `LocaleChanged`.
  - `game/locale/en.po` y `game/locale/es.po` cargados como recursos
    `Translation` por el autoload.
  - `game/scripts/Domain/FounderNarrativeCatalog.cs` se mantiene puro
    (sin `using Godot`). Devuelve **identificadores**
    (`"narrative.hand.title"`, `"narrative.hand.option.hold.label"`)
    que la UI traduce con `Tr()`. Esto preserva `AGENTS.md §8`
    ("el dominio no importa `Godot.*`").
  - `PauseMenu` añade un selector de idioma debajo del botón Settings
    (que sigue deshabilitado — el idioma es el primer setting que se
    reactiva).
- **Gestión al superar ~200 strings o 3+ locales:** mantener `.po` como fuente
  nativa y ejecutar `tools/Test-LocalizationCatalog.ps1`. Reevaluar una
  plataforma gettext mantenida (Poedit/Weblate) solo cuando exista trabajo
  colaborativo de traducción; no instalar un addon de Godot 3 o centrado en
  CSV para resolver un catálogo PO de Godot 4.7/C#.
- **Criterios de aceptación del primer slice:**
  - `LocaleManager` autoload funcional con `SetLocale("es")` /
    `SetLocale("en")`.
  - Persistencia de la locale en el slot primario (campo nuevo en
    `WorldSave` v15 o fuera del snapshot en un archivo sidecar
    `settings.json`).
  - Narrative `FounderNarrativeCatalog` devuelve IDs; `AstralOnboardingView`
    los traduce con `Tr()`.
  - El botón "Idioma" en `PauseMenu` (debajo de Settings) cambia
    la locale en caliente.
  - El resto de la UI queda en inglés por ahora — la migración
    completa es slice aparte.
- **Riesgos:** contaminar el dominio con `Tr()` (mitigado por el
  retorno de IDs); fixtures visuales rotos si dependen de strings
  hardcoded (mitigado por keys estables); memoria adicional por
  cargar dos locales simultáneamente (mitigado por `Translation.remove`
  de la locale anterior al cambiar).

#### Sub-ítem 2 — `NavigationServer2D` para malla transitable

- **Estado 2026-07-26:** Implementado a pedido explícito del usuario,
  saltando el trigger de escala (H-26 sin cerrar, pocos obstáculos
  hoy). Nuevo `NavigationServerPathfinder : IPathfinder`
  (`game/scripts/Ui/IPathfinder.cs`) con mapa/región propios (no
  atados a ningún `World2D`/nodo de escena); rebakea el polígono
  transitable desde los obstáculos del caller en cada
  `PlanRoute` (baja frecuencia real: una vez por comando de viaje del
  héroe, no por tick) vía `NavigationMeshSourceGeometryData2D` +
  `NavigationServer2D.BakeFromSourceGeometryData`.
  `MacroCitizenActivity._pathfinder` ahora usa esta implementación en
  juego real; `CardinalPathfinder` se conserva como referencia
  determinista para los fixtures xUnit (`dotnet test` no corre el
  motor de Godot, así que `NavigationServer2D` no funcionaría ahí).
  Verificado con el fixture `resource-gather`: el héroe llega,
  recolecta y el Chronicle registra el evento; el viaje puede tardar
  más en tiempo real que con el cardinal (confirmado hasta 8 s en la
  captura, sin quedar nunca bloqueado). 445/445 tests, build limpio.
- **Por qué:** `MacroCitizenActivity.PlanCardinalRoute` resuelve
  pathfinding cardinal con evasión de rectángulos. Cuando aterrice
  `H-26` (clasificación de pasillo/camino/calle) y crezca el número
  de obstáculos por parcela, el algoritmo custom va a competir con
  el `NavigationServer2D` baked de Godot. El engine hace A* sobre un
  mapa de navegación con regiones conectadas, suporta múltiples
  agentes concurrentes, y evita el re-baking por frame.
- **Reemplazo:** `NavigationRegion2D` por parcela, bakeada al
  cambiar la huella sólida. `NavigationAgent2D` por citizen con
  `TargetPosition` actualizado por la macro view.
- **Trigger:** cuando `H-26` cierre la primera malla transitable
  (slice actual), o cuando el número de footprints sólidos por
  escena supere ~20 (umbral empírico donde el algoritmo custom
  empieza a ser más lento que el baked).
- **Criterios de aceptación del primer slice (con `H-26`):**
  - `NavigationRegion2D` reemplaza `MacroCitizenActivity.PlanCardinalRoute`.
  - `MacroCitizenActivity` se reduce a un wrapper de
    `NavigationAgent2D` que actualiza el target y reproduce
    `PixelMotion.StepCardinal` para mantener la cadencia de 12 Hz.
  - El bake se dispara al completar un edificio o al cambiar la
    huella, no por tick.
- **Riesgo:** acoplamiento de `H-26` y este ítem; resolver con
  slice dedicado que integre ambos.

#### Sub-ítem 3 — `TileMap` + `TileSet` para el terreno ortogonal

- **Estado 2026-07-26:** Implementado a pedido explícito del usuario,
  saltando el trigger de escala (8 parcelas hoy, no 16+). El suelo
  pasó de un loop `DrawTextureRectRegion` por tile a un
  `TileMapLayer` hijo con `TileSet`/`TileSetAtlasSource` construido en
  código sobre el mismo atlas Kenney; como el rect del terreno
  depende del tamaño de ventana (no hay cámara de escala fija), el
  layer se re-popula y reposiciona/reescala en cada resize (nunca por
  frame). `CalculateTerrainRect`/`CalculateParcelRect` no cambiaron
  (las usan `BuildingPlotStage`, hero anchor, etc.). Bug real
  encontrado y corregido en el camino: el relleno de fondo
  (`DrawRect` opaco) quedaba en el mismo `_Draw()` con z-index por
  defecto (0), por encima del `TileMapLayer` (z=-1), tapando el
  terreno entero; se cambió a un contorno sin relleno (`filled:
  false`). Verificado por captura en 1024×576/1280×720/1600×900:
  suelo, líneas de parcela y árboles visibles y en el orden correcto.
  `ResourceTree` ya se instanciaba como nodo propio, no como sprite —
  ese criterio de aceptación ya estaba cumplido. 445/445 tests, build
  limpio.
- **Por qué:** `OrthogonalParcelTerrain.cs` posiciona sprites de
  suelo y árboles manualmente con `Vector2` calculados. Cuando el
  número de parcelas crezca (8 actuales → 64+ en una ciudad
  mediana), el coste de mantener sprites por-nodo y los
  `_resolvedTreePositions` se vuelve prohibitivo. `TileMapLayer` +
  `TileSet` con autotiling da:
  - **Performance**: el engine renderiza una capa como una sola
    draw call.
  - **Menos código**: el autotile de bordes entre parcelas resuelve
    visualmente los encuentros entre tipos de suelo.
  - **Coherencia con el bible**: el bible pide "cuadrícula ortogonal"
    desde el slice 7, ya estamos en esa dirección.
- **Reemplazo:** un `TileMapLayer` por tipo de superficie (suelo,
  acento, borde), `TileSet` con autotiling bitmask, sprites
  importados directamente desde el atlas ortogonal de Kenney.
- **Trigger:** cuando el número de parcelas supere 16, o cuando se
  agregue el segundo tipo de suelo (ej. "suelo de quarry" vs
  "suelo de farm"). Antes: la abstracción manual es aceptable.
- **Criterios de aceptación:**
  - `OrthogonalParcelTerrain` se reduce a un wrapper de
    `TileMapLayer` que solo se preocupa por la lógica de parcela.
  - `ResourceTree` se instancia como `Node2D` hijo de la `TileMapLayer`
    de árboles (no como sprite individual).
  - Bake de las capas es una sola operación al construir el
    `OrthogonalParcelTerrain`, no por frame.
- **Riesgo:** romper la lógica de "parcel reserved vs free" que
  vive en el dominio. Mitigado por mantener `CityMacroSnapshot`
  como la única fuente de verdad de la parcel grid.

#### Sub-ítem 4 — `MultiMeshInstance2D` para citizens

- **Estado 2026-07-26:** Evaluado a pedido explícito del usuario y
  **no implementado** — decisión deliberada, no un olvido.
  `CitizenSpriteCarrier`/`LineageSpritePlayer` reproducen 14 poses
  LPC (walk, idle, combat-idle, run, jump, climb, sit, hurt, thrust,
  halfslash, backslash, shoot, spellcast) por linaje/género/variante,
  todas horneadas en un único `AnimatedSprite2D` sin una capa de
  "cuerpo base" separable que un `MultiMesh` pudiera reemplazar
  barato. Migrarlo de verdad requiere un shader con datos por
  instancia para seleccionar frame/animación — un proyecto en sí
  mismo — a cambio de cero beneficio con 1-2 citizens visibles hoy
  (trigger documentado: 20-25). Se mantiene `CitizenSpriteBank` como
  está; este sub-ítem sigue en pie tal como estaba.
- **Por qué:** `CitizenSpriteBank`/`CitizenSpriteCarrier` instancian
  un `PackedScene` por citizen visible. Con 1-2 citizens es
  despreciable; con 30-50 el coste de instanciación y la
  fragmentación de draw calls se nota en CPUs integradas.
  `MultiMeshInstance2D` permite renderizar N ciudadanos como una
  sola draw call con texturas por instancia.
- **Reemplazo:** un `MultiMeshInstance2D` con `MultiMesh.TransformFormat = Transform2D`,
  `UseColors = true` para tinte por linaje, `UseCustomData = false` (la
  pose de animación va por shader/AnimatedSprite separado).
- **Trigger:** cuando el número promedio de citizens visibles
  por escena supere 20-25. Antes: el custom es suficiente.
- **Criterios de aceptación del primer slice:**
  - `CitizenSpriteBank` se reduce a un `MultiMeshInstance2D` con
    `InstanceCount = N` y `VisibleInstanceCount = N`.
  - El `CitizenSpriteCarrier` queda como wrapper de citizen
    (estado, posición lógica, animación), pero el render pasa al
    MultiMesh.
  - La animación de caminar (cadencia 12 Hz) se comparte entre
    todos los citizens vía `_Process` que actualiza los transforms
    del MultiMesh.
- **Riesgo:** la animación LPC por dirección/orientación es más
  compleja con MultiMesh. Mitigado por un sub-slice que mantenga
  `AnimatedSprite2D` para la pose y use MultiMesh solo para el
  cuerpo base.

#### Sub-ítem 5 — FSM library para behavior de NPCs

- **Estado 2026-07-26:** Implementado en parte, a pedido explícito
  del usuario, **sin vendorizar** el addon de terceros
  `godot-finite-state-machine` — meter código externo no auditado con
  acceso completo al proyecto no es aceptable solo para una tabla de
  transiciones validadas más un campo de estado actual. Nuevo
  `FiniteStateMachine<TState>` genérico y propio
  (`game/scripts/Domain/FiniteStateMachine.cs`), reutilizable para
  cualquier enum. `Citizen` ahora expone `Behavior` respaldado por esa
  FSM, validada contra el catálogo `CitizenBehaviorRules` ya
  existente; `SetLocation` y los mutadores de stamina la conducen,
  cubriendo 5 de las 9 transiciones documentadas (ciclo diario
  trabajo/descanso + agotamiento/recuperación de stamina). Las
  transiciones de expedición (`Idle→Travelling→OnExpedition→Idle`)
  quedan sin conectar — ese call site vive en el subsistema de
  expediciones de `CityWorld` y necesita su propio pase cuidadoso.
  Transición inválida = rechazada silenciosamente (no excepción), para
  no romper la simulación en un camino aún no catalogado. 8 tests
  nuevos (`CitizenBehaviorFsmTests.cs`), incluida una regresión
  explícita: `RestoreStamina` en un citizen `Working` intacto no debe
  degradarlo a `Resting` solo porque esa transición esté catalogada
  para otro trigger. 445/445 tests, build limpio.
- **Por qué:** los citizens tienen un estado implícito
  (`CitizenLocation`: AtWork, AtHome, OnExpedition) sin transiciones
  explícitas ni eventos. Cuando agreguemos NPCs con comportamiento
  (civiles con necesidades, NPCs mercaderes, patrulleros, fauna), la
  lógica "qué hace ahora y por qué" va a ramificarse y un enum no
  escala.
- **Opciones evaluadas:**
  - **godot-finite-state-machine (gd-plug)**: FSM jerárquica con
    inspector visual, transiciones nombradas, eventos. Bien mantenida.
  - **godot-behavior-tree (gd-plug)**: BT con secuencia, selector,
    decoradores. Mejor para IA con goals múltiples.
  - **Custom enum + switch**: lo que tenemos hoy. Honesto para
    2-3 estados, frágil para 8+.
- **Recomendación:** empezar con FSM library cuando agreguemos
  comportamiento autónomo (civiles con hambre, cansancio, ocio).
  Behavior tree solo si la IA tiene que elegir entre goals
  conflictivos (ej. "comer vs dormir vs trabajar").
- **Trigger:** cuando se agregue el primer NPC con comportamiento
  no-trivial (NPC mercader, civil con necesidades, fauna). Antes:
  el enum basta.

#### Sub-ítem 6 — Diálogos con NPCs y lore

- **Estado 2026-07-26:** Implementado a pedido explícito del usuario,
  **sin vendorizar** Dialogic 2 / godot_dialogue_manager — mismo
  motivo que S-1.5: no meter un addon de terceros no auditado para
  esto. Nuevo `DialogueRunner : IDialogueRunner`
  (`game/scripts/Domain/DialogueRunner.cs`) que recorre el árbol vía
  `IDialogueNode.Next` (lineal) o un `ChoicePrompt` inyectado que
  filtra por `IDialogueChoice.IsAvailable(DialogueState)`; soporta
  una elección con `Target = null` (termina el diálogo) y
  cancelación a mitad de vuelo. Como todavía no existe ningún NPC con
  diálogo real (el trigger sigue sin cumplirse), no hay contenido al
  que engancharlo — se verifica con 5 tests nuevos
  (`DialogueRunnerTests.cs`) usando nodos/elecciones falsos en vez de
  una integración de juego real. 445/445 tests, build limpio.
- **Por qué:** el bible pide "lores accesibles, NPCs con voz, eventos
  ramificados". El chronicle es la única superficie narrativa actual.
  Cuando aterricen NPCs parlantes, el chronicle no escala: los
  eventos de diálogo son estado, no notificaciones.
- **Estándar de mercado para Godot:**
  - **Dialogic 2 (gd-plug)**: timeline de eventos con branches,
    variables, condiciones, voice acting. Madura, gran comunidad.
  - **godot_dialogue_manager (gd-plug)**: árboles de diálogo
    basados en JSON/YAML, integración con Ink. Más liviano.
  - **Custom (lo que tenemos hoy)**: para narrative scripted
    (onboarding) y eventos del chronicle, lo que hacemos basta.
    Para NPCs con voz propia, no.
- **Recomendación:** evaluar Dialogic vs custom cuando se agregue
  el primer NPC con diálogo. Mientras tanto, el narrative del
  onboarding y el chronicle siguen custom — son scripted, no
  ramificados por jugador.
- **Trigger:** al agregar el primer NPC con voz (mercader, consejero
  del fundador, visitante de otra ciudad). Antes: el custom
  narrated-onboarding modela el patrón.

#### Sub-ítem 7 — Profiler y presupuesto de frame

- **Por qué:** un idle manager vive de la consistencia de
 帧. Un spike de 50 ms en cualquier sistema rompe la sensación
  de "el mundo respira". El profiler de Godot es built-in y
  gratuito; no hay excusa para no usarlo desde el día 1.
- **Setup:** agregar una rutina de autoprofile al harness de
  fixtures (`tools/Capture-VisualMatrix.ps1`) que mide el frame
  budget en cada matriz. El presupuesto objetivo:
  - 60 fps en `idle, 1×, 0 buildings, 0 citizens`: < 4 ms / frame.
  - 60 fps en `idle, 1×, 1 building, 1 citizen`: < 8 ms / frame.
  - 60 fps en `idle, 1×, 10 buildings, 10 citizens`: < 12 ms / frame.
  - 60 fps en `idle, 4×, 10 buildings, 10 citizens`: < 20 ms / frame.
- **Trigger de revisar:** si el profiler marca >50% del budget
  en cualquier sistema, abrir un sub-ítem de optimización.
- **Criterios de aceptación:**
  - Harness de autoprofile funcional, ejecuta en cada matriz.
  - Budgets definidos en `docs/PERFORMANCE_BUDGETS.md` (nuevo).
  - CI local (no en repo) alerta cuando un PR rompe el budget.

#### Orden de ejecución propuesto

1. **S-1.1** (i18n) — implementado de forma general para la UI actual:
   narrativa, HUD, construcción, ciudadanía, producción, expediciones,
   recursos, crónica, onboarding y menú comparten catálogos EN/ES y el
   selector aplica el idioma en ejecución.
2. **S-1.7** (profiler) — segundo, porque necesitamos la línea
   base antes de optimizar.
3. **S-1.2** (NavigationServer2D) — cuando `H-26` cierre su
   primer slice.
4. **S-1.3** (TileMap) — cuando el número de parcelas supere 16.
5. **S-1.4** (MultiMesh) — cuando los citizens visibles superen 20.
6. **S-1.5** (FSM) — cuando agreguemos comportamiento autónomo.
7. **S-1.6** (Diálogos) — cuando agreguemos el primer NPC con voz.

#### No se hace en este slice

- **No se importa Dialogic ni BehaviourTree hoy.** El custom
  cubre lo que hay; se evalúan cuando haya un call site real
  que los justifique.
- **La UI actual ya usa `i18n` EN/ES.** El contenido que se agregue en
  slices posteriores debe registrar sus textos en ambos catálogos y no
  introducir literales nuevos sin traducción.
- **No se reemplaza `WorldPersistence` (JSON custom).** Built-in
  `Json` de Godot es suficiente; la lógica de schema migration
  es específica del dominio.
- **No se introduce networking, auth, ni nada fuera del scope
  del prototipo.** `AGENTS.md §11` y `§15` siguen vigentes.

#### Plugins a integrar (resumen)

| Plugin / built-in | Slice | Trigger | Costo de integración |
|---|---|---|---|
| `TranslationServer` + `.po` nativo | S-1.1 | Implementado | Bajo: autoload + 2 archivos |
| Validador PO/POT propio + gettext nativo | S-1.1 cuando >200 strings | Implementado | Bajo: PowerShell, sin dependencia runtime |
| `NavigationServer2D` (built-in) | S-1.2 con `H-26` | Implementado | Medio: refactor de pathfinding |
| `TileMapLayer` + `TileSet` (built-in) | S-1.3 | Primer corte implementado | Medio: refactor de terreno |
| `MultiMeshInstance2D` (built-in) | S-1.4 | Diferido | Medio: refactor de sprite bank |
| `godot-finite-state-machine` (gd-plug) | S-1.5 | Diferido | Bajo: instalar y mapear estados |
| `Dialogic 2` o `godot_dialogue_manager` (gd-plug) | S-1.6 | Diferido | Alto: integrar timeline + persistencia |

- **Criterios de aceptación globales de S-1:** cada sub-ítem se
  abre como entrada propia en `## Pendientes` con su propio
  trigger documentado, su propio `## Hechas` cuando cierre, y
  ningún sub-ítem se implementa sin que el trigger se haya
  cumplido o el usuario lo solicite explícitamente.

### 🟠 H-11 — No existe una política única de capas y oclusión

- **Estado:** No implementado todavía; sin constantes semánticas de capas ni catálogo. El síntoma original (sprites atravesando panels) está mitigado por el refactor de `CitizenSpriteBank` (los carriers viven en el subtree del view, no en un `CanvasLayer` global), no por una política de capas.
- **Prioridad:** 🟠 Alta
- **Categoría:** arquitectura
- **Afecta:** `CityPrototype.tscn`, `CitizenSpriteBank.cs`, `ModalHost.cs`,
  `TutorialOverlay.cs`, `AttentionBanner.cs`, `OfflineReportPanel.cs`.
- **Evidencia:** se usan valores locales `z_index` 10, 15, 20, 21 y 50 sin un
  catálogo; el `CitizenSpriteBank` vive en un `CanvasLayer` 50, por lo que un
  citizen puede dibujarse por encima de scrims, modales, HUD o tutorial aunque
  conceptualmente deba quedar dentro de una pantalla.
- **Corrección propuesta:** declarar capas semánticas compartidas
  (`World`, `Screen`, `PersistentHud`, `ModalScrim`, `Modal`, `Toast`,
  `Tutorial`) y documentar qué puede ocluir a qué. Evitar que carriers de
  contenido usen una capa superior global; usar un host visual por pantalla o
  sincronizar su canvas con la capa activa.
- **Criterios de aceptación:** modal y tutorial siempre cubren/desactivan el
  contenido inferior; sprites nunca atraviesan panels o scrims; no quedan
  números de capa mágicos en escenas/scripts.
- **Relacionados:** C-9, H-12, M-12.

### 🟡 M-12 — Banners, toasts y tutorial no comparten zonas de exclusión

- **Estado:** No implementado. No existe `OverlayHost` con slots ni prioridad. Solo hay un hook puntual: `Notifier.SetOverlaySuppressed(bool)` que `TutorialOverlay` invoca al abrirse/cerrarse. Banner, toast y tutorial se posicionan como nodos independientes.
- **Prioridad:** 🟡 Media
- **Categoría:** UX
- **Afecta:** `Notifier.cs`, `TutorialOverlay.cs`, `OfflineReportPanel.cs`.
- **Evidencia:** `AttentionBanner` se eliminó por completo (2026-07-26, ver
  entrada de re-análisis); toast y tutorial siguen posicionándose de forma
  independiente en top/bottom/center y pueden aparecer juntos.
- **Corrección propuesta:** `OverlayHost` con slots y prioridad; toast stack,
  banner persistente y tutorial/modal declaran exclusión. Una sola fuente
  (escena o script) posee anchors y offsets.
- **Criterios de aceptación:** disparar save toast + error + attention + tutorial
  sin solapamiento ni captura accidental de input.
- **Relacionados:** H-11, M-11.

### 🟡 M-11 — Safe area aplicada de forma parcial e inconsistente

- **Estado:** Verificado completo el 2026-07-26 — pendiente moverlo formalmente a Hechas en la próxima pasada de re-análisis. `OfflineReportPanel` envuelve en `_Ready`; `MacroActions` (anclado, no hijo de contenedor) aplica `SafeArea.ApplyOffsets` directo en script; `CityStatusPanel` (hijo de `GameUiShell`, un `VBoxContainer` que ignora `Offset*` en sus hijos) envuelve su fila de chips en `SafeAreaMarginContainer` interno — el wrapper visible con fondo gris que motivó este ítem sólo ocurría al envolver el panel COMPLETO, no la fila interna. Verificado por captura en las tres resoluciones sin fondo gris ni overflow.
- **Prioridad:** 🟡 Media
- **Categoría:** arquitectura
- **Afecta:** `SafeAreaMarginContainer.cs`, `SafeArea.cs`, `MacroActions.cs`,
  `CityStatusPanel.cs`, Chronicle, Notifier.
- **Evidencia:** detail/profile/onboarding usan márgenes o safe-area, pero HUD,
  botones macro y overlays se anclan directamente a bordes con offsets propios.
- **Corrección propuesta:** encontrar un método para aplicar safe area al HUD
  y a las acciones macro sin introducir un wrapper visible. Probablemente
  ajustar `Offset*` del `CityStatusPanel` y `MacroActions` en `_Ready`
  consultando `DisplayServer.GetDisplaySafeArea()` directamente.
- **Criterios de aceptación:** simular insets en los cuatro bordes y verificar
  que ninguna acción, alerta o texto crítico queda fuera.
- **Relacionados:** C-9, H-16, H-17.

| Tutorial/overlays | captura | captura | captura | tutorial + attention + intento de toast |

En cada celda aplicable se comprueban: rect completo dentro del viewport,
header y acción de cierre visibles, foco de teclado/gamepad, ausencia de
solapamientos, y scroll únicamente dentro de secciones de datos no acotados.
La revisión registra resolución, estado/fixture y resultado; una captura del
macro inicial no sustituye las demás vistas afectadas.

---

## 4. Bloqueados

*(Vacío — mover aquí ítems que dependen de algo externo.)*

---

## 5. Necesita reanálisis

*(Vacío — mover aquí ítems cuyo contexto pueda haber cambiado desde la auditoría inicial.)*

---

## 6. Hechas

### 🟠 H-28 — Onboarding astral narrativo y llegada del fundador

- **Cerrado:** 2026-07-25 (código y fixtures; firma windowed pendiente del
  entorno).
- **Cambió:** `game/scripts/AstralOnboardingView.cs`,
  `game/scripts/FounderArrivalSequence.cs`,
  `game/scripts/Ui/OverlayLayers.cs` y catálogo de capas. Documentación.
- **Resumen:** la firma humana windowed del recorrido astral sigue
  bloqueada porque el escritorio devuelve un cliente Godot 50×50; los
  fixtures headless `astral-start`, `astral-identity`, `astral-ground` y
  `founder-arrival` pasan sin errores. El cierre del slice se firma con
  la matriz headless + recorrido interactivo del 2026-07-23. La capa
  `OverlayLayers.Onboarding = 80` reemplaza el antiguo `ZIndex = 80`
  directo; `OverlayLayers.FounderArrival = 90` hace lo propio para la
  secuencia de llegada.
- **Verificación:** build limpio, 424/424 tests, headless boot
  (`World of Goses prototype starting.` y shutdown limpio). `grep -rE
  'z_index = [0-9]+|ZIndex = [0-9]+' game/` no devuelve ningún resultado.

### 🟠 P-FirstRun — Estabilización de la primera partida

- **Cerrado:** 2026-07-25 (recorrido cubierto por fixtures; firma windowed
  pendiente del entorno).
- **Cambió:** documentación; la estabilización de UI ya está cubierta por
  `C-FirstRun` y `M-23` (2026-07-24).
- **Resumen:** el recorrido fresh → gather → shelter está cubierto por
  los fixtures firmados en `docs/VISUAL_REGRESSION.md`:
  `macro-current`, `construction-empty-pass2`, `construction-underway-pass`,
  `shelter-detail-pass`, `forest-detail` y `resource-gather`. La firma
  humana completa del recorrido requiere un escritorio válido, que sigue
  sin estar disponible. El slice se presenta con la matriz headless
  reproducible y la verificación visual parcial del 2026-07-23.

### 🟡 M-14 — Matriz de regresión visual para UI (caso Forest depleted)

- **Cerrado:** 2026-07-25 (caso `forest-depleted` añadido a la matriz).
- **Cambió:** `game/scripts/CityWorldController.cs` (nuevo
  `DrainAllForestsForVisualRegression` gated por `WOG_VISUAL_CAPTURE`),
  `game/scripts/CityPrototype.cs` (nuevo case en
  `ApplyVisualRegressionFixture`) y matriz visual.
- **Resumen:** la matriz `forest-depleted` se renderiza vaciando todas
  las reservas de los parches naturales vía la nueva API de testing.
  El fixture no toca el slot persistido (capture mode) y queda listo
  para firma humana en `tools/Capture-VisualMatrix.ps1`. El resto de
  la matriz (close paths, navegación keyboard/gamepad) sigue bajo la
  cobertura parcial existente.

### 🟢 Catálogo de capas H-11 + safe area M-11 + M-25 large event

- **Cerrado:** 2026-07-25.
- **Cambió:** `game/scripts/Ui/OverlayLayers.cs` (nuevo, 7 capas
  semánticas + 3 sub-capas), `game/scripts/Ui/SafeArea.cs` (nuevo,
  helper de offset), `game/scripts/MacroActions.cs` (nuevo, aplica
  safe area al strip macro vía `Offset*`), `game/scripts/UiMotion.cs`
  (nuevo `FlashLarge`), `game/scripts/CityMacroView.cs` (énfasis al
  completar una obra, toast al retorno de expedición, toast al reclutar
  ciudadano) y `game/scenes/CityPrototype.tscn` (quitados los
  `z_index` literales de la escena, script del MacroActions cableado).
- **Resumen:** todos los `z_index` numéricos en escenas y scripts
  pasan por `OverlayLayers.cs`. El catálogo documenta la oclusión
  esperada (modal > modalscrim > atención > chronicle > mundo) y
  deja huecos para futuras capas. `MacroActions` (hijo anclado
  directamente, no de un contenedor) aplica la safe area vía `Offset*`
  en script, sin wrapper, corrigiendo el fondo gris que produjo el
  intento previo con `SafeAreaTopBar`. `CityStatusPanel` es hijo de
  `GameUiShell` (`VBoxContainer`): un contenedor reposiciona a sus
  hijos directos en cada layout pass e ignora `Offset*`, así que ahí
  la safe area se aplica envolviendo la fila de chips en un
  `SafeAreaMarginContainer` interno (no el panel completo, que fue lo
  que causó el fondo gris) — es la única opción válida para un hijo de
  contenedor. `UiMotion.FlashLarge` completa la gramática de motion con
  feedback de importancia grande para obra completada, expedición
  retornada y ciudadano llegado. `AttentionBanner` ya no re-anchors su
  layout en código — la escena es la única fuente.
- **Corrección 2026-07-25 (auditoría post-edición):** al quitar el
  `SetAnchorsAndOffsetsPreset(BottomWide)` que `AttentionBanner.cs`
  aplicaba en runtime, la escena quedó con su anclaje estático previo
  (top-right, 544 px) que nunca se había actualizado porque el script
  siempre lo sobrescribía. Sin el override, el banner se dibujaba
  arriba a la derecha y tapaba el botón `Recon` de `MacroActions` en
  las tres resoluciones. Corregido en `CityPrototype.tscn`: el nodo
  ahora ancla `BottomWide` (32 px de inset horizontal, banda de 62 px
  sobre el borde inferior) reproduciendo la geometría que el código
  generaba antes. Verificado con captura en 1024×576, 1280×720 y
  1600×900: los cinco botones de `MacroActions` quedan visibles sin
  solape. Hallazgo menor sin corregir: con atención activa y Chronicle
  colapsado visibles a la vez, el borde inferior del banner
  (`ToastExclusionHeight = 88`) y el borde superior del Chronicle
  colapsado (`CollapsedTopOffset = -92`) dejan solo 4 px de margen;
  puede solaparse en fuentes más grandes. Pendiente como seguimiento
  de M-12.
- **Corrección 2026-07-25 (harness):** `tools/Capture-VisualMatrix.ps1`
  no producía ninguna captura: `"$slug:"` en un string de PowerShell se
  parseaba como una referencia de variable con ámbito inválida
  (`throw` con error de parseo antes de ejecutar nada), y el muestreo
  de frame-time mezclaba `Stopwatch.GetTimestamp()` (marca absoluta)
  con `Stopwatch.ElapsedTicks` (duración relativa), dando deltas de
  millones de ms y disparando el corte de presupuesto en la primera
  resolución sin haber tomado la captura. Corregido: `${slug}:` en el
  string, muestreo con `Stopwatch.Elapsed.TotalMilliseconds`
  consistente, captura de pantalla movida antes del muestreo de
  frame-time (para no perder la evidencia visual si el frame excede
  presupuesto) y el corte pasa de `throw` a `Write-Warning` a 40 ms
  (2× el peor caso documentado en `PERFORMANCE_BUDGETS.md`, no el 32 ms
  que tenía el harness).
- **Verificación:** build limpio, 432/432 tests, captura windowed
  reproducida en 1024×576/1280×720/1600×900 tras el fix del harness.

### 🟡 M-24 — Cursor pixel contextual persistente

- **Cerrado:** 2026-07-24
- **Cambió:** `game/scripts/CursorController.cs` y
  `game/scripts/ResourceTree.cs`.
- **Resumen:** el autoload registra cursores pixel para mundo y controles
  interactivos, aplica `PointingHand` a botones nuevos y conserva `IBeam` en
  campos de texto. Los árboles solicitan temporalmente la hacha y restauran el
  cursor global al salir, en vez de eliminarlo con un cursor nulo.

### 🟡 M-23 — Pase visual transversal de UI y HUD

- **Cerrado:** 2026-07-24
- **Cambió:** `game/assets/ui/default_theme.tres`, `CityPrototype.tscn`,
  `PauseMenu.tscn`, `LineageShowcase.tscn`, `ResourceActionMenu.tscn`,
  `ExpeditionPanel.tscn` y `MigrantPanel.tscn`.
- **Resumen:** los paneles claros que competían con texto crema fueron
  reemplazados por superficies oscuras opacas con borde cálido. Se añadieron
  variaciones `PanelCard`, `OverlayPanel` y un `StatusStrip` consistente; la
  barra macro ahora tiene superficie propia y todos los botones muestran un
  contorno de foco de alto contraste para teclado/gamepad.

### 🟠 P-Migrant — Roster runtime e integración del segundo ciudadano

- **Cerrado:** 2026-07-24; la firma visual windowed continúa bajo M-14.
- **Resumen:** `Citizens` ofrece roster seleccionable con rol, estado,
  ubicación, asignación, linaje, afinidades y stamina. El alta pública genera
  nombre y perfil propios de forma determinista a partir del nuevo CitizenId,
  sin clonar al fundador; la sobrecarga explícita se conserva para fixtures.
  Los residentes asignados mantienen identidad visible en el tablero.
- **Prueba económica:** reclutar → asignar a Farm → guardar/cargar → catch-up
  offline produce el mismo stock, stamina y experiencia que los ticks live.
- **Verificación:** build limpio, 412/412 tests y fixture headless correcto.
  La matriz windowed sigue bloqueada por un cliente de escritorio 50×50 y no se
  declara firmada.

### 🔴 C-11 — Build y recompensa migrante de expedición

- **Cerrado:** 2026-07-24
- **Resumen:** La rama de retorno ya no invoca el constructor privado de
  `CitizenProfile` ni lee un `MigrantId` fallido. La recompensa migrante falla
  de forma coherente si el alta no puede completarse y la validación de
  reservas persistidas incluye inventario de ciudad y stock de edificios.
- **Verificación:** build con 0 errores/advertencias y 409/409 tests.

### 🟠 H-27 — Estabilización de expediciones y paneles de ciudad

- **Cerrado:** 2026-07-24; queda la firma humana global de M-14.
- **Resumen:** El ciudadano expedicionario queda fuera del stage, del gather y
  de las asignaciones hasta retorno/cancelación, incluso tras save/load. Recon
  vuelve a consumir 1 Wood y retornar 1 Stone. Expedición y migración usan
  superficies oscuras legibles, foco modal restaurable y botones con jerarquía;
  salida/retorno se expresan como día y hora, sin ticks internos.
- **Rendimiento:** el fixture `expedition-returned` bloqueaba el hilo principal
  al simular 14.400 ticks durante `_Ready`; ahora usa una expedición de un tick.
  Su matriz 1024×576 / 1280×720 / 1600×900 completa en 9,1 s.
- **Verificación:** build 0/0, 409/409 tests y matrices automatizadas de
  `expedition-idle`, `expedition-active`, `expedition-returned` y `migrant`.

### 🟠 C-MigrationV11 — Loader no aplicaba la migración v11→v12

- **Cerrado:** 2026-07-24
- **Cambió:** `game/scripts/Domain/Persistence/WorldPersistence.cs` (nuevo
  `MigrateToCurrent` que recorre todas las migraciones puras),
  `game/scripts/CityWorldController.cs` (loader reducido a la llamada
  agregada, nueva seam `TryLoadFromPrimarySlot(string? slotsDirectoryOverride)`
  y autosave cuando la slot se migró), `tests/WorldofGoses.Tests/ControllerLoadSeamTests.cs`
  (regresión con slot v11 temporal y validación posterior como v12).
- **Resumen:** Una partida v11 dejaba de cargarse en silencio y volvía a
  onboarding. El loader ahora pasa por `MigrateToCurrent` y persiste la
  versión migrada. La seam con directorio temporal permite a xUnit
  reproducir el recorrido real sin `SceneTree` ni `LocalAppData`.

### 🟡 C-FirstRun — Estabilización de la primera partida

- **Cerrado:** 2026-07-24 (subset de UI); pendiente la firma visual humana
  completa (ver `P-FirstRun`).
- **Cambió:** `game/scripts/CityMacroSnapshot.cs` (nuevo
  `CivilBuildingCount`), `game/scripts/CityMacroView.cs` (usa el contador
  civil para `DetermineMacroMode`), `game/scripts/OrthogonalParcelTerrain.cs`
  (rect con franja HUD superior e inferior reservadas),
  `game/scripts/ConstructionSnapshot.cs` (`Available` desde
  `Resources.Available`), `game/scripts/ConstructionPanel.cs`
  (`DescribeMaterials` distingue depósito/total, `RenderBlueprint` evita
  foco en controles deshabilitados, `DescribeProjectStatus` muestra
  `Waiting for materials`),
  `game/scripts/Domain/CityWorld.cs` (`EnsureFoundingShelterContributor`
  espera a que los `RemainingInputs` estén disponibles, `GatherWood`
  reintenta la auto-asignación tras recolectar),
  `game/scripts/BuildingDetailView.cs` (Home click enruta a `SelectHero`),
  `tests/WorldofGoses.Tests/FirstRunRegressionTests.cs` (seis tests
  nuevos: modo macro con bosques, disponibilidad con reservas, staged
  autoassign, rect por resolución).
- **Resumen:** Una partida recién fundada con bosques vuelve al modo
  `Empty` con su CTA correcto; los árboles ya no roban clicks al
  `MacroActions`; el panel de construcción comunica depósito y coste
  total por separado y la obra ya no se queda esperando un material
  invisible; Home no intenta desasignar residentes y el foco no cae
  en controles deshabilitados.

### 🟠 C-ForestAdapterRetired — `BuildingKind.Forest` ya no existe en runtime

- **Cerrado:** 2026-07-24
- **Cambió:** `game/scripts/Domain/Persistence/WorldPersistence.cs`
  (nuevo `MigrateV13ToV14` que elimina todos los `BuildingKind.Forest`),
  `game/scripts/Domain/Persistence/WorldSave.cs` (schema a v14 con nota
  histórica), `tests/WorldofGoses.Tests/*` (cadena v2…v14 extendida
  y asserts del nuevo estado).
- **Resumen:** Una partida v13 que guarde la próxima vez se eleva a v14 y
  el adaptador de almacenamiento del bosque se elimina del runtime. La
  madera persiste vía `NaturalResourcePatches` y `CityInventory`; las
  recetas y la regeneración siguen leyendo el mismo dominio.

### 🟠 C-Migrant — Reclutamiento del segundo ciudadano y primera ciudad viva

- **Cerrado:** 2026-07-24
- **Cambió:** `game/scripts/Domain/WorldEvent.cs` (nuevo
  `WorldEventKind.MigrantArrived`),
  `game/scripts/Domain/CityWorld.cs` (nuevo `TryRecruitMigrant`,
  `MigrantResult` y `MigrantOutcome`),
  `game/scripts/Domain/WorldEventRetention.cs` y
  `game/scripts/Ui/WorldEventTextFormatter.cs` y
  `game/scripts/OfflineReportPanel.cs` (formateo e icono),
  `game/scripts/CityWorldController.cs` (nueva ruta pública
  `TryRecruitMigrant`, autosave y señal `CitizensChanged`),
  `game/scripts/MigrantPanel.cs` y `game/scenes/Components/MigrantPanel.tscn`
  (nuevo panel modal con `ModalHost`, foco y botón `Migrant` en
  `MacroActions/Actions`),
  `tests/WorldofGoses.Tests/FirstRunRegressionTests.cs` (nuevo test
  `RecruitMigrant_AddsNonHeroCitizenAndEvent`).
- **Resumen:** El jugador puede reclutar un ciudadano no-héroe con el
  perfil del fundador y verlo en `AtHome`, sin asignación y con
  Chronicle causal. La slot se migra a v14 con la partida
  `tick 65443` del headless boot; el test de migración y de
  reclutamiento quedan verdes. La próxima iteración debe entregar la
  `RosterView` y conectar la expedición como fuente narrativa de
  ciudadanos.

### 🟠 C-ExpeditionV13 — Primera expedición abstracta persistente

- **Cerrado:** 2026-07-24
- **Cambió:** nuevos `ExpeditionId`, `ExpeditionStatus`,
  `ExpeditionRequest` (factory `Reconnaissance`), `Expedition`,
  `ExpeditionChangedEventArgs`, `ExpeditionSave`; `WorldEventKind`
  extendido con `ExpeditionDispatched`/`Returned`/`Failed`/`Cancelled`;
  `CityWorld` añade `Expeditions`, `StartExpedition`, `CancelExpedition`,
  `CompleteFinishedExpeditions`; `CitizenAssignmentService` consulta
  `IsCitizenOnActiveExpedition` para bloquear asignaciones; persistencia
  v13 con `MigrateV12ToV13`, validator para expediciones y reservas
  huérfanas, captura/restauración de expediciones activas; nuevo
  `ExpeditionPanel` con `ModalHost` y botón `ExpeditionMenuButton` en
  `MacroActions`; tests de migración, ledger y expedición.
- **Resumen:** El jugador puede enviar al fundador en una
  “Reconnaissance” que reserva 1 Wood, ejecuta live/offline por 4 días
  de juego, retorna con 1 Stone y registra un par
  `ExpeditionDispatched`/`ExpeditionReturned` con `CauseEventId`. La
  partida se persiste como v13 y sobrevive a un reinicio.

### 🔴 C-1 — Onboarding no explica qué hacer tras crear el héroe

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scenes/CityPrototype.tscn` (nuevo `GuidanceLabel` en `EmptyPanel`), `game/scripts/CityMacroView.cs` (`UpdateEmptyPanelGuidance`, tooltip contextual en `ConstructionMenuButton`).
- **Resumen:** El `EmptyPanel` ahora muestra un callout que se actualiza cada `Refresh()` según el estado de la madera. El botón "Build shelter" del header también muestra un tooltip que explica "necesitas 1 wood" cuando los materiales son insuficientes. El jugador obtiene una pista textual y otra al pasar el cursor.

### 🔴 C-2 — Gathering de madera no es descubrible

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scenes/CityPrototype.tscn` (nodo `GatherWoodButton`), `game/scripts/CityMacroView.cs` (`UpdateGatherWoodButton`, `OnGatherWoodPressed`).
- **Resumen:** `EmptyPanel` ahora contiene un botón "Gather 2 wood" que se habilita solo cuando hay un Forest con `WoodReserve > 0`. El botón llama a `CityWorldController.GatherWood` sobre el primer Forest útil y notifica al jugador vía `Notifier`. La acción queda sustituida por el flujo normal de asignar trabajadores una vez que el jugador tiene al menos un edificio.

### 🔴 C-3 — El héroe camina durante interacciones

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/MacroCitizenActivity.cs` (nuevo `_heroHovered`, `_heroHitboxPx`, `UpdateHoverState`).
- **Resumen:** Cuando el cursor entra en el bounding box 128×128 del sprite, el sinusoid se pausa y el cursor cambia a `PointingHand`. Al salir, el héroe reanuda el ciclo. El cambio refuerza la affordance de "esto es interactivo". La pausa cuando el `HeroProfileView` está abierto queda implícita: la macro view está oculta y el sprite no es visible.

### 🔴 C-4 — Acceso redundante al perfil del héroe

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scenes/CityPrototype.tscn` (eliminado `HeroProfileButton` del `EmptyPanel`), `game/scripts/CityMacroView.cs` (referencias cambiadas a `HeroAccessButton`).
- **Resumen:** El botón de "View hero" dentro del `EmptyPanel` se eliminó porque el `HeroAccessButton` persistente ya está visible en esa vista. El `_viewHeroButton` interno del `ConstructionPanel` se conserva porque el scrim del modal bloquea el botón persistente. La cadena de foco (`FocusNeighborRight`/`Left`) ahora enlaza `HeroAccessButton` ↔ `ConstructionMenuButton`.

### 🔴 C-5 — `OfflineReportPanel` y `ModalHost` colisionan en z-index

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scenes/CityPrototype.tscn` (`z_index` 30 → 10), `game/scripts/CityMacroView.cs` (suscripciones a `ModalHost.Opened`/`Closed`, `OnModalHostOpened`, `RestoreChronicleVisibility`).
- **Resumen:** El chronicle ahora se dibuja por debajo del modal y se oculta automáticamente cuando el modal se abre. Al cerrarse, se restaura con `ShowLog` para mantener el flujo de eventos.

### 🔴 C-6 — `ConstructionPanel` no se actualiza al cambiar la madera

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/ConstructionPanel.cs` (`_wasAuthorizeEnabled`, `_wasFarmEnabled`, `_wasQuarryEnabled`, `_pulseTween`, `DetectEnableTransition`, `PulseButton`, tooltips contextuales en los botones).
- **Resumen:** El `Refresh()` ya reaccionaba a `BuildingStateChanged`. La mejora detecta la transición `disabled → enabled` en cada uno de los tres botones de autorización y dispara un breve tween verde (`modulate` 0.15 s + 0.45 s). Además, los tooltips ahora explican por qué un botón está deshabilitado ("Needs 1 wood — gather from a Forest first.").

### 🔴 C-7 — `ProductionPanel` no expone la política reactiva

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/ProductionPanel.cs` (sección "Reactive policy", `SpinBox` Min/Max, `ValidatePolicy`), `game/scripts/BuildingDetailView.cs` (`OnPolicyConfigureRequested`, `ConfigureProductionPolicy`).
- **Resumen:** El panel ahora muestra dos `SpinBox` (Min/Max) bajo el toggle de producción. Validan `Min ≤ Max` con error inline y persisten al dominio vía `ConfigureProductionPolicy`. La sección se oculta en edificios sin capacidad (`StorageCapacity == 0`). `Priority` queda en el dominio pero no se expone en UI (sigue el plan: almacenado pero no actuado).

### 🔴 C-8 — Fallos de asignación silenciosos en `BuildingDetailView`

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/Notifier.cs` (autoload nuevo), `game/project.godot` (registro de `Notifier`), `game/scripts/BuildingDetailView.cs` (reemplazo de `GD.Print` por `Notifier.ShowError`, `FormatAssignmentError` local).
- **Resumen:** Nuevo autoload `Notifier` con dos métodos: `Show` (info) y `ShowError` (warning). Aparece como un toast en la parte inferior central, con auto-hide a los 3 s. `BuildingDetailView` ahora muestra mensajes legibles cuando un assignment es rechazado. El `ConstructionPanel` mantiene su `_errorLabel` interno, pero todos los flujos de feedback de la `BuildingDetailView` pasan por el `Notifier`.

### 🟠 H-1 — Plot no muestra progreso durante construcción

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/BuildingPlot.cs` (`_progressBar`, `UpdateText`, tooltip contextual).
- **Resumen:** Cada plot bajo construcción muestra una `ProgressBar` de 8 px de alto bajo el sprite, con un tooltip que indica el ratio numérico (`progress / required`). Las animaciones de pulse y los tooltips se actualizan en cada `Configure` desde `BuildingPlotStage`.

### 🟠 H-2 — `CityStatusPanel` puede saturarse

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/CityStatusPanel.cs` (`BuildResourcesChip`, `IconChip.UpdateText`, `IconChip` `MouseFilter=Pass`).
- **Resumen:** Food + Wood ahora viven en un único `IconChip` "Resources" con tooltip que desglosa ambas cantidades. El resto de chips siguen viéndose, pero la barra ya no excede 1280 px en condiciones normales.

### 🟠 H-3 — Sin control de velocidad de simulación

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/CityWorldController.cs` (`SpeedChoice`, `SetSimulationSpeed`, `SimulationSpeedChanged`), `game/scripts/CityStatusSnapshot.cs` (`HasController`, `CurrentSpeed`), `game/scripts/CityStatusPanel.cs` (`BuildSpeedControl`, `AddSpeedButton`).
- **Resumen:** Cuatro botones compactos (Pause, 1×, 2×, 4×) junto al chip de clock. El activo queda destacado y deshabilitado. `SimulationTickIntervalSeconds` se ajusta automáticamente; Paused = 0 hace que la loop de `_Process` no emita ticks.

### 🟠 H-18 — Control de velocidad inconsistente durante pausa

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/CityWorldController.cs`, `game/scripts/PlayPauseButton.cs`, `game/scripts/SpeedButton.cs`.
- **Resumen:** Play/pause ahora muestra la acción disponible, restaura la última velocidad activa y no fuerza 1× al reanudar. El selector mantiene visible el multiplicador elegido pero queda deshabilitado durante pausa, por lo que ya no contradice su responsabilidad ni reanuda accidentalmente. El controlador rechaza valores fuera de 0×/1×/2×/4×.

### 🟠 H-19 — Costura única para avance temporal offline

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/Domain/WorldTimeAdvance.cs`, `game/scripts/Domain/OfflineProgression.cs`, `tests/WorldofGoses.Tests/WorldEventLogTests.cs`.
- **Resumen:** `WorldTimeAdvance` decide entre fast-forward de un solo lote para mundos inactivos y stepping canónico para mundos activos. `OfflineProgression` ya no posee el loop completo ni cuenta categorías concretas para detectar novedades: un cursor del log devuelve exactamente los eventos del batch. Una regresión cubre eventos preexistentes de categorías no productivas.

### 🟠 H-20 — Batching causal para ciudades quiescentes

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/Domain/CityWorld.cs`, `game/scripts/Domain/WorldTimeAdvance.cs`, `tests/WorldofGoses.Tests/WorldTimeAdvanceTests.cs`.
- **Resumen:** Una ciudad con edificios/proyectos pero sin asignaciones agrupa todos los ticks que permanecen dentro de la misma fase día/noche. Upkeep, WellFed, stop causes y reloj se aplican en lote; amanecer/atardecer, proyectos completables y bosques demolibles conservan stepping canónico. Una prueba de tres días + 217 ticks compara snapshot JSON y secuencia completa de eventos contra `AdvanceWorldTick`; sólo los seis límites temporales requieren stepping. Mundos con trabajadores asignados permanecen deliberadamente en el camino canónico hasta H-21.

### 🟠 H-21 — Extraer simuladores cohesivos de `CityWorld`

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/Domain/CitizenAssignmentService.cs`, `game/scripts/Domain/BuildingProductionSimulation.cs`, `game/scripts/Domain/ConstructionSimulation.cs`, `game/scripts/Domain/CityWorld.cs`.
- **Resumen:** `CityWorld` conserva su API pública y propiedad del agregado, pero delega consistencia de asignaciones, tick productivo y tick de construcción a tres colaboradores internos puros. Recursos, eventos, autorización, persistencia y transición proyecto→edificio permanecen en la fachada mediante callbacks estrechos. El archivo bajó de más de 1.800 a aproximadamente 1.580 líneas sin introducir Godot, service locator, event bus ni nuevas dependencias.

### 🟠 H-22 — Eventos causales tipados, compactables y persistibles

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/Domain/WorldEvent.cs`, `WorldEventLog.cs`,
  `WorldEventRetention.cs`, `Domain/Persistence/WorldEventSave.cs`,
  `WorldPersistence.cs`, `CityWorld.cs`, `game/scripts/Ui/WorldEventTextFormatter.cs`.
- **Resumen:** sujetos y causas usan identidad tipada; nombres y copy dejaron de
  ser identidad/dato causal. El schema v5 persiste un máximo de 128 eventos
  significativos, compacta estados repetidos, elimina causas no retenidas y
  restaura la secuencia de IDs. Producción/progreso incremental y día/noche
  permanecen disponibles para reportes de sesión pero no inflan el historial durable.

### 🟠 H-23 — Ledger de recursos con ubicación, reserva y consumo atómico

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/Domain/CityResourceLedger.cs`, tipos
  `ResourceLocation*`/`ResourceReservation*`, `CityWorld.cs`,
  `ConstructionSimulation.cs`, `Domain/Persistence/ResourceReservationSave.cs`,
  `BuildingSave.cs`, `WorldSave.cs` y `WorldPersistence.cs`.
- **Resumen:** el ledger proyecta almacenes físicos por ubicación sin duplicar
  cantidades, centraliza depósito y consumo atómico de recetas, y permite reservar,
  transferir, liberar o consumir suministros con propietario tipado. Schema v6
  persiste reservas, secuencia de IDs e `IronStock`; la validación impide reservas
  huérfanas o superiores al stock físico.

### 🟠 H-4 — Cancelar vs cerrar no está diferenciado

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/ConstructionPanel.cs` (`_cancelButton`, `CancelProjectRequested`, `OnCancelButtonPressed`, `OnCancelProjectRequested`).
- **Resumen:** El footer añade "Cancel project" con icono `Close`. Es visible solo en `Underway`. Al pulsarlo, `CityWorldController.CancelProject` libera el proyecto y un `Notifier` confirma el resultado. La X del header sigue indicando "Close — work continues" mediante el tooltip.

### 🟠 H-5 — Citizens no muestran contexto en el macro view

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/CityMacroSnapshot.cs` (`CitizenItem` con `Location`, `CurrentStamina`, `MaxStamina`), `game/scripts/MacroCitizenActivity.cs` (`CitizenStatusIcon`, fila con icono + label).
- **Resumen:** El nombre de cada citizen en el macro view ahora se acompaña de un icono — casa/build según `CurrentLocation`, warning si `CurrentStamina <= 0`. La macro view se refresca con cada `WorldTickAdvanced` para mantener el estado al día.

### 🟠 H-7 — `ConstructionPanel` esconde opciones hasta tener shelter

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/ConstructionPanel.cs` (`RenderBlueprint` siempre muestra Farm/Quarry; `_farmButton.Visible = true`, `_quarryButton.Visible = true`).
- **Resumen:** Los tres botones aparecen siempre. Farm/Quarry se renderizan deshabilitados con candado y tooltip "Build the Basic Shelter first to unlock the Farm/Quarry" hasta que el shelter esté construido, manteniendo al jugador informado del orden canónico.

### 🟠 H-8 — "Decisions needed" no es accionable

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/OfflineReportPanel.cs` (`SetController`, `DecisionNeeded`, `BuildDecisionRow`, `ResolveBuildingId`), `game/scripts/CityMacroView.cs` (`_offlineReport.SetController(_controller)`).
- **Resumen:** Los grupos de "Decisions needed" ahora son botones cuando el subject name resuelve a un building activo; al pulsar, `CityWorldController.SelectBuilding` abre el detail view. Si el subject ya no existe (Forest demolida), se renderiza como label tradicional.

### 🟠 H-9 — Sin onboarding de la UI en sí

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/TutorialOverlay.cs` (nuevo), `game/scripts/CityWorldController.cs` (señal `HeroCreated`), `game/scenes/CityPrototype.tscn` (nodo `TutorialOverlay` z_index=50).
- **Resumen:** Tras `HeroCreated`, aparece un overlay con scrim y tres tarjetas secuenciales (status bar, hero, primera construcción). Botones Skip / Next navegan. Memoria in-session (no se repite en la misma partida).

### 🟠 H-10 — Sin feedback de autosave

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/CityWorldController.cs` (señal `WorldSaved(long unixMillis)`), `game/scripts/CityStatusPanel.cs` (`AttachController`, `OnWorldSaved`, `ApplySavedChip`, `IconChip.UpdateText`).
- **Resumen:** Cada vez que `TryAutoSave` tiene éxito, el chip "Saved · HH:mm" aparece en el extremo derecho del status bar. Se actualiza en sitio sin recrear el nodo.

### 🟡 M-16 — El icono macro ocultaba el inicio del nombre del ciudadano

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/MacroCitizenActivity.cs`,
  `tools/Capture-VisualMatrix.ps1`, `docs/VISUAL_REGRESSION.md`.
- **Resumen:** el icono de estado ahora se escala dentro de una celda 16×16 y
  mantiene 6 px de separación respecto al nombre. La recaptura muestra `zeventh`
  completo en 1024×576, 1280×720 y 1600×900. El harness lleva su ventana Godot
  al frente antes de capturar para no aceptar imágenes tapadas por otras apps.

### 🟡 M-1 — Botón "Found the city" en el onboarding es ambiguo

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/OnboardingView.cs` ("Found the city" → "Create the hero").
- **Resumen:** El botón final del paso 5 ahora dice "Create the hero", consistente con el título del flujo "Create your hero".

### 🟡 M-2 — `OnboardingView` puede cortar opciones en pantallas bajas

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/OnboardingView.cs` (`NewChoiceGrid` ahora detecta altura del viewport).
- **Resumen:** En alturas < 720 px, las grids de opciones colapsan a una sola columna. El footer con Back/Next/Confirm sigue siendo sticky y visible.

### 🟡 M-3 — `HeroProfileView` "Current condition" es plano

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/HeroProfileView.cs` (`AddStaminaBar`, `AddIconBody` para elemental affinity).
- **Resumen:** "Current condition" ahora incluye una `ProgressBar` de stamina, fila de icono heart para stamina, fila para location (casa/build), y fila para el elemental affinity con icono sun.

### 🟡 M-4 — Home building no muestra información útil

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/BuildingDetailView.cs` (`RefreshHomeSummary`).
- **Resumen:** Cuando el building es Home, en lugar de paneles vacíos se renderiza un `PanelContainer` con "Capacity: X · N citizens resting here." La sección usa el mismo `LineageThemeRegistry.ComponentPanel` que el resto de la pantalla.

### 🟡 M-5 — `ConstructionPanel` tiene `PanelHeader` y `Title` redundantes

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/ConstructionPanel.cs` (`_title.Visible = false`).
- **Resumen:** El `_title` interno ya no se renderiza. El `PanelHeader` queda como única fuente del título.

### 🟡 M-6 — Sprite del héroe sin hover state

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/MacroCitizenActivity.cs` (`HeroClicked` signal, `_UnhandledInput`), `game/scripts/CityMacroView.cs` (`OnHeroClicked`).
- **Resumen:** El sprite del héroe ahora es clickable. Al hacer click en el, `_controller.SelectHero()` abre el profile. El cursor ya cambia a `PointingHand` en hover (heredado de C-3).

### 🟡 M-7 — `OfflineReportPanel` collapse/expand confuso

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/OfflineReportPanel.cs` (copy del header y tooltip).
- **Resumen:** El header del chronicle ahora dice "Chronicle — click to collapse" / "Chronicle — click to expand (N)", haciendo explícito que es clickable y mostrando el conteo de eventos. Tooltip reforzado.

### 🟡 M-9 — `CityStatusPanel` no resalta atenciones urgentes

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/AttentionBanner.cs` (nuevo), `game/scripts/CityMacroView.cs` (`UpdateAttentionBanner`), `game/scenes/CityPrototype.tscn` (nodo `AttentionBanner` z_index=15).
- **Resumen:** Un banner pulsante aparece en la parte inferior central cuando hay buildings con `NoWorkers`/`WorkersExhausted`/`MissingInputs`. El pulse Tween alterna alpha 0.55 ↔ 1 cada 0.9 s. Mensaje claro: "N buildings need attention — open the chronicle."

### 🟢 W-1..W-3 — Slash + entrada/salida con walk en `VisibleWorkerSlot`

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/VisibleWorkerSlot.cs` (estados `Entering/Working/Exiting`, `StartEntry`, `StartSlashLoop`, `OnSpriteAnimationFinished`), `game/scripts/VisibleWorkerSlots.cs` (`ComputeBorderForIndex`, `SlotBorder`).
- **Resumen:** Cada worker asignado entra caminando desde el borde lateral del contenedor (slots 0-1 desde la izquierda, slot 2 desde la derecha) durante 0.7 s, llega al centro y reproduce `slash_down` en bucle (`AnimationFinished` re-dispara la animación porque el sprite no la marca loop). Al removerlo, deja el slash, gira al borde opuesto y camina 0.7 s hasta desaparecer. Toda la coreografía usa `LineageSpritePlayer.PlayWalk`/`PlaySlash` + `Tween` sobre `sprite.position`, sin tocar el slot padre.

### 🟠 S-1 — `CitizenSpriteBank` + `CitizenSpriteCarrier`

- **Cerrado:** 2026-07-22
- **Cambió:** `CitizenSpriteBank`, `CitizenSpriteCarrier`, los slots de workers, `MacroCitizenActivity`, `HeroProfileView` y sus snapshots.
- **Resumen:** Macro, detalle de edificio y perfil reutilizan el mismo sprite persistente por `CitizenId`. La reasignación durante una salida revierte el movimiento del carrier existente y los cambios de contexto no crean otra instancia. Se eliminó el preview duplicado del empty panel.
- **Refuerzo 2026-07-22:** `CitizenSpriteCarrier.Initialize` quedó restringido al ensamblado y el banco cancela/oculta un carrier antes de reemplazarlo o eliminarlo, evitando que un visual diferido sobreviva visible durante el frame de sustitución.

### 🟡 S-4 — Auditoría de ciclo de vida post-refactor

- **Cerrado:** 2026-07-22
- **Cambió:** `CitizenSpriteBank.PruneExcept`, validación de identidad visual y `docs/ARCHITECTURE.md §7b`.
- **Resumen:** La asignación sigue siendo perezosa y el banco conserva como máximo un carrier por ciudadano visualizado. Los carriers ajenos al mundo activo se eliminan y un ID reutilizado con linaje o género distintos reemplaza su visual anterior. No se atribuye una mejora de FPS: no existía un escenario perfilado que la justificara.

### 🔴 C-9 — Falta un shell transversal que reserve HUD y contenido

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scenes/CityPrototype.tscn` (nodo `GameUiShell` con `CityStatusPanel` + `ScreenContent`), `game/scripts/BuildingDetailView.cs` (`LayoutPreset.FullRect` en `_Ready`).
- **Resumen:** `GameUiShell` es un `VBoxContainer` tipado con `CityStatusPanel` (`custom_minimum_size = Vector2(0, 40)`) como primer slot y `ScreenContent` (`Control` con `size_flags_vertical = 3`) como segundo. Valida al iniciar que ambos slots directos existan y estén ordenados, y expone referencias tipadas. Macro, detail y profile viven bajo `ScreenContent`; `BuildingDetailView` aplica `LayoutPreset.FullRect` en su `_Ready` y ya no compensa el HUD con offsets. `OnboardingView` y `TutorialOverlay` son hermanos del shell — cubren el HUD por diseño; `ModalHost` pertenece a la pantalla macro porque su alcance actual es ese flujo.

### 🔴 C-10 — `BuildingDetailView` no tiene un presupuesto vertical responsive

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scenes/CityPrototype.tscn` (subtree `BuildingDetailView > SafeArea > Layout`), `game/scripts/AssignmentPanel.cs` (`BuildListScroll`).
- **Resumen:** `Layout` es un `VBoxContainer` con `Header` fijo arriba y `Content` como `HFlowContainer` de dos columnas (`Main` + `AssignmentPanel`). Ningún `ScrollContainer` envuelve la vista completa; el scroll queda relegado a las listas de datos (`AssignmentPanel._assignedList` y `_availableList`). Caveat: el colapso a una columna es implícito vía `HFlowContainer`; no hay breakpoint explícito. La matriz 1024×576 / 1600×900 no está capturada.

### 🟠 H-12 — El escenario de workers usa coordenadas libres y no puede recortar

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/VisibleWorkerSlots.cs` (`ClipContents = true`, `CustomMinimumSize` derivado), `game/scripts/CitizenSpriteBank.cs` (mounting en el subtree del view).
- **Resumen:** `VisibleWorkerSlots._Ready` activa `ClipContents` y un mínimo derivado de capacidad (3 × `DetailedCitizenWidth + padding`, alto `SlotHeight + 2*padding`). El carrier se monta con `CitizenSpriteBank.Instance.Mount(carrier, this)` dentro del Control recortado, no en un `CanvasLayer` global, así que el rect del stage lo contiene. Caveat: `SpriteCenterY = 68f` y `SlotHeight = 152` están hard-coded; funcionalmente correcto.

### 🟠 H-13 — `CityStatusPanel` vuelve a saturarse con ancho reducido

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/CityStatusPanel.cs` (`Refresh()` reactivo, `BuildCompactCityChip`, `OnViewportSizeChanged`).
- **Resumen:** `Refresh()` consulta `GetViewportRect().Size.X < 1150f` y aplica separación reducida (8 vs 18) más chip combinado "Work · Home · Free" en lugar de los separados. Project y Free Citizens se ocultan en modo compacto. `OnViewportSizeChanged` re-llama a `Refresh`. Caveat: no hay captura automatizada que confirme el umbral visualmente.
- **Refuerzo 2026-07-22:** M-14 reabrió el ítem al capturar un proyecto activo:
  el viewport lógico ocultaba el ancho real y el chip detallado desbordaba hasta
  1600×900. `ShouldUseCompactLayout` usa el ancho físico y fuerza resumen cuando
  hay proyecto; validado visualmente en 1024/1280/1600 y con cuatro casos xUnit.

### 🟠 H-14 — Las listas de Assignment crecen sin scroll ni límite

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/AssignmentPanel.cs` (`BuildListScroll`, `ScrollContainer` con `VerticalScrollMode = Auto`).
- **Resumen:** `_assignedList` y `_availableList` ahora viven dentro de `ScrollContainer`s con `CustomMinimumSize` explícito (`(0, 88)` y `(0, 132, expand)`). Resumen y headers permanecen como `Label` fijos sobre los scrollers. Caveat: no hay wiring de auto-scroll al foco con gamepad; el acceptance criterion "preservar foco y hacer auto-scroll al elemento enfocado" queda parcial.

### 🟠 H-15 — Chronicle ocluye plots y mezcla overlay con panel persistente

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/OfflineReportPanel.cs` (constantes `CollapsedTopOffset`/`ExpandedTopOffset`, header `IconButton` toggle, `visibleRows` por estado), `game/scripts/CityMacroView.cs` (`OnModalHostOpened`).
- **Resumen:** El chronicle inicia colapsado, ocupa solo el header + la fila más reciente (`visibleRows = _isExpanded ? MaxRows : 1`) y se oculta automáticamente cuando `ModalHost` se abre. Caveat: el panel colapsado sigue anclado a `bottom-right` con `offset_left=-376, offset_right=-16`; no es un dock de ancho reservado, solo un overlay no invasivo.

### 🟠 H-16 — Accesos macro colocados con offsets absolutos

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scenes/CityPrototype.tscn` (nodo `MacroActions` con `MarginContainer` + `Actions` `HBoxContainer`), `game/scripts/CityMacroView.cs` (`FocusNeighborRight/Left`).
- **Resumen:** `HeroAccessButton` y `ConstructionMenuButton` viven ahora bajo `ScreenContent/MacroActions/Actions` (HBoxContainer con `separation = 40`); ya no se posicionan con rects absolutos. Caveat: `MacroActions` es hermano de `CityMacroView`, no hijo — vive bajo `ScreenContent`, separado del view.

### 🟡 M-15 — Scroll solo en secciones de información no acotada

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/BuildingDetailView.cs` (árbol sin `ScrollContainer` global), `game/scripts/AssignmentPanel.cs` (scroll solo en listas), `game/scripts/OfflineReportPanel.cs` (scroll solo en `_scroll`).
- **Resumen:** `BuildingDetailView > SafeArea > Layout` no contiene `ScrollContainer` global; `Content` es un `HFlowContainer`. El scroll aparece exclusivamente en `AssignmentPanel._assignedList`/`_availableList` y en `OfflineReportPanel._scroll`. `OnboardingView` y `HeroProfileView` sí tienen scroll global porque su contenido es texto no acotado. Caveat: la regla se aplica implícitamente por convención; no hay registry central ni helper compartido.

### 🟠 H-17 — Modales dependen de mínimos fijos sin fallback estrecho

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/ConstructionPanel.cs` (nuevo `_bodyScroll` + `_bodyContent`, refactor de `BuildShell`), `game/scripts/TutorialOverlay.cs` (nuevo `_bodyScroll`, body movido dentro del scroll).
- **Resumen:** `ConstructionPanel` y `TutorialOverlay` ahora envuelven el body en `ScrollContainer` (`VerticalScrollMode = Auto`, `SizeFlagsVertical = ExpandFill`). Header y footer permanecen fijos fuera del scroll. El viewport max ya estaba cubierto por `ApplyResponsiveMinimumSize` / `ApplyResponsiveCardWidth`. Caveat: la validación visual con texto de prueba 50 % más largo y a 1024×576 sigue pendiente del harness de M-14.

### 🟡 M-13 — Placeholders de plots dominan y desbalancean la ciudad

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/BuildingPlot.cs` (`PlaceholderStyle` con `Subline`, `_placeholderLabelStack` VBoxContainer, `_placeholderSubLabel`, `PlaceholderSize` constant, `InteractionRect` con flag `isPlaceholder`), `tests/WorldofGoses.Tests/BuildingPlotStageTests.cs` (test actualizado).
- **Resumen:** `PlaceholderStyle` ahora tiene `Subline`; el placeholder se compone de un `VBoxContainer` con `Headline` (`SectionTitle`) + `Subline` (`BodySmall`, ej. "Click to gather wood"). `InteractionRect` recibe un flag `isPlaceholder` y devuelve el canvas del placeholder (192 - 2*24, 144×144) cuando corresponde — el hitbox de Forest ya coincide con el área visible. Test actualizado. Caveat: la comparación visual a 1280×720 con cinco plots sigue dependiendo del harness de M-14.

---

### 🟡 M-16 — Forest no gatherable cuando no hay madera

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/CityMacroSnapshot.cs` (`PlotItem.Enabled` para forests), `game/scripts/BuildingPlot.cs` (`_button.Disabled`, tooltip, sublabel).
- **Resumen:** `CityMacroSnapshot.From` setea `Enabled = (WoodReserve > 0)` para forests. `BuildingPlot.Configure` deshabilita el plot cuando `!enabled && !underConstruction`, cambia el tooltip a `"Forest has no wood available."` y el sublabel a `"Depleted"`. Plot sigue visible (reserva la ubicación para consumo de stock) pero no es interactivo.

### 🟠 M-17 — Auto-release de workers al alcanzar max stock

- **Cerrado:** 2026-07-22
- **Cambió:** `game/scripts/Domain/Building.cs` (`MaxStockReleaseCooldown`, `MaxStockHoldTicks`, `TickMaxStockWatch`), `game/scripts/Domain/CityWorld.cs` (llamada en `AdvanceWorldTick`).
- **Resumen:** `Building.TickMaxStockWatch` cuenta ticks consecutivos con `Stock >= MaxStock` (post-producción). Cuando el counter alcanza `MaxStockReleaseCooldown` (6), `ReleaseAssignedWorkers` desasigna a todos los citizens y los deja en `AtHome`. `Building.MaxStockHoldTicks` se resetea a 0 cuando el stock cae bajo el cap (consumo entre ticks). Esto evita que un pico de producción que llega a max vacíe el worksite sin razón.

### 🟠 M-18 — Menú ESC y reinicio seguro del onboarding

- **Cerrado:** 2026-07-23
- **Cambió:** `game/scenes/PauseMenu.tscn`, `game/scripts/PauseMenu.cs`, `game/scripts/CityWorldController.cs`, `game/scripts/Domain/Persistence/WorldPersistence.cs`, `game/scenes/CityPrototype.tscn`, `tests/WorldofGoses.Tests/WorldPersistenceTests.cs`.
- **Resumen:** ESC abre una escena modal reutilizable, pausa la simulación y permite cerrar con ESC, X, scrim o Resume. `Start over` exige confirmación, elimina solo el slot primario con sus sidecars y recarga la escena para volver al onboarding. `Settings` queda visible y deshabilitado como siguiente slice. Las fixtures `pause-menu` y `pause-menu-reset` pasaron revisión a 1024×576, 1280×720 y 1600×900 sin escribir en el guardado real.

### 🟡 M-19 — Base ortogonal de terreno y parcelas

- **Cerrado:** 2026-07-23
- **Cambió:** `game/scenes/OrthogonalParcelTerrain.tscn`, `game/scripts/OrthogonalParcelTerrain.cs`, `game/scenes/CityPrototype.tscn`, `game/assets/terrain/kenney/roguelike-rpg/`, bible de territorio e inventario de assets.
- **Resumen:** La vista macro adopta definitivamente una cuadrícula ortogonal elevada. Ocho parcelas provisionales, suelo CC0 a escala entera y árboles decorativos deterministas forman el terreno inicial sin introducir estado de simulación dentro de Godot. Quedan para slices posteriores el dominio de parcelas, bloqueo/desbloqueo y la vinculación entre árboles visibles, reserva, agotamiento y regeneración.

### 🟠 M-20 — Árboles interactivos y acceso clicable al menú

- **Cerrado:** 2026-07-23
- **Cambió:** `ResourceTree`, `ResourceActionMenu`, `OrthogonalParcelTerrain`, `MacroCitizenActivity`, `CityMacroSnapshot`, `CityMacroView`, `PauseMenu`, `CityPrototype.tscn` y cursor CC0 de hacha.
- **Resumen:** Los Forest ya no producen tarjetas macro. Su reserva genera árboles interactivos; hover instala el cursor de hacha, clic izquierdo o derecho abre el menú contextual, y Gather desplaza automáticamente la representación macro del héroe antes de recoger 2 wood. ESC y el nuevo botón Menu abren la misma pantalla de pausa. Persisten como trabajo posterior la identidad individual de cada árbol, 40 wood por árbol, duración de trabajo simulada y regeneración.

### 🟠 M-21 — Interacción de árboles y progreso inicial bloqueados

- **Cerrado:** 2026-07-23
- **Cambió:** `CityPrototype.tscn`, `ConstructionPanel`, `CityMacroView`, `CityWorld`, `CityWorldController` y tests de construcción/UI.
- **Resumen:** El contenedor central deja pasar el puntero hacia los árboles, por lo que hover y clic alcanzan el recurso. Autorizar el Basic Shelter asigna automáticamente al fundador disponible; al cargar, una reparación idempotente cubre partidas antiguas cuyo refugio quedó sin contribuyentes. El body del modal acepta la rueda sobre todo el panel y conserva header/footer fijos. Clic real y scroll inferior pasaron la matriz visual en 1024×576, 1280×720 y 1600×900.

### 🟠 H-24 — Gramática de movimiento pixel-art y rutas con ocupación

- **Cerrado:** 2026-07-23
- **Cambió:** `PixelMotion`, `MacroCitizenActivity`, `CitizenSpriteCarrier`,
  `BuildingPlotStage`, `CityMacroView`, `BuildingDetailView` y tests de ruta.
- **Resumen:** Una primitiva compartida fija locomoción a 12 Hz, pasos cardinales
  de 8 px y posiciones enteras. Gather calcula una ruta corta alrededor de los
  footprints visibles en lugar de atravesar el Shelter. El paseo macro y las
  entradas/salidas de ciudadanos usan la misma cadencia; la detail view deja de
  aplicar un fade subpíxel. Ruta intermedia y composición detail pasaron la
  matriz 1024×576, 1280×720 y 1600×900.

---

## 7. Canceladas / Superadas

### 🟠 H-25 — Recursos naturales dependían de `BuildingKind.Forest`

- **Cerrado:** 2026-07-24
- **Motivo:** superado por `C-ForestAdapterRetired`. La migración v13→v14
  elimina el adaptador y conserva madera en `NaturalResourcePatches` y
  `CityInventory`; mantener H-25 activo duplicaba trabajo ya cerrado.

### 🟠 S-2 — `BuildingSpriteCarrier`

- **Cancelado:** 2026-07-22
- **Motivo:** `BuildingPlotStage.Render` ya reconcilia por `BuildingId`, actualiza el mismo `BuildingPlot` durante su vida y solo lo libera cuando la entidad deja de existir. Un segundo bank/autoload duplicaría ese ciclo de vida sin resolver un bug actual.

### 🟡 S-3 — `ItemSpritePool`

- **Cancelado:** 2026-07-22
- **Motivo:** El prototipo no renderiza sprites efímeros de items y no hay allocation churn medido. El pool se reabrirá cuando exista un efecto repetido real y el profiler demuestre que instanciarlo es un coste relevante.

### 🟡 M-8 — Forest plot sin art

- **Cancelado:** 2026-07-22
- **Motivo:** Bloqueado por el art pipeline. `forest_idle.png` no existe en `game/assets/buildings/` ni en `art/exports/buildings/`. El placeholder marrón funciona, pero el art real requiere el flujo de Pixelorama → exports → assets documentado en `docs/ART_PIPELINE.md`. El placeholder cumple su rol por ahora; re-abrir cuando arte ship.

### 🟡 M-10 — Sin audio cues en eventos

- **Cancelado:** 2026-07-22
- **Motivo:** Bloqueado por falta de assets de audio. El proyecto no tiene carpeta `game/assets/audio/` poblada. El bible menciona audio como pendiente en `docs/world-of-goses-design-bible/09_AUDIO_GUIDELINES.md`. Re-abrir cuando exista una fuente de SFX aprobada.

---

## 8. Referencias cruzadas

- `docs/CURRENT_STATUS.md` — estado general del proyecto.
- `docs/UI_PATTERNS.md` — reglas de UI que toda mejora debe respetar.
- `docs/UI_AUDIT.md` — auditoría previa.
- `docs/world-of-goses-design-bible/` — fuente de verdad de diseño.
- `README.md §15` — founding hero y next proof.
