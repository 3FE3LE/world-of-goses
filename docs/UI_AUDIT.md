# UI Audit — Estado actual

> Auditoría de la capa de presentación actualizada el 2026-07-21 tras la
> migración a snapshots de macro/perfil, componentes reutilizables, safe area,
> foco inicial y layout responsive del detalle de edificio.
>
> Completa `docs/CURRENT_STATUS.md` § *Presentation, themes, and navigation*;
> la checklist de firma humana sigue requiriendo captura visual para los items
> que dependen del render final.

## Resumen de los cambios vigentes

| Pieza | Detalle |
| --- | --- |
| `default_theme.tres` | Añadida entrada base `Label/font = Pixelify Sans`, 18 px, cream. Cualquier `Label` sin variación explícita cae aquí — incluyendo el `Label` interno del popup de tooltip. |
| `Ui/StandardButtons.cs` | Factoría estática con `BackToCityButton()` — instanciado desde `Components/BackToCityButton.tscn` — y `ViewHeroButton()`. El componente Back es consumido por `BuildingDetailView` y por la factoría usada en `HeroProfileView`; View Hero es consumido por `ConstructionPanel` y mantiene las mismas propiedades que `HeroAccessButton`. |
| `Ui/ModalHost.cs` | Modal reusable: scrim semitransparente, `CenterContainer`, escucha de `ui_cancel` (ESC) y cierre al clic sobre el scrim. Se usa para el Construction modal. |
| `Ui/PanelHeader.cs` | Header `HBoxContainer` con título `PanelTitle` + `IconButton` Close. El botón X emite `CloseRequested` que el modal propaga a `Closed`. |
| Snapshots de presentación | `CityMacroView`, `MacroCitizenActivity`, `HeroProfileView` y `HeroAccessButton` ya no leen entidades/colecciones vivas. Usan `CityMacroSnapshot`, `HeroProfileSnapshot` y `HasHero()`. |
| Componentes | `Components/AssignmentRow.tscn` unifica filas Assign/Remove en edificio y construcción. `ModalHost`, `PanelHeader`, `AssignmentRow` y `SafeAreaMarginContainer` están registrados con `[GlobalClass]`. |
| Responsive y safe area | Building detail usa `SafeAreaMarginContainer` + `VBoxContainer`/`HBoxContainer`; onboarding y hero profile aplican safe area y el proyecto mantiene viewport 1280×720 con `canvas_items/expand`. |
| Foco | Macro, building detail, hero profile, onboarding, construction y lineage showcase asignan foco inicial; los accesos principales del macro tienen vecinos izquierda/derecha explícitos. |
| Theme | `ErrorText` centraliza los mensajes de error. Se eliminaron overrides redundantes que repetían tamaños ya definidos por las variaciones. |
| Botones icono + texto | `IconButton` usa el renderer nativo `Button.Text` + `Button.Icon`; Back to city y View hero comparten PackedScenes canónicos. Pause/Resume de producción muestra texto. Solo la X de cierre permanece intencionalmente icon-only. |
| Contraste y métricas | Las claves del theme usan `colors/*` (no el inválido `font_colors/*`), los botones amarillos/verdes tienen texto marrón oscuro en todos los estados y Assign/Remove usa la métrica compacta 88×36. |
| Modal y status bar | El scrim exige press+release fuera del rectángulo del contenido; movimiento o el release que abrió el modal no lo cierran. La intención abierta/cerrada persiste durante ticks del mundo y solo cambia automáticamente al cambiar de modo macro. `BuildingDetailView` comienza debajo de los 40 px del status bar y conserva además su safe-area interna. |
| Jerarquía de navegación | `View hero` y `Construction` son acciones exclusivas del macro view. Forest/Basic Shelter y Hero Profile usan encabezado local con título + Back. Back es un `Button` nativo PackedScene con `text` e `icon` directos. |
| `TooltipPanel.cs` | Helpers `TooltipButton` y `TooltipPanelContainer` para botones/páneles que necesitan tooltip consistente (sin override de popup, simplemente exponen la propiedad para que use el theme base). |
| Forest como productor orgánico | `SeedStartingForests` crea 2 forests por héroe fundador con `workerCapacity: 2`, `visualCapacity: 2`, `baseProductionPerWorker: 1`. El tick transfiere 1 wood por worker de `WoodReserve` a `Stock`. Cuando `WoodReserve == 0`, `DemolishDepletedForests` retira el edificio y registra `WorldEventKind.ForestDemolished`. |
| `BuildingPlot` placeholder | `texturePath` nullable; cuando es null renderiza `ColorRect` marrón + label grande "FOREST" usando `GameTitle` (Geist Pixel). Mantiene click → detail view. |
| `BuildingSave.WoodReserve` | Nuevo campo nullable. Saves viejos se hidratan vía `Building.SeedWoodReserve(StartingForestWoodReserve)` y un reemplazo de `WorkerCapacity`/`VisualCapacity`/`BaseProductionPerWorker` (los forests pre-slice tenían ceros como marcador de no-productivo). |
| `ProductionPanel` simplificado | Solo: título, stock (con `reserve` paréntesis en Forest), rate, inputs due, stop-cause line, toggle on/off `IconButton` (play/pause). Sin SpinBoxes de MinStock/MaxStock/Priority. |
| `OfflineReportPanel` layout | 4 esquinas ancladas (top=bottom=1.0), offsets negativos: 360×320 px en la esquina inferior derecha. `z_index = 10` para que renderice por encima de los plots marrón. `grow_vertical = 0` (Begin) para que ningún crecimiento interno desborde el canvas. |
| Chronicle interaction | The full-width native collapse button has the same hover/focus feedback as other actions. Collapsed mode shows the latest rendered entry; expanded mode restores the bounded scroll view. Consecutive equivalent events accumulate into one row, and the counter matches rendered rows rather than raw repetitions. |
| Plot interaction geometry | Hit targets follow the visible subject: Forest retains its territorial footprint, while Shelter/Farm/Quarry and construction stages use centred bounds aligned with their art. Placeholder textures, labels, and citizen labels share those visual anchors. |
| Modal/Tooltip reusables | `ui_cancel` (ESC), click en scrim, y el X del header son tres rutas independientes para cerrar el modal de construcción. |

## Verificación automática

| Comando | Resultado actual |
| --- | --- |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test --no-build` | ✅ 327 / 327 (2026-07-21). |
| `Godot --headless --quit-after 3` | ✅ 2026-07-21: slot 0 carga con Basic Shelter + Forest placeholder sin errores de escena o C#. |

## Checklist de auditoría visual (firma humana)

The canonical capture command and state/resolution matrix now live in
[`VISUAL_REGRESSION.md`](VISUAL_REGRESSION.md). This checklist remains the
detailed human sign-off for the current prototype.

Responde sí/no y anota la observación.

### Canvas y resolución

- [ ] 1280×720: macro sin bandas, log inferior derecho visible, modal centrado.
- [ ] 1920×1080 y 2560×1080 ultrawide: sin recortes en plots ni en modal.

### Tooltips — tipografía Pixelify

- [ ] Hover sobre `HeroAccessButton`, `ConstructionMenuButton`, `BackButton` del detail, `Back to city` del hero profile: popup nativo de Godot, texto en Pixelify cream.
- [ ] Hover sobre cualquier plot (incluido los Forest marrón): tooltip *"Click to enter"* o *"Click to enter {name}"* en Pixelify.
- [ ] Hover sobre botones `Assign` / `Remove` del `AssignmentPanel`: tooltip Pixelify.
- [ ] Hover sobre slot de `VisibleWorkerSlots`: *"Click to remove this worker"* en Pixelify.
- [ ] Ningún tooltip con la fuente default del engine (texto liso).

### Modal y navegación

- [ ] `ConstructionMenuButton` alterna entre "Build shelter", "Construction progress" o "Close construction" según modo.
- [ ] X / ESC / click-en-scrim cierran el modal consistentemente.
- [ ] `View hero` desde el modal cierra el modal y abre `HeroProfileView`.
- [ ] Al volver con `Back to city`, el macro view se restaura limpio.

### Forest productor orgánico

- [ ] En detail view de un Forest se ve: `Wood: X / Y (reserve R)` + `Foraging rate: N wood / tick (M workers)` + toggle on/off.
- [ ] Asignando 1-2 workers, cada tick el contador `reserve` baja y el `Stock` sube al mismo ritmo.
- [ ] Cuando `reserve` llega a 0, el Forest conserva el wood ya recolectado en `Stock`; solo se demuele después de que construcción consuma también ese stock, entonces el plot desaparece y aparece *"Forest demolished"* en el log.
- [ ] Un Forest con `reserve = 0` produce exactamente 0 wood por tick, no consume stamina y muestra `MissingInputs`; nunca cae en la fórmula genérica de producción por trabajadores.

### Botones reutilizables (factory)

- [ ] "Back to city" en detail view y en hero profile son **idénticos** (icono arrow-left, mismo label, mismo tamaño, mismo tooltip).
- [ ] "View hero" en `HeroAccessButton` (macro) y en `ConstructionPanel._viewHeroButton` son **idénticos** (icono user, mismo label).
- [ ] Construction, Authorize, Build Farm/Quarry, Pause/Resume y View shelter muestran texto visible además del icono.

### Layout del log

- [ ] El `OfflineReportPanel` está anclado a la esquina inferior derecha (360×320) sin salirse del canvas a 1280×720 ni a 1920×1080.
- [ ] El log permanece legible por encima de los plots y no bloquea la ruta principal de interacción.
- [ ] El contenido del log usa scroll cuando la lista crece.
- [x] Collapse alterna entre el último registro y el historial expandido; el botón muestra hover y conserva una hitbox completa.
- [x] El contador coincide con las filas visibles compactadas, no con repeticiones internas.

### Plot hit targets

- [x] Farm, Quarry, Shelter, and construction placeholders are centred inside their visible interaction outlines.
- [x] Clicking transparent legacy-container space outside the visible subject does not open its detail panel.
- [x] Forest remains intentionally clickable across its larger territorial footprint.

### Focus y teclado

- [ ] Tab desde teclado recorre `HeroAccessButton` → `ConstructionMenuButton` → macro view.
- [ ] ESC capturado por el modal cuando está abierto; inerte fuera del modal.
- [ ] Gamepad puede navegar las opciones del modal y cerrarlo con la misma X visual.

## Deuda explícita restante

- `ComponentTooltip` stylebox por linaje (hoy cae al panel genérico del linaje en fallback chain).
- Sprite definitivo del Forest (placeholder marrón es funcional).
- La matriz visual 1280×720, 1920×1080, 2560×1080, 4:3 y vertical sigue
  requiriendo firma humana. La verificación headless valida carga, no composición visual.

## Historial de firmas

| Fecha | Revisor | Resultado |
| --- | --- | --- |
| _(pendiente)_ | _(humano)_ | _( pendiente de ejecutar manualmente )_ |
