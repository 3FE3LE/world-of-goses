# Ciudad, territorio y crecimiento

## Vista macro

La pantalla principal muestra parcelas, edificios, caminos, rutas, recursos,
zonas bloqueadas, infraestructura y actividad urbana desde una perspectiva
ortogonal elevada. La cuadrícula y las siluetas se leen de frente y desde
arriba; la dirección pseudoisométrica queda descartada para preservar claridad,
coste de producción de assets y compatibilidad con parcelas desbloqueables.

Los habitantes macro son representaciones de 4 a 8 píxeles. Comunican tránsito y vida, pero no representan uno a uno toda la población ni ejecutan simulación completa.

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
