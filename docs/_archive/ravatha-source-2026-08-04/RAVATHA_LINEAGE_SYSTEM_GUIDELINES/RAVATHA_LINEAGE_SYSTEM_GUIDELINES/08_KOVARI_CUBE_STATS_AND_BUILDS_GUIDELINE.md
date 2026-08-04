# Kovari — El Cubo aplicado a atributos, estadísticas y builds

## Estado

Guideline funcional para integrar la tradición Kovari en el perfil de ciudadanos, el onboarding, las estadísticas explícitas, el combate automático y la relación con equipamiento de **World of Goses**.

---

# 1. Objetivo

El Cubo debe proporcionar el lenguaje con el que Ravatha representa:

- predisposiciones corporales;
- capacidades continuas;
- relación entre atributos;
- estadísticas derivadas;
- afinidad elemental;
- builds;
- entrenamiento;
- uso de herramientas;
- desgaste;
- especialización sin clases permanentes.

El Cubo no convierte a los Kovari en los únicos capaces de comprender estadísticas. Es el modelo cultural más desarrollado para describirlas.

> El ciudadano produce la capacidad. El equipo define cómo esa capacidad puede expresarse, cuánto exige y cuánto tiempo resiste.

---

# 2. Geometría

El Cubo contiene:

- 8 vértices;
- 12 aristas;
- 6 caras;
- 3 ejes.

Los ocho linajes ocupan los ocho vértices.

Las seis afinidades elementales pueden representarse mediante las seis caras.

Los tres ejes describen predisposiciones continuas.

---

# 3. Los tres ejes

Cada pareja suma `100` en el perfil inicial recomendado.

## Cuerpo ↔ Vínculo

### Cuerpo

- fuerza aplicada;
- vida;
- carga;
- esfuerzo físico;
- manejo de peso;
- contacto material.

### Vínculo

- canalización elemental;
- transmisión;
- curación;
- escudos;
- efectos compartidos;
- resonancia.

## Estabilidad ↔ Impulso

### Estabilidad

- continuidad;
- recuperación;
- fatiga;
- resistencia;
- regeneración;
- control bajo presión.

### Impulso

- velocidad de ataque;
- velocidad de lanzamiento;
- iniciativa;
- reacción;
- frecuencia de habilidades;
- aceleración.

## Dominio ↔ Alcance

### Dominio

- precisión;
- técnica;
- crítico;
- penetración;
- concentración;
- manejo eficiente.

### Alcance

- distancia;
- área;
- propagación;
- cobertura;
- objetivos múltiples;
- coordinación espacial.

---

# 4. Los ocho vértices

| Linaje | Eje I | Eje II | Eje III |
|---|---|---|---|
| Ardhen | Cuerpo | Estabilidad | Dominio |
| Eirune | Cuerpo | Estabilidad | Alcance |
| Kovari | Cuerpo | Impulso | Dominio |
| Vaelun | Cuerpo | Impulso | Alcance |
| Orveth | Vínculo | Estabilidad | Dominio |
| Myrven | Vínculo | Estabilidad | Alcance |
| Theryn | Vínculo | Impulso | Dominio |
| Caelith | Vínculo | Impulso | Alcance |

El vértice determina la configuración corporal del linaje, no una clase.

Un ciudadano puede cruzar el centro de cualquier eje mediante experiencia sin cambiar de linaje.

---

# 5. Integración con onboarding

El onboarding debe producir:

```text
Linaje
Afinidad elemental
Perfil inicial del Cubo
Memorias narrativas
```

No debe producir:

- arma preferida;
- profesión;
- clase;
- estilo de combate permanente;
- rasgos mecánicos sin uso;
- orientación política definitiva.

## Conservación de predictibilidad

El scoring actual de linaje debe permanecer como fuente de verdad durante el primer refactor.

Calcular en paralelo:

```text
Scoring actual de linaje
Scoring elemental
Scoring de ejes del Cubo
```

El Cubo comienza en modo sombra.

Solo podrá reemplazar el selector actual si pruebas doradas demuestran que conserva resultados conocidos, empates y distribución.

---

# 6. Atributos y estadísticas derivadas

El Cubo no sustituye las estadísticas explícitas.

Ejemplos de estadísticas derivadas:

## Ofensivas

- daño físico;
- daño por elemento;
- velocidad de ataque;
- velocidad de lanzamiento;
- reducción de enfriamiento;
- crítico;
- daño crítico;
- precisión;
- penetración.

## Defensivas

- vida;
- defensa física;
- resistencia elemental;
- reducción de daño;
- regeneración;
- esquiva física;
- esquiva elemental;
- resistencia a interrupción;
- resistencia a estados.

## Utilidad

- potencia de curación;
- potencia de escudos;
- duración de efectos;
- alcance;
- área;
- propagación;
- recuperación de fatiga.

Toda estadística importante debe permitir un desglose de fuentes.

Ejemplo:

```text
Velocidad de ataque: 1,18/s

Impulso                 +0,22
Dominio con dagas       +0,11
Peso del equipo         -0,07
Fatiga actual           -0,04
Condición del filo      -0,03
Postura                 +0,09
```

---

# 7. El ciudadano como fuente de poder

El equipamiento no otorga ataque base ni velocidad estándar.

No utilizar:

```text
Espada común: +20 ataque
Espada legendaria: +200 ataque
```

Utilizar:

```text
Capacidad del ciudadano
× eficiencia de manejo
× condición del equipo
× afinidad
= rendimiento efectivo
```

El ciudadano aporta:

- fuerza;
- técnica;
- velocidad;
- aguante;
- afinidad;
- experiencia;
- competencia.

El arma define:

- cómo canaliza esas capacidades;
- qué atributos aprovecha;
- cuánto esfuerzo exige;
- qué alcance permite;
- cómo se degrada;
- qué tipo de acción ejecuta.

---

# 8. Propiedades del equipamiento

## Peso

Masa transportada, acelerada y detenida.

Afecta:

- fatiga;
- velocidad;
- capacidad de carga;
- recuperación;
- estabilidad.

## Exigencia

Esfuerzo técnico y físico necesario para utilizar correctamente la pieza.

Un arco puede ser ligero y muy exigente.

## Integridad máxima

Cantidad de deterioro acumulado que soporta antes de quedar inutilizable.

## Condición actual

Porcentaje de funcionamiento conservado.

## Resonancia elemental

Eficiencia con la que transmite cada afinidad.

## Tolerancia elemental

Carga elemental que soporta antes de degradarse aceleradamente.

## Perfil de desgaste

Describe qué partes o propiedades pierden rendimiento.

---

# 9. Fatiga y desgaste

Separar:

## Fatiga del ciudadano

Proviene de:

- peso;
- exigencia;
- frecuencia;
- postura;
- duración;
- heridas;
- falta de entrenamiento.

Reduce:

- velocidad;
- precisión;
- fuerza efectiva;
- bloqueo;
- lanzamiento;
- resistencia a interrupción.

## Desgaste del equipo

Proviene de:

- impactos;
- bloqueos;
- material golpeado;
- uso;
- afinidad;
- técnica deficiente;
- ambiente;
- calidad.

Reduce rendimiento según el perfil de la pieza.

---

# 10. Familias iniciales de armas

## Pesada a dos manos

- Cuerpo y Estabilidad;
- alta exigencia;
- impacto;
- interrupción;
- cadencia baja.

## Una mano equilibrada

- Dominio y Cuerpo;
- flexibilidad;
- cadencia media;
- defensa parcial.

## Armas dobles

- Impulso y Dominio;
- frecuencia alta;
- desgaste independiente;
- fatiga sostenida.

## Dagas

- Dominio e Impulso;
- alcance corto;
- dependencia del filo;
- precisión y frecuencia.

## Lanza o asta

- Alcance y Dominio;
- control espacial;
- punta y asta degradables;
- segunda línea.

## Arco

- Dominio, Estabilidad, Cuerpo y Alcance;
- bajo peso;
- alta exigencia;
- cuerda, palas y munición.

## Arma y escudo

- Estabilidad y Cuerpo;
- dos piezas independientes;
- bloqueo;
- peso combinado.

## Lanza y escudo

- Estabilidad, Cuerpo y Alcance;
- formación;
- alta exigencia;
- control frontal.

Estas familias no son clases y pueden ampliarse.

---

# 11. Afinidad elemental

Afinidades:

```text
Tierra
Agua
Fuego
Aire
Éter
Neutra o Silencio
```

La afinidad pertenece al ciudadano.

El equipo responde mediante Resonancia y Tolerancia.

```text
Potencia elemental personal
× resonancia
× condición
× control técnico
= efecto elemental
```

El equipo no contiene el poder elemental como una batería independiente.

## Riesgo material por afinidad

- Fuego: temperatura y deformación;
- Agua: corrosión, humedad y pérdida de tensión;
- Tierra: presión, vibración y fractura;
- Aire: torsión, vibración y desalineación;
- Éter: interferencia e inestabilidad;
- Neutra: desgaste físico sin carga elemental.

---

# 12. Rasgos y competencias

Los rasgos mecánicos deben adquirirse durante la vida del ciudadano.

Deben incluir:

- condición de activación;
- efecto explícito;
- origen;
- posibilidad de evolución o pérdida cuando corresponda.

Ejemplo:

```text
Temerario
+10 % velocidad de ataque
-6 % reducción de daño
Origen: sobrevivió a tres retiradas fallidas
```

Las competencias provienen de:

- entrenamiento;
- experiencia;
- mentores;
- instituciones;
- práctica;
- heridas;
- decisiones.

El onboarding no puede afirmar preferencia por armas que el fundador jamás ha utilizado.

---

# 13. Combate automático

La build puede contener:

```text
Ciudadano
Equipo
Afinidad
Habilidades
Postura
Posición
Prioridades automáticas
Condición de retirada
```

Las posturas deben modificar estadísticas y prioridades explícitas.

Ejemplo:

```text
Agresiva
+15 % velocidad de ataque
+10 % daño efectivo
-8 % reducción de daño
Prioridad: objetivo con menor vida
```

No utilizar descripciones tácticas imposibles de observar o medir.

---

# 14. Instituciones Kovari

## Talleres de apertura

- reparación;
- enseñanza;
- inspección de herramientas.

## Casas de recombinación

- reciclaje;
- prótesis;
- módulos;
- adaptación de equipo.

## Consejos de seguridad técnica

- límites;
- riesgo;
- pruebas;
- demolición.

## Escuelas del Cubo

- lectura de atributos;
- builds;
- entrenamiento;
- análisis de rendimiento.

---

# 15. Relación con otros sistemas

## Vaelun

Las builds deben sobrevivir duración, carga y regreso.

## Eirune

Salud, clima y ambiente afectan rendimiento y desgaste.

## Ardhen

Estructuras y carga física limitan herramientas y talleres.

## Orveth

Equipo, materiales, reservas y procedencia deben administrarse.

## Caelith

Diagnostica builds, fórmulas, fallos y evidencia.

## Myrven

La sociedad interpreta capacidades mediante profesiones, prestigio y reconocimiento.

## Theryn

Las velocidades, enfriamientos y acciones forman ritmo musical perceptible.

---

# 16. Interfaz recomendada

La ficha del ciudadano debe permitir:

- Cubo;
- estadísticas derivadas;
- equipo;
- competencias;
- rasgos adquiridos;
- afinidad;
- fatiga;
- condición;
- desglose de fórmulas.

La vista simplificada puede resumir, pero nunca reemplazar o esconder la hoja completa.

El jugador casual puede utilizar recomendaciones. El jugador experto debe poder inspeccionar cada número importante.

---

# 17. Límites de diseño

No implementar el Cubo como:

- seis estadísticas vagas que oculten todo;
- clase racial;
- destino permanente;
- preferencia de armas del onboarding;
- equipo con ataque mágico independiente del usuario;
- loot con números inflados;
- fórmulas imposibles de inspeccionar;
- modelo que sustituya la experiencia y la vida del ciudadano.

---

# 18. Fases de implementación

## Fase 1

- Cubo en modo sombra;
- preservación de linaje;
- afinidad;
- estadísticas derivadas básicas.

## Fase 2

- peso;
- exigencia;
- fatiga;
- condición;
- familias de armas.

## Fase 3

- resonancia;
- tolerancia;
- desgaste por partes;
- posturas y prioridades.

## Fase 4

- rasgos adquiridos;
- entrenamiento institucional;
- builds avanzadas;
- análisis de combate y música reactiva.

---

# 19. Criterios de aceptación

1. el scoring actual de linaje se conserva hasta demostrar equivalencia;
2. el onboarding produce Cubo y afinidad, no arma o profesión;
3. las estadísticas derivadas son explícitas;
4. cada estadística importante tiene desglose;
5. el ciudadano es la fuente de poder;
6. el arma canaliza, exige y se degrada;
7. peso y exigencia son propiedades diferentes;
8. fatiga y desgaste son sistemas separados;
9. la afinidad pertenece al ciudadano;
10. cualquier linaje puede desarrollar cualquier build;
11. los rasgos nacen de experiencia real;
12. el sistema soporta combate automático profundo sin ocultar sus números.
