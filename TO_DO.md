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
- Próxima revisión sugerida: tras cerrar M-14 (cross-cutting) o durante el
  próximo PR de UI.

---

## 1. Resumen rápido

| Prioridad | Pendientes | En curso | Bloqueados | Hechos | Cancelados |
| --------: | ---------: | -------: | ---------: | -----: | ---------: |
| 🔴        | 0          | 0        | 0          | 10     | 0          |
| 🟠        | 1          | 1        | 0          | 23     | 1          |
| 🟡        | 3          | 1        | 0          | 12     | 3          |
| 🟢        | 0          | 0        | 0          | 1      | 0          |

### Cola activa (orden sugerido)

1. **H-25** — Convertir los recursos naturales en estado persistente de parcela con unidades, agotamiento y regeneración.
2. **M-22** — Cerrar la integración selectiva de los assets descargados y el alcance real del menú.
3. **M-14** — Construir la matriz de regresión visual (cross-cutting; cierra los acceptance criteria visuales del resto).
4. **H-11** — Definir una política única de capas y oclusión.
5. **M-12** — `OverlayHost` con slots y prioridad para banners, toasts y tutorial.
6. **M-11** — Reintentar safe area para HUD y macro actions (enfoque alternativo: aplicar `Offset*` en el script, no vía wrapper).

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

### 🟠 H-25 — Recursos naturales aún dependen de `BuildingKind.Forest`

- **Estado:** En curso. El schema v8 introduce `CityParcel` y
  `NaturalResourcePatch` como estado persistente separado de construcciones.
  La migración v7 → v8 convierte cada Forest legacy en una parcela desbloqueada
  y un patch de madera, y la vista macro deriva sus árboles del patch. El schema
  v7 ya conservaba unidades de árbol estables y la
  última visita semántica del ciudadano (`forestId + unitId + logicalSlot`).
  Gather agota el
  slot seleccionado; tras refresh o reload, el trabajador permanece en ese
  lugar incluso cuando un tick retira el Forest agotado, y su marcador/nombre
  forman una sola representación. Las partidas v6 migran cada reserva agregada
  a unidades compatibles. Los refresh de construcción/producción preservan un
  viaje activo y solo reconstruyen la actividad después de ejecutar su callback
  de llegada.
- **Prioridad:** 🟠 Alta
- **Categoría:** dominio / territorio
- **Pendiente:** retirar el adaptador de almacenamiento
  `BuildingKind.Forest` (se conserva para que recetas y partidas actuales no
  pierdan madera), asignar parcelas a construcciones, balancear 40 wood por
  árbol y añadir regeneración/offline catch-up.
- **Criterios de aceptación:** parcela persistente mínima; patch de recurso
  separado de una construcción con detail view; unidades visibles derivadas de
  reserva; agotamiento que elimina la unidad seleccionada; ciclo de regeneración
  compatible con progreso offline; contrato reutilizable para piedra
  superficial y recursos posteriores.

---

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

---

## 3. Pendientes

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
- **Afecta:** `AttentionBanner.cs`, `Notifier.cs`, `TutorialOverlay.cs`,
  `OfflineReportPanel.cs`.
- **Evidencia:** todos se posicionan independientemente en top/bottom/center y
  pueden aparecer juntos. Attention además tiene anchors definidos en escena y
  vuelve a aplicar `BottomWide` en `_Ready`, mezclando dos fuentes de layout.
- **Corrección propuesta:** `OverlayHost` con slots y prioridad; toast stack,
  banner persistente y tutorial/modal declaran exclusión. Una sola fuente
  (escena o script) posee anchors y offsets.
- **Criterios de aceptación:** disparar save toast + error + attention + tutorial
  sin solapamiento ni captura accidental de input.
- **Relacionados:** H-11, M-11.

### 🟡 M-11 — Safe area aplicada de forma parcial e inconsistente

- **Estado:** Parcial. Implementado en `OfflineReportPanel` (envoltorio en `_Ready`) y `AttentionBanner` (envoltorio en `EnsureBuilt`). Pendiente: extender safe area a `CityStatusPanel` y `MacroActions`. Los intentos previos de envolver `CityStatusPanel` con `SafeAreaTopBar` o cambiar `MacroActions` a `SafeAreaMarginContainer` añadieron un `MarginContainer` visible con fondo gris por encima del HUD y se revirtieron. Necesita un enfoque distinto (probablemente `Offset*` en el script, no wrapper).
- **Prioridad:** 🟡 Media
- **Categoría:** arquitectura
- **Afecta:** `SafeAreaMarginContainer.cs`, macro actions, status, Chronicle,
  AttentionBanner, Notifier.
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
