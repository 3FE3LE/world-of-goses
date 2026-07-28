# Arquitectura técnica y roadmap

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

## Escenas sugeridas

```text
scenes/
├── city/
│   ├── MacroStreetLiveView.tscn
│   ├── PlotView.tscn
│   └── MacroCitizenDot.tscn
├── buildings/
│   ├── BuildingDetailView.tscn
│   ├── MineDetailView.tscn
│   ├── FarmDetailView.tscn
│   └── HospitalDetailView.tscn
├── gardens/
│   └── GardenDetailView.tscn
├── gathering/
│   └── GatheringDetailView.tscn
├── citizens/
│   ├── CitizenDetailedView.tscn
│   └── CitizenPortraitView.tscn
├── expeditions/
│   ├── ExpeditionView.tscn
│   ├── ExpeditionMemberView.tscn
│   └── ExpeditionSegmentView.tscn
└── ui/
```

`MacroStreetLiveView.tscn` represents the walkable camera world described in
"Cámara y mundo caminable" below, rather than a static view.

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

Todavía no está fijado.

Primera opción: guardado local estructurado, versionado de esquema, migraciones, snapshots y registro de eventos importantes.

Postgres no se justifica para el primer prototipo.

## Primer slice

```text
Ciudad macro
→ mina seleccionable
→ escena detallada
→ ciudadanos asignados
→ producción
→ UI temática
→ audio básico
```

### Contenido

- Asentamiento central.
- Mina.
- Granja.
- Actividad macro.
- Panel superior.
- Menú lateral.
- Tema de un linaje.
- Dos trabajadores iniciales.
- Asignación y remoción.
- Producción y almacenamiento.
- Bloqueos visibles.

## Segundo slice

```text
salida
→ caminar
→ enemigo
→ combate automático
→ destino
→ regreso
```

Usar un ciudadano existente convertido en héroe.

## Orden sugerido

1. Ciudad macro y selección.
2. Escena de mina.
3. Asignación.
4. Producción.
5. Afinidad y experiencia.
6. Almacenamiento y bloqueos.
7. Tema visual.
8. Audio básico.
9. Parcela bloqueada.
10. Expedición.
11. Desbloqueo.
12. Retorno herido.
13. Tratamiento.
14. Guardado.
15. Progreso offline.

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
- Convención de tileset con elevación: cuántos niveles, alto en píxeles por
  nivel, y tiles de rampa/escalera/puente — pendiente de definir en la fase
  de integración técnica de "Cámara y mundo caminable".
- Convención de proyección pseudo-3D por calle (ciudad macro): factores de
  achicamiento vertical/horizontal por profundidad, cuántas calles visibles
  simultáneamente, número final de calles de la ciudad.
- Colisión de nombres: "calle" (fila de profundidad de la perspectiva macro)
  vs. "calle" de `H-26` (corredor de 2 tiles para navmesh) — reconciliar o
  renombrar cuando se retome `H-26`/`S-1.2`.
