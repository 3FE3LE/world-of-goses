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
11. `11_ELEMENTAL_AFFINITIES_AND_WORLD_INTERACTIONS.md`
12. `12_DYNAMIC_FRONTAGE_PLOTS_AND_CORRIDORS.md`
13. `13_KOVARI_CUBE.md`
14. `14_LINEAGES_ARDHEN.md`
15. `15_LINEAGES_EIRUNE.md`
16. `16_LINEAGES_KOVARI.md`
17. `17_LINEAGES_MYRVEN.md`
18. `18_LINEAGES_VAELUN.md`
19. `19_LINEAGES_ORVETH.md`
20. `20_LINEAGES_CAELITH.md`
21. `21_LINEAGES_THERYN.md`
22. `22_STATISTICS_PROGRESSION_AND_COMBAT_FORMULAS.md`
23. `23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`

> **Numeración.** El número es la identidad estable del capítulo, no su
> posición en la lista: nunca se reutiliza ni se reordena. Un capítulo nuevo
> toma el siguiente número libre. Los capítulos 11, 22 y 23 vivieron un tiempo
> en la raíz de `docs/` y entraron aquí el 2026-08-07; 22 y 23 conservan el
> título pero cambiaron de número porque el 12 y el 19 ya estaban ocupados.

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
- Ocho linajes fundacionales con cultura, sistema jugable, firma y vértice del Cubo (capítulos 14–21).
- Cubo Kovari como sistema mecánico común: tres ejes (Cuerpo/Vínculo, Estabilidad/Impulso, Dominio/Alcance), seis caras elementales, ocho vértices, stats derivados, equipamiento y modo sombra (capítulo 13).
- Afinidades elementales (Tierra, Agua, Fuego, Aire, Éter, Silencio) como contrato común entre onboarding, equipamiento, ambiente, ciudad y combate, no como seis escuelas de daño (capítulo 11).
- Estadísticas derivadas, progresión y fórmulas de combate en versión de prototipo `v0.1`; los coeficientes son balance inicial, no arquitectura (capítulo 22).
- Primera noche del fundador y espíritu de fuego: ruta lineal, reacciones textuales por linaje, salida del espíritu al amanecer (capítulo 23, DEC-0014).
- Afinidades profesionales sin profesiones bloqueadas; ocho enfoques por oficio.
- Expansión territorial mediante expediciones.
- Producción causal basada en población, territorio, herramientas, logística y almacenamiento.
- Reservas urbanas dinámicas por columnas, huellas físicas parciales y corredores transitables.
- Interfaz temática por linaje.
- Jerarquía tipográfica de tres niveles.
- Sixteen Pixel Perfect como generador paramétrico de UI para Godot.

Todavía requiere aterrizaje:

- Escala temporal definitiva.
- Fórmulas de aprendizaje, producción y envejecimiento.
- Sistema político completo.
- Nombre final del eje regenerativo/extractivo.
- Mezcla cultural entre linajes.
- Persistencia final y progreso offline.
- Gramática musical completa (más allá del esqueleto Theryn del capítulo 21).
