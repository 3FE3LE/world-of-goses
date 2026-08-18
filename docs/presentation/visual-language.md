# Lineamientos visuales, UI y assets

## Dirección general

- Pixel art 2D puro.
- No 2.5D como dirección principal.
- Bordes nítidos.
- Sin antialiasing en sprites y UI pixel art.
- Filtro nearest.
- Paletas controladas.
- Iluminación coherente.
- Diseño original.

## La rejilla de 32

**El píxel de diseño del juego mide 32 unidades por tile.** Todo el arte se
autora contra esa rejilla: un tile de suelo es 32 × 32, un lote estándar son
3 × 3 tiles, y el resto de la escalera se deriva de ahí (ver
[`art-pipeline.md`](art-pipeline.md) § *Escalera de tamaños*).

De la rejilla sale la geometría del macro, no al revés:

```text
TileUnitPx = 32          un tile de suelo
LotUnitPx  = 96          = 3 × TileUnitPx, un lote estándar
zoom       = 1 · 2 · 3   enteros; 2 es el valor por defecto
```

Con eso, zoom 1 dibuja un píxel de origen por píxel lógico, zoom 2 lo dibuja
como un bloque de 2 × 2 y zoom 3 como uno de 3 × 3. **El zoom no escala nunca
la interfaz**: el HUD vive en `Control` sobre el lienzo lógico y no se entera
de la cámara del mundo.

## Resolución y escala entera

Resolución lógica base:

```text
1280 × 720
```

`stretch/mode = canvas_items` con `aspect = expand`, así que el lienzo lógico
son 1280 × 720 en toda resolución y la ventana lo reescala:

| Pantalla | Factor | Entero |
| --- | ---: | --- |
| 1280 × 720 | ×1 | sí |
| 1920 × 1080 | ×1,5 | **no** |
| 2560 × 1440 | ×2 | sí |
| 3840 × 2160 | ×3 | sí |

**La escala entera es la regla dentro del lienzo lógico**, que es donde el arte
y la UI se diseñan y se firman. Fuera de él, la única resolución común que cae
en un factor fraccionario es 1080p, y se acepta: bajar la base para arreglarla
—640 × 360 escalaría entero a 1080p— partiría en dos el espacio lógico y
obligaría a rehacer todos los tokens de `Ui/Tokens.cs`, todos los tamaños
tipográficos y todos los layouts. No compensa por una resolución.

Las capturas oficiales de regresión visual siguen siendo 1280 × 720 y
1920 × 1080; la primera es la que manda para juzgar el píxel.

## Tipografía

```text
Jacquard 24 · Jacquard 12
Títulos, pantallas destacadas y marca del HUD

Jacquarda Bastarda 9
El nombre del fundador, y nada más

Jersey 15 · Jersey 10
Subtítulos, encabezados, botones, párrafos y tooltips

Micro 5
Lectura y cifras del HUD compacto
```

Todas son fuentes de rejilla y se renderizan sin antialiasing. El tamaño de
cada variación se deriva del em nativo de su familia, no se elige: por debajo
de unos 0,7 px por píxel de diseño el glifo no se vuelve áspero, se rompe. La
tabla medida vive en `ui-patterns.md` §5.

## Tres escalas visuales

Las tres se miden contra la rejilla de 32. **El ciudadano mide lo mismo en las
tres: un único lienzo de 64 × 64.** Lo que cambia entre escalas es cuántas
direcciones necesita y cuántos clips recibe, nunca su tamaño.

Las cifras anteriores —habitantes de 4 a 8 píxeles, lienzo detallado de 64 × 96,
lienzo macro de 32 × 64 con la figura ocupando 24–32 px— se escribieron para
escalas de placeholder distintas y llegaron a coexistir **contradiciéndose entre
sí dentro de este mismo archivo**. Un ciudadano no tiene tres tamaños.

### Ciudad macro

- Parcelas y edificios como foco.
- Tile de suelo: 32 × 32. Huella de lote estándar: 96 × 96.
- Lienzo de ciudadano: 64 × 64, en **cuatro direcciones**.
- La lectura la da la silueta, no el rasgo.

### Escena de edificio

- Ciudadanos reales asignados.
- Lienzo de ciudadano: 64 × 64 — **el mismo sprite que el macro**, sin reescalar.
- Animaciones moderadas.
- Límite visual de trabajadores.

### Expedición

- Sprites completos, vista lateral.
- Lienzo de combatiente: 64 × 64, **una sola dirección, espejada** — el mismo
  lienzo que el macro, del que reutiliza la locomoción lateral tal cual.
- Es la escala donde se invierte en animación, equipo, heridas y efectos: **no
  más resolución, más clips**.
- Fondos por capas de parallax, no dibujados por código.

**Proporción con los edificios.** Un edificio base de una planta corresponde a un
techo interior de unos 2,2 m con una persona de ~1,75 m: la figura ocupa el **80 %
del techo interior**, pero sólo un **55–65 % del volumen exterior visible**, porque
la fachada añade suelo y alero y el tejado añade su propio plano. Con un ciudadano
cuya altura dibujada típica ronda los 56 px de su lienzo de 64, eso sitúa un
edificio de una planta en torno a **96 px de alto**, que es exactamente la cifra
heredada. **La proporción de una planta ya es correcta.**

Lo que sí es un eje independiente es el **alto del lienzo**, y por dos razones que
no son «una planta se ve baja»:

- Un edificio de más de una planta necesita más alto sin cambiar su huella.
- El plano del tejado **se abre en las filas cercanas y se cierra en las lejanas**
  (§Ciudad macro), así que el lienzo debe caber el frame más alto de la serie.

Por tanto: la huella de un lote estándar sigue siendo 96 × 96; el ancho del lienzo
sale de la huella, y el alto se mide contra la figura y se redondea a tile entero.
Las dos proporciones que se miden son distintas y las dos deben cumplirse: figura
contra fachada ≈ 75–85 %, figura contra sprite completo ≈ 55–65 %.

**Los frames de un edificio son de perspectiva, no de animación.** El código
reescala por profundidad (`HorizontalScale`, `ProjectedRowScreenY`), pero un
reescalado no cambia el escorzo del tejado: una fila cercana ve su plano superior y
una lejana casi no. La fachada se autora una vez y la escala el código; el **plano
del tejado se autora una vez por fila de descanso de la ventana de cámara**. El
número de frames lo acota la ventana (unas cuatro filas), nunca el tamaño del
mundo.

## Profundidad y desniveles

Dirección futura: mundo 2D puro con sensación de profundidad — no 2.5D ni un
motor 3D. El mecanismo difiere según la escala visual; coexisten dos
sub-modelos, no uno solo.

Jardines y puntos de gathering se suman a la lista de escenas detalladas
instanciadas, junto a minas, granjas, hospitales y talleres.

Esta es una dirección documentada para una fase de integración técnica
posterior; no implica cambios inmediatos al prototipo actual salvo lo ya
prototipado (ver `../engineering/architecture.md`).

### Interiores (elevación plana)

Aplica a escena de edificio, jardín y gathering: desniveles (cuestas,
escalones, puentes, saltos de un tile hacia abajo), al estilo de la
navegación de Pokémon Blanco/Negro 2.

Mecanismo:

- Capas de `TileMapLayer` por nivel de elevación.
- Y-sort para que personajes y bordes elevados se oculten o revelen
  correctamente según la posición.
- Tiles de transición (escaleras, rampas) conectan niveles distintos.

Cada escena puede tener su propio rango de elevaciones, coherente con las
unidades base ya definidas abajo.

### Ciudad macro (perspectiva por calles)

Aplica solo a la ciudad macro — un espacio grande, no contenido, donde la
elevación plana no comunica distancia. En vez de eso: escala pseudo-3D por
profundidad, al estilo de las pistas y obstáculos de los juegos de carreras
Atari (Pole Position, Out Run). Sigue siendo 2D puro (sprites/tiles
reescalados por código, no un motor 3D ni geometría extruida) — no
contradice "No 2.5D como dirección principal".

- Un elemento más arriba en pantalla (más lejos) se ve **más pequeño y más
  angosto**; más abajo (más cerca), **más grande y más ancho**. El
  angostamiento horizontal es un achicamiento adicional al vertical, no el
  mismo factor — así se lee como convergencia de perspectiva, no solo un
  sprite chico.
- La ciudad se organiza en "calles": filas discretas de profundidad.
  **Desambiguación importante:** esta "calle" (fila de profundidad de la
  vista macro) no es el corredor de navegación de
  `../systems/frontage-and-corridors.md` (2 tiles de ancho, para
  navmesh/pathfinding). Comparten nombre por coincidencia de vocabulario, no
  por diseño.
- Navegación vertical (avanzar/retroceder en profundidad): **escalonada**,
  nunca un scroll continuo — pasar de una calle a la adyacente (anterior o
  posterior) es una transición discreta, con una animación breve y
  cuantizada (varios pasos, no un tween continuo ni un corte instantáneo)
  que reescala y reacomoda los edificios visibles a su nueva profundidad.
- Navegación horizontal dentro de una calle: cuantizada (misma cadencia que
  el resto del "Pixel-motion grammar" abajo), "medianamente libre" — no
  salta de calle, solo se mueve dentro de la actual.
- El ángulo y la convergencia no cambian con el zoom. La cámara muestra una
  ventana móvil de aproximadamente cuatro filas de parcelas: trece calles de
  construcción, incluyendo dos franjas delante de la calle enfocada; la cuarta
  posición contando el foco cruza el plano cercano. El territorio semántico
  fuera de esa ventana no se comprime ni se estira para caber: se revela al
  avanzar la cámara por calles discretas.
- El zoom-out máximo usa escala uniforme. Con el foco inicial en la tercera
  calle, la primera fila queda cerca del borde inferior y la última fila de la
  ventana cerca del borde superior, sin alterar la pendiente del trapecio.

### Cámara (ambos sub-modelos)

Libre (pan/zoom) siempre disponible, haya o no un ciudadano seleccionado.
Seleccionar un ciudadano (info/delegación) no activa la cámara por sí solo;
el jugador debe activar explícitamente el modo cámara-sigue como toggle
aparte (ver `04_CITIZENS_PROFESSIONS_AND_HEROES.md`, "Cámara-sigue"). En la
ciudad macro, el "paneo libre" es él mismo cuantizado/escalonado por calle
(no un arrastre continuo 1:1) — ver "Pixel-motion grammar". Todos los modos
respetan posiciones y escala entera ("Pixel perfect" en
`../engineering/architecture.md`).

## Unidad base

La rejilla es **32 unidades por tile**, y nada más se fija aquí. La escalera
completa de tamaños —tile, prop, árbol, ciudadano, edificio, emblema, icono— vive
en [`art-pipeline.md`](art-pipeline.md) § *Escalera de tamaños*, que es su única
fuente.

El bloque que ocupaba esta sección (`Terreno: 64 × 64`, `Habitante detallado:
64 × 96`, `Habitante macro: 4 a 8 píxeles`, `Edificios: múltiplos de 64`) era
anterior a la rejilla de 32 y contradecía la sección *Tres escalas visuales* en
las cuatro líneas. Se retira en vez de actualizarse: una cifra escrita en dos
sitios termina divergiendo en los dos.

## Anclaje

Personajes anclados al centro inferior, entre los pies. La baseline debe permanecer estable entre frames.

## Animación

### Pixelorama

Movimiento dibujado: caminar, trabajar, atacar, curar, recibir impacto y teletransportarse.

### Godot

Posición, entrada y salida, brillo, sombras, opacidad, partículas, UI y efectos ambientales.

### Pixel-motion grammar

- Simulation time remains continuous; character presentation is quantized.
- Macro and building-detail locomotion uses integer positions at a deliberate
  24 Hz visual cadence, advancing by 4 pixels per step. It was 12 Hz at 8 px
  until 2026-08-06; the effective speed is identical (96 px/s), but the coarser
  step read as a jerk rather than as a gait. The grammar is unchanged — motion
  is still discrete, never interpolated — only the grain is finer. Anything
  that advances a fixed fraction per cadence tick (the camera's depth pan, the
  building-entry zoom) carries twice the step count so its duration holds.
- Macro travel follows cardinal routes and must not cross occupied building
  footprints.
- The macro view's perspective trapezoids climb in whole-pixel treads rather
  than as true diagonals, which would betray the pixel art. The tread is 2 px
  (4 px until 2026-08-06, which read as a sawtooth on the long shallow edges of
  the near streets). This is a grain adjustment, not a step toward
  antialiasing: edges stay snapped to a whole-pixel grid. Two is the floor
  worth taking — at 1 px the treads stop reading as deliberate and the edge
  becomes the diagonal this quantisation exists to avoid.
- **World-camera pan/observation follows the same discrete cadence as
  character locomotion — it is not a continuous 1:1 mouse-drag.** Fluid,
  continuously-interpolated motion is the explicit exception, not the
  default, for any world navigation (camera included), so base movement
  stays visually consistent with how a future combat/ability system would
  likely telegraph its own motion.
- **Expedition combat is that exception, and the only one.** While an
  expedition is travelling, the world advances one 4 px locomotion step at a
  time, the same cadence the walker's own gait uses; the moment an encounter
  begins the camera drops the grid and moves continuously, and it picks the grid
  back up when travel resumes. Impact reactions and camera pans are readable
  only against continuous motion, and a fight is where the game stops being a
  walk and asks to be watched.
  - The switch is not a setting. It follows from which call the stage makes —
    `FollowTravel` quantizes, `FrameEncounter` does not — so there is no mode
    left enabled by mistake, and the return to stepping needs no second call.
  - The travelling party is drawn at the camera's own quantized offset
    (`TravelDrawPositionX`), never at the raw `Travel.PositionX`. Projecting the
    raw value against a stepped offset inverts the grammar: the ground jumps
    while the walker slides across it, which reads worse than either motion
    alone.
  - A struck combatant flinches (`HitReaction`): a transient shove away from
    whoever hit it, sized by the bodily share of the blow against the target's
    Stability, decaying to zero inside the step that caused it. It is drawn on
    top of the authoritative position and never replaces it, so the figure
    always settles where the domain says it is. It stays visibly smaller than
    the displacement a Knockdown produces. An evaded or fully absorbed blow does
    not flinch.
- UI scrolling may remain smooth. Continuous character fades or subpixel
  locomotion require an explicit visual exception.

## Recursos, Shelter y Chronicle

- La barra de estado global integra una franja pequeña de recursos: solo icono
  y cantidad disponible, sin nombre permanente. El tooltip conserva nombre,
  total almacenado y reserva cuando existe. La franja lee la proyección del
  ledger; no implica un inventario global nuevo ni cambia propiedad física.
- El inventario de la ciudad se consulta desde el Shelter mediante una
  sección plegable, con icono, cantidad total y reserva disponible cuando
  corresponda. Esta ubicación es una superficie de gestión, no implica que
  todos los recursos compartan un único contenedor físico.
- Antes del Cache no existe un almacén implícito: la interfaz de Construcción
  muestra abierta la carga personal del fundador (6 unidades rudimentarias).
  Al completar el Cache, la misma superficie cambia a almacenamiento del sitio
  (12); después de consolidar el Shelter, su detalle asume la gestión (24).
- El Chronicle conserva hitos, decisiones, bloqueos y cadenas causales, pero
  no muestra la aritmética rutinaria de obtención (`StockProduced` ni
  `CropHarvested`). Esos hechos siguen existiendo en el dominio para métricas,
  persistencia y causalidad.
- Al recoger un recurso básico del terreno se muestra únicamente su icono y
  `+cantidad` sobre el propietario físico actual: el fundador mientras carga,
  el Founding Site después del Cache o el Shelter consolidado. El aviso
  desaparece en pocos pasos discretos. Mientras el fundador sea el propietario,
  el aviso sigue su posición durante todo su recorrido; no queda fijado al
  punto del terreno donde terminó la recolección.

## Navegación HUD

- La navegación primaria usa un dock oscuro, etiquetado y centrado en el borde
  inferior. Solo contiene destinos o utilidades realmente conectados.
- El dock primario y las acciones contextuales comparten la misma zona, nunca
  se apilan: la colocación oculta navegación y muestra confirmar/cancelar; al
  terminar o cancelar vuelve la navegación.
- Cámara, velocidad y Menú viven en el `UtilityCluster` del borde derecho de
  `CityStatusPanel`. La velocidad global ofrece únicamente 1x/2x/4x; no existe
  pausa/reanudación ni una superficie de simulación en la esquina inferior.
- Ratón, rueda, teclado y gamepad quedan contenidos por las superficies HUD;
  foco y estado visible no dependen solo del color.

La banda derecha persistente resume expediciones activas y acontecimientos
recientes sin sustituir el panel de planificación. Solo muestra miembros,
suministros, fase y tiempo procedentes del estado real; si no existe una cola,
no representa una cola ficticia. El registro compacto y la Crónica completa
comparten las mismas reglas de filtrado, causalidad y texto.

## UI por linaje

La estructura funcional es compartida. Cambian paleta, bordes, esquinas, rellenos, sombras, patrones, selección, microanimaciones y tratamiento de iconos.

No cambian navegación, jerarquía, semántica, tamaños mínimos ni accesibilidad.

## Sixteen Pixel Perfect

Genera paneles, botones, tooltips, barras, contenedores, marcos, estados y 9-slice.

Exportaciones:

```text
<asset>.png
<asset>.recipe.json
<asset>.godot.json
<asset>.stylebox.tres
<asset>.preview.tscn
```

El `.stylebox.tres` es el recurso nativo principal. No generar `.import` manualmente.

## Identidad resumida

- **Ardhen:** piedra, cobre, placas, contrafuertes, impacto.
- **Eirune:** fibras, células, ramas, crecimiento.
- **Kovari:** placas, remaches, circuitos, segmentos.
- **Myrven:** capas, pliegues, marcos dobles, revelación.
- **Vaelun:** mapas, rutas, nodos y señales.
- **Orveth:** unidades, sellos, cajas y simetría.
- **Caelith:** retículas, nodos y diagramas.
- **Theryn:** ondas, pulsos y círculos.

## Iconografía

Fuentes provisionales:

- Pixelarticons.
- Kenney UI Pixel Kit.

No deformar geometría. Recolorear por tokens y enmarcar según linaje.

## Assets provisionales

```text
game/assets/placeholders/
├── macro/
├── buildings/
├── citizens/
├── expeditions/
├── terrain/
├── ui/
└── audio/
```

Registrar fuente, autor, licencia, uso y reemplazo requerido.

## Pipeline artístico

```text
Pixelorama .pxo
↓
PNG o sprite sheet
↓
Godot resource o scene
↓
C# decide el estado
```
