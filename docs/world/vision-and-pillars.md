# Visión y pilares

> Qué es World of Goses y qué restricciones vinculan cualquier sistema que se
> le añada. Cuando un sistema concreto contradice algo de aquí, se rediseña el
> sistema, no el principio.


## Fantasía central

**World of Goses** es un juego 2D pixel art de gestión de una civilización persistente y expediciones automatizadas.

El jugador construye una única ciudad cuya identidad emerge de:

- Linaje fundador.
- Héroe fundador.
- Decisiones del onboarding.
- Profesiones e instituciones.
- Composición poblacional.
- Territorio explorado.
- Relación con el ambiente.
- Crisis, victorias y pérdidas.
- Culturas y conocimientos incorporados.

La ciudad es la protagonista de largo plazo.

## Historia emergente

No se plantea una campaña lineal como columna vertebral. La historia surge de:

- Quién fundó la ciudad.
- Qué profesiones recibieron prestigio.
- Qué regiones fueron preservadas o explotadas.
- Qué habitantes se convirtieron en héroes.
- Qué expediciones fracasaron.
- Qué enfermedades o desastres dejaron cicatrices.
- Qué instituciones nacieron como respuesta.
- Cómo envejeció la ciudad.

El mundo necesita lore, culturas y memoria, aunque no necesite un villano final obligatorio.

## Una sola ciudad

- Una partida representa una ciudad.
- No hay prestigio que recompense destruirla y reiniciar.
- Reiniciar exige eliminar la ciudad o crear otra partida.
- El conocimiento del jugador es la progresión transferible.
- La ciudad debe poder adquirir una historia larga y difícil de reemplazar.

## Persistencia

La ciudad continúa operando cuando el juego está cerrado.

La ausencia no aplica una penalización artificial. Durante ella:

- Se ejecutan políticas configuradas.
- Continúan órdenes de producción.
- Se consumen y generan recursos.
- Se procesan recuperaciones y tiempos de espera.
- Pueden expirar oportunidades si no existe una institución capaz de conservarlas.

No se debe simular cada segundo perdido. El progreso offline se resuelve mediante eventos, ciclos y cálculos discretos.

## Tono sistémico

El juego permite sociedades regenerativas, extractivas, agrícolas, industriales, mercantiles, académicas, médicas, militarizadas, comunitarias o autoritarias.

El sistema no moraliza automáticamente una estrategia. Cada modelo obtiene ventajas, costes, dependencias y consecuencias.

## Inspiración y originalidad

Existe una inspiración estructural visible en la diversidad sistémica de juegos como *Wakfu*, especialmente en la unión entre cultura, estética y gameplay.

No se reproducen nombres, personajes, dioses, símbolos, siluetas, hechizos, animaciones, lore, naciones, terminología ni interfaces.

## Principios de diseño

Estos principios son restricciones sobre decisiones futuras, no aspiraciones. Un sistema que viole uno de ellos debe rediseñarse, no aprobarse.

1. **Una ciudad. Una historia.** Sin meta-progresión entre ciudades. Sin bonificaciones por reiniciar.
2. **Sin penalizaciones artificiales por ausencia.** El mundo continúa. No castiga al jugador por estar lejos.
3. **Sin decisiones soberanas sin autorización.** El mundo solo ejecuta lo que el jugador autorizó, configuró o delegó.
4. **Sin un nivel global único.** El desarrollo es multidimensional.
5. **Sin desbloqueos arbitrarios.** Los edificios requieren condiciones reales.
6. **Sin botín aleatorio.** El equipo se produce, no se encuentra.
7. **Sin muerte invisible.** La muerte tiene causas explicables.
8. **Sin curación instantánea.** Las heridas requieren tratamiento.
9. **Sin eficiencia mágica.** Los cambios tienen causas internas.
10. **Sin un único modelo correcto de desarrollo.** Agrícola, académico, mercantil, industrial, nómada, militar, rapaz o combinaciones emergentes son caminos válidos.
11. **Causalidad sobre aleatoriedad.** Cada consecuencia rastrea una cadena real de eventos.
12. **Composición sobre herencia.** El código se estructura por partes que se combinan, no por jerarquías profundas.
13. **El dominio no es presentación.** La simulación no depende de sprites, cámaras ni animaciones.
14. **Originalidad.** Todos los nombres actuales —incluido el nombre del proyecto— son provisionales. Las inspiraciones informan el diseño. Los productos son originales.

## Disciplina de nombres

Los nombres provisionales, incluido el nombre del proyecto, existen para hacer concreto el diseño. El proceso para promover un nombre provisional a un nombre público es:

1. El nombre provisional se documenta en un documento interno.
2. El nombre se revisa contra las reglas de originalidad de la sección *Inspiración y originalidad* de este capítulo.
3. El nombre se confirma como original o se reemplaza.
4. El artefacto público se actualiza para usar el nombre confirmado.

Hasta que un nombre se confirme, trátalo como marcador interno. No presentes nombres provisionales como terminología final en artefactos públicos.

## Frontera de inspiración e IP

El proyecto reconoce inspiración estructural en la diversidad sistémica de juegos como *Wakfu*, especialmente en la unión entre cultura, estética y gameplay. Esta intención informa la amplitud del espacio de diseño, el énfasis en interacciones sistémicas, la identidad profesional de los ciudadanos y la manera en que las elecciones ambientales se propagan por el mundo. No es una licencia. El juego final debe ser una propiedad intelectual original.

Lo que puede usarse como inspiración conceptual (vocabulario interno, notas, discusiones):

- La fantasía general de un sistema de clases / arquetipos con diversidad amplia.
- La fantasía general de identidad basada en profesión.
- La fantasía general de un tema ecológico que interactúa con elecciones del jugador.
- La fantasía general de una alineación ambiental expresada a través de acciones acumuladas, no de una elección única.
- La fantasía general de jugabilidad automática de expedición con configuración en lugar de control directo.
- La fantasía general de un asentamiento persistente único.

Lo que debe permanecer original en cualquier forma, en cualquier artefacto (código, arte, lore, UI, documentación, naming, marketing u otro):

- Nombres de clases o razas existentes.
- Siluetas de personajes.
- Vestuario.
- Símbolos de clase.
- Nombres de hechizos.
- Kits de hechizos exactos.
- Lore.
- Religiones.
- Naciones.
- Ubicaciones.
- Diseños de interfaz.
- Obras de arte.
- Animaciones.
- Música.
- Diálogos.
- Sistemas numéricos exactos.

Los documentos internos pueden referenciar las inspiraciones originales para comunicar intención de diseño. Los nombres, arte, lore e implementaciones públicos deben ser creados independientemente.

---

# Pilares de gameplay

### 1. Desarrollo de ciudad

El jugador administra territorio, edificios, población, profesiones, producción, almacenamiento, transporte, salud, educación, políticas, instituciones, riesgos y cultura.

La ciudad no sube mediante un único nivel global.

### 2. Expediciones automatizadas

El jugador prepara equipos, pero no controla movimiento manual.

Configura integrantes, formación, roles, equipo, suministros, habilidades automáticas, prioridades, retirada y objetivo.

### 3. Habitantes con trayectoria

Los ciudadanos:

- No tienen profesión permanente.
- Pueden desarrollar cualquier competencia.
- Acumulan historia.
- Cambian de oficio.
- Reciben rangos y reconocimientos.
- Pueden convertirse en héroes por oportunidad y experiencia.

### 4. Producción causal

Un edificio no produce por existir.

La producción depende de:

- Recurso accesible.
- Trabajadores.
- Competencia.
- Herramientas.
- Materiales.
- Energía.
- Salud.
- Logística.
- Almacenamiento.
- Políticas.
- Riesgo.

Ejemplo:

```text
Stock de armaduras: mínimo 8, máximo 12
↓
Una expedición pierde 3
↓
El stock baja a 7
↓
La orden de producción se reactiva
↓
Consume metal, combustible, tiempo y trabajo
↓
Se detiene al llegar a 12 o por un cuello de botella
```

### 5. Expansión territorial

El territorio se habilita mediante exploración, expediciones, apertura de rutas, eliminación o comprensión de amenazas y reconocimiento de recursos.

Después el jugador decide preservar, extraer, cultivar, construir, comerciar, defender o investigar.

### 6. Salud y consecuencias

No existe curación instantánea general.

Una persona herida requiere camas, personal, medicamentos, tiempo y rehabilitación.

La derrota expedicionaria devuelve al habitante vivo, sin equipo y con sus lesiones actuales. La muerte puede ocurrir luego por falta de atención o colapso sistémico.

### 7. Relación ambiental

La civilización se mueve entre regeneración y extracción.

No es bueno contra malo. Ambos extremos ofrecen ventajas, costes y dependencias.

#### 7.1 Tendencias regenerativas

Acciones que pueden mover la ciudad hacia una alineación regenerativa:

- Reforestar.
- Mantener la fertilidad agrícola.
- Preservar los sistemas de agua.
- Proteger poblaciones animales.
- Restaurar territorios dañados.
- Reciclar materiales.
- Producir sin agotar tasas de renovación.
- Establecer asentamientos sostenibles.

#### 7.2 Tendencias extractivas

Acciones que pueden mover la ciudad hacia una alineación extractiva o destructiva:

- Tala excesiva.
- Agotar minas sin restauración.
- Asaltar otros asentamientos.
- Destruir infraestructura.
- Contaminar agua.
- Cazar en exceso.
- Quemar territorio.
- Priorizar extracción inmediata sobre regeneración.

#### 7.3 Ninguna alineación es automáticamente victoria o derrota

Una ciudad regenerativa puede obtener ventajas como recuperación biológica más rápida, agricultura más fiable, migración de fauna, mejor renovación de recursos a largo plazo, mejor salud pública y acceso a descubrimientos basados en la naturaleza. También puede enfrentar desventajas como extracción más lenta, mayores costes de manejo del territorio, restricciones a expansión rápida y menor producción industrial a corto plazo.

Una ciudad extractiva puede obtener ventajas como adquisición más rápida de recursos, expansión industrial fuerte a corto plazo, suministro militar eficiente, mayor capacidad de construcción rápida y acceso a tecnologías o doctrinas destructivas. También puede enfrentar consecuencias como agotamiento de recursos, inestabilidad ambiental, menor fiabilidad agrícola, patrones migratorios hostiles, problemas de salud pública y dependencia de razias o expansión territorial.

El jugador determina si el modelo resultante es sostenible a través de los sistemas que construye.

#### 7.4 Influencia sistémica de la alineación

La alineación ambiental puede influir en:

- Agricultura.
- Regeneración de recursos.
- Arquitectura.
- Materiales disponibles.
- Comportamiento de criaturas.
- Migración.
- Salud pública.
- Facciones políticas.
- Valores culturales.
- Investigación.
- Oportunidades de expedición.
- Apariencia de la ciudad.
- Efectos ambientales.
- Música y paisaje sonoro.
- Caminos institucionales disponibles.

La alineación emerge de acciones y políticas acumuladas, no de seleccionar una facción permanente al crear el personaje.

#### 7.5 Independencia de la identidad de linaje

La alineación ambiental es **independiente** de la identidad de los ocho linajes. Una ciudad Ardhen puede ser regenerativa o extractiva; una ciudad Eirune puede ser cualquiera de las dos. Las dos dimensiones se componen, no se contradicen.

### 8. Delegación

El jugador configura reglas de stock, prioridades, reservas, cuotas, seguridad y reacción ante escasez. La ciudad ejecuta esas decisiones.

### 9. Dificultad orgánica

La población no crece linealmente solo porque pasa el tiempo. A mayor escala aparecen vivienda, logística, enfermedad, educación, desempleo, desigualdad, conflicto cultural, seguridad y administración.

---

## Guardarraíles

Restricciones transversales que cualquier sistema hereda:

- No convertir la ciudad en un colony simulator tradicional.
- No separar héroes y habitantes en entidades distintas.
- No convertir linajes en clases profesionales.
- No convertir el eje ambiental en moralidad binaria.
- No convertir al fundador en un destino permanente ni en un bono eterno.
- No confundir placeholders con dirección artística final.
- No optimizar antes de medir.
- No añadir datos que no permitan una decisión ni comuniquen una consecuencia.
