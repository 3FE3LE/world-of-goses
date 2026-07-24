# Importación en Godot 4

## Instalación

1. Copia `assets/`, `scripts/` y `docs/` junto a `project.godot`.
2. Abre Godot y espera la importación de PNG.
3. Instancia una escena desde `res://assets/characters/lineages/<linaje>/<male|female>/`.

## Animaciones generadas

El build actual exporta catorce animaciones, cada una en cuatro direcciones:

- `idle`: 2 frames, 3 FPS, loop.
- `combat_idle`: 2 frames, 4 FPS, loop.
- `walk`: 9 frames, 9 FPS, loop.
- `run`: 8 frames, 12 FPS, loop.
- `jump`: 5 frames, 9 FPS, sin loop.
- `climb`: 6 frames, 6 FPS, loop; usa `sheet_mirror`.
- `sit`: 3 frames, 3 FPS, loop.
- `hurt`: 6 frames, 9 FPS, sin loop; usa `sheet_mirror`.
- `slash`: 6 frames, 11 FPS, sin loop.
- `thrust`: 8 frames, 12 FPS, sin loop.
- `halfslash`: 6 frames, 11 FPS, sin loop.
- `backslash`: 13 frames, 14 FPS, sin loop.
- `shoot`: 13 frames, 14 FPS, sin loop.
- `spellcast`: 7 frames, 10 FPS, sin loop.

Las celdas miden `128 × 128`, usan transparencia real y baseline `[64, 126]`. Las escenas fuerzan `Texture Filter = Nearest`, `centered = true` y `offset = Vector2(0, -62)`.

Las fuentes LPC usan celdas de `64 × 64`. El generador las coloca sin escalado en la mitad inferior del canvas 128×128. `climb` y `hurt` parten de una única fila: down/up reciben la fila original y left/right reciben su espejo horizontal. El idle femenino utiliza el fallback definido en `build.json` a partir de columnas neutrales de walk.

## Uso desde C#

El recurso `SpriteFrames` contiene las catorce animaciones. El adaptador del proyecto puede exponer sólo las que necesita el gameplay; también es posible usar el método direccional por nombre cuando exista la animación:

```csharp
sprite.PlayIdle(Vector2.Down);
sprite.PlayWalk(velocity);
sprite.PlaySlash(Vector2.Right);
sprite.PlayDirectional("run", velocity);
sprite.PlayDirectional("spellcast", Vector2.Down);
```

## Licencias

Conserva `docs/licenses/` al distribuir el paquete. Las hojas corporales proceden del ecosistema Universal LPC; las paletas, cabezas, cabello, prendas, símbolos y accesorios de World of Goses son transformaciones originales documentadas en `LICENSING_AND_ATTRIBUTION.md`.

## Límites del paquete

El generador produce personajes completos aplanados en PNG. Esta versión no exporta capas de ropa independientes ni equipamiento intercambiable en runtime. No copies el `scripts/visual/LineageSpritePlayer.cs` del paquete sobre un proyecto que ya tenga su propia implementación compatible: el archivo del paquete es una plantilla de ejemplo.
