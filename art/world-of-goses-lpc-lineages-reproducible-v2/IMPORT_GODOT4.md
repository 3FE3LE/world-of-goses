    # Importación en Godot 4

    ## Instalación

    1. Copia `assets/`, `scripts/` y `docs/` junto a `project.godot`.
    2. Abre Godot y espera la importación de PNG.
    3. Instancia una escena desde `res://assets/characters/lineages/<linaje>/<male|female>/`.

    ## Animaciones generadas

    - `idle_down`, `idle_left`, `idle_up`, `idle_right`
- `walk_down`, `walk_left`, `walk_up`, `walk_right`
- `slash_down`, `slash_left`, `slash_up`, `slash_right`

    Animaciones sin loop: `slash_*`.

    Todas las celdas miden `128 × 128`, usan transparencia real y baseline `(64, 126)`.
    Las escenas fuerzan `Texture Filter = Nearest` y `offset = Vector2(0, -62)`.

    ## Uso desde C#

    ```csharp
    sprite.PlayIdle(Vector2.Down);
    sprite.PlayWalk(velocity);
    sprite.PlaySlash(Vector2.Right);
    sprite.PlayDirectional("run", velocity); // cuando run exista en build.json
    ```
