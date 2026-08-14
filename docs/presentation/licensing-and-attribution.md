# Licencias y atribución

## Packs de Kenney (CC0 1.0)

Cuatro packs de **Kenney (www.kenney.nl)** están en el repositorio bajo
**Creative Commons Zero 1.0**. CC0 renuncia a los derechos: el uso personal,
educativo y comercial está permitido y **la atribución no es obligatoria** en
ninguno de los cuatro. Aun así acreditamos a Kenney, y a **Lynn Evers** donde
el propio pack la co-acredita.

| Pack | Origen | En el juego |
| --- | --- | --- |
| Pixel UI pack | `art/Kenney/` | **Ya no está en el juego.** El 2026-08-07 se borró `game/assets/ui/kenney/` completo: sus últimos consumidores (`ButtonWarning` y el relleno de `ProgressBar`) pasaron al pack Pixel Adventure, y el resto (`ancient_*`, `grey*`, `green_pressed`) ya no lo referenciaba nadie. El kit sigue disponible en `art/` como fuente. |
| UI Pack – Pixel Adventure 2.0 | `art/exports/ui/kenney_ui-pack-pixel-adventure/` | `game/assets/ui/kenney-pixel-adventure/9-slice/` — la superficie pizarra que gobierna el tema, más el rojo destructivo y el verde de progreso. 7 de 504 tiles. Es el único pack de UI en el juego, así que ya no se mezclan dos geometrías en la misma familia de botones. |
| Derivados compuestos del pack Pixel Adventure | `tools/New-CompositeStylebox.ps1` sobre `tile_0008` y `tile_0009` | `game/assets/ui/composites/` — marco de tarjeta y marco elevado. Obra derivada de material CC0, que CC0 permite sin condiciones; el `.recipe.json` de cada PNG registra el tile de origen, el relleno y el remapeo de tonos, así que la procedencia es reconstruible desde el repositorio. |
| Roguelike pack (con Lynn Evers) | `art/exports/ui/kenney_roguelike-rpg-pack/` | `game/assets/terrain/kenney/roguelike-rpg/` — terreno y árboles del macro |
| Cursor Pixel Pack 1.0 | `art/exports/ui/kenney_cursor-pixel-pack/` | `game/assets/ui/cursors/kenney-pixel/` — cursor de recolección |

Cada carpeta promocionada conserva el `LICENSE.txt` original del pack. Ninguno
se extrajo completo: se promocionan archivos concretos según la lista de
comprobación de `docs/presentation/asset-inventory.md`.

Estos packs son **provisionales como dirección de arte**, no definitivos: la
guía visual (`visual-language.md`) los admite como base mientras
no exista arte propio, y exige recolorear por tokens sin deformar geometría.

## Universal LPC Spritesheet Character Generator

Los cuerpos y ciclos de movimiento proceden de las bases oficiales de **Universal LPC Spritesheet Character Generator**. Las prendas, símbolos, accesorios, paletas y la composición `slash` de este paquete son adaptaciones originales creadas para World of Goses.

Los assets LPC seleccionados declaran combinaciones de OGA-BY 3.0, CC-BY-SA 3.0 y GPL 3.0. Este paquete incluye:

- `licenses/LPC_SELECTED_CREDITS.csv`: créditos exactos de las cuatro hojas fuente consultadas.
- `licenses/LPC_FULL_CREDITS.csv`: catálogo completo del repositorio oficial, incluido como respaldo.
- `licenses/GENERATOR_GPL-3.0.txt`: licencia del código del generador, separada de las licencias individuales del arte.

Conserva estos archivos en la distribución y ofrece una entrada visible a los créditos desde el juego. Las licencias individuales pueden exigir atribución y condiciones adicionales para obras derivadas. Este documento no sustituye asesoría legal.

## Transformaciones realizadas

- Recoloración por paleta y región corporal.
- Cabezas, cabello, prendas, accesorios y símbolos dibujados como pixel art nuevo.
- Normalización a celdas `128 × 128` con transparencia real.
- Ciclo `slash` de seis frames compuesto sobre poses LPC y arma original.
- La variante femenina de `idle` usa poses neutrales de la hoja oficial `female/walk.png` para mantener el paquete autocontenido.
