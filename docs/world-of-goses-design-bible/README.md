# World of Goses
## Índice de documentación

Esta carpeta conserva las decisiones de diseño establecidas durante la conceptualización de **World of Goses**.

Sirve para:

- Recontextualizar futuras sesiones.
- Mantener alineados a agentes de diseño y código.
- Evitar que un prototipo provisional reemplace la visión del producto.
- Distinguir decisiones firmes de asuntos todavía abiertos.

## Orden recomendado

1. `01_GAME_VISION.md`
2. `02_CORE_GAMEPLAY_PILLARS.md`
3. `03_CITY_TERRITORY_AND_GROWTH.md`
4. `04_CITIZENS_PROFESSIONS_AND_HEROES.md`
5. `05_EXPEDITIONS.md`
6. `06_LINEAGES.md`
7. `07_ONBOARDING_AND_FOUNDER.md`
8. `08_VISUAL_UI_AND_ASSET_GUIDELINES.md`
9. `09_AUDIO_GUIDELINES.md`
10. `10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`

## Jerarquía de autoridad

1. Las decisiones más recientes y explícitas tienen prioridad.
2. La visión del producto tiene prioridad sobre un prototipo temporal.
3. El dominio tiene prioridad sobre la representación visual.
4. La experiencia del jugador tiene prioridad sobre una simulación exhaustiva sin valor jugable.
5. Una mecánica no se implementa solo porque sea técnicamente posible.

## Estado actual

Ya está definido:

- Juego 2D pixel art en Godot .NET con C#.
- Una ciudad persistente por partida.
- Vista macro, escenas detalladas de edificios y expediciones laterales.
- Habitantes con identidad, competencias múltiples e historia.
- Héroe como rango o función de un habitante, no como entidad separada.
- Ocho linajes fundacionales.
- Afinidades profesionales sin profesiones bloqueadas.
- Expansión territorial mediante expediciones.
- Producción causal basada en población, territorio, herramientas, logística y almacenamiento.
- Interfaz temática por linaje.
- Jerarquía tipográfica de tres niveles.
- Sixteen Pixel Perfect como generador paramétrico de UI para Godot.

Todavía requiere aterrizaje:

- Escala temporal definitiva.
- Fórmulas de aprendizaje, producción y envejecimiento.
- Sistema político completo.
- Nombre final del eje regenerativo/extractivo.
- Elementos, armas y habilidades.
- Mezcla cultural entre linajes.
- Persistencia final y progreso offline.
- Gramática musical completa.
