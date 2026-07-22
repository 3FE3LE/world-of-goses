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
- **2026-07-22** — Auditoría estructural de UI sobre la escena ejecutada a
  1280×720 y revisión estática de layouts, capas y controles dinámicos.
- Próxima revisión sugerida: después de cerrar C-9, H-11 y C-10.

---

## 1. Resumen rápido

| Prioridad | Pendientes | En curso | Bloqueados | Hechos | Cancelados |
| --------: | ---------: | -------: | ---------: | -----: | ---------: |
| 🔴        | 2          | 0        | 0          | 8      | 0          |
| 🟠        | 7          | 0        | 0          | 11     | 1          |
| 🟡        | 5          | 0        | 0          | 9      | 4          |
| 🟢        | 0          | 0        | 0          | 9      | 2          |

### Cola activa (orden sugerido)

1. **C-9** — Crear el shell transversal de pantallas y reservar el HUD.
2. **H-11** — Definir una política única de capas y oclusión.
3. **C-10** — Rehacer el layout responsive del building detail.
4. **H-12** — Contener correctamente el escenario visual de workers.
5. **H-13 / H-14** — Hacer scrollables el status bar y las listas de workers.
6. **H-15 / H-16 / H-17** — Corregir Chronicle, accesos macro y modales.
7. **M-11..M-15** — Safe area, overlays, plots, regresión y scroll seccional.

---

## 2. En curso

*(Vacío — mover aquí el primer ítem cuando se tome.)*

---

## 3. Pendientes

### 🔴 C-9 — Falta un shell transversal que reserve HUD y contenido

- **Estado:** Implementado; pendiente de cierre junto con la matriz visual del slice.
- **Prioridad:** 🔴 Crítica
- **Categoría:** arquitectura / UX
- **Afecta:** `game/scenes/CityPrototype.tscn`, `CityMacroView`,
  `BuildingDetailView`, `HeroProfileView`, `CityStatusPanel`.
- **Evidencia:** `CityStatusPanel`, `BuildingDetailView`, macro y perfil son
  hermanos full-rect. El detail compensa el HUD con `offset_top = 40`, mientras
  otras pantallas lo resuelven de otra forma. El status se dibuja después del
  detail y puede tapar su header; esto reproduce el caso reportado del botón
  "Back to city" detrás de la barra.
- **Corrección propuesta:** crear un `GameUiShell` con dos regiones explícitas:
  `StatusSlot` de altura canónica y `ScreenContent` expandible. Macro, detail y
  perfil ocupan solo `ScreenContent`; ninguna pantalla resta offsets a mano.
  Decidir explícitamente si onboarding/modales cubren también el status.
- **Criterios de aceptación:** ningún header queda debajo del HUD a 1280×720,
  1024×576 ni 1600×900; cambiar de pantalla no mueve el borde superior útil;
  no quedan offsets que dupliquen la altura del status.
- **Relacionados:** H-11, C-10, M-11.

### 🔴 C-10 — `BuildingDetailView` no tiene un presupuesto vertical responsive

- **Estado:** En curso; shell estable y scroll global retirado por decisión de UX.
- **Prioridad:** 🔴 Crítica
- **Categoría:** UX
- **Afecta:** `CityPrototype.tscn`, `BuildingDetailView.cs`,
  `VisibleWorkerSlots.cs`, `ProductionPanel.cs`, `AssignmentPanel.cs`.
- **Evidencia:** el contenido combina en un `HFlowContainer` un `Main` con art
  de 192 px, workers de 144 px y un ProductionPanel de altura variable, más un
  AssignmentPanel sin límite. No existe `ScrollContainer`. Cuando el flujo
  envuelve o crece, el contenido excede la altura disponible; reproduce el
  desborde reportado en Quarry.
- **Corrección propuesta:** header fijo dentro del shell; layout de dos columnas
  mientras haya ancho suficiente y una columna en viewport estrecho. La vista
  completa no hace scroll: únicamente listas o feeds con crecimiento no acotado
  pueden desplazarse dentro de su propia región. Definir mínimos reales en vez
  de depender implícitamente de `HFlowContainer`.
- **Criterios de aceptación:** todo el detail es alcanzable sin dibujar fuera
  del viewport; Back permanece visible; Quarry/Farm/Forest/Home funcionan con
  0, 1 y capacidad máxima de workers en las tres resoluciones de referencia.
- **Relacionados:** C-9, H-12, H-14.

### 🟠 H-11 — No existe una política única de capas y oclusión

- **Estado:** Implementado estructuralmente; pendiente de validación visual final.
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

### 🟠 H-12 — El escenario de workers usa coordenadas libres y no puede recortar

- **Estado:** Implementado estructuralmente; pendiente de validación visual final.
- **Prioridad:** 🟠 Alta
- **Categoría:** UX / arquitectura
- **Afecta:** `VisibleWorkerSlots.cs`, `VisibleWorkerSlot.cs`,
  `CitizenSpriteCarrier.cs`, `CitizenSpriteBank.cs`.
- **Evidencia:** `VisibleWorkerSlots` fija `Size` dentro de un `VBoxContainer`
  (el container puede sobrescribirla), coloca slots manualmente y calcula el
  sprite a `Y = 126` dentro de una región declarada de 144 px. El sprite vive
  fuera del subtree en un CanvasLayer, así que `clip_contents` no puede contenerlo.
- **Corrección propuesta:** convertir el stage en un Control con mínimo derivado
  de capacidad, anchors internos y clip/viewport explícito; separar label y
  área de sprite; hacer que el host del carrier respete el rect global del stage.
- **Criterios de aceptación:** ningún píxel o hitbox sale del panel de workers;
  1–3 workers se centran y reordenan sin solaparse; entrada/salida permanece
  contenida y no cruza el status ni el ProductionPanel.
- **Relacionados:** C-10, H-11.

### 🟠 H-13 — `CityStatusPanel` vuelve a saturarse con ancho reducido

- **Estado:** Implementado: modo compacto reactivo bajo 1150 px; pendiente matriz visual.
- **Prioridad:** 🟠 Alta
- **Categoría:** UX
- **Afecta:** `CityStatusPanel.cs`, `CityPrototype.tscn`.
- **Evidencia:** un solo `HBoxContainer` centra todos los chips con separación
  fija de 18 px. En la captura 1280×720 ya consume casi todo el ancho; Saved,
  textos largos o 1024×576 pueden recortar extremos. No hay scroll, compactación
  por breakpoint ni overflow affordance.
- **Corrección propuesta:** definir prioridades de chips y estados compacto/
  extendido; agrupar velocidad; permitir un overflow accesible o scroll
  horizontal deliberado. La altura del HUD debe salir del shell, no de texto.
- **Criterios de aceptación:** ningún chip se corta en las resoluciones objetivo;
  reloj, pausa y alertas críticas siempre permanecen visibles; Saved y datos
  secundarios degradan de forma predecible.
- **Relacionados:** C-9.

### 🟠 H-14 — Las listas de Assignment crecen sin scroll ni límite

- **Estado:** Implementado estructuralmente; pendiente de fixtures con listas largas.
- **Prioridad:** 🟠 Alta
- **Categoría:** UX
- **Afecta:** `AssignmentPanel.cs`, `AssignmentRow.tscn`,
  `BuildingDetailView.cs`.
- **Evidencia:** Assigned y Available son `VBoxContainer` directos dentro del
  panel. Cada ciudadano añade una fila de al menos 36 px y empuja todo el detail;
  el número de ciudadanos no tiene límite visual.
- **Corrección propuesta:** resumen y headers fijos; listas dentro de uno o dos
  `ScrollContainer` con altura compartida; preservar foco y hacer auto-scroll al
  elemento enfocado con teclado/gamepad.
- **Criterios de aceptación:** probar 0, 1, 6 y 20 ciudadanos sin crecimiento
  fuera del viewport; todas las acciones siguen alcanzables por foco.
- **Relacionados:** C-10.

### 🟠 H-15 — Chronicle ocluye plots y mezcla overlay con panel persistente

- **Estado:** Mitigado: inicia colapsado y solo la expansión explícita ocupa el overlay.
- **Prioridad:** 🟠 Alta
- **Categoría:** UX
- **Afecta:** `OfflineReportPanel.cs`, `CityPrototype.tscn`,
  `BuildingPlotStage.cs`.
- **Evidencia:** el panel está anclado bottom-right con rect fijo 360×320 y
  `z_index = 10`. En la captura 1280×720 tapa parte de Farm/Quarry y captura
  interacción sobre esa zona.
- **Corrección propuesta:** convertir Chronicle en dock/cajón que reserve ancho,
  o en overlay colapsado que se expanda modalmente y desplace/cubra de forma
  intencional. El stage de plots debe recibir el rect útil restante.
- **Criterios de aceptación:** ningún plot queda parcialmente oculto o
  inaccesible; collapsed/expanded tienen áreas y foco inequívocos.
- **Relacionados:** C-9, H-11.

### 🟠 H-16 — Accesos macro colocados con offsets absolutos

- **Estado:** Implementado y verificado a 1280×720; pendiente matriz estrecha.
- **Prioridad:** 🟠 Alta
- **Categoría:** UX
- **Afecta:** `HeroAccessButton`, `ConstructionMenuButton`,
  `CityPrototype.tscn`.
- **Evidencia:** los botones usan rects fijos `(16..176)` y `(216..416)` bajo
  una barra asumida de 40 px. No forman parte de un container ni responden a
  textos más largos, safe area o ancho reducido.
- **Corrección propuesta:** barra de acciones dentro del shell, con
  `MarginContainer` + `HBoxContainer`/wrap y componentes canónicos.
- **Criterios de aceptación:** no se solapan ni recortan a 1024×576; soportan
  navegación explícita y texto expandido sin offsets por callsite.
- **Relacionados:** C-9, M-11.

### 🟠 H-17 — Modales dependen de mínimos fijos sin fallback estrecho

- **Estado:** Implementado estructuralmente; pendiente matriz visual con texto largo.
- **Prioridad:** 🟠 Alta
- **Categoría:** UX
- **Afecta:** `ConstructionPanel.cs`, `ModalHost.cs`, `TutorialOverlay.cs`.
- **Evidencia:** Construction exige 720×420 y Tutorial 360 px de ancho; el host
  centra contenido pero no impone máximo relativo al viewport ni scroll. El
  footer y las opciones pueden salir del rect útil en pantallas bajas.
- **Corrección propuesta:** wrapper modal con máximo `viewport - safe margins`,
  body scrollable y header/footer fijos; un único componente para tamaños y
  close paths.
- **Criterios de aceptación:** construcción y tutorial caben y se pueden cerrar
  a 1024×576 y con textos de prueba 50% más largos.
- **Relacionados:** H-11, M-11.

### 🟡 M-11 — Safe area aplicada de forma parcial e inconsistente

- **Estado:** En curso; detail, onboarding y toast usan el componente canónico.
- **Prioridad:** 🟡 Media
- **Categoría:** arquitectura
- **Afecta:** `SafeAreaMarginContainer.cs`, macro actions, status, Chronicle,
  AttentionBanner, Notifier.
- **Evidencia:** detail/profile/onboarding usan márgenes o safe-area, pero HUD,
  botones macro y overlays se anclan directamente a bordes con offsets propios.
- **Corrección propuesta:** el shell calcula una sola área segura y expone slots
  para HUD/content/overlays; eliminar márgenes duplicados en consumidores.
- **Criterios de aceptación:** simular insets en los cuatro bordes y verificar
  que ninguna acción, alerta o texto crítico queda fuera.
- **Relacionados:** C-9, H-16, H-17.

### 🟡 M-12 — Banners, toasts y tutorial no comparten zonas de exclusión

- **Estado:** En curso; toast/banner separados y toast suprimido durante tutorial.
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

### 🟡 M-13 — Placeholders de plots dominan y desbalancean la ciudad

- **Estado:** Implementado; pendiente comparación visual final.
- **Prioridad:** 🟡 Media
- **Categoría:** polish / UX
- **Afecta:** `BuildingPlot.cs`, `BuildingPlotStage.cs`.
- **Evidencia:** en la captura 1280×720, cada Forest sin art ocupa 192×192 con
  la palabra FOREST a gran tamaño, mientras Shelter/Farm/Quarry usan siluetas
  mucho menores. La jerarquía visual no representa importancia ni hitbox real.
- **Corrección propuesta:** placeholder provisional del mismo peso óptico que
  el canvas final, label secundario y hitbox/spacing derivados del subject.
- **Criterios de aceptación:** cinco plots caben sin que placeholders tapen o
  desplacen la lectura de edificios reales; foco/hover muestran su área exacta.
- **Relacionados:** H-15.

### 🟡 M-14 — No existe una matriz de regresión visual para UI

- **Estado:** Checklist reproducible definido; capturas comparativas pendientes.
- **Prioridad:** 🟡 Media
- **Categoría:** arquitectura
- **Afecta:** verificación de escenas/UI.
- **Evidencia:** build, tests y boot headless no detectan overflow, oclusión ni
  foco perdido. Los defectos reportados sobreviven con 327 tests verdes.
- **Corrección propuesta:** checklist reproducible con capturas de macro,
  building detail, profile, construction y tutorial en 1280×720, 1024×576 y
  1600×900; fixtures para listas vacías/máximas y textos largos. Automatizar
  solo cuando exista un harness pequeño y concreto.
- **Criterios de aceptación:** cada PR de UI adjunta la matriz aplicable y
  compara rects/capturas; ningún cierre se basa únicamente en headless boot.
- **Relacionados:** todos los ítems de esta auditoría.

#### Matriz mínima por cambio de UI

| Vista/estado | 1024×576 | 1280×720 | 1600×900 | Casos obligatorios |
| --- | --- | --- | --- | --- |
| Macro | captura | captura | captura | ciudad vacía, cinco plots, Chronicle abierto |
| Building detail | captura | captura | captura | sin workers, workers máximos, listas largas |
| Hero profile | captura | captura | captura | texto normal y texto de prueba +50% |
| Construction modal | captura | captura | captura | blueprint, underway, footer envuelto |
| Tutorial/overlays | captura | captura | captura | tutorial + attention + intento de toast |

En cada celda aplicable se comprueban: rect completo dentro del viewport,
header y acción de cierre visibles, foco de teclado/gamepad, ausencia de
solapamientos, y scroll únicamente dentro de secciones de datos no acotados.
La revisión registra resolución, estado/fixture y resultado; una captura del
macro inicial no sustituye las demás vistas afectadas.

### 🟡 M-15 — Scroll solo en secciones de información no acotada

- **Prioridad:** 🟡 Media
- **Categoría:** arquitectura / UX
- **Afecta:** `BuildingDetailView`, `AssignmentPanel`, `OfflineReportPanel`,
  futuros roster e inventarios.
- **Contexto:** La primera solución de C-10 añadió scroll al cuerpo completo de
  Building Detail. La revisión visual confirmó que una vista principal completa
  debe sentirse estable, no como un documento desplazable.
- **Regla propuesta:** headers, acciones primarias, arte y composición principal
  permanecen fijos y deben caber en el shell. Solo una sección cuyo contenido
  crece por datos (Chronicle, Assigned/Available, roster) recibe
  `ScrollContainer`, con tamaño y affordance visibles.
- **Criterios de aceptación:** Building Detail no tiene scroll global; Chronicle
  y listas largas desplazan únicamente sus filas; foco de gamepad hace
  auto-scroll dentro de la sección sin mover el resto de la pantalla.
- **Relacionados:** C-10, H-14, H-15.

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
