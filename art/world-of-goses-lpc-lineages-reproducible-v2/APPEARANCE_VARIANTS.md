# Variantes visuales precompuestas

`source/recipes/appearance_variants.json` define el piloto de apariencias Ardhen masculino. Cada variante se genera como un bundle completo con las 14 animaciones actuales; no son capas equipables en runtime.

## Variantes del piloto

- `standard`: receta canónica.
- `worker`: paleta terrosa y perfil trasero disponible.
- `traveler`: paleta de viaje y perfil trasero disponible.
- `guard`: contraste oscuro y acento metálico.

Todas conservan la paleta familiar, símbolo y accesorios de Ardhen. `thin`, `young` y cuerpos infantiles no están definidos: requieren fuentes corporales compatibles y no deben simularse cambiando únicamente el tamaño del canvas.

## Generación

Desde la raíz del paquete:

```powershell
.\.venv\Scripts\python.exe .\source\generate_appearance_variants.py
```

La salida queda en:

```text
dist/appearance_variants/ardhen/male/<variant>/
```

El wrapper usa la receta canónica sin modificarla y nunca escribe en `game/assets`. Cada ejecución elimina y regenera sólo el directorio de variantes. La selección de apariencia en `CitizenSpriteBank`/`CharacterVisualRegistry` todavía no está implementada; primero hay que revisar visualmente el piloto.

## Promoción futura

Cuando las variantes estén aprobadas, el runtime puede añadir una dimensión `AppearanceVariant` al registro visual y reemplazar el carrier completo al cambiarla. No se deben mezclar estas salidas con `game/assets/characters/lineages/` hasta definir y probar esa convención.
