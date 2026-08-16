# Habitantes, profesiones y héroes

## Entidad única

El dominio tiene una entidad principal:

```text
Citizen
```

No crear entidades o subclases separadas para héroe, minero, médico, artesano, líder o aventurero. Son asignaciones, competencias, rangos, membresías, reconocimientos o historial.

## Identidad acumulativa

Una persona puede ser simultáneamente minera veterana, médica en formación, heroína activa, instructora y representante política.

Cambiar de profesión no elimina la vida anterior.

## Componentes conceptuales

```text
Citizen
├── identidad
├── linaje
├── apariencia
├── edad
├── aptitudes
├── competencias
├── experiencia profesional
├── salud
├── rasgos
├── educación
├── asignaciones
├── membresías
├── rangos
├── reconocimientos
├── relaciones
└── historia
```

La implementación inicial debe ser mínima y crecer según el gameplay.

## Salud persistente y stamina

Las heridas persistentes y la stamina son estados distintos. La stamina
representa esfuerzo y recuperación cotidiana; descansar puede restaurarla,
pero nunca cura por sí solo una herida. Una herida sí puede limitar la stamina
utilizable y restringir trabajo o expediciones.

La recuperación de una herida requiere infraestructura de refugio, tiempo y
recursos explícitos. Esta separación permite que una persona esté descansada
pero todavía herida, y evita convertir una consecuencia duradera en otra barra
que se rellena automáticamente.

## Habilidades

Todos pueden desarrollar todas las competencias. La interfaz puede mostrar las tres más destacadas, pero el dominio no limita a tres.

## Aptitud, experiencia y oportunidad

- **Aptitud:** facilidad personal.
- **Experiencia:** práctica y eventos vividos.
- **Oportunidad:** acceso a maestros, instituciones, herramientas, educación y tiempo.

### Cómo se traduce mecánicamente

Una aptitud **no produce nada**. Cambia lo que cuesta aprender:

```text
experiencia_para_subir_de_nivel = requisito_base / factor_de_aprendizaje
```

El factor sale de cuántas de las tres aptitudes del `Citizen` aceleran esa
competencia (`AptitudeLearning`). Dos ciudadanos del mismo nivel de cantería
pican lo mismo; el que tiene la aptitud llegó antes a ese nivel.

Esto es lo que la define frente a la afinidad de linaje: **la aptitud es
individual y el linaje no la determina**. Y es lo que la mantiene dentro del
pilar, porque una ventaja automática de producción concedida por la identidad
está prohibida, mientras que "facilidad personal para aprender" es exactamente
lo que esta sección venía describiendo.

Antes la única lectura mecánica de toda la lista de aptitudes era un bonus plano
al trabajo de construcción por tick, que es justo la forma prohibida.

Las competencias de ciudad tienen nivel derivado de la experiencia acumulada
(`CityCompetency`); la experiencia es lo que se persiste, el nivel se calcula.
Los valores viven en el dominio y son provisionales.

### El fundador también tiene aptitudes

El cuestionario del onboarding puntúa el eje de aptitud en más de treinta
respuestas. Ese eje es salida canónica: DEC-0013 enumera lo que el onboarding
**no** debe producir —preferencias de arma, afinidades profesionales, estilo de
combate, orientación política, postura espiritual, estilo de liderazgo, perfil
de riesgo y rasgos— y las aptitudes nunca estuvieron en esa lista.

Ejemplo:

```text
Crece en una cantera
↓
Desarrolla minería y resistencia
↓
Participa en una expedición
↓
Atiende una emergencia médica
↓
Obtiene experiencia contextual
↓
Comienza formación clínica
```

## Afinidad de linaje

El linaje puede modificar aprendizaje inicial, comprensión, retención, transferencia entre habilidades, errores de principiante, adaptación a herramientas y prestigio cultural.

No bloquea profesiones, no garantiza competencia y no sustituye experiencia.

## Héroe

Héroe es una condición social y funcional vinculada a expediciones.

Estados posibles:

```text
Aspirante
Héroe activo
Héroe veterano
Héroe retirado
```

Cualquier ciudadano puede incorporarse por decisión del jugador.

## Fundador

El fundador influye en conocimiento, prestigio profesional, instituciones, políticas y cultura inicial. Su legado puede transformarse, ser cuestionado o desaparecer.

## Escala poblacional

Separación obligatoria:

```text
Citizen = dato persistente
CitizenView = representación visual temporal
```

No debe existir un nodo Godot activo por ciudadano.

## Quién llega a la ciudad

Un migrante no se sortea con azar libre: la progresión offline y la reproducción
de una partida guardada exigen que el mundo se rehaga a partir de lo que
almacenó. Su identidad es **función pura de tres entradas** —el fundador, el
tick en que fue hospedado y su id de ciudadano— y de ellas salen linaje, cuerpo,
afinidad, cubo, aptitudes, nombre y oficios previos (`MigrantGenerator`).

Las tres entradas son obligatorias juntas:

- **El fundador** es lo único único por partida y fijo durante toda su vida. Sin
  él en la semilla, dos ciudades distintas reciben exactamente a la misma
  persona, porque el primer migrante de cualquier ciudad tiene siempre el id 2.
- **El tick de llegada** distingue a dos migrantes de la misma ciudad y por eso
  se persiste (esquema v36). Un save que lo pierde no puede regenerar a la misma
  persona.
- **El id de ciudadano** los distingue dentro del mismo tick.

Un migrante **llega con oficio**. Tuvo una vida antes de la ciudad, así que trae
experiencia previa en hasta tres competencias, nunca por encima de un nivel
modesto: aceptar a uno es una decisión —un cantero y un recolector valen cosas
distintas el mismo día— y no añadir otro trabajador en blanco idéntico.

Lo que **no** hay todavía: convenciones de nombres por linaje. Los ocho
documentos de linaje definen gramática visual y sonora pero ninguno dice cómo
suena un nombre Kovari frente a uno Theryn, así que el repertorio es común a
todos. Es un hueco de lore, no una decisión.

## Cuánto se tarda en cruzar la ciudad

La duración de un trayecto **sale de la distancia**, no al revés
(`CityTravel`). Antes toda travesía costaba una constante fija, y como la
distancia sí variaba, el mismo ciudadano parecía ir despacio a un sitio cercano
y deprisa a uno lejano.

```text
duración = distancia_de_rejilla / MovementSpeed
distancia_de_rejilla = columnas × coste_columna + filas × coste_fila
```

- La geometría es la que la ciudad ya guardaba: fila y banda de columnas por
  edificio en `ParcelPlacement`.
- Cruzar de calle cuesta más que avanzar por una frontada: es un movimiento en
  profundidad y no un paso a lo largo.
- **La velocidad es la del ciudadano.** `MovementSpeed` ya existía como stat
  derivado de `Reach` y sólo lo leía el combate; alguien construido para
  moverse ahora también cruza antes su propia ciudad.
- Hay suelo y techo: ningún trayecto es instantáneo y ninguno deja a nadie
  varado.
- **Un extremo sin colocar conserva la duración plana antigua.** Medir nada y
  llamarlo "aquí al lado" sería mentira mayor que la constante.

La duración es una **derivación cacheada, no un dato durable**: el save guarda
que alguien está en tránsito y cuándo salió, y el mundo la recalcula al cargar
desde la misma geometría. Por eso una partida recargada a mitad de camino llega
en el mismo tick que la sesión que la escribió.

El dominio tiene coordenadas pero **no ruteo** — `StreetRoutePlanner` es
presentación —, así que esto mide distancia cardinal de rejilla. El rodeo
alrededor de un obstáculo lo absorbe la presentación dentro de la ventana que
recibe. Ver [#58](https://github.com/3FE3LE/world-of-goses/issues/58).

## Cámara-sigue

En el mundo macro y en las escenas detalladas caminables, seleccionar un
ciudadano (para ver su info o delegarlo a una zona/asignación) **no** activa
la cámara por sí solo. La cámara libre (pan/zoom) sigue disponible siempre,
con o sin selección. Seguir con la cámara al ciudadano seleccionado es un modo
aparte que el jugador activa/desactiva explícitamente (toggle).

Es una función de observación/UI, no un modo de control directo de
movimiento: el ciudadano conserva su propia IA/agenda y sigue moviéndose por
delegación aunque la cámara lo esté siguiendo. Aplica igual a cualquier
`Citizen`, no solo a héroes; no crea una subclase controlable ni separa
héroes de habitantes.

## Cinco capas de competencia

El desarrollo profesional es la interacción de cinco capas:

```text
lineage
+ aptitudes personales
+ educación disponible
+ experiencia práctica
+ condición actual
```

### Linaje

Una predisposición parcialmente compartida a nivel corporal, cognitivo, ambiental y cultural. Es más fuerte al inicio y debe perder peso relativo a medida que se acumulan experiencia y oportunidad.

### Aptitudes personales

Variación individual que puede coincidir o contradecir las tendencias del linaje. La aptitud no es destino ni un multiplicador de producción.

### Educación

Mentores, escuelas, documentación, instituciones y acceso a la enseñanza.

### Experiencia

Trabajo efectivamente realizado, problemas resueltos y eventos sobrevividos. La experiencia es la fuente principal de la competencia final.

### Condición actual

Salud, herramientas, descanso, ambiente, motivación, seguridad y otras circunstancias que cambian de un día a otro.

## Doce familias profesionales

Una profesión concreta puede pertenecer a más de una familia. La lista es un vocabulario organizador, no un sistema de clases.

1. **Extracción:** minería, cantería, tala, recolección, excavación, prospección.
2. **Construcción e infraestructura:** albañilería, carpintería estructural, caminos, puentes, hidráulica, fortificación, mantenimiento.
3. **Agricultura y sistemas vivos:** cultivo, ganadería, silvicultura, cría, manejo de suelo, tratamiento de agua.
4. **Medicina y cuidados:** primeros auxilios, cirugía, farmacología, rehabilitación, salud mental, cuidado comunitario.
5. **Ingeniería y manufactura:** mecánica, herrería, automatización, alquimia industrial, demolición, fabricación, reparación.
6. **Exploración y supervivencia:** cartografía, navegación, rastreo, caza, campamentos, reconocimiento, supervivencia.
7. **Logística:** transporte, almacenamiento, inventarios, distribución, preparación de expediciones, suministros.
8. **Comercio y administración:** contabilidad, negociación, valoración, seguros, gestión pública, contratos, planificación económica.
9. **Investigación y educación:** investigación, diagnóstico, estadística, historia, astronomía, enseñanza, diseño institucional.
10. **Relaciones sociales:** diplomacia, mediación, psicología, actuación, traducción, inteligencia, comunicación.
11. **Seguridad y combate:** defensa, vigilancia, táctica, escolta, combate, rescate, manejo de amenazas.
12. **Artes y cultura:** música, narrativa, diseño, ceremonias, artes visuales, preservación cultural.

## Reglas de datos y balance

Un linaje puede exponer eventualmente dimensiones cualitativas como afinidad de aprendizaje, afinidad de retención, afinidad de enseñanza, adaptación ambiental y disponibilidad cultural. Esas dimensiones no son asignaciones de profesión almacenadas en el linaje mismo.

Una definición de linaje **no debe**:

- Bloquear una profesión.
- Establecer un techo permanente de nivel.
- Garantizar competencia.
- Reemplazar la experiencia real.
- Convertir una afinidad en un bono automático de producción.

Los ciudadanos sin afinidad de linaje común pueden superar completamente a quienes sí la tienen mediante aptitud individual, experiencia, educación, mentoría, herramientas, salud, motivación, oportunidad e instituciones de alta calidad.

Los primeros efectos mecánicos deben ser pequeños y causales. Pueden influir en aprendizaje temprano, contexto de error, fatiga, retención o transferencia entre habilidades relacionadas. No deben convertir al linaje en el mayor determinante del desempeño final. No introducir valores como `Minería +15 %` o `Medicina -5 %` antes de que existan el sistema de habilidades y sus experimentos.

La alineación ambiental de la ciudad sigue siendo un sistema emergente separado. Proviene de políticas y acciones acumuladas, no de seleccionar una facción permanente o afinidad elemental durante la creación del personaje.
