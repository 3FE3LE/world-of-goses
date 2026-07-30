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
