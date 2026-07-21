# UI Audit — Estado actual

> Auditoría de la capa de presentación tras los slices de estabilización más
> recientes (tooltip nativo con tipografía Pixelify, ModalHost + PanelHeader,
> botón standar, simplificación de ProductionPanel, Forest como productor
> orgánico, layout anclado del log).
>
> Completa `docs/CURRENT_STATUS.md` § *Presentation, themes, and navigation*;
> la checklist de firma humana sigue requiriendo captura visual para los items
> que dependen del render final.

## Resumen de los cambios vigentes

| Pieza | Detalle |
| --- | --- |
| `default_theme.tres` | Añadida entrada base `Label/font = Pixelify Sans`, 18 px, cream. Cualquier `Label` sin variación explícita cae aquí — incluyendo el `Label` interno del popup de tooltip. |
| `Ui/StandardButtons.cs` | Factoría estática con `BackToCityButton()` (arrow-left + Jersey 10 + tooltip *"Return to the city view"*) y `ViewHeroButton()` (user + Jersey 10 + tooltip *"Open the hero profile"*). Consumida por `BuildingDetailView`, `HeroProfileView`, `ConstructionPanel` y alineada con `HeroAccessButton` (.tscn). |
| `Ui/ModalHost.cs` | Modal reusable: scrim semitransparente, `CenterContainer`, escucha de `ui_cancel` (ESC) y cierre al clic sobre el scrim. Se usa para el Construction modal. |
| `Ui/PanelHeader.cs` | Header `HBoxContainer` con título `PanelTitle` + `IconButton` Close. El botón X emite `CloseRequested` que el modal propaga a `Closed`. |
| `TooltipPanel.cs` | Helpers `TooltipButton` y `TooltipPanelContainer` para botones/páneles que necesitan tooltip consistente (sin override de popup, simplemente exponen la propiedad para que use el theme base). |
| Forest como productor orgánico | `SeedStartingForests` crea 2 forests por héroe fundador con `workerCapacity: 2`, `visualCapacity: 2`, `baseProductionPerWorker: 1`. El tick transfiere 1 wood por worker de `WoodReserve` a `Stock`. Cuando `WoodReserve == 0`, `DemolishDepletedForests` retira el edificio y registra `WorldEventKind.ForestDemolished`. |
| `BuildingPlot` placeholder | `texturePath` nullable; cuando es null renderiza `ColorRect` marrón + label grande "FOREST" usando `GameTitle` (Geist Pixel). Mantiene click → detail view. |
| `BuildingSave.WoodReserve` | Nuevo campo nullable. Saves viejos se hidratan vía `Building.SeedWoodReserve(StartingForestWoodReserve)` y un reemplazo de `WorkerCapacity`/`VisualCapacity`/`BaseProductionPerWorker` (los forests pre-slice tenían ceros como marcador de no-productivo). |
| `ProductionPanel` simplificado | Solo: título, stock (con `reserve` paréntesis en Forest), rate, inputs due, stop-cause line, toggle on/off `IconButton` (play/pause). Sin SpinBoxes de MinStock/MaxStock/Priority. |
| `OfflineReportPanel` layout | 4 esquinas ancladas (top=bottom=1.0), offsets negativos: 360×320 px en la esquina inferior derecha. `z_index = 10` para que renderice por encima de los plots marrón. `grow_vertical = 0` (Begin) para que ningún crecimiento interno desborde el canvas. |
| Modal/Tooltip reusables | `ui_cancel` (ESC), click en scrim, y el X del header son tres rutas independientes para cerrar el modal de construcción. |

## Verificación automática

| Comando | Resultado actual |
| --- | --- |
| `dotnet build` | ✅ 0 / 0 |
| `dotnet test --no-build` | ✅ 309 / 309 |
| `Godot --headless --quit-after 3` | ✅ slot 0 carga con Home + 2 plots Forest placeholder; los bosques que ya no tienen `WoodReserve` fueron demolidos por el sweep correcto. |

## Checklist de auditoría visual (firma humana)

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
- [ ] Cuando `reserve` llega a 0, el Forest se demuele automáticamente, el plot desaparece del macro view y aparece un evento *"Forest demolished"* en el log.

### Botones reutilizables (factory)

- [ ] "Back to city" en detail view y en hero profile son **idénticos** (icono arrow-left, mismo label, mismo tamaño, mismo tooltip).
- [ ] "View hero" en `HeroAccessButton` (macro) y en `ConstructionPanel._viewHeroButton` son **idénticos** (icono user, mismo label).

### Layout del log

- [ ] El `OfflineReportPanel` está anclado a la esquina inferior derecha (360×320) sin salirse del canvas a 1280×720 ni a 1920×1080.
- [ ] El log nunca cubre los plots marrón.
- [ ] El contenido del log usa scroll cuando la lista crece.

### Focus y teclado

- [ ] Tab desde teclado recorre `HeroAccessButton` → `ConstructionMenuButton` → macro view.
- [ ] ESC capturado por el modal cuando está abierto; inerte fuera del modal.
- [ ] Gamepad puede navegar las opciones del modal y cerrarlo con la misma X visual.

## Deuda explícita (siguiente iteración de estabilización)

- Layout responsive para aspect ratios verticales / 4:3 — anchors actuales se ven bien a 16:9 / 21:9.
- `ComponentTooltip` stylebox por linaje (hoy cae al panel genérico del linaje en fallback chain).
- Sprite definitivo del Forest (placeholder marrón es funcional).
- Focus traversal automático (tab order cableado en código) — Godot usa el orden de inserción.
- Botón cerrar el log de eventos (ahora siempre visible; el jugador puede ignorarlo pero no esconderlo).

## Historial de firmas

| Fecha | Revisor | Resultado |
| --- | --- | --- |
| _(pendiente)_ | _(humano)_ | _( pendiente de ejecutar manualmente )_ |
