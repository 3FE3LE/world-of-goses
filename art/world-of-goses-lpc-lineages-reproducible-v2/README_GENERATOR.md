# Generador reproducible de linajes

Este paquete usa como fuente canónica los cuerpos LPC incluidos en `source/lpc_bases/` y una receta explícita para cada linaje. No depende de que ChatGPT recuerde el sprite ni de volver a acertar una combinación visual.

## Inicio rápido en Windows

Desde PowerShell:

```powershell
.\build.ps1
```

El resultado queda en:

```text
dist/world-of-goses-lpc-lineages/
dist/world-of-goses-lpc-lineages-godot4.zip
```

## Cambiar colores o perfiles

Edita:

```text
source/recipes/lineages.json
```

Ejemplo:

```json
{
  "key": "ardhen",
  "colors": {
    "primary": "#72685D",
    "secondary": "#A65D38",
    "accent": "#D7A35F",
    "skin": "#99634A",
    "hair": "#3A2924"
  },
  "profiles": {
    "accessories": "ardhen",
    "back": "none",
    "female_hair_back": "braid",
    "weapon": "sword"
  }
}
```

Perfiles disponibles actualmente:

- `accessories`: `ardhen`, `eirune`, `kovari`, `myrven`, `vaelun`, `orveth`, `caelith`, `theryn`.
- `back`: `none`, `vine`, `mantle`.
- `female_hair_back`: `bun`, `braid`, `mechanical_ponytail`, `long_locks`.
- `weapon`: cualquier entrada definida en `weapons` dentro del mismo JSON.

Puedes intercambiarlos. Por ejemplo, un Caelith con moño:

```json
"female_hair_back": "bun"
```

### Cambiar solo male o female

Cada linaje incluye `variants.male` y `variants.female`. Solo escribe los campos que quieras sobrescribir:

```json
"variants": {
  "male": {"colors": {}, "profiles": {}},
  "female": {
    "colors": {"hair": "#6A4036"},
    "profiles": {"female_hair_back": "bun"}
  }
}
```

También puedes probarlo sin editar el archivo:

```powershell
.\build.ps1 -Lineage ardhen -Set 'ardhen.variants.female.colors.hair=#6A4036'
```

## Probar un cambio sin editar JSON

PowerShell requiere comillas alrededor de los colores porque `#` inicia comentarios:

```powershell
.\build.ps1 -Lineage ardhen -Set 'ardhen.colors.primary=#5F6259'
```

Varios linajes:

```powershell
.\build.ps1 -Lineage ardhen,eirune -Set `
  'ardhen.colors.accent=#E0A458', `
  'eirune.colors.primary=#47795A'
```

## Animaciones incluidas

La receta `source/recipes/build.json` declara 14 animaciones por linaje y género. Todas las hojas proceden del **Universal LPC Spritesheet Character Generator** y se localizan en `source/lpc_bases/`.

| Animación      | Frames | Loop | Modo          | Notas                              |
|----------------|-------:|:----:|---------------|------------------------------------|
| `idle`         |      2 |  sí  | `sheet`       | `female` usa fallback a `walk`     |
| `combat_idle`  |      2 |  sí  | `sheet`       |                                    |
| `walk`         |      9 |  sí  | `sheet`       |                                    |
| `run`          |      8 |  sí  | `sheet`       |                                    |
| `jump`         |      5 |  no  | `sheet`       |                                    |
| `climb`        |      6 |  sí  | `sheet_mirror`| LPC provee solo 1 fila (frontal)   |
| `sit`          |      3 |  sí  | `sheet`       |                                    |
| `hurt`         |      6 |  no  | `sheet_mirror`| LPC provee solo 1 fila (frontal)   |
| `slash`        |      6 |  no  | `sheet`       | Hoja LPC oficial (arma ya pintada) |
| `thrust`       |      8 |  no  | `sheet`       |                                    |
| `halfslash`    |      6 |  no  | `sheet`       |                                    |
| `backslash`    |     13 |  no  | `sheet`       |                                    |
| `shoot`        |     13 |  no  | `sheet`       |                                    |
| `spellcast`    |      7 |  no  | `sheet`       |                                    |

`emote.png` queda fuera (es un set de retratos, no movimiento in-world).

## Agregar una animación LPC

Las animaciones se declaran en:

```text
source/recipes/build.json
```

Para añadir `run`:

1. Exporta o copia una hoja LPC de cuatro filas a:

   ```text
   source/lpc_bases/body_male_run.png
   source/lpc_bases/body_female_run.png
   ```

2. Cada celda fuente debe medir `64×64`; las filas deben seguir `down`, `left`, `up`, `right`.
3. Añade la entrada en `animations`:

```json
{
  "name": "run",
  "mode": "sheet",
  "source": "body_{gender}_run.png",
  "frames": 8,
  "fps": 12.0,
  "loop": true
}
```

4. Ejecuta `build.ps1`.

El motor aplicará automáticamente la misma piel, ropa, cabeza, cabello y accesorios a cada frame, exportará tiras `128×128` y actualizará los `SpriteFrames.tres`.

### Hojas de una sola fila (`sheet_mirror`)

`climb` y `hurt` en el set LPC solo proveen una fila de 6 frames (la pose frontal, sin variantes de dirección). El modo `sheet_mirror` replica esa fila en las cuatro ranuras de dirección:

- `down` y `up` reciben la fila original tal cual.
- `left` y `right` reciben la fila espejada horizontalmente.

El motor sigue aplicando el pipeline cultural encima (cabello, cabeza, accesorios por dirección), de modo que las poses laterales conservan el mismo silhouette que en `walk` o `run`.

### Una animación que no exista en LPC

Necesita primero una hoja corporal coherente con el resto. El script automatiza composición y exportación, pero no inventa frames anatómicos nuevos. Ese sería el reino de Pixelorama, donde cada codo exige sus impuestos.

## Comandos directos

```powershell
.\.venv\Scripts\python.exe .\source\generate_lineage_sprites.py --help
```

Ejemplos:

```powershell
# Todos
.\build.ps1

# Solo Ardhen masculino
.\build.ps1 -Lineage ardhen -Gender male

# Sin crear ZIP
.\build.ps1 -NoZip
```

## Promover los assets al proyecto Godot

El generador escribe todo en `dist/world-of-goses-lpc-lineages/`. Para integrarlo al proyecto Godot `game/` hay que copiar dos subárboles — **no todos**:

```powershell
# Assets de personajes (tiras PNG, .tres, .tscn, metadata)
Copy-Item -Recurse -Force `
  'dist\world-of-goses-lpc-lineages\assets\characters\lineages\ardhen' `
  'game\assets\characters\lineages\ardhen'
# Repetir para cada linaje (caelith, eirune, kovari, myrven, orveth, theryn, vaelun).

# Documentación (manifest, matriz, licencias)
Copy-Item -Recurse -Force `
  'dist\world-of-goses-lpc-lineages\docs\*' `
  'game\docs\'
```

El `LineageSpritePlayer.cs` que produce `write_csharp()` **no debe copiarse** si ya existe `game/scripts/visual/LineageSpritePlayer.cs`: la versión de `game/` expone todas las 14 animaciones (`PlayIdle`, `PlayCombatIdle`, `PlayWalk`, `PlayRun`, `PlayJump`, `PlayClimb`, `PlaySit`, `PlayHurt`, `PlaySlash`, `PlayThrust`, `PlayHalfslash`, `PlayBackslash`, `PlayShoot`, `PlaySpellcast`, `ResumeIdle`) y el flujo `PlayDirectional(string, Vector2)`. La versión generada sirve únicamente como punto de partida para proyectos que aún no tienen el runtime.

## Archivos importantes

```text
source/recipes/lineages.json       Identidad visual y paletas
source/recipes/build.json          Animaciones, FPS y fuentes
source/generate_lineage_sprites.py Motor determinista
source/lpc_bases/                  Hojas corporales LPC congeladas
source/reference/06_LINEAGES.md    Referencia narrativa
source/vendor/                     Créditos y licencia del generador
```

## Relación con la web LPC

La web oficial puede exportar e importar JSON y producir ZIPs por animación. Este paquete no reconstruye personajes mediante los parámetros del hash de la URL, porque las capas culturales de World of Goses son overlays originales del script. La URL oficial se conserva en la receta para consultar o incorporar nuevas hojas LPC.
