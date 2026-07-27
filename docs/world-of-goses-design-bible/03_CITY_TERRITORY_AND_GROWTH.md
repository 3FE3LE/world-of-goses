# Ciudad, territorio y crecimiento

## Vista macro

La pantalla principal muestra parcelas, edificios, caminos, rutas, recursos,
zonas bloqueadas, infraestructura y actividad urbana desde una perspectiva
ortogonal elevada. La cuadrícula y las siluetas se leen de frente y desde
arriba; la dirección pseudoisométrica (proyección diagonal) queda descartada
para preservar claridad, coste de producción de assets y compatibilidad con
parcelas desbloqueables. Esto no excluye la perspectiva pseudo-3D por calles
descrita abajo: es una vista frontal con escala por profundidad (estilo
juegos de carreras Atari), no una proyección isométrica/diagonal.

Los habitantes macro son representaciones de 4 a 8 píxeles. Comunican tránsito y vida, pero no representan uno a uno toda la población ni ejecutan simulación completa.

### Dirección futura: mundo macro caminable

La vista macro evoluciona de panel estático a un mundo caminable con cámara,
en vez de un lienzo fijo — con una perspectiva pseudo-3D por calles (ver
`08_VISUAL_UI_AND_ASSET_GUIDELINES.md`, "Ciudad macro (perspectiva por
calles)"): los elementos más lejos (arriba en pantalla) se ven más pequeños y
angostos; los más cerca (abajo), más grandes y anchos, al estilo de las
pistas de carreras Atari. La ciudad se organiza en calles — filas discretas
de profundidad, distintas de la "calle" de `H-26` (corredor de 2 tiles para
navmesh; ver desambiguación en el doc visual). Avanzar/retroceder en
profundidad es una transición escalonada entre calles adyacentes, con una
animación breve y cuantizada, nunca un scroll continuo; el desplazamiento
horizontal dentro de una calle es cuantizado pero medianamente libre.

**Seleccionar un ciudadano** (con `CitizenId` real) es independiente de la
cámara: sirve para ver su info y delegarlo a una zona/asignación, y no mueve
la cámara por sí solo. La **cámara libre** (pan/zoom) sigue disponible en todo
momento, haya o no un ciudadano seleccionado — en la ciudad macro, ese paneo
libre es él mismo cuantizado/escalonado por calle, no un arrastre continuo.

**Cámara-sigue** es un modo aparte que el jugador activa explícitamente (un
toggle) sobre el ciudadano ya seleccionado. Es una función de observación, no
de control: el ciudadano sigue moviéndose por su propia agenda/asignación
(delegación), el jugador solo decide a quién mirar, y puede desactivar el
seguimiento en cualquier momento para volver a pan/zoom libre. No aplica a los
puntos macro genéricos de 4-8 píxeles, solo a un ciudadano explícitamente
seleccionado y con seguimiento activado.

La selección de parcelas y edificios por clic se mantiene igual; ahora ocurre
dentro de un mundo con cámara en vez de sobre una vista estática. Ver
"Profundidad y desniveles" en `08_VISUAL_UI_AND_ASSET_GUIDELINES.md` para la
mecánica visual, y "Cámara y mundo caminable" en
`10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md` para la arquitectura técnica. Esta
es una dirección documentada para una fase de integración posterior; el
prototipo actual permanece vigente hasta entonces.

## Escenas detalladas de edificios

Seleccionar una mina, granja, hospital o taller abre una estancia visual propia.

Ejemplo:

```text
Trabajadores asignados: 18
Capacidad visual: 4
Trabajadores visibles: 4
Trabajando dentro: 14
```

Cada trabajador visible corresponde a un `CitizenId` real. Al reasignarlo, abandona visualmente la escena y cambia su asignación lógica.

## Parcelas

Una parcela puede contener:

- Terreno.
- Recursos.
- Fertilidad.
- Agua.
- Amenazas.
- Ruinas.
- Poblaciones.
- Infraestructura.
- Estado de exploración.
- Estado de seguridad.
- Conexiones.
- Uso actual.

La unidad urbanística estándar de una parcela es una matriz de 3 × 3 solares.
Cada solar equivale a 3 × 3 tiles visuales, pero la geometría lógica admite
medios tiles para diferenciar el área reservada de la huella sólida de un
edificio. Los márgenes de edificios adyacentes pueden combinarse en pasillos
de 0.5 tile, caminos de 1 tile o calles de 2 tiles. Esta granularidad no implica
colocación libre: los edificios se ajustan a solares y los complejos pueden
reservar varios solares contiguos o una parcela completa.

Estados sugeridos:

```text
Desconocida
Reconocida
En exploración
Amenaza identificada
Ruta parcial
Ruta segura
Disponible
En explotación
Preservada
Urbanizada
Degradada
Restaurada
```

## Expansión

Una región puede requerir varias expediciones.

Al completarlas:

- Se establece acceso.
- Se revelan recursos.
- Se desbloquean usos.
- Se habilita producción o construcción.
- Puede comenzar la transformación ecológica.

## Ejes de crecimiento

La ciudad no tiene un único nivel.

### Antigüedad e historia

Tiempo, generaciones, acontecimientos, desastres, reformas y obras históricas.

### Desarrollo cultural

Instituciones, artes, educación, identidades, integración de linajes y prestigio profesional.

### Desarrollo político

Administración, representación, leyes, derechos, corrupción y estabilidad.

### Desarrollo económico

Producción, transformación, comercio, reservas, distribución y desigualdad.

### Desarrollo geográfico

Parcelas conocidas, rutas, territorio, infraestructura, fronteras y acceso a biomas.

### Complejidad demográfica

Población, linajes, edades, profesiones, migración y dependencia.

### Cobertura profesional

Cantidad, redundancia, maestros, aprendices, sustitución e instituciones.

### Longevidad y bienestar

Esperanza de vida, recuperación, salud, seguridad, nutrición, vivienda y calidad laboral.

## Edificios

No se desbloquean solo por nivel. Requieren conocimiento, planos, política, materiales, profesionales, territorio, infraestructura y demanda.

## Apariencia cultural

La arquitectura final depende de:

```text
Linaje fundador
+ población actual
+ recursos locales
+ tecnologías
+ políticas
+ orientación ambiental
+ historia
```
