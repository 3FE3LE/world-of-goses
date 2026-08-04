# Sistema de estadísticas — El Cubo Kovari

## Corrección geométrica

Un cubo posee:

- **8 vértices**;
- **12 aristas**;
- **6 caras**;
- **3 ejes dimensionales**.

Los ocho linajes se representan mediante los ocho vértices, no mediante ocho aristas.

El modelo se atribuye culturalmente a los Kovari porque lo construyeron, conservaron o popularizaron como mecanismo. Eso no significa que los Kovari sean propietarios de las fuerzas que describe.

## Propósito de diseño

El Cubo debe cumplir cuatro funciones:

1. Dar una estructura coherente a los ocho linajes.
2. Diferenciar afinidades sin convertirlas en clases.
3. Proporcionar bonificaciones pequeñas y legibles.
4. Servir como lenguaje compartido entre ciudad, expediciones y desarrollo individual.

El linaje otorga una predisposición inicial. La experiencia, educación, equipo, salud, instituciones y decisiones deben pesar más a medio y largo plazo.

---

# Los tres ejes

## Eje I — Sustancia ↔ Relación

Pregunta:

> ¿La acción se apoya primero en el cuerpo y la materia, o en los vínculos entre personas y sistemas?

### Sustancia

Nombre de stat propuesto: **Cuerpo**

Representa:

- capacidad material;
- esfuerzo físico;
- contacto con herramientas;
- tolerancia ambiental;
- lectura de recursos;
- ejecución práctica.

Efectos de ciudad:

- construcción y reparación física;
- extracción;
- transporte de carga;
- tolerancia a condiciones laborales;
- producción basada en herramientas.

Efectos de expedición:

- salud base;
- capacidad de carga;
- resistencia física;
- potencia de acciones corporales;
- manejo de obstáculos.

### Relación

Nombre de stat propuesto: **Vínculo**

Representa:

- coordinación social;
- confianza;
- lectura interpersonal;
- influencia;
- transferencia de conocimiento;
- interacción entre sistemas.

Efectos de ciudad:

- integración migratoria;
- diplomacia;
- cohesión;
- enseñanza;
- negociación;
- liderazgo de equipos.

Efectos de expedición:

- moral;
- eficacia de apoyo;
- coordinación;
- protección de aliados;
- resolución de eventos sociales.

---

## Eje II — Contención ↔ Proyección

Pregunta:

> ¿La acción busca conservar y estabilizar, o intervenir y provocar un cambio?

### Contención

Nombre de stat propuesto: **Estabilidad**

Representa:

- conservación;
- resistencia;
- recuperación;
- prevención;
- retención;
- control del riesgo.

Efectos de ciudad:

- mantenimiento;
- reducción de deterioro;
- preservación de recursos;
- recuperación médica;
- seguridad;
- continuidad durante crisis.

Efectos de expedición:

- defensa;
- resistencia a estados;
- recuperación;
- protección;
- retirada ordenada;
- reducción de pérdidas.

### Proyección

Nombre de stat propuesto: **Impulso**

Representa:

- iniciativa;
- intervención;
- velocidad de respuesta;
- experimentación;
- expansión;
- capacidad de alterar un sistema.

Efectos de ciudad:

- innovación;
- respuesta a emergencias;
- velocidad de reparación activa;
- exploración;
- adaptación tecnológica;
- puesta en marcha de proyectos.

Efectos de expedición:

- iniciativa;
- frecuencia de acciones;
- potencia de habilidades activas;
- ruptura de obstáculos;
- persecución o avance;
- reacción ante eventos.

---

## Eje III — Concentración ↔ Distribución

Pregunta:

> ¿La capacidad se reúne en un foco especializado o se extiende a través de una red?

### Concentración

Nombre de stat propuesto: **Dominio**

Representa:

- especialización;
- precisión;
- profundidad;
- control local;
- rendimiento de un individuo o punto crítico.

Efectos de ciudad:

- producción de especialistas;
- calidad de una instalación;
- eficiencia de un edificio concreto;
- formación avanzada;
- resolución de cuellos de botella.

Efectos de expedición:

- precisión;
- efecto sobre un objetivo;
- probabilidad crítica;
- dominio de una herramienta;
- ejecución de roles especializados.

### Distribución

Nombre de stat propuesto: **Alcance**

Representa:

- logística;
- difusión;
- redes;
- efectos de área;
- transferencia;
- coordinación entre varios puntos.

Efectos de ciudad:

- transporte;
- cobertura de servicios;
- difusión tecnológica;
- coordinación entre distritos;
- cadenas de suministro;
- educación general.

Efectos de expedición:

- habilidades de área;
- bonificaciones de formación;
- suministro;
- comunicación;
- exploración;
- efectos compartidos.

---

# Los ocho vértices

| Linaje | Eje I | Eje II | Eje III | Lectura |
|---|---|---|---|---|
| Ardhen | Sustancia | Contención | Concentración | materia estable reunida en un punto de carga |
| Eirune | Sustancia | Contención | Distribución | vida preservada mediante redes |
| Kovari | Sustancia | Proyección | Concentración | intervención técnica precisa |
| Vaelun | Sustancia | Proyección | Distribución | movimiento material a través de rutas |
| Orveth | Relación | Contención | Concentración | confianza y valor custodiados en acuerdos |
| Myrven | Relación | Contención | Distribución | identidad sostenida por contextos y representaciones |
| Theryn | Relación | Proyección | Concentración | intensidad colectiva enfocada |
| Caelith | Relación | Proyección | Distribución | conocimiento conectado y aplicado en redes |

---

# Bonificación inicial recomendada

## Regla simple para prototipo

Cada linaje selecciona tres polos, uno por eje.

Cada polo otorga:

```text
+5 puntos de afinidad
```

o, si se implementa porcentualmente:

```text
+5 % a los cálculos derivados relevantes
```

No aplicar penalización al polo contrario.

Ejemplo:

```text
Ardhen
+5 Cuerpo
+5 Estabilidad
+5 Dominio
```

```text
Caelith
+5 Vínculo
+5 Impulso
+5 Alcance
```

El valor exacto debe ajustarse mediante pruebas. La recomendación es mantener el efecto inicial entre 3 % y 7 %.

Un bono mayor corre el riesgo de convertir el linaje en clase.

## Peso relativo

Propuesta de peso en una acción madura:

| Fuente | Peso orientativo |
|---|---:|
| Linaje | 5–10 % |
| Rasgos personales | 5–15 % |
| Educación | 10–25 % |
| Experiencia | 20–40 % |
| Herramientas y equipo | 10–30 % |
| Salud y contexto | variable |
| Instituciones y políticas | variable |

La afinidad de linaje facilita el comienzo. No decide el resultado final.

---

# Firmas de combinación

Además de los tres polos, cada linaje recibe una firma pequeña que expresa la interacción específica de su vértice.

| Linaje | Firma | Propuesta |
|---|---|---|
| Ardhen | Anclaje | al mantener una tarea o posición, acumula resistencia y eficiencia local hasta un límite |
| Eirune | Corola | una recuperación o prevención individual transmite una fracción a la red cercana |
| Kovari | Reconfiguración | cambiar herramienta, rol técnico o configuración tiene menor coste y conserva parte del progreso |
| Vaelun | Rumbo | rutas exploradas mejoran retirada, transporte y descubrimiento para todo el grupo |
| Orveth | Custodia | reservas y objetos asignados a un propósito sufren menos pérdida y producen mayor confianza contractual |
| Myrven | Adaptación | reasignar rol social o profesional conserva mejor experiencia transferible |
| Theryn | Resonancia | acciones coordinadas sobre un mismo objetivo aumentan moral y efecto colectivo |
| Caelith | Síntesis | información procedente de fuentes distintas produce bonificaciones de predicción y planificación |

Las firmas no deben activarse únicamente por pertenecer al linaje. Requieren una condición jugable coherente.

---

# Aplicación a ciudadanos

## Datos mínimos

```csharp
public enum LineageAxisPole
{
    Substance,
    Relation,
    Containment,
    Projection,
    Concentration,
    Distribution
}

public sealed record LineageCoordinates(
    LineageAxisPole Nature,
    LineageAxisPole Expression,
    LineageAxisPole Reach
);
```

Los nombres técnicos pueden mantenerse neutrales aunque la UI utilice:

```text
Cuerpo / Vínculo
Estabilidad / Impulso
Dominio / Alcance
```

## Cálculo orientativo

```text
resultado =
base
× experiencia
× salud
× equipo
× contexto
× (1 + modificador_de_linaje)
```

Evitar sumar el bono de linaje repetidamente en cada capa.

## Aprendizaje

Los polos también pueden modificar:

- velocidad de aprendizaje inicial;
- retención;
- transferencia entre competencias;
- errores de principiante;
- adaptación a herramientas;
- prestigio cultural.

Ejemplos:

- Dominio acelera especializaciones profundas.
- Alcance mejora transferencia y enseñanza.
- Estabilidad reduce pérdida de competencia tras enfermedad o pausa.
- Impulso acelera adaptación a una tarea nueva.
- Cuerpo facilita competencias materiales.
- Vínculo facilita competencias sociales y coordinadas.

---

# Aplicación a combate y expediciones

## Estadísticas derivadas

| Stat | Derivaciones posibles |
|---|---|
| Cuerpo | HP, carga, resistencia física, potencia corporal |
| Vínculo | moral, apoyo, coordinación, negociación |
| Estabilidad | defensa, recuperación, resistencia a estados |
| Impulso | iniciativa, velocidad de activación, interrupción |
| Dominio | precisión, crítico, efecto de objetivo único |
| Alcance | área, formación, suministros, exploración |

No todas deben existir como números visibles. Algunas pueden actuar detrás de sistemas más concretos.

## Ejemplo de grupo

Un equipo con:

- Ardhen: mantiene la línea y transporta carga.
- Eirune: extiende recuperación.
- Kovari: desactiva o reconfigura obstáculos.
- Myrven: resuelve eventos sociales y cambia roles.
- Vaelun: mejora ruta y retirada.
- Orveth: conserva suministros y negocia.
- Caelith: predice riesgos.
- Theryn: coordina picos de acción.

Esto no asigna profesiones obligatorias. Describe ventajas relativas cuando las competencias y oportunidades acompañan.

---

# Aplicación a ciudad

## Evitar un bono eterno del fundador

El fundador no debe dar un multiplicador global permanente solo por existir.

La influencia cultural puede calcularse mediante:

```text
peso cultural =
población participante
× prestigio institucional
× presencia política
× antigüedad
× adopción voluntaria
```

El linaje fundador facilita las primeras instituciones, pero la composición de la ciudad cambia con migración, educación, políticas y generaciones.

## Bonificaciones poblacionales

Una institución puede aprovechar la distribución de polos entre trabajadores.

Ejemplo:

```text
Hospital
Estabilidad → continuidad de tratamientos
Alcance → cobertura de pacientes
Dominio → procedimientos complejos
Vínculo → adherencia y confianza
Cuerpo → intervención física
Impulso → respuesta de emergencia
```

El mejor hospital no pertenece automáticamente a Eirune. Depende de qué problema intenta resolver.

## Tensión política

Adoptar una institución asociada a un linaje debe modificar:

- relaciones diplomáticas;
- migración;
- prestigio;
- oposición interna;
- costes;
- doctrina urbana;
- acceso a conocimiento.

No debe limitarse a un bono productivo.

---

# Aplicación al onboarding

Las respuestas del onboarding generan dos resultados separados:

## 1. Compatibilidad corporal

Determina el vértice del Cubo más compatible.

No necesita mostrar los tres ejes durante las preguntas.

## 2. Perfil personal

Determina valores que pueden contradecir el linaje:

- rasgos;
- aptitudes;
- postura política;
- tolerancia al riesgo;
- estilo de liderazgo;
- afinidades profesionales;
- preferencias de combate.

Ejemplo:

```text
Cuerpo Ardhen
+
perfil personal orientado a Vínculo, Impulso y Alcance
=
fundador corporalmente Ardhen con decisiones cercanas a Caelith
```

La contradicción es deseable. Produce narrativa y evita esencialismo.

---

# Relación con inmortalidad y descendencia

El Cubo describe predisposición, no alma ni capacidad reproductiva.

La ruta de inmortalidad debe ser un sistema independiente.

Posible interacción:

- la permanencia congela parcialmente el desarrollo corporal;
- el fundador conserva sus coordenadas iniciales;
- pierde reproducción biológica;
- las generaciones siguientes continúan combinando cultura, educación y linaje sin heredar obligatoriamente su doctrina.

La inmortalidad no debe convertir un vértice en superior a los demás.

---

# Guardarraíles

- No aplicar penalizaciones raciales.
- No bloquear armas, profesiones o instituciones.
- No mostrar el Cubo como una prueba de superioridad Kovari.
- No permitir que tres bonificaciones sustituyan personalidad y experiencia.
- No hacer que una ciudad de mayoría de un linaje sea automáticamente mejor en su afinidad.
- No convertir el polo Relación en “magia social” ni Sustancia en “fuerza bruta”.
- No confundir Contención con pasividad.
- No confundir Proyección con violencia.
- No confundir Concentración con egoísmo.
- No confundir Distribución con colectivismo moral.
- No usar el modelo para justificar segregación dentro del lore sin mostrar sus consecuencias.

---

# Decisiones pendientes

1. Nombres finales de los seis stats visibles.
2. Magnitud exacta de los bonos.
3. Si las firmas pertenecen a individuos, instituciones o ambos.
4. Cómo se representa el Cubo en UI sin revelar el cálculo completo durante el onboarding.
5. Qué rasgos personales pueden modificar temporalmente un polo.
6. Cómo heredan los hijos la forma corporal cuando sus progenitores tienen linajes distintos.
7. Qué tecnologías permiten medir o alterar las coordenadas.
8. Qué parte del modelo Kovari es verdadera y qué parte fue añadida políticamente.
