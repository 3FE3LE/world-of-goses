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

- **2026-07-26** — Abierto H-32: migración de la vista macro a una
  perspectiva pseudo-3D por calles (dirección documentada en el design
  bible, prototipos aislados validados, e integración real como vista por
  defecto con clic-a-detalle y recolección de madera funcionando). Ver
  detalle completo en la entrada de H-32 en §2. Build limpio, 436/436
  tests, headless boot verificado, save real intacto tras las pruebas de
  solo lectura. Próximos pasos documentados en la propia entrada.

- **2026-07-27** — Auditoría global código↔documento (cuatro barridos:
  H-32, H-26/H-29, S-1/H-30/H-31, y UI H-11/M-11/M-12/M-25/M-22) más el
  slice de gameplay/UX de la perspectiva pedido por el usuario:
  - **H-32, defectos corregidos:** F9 bidireccional y overlays de la vista
    plana que reaparecían por tick sobre la perspectiva (guards de
    visibilidad en `CityMacroView`). Detalle en la entrada de H-32.
  - **H-32, plano de calles:** edificios/árboles anclados detrás de su
    calle (la calle es el corredor frontal libre), árboles con los tiles
    Kenney reales + cursor de hacha + menú de acción reanclado, fundador
    renderizado con su carrier canónico LPC, y Gather con ruteo cuantizado
    por calles (`StreetRoutePlanner`, cruces solo por huecos viables; W/S
    manual respeta la misma regla). 11 tests nuevos. 447/447.
  - **Correcciones documentales de la auditoría:** la entrada Pendientes de
    H-11 (duplicado obsoleto de un cierre del 2026-07-25) se eliminó; M-11
    se movió a Hechas; H-26 aclara que el schema vigente es v14 (v9 fue el
    que introdujo los placements); S-1.7 registra su estado real (40 ms +
    `Write-Warning`, muestreo que no mide frames del engine — pendiente
    real); la afirmación de que `FlashLarge` cubría expedición/ciudadano se
    corrigió (solo cubre obra completada; el resto sigue en M-25); los
    marcadores `# TODO (i18n)` de `en.po` que citaba S-1.1 ya no existen
    (el único es la instrucción de convención del encabezado);
    `docs/CURRENT_STATUS.md` corregido (capa `AttentionBanner` inexistente
    y call sites de `FlashLarge`).
  - **Purga:** primeras cerradas vencidas eliminadas según la política de
    dos días (todas las de 2026-07-22/24); tabla de resumen recalculada.
  - Hallazgos que quedan abiertos con dueño: fragilidad de orden de dibujo
    y comentarios erróneos de `OrthogonalParcelTerrain` (H-29), cruce del
    modelo de corredores H-26 ↔ `StreetRoutePlanner` (nota en Cola activa),
    paridad del Chronicle en la perspectiva (paso 3 de H-32), y ausencia de
    licencia MIT junto a los 28 iconos Pixelarticons promovidos (M-22 —
    el inventario decía 3 iconos; son 28).

- **2026-07-27 (continuación: bug real de clic + migración pedida por el
  usuario)** — El usuario reportó en juego real que el clic sobre un árbol
  no abría las opciones de gathering aunque el hover mostraba el cursor de
  hacha, y pidió migrar por completo (adaptado) el sistema de parcela y
  construcción con placement a la perspectiva, más un piso con tiles que
  respete la profundidad. La causa del bug de clic era estructural, no de
  `MacroStreetLiveView`: `ScreenContent` (contenedor padre de ambas vistas
  macro) nunca tuvo `mouse_filter` seteado, así que con el default `Stop`
  tragaba todo clic/motion antes de `_UnhandledInput` — afectaba también
  (y probablemente siempre afectó, sin que nadie lo hubiera probado con un
  clic real) el clic sobre edificios completados. Corregido con una línea
  en `CityPrototype.tscn`. Implementado además: piso de calle como tiles
  individuales proyectados por profundidad con el mismo patrón de color de
  `OrthogonalParcelTerrain`, y el sistema de construcción/placement
  completo (`ConstructionMenuButton`, `ConstructionPanel`, selección de
  lote con resalte y Confirm/Cancel) nativo de la perspectiva, verificado
  de punta a punta con clics OS simulados. Ver detalle en H-32 §2. Build
  limpio, 447/447 tests, headless boot con el save real verificado.

- **2026-07-28 — Reorientación al cierre del primer ciclo jugable:** se
  consolidó la auditoría en `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` y se decidió
  detener la ampliación horizontal. Las iteraciones posteriores corrigieron
  llegada causal al trabajo, acceso por la entrada frontal, cadencia económica,
  almacenamiento, asignación mutuamente exclusiva, retorno autónomo al Shelter
  y el primer slice de trabajo interrumpible. `Citizen.WorkOrder` conserva la
  orden del jugador durante saturación, necesidades vitales y expedición;
  `CitizenVitalStatus` solo decide comer/descansar, nunca elige profesión ni
  trabajo. Los viajes tienen confirmación visual y fallback abstracto de 30
  ticks para equivalencia offline. Estado verificado: build sin errores ni
  warnings, suite completa vigente y arranque headless correcto. El milestone y sus
  gaps quedan fijados debajo para que `CitizenAgenda` y otros sistemas de
  profundidad no desplacen el cierre del loop.

## 1. Resumen rápido

| Prioridad | Pendientes | En curso | Bloqueados | Hechos | Cancelados |
| --------: | ---------: | -------: | ---------: | -----: | ---------: |
| 🔴        | 6          | 0        | 0          | 0      | 0          |
| 🟠        | 4          | 2        | 0          | 2      | 0          |
| 🟡        | 3          | 1        | 0          | 2      | 0          |
| 🟢        | 0          | 0        | 0          | 1      | 0          |

> La tabla cuenta solo las entradas presentes en el archivo: la purga del
> 2026-07-27 eliminó las cerradas del 2026-07-22/24 (política de dos días).

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

### Milestone VS-1 — Primer ciclo jugable completo y repetible

> Esta sección es el contrato activo de producto y **no se purga** con la regla
> de dos días de `Hechas`. Hasta cerrarla, cualquier sistema nuevo debe probar
> que elimina un bloqueo del flujo objetivo. `CitizenAgenda`, profesiones
> profundas, instituciones, política, múltiples biomas y contenido adicional
> quedan subordinados a este milestone.

#### Flujo objetivo

```text
Onboarding → caída → gathering → Shelter/Farm/Quarry → reclutamiento
→ asignación → producción/consumo → preparación de expedición
→ salida/encuentro/objetivo/regreso → consecuencias → territorio
→ nueva decisión urbana → guardado/carga → repetición
```

#### Implementado y comprobado hasta 2026-07-28

- Onboarding astral, fundador persistente, caída y primer gathering de Wood.
- Construcción secuencial de Basic Shelter, Farm y Quarry con placements reales.
- Reclutamiento mínimo y roster de ciudadanos persistentes.
- Una sola entidad `Citizen`; rol, competencias, asignación, expedición y
  necesidades se componen sobre ella.
- `Citizen.Commitment` evita responsabilidades activas incompatibles y
  `Citizen.WorkOrder` conserva la orden laboral decidida por el jugador durante
  una interrupción temporal.
- La asignación diurna comienza en `InTransit`. Producción, stamina y experiencia
  esperan llegada semántica; Godot puede confirmarla al terminar la ruta y el
  dominio aplica un fallback abstracto de 30 ticks para progreso offline.
- Los ciudadanos se aproximan por la banda frontal del edificio; el fundador
  queda oculto en macro mientras trabaja dentro y vuelve caminando al Shelter.
- Farm, Quarry y gathering producen por ciclos de 10 ticks, no por frame/pico.
  La comida tiene cadencia separada; capacidades provisionales Farm/Quarry son
  60/80 y el cierre por stock espera 60 ticks.
- Stock completo pausa la ejecución sin borrar la orden. Cuando reaparece
  demanda, la orden vuelve a ser elegible.
- Primer slice vital agnóstico a la actividad: stamina ≤20 interrumpe, regreso
  causal al Shelter, comida + descanso, reanudación con stamina ≥70. Sin comida
  queda `WorkersBlockedNoFood`; durante recuperación queda `WorkersRecovering`.
- Una expedición puede interrumpir una orden y conservarla en save/load. Al
  regresar exige recuperación y no reanuda antes del siguiente día.
- Persistencia v14 conserva orden, compromiso, necesidad, ubicación, sentido y
  comienzo del viaje, y límite temporal de reanudación. Campos nuevos aditivos.
- Corrección de validación 2026-07-28: el fallback de 30 ticks queda restringido
  al catch-up offline. En vivo, solo la llegada visual confirma `AtWork`/`AtHome`;
  además, la ruta activa al Shelter no se replantea en cada snapshot.
- Evidencia automática actual: 455/455 pruebas, build 0 errores/0 warnings,
  Godot 4.7.1 headless carga el slot real y `git diff --check` no reporta errores.
- Estabilización HUD 2026-07-28: control de velocidad con caja estable y cuatro
  indicadores visibles a 4×; barra de estado ornamental por linaje con copia
  compacta del recurso; safe area global de 8 px; navegación edge-to-edge sin
  offsets superiores/laterales; botones Kenney normalizados a padding 16×4 para
  icono+texto y 2×2 para acciones solo-icono. Los botones de navegación dejaron
  de imponer anchos manuales y `HeroAccessButton` comparte la inicialización
  canónica de `IconButton`.

#### Hallazgos que condicionan los siguientes pasos

- El prototipo conserva una sola `CitizenWorkOrder`, no una agenda de varias
  directivas. Es suficiente para probar interrupción/reanudación; no resuelve
  aún "pociones hasta N → investigar". `CitizenAgenda` queda en profundidad
  posterior al vertical slice.
- La expedición vigente es abstracta, de un solo líder y sin equipo real,
  preparación por ciudadano, fases visibles completas, retirada ni resolución
  de heridas/territorio.
- `CitizenBehaviorState.Injured` todavía mezcla agotamiento cero con lesión. El
  slice de consecuencias debe separar `Exhausted` de heridas persistentes.
- `Recovery` existe como compromiso reservado, pero aún no hay condición de
  herida, tratamiento, tiempo, coste ni historial personal completo.
- Los trabajadores no héroes usan representación macro simplificada: su llegada
  visual se reconcilia inmediatamente; falta validar varios ciudadanos viajando,
  volviendo y cambiando de vista sin duplicar carriers ni teletransportes visibles.
- La comida se obtiene del inventario urbano al llegar al Shelter. Esto prueba
  causalidad/bloqueo, pero todavía no modela transporte, comedor, raciones ni
  políticas. Esas extensiones no bloquean el primer loop.
- La UI explica bloqueos del edificio, pero falta una superficie consistente que
  explique por ciudadano: orden conservada, actividad actual, necesidad y próxima
  acción.
- El fallback abstracto de viaje permite offline determinista, pero sus 30 ticks
  son tuning provisional y debe verificarse contra duración visual real.
- Falta un recorrido humano completo desde onboarding hasta segunda decisión
  después de una expedición; headless/tests no sustituyen esa firma.

#### Cola de estabilización obligatoria

##### 🔴 VS-0 — Congelar y validar la ciudad causal actual

- **Estado:** Completado y validado por recorrido humano el 2026-07-28.
- **Avance 2026-07-28:** dos regresiones end-to-end nuevas fijan save/load en
  tránsito hacia el trabajo y hacia el Shelter. Live y mundo restaurado conservan
  inicio/dirección del viaje, llegada, stock y stamina; la comida no se consume
  antes de la llegada física. La pasada humana detectó desaparición prematura y
  un loop lateral de tres tiles al volver: se separó llegada live/offline y se
  impidió reiniciar una ruta de retorno activa. Una segunda pasada encontró que
  el carrier flyweight conservaba simultáneamente el `GoTo` del slot interior y
  la ruta procedural macro; la transferencia de vista ahora cancela el movimiento
  anterior para que exista un solo escritor de posición. La tercera observación
  precisó además un error geométrico: `WorkplaceEntranceStreet` restaba una calle
  aunque `PlotBox.Street` ya representa la banda frontal del lote; el destino
  ahora llega a esa banda antes de confirmar entrada. La siguiente comprobación
  detectó una movilización innecesaria: asignar a un edificio en `MaxStock` ahora
  conserva la orden pero mantiene al ciudadano en casa hasta que el stock vuelva
  a requerir producción. Tras retirar la ruta visual plana y sus pruebas de
  geometría obsoletas, la suite vigente es 448/448; queda firmar el recorrido
  humano sobre la única vista en perspectiva.
- **Regresión fresh start 2026-07-28:** el reset total ahora suprime escrituras
  del controlador viejo antes de borrar el slot y recargar la escena, evitando
  que el autosave reconstruya el fundador durante el teardown. La confirmación
  de identidad también es idempotente ante una segunda activación encolada: si
  el primer intento ya creó al héroe, continúa sin mostrar `AlreadyExists` y sin
  sobrescribir su perfil.
- **Regresión de primera obra 2026-07-28:** `MacroStreetLiveView.AddPlot`
  confundía “no clicable” con “sin identidad” y guardaba `-1` para proyectos.
  La ruta no encontraba el worksite y confirmaba llegada remota. `PlotBox` ahora
  conserva siempre el ID de dominio y modela interactividad por separado; el
  fundador debe caminar al lote antes de contribuir al Basic Shelter.
- **Building-detail regression 2026-07-28:** removing a worker exposed three
  presentation problems after the flat-view retirement: unstable Back
  navigation, Chronicle leaking into detail, and a stretched/cropped building
  preview inside an oversized sparse layout. The detail screen now keeps a
  stable header, uses a compact two-column composition, renders art at 256×256
  with preserved aspect ratio, and Chronicle is explicitly macro-only. A
  follow-up removed the macro navigation strip from all subviews and placed the
  title/Back header above the content, preventing top overlap. A subsequent UX
  review rejected view-level scrolling: the body is fixed again, worker and
  production panels are bottom-anchored and grow upward, and only the Assigned/
  Available lists scroll after five rows. The building preview and citizen
  stage now share one 256 px visual layer, ready to become an interior scene
  later without stacking extra vertical sections.
- **Perspective stabilization 2026-07-28:** placement no longer draws the nine
  internal tile cells of a 3×3 lot; each candidate is one unified footprint
  with 2 px stepped sides matching the terrain projection. Foot traffic now
  reveals a narrow dirt trace and widens it progressively instead of replacing
  a whole floor tile at 50% wear. Shelter arrival now hides the macro carrier
  only after the domain confirms `AtHome`, preventing the entrance sit/loop,
  and Chronicle localizes semantic building names before formatting events.
  A follow-up found project completion released the work order without moving
  the contributor out of `AtWork`; completion now starts a real return-home
  transition (or preserves `AtHome` when night already moved them), so Shelter
  arrival can be confirmed instead of immediately remounting the carrier. The
  previously inert Explore toolbar button is now wired to toggle the existing
  `ExpeditionPanel` through `ModalHost`.
- **Bloquea:** toda ampliación de expediciones y consecuencias.
- **Trabajo:** recorrido manual fresh save; asignar/reasignar durante batch;
  llegada Farm/Quarry; stock lleno; retorno al Shelter; hambre sin/con comida;
  cambio macro↔detalle; amanecer; save/load en cada tránsito; catch-up offline.
- **Orden de firma restante:** (1) fresh save hasta Shelter/Farm/Quarry;
  (2) Farm→Quarry durante jornada y llegada por entrada frontal; (3) edificio
  lleno sin viaje innecesario; (4) agotamiento con comida y sin comida;
  (5) retorno al Shelter y reanudación de la orden al recuperarse; (6) repetir
  un tránsito cruzando macro↔detalle; (7) guardar/cargar durante ida y retorno;
  (8) cerrar/reabrir con catch-up offline y comparar stock, comida y stamina.
- **Correcciones incluidas:** cualquier loop, teleport, producción anticipada,
  carrier duplicado, orden perdida o bloqueo sin explicación encontrado.
- **Aceptación:** mismos resultados live/offline y ningún paso requiere editor,
  fixture o comando de depuración.

##### 🔴 VS-1 — Reclutamiento y asignación como decisión real

- **Estado:** En progreso. Primer corte: el reclutamiento deja de ser ilimitado;
  la navegación directa de reclutamiento fue retirada. El nuevo Ayuntamiento
  se construye con madera y piedra, usa el placeholder marrón y aloja como máximo
  un prospecto encontrado por expedición. El prospecto conserva identidad y
  ficha técnica, no cuenta como ciudadano ni puede trabajar hasta ser acogido
  desde el detalle del Ayuntamiento; la aceptación también exige vivienda libre.
  El prospecto pendiente persiste en save/load.
- **Trabajo:** reclutar al menos un segundo ciudadano en partida normal; asignar
  y remover desde UI; incompatibilidad trabajo/construcción/expedición; ciudadano
  concreto visible y persistente; presión de comida suficiente para elegir quién
  produce, quién descansa y quién queda disponible.
- **Aceptación:** el jugador no puede usar la misma persona simultáneamente y la
  UI explica tanto indisponibilidad como coste de oportunidad.

##### 🔴 VS-2 — Expedición completa mínima

- **Estado:** Pendiente; la expedición actual solo prueba reserva/tiempo/retorno.
- **Trabajo:** formación con ciudadanos reales; retirar temporalmente del trabajo;
  preparación; salida; trayecto; un encuentro; objetivo; regreso; resolución;
  recompensa o pérdida; cancelación/retirada mínima; Chronicle causal.
- **Aceptación:** ninguna fase se salta directamente por UI y el retorno no se
  reduce a un contador o toast.

##### 🔴 VS-3 — Consecuencias y territorio

- **Estado:** Pendiente.
- **Trabajo:** estado territorial mínimo extensible (bloqueada → reconocida →
  ruta asegurada → disponible); una consecuencia personal persistente; herida
  distinta de agotamiento; indisponibilidad; recuperación en Shelter con tiempo
  y recurso; cambio de rendimiento; historial del ciudadano; desbloqueo de una
  parcela, ruta o posibilidad sistémica.
- **Aceptación:** el regreso obliga a reorganizar la ciudad y produce una nueva
  decisión significativa.

##### 🔴 VS-4 — Persistencia completa del loop

- **Estado:** Pendiente; save v14 cubre la base, no las consecuencias futuras.
- **Trabajo:** persistir equipo expedicionario, fase, resultados, territorio,
  condiciones, recuperación, historial y relaciones nuevas; validar referencias;
  migración aditiva mínima; save/load durante salida, regreso y recuperación;
  equivalencia offline por lotes/eventos discretos.
- **Aceptación:** cerrar y abrir la aplicación en cualquier fase no duplica,
  pierde ni contradice ciudadanos, reservas, cargas, heridas u órdenes.

##### 🔴 VS-5 — Firma y repetición del vertical slice

- **Estado:** Pendiente.
- **Trabajo:** ejecutar desde onboarding hasta nueva decisión post-expedición,
  guardar/cargar, iniciar un segundo ciclo sin reset, documentar tiempo real,
  bloqueos y decisiones observadas; añadir fixtures solo donde protejan bugs
  reales encontrados en la pasada humana.
- **Aceptación:** se cumplen los 17 criterios de
  `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`; solo entonces se abre profundidad.

#### Estado de los 17 criterios de aceptación

| # | Criterio | Estado 2026-07-28 |
| -: | --- | --- |
| 1 | Onboarding y fundador persistente | ✅ Implementado |
| 2 | Gathering y asentamiento inicial | ✅ Implementado |
| 3 | Reclutar un nuevo habitante | 🟡 Prototipado; validar flujo normal |
| 4 | Asignar y remover habitantes | ✅ Implementado; falta pasada humana multi-citizen |
| 5 | Farm/Quarry producen causalmente | ✅ Implementado |
| 6 | Consumo/presión genera decisiones | 🟡 Primer bloqueo vital implementado; falta calibración jugable |
| 7 | Exclusión entre tareas incompatibles | ✅ Implementado en compromiso activo |
| 8 | Formar expedición con ciudadanos reales | 🟡 Solo líder/fundador abstracto |
| 9 | Salida, encuentro y regreso | 🟡 Tiempo/resultado existen; fases completas no |
| 10 | Resultado modifica ciudad/persona/territorio | 🟡 Recursos/migrante; consecuencias incompletas |
| 11 | Ciudadano herido o indisponible | ❌ Herida persistente no implementada |
| 12 | Ciudad responde a consecuencia | ❌ No implementado end-to-end |
| 13 | Desbloqueo territorial/sistémico | ❌ No conectado a expedición |
| 14 | Guardar y cargar estado completo | 🟡 Base v14 sólida; faltan futuros estados del loop |
| 15 | Nueva decisión significativa al final | ❌ No implementada |
| 16 | Repetir sin reiniciar | ❌ No firmado |
| 17 | Sin editor/debug | 🟡 Ciudad base sí; loop completo no existe aún |

#### Profundidad explícitamente aplazada

- `CitizenAgenda` con varias directivas, condiciones y prioridades.
- Profesiones/competencias profundas, héroes, sinergias, linajes completos.
- Enfermedades, venenos, maldiciones, discapacidades y tratamientos múltiples.
- Instituciones, políticas de racionamiento, economía compleja y transporte.
- Política/cultura avanzada, generaciones, árboles de habilidades, biomas y
  campañas. Solo se adelanta una de estas piezas si un gap VS demuestra que es
  técnicamente imprescindible para cerrar el loop.

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

1. **VS-0** — Congelar y validar la ciudad causal actual con recorrido humano.
2. **VS-1** — Cerrar reclutamiento/asignación/coste de oportunidad.
3. **VS-2 → VS-5** — Expedición mínima, consecuencias, persistencia y firma.
4. **H-32** — Solo pasos que bloqueen VS-0/VS-1 (clic en proyecto en
   construcción, guía de estado vacío, paridad del Chronicle, anclaje
   `LotHeight > 1`, evaluación de retiro de la vista plana).
5. **M-22** — Cerrar solo integración de assets que bloquee legibilidad del slice.
6. **H-26** — Malla transitable y clasificación de pasillo / camino / calle
   (slices siguientes; el primer corte ya está cerrado). Cuando cierre,
   abre **S-1.2** (NavigationServer2D) que reemplaza el pathfinding
   cardinal. Nota 2026-07-27: `StreetRoutePlanner` (H-32) ya aplica la
   lectura de "cruce viable entre construcciones" a escala de calle macro;
   reconciliar ambos modelos al retomar este ítem.

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
  calles. El schema persiste para cada edificio/proyecto (introducido en v9;
  la versión vigente es v14 — ver `WorldSave.CurrentVersion`)
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

### 🟠 H-32 — Migración de la vista macro a perspectiva pseudo-3D por calles

- **Estado:** en curso, avance sustancial. Dirección documentada en el
  design bible (`03_CITY_TERRITORY_AND_GROWTH.md`,
  `08_VISUAL_UI_AND_ASSET_GUIDELINES.md` "Ciudad macro (perspectiva por
  calles)", `04_CITIZENS_PROFESSIONS_AND_HEROES.md` "Cámara-sigue",
  `10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md` "Cámara y mundo caminable").
  Prototipos aislados validados (`game/scenes/prototypes/`,
  `game/scripts/Prototypes/`): `StreetDepthProjection.cs` (matemática de
  escala/convergencia por profundidad, punto de fuga relativo al jugador —
  no a un centro fijo del mundo), `MacroStreetWorld.cs` (navegación
  cuantizada validada con placeholders), `RealCityStreetPreview.cs`
  (proyección validada con datos reales de solo lectura, sin escribir el
  save). Integración real: `MacroStreetLiveView.cs` es ahora la **vista
  macro por defecto** en `CityPrototype.tscn` (F9 / botón "Perspective" en
  `MacroActions` alternan a la vista plana como respaldo), conectada al
  `CityWorldController` real. Clic en edificio completado abre el
  `BuildingDetailView` real; recolección de madera portada (árboles
  individuales vía `ParcelGrid.NaturalResourceLot`, mismo `ResourceActionMenu`
  reusado como instancia propia; la simplificación "sin animación de viaje
  del héroe" quedó superada el 2026-07-27 — ver el avance de esa fecha). Se resolvió un hallazgo estructural:
  `ModalHost`/`ExpeditionPanel`/`MigrantPanel`/`BuildingPlotStage`/
  `ConstructionPlacementOverlay`/`Center` (`EmptyPanel`/`ConstructionPanel`)
  eran hijos exclusivos de `CityMacroView` — un hijo no puede ser visible si
  su padre no lo es, así que quedaban invisibles con la perspectiva activa.
  Se reparentaron como hermanos bajo `ScreenContent`
  (`CityMacroView.cs`/`CityPrototype.cs`/`CityPrototype.tscn` actualizados,
  sin tocar `OrthogonalParcelTerrain.cs`/`BuildingPlotStage.cs`/
  `ConstructionPlacementOverlay.cs`). Encontrado y corregido después: `_plotStage`/
  `_emptyPanel`/`_offlineReport` seguían refrescándose en segundo plano
  independientemente de qué vista estuviera activa, interceptando clics
  sobre la perspectiva — `MacroStreetLiveView` ahora los oculta al activarse
  y usa el método público `CityMacroView.OnReturnedToCity()` para
  restaurarlos al volver a la vista plana.
- **Prioridad:** 🟠 Alta — el usuario pidió explícitamente completar la
  migración, no solo este slice.
- **Categoría:** arquitectura / presentación / gameplay
- **Afecta:** `game/scripts/Prototypes/*.cs` (nuevos), `CityMacroView.cs`,
  `CityPrototype.cs`, `CityPrototype.tscn`, design bible §03/04/08/10.
- **Avance 2026-07-27 (plano de calles + hero real + ruteo):** corregidos
  los dos defectos de integración detectados en la auditoría del mismo día:
  F9 ahora alterna en ambos sentidos (antes `_UnhandledInput` retornaba con
  el nodo oculto y F9 solo funcionaba perspectiva→plana; sigue bloqueado
  durante onboarding y con una vista de detalle abierta) y la vista plana ya
  no resucita sus overlays sobre la perspectiva por tick/evento de dominio
  (`CityMacroView.Refresh`/`RestoreChronicleVisibility`/`OnWorldTickAdvanced`
  ahora se autoguardan por visibilidad; la perspectiva conserva el
  re-ocultado defensivo). Además, a pedido del usuario, la perspectiva
  adopta el plano de calles definido (bible §08 + lectura de corredores de
  H-26): la calle es la banda frontal libre de cada fila de lotes, así que
  edificios y árboles anclan su base DETRÁS de su calle (ya no montados
  sobre ella); los árboles usan los mismos tiles Kenney de la vista plana
  (`ResourceTree.AtlasRegionRect`, filtro Nearest, ~44 px base escalados por
  profundidad) con cursor de hacha en hover y el `ResourceActionMenu`
  reanclado al espacio de `ScreenContent` (antes abría desplazado por la
  banda del HUD); el avatar naranja se reemplazó por el carrier canónico del
  fundador (`CitizenSpriteBank`, escala 0.25 macro × escala de profundidad,
  anclado a los pies, walk/idle/slash reales); y Gather ya no resuelve
  instantáneo: `StreetRoutePlanner` (nuevo, puro, sin Godot) planifica una
  ruta cuantizada por calles — lateral por la calle actual hasta un cruce
  viable, cruce escalonado de a una calle, y así hasta el árbol — donde un
  cruce solo es viable por los huecos que dejan las construcciones/árboles
  de la banda intermedia; W/S manual respeta la misma regla (aviso cuando
  está bloqueado). 11 tests nuevos (`StreetRoutePlannerTests` ×8,
  `StreetDepthProjectionTests` ×3 — primera cobertura de la proyección).
  Build limpio, 447/447 tests, headless boot verificado con el save real.
- **Avance 2026-07-27 (bug real de clic-a-gather + piso con tiles +
  construcción/placement migrados):** el usuario reportó que el hover sobre
  un árbol sí mostraba el cursor de hacha pero el clic no abría las
  opciones de gathering. La causa raíz no era de `MacroStreetLiveView`:
  `GameUiShell/ScreenContent` (el `Control` padre de ambas vistas macro)
  nunca tuvo `mouse_filter` seteado, así que usaba el default `Stop` —
  eso capturaba todo mouse motion/click sobre el área de juego a nivel de
  GUI, antes de que llegara a `_UnhandledInput`. Sus hermanos
  (`Background`, `Center`, `CityStatusPanel`, `MacroActions`,
  `BuildingDetailView/DetailBackground`) sí tenían `mouse_filter = 2`
  (Ignore); `ScreenContent` se quedó fuera de ese patrón. Diagnosticado
  comparando `_Input` (recibía los eventos) contra `_UnhandledInput` (casi
  nunca los recibía) con clics OS simulados vía
  `tools/Capture-VisualMatrix.ps1 -NormalizedClicks`, confirmado y
  corregido con una línea en `CityPrototype.tscn`
  (`mouse_filter = 2` en `ScreenContent`). Esto también arregla
  (retroactivamente) el clic sobre edificios completados: el `Hecho` previo
  para ese punto de H-32 estaba verificado solo por lectura de código, no
  por un clic real, y probablemente nunca funcionó en juego interactivo.
  A pedido explícito del usuario ("la vista anterior ya no es de mi
  interés… migrar en su totalidad, adaptado, el sistema de parcela y
  construcción con placement, y el piso con tiles respetando la
  perspectiva"): (1) el piso de cada calle ahora se dibuja como tiles
  individuales (`DrawTiledFloor`, granularidad
  `ParcelGrid.TilesPerStandardLot`) con el mismo patrón/colores de
  `OrthogonalParcelTerrain.RebuildGround` (`#385a3d`/`#3f6343`, misma
  fórmula de alternancia `(i*3+fila*5)%11`), cada tile proyectado
  individualmente por profundidad — reemplaza el rect sólido único
  anterior; (2) sistema de construcción con placement migrado por completo
  a la perspectiva: `ConstructionMenuButton`/`ConstructionPanel`/
  `ModalHost` ahora también son manejados por `MacroStreetLiveView` (cada
  handler compartido con `CityMacroView` se autoguarda por `Visible`, mismo
  patrón que `Refresh()`), y un nuevo modo de selección de lote
  propio — equivalente a `ConstructionPlacementOverlay` pero sin depender
  de `CalculateParcelRect` — proyecta cada `ConstructionLot` disponible en
  su posición calle/lateral real (mismo mapeo que `AddPlot`), lo dibuja
  como marcador clickeable con resalte al seleccionar, y reutiliza los
  botones reales `Confirm placement`/`Cancel` (ESC también cancela).
  Confirmar llama a `TryAuthorizeConstruction` real; cancelar reabre el
  panel de blueprint, igual que el flujo plano. Verificado en vivo con
  clics OS simulados de principio a fin: abrir Construction → elegir Build
  Farm → seleccionar lote (resalte amarillo) → Confirm placement → el
  nuevo proyecto aparece en la calle elegida y el HUD muestra
  `Build 0/960`. Confirmado que estas pruebas no tocan el save real:
  `CityWorldController.PersistenceWritesEnabled` se desactiva con el flag
  `--wog-visual-capture` que el harness siempre inyecta. Build limpio,
  447/447 tests, headless boot con el save real verificado.
- **Alcance explícitamente NO cubierto en el avance anterior** (retiro de la
  vista plana sigue siendo un paso deliberado y posterior, no implícito en
  "migrar la funcionalidad"): `CityMacroView`/`OrthogonalParcelTerrain`/
  `BuildingPlotStage`/`ConstructionPlacementOverlay` y los tests/matriz
  visual que asumen su geometría siguen intactos como respaldo de código
  (ya no hay UI para volver a esa vista — ver avance siguiente). Tampoco se
  migró: Recon/expediciones, Citizens/roster, Menu/pause — esos paneles son
  modales UI-agnósticos de vista y ya funcionan igual desde ambas (no
  requerían migración).
- **Avance 2026-07-27 (segunda ronda, corrección de calidad tras feedback en
  juego real):** el usuario jugó el avance anterior y reportó seis problemas
  concretos. Todos corregidos:
  1. **Piso "plano" y de una sola fila** — el piso anterior dibujaba una
     banda delgada de `RoadHeightPx=20px` por calle, dejando un hueco gris
     entre calles (donde vive el lote de 3 tiles de profundidad con el
     edificio/árbol) sin ningún tile debajo. `DrawTiledFloor` ahora dibuja
     las `ParcelGrid.TilesPerStandardLot` (3) sub-filas de profundidad de
     cada calle como piso continuo, cada sub-fila con sus propios bordes de
     profundidad (`StreetDepthProjection.RowScreenY` en los límites
     `street+k/3`) y su propia escala horizontal — el piso ahora narrows/
     recede tile por tile, sin huecos, y `roadTop` (donde anclan
     edificios/árboles) pasa a ser el borde lejano de la ÚLTIMA sub-fila
     (`depth=street+1`, justo donde empieza la siguiente calle) en vez de
     una banda arbitraria pequeña — refleja que el lote vive detrás de su
     calle con la profundidad real de 3 tiles.
  2. **Pathfinding bordea todo el conjunto de árboles** — causa real:
     `StreetRoutePlanner.Plan` buscaba el cruce viable más cercano a la
     posición LATERAL ACTUAL del héroe (el origen), no al destino; con una
     fila densa de árboles esto hace que el héroe cruce cerca de donde
     empezó y luego camine TODA la distancia lateral restante sin ninguna
     evitación adicional, leyéndose como si bordeara el grupo entero.
     Corregido: cada cruce ahora se busca cerca de `toLateral` (el destino
     final), así el héroe apunta hacia el objetivo desde el principio.
     Nuevo test de regresión `Plan_PrefersCrossingNearDestination_NotNearOrigin`
     que falla con el comportamiento anterior y pasa con el corregido.
  3. **El citizen hace pop-out/in en vez de mostrar el desplazamiento** —
     la motion cuantizada corría en `_PhysicsProcess`, cuyo delta puede
     desincronizarse del framerate realmente renderizado (Godot ejecuta
     varios pasos de física de "catch-up" entre dos frames renderizados si
     el framerate de render cae), colapsando visualmente varios pasos de
     8px en un solo frame — se ve como un salto. Movido a `_Process`
     (delta siempre igual a lo que se renderiza), mismo patrón que
     `CitizenSpriteCarrier` ya usa para su propia interpolación.
  4. **Assets de construcción eran rectángulos cafés** — ahora usa los
     mismos placeholders reales que la vista plana
     (`BuildingArt.GetTexturePath`: `home_idle.png`/`quarry_idle.png`/
     `farm_idle.png`), con un tinte gris (`UnderConstructionModulate`) para
     proyectos en obra; sin cambios de asset, solo consumidos también desde
     la perspectiva.
  5. **Sin vista de detalle de edificios** — resultó ser el MISMO bug de
     `ScreenContent.mouse_filter` corregido en el avance anterior: verificado
     en vivo que el clic sobre un edificio completado SÍ abre
     `BuildingDetailView` (con asignación de citizens/producción) y que
     "Back to city" regresa correctamente a la perspectiva. Se encontró y
     corrigió un bug relacionado en el camino: `BuildingDetailView.OnBackPressed`
     llamaba directamente a `CityMacroView.OnReturnedToCity()` (herencia de
     cuando la vista plana era la única), lo que resucitaba la vista plana
     por encima de la perspectiva cada vez que se volvía de un detalle. Esa
     llamada directa se quitó; `CityWorldController.ReturnToCity()` ya
     dispara `SelectionChanged`, que `MacroStreetLiveView` ya escucha
     correctamente.
  6. **Zoom con scroll del mouse no funciona** — no existía ninguna
     implementación de zoom en ninguna de las dos vistas. Implementado
     zoom cuantizado (pasos discretos de `ZoomStep=0.15`, rango
     `[0.7,1.6]`, nunca un slider continuo, coherente con la preferencia de
     motion cuantizada) vía `Scale` del propio `Node2D`, preservando el
     punto de fuga (`CenterX,BaseY`) fijo en pantalla al hacer zoom. Esto
     requirió convertir clic/hover de espacio global a local
     (`ToLocal(...)`) antes de comparar contra los rects, y el ancla del
     menú de gather a `ToGlobal(...)` antes de convertir al espacio de
     `ScreenContent` — ambos eran asunciones válidas solo mientras
     `Scale==1`.
  - **Además, a pedido explícito ("no pretendo volver a la otra vista, esta
    será la que se quedará")**: se quitó el botón "Perspective" de
    `MacroActions` y el atajo F9 de `MacroStreetLiveView`. La perspectiva
    es ahora la única vista macro alcanzable desde la UI/teclado; el código
    de la vista plana permanece intacto solo como respaldo de los tests y
    la matriz visual (ver punto 5 de próximos pasos).
  - Verificado en vivo con clics OS simulados: piso con profundidad visible
    sin huecos, clic en árbol → menú de gather → Gather → recolecta con la
    ruta corregida; clic en edificio → `BuildingDetailView` → "Back to
    city" → perspectiva (no vista plana). Build limpio, **448/448 tests**
    (1 nuevo: el test de regresión del pathfinding), headless boot con el
    save real verificado.
- **Avance 2026-07-27 (tercera ronda, tiles reales + bug de profundidad
  inconsistente):** el usuario jugó de nuevo y reportó dos problemas más
  sobre el piso con tiles del avance anterior:
  1. **Tiles "planos", no trapezoidales** — los tiles se dibujaban con
     `DrawRect` usando una única `horizontalScale` por fila (misma
     profundidad para ambos bordes), dando rectángulos uniformemente
     escalados en vez de trapezoides reales con lados convergentes hacia
     el punto de fuga. `DrawTiledFloor` ahora usa `DrawColoredPolygon` con
     4 vértices propios por tile (borde cercano vs. lejano, cada uno con su
     propia `HorizontalScale` evaluada en su propia profundidad), dando el
     trapecio real (base más ancha, lado superior más angosto, lados
     inclinados) pedido — estilo Pole Position/Out Run. Como
     `HorizontalScale` es función pura de la profundidad, el borde lejano
     de una sub-fila coincide exactamente con el borde cercano de la
     siguiente (y con el de la calle siguiente), así que los tiles calzan
     sin costuras.
  2. **Edificios/árboles "fuera del plano", se desplazan al caminar
     lateralmente** — causa real: el ancla de edificios/árboles usaba
     `depth=street` (el borde cercano de la calle, para la posición X) pero
     `roadTop` (el borde lejano del lote completo, `depth=street+1`, para
     la posición Y) — dos profundidades DISTINTAS para X e Y del mismo
     sprite. Como el escalado horizontal depende de la profundidad, esto
     hacía que el edificio se posicionara con la escala X de una
     profundidad que no correspondía a su propia posición Y, y el
     desajuste crecía proporcionalmente con `_heroLateral` — por eso solo
     se notaba al desplazarse lateralmente, coincidiendo con el reporte de
     "el pathfinding se rompió" del usuario (en realidad el pathfinding
     estaba bien; el sprite se dibujaba una calle más adelante de donde
     realmente ocupaba/bloqueaba). Corregido: nuevo `AnchorDepth(depth)` =
     `depth + 0.5 tile` (el baseline cerca del frente del lote, como pidió
     el usuario, no al fondo) usado para AMBOS ejes en la misma llamada a
     `Project(...)`, eliminando el parámetro `roadTop` por completo de
     `DrawStreetRow`/`DrawPlacementLots`.
  - Verificado en vivo: piso con trapecios reales visible (forma
    triangular/de calle clásica, no un rombo de rectángulos apilados);
    clic en árbol lejano lateral → Gather → tras esperar a que la ruta
    complete (varios clics de espera adicionales, ya que la primera
    captura fue demasiado temprana y no mostraba avance), la cámara sigue
    al héroe y los edificios permanecen sobre su fila de tiles sin
    flotar, incluso con un desplazamiento lateral considerable. Build
    limpio, 448/448 tests, headless boot con el save real verificado.
- **Avance 2026-07-27 (cuarta ronda, pixel-art + pathfinding multi-fila +
  bug real de asignación de citizens):** tres reportes más del usuario:
  1. **Trapecio matemáticamente perfecto contrasta con el pixel art** —
     `DrawColoredPolygon` da un lado inclinado perfectamente suave
     (anti-aliased), leyéndose como arte vectorial en vez de pixel art.
     Reemplazado por `DrawPixelStaircaseTrapezoid`: aproxima el trapecio
     como una "escalera" de franjas horizontales pequeñas, cada una un
     rect plano (ancho constante dentro de la franja, sin interpolar), con
     vértices redondeados a una grilla de 4 px (`PixelStepPx`) — así el
     lado inclinado avanza a saltos (como el piso perspectivo de un juego
     pixel-art real, ej. Pole Position/Out Run de 8-16 bits) en vez de una
     diagonal matemáticamente lisa.
  2. **Pathfinding sigue rodeando filas de árboles** (mejoró pero no se
     resolvió del todo con el fix de la ronda anterior) — causa adicional:
     cada cruce entre calles se optimizaba de forma INDEPENDIENTE (banda
     por banda), así que con varias filas de árboles de por medio, el
     héroe podía terminar haciendo zigzag entre huecos en posiciones
     laterales distintas por fila. `StreetRoutePlanner.Plan` ahora primero
     intenta un ÚNICO corredor lateral que atraviese TODAS las bandas
     intermedias simultáneamente (cerca del destino); solo si ningún
     lateral único las despeja todas cae al método banda-por-banda
     anterior (que sí puede zigzaguear). Nuevo test
     `Plan_PrefersASingleCorridorAcrossMultipleBands_NoZigzag`. Esto reduce
     el zigzag para el caso común pero **no lo elimina del todo** — un
     navmesh real (ver pregunta del usuario sobre S-1.2/S-1.5 abajo)
     seguiría siendo la solución robusta para el caso en que ninguna
     posición lateral despeje todas las filas a la vez.
  3. **Bug real: asignar el citizen a una obra hace que aparezca al
     instante y luego "se mueva en loop sin razón"** — encontrado el bug
     de arquitectura de fondo: `EnsureHeroCarrier` nunca consultaba
     `hero.CurrentAssignment`; en cada tick de mundo (muy frecuente)
     forzaba al carrier compartido de vuelta a estado `Macro` y lo
     posicionaba según la navegación LIBRE del jugador (`_heroStreet`/
     `_heroLateral`), sin relación alguna con dónde estaba realmente
     asignado el citizen — exactamente la "doble instancia" que el usuario
     sospechaba, aunque no era una instancia duplicada del flyweight
     (`CitizenSpriteBank` sigue siendo una sola instancia por citizen,
     ese trabajo previo sigue vigente) sino DOS SISTEMAS peleando por
     posicionar la MISMA instancia compartida en cada tick. Corregido con
     seguimiento de cambio (`_lastKnownAssignment`): al detectar una
     asignación NUEVA, se planea una ruta (mismo motor de
     `StreetRoutePlanner` que gather) desde la posición actual hasta la
     calle/lateral de la obra, y al llegar se asienta en idle — igual que
     la vista plana, donde un worker asignado se muestra en su lugar de
     trabajo en la vista macro, no vagando. Mientras está asignado, W/S y
     el movimiento lateral manual quedan bloqueados (el fundador está
     ocupado, no puede pasear). Verificado en vivo: asignar → volver a la
     ciudad → el citizen camina gradualmente hacia la granja en capturas
     sucesivas con más tiempo de espera → posición IDÉNTICA entre 8 y 20
     clics de espera adicionales, confirmando que se asienta y no queda en
     loop.
  - **Sobre la pregunta del usuario (¿ayudaría un addon/tool nativo de
    S-1.2/S-1.5?):** no para el bug #3 — ese era un problema de propiedad/
    coordinación entre `MacroStreetLiveView` (esta vista) y el resto de
    sistemas sobre quién posiciona el carrier compartido, no algo que un
    plugin de pathfinding o de FSM resuelva; el fix tenía que vivir en el
    código de esta vista y ya se hizo. Para el bug #2 (pathfinding), la
    respuesta es matizada: S-1.2 (`NavigationServer2D`) ya está
    implementado, pero para la VISTA PLANA (`MacroCitizenActivity` vía
    `NavigationServerPathfinder`) — la perspectiva usa su propio
    `StreetRoutePlanner`, un heurístico voraz separado. Adoptar un navmesh
    real (misma idea que S-1.2, aplicada a la perspectiva) SÍ resolvería
    el zigzag entre filas de forma más robusta que la heurística de
    "corredor compartido" que se acaba de agregar — es un candidato de
    mejora futura razonable, pero implica modelar el mundo de la
    perspectiva como malla 2D real y hornearla, un esfuerzo aparte (no
    incluido en este avance). S-1.5 (FSM de comportamiento) tampoco aplica
    aquí: modela estados del citizen (Idle/Working/Resting/etc.), no la
    propiedad del sprite compartido.
  - Build limpio, 449/449 tests (2 nuevos), headless boot con el save real
    verificado.
- **Avance 2026-07-27 (quinta ronda, pathfinding directo + bug de dominio
  real en auto-liberación de workers):** dos reportes más, ambos con causa
  raíz encontrada y corregida:
  1. **El citizen camina hacia el punto de asignación pero no queda
     asignado — se queda "en la entrada"** — el usuario acertó que esto NO
     era un problema de esta vista: causa real en el DOMINIO
     (`game/scripts/Domain/Building.cs`), afecta también a la vista
     plana. `Building.TickMaxStockWatch()` cuenta ticks consecutivos con
     `Stock >= MaxStock` y libera a los workers (M-17) al llegar a
     `MaxStockReleaseCooldown` (6); el contador solo se resetea cuando el
     stock CAE por debajo del máximo — algo que nunca ocurre mientras no
     hay nadie asignado consumiéndolo/produciendo. Una granja que ya había
     sido vaciada una vez por este mecanismo (stock estancado en el tope,
     contador ya en 6+) reasignaba un citizen NUEVO y el vigilante lo
     liberaba de nuevo en el siguiente tick — antes de que el nuevo worker
     produjera nada. Corregido: `Building.TryAssign` ahora resetea el
     contador a 0 en cada asignación exitosa — un worker recién asignado
     siempre obtiene la ventana de gracia completa. Dos tests nuevos en
     `BuildingTests.cs`, incluida la regresión exacta del bug
     (`TryAssign_AfterPriorAutoRelease_GetsAFreshCooldownWindow`).
     Verificado en vivo con el save real: asignar al citizen a la granja
     que antes lo expulsaba de inmediato ahora lo mantiene asignado,
     produciendo (`Rate: 2 Food/tick`, stock bajando de 20/20 a 19/20 y
     subiendo de nuevo) en vez de "Waiting for contributors".
  2. **"¿Por qué no caminar directo entre los árboles si es más rápido?"**
     — el usuario cuestionó directamente el diseño de evitación de
     obstáculos: dado que el movimiento LATERAL dentro de una calle nunca
     evitó árboles/edificios, ¿por qué el CRUCE entre calles sí lo hacía,
     si caminar derecho es más rápido y ya "funciona" visualmente? Aceptado
     el argumento: `StreetRoutePlanner` se reescribió por completo,
     eliminando toda la infraestructura de evitación (`Interval`,
     `IsCrossingBlocked`, `FindViableCrossing`, `FindSharedCorridor` y el
     `_bandOccupancy`/`AddBandInterval`/`GetBandOccupancy` de
     `MacroStreetLiveView`, todo código muerto una vez quitada la
     evitación). El plan ahora es directo y diagonal: una calle cruzada
     por waypoint, con el lateral interpolado proporcionalmente al avance
     (converge hacia el destino a través de toda la secuencia de cruces en
     vez de cruzar en un lateral fijo y recién después deslizarse de
     lado). W/S manual también cruza siempre, sin el aviso de "Something
     blocks the way". 6 tests reescritos (de 10 anteriores; la cobertura
     de evitación ya no aplica).
  - Build limpio, 447/447 tests, headless boot con el save real
    verificado.
- **Avance 2026-07-27 (sexta ronda, corrección de la ronda anterior +
  minimalismo de UI + animación de entrada a edificios):** el usuario
  corrigió la ronda 5: NO quería quitar la evitación de obstáculos —
  quería que se REFINARA para la nueva geometría, y manualmente movía al
  héroe por los huecos visibles entre assets. Además pidió un menú de
  gather más minimalista y esa misma revisión para la vista de detalle de
  edificios, más una animación de zoom con paneo de cámara al entrar a un
  edificio.
  1. **Evitación de obstáculos restaurada y con el bug real corregido** —
     `StreetRoutePlanner` recupera `Interval`/`IsCrossingBlocked`/
     `FindViableCrossing`/`FindSharedCorridor` (versión de la ronda 3:
     corredor compartido multi-banda con fallback banda-por-banda) y
     `MacroStreetLiveView` recupera `_bandOccupancy`/`AddBandInterval`/
     `GetBandOccupancy`. La causa real de "rodea la fila entera" no era
     la evitación en sí: `CrossingScanStepPx` (30 px, un tercio de
     `LotUnitPx`) divide exactamente 90 px (la separación entre árboles),
     así que el escaneo caía siempre en el mismo offset relativo a CADA
     árbol de la fila — si fallaba en encontrar el hueco de ~18 px junto a
     un árbol, fallaba junto a todos, forzando la búsqueda a escapar la
     fila COMPLETA en vez de cruzar entre dos árboles adyacentes. Bajado a
     6 px — lo bastante fino para no saltarse ningún hueco realista.
     Nuevo test `NarrowGapBetweenAdjacentTreesInADenseRow_IsFound_NotSkippedOver`
     con una fila de 5 árboles que reproduce exactamente el bug (falla con
     scanStep=30, pasa con 6). W/S manual recupera el aviso "Something
     blocks the way" al intentar cruzar un tramo sin hueco.
  2. **Menú de gather minimalista** — `ResourceActionMenu.tscn`: se quitó
     el título "Tree" (redundante, el jugador ya sabe qué clicó) y la
     línea de regeneración (movida a tooltip del label de reserva); Gather
     y Close ahora van lado a lado en una fila compacta en vez de
     apilados; el panel bajó de 190px a 148px de ancho mínimo y de
     márgenes 12/10 a 8/6.
  3. **Mismo repaso en la vista de detalle** — `ProductionPanel` mostraba
     "Farm — Food" duplicando el título "Farm (Food)" que ya muestra el
     header de `BuildingDetailView`; ahora dice solo "Production" (rótulo
     de sección estático, sin duplicar el nombre del edificio). Eliminada
     la clave de localización `ui.production.title` (sin más usos) de
     en.po/es.po; catálogo revalidado y `messages.pot` regenerado
     (`tools/Test-LocalizationCatalog.ps1 -UpdateTemplate`).
  4. **Animación de zoom + paneo al entrar a un edificio** —
     `UiMotion.RevealBuildingEntry` (nuevo): escala `BuildingDetailView`
     desde 0.72 hasta 1.0 alrededor de un `PivotOffset` — la posición en
     pantalla donde se clickeó el edificio, no el centro — así el punto
     clickeado queda fijo mientras el resto del contenido "crece" a su
     alrededor, leyéndose como que la cámara empuja hacia ese lugar
     específico en vez de un zoom genérico centrado. `MacroStreetLiveView`
     pasa el origen del clic vía el nuevo `BuildingDetailView.SetEntryOrigin`
     justo antes de `SelectBuilding`. Sin nuevos ejes/estados: reutiliza el
     mismo patrón de `Tween` cuantizado que `UiMotion.RevealModal` ya
     usaba.
  - Build limpio, 452/452 tests (2 nuevos: la corrección de granularidad
    del pathfinding y la fila densa de árboles), headless boot con el
    save real verificado. Verificado en vivo: menú de gather compacto con
    Gather/× lado a lado; clic en edificio → zoom hacia el punto
    clickeado → vista de detalle con "Production" sin duplicar el nombre.
- **Avance 2026-07-27 (séptima ronda, gather menu realmente minimalista +
  cursor sin redundancia + cierre por clic afuera):** el usuario refinó el
  pedido anterior — el botón Gather seguía dejando espacio en los bordes,
  el texto "Gather" debía ser solo tooltip (icono de hacha únicamente), el
  cursor de hacha sobre el botón era redundante con el icono del propio
  botón (debía mostrar la manito estándar, como cualquier elemento
  interactivo), y el botón "Close" debía desaparecer a favor de cerrar con
  clic afuera del panel.
  1. **Botón Gather solo-icono con tooltip** — `ResourceActionMenu.tscn`:
     `GatherButton` ahora tiene `text=""` (antes "Gather" visible) con
     `tooltip_text="Gather"`, `SizeFlagsHorizontal=Fill` para ocupar el
     ancho completo sin huecos laterales. `CloseButton`/el `HBoxContainer`
     "Actions" se eliminaron por completo — el `Gather` queda como único
     hijo de `Content`.
  2. **Cierre por clic afuera + Escape, sin botón "Cerrar"** —
     `MacroStreetLiveView.TryClick` ahora oculta `_actionMenu` al inicio
     si está visible (un clic que llega a este método nunca cayó SOBRE el
     menú, ya que su propio filtro de mouse Stop lo habría consumido antes
     — así que llegar aquí ya significa "afuera"); si el clic también
     acierta un árbol/edificio distinto, la lógica normal reabre para ese
     nuevo objetivo justo después. `_UnhandledInput` añade Escape
     (`ui_cancel`) como cierre alternativo, mismo patrón que
     `ConstructionPlacementOverlay`.
  3. **Cursor sin icono duplicado** — encontrada la causa real: el
     autoload `CursorController` ya da a TODO `BaseButton` un cursor de
     "manito" distinto por defecto (`OnNodeAdded` + `RestoreSurfaceCursor`)
     — el pedido del usuario de "manito en todo lo interactivo" ya estaba
     implementado globalmente. El bug era que `UseGatherCursor()` (llamado
     al pasar el mouse sobre un árbol) sobreescribe TANTO `Arrow` como
     `PointingHand` con la imagen del hacha — y como el menú abierto
     intercepta el mouse a nivel de Control (su propio filtro Stop), el
     movimiento del mouse desde el árbol hacia el botón nunca vuelve a
     pasar por `MacroStreetLiveView.UpdateTreeHover`, así que el override
     de hacha quedaba pegado indefinidamente sobre el botón — mostrando el
     hacha dos veces (cursor + icono del botón). Corregido: `OpenGatherMenu`
     llama a `ClearTreeHover()` justo al abrir, restaurando el cursor
     estándar antes de que el mouse llegue al botón.
  - Build limpio, 452/452 tests, headless boot con el save real
    verificado. Verificado en vivo: "40 wood remains" en una sola línea,
    botón Gather (solo icono de hacha) ocupando el ancho completo sin
    huecos laterales, panel bajado a 130px de ancho mínimo.
- **Avance 2026-07-27 (octava ronda, selección con panel HUD + clic
  izq/der separado + acción realmente sin marco):** el usuario adjuntó una
  imagen de referencia (dos botones cuadrados solo-icono, sin marco ni
  texto, flotando sobre un recurso) y pidió separar dos responsabilidades
  que hasta ahora compartía el clic izquierdo: **seleccionar** (ver
  información) vs. **actuar** (gather). Nuevo modelo de interacción, solo
  para la vista perspectiva (la vista plana es fixture/legado, sin tocar):
  1. **Clic izquierdo = seleccionar.** `MacroStreetLiveView.TryClick` ya
     no abre el menú de gather al acertar un árbol — llama a
     `SelectTree(tree)`, que puebla un nuevo panel de HUD persistente
     (`SelectionInfoPanel`, esquina inferior izquierda) con el ícono real
     del árbol (recortado del mismo atlas Kenney vía
     `ResourceTree.CreateRegion`), "Tree" como título, madera disponible
     (`ui.resource.wood_remains`, reutilizada) y la fecha/hora de
     regeneración calculada reutilizando el mismo formateador que ya usa
     el reloj del HUD superior (`SimulationTimeText.FormatLocalized(tick
     actual + TicksUntilRegeneration)` — sin reinventar el cálculo de
     día/hora). Clic en edificio sigue igual (abre `BuildingDetailView`
     directo — ya es una superficie de info más completa que un panel de
     esquina). Clic en terreno vacío llama a `ClearSelection()` — el panel
     se oculta. `RefreshPlots()` refresca el panel si el árbol
     seleccionado sigue vivo (nuevos ticks pueden cambiar la madera
     restante) o lo deselecciona si ya no existe (unidad agotada).
  2. **Clic derecho = actuar.** Nuevo caso en `_UnhandledInput` para
     `MouseButton.Right`; `TryRightClick` es el único lugar que abre
     `ResourceActionMenu` (antes vivía en el clic izquierdo). También
     selecciona el árbol (mantiene el panel de info sincronizado con lo
     que se está accionando).
  3. **`ResourceActionMenu` sin marco, de verdad.** El pedido anterior ya
     había quitado el texto "Gather" y el botón "Close", pero seguía
     siendo un `PanelContainer` con borde/tema `OverlayPanel` alrededor de
     un solo botón — exactamente el "marco innecesario" que la imagen de
     referencia mostraba que NO debía existir. Reescrito: la clase ahora
     extiende `IconButton` directamente (`ResourceActionMenu : IconButton`)
     y la escena (`ResourceActionMenu.tscn`) tiene como raíz un `Button`
     de 40×40 — sin `PanelContainer`, `MarginContainer` ni labels. Las
     etiquetas de reserva/disponibilidad que antes vivían aquí se movieron
     al nuevo panel de selección (más apropiado: son info, no parte de la
     acción); `Open(...)` perdió los parámetros `reserve`/
     `ticksUntilRegeneration` en consecuencia. La vista plana
     (`OrthogonalParcelTerrain.OnTreePressed`, fixture-only) se actualizó
     al mismo llamado de 6 argumentos para seguir compilando.
  4. **Nuevas claves de localización**: `ui.selection.tree_title` ("Tree"/
     "Árbol") y `ui.tree.regrows_at` ("Regrows {0}"/"Vuelve a crecer {0}")
     en `en.po`/`es.po`; `messages.pot` regenerado (341 IDs, 111 claves en
     runtime).
  5. **Nueva capa de overlay**: `OverlayLayers.SelectionInfo = 9` (entre
     `ContextMenu` y `Chronicle`), documentando dónde vive este panel en
     el catálogo de capas.
  6. **Herramienta de captura extendida**: `tools/Capture-VisualMatrix.ps1`
     ahora acepta un prefijo `R:`/`L:` en `-NormalizedClicks` (p. ej.
     `"R:0.5,0.6"`) para simular clic derecho — antes solo soportaba clic
     izquierdo, insuficiente para verificar este cambio y cualquier
     interacción de clic derecho futura.
  - Verificado en vivo con capturas reales (clics OS simulados, juego
    pausado para descartar eventos automáticos del mundo interfiriendo
    con la ventana de verificación): clic izquierdo en árbol → panel de
    selección aparece con "Tree / 40 wood remains / Regrows Day 70 ·
    00:00", sin abrir el menú de gather; clic derecho en el mismo árbol →
    aparece solo el botón-icono de hacha (sin marco), panel de selección
    se mantiene sincronizado; clic en edificio tras tener un árbol
    seleccionado → abre `BuildingDetailView` con su animación de entrada
    normalmente; clic en terreno vacío tras seleccionar → oculta tanto el
    panel de selección como el botón de acción si estaba abierto. Build
    limpio, 452/452 tests, headless boot con el save real verificado.
  - **Nota de proceso**: durante la verificación, una secuencia de dos
    clics con el mundo SIN pausar produjo un falso positivo (un panel
    modal grande aparecía tras el segundo clic sin relación aparente con
    su posición) — resultó ser un evento autónomo del mundo (no una
    regresión de este cambio); pausar la simulación antes de la secuencia
    de clics eliminó el ruido y confirmó el comportamiento real.
- **Avance 2026-07-27 (novena ronda, construcciones con el mismo modelo
  select/act + zoom de entrada movido al MAPA y escalonado + toggle de
  cámara libre/seguimiento + terreno menos empinado):** el usuario jugó de
  nuevo y pidió cuatro cosas en un solo turno:
  1. **"Lo mismo para las construcciones."** Clic izquierdo en un edificio
     ya no abre `BuildingDetailView` directo — llama a
     `SelectBuildingPlot(buildingId)`, que reutiliza
     `_controller.GetBuildingDetailSnapshot(...)` (ya existía, sin nueva
     plumbing) para poblar el mismo `SelectionInfoPanel` con el ícono real
     del edificio, su `FullDisplayLabel` como título y "N/M workers" (o
     "N/M resting" para Home) como detalle — nuevas claves
     `ui.selection.building_workers`/`ui.selection.building_home`. Clic
     derecho en un edificio es ahora la única forma de "entrar"
     (`BeginBuildingEntry`, ver punto 2). El ícono del panel
     (`TextureRect`) pasó de `StretchMode.Keep` (tamaño nativo — con un
     edificio grande se desbordaba fuera del panel) a
     `KeepAspectCentered`/`FitWidthProportional`, para que tanto el sprite
     16×16 de un árbol como una textura de edificio mucho más grande
     encajen igual en la caja de 40×40.
  2. **El zoom de entrada vive en el MAPA, no en la vista de detalle, y es
     escalonado.** El usuario señaló correctamente que el zoom+paneo al
     entrar a una construcción animaba `BuildingDetailView` (un `Control`
     de UI) en vez del mundo — y que se sentía "muy lineal" en vez de
     escalonado como el resto del movimiento. Ambos problemas compartían
     una causa: `UiMotion.RevealBuildingEntry` era un `Tween` continuo con
     easing Quad sobre el `Scale` de la VISTA, no de la cámara del mapa.
     Solución: `MacroStreetLiveView.BeginBuildingEntry` empuja la cámara
     (este mismo `Node2D`) hacia la posición del edificio clickeado en
     exactamente `BuildingEntryZoomSteps` (5) pasos discretos a la cadencia
     de 12 Hz compartida (`ZoomTowardPivot`, la generalización con pivote
     arbitrario de lo que `AdjustZoom` ya hacía con el punto de fuga fijo)
     — el mismo "sentir escalonado" que el desplazamiento del citizen y de
     calle. Solo al completarse los 5 pasos se llama
     `_controller.SelectBuilding(...)`. `BuildingDetailView` ya no anima
     su propio `Scale`/`PivotOffset` — `UiMotion.RevealBuildingEntry` se
     reemplazó por `UiMotion.FadeIn` (solo opacidad); se eliminaron
     `SetEntryOrigin`/`_pendingEntryOrigin` de `BuildingDetailView` (dead
     code una vez que el pivote vive y se consume enteramente en el mapa).
     El zoom se resetea (`ResetZoom`) cada vez que la vista se desactiva,
     para que volver a la ciudad no la deje con el zoom pegado.
  3. **Toggle de cámara libre/seguimiento** (documentado en el design
     bible §04 "Cámara-sigue" y ya validado en un prototipo aislado —
     `WalkableWorldCamera.cs` — pero nunca conectado a la vista en
     producción). Nuevo botón "Follow founder"/"Free camera" en
     `MacroActions` + tecla F. Arquitectura: el punto de fuga ahora lee de
     `CameraLateral`/`CameraDepthAnchor` (propiedades calculadas) en vez
     de `_heroLateral`/`_depthAnchor` directamente; en modo seguimiento
     (default) devuelven exactamente los valores del fundador — CERO
     cambio de comportamiento respecto a antes. En modo libre devuelven
     `_freeCameraLateral`/`_cameraDepthAnchor`, un estado
     independiente, movido por las mismas teclas (W/S, flechas) pero SIN
     las validaciones de "el fundador está ocupado" (una cámara libre no
     es un cuerpo caminando — el fundador sigue su propia ruta/IA en
     segundo plano, ajeno a hacia dónde mira la cámara). El fundador se
     proyecta como cualquier otro sprite del mundo en modo libre
     (`depth = _depthAnchor - CameraDepthAnchor`, `lateral = _heroLateral
     - CameraLateral`) en vez de fijo siempre en el centro. El modo
     construcción/placement fuerza temporalmente el seguimiento (los lotes
     se renderizan alrededor del fundador) y restaura el modo previo al
     terminar.
  4. **Terreno menos empinado, sin tocar el pixel-art.** El usuario notó
     que los tiles más lejanos al punto de fuga se ven "estirados" y pidió
     reducir la "altura"/ángulo del terreno — explícitamente SIN afectar
     la pixelación que le gustó. Como la técnica de "escalera de píxeles"
     (`DrawPixelStaircaseTrapezoid`) solo consume las coordenadas que le
     da `StreetDepthProjection`, ajustar esa proyección no la toca en
     absoluto. Cambios en `StreetDepthProjection.cs`:
     `VerticalDepthFactor` 0.85→0.90, `HorizontalDepthFactor` 0.80→0.87
     (la brecha entre ambos controla cuánto se distorsiona el aspecto de
     cada tile con la profundidad — se redujo de 0.05 a 0.03, manteniendo
     la propiedad requerida por los tests de que horizontal siga
     encogiendo más rápido que vertical), `BaseRowSpacingPx` 96→80,
     `HorizonY` 80→200 (menor recorrido vertical total = inclinación más
     plana). El efecto es más notorio cuantas más filas de calles haya
     visibles — en el save actual (una ciudad pequeña, ~4 filas) el cambio
     es sutil pero medible; queda abierto seguir afinando estas
     constantes con una ciudad más grande.
  - **Lección de proceso importante**: al verificar con clics OS
    simulados, una secuencia "clic derecho, luego clic izquierdo" resultó
    consistentemente en que el clic izquierdo NUNCA llegara a
    `_UnhandledInput` (0/5 intentos, con o sin pausa, con delays de hasta
    2s) — mientras que clic-izquierdo-tras-clic-izquierdo y clic-derecho
    en solitario funcionan de forma fiable. Diagnosticado como una
    limitación del propio harness (`mouse_event` con flags
    RIGHTDOWN/RIGHTUP parece dejar a Windows creyendo que el botón derecho
    sigue "presionado" para eventos sintéticos posteriores) — NO un bug
    del juego: se confirmó visualmente que clic derecho abre el botón de
    gather/hace zoom y entra al edificio correctamente cada vez; solo la
    verificación automatizada del clic SIGUIENTE a un clic derecho no es
    fiable con esta técnica. Ver [[verify-clicks-with-real-clicks]] para
    la entrada de memoria actualizada con este caso.
  - Build limpio, 452/452 tests, headless boot con el save real
    verificado. Verificado en vivo: selección de edificio (ícono +
    "Basic Shelter (Rest)" + "0/3 resting"), entrada con zoom escalonado
    en el mapa seguido de transición a `BuildingDetailView`, botón de modo
    de cámara alternando correctamente entre "Follow founder"/"Free
    camera" con su tooltip localizado.
- **Próximos pasos (orden sugerido):**
  1. Clic en un proyecto en construcción en curso desde la perspectiva
     (hoy abre el panel de construcción solo desde la vista plana; con el
     sistema de placement ya migrado, este es el siguiente hueco obvio).
  2. Texto de guía de estado vacío ("Select a tree and choose Gather...")
     equivalente en la perspectiva, para una partida nueva desde cero (no
     bloquea el save actual, que ya tiene ciudad construida).
  3. Paridad del Chronicle/reporte offline en la perspectiva: con los
     guards nuevos, el reporte y el log viven solo en la vista plana; la
     perspectiva necesita su propia superficie (o compartir la existente
     de forma explícita, no por accidente de refresh).
  4. Resolver el anclaje de edificios/complejos con `LotHeight > 1`
     (hoy se anclan a su calle más cercana al visor — simplificación
     documentada, no la asignación final).
  5. **Completed 2026-07-28:** physically removed the flat route and its
     geometry-only tests. Onboarding, construction and active fixtures now
     target `MacroStreetLiveView`; no parallel visual fallback remains.
- **Criterios de aceptación:** ciudad completa navegable y jugable
  (construcción, asignación, gather, expediciones) únicamente desde la
  perspectiva, sin regresión funcional respecto a la vista plana anterior, y
  con la matriz de regresión visual/tests actualizados a la nueva geometría.
- **Riesgo:** alcance grande tocando código de producción probado; cada
  paso debe verificarse con `dotnet test` (448/448 hoy) y un smoke real
  windowed antes de continuar al siguiente.
- **Lección de proceso:** el veredicto "Hecho" de un flujo de clic no se da
  por bueno solo con lectura de código o con headless boot — requiere un
  clic real (interactivo o simulado por OS) verificado hasta el efecto de
  dominio. El bug de clic-a-gather y el de "sin vista de detalle" fueron el
  MISMO bug estructural (`ScreenContent.mouse_filter`), descubierto solo
  por reporte directo del usuario jugando, no por la auditoría previa del
  mismo día.

---

## 3. Pendientes

### 🟡 M-25 — Gramática visual de motion y feedback causal

- **Estado:** primer corte implementado; pendiente firma visual humana y
  feedback de importancia grande.
- **Prioridad:** 🟡 Media
- **Categoría:** polish / UI / presentación
- **Afecta:** `ModalHost.cs`, `PauseMenu.cs`, `ConstructionPanel.cs`,
  `ResourceActionMenu.cs`, `MacroBuildingView.cs`, `BuildingPlot.cs`,
  `OfflineReportPanel.cs` y un componente C# compartido de transiciones
  (`AttentionBanner.cs` ya no existe — eliminado el 2026-07-26).
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
- **Avance 2026-07-27 — vista de perfil de héroe traducida:** el usuario
  señaló que a `HeroProfileView` le faltaba traducción completa; estaba
  100% en inglés hardcoded (título, encabezados de sección, disclaimer de
  linaje, todas las líneas de formato) y, más de fondo, todo el contenido
  DINÁMICO que muestra viene de `ProfileCatalog.cs` (dominio puro,
  ~85 strings: resumen y forma de aprender de cada uno de los 8 linajes,
  10 aptitudes, 12 familias profesionales, 6 afinidades elementales, 5
  estilos de combate, 6 preferencias de arma, 16 rasgos de personalidad, 8
  orientaciones políticas, 6 posturas espirituales) — nunca pasaba por
  `UiText`.
  - Texto estático de `HeroProfileView.cs` envuelto con
    `UiText.Get`/`Format` (20 claves nuevas bajo `ui.hero_profile.*`).
  - Contenido dinámico: **intento inicial equivocado** — envolver
    `ProfileCatalog.DisplayName(...)` con `UiText.Get` directamente dentro
    de `HeroProfileSnapshot.From(...)` (que vive en namespace raíz
    `WorldofGoses`, no en `.Domain`, así que compila bien) rompió
    `UiSnapshotTests.HeroProfileSnapshot_ProjectsPresentationData` con un
    **crash real** (no solo un test fallido) — ese test construye el
    snapshot directamente en un proceso xUnit sin motor Godot, y
    `UiText.Get` llama a `Godot.TranslationServer.Translate(...)`, que no
    existe fuera del engine. Corregido: `HeroProfileSnapshot` se revirtió
    a devolver texto crudo en inglés (sigue siendo Godot-free y testeable
    tal como antes); la traducción se aplica en `HeroProfileView.Render()`
    — la misma capa que ya posee cada otra llamada a `UiText` en el
    proyecto — vía un nuevo helper `JoinLocalized` (traduce cada elemento
    de una lista antes de unirlos con coma) y `UiText.Get(...)` en cada
    string individual (afinidad elemental, estilo de combate, orientación
    política, postura espiritual). El nombre propio del linaje (p. ej.
    "Ardhen") NO se traduce — es un nombre, no una descripción.
  - `GenderId` (enum, sin helper de display name propio) se traduce vía
    `UiText.Get(hero.Gender.ToString())` — mismo patrón "el texto en
    inglés es la clave" que ya usan `"Gather"`/`"Cancel"` en el proyecto;
    nuevas claves `"Feminine"`/`"Masculine"`.
  - Añadidas ~107 entradas msgid/msgstr nuevas a `en.po`/`es.po`
    (135 claves runtime totales, 453 IDs en `messages.pot` tras
    regenerar). Las traducciones de linaje/aptitud/afinidad/rasgo se
    escribieron a mano (no son autogeneradas) — traducciones de
    contenido, no solo de UI.
  - Verificado en vivo: la pantalla de perfil de héroe con locale español
    (el save de prueba ya arranca en `es` — confirmado también que el
    botón nuevo de cámara del H-32 ya sale como "Seguir al fundador" en
    ese idioma) muestra "Perfil del héroe", "Rol: Héroe", resumen y forma
    de aprender del linaje Vaelun en español, "Aptitudes personales" /
    "Afinidades profesionales" con sus valores traducidos
    ("Orientación, Adaptabilidad, Observación", etc.). Build limpio,
    452/452 tests (el crash de `UiSnapshotTests` NUNCA debió llegar a
    verificación en vivo — se detectó corriendo la suite completa antes de
    dar por cerrado el cambio), catálogo de localización válido.
  - **Nota para futuras traducciones de contenido de dominio:** cuando un
    record/snapshot vive fuera de `Domain/` pero es exercised directamente
    por tests xUnit (buscar en `tests/` antes de asumir), tratarlo como si
    fuera dominio puro a efectos de `UiText`/Godot — la traducción de
    datos catalogados debe aplicarse en la vista, nunca en el snapshot.
- **Avance 2026-07-27 — barrido completo de textos sin traducir en el
  resto de pantallas.** El usuario pidió revisar si quedaban más textos sin
  traducir además del perfil de héroe. Se lanzó una auditoría (fork) sobre
  todo `game/scripts/` (excluyendo `Domain/`) que encontró ~15 archivos con
  literales en inglés sin pasar por `UiText`. Corregidos, de mayor a menor
  alcance en el loop jugable:
  - **`AssignmentPanel.cs`**: título "Workers", encabezados
    "Assigned"/"Available", resumen de conteo, botones "Remove"/"Assign" +
    tooltips, estados vacíos.
  - **`ConstructionPanel.cs`** (el archivo con más texto suelto): título y
    descripción del blueprint (con **dos ternarios** cuyo string literal
    NO fue detectado por el script de "claves faltantes" hasta una segunda
    pasada — ver nota de proceso abajo), tooltips de los tres botones de
    autorizar (shelter/farm/quarry, incluyendo ternarios anidados),
    descripción del progreso, "Basic Shelter completed", encabezados
    Assigned/Available + tooltips de fila, y los switches completos de
    `ConstructionStopCause`/`ConstructionAuthorizationOutcome`.
  - **`BuildingDetailView.cs`**: resumen de "Home" con pluralización
    correcta (antes concatenaba una "s" en inglés directo al string, lo
    cual nunca funcionaría en español — ahora son 3 claves separadas
    `capacity_empty`/`capacity_resting_one`/`capacity_resting_many`),
    mensaje de asignación rechazada, y el título del edificio
    (`FullDisplayLabel`, ver nota de `DisplayName`/`ResourceLabel` abajo).
  - **`AssignmentOutcome` duplicado**: `ConstructionPanel.cs` y
    `BuildingDetailView.cs` tenían el MISMO switch privado
    `FormatAssignmentError` copiado y pegado — extraído a
    `AssignmentErrorText.cs` (helper compartido nuevo), eliminando la
    duplicación de las 7 claves de traducción.
  - **`ProductionPanel.cs`**: placeholders iniciales antes del primer
    `Refresh()` ("Pause"/tooltip, "Reactive policy", "Min"/"Max",
    "Resting site — no production").
  - **`VisibleWorkerSlot.cs`** / **`Ui/PanelHeader.cs`**: tooltips
    ("Click to remove this worker", "Close (ESC)" — este último se usa en
    CADA panel modal, alto impacto).
  - **`Prototypes/MacroStreetLiveView.cs`**: botón "Confirm placement",
    los 3 mensajes de gather (fundador no disponible/en expedición/ya
    asignado), "Gathered {0} wood.", mensaje de calle bloqueada, y el
    título del panel de selección de edificio (mismo `FullDisplayLabel`).
  - **`OfflineReportPanel.cs`** / **`TutorialOverlay.cs`**: placeholders
    iniciales de botones (Chronicle collapse, Skip/Next).
  - **`OnboardingView.cs`** (creación de héroe clásica, una vez por
    partida): barrido completo — título, los 5 `AddHeading`, todos los
    `AddSectionTitle` (incluyendo los que llevan contador dinámico, p. ej.
    "choose exactly 3 (N/3)"), el par Feminine/Masculine (gap ya conocido
    del audit), el bloque completo de `UpdateReview` (10 líneas, antes
    100% concatenación cruda en inglés) y `FormatLineage`.
  - **`AstralOnboardingView.cs`**: el gap conocido de
    Feminine/Masculine + `DescribeResult` (usaba literales en ESPAÑOL
    directo — "Aptitudes:", "Rasgos:", etc. — porque esta pantalla nació
    en castellano; ahora esos literales son la clave y `en.po` lleva la
    traducción al inglés) + las 4 frases de "pregunta falsa"
    ("Conservaré…", etc.) que nunca se cablearon a `UiText.Get`.
  - **Nombres de recursos/edificios sin traducir**: `DescribeMaterials`/
    `DescribeInputs` en `ConstructionPanel.cs` ya llamaban
    `UiText.Get(resource)` con el nombre del enum en minúsculas
    ("wood"/"food"/"stone"...) pero esas claves NUNCA existieron en el
    catálogo — se veían literalmente en inglés en medio de una oración en
    español. Añadidas. También se tradujo `BuildingDetailSnapshot.
    FullDisplayLabel` ("Farm (Food)" → "Granja (Comida)") reconstruyendo
    el formato en la vista (`UiText.Format("ui.building_detail.full_label",
    UiText.Get(DisplayName), UiText.Get(ResourceLabel))`) en los DOS sitios
    que lo consumían (`BuildingDetailView`/`MacroStreetLiveView`), en vez
    de traducir el string ya concatenado por el dominio.
  - **Bug real encontrado en la propia herramienta de validación**:
    `tools/Test-LocalizationCatalog.ps1` usaba un hashtable de PowerShell
    (`@{}`) para detectar `msgid` duplicados — PowerShell compara claves de
    string de forma **insensible a mayúsculas por defecto**, mientras que
    `Godot.TranslationServer` (en tiempo de ejecución real) SÍ distingue
    mayúsculas. Esto hacía que el validador reportara un falso positivo de
    "duplicado" en cuanto intenté añadir `"wood"` (minúscula, para uso
    dentro de una oración) junto al `"Wood"` (mayúscula) ya existente — dos
    claves legítimamente distintas para el mismo idioma. Corregido:
    `Read-PoCatalog`/`Read-PoTranslations` ahora usan
    `Dictionary<string,int>`/`Dictionary<string,string>` con
    `StringComparer.Ordinal` explícito, igual que ya hacía el `$allIds`
    del mismo script — sin este fix, cualquier futuro par
    mismo-texto-distinta-mayúscula habría fallado igual (o peor, en el
    caso de las traducciones, se habrían pisado silenciosamente sin ni
    siquiera un error).
  - **Nota de proceso — punto ciego real en el detector de claves
    faltantes**: el script de detección de claves usa una regex que solo
    encuentra `UiText.Get("literal")`/`UiText.Format("literal", ...)`
    cuando el argumento es un string literal INMEDIATO tras el paréntesis
    — `UiText.Get(condición ? "a" : "b")` no lo detecta en absoluto (ni el
    caso "a" ni el "b"), incluyendo ternarios anidados. Esto causó que
    varias claves de `ConstructionPanel.cs` (títulos/descripciones del
    modo Blueprint) quedaran SIN traducir en una primera pasada — se veían
    perfectamente bien en la lista de "claves faltantes" (porque no
    aparecían ahí en absoluto) pero seguían en inglés en pantalla. Se
    detectó solo al verificar visualmente el panel en español, NO por el
    validador. Lección: tras envolver texto con `UiText`, buscar
    manualmente patrones `UiText\.(Get|Format)\([a-zA-Z_!]` (paréntesis
    seguido de algo que no sea una comilla) en los archivos tocados, y
    SIEMPRE verificar visualmente en vivo — el validador de claves es una
    red de seguridad parcial, no prueba de que el texto realmente se vea
    traducido.
  - Build limpio, 452/452 tests, catálogo válido (580 IDs en plantilla,
    255 claves runtime). Verificado en vivo en español: panel de
    Construcción (título, descripción, requisitos con "madera"/"comida"
    traducidos, botones Construir granja/cantera), panel de Asignación
    dentro de `BuildingDetailView` (Producción, Política reactiva,
    Min./Máx., Trabajadores/Asignados/Disponibles, título "Granja
    (Comida)" completamente traducido).
  - Quedan sin tocar deliberadamente (fixture/legado, no alcanzable en
    juego): `CityMacroView.cs`, `ConstructionPlacementOverlay.cs`.

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
- **Avance 2026-07-27 — redirigido a la vista de perspectiva (la que
  realmente se juega).** Antes de tocar código se investigó (fork de
  auditoría) si el estado "implementado" de arriba seguía teniendo
  sentido, dado que H-32 (rondas anteriores de esta misma sesión) retiró
  la vista plana (`CityMacroView`) de la UI por completo. Confirmado:
  `MacroCitizenActivity`/`NavigationServerPathfinder` NO se ejecutan en
  ningún flujo de juego real hoy — `CityMacroView` queda oculta apenas
  `MacroStreetLiveView` activa la perspectiva (que corre después en el
  árbol de escena y gana), así que ese código solo lo ejercitan
  `tools/Capture-VisualMatrix.ps1` (fixtures) y los 5 tests que en
  realidad prueban `CardinalPathfinder`, no `NavigationServerPathfinder`.
  Terminar el refactor "tal como está descrito arriba" habría sido pulir
  una ruta muerta. En cambio, el propio `StreetRoutePlanner.cs` (el
  pathfinding real de la vista de perspectiva) ya documentaba en su
  propio comentario que un navmesh real sería "la mejora futura" sobre
  su heurística greedy de corredor-compartido/gap-más-cercano, que
  todavía puede zigzaguear cuando ningún lateral único cruza limpio
  TODAS las bandas intermedias a la vez. Se le preguntó al usuario y
  confirmó: redirigir el esfuerzo del navmesh hacia `StreetRoutePlanner`.
  - Nuevo `StreetNavigationServerPlanner.cs` (Godot-dependiente,
    `game/scripts/Prototypes/`) — espejo exacto del patrón ya validado de
    `NavigationServerPathfinder`: mapa/región propios, rebake completo en
    cada `Plan()` (una vez por comando de viaje — gather o nueva
    asignación —, nunca por tick). El espacio de la malla es (lateral,
    calle × 200px) — las calles se escalan a una unidad "real" en vez de
    índices enteros desnudos para que el cell-size de bake por defecto se
    comporte igual en ambos ejes. El "outline" transitable es un rect
    [min,max] × [callesRelevantes]; cada intervalo ocupado de una banda se
    agrega como "obstruction outline" ya inflado por `clearance` — solo
    se hornea el rango de calles relevante (origen/destino ±1), no la
    ciudad completa.
  - `StreetRoutePlanner.cs` (Godot-free, sigue intacto y con sus 11 tests
    originales sin tocar) ganó un método nuevo puro y testeable,
    `ConvertNavmeshPathToWaypoints`: convierte la polilínea cruda que
    devuelve `NavigationServer2D.MapGetPath` (que puede cortar en
    diagonal, cualquier ángulo) en la MISMA forma de `Waypoint` que ya
    consume `MacroStreetLiveView.AdvanceRouteTick` — "camina por esta
    calle hasta X, luego cruza" en cada calle intermedia — muestreando la
    lateral del camino real en cada cruce de calle entera. Esto es
    deliberado: el héroe nunca corta en diagonal a través de la malla
    (rompería la gramática de movimiento cuantizado/no-fluido), la malla
    solo decide QUÉ secuencia de posiciones laterales usar en cada
    cruce — decisión que la heurística greedy anterior no siempre
    acertaba en el caso multi-banda.
  - `MacroStreetLiveView.cs`: nuevo campo `_navmeshPlanner`
    (instanciado en `_Ready`, `Dispose()` en `_ExitTree`) + helper privado
    `PlanHeroRoute(...)` que reemplaza las dos llamadas directas a
    `StreetRoutePlanner.Plan` (`OnGatherRequested`,
    `BeginWalkToAssignment`): intenta el navmesh primero, cae al
    planificador greedy SOLO si el navmesh no encuentra ningún camino en
    absoluto (geometría totalmente sellada) — mismo espíritu de "una ruta
    de mejor esfuerzo vence a un héroe varado" que ya tenía el
    planificador original.
  - **Por qué mantener DOS planificadores en vez de reemplazar uno por
    otro**: `StreetRoutePlanner.cs` documenta explícitamente "Deliberately
    Godot-free so xUnit covers it directly" — llamar a
    `NavigationServer2D` (un singleton del motor) ahí adentro habría roto
    sus 11 tests existentes con el MISMO crash que ya se encontró dos
    veces esta sesión (ver [[localize-at-display-not-snapshot]]):
    `dotnet test` no tiene motor de Godot corriendo. Se replicó el patrón
    YA establecido en este mismo proyecto
    (`IPathfinder`/`CardinalPathfinder`/`NavigationServerPathfinder`):
    una implementación pura+testeada, una implementación real+sin tests
    directos (verificada por inspección de código + boot en vivo).
  - 5 tests nuevos para `ConvertNavmeshPathToWaypoints` (mismo calle, cruce
    recto sin ajuste lateral, cruce único con ajuste lateral, cruce
    multi-calle muestreando el punto exacto de cada frontera, y el
    fallback cuando el camino nunca alcanza la profundidad de una calle
    intermedia). 457/457 tests (452 + 5), build limpio, boot headless con
    el save real verificado sin errores de `NavigationServer2D`.
  - **Limitación de verificación honesta**: NO se pudo confirmar en vivo,
    vía clic simulado, que el navmesh realmente produce una ruta real en
    lugar de caer siempre al fallback — la secuencia "clic derecho para
    abrir el menú de recolectar, luego clic izquierdo en el botón" sigue
    sin poder automatizarse en este entorno (ver
    [[verify-clicks-with-real-clicks]]); se probó de nuevo con la API
    moderna `SendInput` además de `mouse_event`, con un evento de
    liberación redundante y reposicionamiento del cursor — mismo
    resultado: CUALQUIER clic después de un clic derecho, sin importar el
    objetivo o la API usada, no llega a `_UnhandledInput`. La confianza
    en la corrección se apoya en: (a) los 5 tests nuevos que cubren
    exactamente la lógica de conversión más propensa a errores de
    interpolación, (b) múltiples boots limpios sin ningún error de
    `NavigationServer2D` en el log (confirma que `MapCreate`/
    `RegionCreate`/bake no fallan), y (c) revisión de código cuidadosa del
    espejo con `NavigationServerPathfinder`. Pendiente: verificación
    interactiva real por el usuario, o una vía de automatización que no
    dependa de encadenar un clic tras un clic derecho.

#### Sub-ítem 3 — Terreno con textura real y biomas en la vista pseudo-3D

- **Redirigido 2026-07-27** (mismo patrón que S-1.2): el objetivo
  original de este sub-ítem — migrar `OrthogonalParcelTerrain.cs` a
  `TileMapLayer`/`TileSet` — apuntaba a la vista plana
  (`CityMacroView`), retirada de la UI desde H-32. Ese trabajo de
  2026-07-26 (TileMapLayer sobre el suelo plano) sigue en el código
  como respaldo de `tools/Capture-VisualMatrix.ps1`, pero no es
  alcanzable en juego. A pedido explícito del usuario, este sub-ítem
  se re-scope a la vista real y actual: `MacroStreetLiveView`, la
  "perspectiva por calles" pseudo-3D con proyección de profundidad
  (`StreetDepthProjection`) y suelo tipo staircase pixel-art
  (Pole Position/Out Run-style). No hay un nombre de industria más
  específico que ese; el propio bible ya usa "perspectiva por calles"
  y es tan preciso como cualquier término externo ("2.5D"/pseudo-3D
  scaling son sinónimos genéricos, no algo más concreto).
- **Por qué `TileMapLayer` NO aplica aquí:** cada tile del suelo de
  `DrawTiledFloor` es un trapecio recalculado en cada redraw a partir
  de la cámara (lateral offset + escala no uniforme por profundidad,
  `StreetDepthProjection.HorizontalScale`); no es una celda de grid
  con posición fija. `TileMapLayer`/`TileSet` asumen exactamente lo
  opuesto: una única transformación afín por capa y coordenadas de
  celda estáticas. Forzar `TileMapLayer` aquí requeriría un quad
  por-tile con transform propio recalculado cada frame — algo que
  `TileMapLayer` no expone — o degradar el proyector de profundidad.
  Migrar literalmente habría roto la perspectiva o el look
  "staircase" pixel-art que el usuario explícitamente pidió preservar.
- **Implementado 2026-07-27 — fase 1 (bioma + textura estática):**
  `DrawTiledFloor`/`DrawPixelStaircaseTrapezoid` ya no rellenan con
  color plano (`GroundTileColorA/B`); muestrean el mismo atlas Kenney
  que ya usan los árboles (`ResourceTree.TerrainAtlasPath`/
  `AtlasRegionRect`), vía `DrawTextureRectRegion` por cada stripe del
  staircase (cada stripe recorta su propia franja vertical de la
  región fuente de 16×16, no la textura completa estirada, para que
  el tile se lea coherente de punta a punta en vez de repetirse por
  stripe). `StreetGroundAtlasColumn(street)` asigna un bioma
  determinístico por calle ciclando Grass(col 5)/Dirt(col 6)/
  Stone(col 7) — sin estado nuevo de dominio/save, puramente
  presentacional, mismo espíritu que el comentario propio de
  `OrthogonalParcelTerrain` de que el arte de terreno nunca debe
  convertirse en estado de simulación. La columna "Dirt" se eligió
  deliberadamente igual a la que usará la fase 2 (desgaste), para no
  necesitar una cuarta textura cuando llegue el pisoteo procedural.
  El "alternate" checkerboard que antes elegía entre dos colores
  planos ahora elige entre `GroundAtlasRowA`/`RowB` (dos variantes del
  mismo material), preservando el hash de `OrthogonalParcelTerrain`.
  Verificado con captura real (`tools/Capture-VisualMatrix.ps1` /
  script de verificación equivalente): las 6 calles muestran
  grass/dirt/stone/grass/dirt/stone en profundidad, staircase y
  proyector intactos, edificios/árboles/héroe anclados correctamente.
  457/457 tests, build limpio.
  - **Bug real encontrado y corregido en el camino:** `((street % 3)
    + 3) % 3 switch { ... }` — sin paréntesis alrededor de todo el
    módulo — compila y corre sin error, pero el operando del `switch`
    se ató solo al literal `3` final, no a la expresión completa
    (`X % 3 switch {...}` se parsea como `X % (3 switch {...})`, no
    `(X % 3) switch {...}`). Producía un bioma visualmente incorrecto
    (franjas de agua turquesa en vez de tierra/piedra) sin ningún
    error de compilación — solo visible al capturar la vista real y
    comparar el RGB exacto contra el atlas. Corregido envolviendo el
    módulo completo entre paréntesis antes del `switch`. Ver
    [[verify-clicks-with-real-clicks]]: otro caso de "el código
    compila y los tests pasan" no siendo suficiente sin una captura
    visual real.
- **Implementado 2026-07-27 — fase 2 (desgaste procedural / caminos):**
  `Domain/TerrainWearGrid.cs` (sin `using Godot`) trackea un nivel de
  desgaste 0..1 por `(street, tileIndex)`, incrementado
  `WearPerTrample`(0.05) por pisada hasta cruzar `DirtThreshold`(0.5).
  `MacroStreetLiveView._terrainWear` se marca en los 3 puntos donde el
  héroe cambia `_heroStreet`/`_heroLateral`: `AdvanceRouteTick` (ruta
  automática de gather/asignación), `StepHeroStreet` y
  `TryStepHeroLateral` (pasos manuales) — vía un único helper
  `TrampleHeroTile()`/`TileIndexAtLateral(lateral)` (misma
  granularidad de tile que `DrawTiledFloor`, mismo espacio lateral
  "global" que ya usa `StreetRoutePlanner`). `DrawTiledFloor` sólo
  aplica el desgaste a `tileRow == 0` (la banda frontal caminable de
  la calle — el resto de la profundidad del lote, donde están
  árboles/edificios, nunca se pisa) y, si está desgastado, sustituye
  la columna de bioma por `DirtAtlasColumn` sin importar el bioma base
  — el mismo tile que usará "Dirt" como calle entera, reutilizado. Sin
  estado de dominio nuevo en `CityMacroSnapshot`/save: deliberadamente
  session-scoped (no `TerrainWearSave`, se resetea cada boot) — ver el
  comentario propio de `TerrainWearGrid` sobre por qué. Verificado
  visualmente sembrando desgaste sintético en una tile conocida
  (`street=0, tileIndex=5/6`) y confirmando el parche de tierra
  correcto en captura real, luego revertido antes de commitear — la
  única vía práctica dado que acumular 10 pisadas reales sobre el
  mismo tile vía clicks simulados no es viable con las limitaciones ya
  documentadas en [[verify-clicks-with-real-clicks]]. 4 tests nuevos
  (`TerrainWearGridTests`), 461/461 en total, build limpio.
- **Riesgo:** ninguno para la lógica de dominio — todo el cambio de
  fase 1 y 2 es presentacional dentro de `MacroStreetLiveView`/
  `TerrainWearGrid`, no toca `CityMacroSnapshot` ni la parcel grid. El
  desgaste no persiste entre sesiones (decisión deliberada, no un
  olvido); si en el futuro se quiere que sobreviva un reload, hace
  falta un `TerrainWearSave` en `Domain/Persistence/` análogo a los
  existentes.

#### Sub-ítem 4 — `MultiMeshInstance2D` para citizens

- **Redirigido 2026-07-27:** investigado a pedido del usuario, en el
  mismo espíritu que S-1.2/S-1.3 — pero acá el problema era más
  profundo que un target muerto. La premisa completa del sub-ítem
  ("con 30-50 citizens visibles el costo de instanciar un
  `PackedScene` por citizen se nota") no tenía nada que optimizar
  todavía: `MacroStreetLiveView` (la vista real) solo renderizaba al
  héroe; el único código que mostraba VARIOS citizens ambientales
  (`MacroCitizenActivity`) estaba conectado exclusivamente a
  `CityMacroView`, la vista plana ya retirada — el mismo patrón de
  H-32 orfanando código, ahora sobre una feature entera, no solo una
  técnica de render. El reclutamiento (`CityWorld.TryRecruitMigrant`)
  sí es real y ya funciona; la población crecía en el dominio sin
  nunca verse caminando por la calle.
- **Implementado 2026-07-27 — presencia ambiente de citizens:** el
  prerequisito que faltaba. `CityMacroSnapshot.CitizenItem` ahora
  lleva `Lineage`/`Gender`/`Appearance` (antes solo `HeroVisual` los
  tenía), necesarios para crear un `CitizenSpriteCarrier` por
  cualquier citizen, no solo el héroe.
  `MacroStreetLiveView.RefreshCitizenVisuals(snapshot)` — llamado
  junto a `EnsureHeroCarrier` en `RefreshPlots()` — agrupa los
  citizens no-héroe con `CurrentAssignment` (y no en expedición) por
  edificio, y para cada uno reusa exactamente la misma infraestructura
  que el héroe (`CitizenSpriteBank.Instance.GetOrCreate`/`Mount`,
  `VisualState.Macro`): aparecen al ser asignados, desaparecen al ser
  desasignados o partir de expedición — sin route-walking, se
  posicionan directamente en su lugar de trabajo (a diferencia del
  héroe, que sí camina hasta su asignación vía
  `BeginWalkToAssignment`). Varios workers en el mismo edificio se
  abren en abanico lateralmente (`WorkerLateralSpacingPx`) para no
  superponerse. `UpdateWorkerVisuals()` (llamado desde `_Draw()` junto
  a `UpdateHeroVisual()`) los ancla a la profundidad de la CALLE
  (`depth`, no `AnchorDepth(depth)` como el edificio) — medio tile
  más cerca del espectador que el propio edificio — para que se vean
  parados al frente, no tapados por el sprite del edificio.
  Verificado reclutando y asignando un citizen sintético en vivo
  (revertido después, no quedó en el save): visible parado frente a
  su lugar de trabajo, distinto y no superpuesto con el edificio. Sin
  cambios a `_pathfinder`/dominio; 461/461 tests, build limpio.
- **Por qué NO se tocó MultiMesh:** con el sistema recién nacido, el
  conteo de citizens visibles hoy sigue muy por debajo del trigger de
  20-25 documentado — no hay datos reales todavía para justificar la
  migración. `CitizenSpriteCarrier`/`LineageSpritePlayer` siguen
  reproduciendo 14 poses LPC horneadas en un único `AnimatedSprite2D`
  sin capa de "cuerpo base" separable; migrar de verdad seguiría
  requiriendo un shader con datos por instancia — un proyecto en sí
  mismo. Instanciar un `PackedScene`/`CitizenSpriteCarrier` por
  citizen sigue siendo lo correcto mientras el conteo sea bajo (mismo
  razonamiento de "no over-engineer" que ya se aplicó acá).
- **Reemplazo (sin cambios, sigue en pie tal como estaba):** un
  `MultiMeshInstance2D` con `MultiMesh.TransformFormat = Transform2D`,
  `UseColors = true` para tinte por linaje, `UseCustomData = false` (la
  pose de animación va por shader/AnimatedSprite separado).
- **Trigger:** cuando el número promedio de citizens visibles
  por escena supere 20-25 — ahora sí medible en la vista real, ya
  que existe presencia ambiente de citizens para contar. Antes: el
  custom instancing es suficiente.
- **Criterios de aceptación del primer slice (sin cambios):**
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
  cuerpo base. Ningún riesgo nuevo por la presencia ambiente en sí:
  es presentacional, no toca `CityWorld`/asignaciones.

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
- **Completado 2026-07-27 — transiciones de expedición:** a diferencia
  de S-1.2/S-1.3/S-1.4, este pase no necesitó redirect — es dominio
  puro, sin dependencia de qué vista esté viva. Verifiqué primero que
  `StartExpedition` exige `!hero.CurrentAssignment.HasValue`, y que
  por la propia lógica de `SetLocation` un citizen desasignado en casa
  siempre es `Idle` (nunca `Resting`/`Injured`, que requieren
  asignación) — así que el primer salto `Idle→Travelling` nunca se
  topa con una transición no catalogada. `Citizen.DispatchOnExpedition()`
  (nuevo, interno) encadena `Travelling` y `OnExpedition` en la misma
  llamada — no hay demora de viaje modelada todavía, así que
  `Travelling` se visita pero no se permanece en él; eso es un hueco
  honesto documentado, no un bug. `Citizen.ReturnFromExpedition()`
  (nuevo, interno) vuelve a `Idle`. `CityWorld` los llama: `hero.DispatchOnExpedition()`
  tras crear la expedición en `StartExpedition`; un nuevo helper
  privado `ReturnLeadFromExpedition(expedition)` (busca al líder por
  `LeadCitizenId` en `_citizens`) llamado desde `CancelExpedition` y
  desde los 3 desenlaces de `CompleteFinishedExpeditions`
  (retorno con migrante, retorno con suministros, fallo). Con esto:
  **8 de 9 transiciones documentadas conectadas** (antes 5). La única
  que sigue sin call site es `Working→Travelling` ("Cancelled
  assignment + new target") — no existe hoy un flujo de dominio que
  cancele una asignación y camine a un nuevo target en el mismo paso;
  queda catalogada pero no cableada, mismo criterio que antes. 3 tests
  nuevos (`CitizenBehaviorFsmTests.cs`: dispatch→OnExpedition,
  completar expedición→Idle, cancelar expedición→Idle), 464/464 tests,
  build limpio.
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

- **Estado 2026-07-27 (auditoría):** parcial y con una brecha sustantiva.
  El umbral real del harness es **40 ms con `Write-Warning`** (deliberado:
  la captura no debe perderse por un spike), no los 32 ms/`throw` que
  registró la entrada de S-1 del 2026-07-25. Más grave: el muestreo de
  30 "frames" mide los intervalos de `Start-Sleep` de PowerShell en el
  proceso host, no frames del engine — un spike real dentro de Godot es
  invisible para esta métrica. `docs/PERFORMANCE_BUDGETS.md` además
  contradice al script en tres puntos (dice "falla la captura/exit 1",
  menciona un memory profiler y un muestreo vía `Engine.GetFramesPerSecond`
  que no existen). Pendiente: medir frames reales (p. ej. volcando
  `Performance.TIME_PROCESS` desde una fixture) y reconciliar el doc.
- **Completado 2026-07-27 — frames reales + doc reconciliado:**
  `CityWorldController.SampleFrameTimeForVisualCapture()` (nuevo) llama
  `Performance.GetMonitor(Performance.Monitor.TimeProcess)` — el costo
  real por frame del engine — desde `_Process`, solo cuando
  `WOG_VISUAL_CAPTURE=1`, e imprime `[WOG-FRAME-TIME] <ms>` (tope de
  300 muestras, ~5s a 60fps, para no crecer el log sin límite si la
  ventana queda abierta). `Capture-VisualMatrix.ps1` ya no mide sus
  propios intervalos de `Start-Sleep`: espera a que el screenshot esté
  listo + 500ms extra, y toma las últimas 30 líneas `[WOG-FRAME-TIME]`
  del log real (`Select-String` + `double.TryParse` con
  `InvariantCulture`). **Bug real encontrado corriendo una captura de
  verdad (no leyendo código):** el juego formateaba el valor con la
  cultura del SO (`F3` sin cultura explícita); en una máquina con
  locale de coma decimal (como esta, es-*), cada muestra fallaba el
  parseo silenciosamente y `frame-time.json` volvía vacío. Corregido
  formateando con `CultureInfo.InvariantCulture` en el origen.
  Verificado: captura real produce 30 muestras numéricas válidas en
  `frame-time.json`, sin warnings. `docs/PERFORMANCE_BUDGETS.md`
  reconciliado en los 3 puntos que el guion no respaldaba: el chequeo
  es `Write-Warning` (nunca falla la captura), el mecanismo real es
  `Performance.Monitor.TimeProcess` vía el log (no
  `Engine.GetFramesPerSecond()`), y el memory profiler/
  `Mono.GetTotalMemory()` nunca existió — marcado explícitamente como
  no implementado en vez de aspiracional-como-si-ya-estuviera. 464/464
  tests, build limpio (el cambio de dominio es solo presentacional,
  ningún test nuevo necesario más allá de la verificación de captura
  en vivo).
- **Por qué:** un idle manager vive de la consistencia de frame a
  frame. Un spike de 50 ms en cualquier sistema rompe la sensación
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
  - Harness de autoprofile funcional, ejecuta en cada matriz — **hecho
    2026-07-27**, con muestras reales del engine, no del host.
  - Budgets definidos en `docs/PERFORMANCE_BUDGETS.md` — **hecho**,
    ya existía y quedó reconciliado con el guion real.
  - CI local (no en repo) alerta cuando un PR rompe el budget —
    **pendiente**, no implementado; hoy el `Write-Warning` solo se ve
    corriendo el harness manualmente.

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
- **Corrección 2026-07-27 (auditoría):** la frase anterior sobreafirma.
  `FlashLarge` tiene un único call site real: obra completada
  (`CityMacroView.EmphasiseCompletedBuilding`). Expedición retornada usa
  solo un toast (`Notifier.Show`) y ciudadano llegado no tiene ningún
  feedback (no existe `OnAnyCitizensChanged` en `CityMacroView`). Los dos
  call sites faltantes quedan bajo M-25 (§3), cuyo Estado ya los declara
  pendientes. `docs/CURRENT_STATUS.md` se corrigió en la misma pasada.
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

### 🟡 M-11 — Safe area aplicada de forma parcial e inconsistente

- **Cerrado:** 2026-07-27 (verificado completo el 2026-07-26; este cierre es
  el movimiento documental pendiente).
- **Cambió:** `SafeArea.cs`, `SafeAreaMarginContainer.cs`, `MacroActions.cs`,
  `CityStatusPanel.cs`, `OfflineReportPanel.cs` (todo ya en el árbol desde
  2026-07-25/26; sin cambios de código en este cierre).
- **Resumen:** `MacroActions` (anclado, no hijo de contenedor) aplica
  `SafeArea.ApplyOffsets` directo en script; `CityStatusPanel` (hijo del
  `VBoxContainer` `GameUiShell`, que ignora `Offset*`) envuelve su fila de
  chips en un `SafeAreaMarginContainer` interno — envolver el panel completo
  era lo que producía el wrapper gris; `OfflineReportPanel` envuelve en
  `_Ready`. Verificado por captura en 1024×576, 1280×720 y 1600×900 sin
  fondo gris ni overflow.

---

## 7. Canceladas / Superadas

*(Vacío — las entradas cerradas el 2026-07-22/24 se purgaron el
2026-07-27 según la política de dos días; Git conserva el historial.)*

---

## 8. Referencias cruzadas

- `docs/CURRENT_STATUS.md` — estado general del proyecto.
- `docs/UI_PATTERNS.md` — reglas de UI que toda mejora debe respetar.
- `docs/UI_AUDIT.md` — auditoría previa.
- `docs/world-of-goses-design-bible/` — fuente de verdad de diseño.
- `README.md §15` — founding hero y next proof.
