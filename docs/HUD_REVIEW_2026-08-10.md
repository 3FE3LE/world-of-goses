# Revisión del HUD — World of Goses

> **Fecha:** 2026-08-10
> **Alcance:** HUD en juego — `game/scripts/Ui/`, `game/scripts/CityStatusPanel.cs`,
> `game/scripts/ExpeditionRail.cs`, `game/scenes/CityPrototype.tscn` y sus
> puntos de composición en `game/scripts/CityPrototype.cs` y
> `game/scripts/Prototypes/MacroStreetLiveView.cs`.
> **Método:** revisión estática de código. No se ejecutó la matriz visual para
> esta evaluación; los puntos que la requieren están marcados.
> **Nota:** este documento es un reporte de hallazgos, no modifica el backlog
> (`TO_DO.md` sigue siendo el dueño de la cola accionable).

---

## 1. Lo que está bien (no tocar sin motivo)

Estas decisiones son la base del HUD y se evalúan como correctas:

1. **`Tokens.cs` como vocabulario de diseño documentado.** Cada constante
   justifica su valor (grid de 24 px de Pixelarticons, stretch `canvas_items`
   que hace que 1080p sea el mismo viewport lógico, nombrar valores sin
   renumerarlos para no mover métricas).
2. **`OverlayLayers` como catálogo semántico de z-order.** Fuente única de
   verdad, huecos numéricos para insertar capas, reglas explícitas (el tint
   ambiental nunca lava la UI; HUD chrome no va en `World`).
3. **Tipografía 100% centralizada** en `default_theme.tres` (cero
   `AddThemeFontOverride`/`AddThemeFontSizeOverride` según el survey
   documentado en `Tokens.cs`).
4. **Accesibilidad real:** signo del delta en texto además de color
   (`HudResourceRow`), tooltips con cantidades exactas frente a formato
   compacto (`1.2K` vs `1,200`), anillos de foco para teclado/gamepad,
   restauración de foco tras rebuild.
5. **`UiInputBoundary`**: contiene el leak de la rueda del mouse hacia la
   cámara del mundo cuando un `ScrollContainer` está en su tope.
6. **Contratos fail-fast**: `GameUiShell._Ready` lanza excepciones
   descriptivas ante slots faltantes u orden incorrecto.
7. **Coalesción de refreshes** con `CallDeferred` en `ExpeditionRail`
   (`RequestRefresh`/`ApplyQueuedRefresh`).
8. **Acordeón rail/crónica.** *(Actualizado 2026-08-10, tras el commit que
   introduce `AccordionHost`.)* La versión revisada aquí resolvía el acordeón
   con relayout deferred que invalidaba el cache de minimum-size de tres
   ancestros (`RequestRailRelayout`). Eso **ya no existe y no debe volver**:
   era la compensación de dos hermanos `ExpandFill` disputándose la columna,
   que dejaba el cuerpo de expediciones en 2 px con las tarjetas todavía
   `Visible`. Hoy los dos cuerpos comparten un `AccordionHost` con exactamente
   uno visible, que es el único hijo expansivo del rail.
9. **Safe area** aplicada sin envolver paneles en `MarginContainer`
   (lección documentada del revert `d0fd51d`).

---

## 2. Problemas (lo que está mal)

Ordenados de mayor a menor impacto.

### P1 — Rebuild completo del status bar en cada tick del mundo

- **Archivos:** `game/scripts/CityStatusPanel.cs:215-243` (`Refresh`),
  llamado desde `MacroStreetLiveView.cs:568,575` (`OnWorldTickAdvanced`) y
  varios puntos de `CityPrototype.cs`.
- **Síntoma:** cada `WorldTickAdvanced` destruye todos los chips
  (`QueueFree` de la fila completa) y crea ~20 controles nuevos. A velocidad
  4x esto es churn continuo de allocations y presión de GC.
- **Agravante 1:** `BuildWorldContext` (`CityStatusPanel.cs:339-340`) ejecuta
  `ResourceLoader.Load<Texture2D>` en caliente para los iconos sol/luna en
  cada refresh.
- **Agravante 2:** el propio archivo reconoce el peligro del teardown — el
  utility cluster es persistente "porque la macro vista cachea referencias"
  (comentario en `Refresh`) — pero la solución se aplicó solo a ese cluster.
- **Solución esperada:** construir los chips una vez y actualizar valores
  in-place. El patrón ya existe y nadie lo usa aquí:
  `HudResourceRow.SetValues` (`game/scripts/Ui/HudResourceRow.cs:111-116`).
- **Verificación:** matriz visual del top status (fixtures existentes) tras
  el cambio.

### P2 — NodePaths como strings mágicos

- **Archivos:**
  - `game/scenes/CityPrototype.tscn`: `ControllerPath =
    NodePath("../../../CityWorldController")` y similares (líneas 89, 98,
    106, 287, 320, 364-368).
  - `game/scripts/CityPrototype.cs`: `GetNode<CityStatusPanel>(
    "GameUiShell/CityStatusPanel")` repetido como literal en 10+ sitios
    (líneas 306-307, 565-566, 617-618, 711-712, 729-730, 806-807, 1409,
    1445, 1472, 1510, 1524, 1633, 2178-2179).
- **Riesgo:** renombrar o mover un nodo rompe en runtime, no en compilación.
  Contradice la convención "no magic strings" de
  `docs/REPOSITORY_CONVENTIONS.md`.
- **Solución esperada:** constantes compartidas o `[Export] NodePath`
  centralizados.

### P3 — Claves de localización derivadas de enums por convención de string

- **Archivo:** `game/scripts/CityStatusPanel.cs:411,427`
  (`UiText.Get(resource.Resource.ToString().ToLowerInvariant())`).
- **Riesgo:** renombrar un valor del enum `ResourceType` rompe
  silenciosamente la clave PO (no falla el build; falla el catálogo en
  runtime).
- **Solución esperada:** mapa explícito `ResourceType → clave i18n`
  (diccionario estático), que falle en compilación si falta una entrada.

### P4 — Tres implementaciones del mismo anillo de foco

- **Archivos:**
  - `game/scripts/Ui/PrimaryNavDock.cs:89-101` (`WireHorizontalFocus`).
  - `game/scripts/Ui/ActionDock.cs:115-126` (wiring inline).
  - `game/scripts/ExpeditionRail.cs:441-465` (`WireFocus`).
- **Síntoma:** la misma lógica de anillo (previous/next circular) duplicada
  con variaciones menores.
- **Solución esperada:** helper único `FocusRing` en `game/scripts/Ui/`.

### P5 — Offsets duplicados a mano en el `.tscn`

- **Archivo:** `game/scenes/CityPrototype.tscn`:
  - `PrimaryNavDock`: `custom_minimum_size = Vector2(520, 60)` + offsets
    `∓260` (líneas 291-302).
  - `ActionDock`: `custom_minimum_size = Vector2(480, 72)` + offsets `∓240`
    (líneas 226-238).
- **Riesgo:** cambiar el ancho exige editar tres números consistentes en
  texto plano; si se desincronizan, el dock queda descentrado sin error.
- **Solución esperada:** calcular offsets desde el ancho en `_Ready`, o al
  menos concentrar el número en una sola constante.

### P6 — Orden de inicialización entre hermanos resuelto caso por caso

- **Síntoma:** el patrón `EnsureBuilt` defensivo en `ActionDock`
  (`ActionDock.cs:78-127`), `CityStatusPanel` (`CityStatusPanel.cs:94-126`)
  y otros existe porque "el `_Ready` del hermano corre antes" (comentario en
  `ActionDock.EnsureBuilt`). Funciona, pero cada superficie resuelve el
  mismo problema de orden de init por su cuenta.
- **Solución esperada:** un punto de composición explícito (el composition
  root inicializa en orden conocido) en vez de construcción perezosa
  defensiva dispersa.

### P7 — Fixtures de regresión visual viven en el script de la escena principal

- **Archivo:** `game/scripts/CityPrototype.cs` (2 499 líneas): métodos
  `Show*ForVisualRegression` (~líneas 1390-1640) mezclados con composition
  root y lógica runtime; activados por la variable de entorno
  `WOG_VISUAL_CAPTURE` (también referenciada en `ExpeditionRail.cs:220`).
- **Riesgo:** el script más grande del proyecto acumula tres
  responsabilidades; cada fixture nuevo lo engorda.
- **Solución esperada:** extraer los fixtures a un harness dedicado (escena
  propia o partial class separada).

### P8 — Hit-testing manual de la rueda en el rail

- **Archivo:** `game/scripts/ExpeditionRail.cs` (`_Input` con
  `GetGlobalRect().HasPoint`).
- **Contexto:** *(Actualizado 2026-08-10.)* La premisa original —"la crónica
  anida su propio scroll bajo el scroll del rail"— ya no es cierta: ambos
  cuerpos son hermanos dentro del `AccordionHost` y sólo uno está visible, así
  que el hit-test ahora se hace contra `_chronicle.Body`. Sigue siendo un
  workaround manual de lo que Godot resuelve con `GuiInput`, y sigue habiendo
  que mantenerlo sincronizado con la jerarquía.
- **Solución esperada:** reevaluar si la crónica puede consumir el evento
  por `MouseFilter`/`AcceptEvent` sin el hit-test manual; si no, al menos
  cubrirlo con fixture.

### P9 — Timer con closure descartando información

- **Archivo:** `game/scripts/CityStatusPanel.cs:148-159` (`OnWorldSaved`):
  `_ = unixMillis` descarta el timestamp que el chip podría mostrar
  ("guardado 14:32"), y el patrón `CreateTimer + lambda + generación` es
  correcto pero frágil ante futuras extensiones.
- **Impacto:** menor; mejora de UX disponible casi gratis.

---

## 3. Mejoras incrementales (bajo riesgo, alto retorno)

| # | Cambio | Problema que cierra | Esfuerzo |
|---|--------|--------------------|----------|
| M1 | Status bar: construir chips una vez y actualizar con `SetValues`; cachear texturas sol/luna | P1 | Medio |
| M2 | Helper `FocusRing` compartido en `Ui/` | P4 | Bajo |
| M3 | Constantes de NodePath compartidas (o `[Export]`) | P2 | Bajo |
| M4 | Mapa `ResourceType → clave i18n` | P3 | Bajo |
| M5 | Offsets de docks calculados desde el ancho en `_Ready` | P5 | Bajo |
| M6 | Evento agregado `CityPresentationStateChanged` en vez de 9 suscripciones individuales en `ExpeditionRail._Ready` (`ExpeditionRail.cs:142-151`) | — | Medio |
| M7 | Chip de guardado con hora del save | P9 | Bajo |

---

## 4. Cambios radicales a considerar (no ejecutar sin aprobación)

### R1 — De "rebuild funcional" a "build once + data binding"

El patrón dominante — destruir y reconstruir el árbol ante cualquier cambio
de estado — es simple y funcionalmente correcto, pero ya mostró sus dientes:
el flicker histórico del ticker (documentado en `CityStatusPanel.cs:26-35`),
la pérdida de foco (que obligó a `_pendingFocusIndex`), y los "sibling-order
traps" (que obligaron a `EnsureBuilt`). No hace falta un MVVM completo: con
que status bar, rail y summary construyan su árbol una vez y apliquen diffs
del snapshot, desaparecen de golpe P1, la restauración manual de foco y
buena parte de P6. **Es el cambio estructural con mejor relación
riesgo/beneficio.** Requiere pasada de la matriz visual completa.

### R2 — Harness dedicado para fixtures visuales

Sacar `Show*ForVisualRegression` de `CityPrototype.cs` a una escena o
partial class propia, dejando el script principal como composition root
puro (cierra P7).

### R3 — Navegación de foco global entre superficies

Hoy cada superficie cablea su anillo interno (P4), pero no existe un
sistema que gobierne el foco *entre* top bar, rail, dock y modales. Cuando
el gamepad importe de verdad, esto será un sistema nuevo, no un parche.

---

## 5. Veredicto

La base conceptual (tokens, layers, theming, accesibilidad, i18n) está por
encima del promedio de proyectos Godot. La deuda está concentrada en un solo
patrón — **rebuild-everything ante cada cambio de estado** (P1, y raíz de
parte de P6/P8) — y en **strings/paths frágiles** (P2, P3). Nada requiere
rescritura urgente, pero el patrón de rebuild es el que escala peor a medida
que el HUD gane superficies.

**Orden sugerido de ataque:** M1 (cierra P1) → M2/M3/M4 (bajo riesgo) →
evaluar R1 con la matriz visual como contrato.
