# Guía tipográfica

## Objetivo

Definir una jerarquía tipográfica clara, consistente y legible para la interfaz del juego, manteniendo una identidad visual inspirada en pixel art sin sacrificar la lectura en textos largos.

El sistema utiliza seis cortes agrupados en cuatro funciones. Cinco de ellos pertenecen a una sola colección, **Soft Type** de Sarah Cadigan-Fried, que es de donde salió Jersey 10 desde el principio:

1. **Jacquard 24 / Jacquard 12** para identidad e impacto visual.
2. **Jacquarda Bastarda 9** para la voz ceremonial del fundador.
3. **Jersey 15 / Jersey 10** para estructura, navegación y lectura.
4. **Micro 5** para el HUD compacto.

---

## 0. La regla que gobierna todas las demás

Todas son **fuentes de rejilla**: sus contornos caen sobre un número entero de píxeles de diseño, y el número del nombre es la altura de las mayúsculas en esos píxeles. El proyecto las renderiza con `antialiasing=0` y filtro nearest.

De ahí sale la única regla que no admite excepción: **el tamaño de una variación se deriva del em nativo de su familia; no se elige por parecerse a otro número.** Por debajo de unos 0,7 px por píxel de diseño un glifo de rejilla no se vuelve áspero, se rompe — sus elementos redondean a cero o se funden con el vecino.

| Familia | `upm` | Unidad | Em nativo | Tamaños en uso | px por píxel de diseño |
|---|---:|---:|---:|---|---:|
| Jacquard 24 | 1290 | 30 | 43 px | 36, 40, 48 | 0,84 – 1,12 |
| Jacquard 12 | 1260 | 60 | 21 px | 20 | 0,95 |
| Jacquarda Bastarda 9 | 1040 | 80 | 13 px | 32 | 2,46 |
| Jersey 15 | 1350 | 50 | 27 px | 22, 26 | 0,81 – 0,96 |
| Jersey 10 | 1400 | 75 | 18,67 px | 14 – 20 | 0,75 – 1,07 |
| Micro 5 | 1650 | 150 | 11 px | 11, 22 | 1,00 / 2,00 |

La unidad se midió sobre las coordenadas reales de los contornos, no sobre `capHeight`: el `OS/2` de Jacquard 24 declara una mayúscula de 750 unidades que no cuadra con su rejilla de 30.

**Corolario: los tamaños de familias distintas no son comparables.** Micro 5 a 22 px y Jersey 10 a 16 px dibujan ambos una mayúscula de unos 9–10 px. Jersey 10 a 22 px dibujaría 11,8. Mover un número de una variación a otra porque «es el mismo tamaño» es el error que esta tabla existe para impedir.

---

## 1. Jacquard 24 y Jacquard 12

### Función

Blackletter de punto de cruz victoriano, revival de un alfabeto de bordado de Heinrich Kuehn (Berlín, ~1880). Es el nivel más alto de la jerarquía: identidad, importancia, dramatismo. Los títulos son cortos, grandes y se leen una vez, que es exactamente donde una blackletter se gana el sitio.

Las dos son la misma letra a dos resoluciones. Se elige por tamaño de render, no por gusto: Jacquard 24 para 36–48 px, Jacquard 12 para 20 px, donde la rejilla gruesa es la correcta.

### Usos recomendados

- Logo del juego.
- Títulos principales y de pantalla.
- Eventos globales y cambios de era.
- Mensajes de victoria, derrota o crisis.
- Marca del HUD (`HudBrand`, Jacquard 12).

### Restricciones

- No utilizar en párrafos ni en descripciones extensas.
- No utilizar en controles pequeños: por debajo de 36 px, Jacquard 24 cae bajo 0,84 px por píxel de diseño.
- La blackletter destruye el escaneo de cifras densas. Un número dentro de un título está bien; una tabla de números, no.

---

## 2. Jacquarda Bastarda 9

### Función

La bastarda del mismo alfabeto de bordado: la letra más ornamentada de la colección a la resolución más baja. Tiene **un solo uso en todo el juego** — el nombre del fundador, en las dos superficies ceremoniales donde aparece.

### Usos recomendados

- `FounderName`, a 32 px, en `FounderArrivalSequence` y `FounderCardPanel`.

### Restricciones

- Nada más. Su ornamento es virtud a 32 px y ruido en cualquier otro sitio.
- No usarla para «dar sabor» a un panel, un encabezado ni una entrada de crónica. La crónica es un log compacto a 16 px; ahí esta letra es ilegible.

---

## 3. Jersey 15 y Jersey 10

### Función

Sans-serif deportiva y versátil, el caballo de tiro del sistema. Cubre **estructura y lectura**, que en este proyecto comparten familia y se distinguen por tamaño y color, no por corte: ninguna Soft Type tiene negrita ni cursiva.

Jersey 15 para 22–26 px, Jersey 10 para 14–20 px. Es un reparto por resolución: Jersey 15 a 16 px rendiría 0,59 px por píxel de diseño y perdería los astiles.

### Usos recomendados

Jersey 15:

- Encabezados de panel, subtítulos, nombres de edificio, botón primario.

Jersey 10:

- Botones, pestañas, etiquetas y cromo del HUD.
- Párrafos, descripciones, tooltips, diálogos, tablas y cifras de pantalla.

### Restricciones

- No escalar el nodo para cambiar el tamaño; modificar `Font Size`.
- Usar interlineado suficiente en párrafos de varias líneas.
- Jersey 10 no baja de 14 px: a 14 px ya está en 0,75.

---

## 4. Micro 5

### Función

«A teeny-tiny typeface that can fit anywhere.» Cinco píxeles de mayúscula. Es un instrumento de densidad, no una fuente de párrafo, y en este proyecto sostiene el **HUD compacto**, que es la única superficie donde el espacio vertical es el recurso escaso.

Es además la única familia que se usa **exactamente sobre su rejilla**: a 22 px son 2,00 px por píxel de diseño, a 11 px son 1,00. Por eso ocupa los huecos donde no hay margen para equivocarse.

### Usos recomendados

| Variación | Tamaño | Mayúscula | Cabe en |
|---|---:|---:|---|
| `HudBody`, `HudNumeric` | 22 px | 10,0 px | la fila de 24 px (`Tokens.HudRowHeight`) |
| `HudProgress` | 11 px | 5,0 px | la barra de 11 px (`Tokens.HudBarHeightCard`) |
| `HudBadgeNumeric` | 11 px | 5,0 px | la píldora de 18 px (`Tokens.HudBadgeHeight`) |

### Restricciones

- Solo 11 y 22 px. No hay tamaño intermedio: entre uno y otro se sale de la rejilla, y con 5 px de mayúscula medio píxel de desalineación destruye el glifo.
- Las dos filas de 11 px están **firmadas por debajo del suelo** de altura de mayúscula y llevan su propia captura. No añadir una tercera sin la suya.
- No usarla en pantallas. El HUD se lee jugando; una pantalla se lee parado y merece Jersey 10.

---

## Jerarquía general

```text
Jacquard 24 · Jacquard 12
└── Identidad, impacto y eventos principales

Jacquarda Bastarda 9
└── El nombre del fundador, y nada más

Jersey 15 · Jersey 10
└── Navegación, estructura, controles, lectura y datos

Micro 5
└── Filas, cifras e insignias del HUD compacto
```

---

## Asignación en el Theme de Godot

La fuente de verdad es `game/assets/ui/default_theme.tres`. Esta tabla la resume; si discrepan, gana el archivo.

| Variación | Fuente | Tamaño |
|---|---|---:|
| `GameTitle` | Jacquard 24 | 48 px |
| `EventTitle` | Jacquard 24 | 40 px |
| `ScreenTitle` | Jacquard 24 | 36 px |
| `FounderName` | Jacquarda Bastarda 9 | 32 px |
| `HudBrand` | Jacquard 12 | 20 px |
| `PanelTitle` | Jersey 15 | 26 px |
| `SectionTitle` | Jersey 15 | 22 px |
| `BuildingName` | Jersey 15 | 22 px |
| `ButtonPrimary` | Jersey 15 | 22 px |
| `ButtonText` | Jersey 10 | 20 px |
| `ButtonWarning`, `TabText` | Jersey 10 | 18 px |
| `Label`, `LineEdit`, `BodyText`, `DialogText`, `NumericText` | Jersey 10 | 18 px |
| `BodySmall`, `TooltipText`, `TableText`, `ErrorText` | Jersey 10 | 16 px |
| `HudHeader` | Jersey 10 | 18 px |
| `HudLabel`, `HudCaption`, `HudButton*` | Jersey 10 | 16 px |
| `HudBody`, `HudNumeric` | Micro 5 | 22 px |
| `HudProgress`, `HudBadgeNumeric` | Micro 5 | 11 px |

---

## Uso de números

Los números siguen la función del componente, no una fuente única.

- **Micro 5** para las cifras del HUD: recursos, producción, porcentajes, contadores de insignia. `1320 · 42 % · 8/10`.
- **Jersey 10** para cifras dentro de texto de pantalla, tablas y listados. `La mina requiere 12 trabajadores adicionales.`
- **Jersey 15** para números integrados en botones y encabezados. `EXPEDICIONES 3`.
- **Jacquard 24** para números destacados en un título. `DÍA 128`.

---

## Reglas de consistencia

1. No utilizar más de una fuente dentro del mismo componente, salvo excepción justificada.
2. **El tamaño se deriva del em nativo de la familia.** No copiar un número de una variación de otra familia.
3. No utilizar Jacquard ni Jacquarda en párrafos ni en cifras densas.
4. Jacquarda Bastarda 9 tiene un único uso: `FounderName`.
5. No utilizar Micro 5 fuera del HUD compacto.
6. No utilizar las caras de escala de pantalla (Jacquard 24, Jersey 15, Jacquarda) dentro del HUD.
7. Cambiar el tamaño de fuente, no la escala del nodo.
8. Usar tamaños enteros y evitar escalas fraccionarias como `1.25` o `1.5`.
9. Probar siempre los textos en la resolución real del juego.
10. Ninguna Soft Type tiene negrita ni cursiva: la jerarquía se hace con tamaño, resolución y color.
11. Antes de añadir una séptima cara, medir su em nativo y comprobar que no la cubre ya una de las seis a otra resolución.

---

## Configuración en Godot

Las seis se importan igual, y `tools/Test-PixelFontImports.ps1` lo verifica:

```text
antialiasing = 0
hinting = 1
subpixel_positioning = 0
generate_mipmaps = false
multichannel_signed_distance_field = false
oversampling = 0.0
```

Godot detecta estas caras como pixel fonts al importarlas y desactiva el hinting por su cuenta; el proyecto lo devuelve a `1` porque casi ninguna variación cae exactamente sobre rejilla y el hinting ligero ajusta los astiles a píxeles enteros.

También:

- Usar escalado entero siempre que sea posible.
- Evitar transformar el `Control` padre con escalas fraccionarias.
- Configurar las fuentes mediante el `Theme` global, nunca con `AddThemeFontOverride`.

### Estructura real

```text
art/                                 game/assets/ui/fonts/
├── Jacquard_24/   + OFL.txt         ├── Jacquard24-Regular.ttf
├── Jacquard_12/   + OFL.txt         ├── Jacquard12-Regular.ttf
├── Jacquarda_Bastarda_9/ + OFL.txt  ├── JacquardaBastarda9-Regular.ttf
├── Jersey_15/     + OFL.txt         ├── Jersey15-Regular.ttf
├── Jersey_10/     + OFL.txt         ├── Jersey10-Regular.ttf
└── Micro_5/       + OFL.txt         └── Micro5-Regular.ttf
```

`art/` no conserva ninguna cara retirada. Geist Pixel y Pixelify Sans se borraron con las seis nuevas ya en su sitio; viven en el historial de git y, como todas, son OFL y se vuelven a descargar de Google Fonts. Un origen muerto bajo `art/` solo invita a que una sesión futura lo reimporte.

---

## Prueba mínima de caracteres

Antes de aprobar una fuente o tamaño, comprobar:

```text
ABCDEFGHIJKLMNOPQRSTUVWXYZ
abcdefghijklmnopqrstuvwxyz
0123456789

ÁÉÍÓÚ áéíóú
Ññ Üü
¿? ¡!
% + - / :
```

### Texto de prueba

```text
¿La población está satisfecha?
¡Producción aumentada un 25 %!
Médicos disponibles: 4
La mina requiere 12 trabajadores adicionales.
Esperanza de vida: 67 años.
```

La escena que ejerce todo esto es `game/scenes/prototypes/TypographySpecimen.tscn`, capturada por `tools/Capture-TypographySpecimen.ps1` a 1280×720 y 1920×1080.

---

## Criterio final

Cada corte responde a una necesidad distinta:

- **Jacquard atrae la atención.**
- **Jacquarda Bastarda nombra al fundador.**
- **Jersey organiza la interfaz y permite leerla.**
- **Micro 5 hace caber el HUD.**

La identidad visual surge de la jerarquía, de la coherencia de una sola colección y del respeto a la rejilla — no de usar tipografías decorativas en todos los elementos.
