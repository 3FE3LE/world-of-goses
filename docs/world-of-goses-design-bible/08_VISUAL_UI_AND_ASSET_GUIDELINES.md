# Lineamientos visuales, UI y assets

## Dirección general

- Pixel art 2D puro.
- No 2.5D como dirección principal.
- Bordes nítidos.
- Sin antialiasing en sprites y UI pixel art.
- Escala entera.
- Filtro nearest.
- Paletas controladas.
- Iluminación coherente.
- Diseño original.

## Resolución

Resolución lógica base:

```text
1280 × 720
```

## Tipografía

```text
Geist Pixel
Títulos y pantallas destacadas

Jersey 10
Subtítulos, encabezados y botones

Pixelify Sans
Párrafos, descripciones y tooltips
```

## Tres escalas visuales

### Ciudad macro

- Parcelas y edificios como foco.
- Habitantes de 4 a 8 píxeles.
- Siluetas ambientales.
- Sin anatomía detallada.

### Escena de edificio

- Ciudadanos reales asignados.
- Canvas aproximado: 64 × 96.
- Animaciones moderadas.
- Límite visual de trabajadores.

### Expedición

- Sprites completos.
- Vista lateral.
- Canvas inicial orientativo: 96 × 96.
- Mayor inversión en animación, equipo, heridas y efectos.

## Profundidad y desniveles

Dirección futura: mundo 2D puro con sensación de profundidad — no 2.5D ni un
motor 3D. El mecanismo difiere según la escala visual; coexisten dos
sub-modelos, no uno solo.

Jardines y puntos de gathering se suman a la lista de escenas detalladas
instanciadas, junto a minas, granjas, hospitales y talleres.

Esta es una dirección documentada para una fase de integración técnica
posterior; no implica cambios inmediatos al prototipo actual salvo lo ya
prototipado (ver `10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`).

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
  vista macro) no es la "calle" de `H-26` en `TO_DO.md` (corredor de 2 tiles
  de ancho para navmesh/pathfinding). Comparten nombre por coincidencia de
  vocabulario, no por diseño — no asumir que son el mismo concepto al
  retomar `H-26`/`S-1.2`.
- Navegación vertical (avanzar/retroceder en profundidad): **escalonada**,
  nunca un scroll continuo — pasar de una calle a la adyacente (anterior o
  posterior) es una transición discreta, con una animación breve y
  cuantizada (varios pasos, no un tween continuo ni un corte instantáneo)
  que reescala y reacomoda los edificios visibles a su nueva profundidad.
- Navegación horizontal dentro de una calle: cuantizada (misma cadencia que
  el resto del "Pixel-motion grammar" abajo), "medianamente libre" — no
  salta de calle, solo se mueve dentro de la actual.

### Cámara (ambos sub-modelos)

Libre (pan/zoom) siempre disponible, haya o no un ciudadano seleccionado.
Seleccionar un ciudadano (info/delegación) no activa la cámara por sí solo;
el jugador debe activar explícitamente el modo cámara-sigue como toggle
aparte (ver `04_CITIZENS_PROFESSIONS_AND_HEROES.md`, "Cámara-sigue"). En la
ciudad macro, el "paneo libre" es él mismo cuantizado/escalonado por calle
(no un arrastre continuo 1:1) — ver "Pixel-motion grammar". Todos los modos
respetan posiciones y escala entera ("Pixel perfect" en
`10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`).

## Unidad base

```text
Terreno: 64 × 64
Habitante detallado: aproximadamente 64 × 96
Habitante macro: 4 a 8 píxeles
Edificios: múltiplos de 64
```

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
  12 Hz visual cadence, advancing by 8 pixels per step.
- Macro travel follows cardinal routes and must not cross occupied building
  footprints.
- **World-camera pan/observation follows the same discrete cadence as
  character locomotion — it is not a continuous 1:1 mouse-drag.** Fluid,
  continuously-interpolated motion is the explicit exception, not the
  default, for any world navigation (camera included), so base movement
  stays visually consistent with how a future combat/ability system would
  likely telegraph its own motion.
- UI scrolling may remain smooth. Continuous character fades or subpixel
  locomotion require an explicit visual exception.

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
