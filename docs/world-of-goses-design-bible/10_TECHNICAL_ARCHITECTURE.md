# Arquitectura técnica

> **Canon técnico, no plan de trabajo.** Este capítulo fija el stack, las
> separaciones y los guardarraíles que ninguna implementación puede violar. No
> dice en qué orden se construye: esa secuencia es
> `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §15, y la cola
> accionable es `TO_DO.md`.
>
> El mapa de escenas propuesto y la secuencia original de quince pasos se
> archivaron el 2026-08-07 en
> [`docs/_archive/design-bible-10-prototype-roadmap-2026-08-07.md`](../_archive/design-bible-10-prototype-roadmap-2026-08-07.md):
> describían una estructura de carpetas que la implementación nunca adoptó, y
> presentarla como canon la convertía en una instrucción equivocada.
>
> Cómo está organizado el código **hoy** lo responde `docs/ARCHITECTURE.md`.

## Stack

- Godot .NET.
- C#.
- Windows como entorno inicial.
- VS Code.
- Pixelorama.
- Sixteen Pixel Perfect.
- Backend externo solo cuando exista una necesidad validada.

## Separación

```text
Dominio
Decide qué ocurre y por qué

Aplicación
Orquesta casos de uso

Presentación Godot
Muestra estado e interacción

Assets
Definen apariencia y sonido
```

El dominio no depende de nodos, sprites, animaciones, cámaras, frame rate, input ni rutas de assets.

## Representaciones

```text
Citizen
Dato persistente

MacroCitizenDot
Representación urbana

CitizenDetailedView
Representación de edificio

ExpeditionMemberView
Representación completa
```

## Simulación

Evitar:

- Actualización de cada ciudadano en `_Process`.
- Simular cada segundo offline.
- Un nodo por habitante.
- Pathfinding para población no visible.
- Estado mutable global.
- Event bus prematuro.
- Dependencias innecesarias.

Favorecer eventos discretos, cálculos por lote, datos compactos y estado bajo demanda.

## Pixel perfect

- Resolución lógica: 1280 × 720.
- Filtro nearest.
- Posiciones enteras.
- Escala entera.
- Sin coordenadas fraccionarias para bordes.

## Cámara y mundo caminable

Current direction: the world (macro city and detailed building/garden/gathering
scenes) lives under a `Camera2D`/`Node2D`;
el HUD permanece en un `CanvasLayer` independiente que la cámara nunca afecta.
Esto reemplaza la decisión previa de evitar `Camera2D` para no mover el HUD:
con esta separación de capas, el HUD sigue estable sin necesidad de mantener
el mundo sin cámara.

Dos modos de cámara, independientes de la selección de ciudadano — ver detalle
en `08_VISUAL_UI_AND_ASSET_GUIDELINES.md` ("Profundidad y desniveles") y
`04_CITIZENS_PROFESSIONS_AND_HEROES.md` ("Cámara-sigue"):

- Cámara libre (pan/zoom): siempre disponible, haya o no un ciudadano
  seleccionado. Seleccionar un ciudadano (info/delegación) no la desactiva ni
  la reemplaza.
- Cámara-sigue-ciudadano-seleccionado: toggle explícito aparte, solo posible
  con un ciudadano ya seleccionado; es observación, no control de movimiento,
  y se puede desactivar en cualquier momento para volver a cámara libre.

**Dos modelos de profundidad, no uno solo** (ver
`08_VISUAL_UI_AND_ASSET_GUIDELINES.md`, "Profundidad y desniveles"):
interiores (edificio/jardín/gathering) usan elevación plana
(`TileMapLayer` por nivel + Y-sort); la ciudad macro usa perspectiva pseudo-3D
por calles (escala no-uniforme por profundidad, navegación escalonada). Both
were initially validated in isolation:
`game/scenes/prototypes/WalkableWorldPrototype.tscn` (interiores) y
`game/scenes/prototypes/MacroStreetPerspectivePrototype.tscn` (macro),
The perspective macro city is now integrated into the main scene as the only
playable representation. The former flat renderer was removed and must not be
reintroduced as a fallback or second construction/movement path.

## Guardado

Guardado local estructurado, con versionado de esquema, migraciones,
snapshots y registro de eventos importantes. La dirección quedó fijada y el
código la sostiene: escritura atómica con `.bak`, validación estructural
antes de restaurar, y una cadena de migraciones secuenciales sobre el JSON
crudo.

Ningún backend externo mientras no exista una necesidad validada; Postgres no
se justifica para el prototipo. El número de esquema vigente y el detalle de
cada migración viven en `docs/ARCHITECTURE.md` §8 y en
`docs/session-state/STATE.txt`, no aquí: son medición, no diseño.

## Guardarraíles

- No convertir la ciudad en un colony simulator tradicional.
- No separar héroes y habitantes.
- No convertir linajes en clases profesionales.
- No convertir el eje ambiental en moralidad binaria.
- No convertir el fundador en destino permanente.
- No confundir placeholders con dirección artística final.
- No optimizar antes de medir.
- No añadir datos que no permitan una decisión o comuniquen una consecuencia.

## Preguntas abiertas

- Cosmología común.
- Nombre del eje ambiental.
- Escala temporal.
- Elementos de combate.
- Familias de armas.
- Experiencia y envejecimiento.
- Migración.
- Mezcla cultural.
- Política.
- Economía.
- Capacidad poblacional.
- Música.
- Primer bioma.
- Primer conflicto sistémico.

Estas son preguntas de diseño: nadie ha decidido la respuesta todavía. Las tres
convenciones técnicas que vivían aquí — niveles de elevación del tileset,
factores de la proyección pseudo-3D por calle, y la colisión del término
"calle" entre la perspectiva macro y el corredor de `H-26` — no eran preguntas
abiertas sino trabajo pendiente con dueño, y se movieron a `TO_DO.md`.
