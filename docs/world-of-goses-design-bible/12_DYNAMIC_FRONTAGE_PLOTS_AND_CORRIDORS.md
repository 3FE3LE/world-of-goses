# Estándar de frente urbano dinámico, reservas de construcción y corredores

**Estado:** dirección aprobada; sustituye la partición rígida de nueve solares
descrita anteriormente en el capítulo 03.

**Aprobado:** 2026-08-03.

**Integración:** `CityParcel` continúa siendo la unidad de territorio. Dentro
de las parcelas disponibles, las filas se resuelven como secuencias continuas
de columnas de frente. Construcciones e infraestructura pueden reservar
intervalos de varias columnas; una unidad de recurso ocupa solo su celda de
frente explícita y no reclama el solar `3×3` que la rodea. Todo elemento que
obstruya declara una huella sólida y clearances. Los árboles son solo un caso
del mismo contrato de obstáculo.

**Primer seam implementado:** schema v25 introduce filas continuas, ventanas
deslizantes de tres columnas, reservas persistentes, corredores protegidos y
perfiles de obstáculo compartidos por recursos y construcciones. Schema v26
añade posiciones unitarias deterministas para recursos: distintos tipos pueden
compartir parcela y fila sin bloquear espacio vacío. El layout fresco dispersa
las unidades proceduralmente, evitando filas repetidas sin convertirlas en
solares. Expansión
lateral, arte por frente e interiores modulares continúan como fases posteriores.

## Propósito

Este documento define el modelo objetivo para reorganizar la colocación, expansión y navegación de edificios en la vista urbana de **World of Goses**.

También conserva el diagnóstico y los criterios de migración usados para
implementar el refactor de forma gradual.

---

# 1. Decisiones de diseño aprobadas

## 1.1 Convención dimensional

Toda dimensión de edificio se expresa como:

```text
frente × profundidad
```

Ejemplos:

```text
3×3 = 3 tiles de frente y 3 tiles de profundidad
4×3 = 4 tiles de frente y 3 tiles de profundidad
5×3 = 5 tiles de frente y 3 tiles de profundidad
6×3 = 6 tiles de frente y 3 tiles de profundidad
```

En nombres de código deben utilizarse propiedades explícitas:

```text
FrontageColumns
DepthRows
```

No utilizar propiedades ambiguas como `width`, `height` o `size = "3x4"` sin indicar qué eje representa cada valor.

---

## 1.2 Profundidad fija

La profundidad de una reserva de construcción permanece fija en:

```text
3 tiles
```

El sistema permitirá crecimiento:

```text
← hacia la izquierda
→ hacia la derecha
↑ mediante nuevas plantas
```

No permitirá crecimiento:

```text
hacia el fondo de la fila
hacia la calle longitudinal
entre filas distintas
```

Regla:

```text
DepthExpansion = NotSupported
FrontageExpansion = Supported
VerticalExpansion = Supported
```

Esta restricción protege:

- La perspectiva por calles.
- La organización de filas.
- La calle frontal obligatoria.
- La oclusión y el orden de renderizado.
- La navegación entre filas.
- La relación entre el edificio, su anchor y la calle.

---

## 1.3 Reserva mínima y expansión máxima

Una construcción estándar comienza normalmente con una reserva:

```text
3×3
```

Puede ampliar su frente, una columna completa cada vez, hasta:

```text
4×3
5×3
6×3
```

La unidad territorial mínima de expansión es:

```text
1 columna de frente × 3 tiles de profundidad
```

No existen expansiones territoriales de `0.5 tiles`.

Los medios tiles solo se utilizan dentro de la reserva para describir:

- El ancho real del asset.
- La huella estructural.
- Los retranqueos laterales.
- La separación física entre edificios.
- La navegación local.

---

## 1.4 El solar no está prepartido de forma rígida

La fila urbana no se divide obligatoriamente desde el principio en cajas fijas de `3×3`.

En su lugar, se modela como una secuencia continua de columnas completas.

Una reserva de construcción se crea cuando el jugador coloca un blueprint sobre un grupo válido de columnas contiguas.

Ejemplo:

```text
Fila urbana:
1 2 3 4 5 6 7 8 9 10 11 12 ...
```

Una construcción base puede reservar:

```text
columnas 1..3
```

Otra puede reservar:

```text
columnas 5..7
```

La columna 4 queda libre y transitable mientras no se reserve.

Las columnas 8 y 9 también permanecen libres. Si posteriormente la columna 10 está disponible, las columnas `8..10` pueden formar una nueva reserva `3×3`.

Regla fundamental:

> El solar es una reserva dinámica de columnas contiguas, no una casilla territorial preasignada e inamovible.

---

# 2. Resoluciones separadas

Debe distinguirse entre la resolución territorial y la resolución interna del edificio.

## 2.1 Resolución territorial

La unidad territorial indivisible es:

```text
FrontageCell = 1 tile de frente × 3 tiles de profundidad
```

La resolución territorial determina:

- Qué columnas están disponibles.
- Qué columnas pertenecen a un edificio.
- Qué columnas están protegidas como corredor.
- Qué columnas contienen infraestructura.
- Dónde puede comenzar una reserva.
- Cuánto puede ampliarse un edificio.

La cuadrícula territorial no necesita medias celdas.

---

## 2.2 Resolución interna del edificio

Dentro de una reserva, la estructura real puede ocupar menos ancho que el terreno reservado.

Ejemplo habitual:

```text
Reserva territorial: 3 tiles
Asset o estructura:   2 tiles
Retranqueo izquierdo: 0.5 tiles
Retranqueo derecho:   0.5 tiles
```

Composición:

```text
[ 0.5 ][ edificio 2 ][ 0.5 ] = reserva 3×3
```

Los retranqueos pueden expresarse mediante unidades internas:

```text
1 tile = 2 clearance units
0.5 tile = 1 clearance unit
```

Esta subdivisión se utiliza en metadata del edificio, navegación y arte. No convierte al mapa en una cuadrícula global de medios tiles.

---

# 3. Conceptos obligatorios

## 3.1 ConstructionRow

Representa una franja horizontal de construcción con profundidad fija de tres tiles.

Responsabilidades:

- Contener una secuencia ordenada de `FrontageCell`.
- Identificar la calle longitudinal asociada.
- Validar que una reserva no cruce a otra fila.
- Resolver coordenadas lógicas antes de aplicar la perspectiva.

Datos conceptuales:

```text
RowId
FrontageCells
LongitudinalStreetId
LogicalDepthIndex
```

---

## 3.2 FrontageCell

Unidad territorial indivisible:

```text
1 tile de frente × 3 tiles de profundidad
```

Estados iniciales recomendados:

```text
Available
ReservedByBuilding
ReservedAsCorridor
Infrastructure
Unavailable
TemporarilyBlocked
```

### Available

- Puede utilizarse para tránsito mientras permanezca libre.
- Puede formar parte de una construcción nueva.
- Puede ser absorbida por una expansión lateral.
- Puede reservarse deliberadamente como corredor.

### ReservedByBuilding

- Pertenece a una `BuildingReservation`.
- No puede utilizarse por otra construcción.
- No implica que toda la celda bloquee navegación.

### ReservedAsCorridor

- Está protegida como vía o espacio público.
- No puede ser consumida por construcciones o expansiones.
- Puede recibir mejoras de camino, drenaje o carretera.

### Infrastructure

- Contiene una infraestructura territorial que impide su uso como construcción normal.
- Ejemplos: canal, vía formal, drenaje mayor o estructura pública.

### Unavailable

- No puede construirse ni transitarse por reglas del terreno o del territorio.

### TemporarilyBlocked

- Permanece territorialmente disponible o pública, pero su navegación está bloqueada temporalmente.
- Ejemplos: obra, derrumbe, mercancía o evento.

---

## 3.3 BuildingReservation

Conjunto contiguo de `FrontageCell` perteneciente a un edificio.

Una reserva puede medir:

```text
3×3
4×3
5×3
6×3
```

Datos conceptuales:

```text
ReservationId
BuildingId
RowId
StartColumn
FrontageColumns
DepthRows = 3
LeftExpansionColumns
RightExpansionColumns
```

Regla:

```text
TotalFrontageColumns =
BaseFrontageColumns
+ LeftExpansionColumns
+ RightExpansionColumns
```

Para el estándar inicial:

```text
BaseFrontageColumns = 3
TotalFrontageColumns ∈ {3, 4, 5, 6}
```

La reserva territorial no equivale a colisión.

---

## 3.4 ObstacleFootprint

Geometría sólida real de cualquier obstáculo dentro de su reserva. Se aplica
por igual a recursos, construcciones e infraestructura.

Determina:

- Qué superficie bloquea el movimiento.
- Dónde se encuentran paredes, columnas o maquinaria.
- Qué accesos existen.
- Qué espacio interno continúa transitable.
- Cómo cambia la navegación al cambiar el recurso o mejorar el edificio.

Nunca debe derivarse automáticamente de los píxeles opacos del PNG.

---

## 3.5 LateralClearance

Espacio libre entre la estructura y los límites laterales de la reserva.

```text
LeftClearance
RightClearance
```

Puede expresarse en `clearance units` de medio tile.

Ejemplos para una reserva base de tres tiles:

### Center

```text
structure_frontage = 2 tiles
left_clearance = 0.5
right_clearance = 0.5
```

### Left

```text
structure_frontage = 2 tiles
left_clearance = 0
right_clearance = 1
```

### Right

```text
structure_frontage = 2 tiles
left_clearance = 1
right_clearance = 0
```

### FullWidth

```text
structure_frontage = 3 tiles
left_clearance = 0
right_clearance = 0
```

No utilizar una alineación ambigua llamada únicamente `side_align`.

Esta regla no depende de la categoría del elemento ni de una excepción visual.
Si dos obstáculos usan la misma reserva y los mismos clearances, producen la
misma topología navegable aunque uno sea un recurso y el otro una construcción.
El asset comunica la apariencia; el perfil de obstáculo autorado define qué
parte es sólida. Nunca se toma automáticamente el rectángulo completo del
solar ni los píxeles opacos del PNG.

### Posiciones de recursos

Una unidad natural usa:

```text
ParcelId
RowWithinParcel
FrontageColumnWithinParcel
ObstacleFootprintId
```

`RowWithinParcel` y `FrontageColumnWithinParcel` identifican una celda, no un
solar de nueve casillas. Varias unidades, incluso de tipos distintos, pueden
compartir una misma fila mientras sus celdas no coincidan. La distribución
inicial selecciona celdas dispersas de forma determinista desde la semilla
persistente del fundador y se guarda explícitamente; no se deriva de
`unitId % 3×3` ni ordena deliberadamente los assets en fila india.

La celda central de la parcela inicial se protege como punto de llegada del
fundador. El asset se ancla con desplazamiento hacia el interior de la fila,
dejando el borde de calle por delante para tránsito y orden de render.

---

## 3.6 OpenStrip

Una o más `FrontageCell` disponibles entre reservas existentes.

Un `OpenStrip`:

- Es transitable mientras permanezca libre.
- No es automáticamente una calle permanente.
- Puede formar parte de una futura construcción.
- Puede ser consumido por la expansión de un edificio contiguo.
- Puede reservarse deliberadamente como corredor.

---

## 3.7 CorridorReservation

Una o más columnas protegidas de manera deliberada para circulación o infraestructura.

Una `CorridorReservation`:

- Es opcional.
- No se crea automáticamente entre todos los solares.
- Impide que una construcción o expansión consuma esas columnas.
- Puede evolucionar visualmente desde tierra transitada hasta calle o carretera.
- Pertenece al sistema urbano, no al asset del edificio.

---

## 3.8 LongitudinalStreet

Calle obligatoria que recorre horizontalmente el frente de una fila de construcciones.

Debe existir como infraestructura lógica independiente.

No debe depender de:

- La transparencia del PNG.
- El margen inferior del asset.
- El offset de renderizado.
- La percepción producida por la proyección.

---

# 4. Ejemplo de una franja de nueve columnas

La expresión “nueve tiles de parcela” debe interpretarse como una franja urbana de nueve columnas disponibles, no como un único solar rígido.

```text
Columnas: 1 2 3 4 5 6 7 8 9
```

## 4.1 Tres construcciones base contiguas

Reservas:

```text
A: 1..3
B: 4..6
C: 7..9
```

Representación:

```text
[A A A][B B B][C C C]
```

Si las tres estructuras ocupan dos tiles y están centradas, sus retranqueos forman:

```text
A.right_clearance 0.5
+
B.left_clearance 0.5
=
1 tile transitable
```

Entre B y C ocurre lo mismo.

No existen columnas territoriales libres, pero sí pueden existir callejones dentro de las reservas.

---

## 4.2 Dos construcciones y espacios deliberados o temporales

Ejemplo:

```text
A: 1..3
Columna 4: libre
B: 5..7
Columnas 8..9: libres
```

Representación:

```text
[A A A][·][B B B][· ·]
```

La columna 4 puede ser:

```text
Available
```

En ese caso:

- Es transitable.
- Puede ser absorbida por A o B.
- Puede formar parte de otra reserva futura.
- No garantiza la permanencia del paso.

También puede ser:

```text
ReservedAsCorridor
```

En ese caso:

- El paso queda protegido.
- A y B no pueden consumirlo.
- Puede recibir una mejora vial.

Las columnas 8 y 9 continúan disponibles, pero aún no forman una reserva base.

Si la columna 10 también está disponible:

```text
8..10 = nueva reserva 3×3 válida
```

Esta reserva puede pertenecer a una región, chunk o parcela territorial adyacente. Los límites visuales o de carga del mapa no deben invalidar una reserva contigua si el dominio confirma disponibilidad.

---

# 5. Construcción mediante ventanas deslizantes

El jugador no seleccionará columnas individuales para ensamblar manualmente una reserva.

Interacción recomendada:

```text
Seleccionar blueprint
→ mover preview sobre una ConstructionRow
→ el sistema evalúa una ventana del ancho requerido
→ confirmar construcción
```

La retícula de colocación representa siempre las columnas de frente y las tres
filas de profundidad completas, incluidas las celdas ocupadas. Un obstáculo no
puede borrar las líneas que explican el espacio: añade un tinte y una marca
`[X]`, pero conserva ambos ejes de la grilla. Antes de cualquier clic, el hover
proyecta la ventana completa y comunica `Available` o el primer bloqueo
devuelto por el dominio (`NaturalResource`, `ReservedByBuilding`,
`ReservedAsCorridor` o territorio no disponible). Color, contorno y texto
`[OK]`/`[X]` se combinan para que la validez no dependa solo del color. La
confirmación no reinterpreta el resultado: usa la misma ventana y validación.

Para una construcción base `3×3`, el preview puede evaluar:

```text
1..3
2..4
3..5
4..6
...
```

Para una construcción futura que nazca con otro ancho permitido, evaluará una ventana del tamaño declarado por el blueprint.

## Algoritmo conceptual

```text
CanReserveBuilding(row, startColumn, frontageColumns):
    require frontageColumns between 3 and 6
    require depthRows == 3

    cells = row.columns[
        startColumn .. startColumn + frontageColumns - 1
    ]

    return
        cells.count == frontageColumns
        and every cell is Available
        and terrain requirements are satisfied
        and street access requirements are satisfied
        and reservation remains inside one ConstructionRow
```

Al confirmar:

```text
ReserveBuilding:
    create BuildingReservation
    mark cells as ReservedByBuilding
    attach BuildingId
    create ObstacleFootprint
    create or update navigation obstacles
    refresh presentation
```

---

# 6. Expansión lateral

## 6.1 Regla general

La expansión lateral incorpora una columna completa y contigua a la reserva actual.

Operaciones:

```text
ExpandLeft
ExpandRight
```

Ejemplos:

```text
3×3 → 4×3
4×3 → 5×3
5×3 → 6×3
```

No se permite superar `6×3` durante esta etapa.

---

## 6.2 Condiciones de expansión

Una expansión puede realizarse cuando la columna candidata:

- Está en la misma `ConstructionRow`.
- Es inmediatamente contigua a la reserva.
- Tiene estado `Available`.
- No pertenece a otro edificio.
- No está reservada como corredor.
- No contiene infraestructura incompatible.
- Cumple las condiciones del terreno.
- No excede el frente máximo del blueprint.

Algoritmo conceptual:

```text
CanExpandLeft(reservation):
    candidate = cell(reservation.startColumn - 1)

    return
        reservation.frontageColumns < reservation.maxFrontageColumns
        and candidate is Available
        and candidate belongs to reservation.rowId
        and expansion rules are satisfied
```

```text
CanExpandRight(reservation):
    candidate = cell(
        reservation.startColumn
        + reservation.frontageColumns
    )

    return
        reservation.frontageColumns < reservation.maxFrontageColumns
        and candidate is Available
        and candidate belongs to reservation.rowId
        and expansion rules are satisfied
```

---

## 6.3 Dirección de expansión

El ancho final no basta para describir el edificio.

Debe conservarse cuánto creció hacia cada costado:

```text
left_expansion_columns
right_expansion_columns
```

Ejemplos para un edificio `5×3`:

```text
Base 3 + 2 izquierda + 0 derecha
Base 3 + 1 izquierda + 1 derecha
Base 3 + 0 izquierda + 2 derecha
```

Esto afecta:

- El anchor visual.
- La posición de entradas.
- Los corredores laterales.
- El asset exterior.
- El interior modular.
- Las conexiones con caminos.

---

## 6.4 Paso informal consumido por una expansión

Ejemplo:

```text
[A A A][·][B B B]
```

La columna libre es transitable, pero continúa disponible.

Si A se amplía hacia la derecha:

```text
[A A A A][B B B]
```

El paso desaparece.

Esto puede permitirse, pero el sistema debe evaluar:

- Si corta una ruta activa.
- Si deja un sector inaccesible.
- Si existe una ruta alternativa.
- Si afecta una entrada.
- Si requiere advertencia al jugador.

Una columna `ReservedAsCorridor` nunca puede consumirse sin una operación explícita de desclasificación.

---

# 7. Expansión estructural dentro de la reserva

Debe distinguirse entre dos operaciones diferentes.

## 7.1 StructuralExpansion

La estructura ocupa más espacio dentro de una reserva existente.

Ejemplo:

```text
Reserva: 3×3
Estructura inicial: 2 tiles centrados
Estructura mejorada: 3 tiles full width
```

No se adquieren nuevas columnas territoriales.

Puede reducir o eliminar retranqueos.

---

## 7.2 ReservationExpansion

El edificio adquiere una nueva columna territorial.

Ejemplo:

```text
Reserva: 3×3
→
Reserva: 4×3
```

La operación consume una `FrontageCell` disponible.

El dominio, la aplicación, la navegación y la persistencia deben tratar ambas operaciones como casos distintos.

---

# 8. Separación física entre edificios

La separación navegable entre dos estructuras se calcula mediante:

```text
corridor_width =
right_clearance(left_building)
+
open_or_reserved_columns_between_reservations
+
left_clearance(right_building)
```

Cada columna intermedia aporta un tile completo.

## Ejemplos

### Center + Center, reservas contiguas

```text
0.5 + 0 + 0.5 = 1 tile
```

Resultado:

```text
callejón peatonal mínimo
```

### FullWidth + Center, reservas contiguas

```text
0 + 0 + 0.5 = 0.5 tiles
```

Resultado:

```text
separación visual o técnica
no es corredor peatonal general
```

### Center + Center, una columna libre

```text
0.5 + 1 + 0.5 = 2 tiles
```

Resultado:

```text
paso amplio o camino local
```

### Edificio alejado del límite + Center, una columna libre

```text
1 + 1 + 0.5 = 2.5 tiles
```

Resultado:

```text
camino amplio con franja lateral
```

---

# 9. Clasificación de corredores

```text
Menos de 1 tile
Separación visual o técnica.
No garantiza tránsito general.

1 tile
Callejón peatonal mínimo.

1.5 tiles
Callejón de 1 tile más una franja lateral de 0.5.

2 tiles
Paso amplio o camino local.

3 tiles o más
Vía amplia, plaza lineal o espacio con potencial constructivo.
```

## 9.1 Tratamiento artístico de 1.5 tiles

Un corredor de `1.5 tiles` no requiere un asset exclusivo de “camino de tile y medio”.

Se compone como:

```text
1 tile de circulación principal
+
0.5 tile de franja lateral
```

La franja lateral permanece del lado que realmente produce el retranqueo.

Puede contener tratamiento no bloqueante:

- Tierra o hierba.
- Drenaje.
- Bordillo.
- Piedra.
- Vegetación baja.
- Elementos de servicio.
- Sombra o alero elevado.

No debe contener obstáculos permanentes que reduzcan el núcleo transitable por debajo del mínimo.

---

# 10. Navegación

Debe eliminarse cualquier equivalencia conceptual como:

```text
solar construido = solar completamente bloqueado
```

Modelo correcto:

```text
BuildingReservation
Reserva territorio y evita solapamientos.

ObstacleFootprint
Bloquea físicamente el movimiento.

LateralClearance
Puede permitir tránsito dentro de la reserva.

OpenStrip
Permite tránsito mientras continúe libre.

CorridorReservation
Garantiza tránsito y protege el espacio.

LongitudinalStreet
Conecta la fila y nunca pertenece al footprint del edificio.
```

El pathfinding debe considerar:

- La geometría sólida.
- El radio del agente.
- Las entradas.
- Los corredores protegidos.
- Los espacios libres temporales.
- Los bloqueos transitorios.

No debe considerar:

- La transparencia del PNG.
- El rectángulo completo de la reserva como obstáculo.
- La apariencia proyectada como fuente de verdad lógica.

La navegación debe actualizarse cuando:

- Se construye un edificio.
- Se expande lateralmente.
- Se modifica su huella estructural.
- Se añade una planta que altera accesos o footprint.
- Se demuele.
- Se reserva o libera un corredor.
- Una obra bloquea temporalmente un paso.

---

# 11. Calles longitudinales y perspectiva por calles

Las calles longitudinales entre filas permanecen protegidas y explícitas.

```text
Fila de construcción
════════════════════ calle longitudinal
Fila de construcción
```

La modularidad descrita en este estándar solo afecta el eje de frente.

No debe permitirse:

- Tomar profundidad de una fila posterior.
- Construir usando columnas de filas diferentes.
- Invadir la calle longitudinal.
- Extender un edificio hacia el fondo.

La proyección perspectiva debe aplicarse después de resolver:

```text
fila lógica
columna lógica
reserva
footprint
anchor
```

La lógica territorial no debe depender de coordenadas ya proyectadas.

---

# 12. Arte exterior

## 12.1 Responsabilidad del asset

El PNG del edificio representa:

- La estructura.
- Su base visual.
- Entradas.
- Retranqueos.
- Sombras propias.
- Elementos elevados.

No representa:

- La calle pública.
- El corredor territorial.
- El ownership del terreno.
- La geometría final de navegación.

Los caminos y terrenos pertenecen a sistemas separados:

```text
terrain/
paths/
streets/
corridors/
```

---

## 12.2 Variantes por frente

Una expansión lateral no debe representarse estirando horizontalmente el mismo PNG.

Estados de frente permitidos:

```text
frontage_3
frontage_4
frontage_5
frontage_6
```

Cada frente puede necesitar variantes según distribución:

```text
expand_left
expand_right
expand_both
```

No todas las combinaciones necesitan un asset diferente cuando el edificio es modular y puede componerse por capas, pero el pipeline debe soportar diferencias asimétricas.

---

## 12.3 Orientaciones exteriores

Cada estado exterior aprobado conserva cinco vistas:

```text
left_side
left_three_quarter
front
right_three_quarter
right_side
```

La orientación se selecciona por cámara y la profundidad se aplica por código.

---

# 13. Interiores

Los interiores detallados utilizan una vista ortográfica fija tipo corte frontal o dollhouse.

No replican la perspectiva por calles.

## 13.1 Correspondencia con el frente exterior

La relación debe ser semántica, no necesariamente pixel por pixel:

```text
3×3 exterior
→ interior base

4×3 exterior
→ interior base + un módulo lateral

5×3 exterior
→ interior base + dos módulos laterales

6×3 exterior
→ interior extendido máximo
```

La dirección de expansión debe conservarse:

```text
módulos agregados a la izquierda
módulos agregados a la derecha
```

## 13.2 Plantas

La expansión vertical no consume `FrontageCell`.

Cada planta puede resolverse como una escena o capa navegable independiente:

```text
Floor0
Floor1
Floor2
Basement
Attic
```

## 13.3 Pipeline interior

Usar:

```text
TileSet + TileMap
```

para:

- Suelos.
- Muros.
- Techos recortados.
- Fondos.
- Elementos repetibles.

Usar:

```text
PackedScene o Node2D
```

para:

- Muebles con estado.
- Estaciones de trabajo.
- Camas.
- Hornos.
- Máquinas.
- Almacenamiento.
- Entradas y escaleras.

La tematización por linaje debe funcionar mediante kits visuales compartidos, no duplicando por completo cada interior ocho veces.

---

# 14. Persistencia

La persistencia debe almacenar explícitamente:

```text
ReservationId
BuildingId
RowId
StartColumn
FrontageColumns
DepthRows
BaseFrontageColumns
LeftExpansionColumns
RightExpansionColumns
ObstacleFootprintId
```

No debe depender exclusivamente de:

- Un índice de solar fijo.
- Una posición visual proyectada.
- El tamaño del PNG.

Al demoler un edificio:

```text
todas las FrontageCell de su BuildingReservation
vuelven al estado correspondiente
```

Normalmente:

```text
Available
```

Pero pueden recuperar otro estado si existe:

- Infraestructura previa.
- Terreno bloqueado.
- Corredor protegido.
- Regla de restauración.

---

# 15. Migración desde el sistema actual

El agente debe investigar cómo se almacenan y calculan actualmente:

- Parcelas y solares.
- Posiciones de edificios.
- Filas.
- Navegación.
- Calles.
- Proyección.
- Anchors.
- Selección y preview.
- Guardado y carga.

El plan debe proponer una migración desde solares fijos hacia reservas dinámicas.

Considerar:

```text
SchemaVersion
MigrationFromFixedPlots
stable BuildingReservationId
row and column coordinates
backward compatibility
fallback reconstruction from current building positions
feature flag for comparing both models
```

No debe asumirse que mover nodos visuales equivale a migrar el dominio.

---

# 16. Casos de aceptación

## Caso A: tres edificios centrados en nueve columnas

```text
A: 1..3
B: 4..6
C: 7..9
```

Cada estructura ocupa dos tiles y está centrada.

Resultado:

```text
A–B: corredor de 1 tile
B–C: corredor de 1 tile
```

---

## Caso B: FullWidth junto a Center

```text
A.right_clearance = 0
C.left_clearance = 0.5
```

Resultado:

```text
0.5 tiles
```

No es una ruta peatonal general.

---

## Caso C: Side-align alejándose junto a Center

El edificio izquierdo deja un tile hacia el límite compartido.

```text
1 + 0.5 = 1.5 tiles
```

Resultado:

```text
callejón + franja lateral
```

---

## Caso D: una columna libre entre dos Center

```text
0.5 + 1 + 0.5 = 2 tiles
```

Resultado:

```text
paso amplio
```

---

## Caso E: espacio disponible reutilizable

```text
A: 1..3
Libre: 4
B: 5..7
Libres: 8..9
```

Si la columna 10 está disponible:

```text
8..10 = nueva reserva 3×3 válida
```

---

## Caso F: corredor protegido

```text
Columna 4 = ReservedAsCorridor
```

Ninguna construcción nueva o expansión puede consumirla.

---

## Caso G: expansión derecha

```text
A 3×3 ocupa 1..3
Columna 4 Available
```

Después:

```text
A 4×3 ocupa 1..4
```

Debe actualizarse:

- Reserva.
- Huella estructural.
- Arte.
- Interior.
- Navegación.
- Persistencia.

---

## Caso H: expansión izquierda

```text
A 3×3 ocupa 5..7
Columna 4 Available
```

Después:

```text
A 4×3 ocupa 4..7
```

`StartColumn` debe cambiar correctamente.

---

## Caso I: expansión máxima

```text
3×3 → 4×3 → 5×3 → 6×3
```

No debe permitirse `7×3` durante esta etapa.

---

## Caso J: expansión bloqueada

La expansión debe rechazarse cuando la columna candidata:

- Pertenece a otro edificio.
- Está reservada como corredor.
- Contiene infraestructura incompatible.
- Está fuera de la fila.
- Está bloqueada por terreno.

---

## Caso K: paso informal consumido

Una columna `Available` utilizada por navegación puede ser absorbida.

El sistema debe:

- Recalcular la ruta.
- Detectar pérdida de conectividad.
- Advertir al jugador cuando corresponda.

---

## Caso L: expansión vertical

Una nueva planta:

- Cambia arte, capacidad e interior.
- No cambia automáticamente `FrontageColumns`.
- No consume columnas laterales.
- No invade profundidad.

---

## Caso M: StructuralExpansion

La estructura pasa de dos tiles centrados a full width dentro de la misma reserva `3×3`.

Resultado:

- La reserva no cambia.
- Los retranqueos sí cambian.
- La navegación lateral puede reducirse.

---

## Caso N: demolición

Un edificio `5×3` libera cinco `FrontageCell`, no tres.

La navegación y las oportunidades de construcción deben recalcularse.

---

## Caso O: guardado y carga

Después de guardar y cargar deben conservarse:

- Frente total.
- Dirección de expansión.
- Posición inicial.
- Corredores protegidos.
- Footprint estructural.
- Conectividad.

---

# 17. Encargo para el agente

Analiza el repositorio actual de **World of Goses**, especialmente los sistemas relacionados con:

```text
parcelas y solares
filas de construcción
colocación de edificios
expansiones
BuildingArt
coordenadas lógicas y proyectadas
perspectiva por calles
anchors
pathfinding y navegación
obstáculos
calles longitudinales
selección y preview de construcción
guardado y carga
migraciones
pruebas existentes
```

Implementa el cambio por fases pequeñas, migrables y verificables.

Entrega un plan técnico para migrar del modelo actual al estándar de `ConstructionRow`, `FrontageCell` y `BuildingReservation` descrito en este documento.

---

## 17.1 Documenta primero el estado actual

Indica:

- Qué clases, escenas, recursos y servicios participan.
- Dónde se asume que cada solar tiene límites fijos.
- Dónde se asume que todos los edificios ocupan `3×3`.
- Dónde se bloquea actualmente la navegación.
- Si la colisión se deriva del solar, del sprite, de un grid o de obstáculos.
- Cómo se calcula la perspectiva por calles.
- Cómo se representan filas y calles.
- Qué datos persistentes dependen del índice del solar.
- Qué pruebas cubren el comportamiento actual.

No adivines. Cita rutas, clases, métodos y líneas relevantes.

---

## 17.2 Propón el modelo objetivo

Define responsabilidades y nombres finales para:

```text
ConstructionRow
FrontageCell
BuildingReservation
ObstacleFootprint
LateralClearance
OpenStrip
CorridorReservation
LongitudinalStreet
```

Aclara qué pertenece a:

```text
Dominio
Aplicación
Presentación Godot
Assets y metadata
```

El dominio no debe depender de nodos, sprites, cámaras ni rutas de assets.

---

## 17.3 Diseña la colocación

Explica:

- Cómo se detectan ventanas contiguas del ancho requerido.
- Cómo se mueve el preview por columnas completas.
- Cómo se confirma o cancela una reserva.
- Cómo se distingue `Available` de `ReservedAsCorridor`.
- Cómo se evita cruzar filas.
- Cómo se mantienen calles longitudinales.
- Cómo se representan `Left`, `Center`, `Right` y `FullWidth`.
- Cómo se valida un blueprint `3×3`, `4×3`, `5×3` o `6×3`.

---

## 17.4 Diseña las expansiones

Explica:

- Cómo se ejecuta `ExpandLeft`.
- Cómo se ejecuta `ExpandRight`.
- Cómo se conserva la dirección de expansión.
- Cómo se limita el máximo a `6×3`.
- Cómo se diferencia `StructuralExpansion` de `ReservationExpansion`.
- Cómo se bloquea una expansión contra corredores protegidos.
- Cómo se detecta pérdida de conectividad.
- Cómo se actualizan arte e interior.

---

## 17.5 Diseña la navegación

Explica:

- Cómo separar reserva territorial y obstáculo físico.
- Cómo transformar footprints y clearances en navegación.
- Cómo tratar corredores de `0.5`, `1`, `1.5`, `2` o más tiles.
- Cómo representar espacios libres transitables pero edificables.
- Cómo actualizar navegación incrementalmente.
- Cómo evitar reconstrucciones costosas de todo el mapa.
- Cómo preservar coordenadas enteras y pixel stability.

---

## 17.6 Diseña el impacto visual

Explica:

- Cómo seleccionará `BuildingArt` el frente correcto.
- Cómo representará expansiones izquierdas y derechas.
- Cómo se mantiene el anchor.
- Qué metadata debe separar canvas, footprint y altura.
- Cómo se integran las cinco vistas.
- Cómo se evita escalar horizontalmente un PNG base.
- Cómo se sincroniza exterior e interior.

---

## 17.7 Diseña la migración

Propón:

- Fases pequeñas y reversibles.
- Adaptadores temporales.
- Migración de partidas guardadas.
- Compatibilidad con edificios actuales.
- Reconstrucción de reservas desde posiciones existentes.
- Feature flags para comparar ambos sistemas.
- Orden recomendado de cambios.

---

## 17.8 Incluye una matriz de pruebas

Debe cubrir como mínimo:

- Reservas solapadas.
- Ventanas deslizantes de tres a seis columnas.
- Construcción en límites de chunks o regiones.
- Reutilización de espacios libres.
- Corredores protegidos.
- Alineaciones laterales.
- Construcción y demolición.
- `3×3 → 4×3 → 5×3 → 6×3`.
- Expansión izquierda y derecha.
- Expansión bloqueada.
- StructuralExpansion.
- Expansión vertical.
- Conectividad entre calles.
- Recalculo de pathfinding.
- Guardado y carga.
- Proyección en filas cercanas y lejanas.

---

## 17.9 Formato de respuesta requerido

Entrega el análisis con esta estructura:

```text
1. Resumen ejecutivo
2. Diagnóstico del sistema actual
3. Suposiciones actuales que deben eliminarse
4. Modelo de dominio propuesto
5. Cambios de aplicación y casos de uso
6. Cambios de presentación Godot
7. Estrategia de colocación y expansión
8. Estrategia de navegación
9. Impacto en arte y metadata
10. Interiores y expansión vertical
11. Persistencia y migración
12. Plan por fases
13. Archivos y clases afectadas
14. Matriz de pruebas
15. Riesgos y decisiones abiertas
16. Recomendación final
```

Cada fase debe incluir:

```text
objetivo
alcance
archivos probables
riesgos
pruebas
criterio de finalización
```

---

# 18. No objetivos del primer refactor

No incluir todavía:

- Construcción libre mediante polígonos.
- Selección manual de medias celdas.
- Expansión sobre el eje de profundidad.
- Edificios que crucen calles longitudinales.
- Frente superior a `6 tiles`.
- Propiedad decimal de solares.
- Autotiling artístico definitivo de todos los caminos.
- Ocho variantes completas por linaje.
- Interiores finales de todos los edificios.
- Optimización prematura sin mediciones.

El primer objetivo es validar:

```text
filas horizontales continuas
+
reservas dinámicas de 3 a 6 columnas
+
profundidad fija de 3 tiles
+
footprints parciales
+
corredores opcionales
+
expansión lateral por columnas completas
+
navegación correcta
+
migración segura
```

---

# 19. Criterio final

El modelo debe permitir:

- Tres construcciones `3×3` contiguas con callejones creados por retranqueos.
- Dos construcciones separadas por una o más columnas libres.
- Caminos opcionales protegidos.
- Espacios transitables que puedan reutilizarse después.
- Nuevas reservas formadas por columnas disponibles de regiones adyacentes.
- Edificios que crezcan de `3×3` a `6×3`.
- Expansiones hacia la izquierda o la derecha.
- Edificios que crezcan verticalmente sin alterar la profundidad.
- Recursos compactos de una celda que puedan compartir parcela y fila.
- Huellas sólidas de recursos, construcciones e infraestructura definidas por
  su perfil de obstáculo y sus clearances.
- Interiores modulares coherentes con el frente exterior.

Regla fundamental:

> Toda reserva y toda obstrucción son geometrías distintas. Una construcción
> reserva entre tres y seis columnas contiguas; cada unidad de recurso ocupa
> solo una celda explícita. Ambos bloquean únicamente su huella sólida definida
> por perfil. Toda celda realmente vacía continúa disponible para formar una
> reserva de construcción, y los clearances permanecen transitables.
