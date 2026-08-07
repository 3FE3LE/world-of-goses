# Licencias y atribución

## Packs de Kenney (CC0 1.0)

Cuatro packs de **Kenney (www.kenney.nl)** están en el repositorio bajo
**Creative Commons Zero 1.0**. CC0 renuncia a los derechos: el uso personal,
educativo y comercial está permitido y **la atribución no es obligatoria** en
ninguno de los cuatro. Aun así acreditamos a Kenney, y a **Lynn Evers** donde
el propio pack la co-acredita.

| Pack | Origen | En el juego |
| --- | --- | --- |
| Pixel UI pack | `art/Kenney/` | `game/assets/ui/kenney/9-slice/` — verde, gris y rojo (estados restantes) y los fondos `ancient_*` |
| UI Pack – Pixel Adventure 2.0 | `art/exports/ui/kenney_ui-pack-pixel-adventure/` | `game/assets/ui/kenney-pixel-adventure/9-slice/` — la superficie pizarra que gobierna el tema |
| Roguelike pack (con Lynn Evers) | `art/exports/ui/kenney_roguelike-rpg-pack/` | `game/assets/terrain/kenney/roguelike-rpg/` — terreno y árboles del macro |
| Cursor Pixel Pack 1.0 | `art/exports/ui/kenney_cursor-pixel-pack/` | `game/assets/ui/cursors/kenney-pixel/` — cursor de recolección |

Cada carpeta promocionada conserva el `LICENSE.txt` original del pack. Ninguno
se extrajo completo: se promocionan archivos concretos según la lista de
comprobación de `docs/ASSET_INVENTORY.md`.

Estos packs son **provisionales como dirección de arte**, no definitivos: la
biblia (`08_VISUAL_UI_AND_ASSET_GUIDELINES.md`) los admite como base mientras
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
