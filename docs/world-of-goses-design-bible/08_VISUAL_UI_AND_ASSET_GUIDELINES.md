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
