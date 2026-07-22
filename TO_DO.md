# TO DO — Mejoras de Usabilidad y Gameplay

<!-- markdownlint-disable MD060 MD024 MD036 -->
> **Backlog vivo** del proyecto. Cada vez que se abra una sesión y se vaya
> a tomar una tarea de aquí, **se debe re-analizar** el contexto que la
> originó y verificar si sigue vigente (código cambió, otra mejora la
> volvió obsoleta, etc.). No se aborda nada sin esa relectura.
>
> Las tareas se mueven de sección cuando cambian de estado. **No se
> borra historia**: lo completado y lo cancelado se conserva con su
> fecha y motivo.

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

### Estados disponibles

| Estado               | Significado                                                  |
| -------------------- | ------------------------------------------------------------ |
| Pendiente            | Listo para tomar; está en cola.                              |
| En curso             | Alguien lo está trabajando en esta sesión.                   |
| Bloqueado            | Depende de otra cosa (externa o de otro ítem). Ver `Bloqueado por`. |
| Necesita reanálisis  | El contexto cambió; antes de tomarlo hay que re-leerlo.     |
| Hecho                | Implementado. Se conserva la fecha y un puntero al commit/PR. |
| Cancelado            | Ya no aplica. Se conserva la fecha y el motivo.              |

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
- Próxima revisión sugerida: tras cerrar las primeras 5 tareas prioritarias.

---

## 1. Resumen rápido

| Prioridad | Pendientes | En curso | Bloqueados | Hechos | Cancelados |
| --------: | ---------: | -------: | ---------: | -----: | ---------: |
| 🔴        | 0          | 0        | 0          | 8      | 0          |
| 🟠        | 0          | 0        | 0          | 11     | 1          |
| 🟡        | 0          | 0        | 0          | 9      | 4          |
| 🟢        | 0          | 0        | 0          | 9      | 2          |

### Cola activa (orden sugerido)

*(Vacía — las tareas S-1..S-4 fueron cerradas o superadas tras el reanálisis.)*

---

## 2. En curso

*(Vacío — mover aquí el primer ítem cuando se tome.)*

---

## 3. Pendientes

*(Vacío — no quedan tareas pendientes de esta auditoría.)*

---

## 4. Bloqueados

*(Vacío — mover aquí ítems que dependen de algo externo.)*

---

## 5. Necesita reanálisis

*(Vacío — mover aquí ítems cuyo contexto pueda haber cambiado desde la auditoría inicial.)*

---

## 6. Hechas

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

### 🟡 S-4 — Auditoría de ciclo de vida post-refactor

- **Cerrado:** 2026-07-22
- **Cambió:** `CitizenSpriteBank.PruneExcept`, validación de identidad visual y `docs/ARCHITECTURE.md §7b`.
- **Resumen:** La asignación sigue siendo perezosa y el banco conserva como máximo un carrier por ciudadano visualizado. Los carriers ajenos al mundo activo se eliminan y un ID reutilizado con linaje o género distintos reemplaza su visual anterior. No se atribuye una mejora de FPS: no existía un escenario perfilado que la justificara.

---

## 7. Canceladas / Superadas

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
