# Afinidades elementales e interacciones con el mundo

## Estado

Guideline sistémico para integrar **Tierra, Agua, Fuego, Aire, Éter y Silencio** en **World of Goses**.

Este documento consolida las decisiones existentes sobre:

- afinidad elemental durante el onboarding;
- las seis caras del Cubo Kovari;
- estadísticas explícitas del ciudadano;
- resonancia y tolerancia elemental del equipamiento;
- relación ambiental entre regeneración y extracción;
- combate automático;
- ciudad, producción, salud, expediciones y territorio.

No reemplaza los guidelines Kovari, Eirune, Ardhen, Vaelun, Orveth, Caelith, Myrven o Theryn. Define el contrato común que permite a esos sistemas utilizar las afinidades sin convertirlas en seis escuelas de daño con distinto color.

---

# 1. Objetivo

Las afinidades elementales deben responder tres preguntas:

1. ¿Con qué tipo de proceso resuena naturalmente un ciudadano?
2. ¿Cómo puede expresar esa resonancia mediante su cuerpo, conocimiento, herramientas y entorno?
3. ¿Qué consecuencias materiales produce esa expresión durante el tiempo?

La afinidad debe tener importancia en:

- ciudadanía y desarrollo individual;
- fabricación y mantenimiento de equipo;
- agricultura y clima;
- arquitectura e infraestructura;
- salud y recuperación;
- producción y transformación de recursos;
- investigación;
- expediciones;
- combate automático;
- criaturas y anomalías;
- orientación regenerativa o extractiva del territorio;
- lenguaje visual y musical.

> Una afinidad no otorga una profesión, una clase ni un conjunto cerrado de habilidades. Describe la forma más natural en que una persona entra en relación con los procesos de Ravatha.

---

# 2. Principios obligatorios

## 2.1 La afinidad pertenece al ciudadano

El ciudadano es la fuente de:

- capacidad elemental;
- control;
- aprendizaje;
- intención;
- resistencia a la carga;
- experiencia.

El equipamiento no contiene el poder elemental como una batería independiente.

Una herramienta puede:

- transmitir mejor o peor una afinidad;
- soportar mayor o menor carga;
- conservar o dispersar una manifestación;
- desgastarse al canalizarla;
- limitar qué acciones son posibles.

No debe otorgar daño elemental base por el simple hecho de existir.

## 2.2 Afinidad no significa dominio

El onboarding asigna una afinidad inicial, pero no entrega automáticamente:

- técnicas avanzadas;
- control experto;
- una profesión;
- conocimiento de combate;
- capacidad para manipular grandes volúmenes;
- inmunidad al mismo elemento.

Un fundador de Fuego puede no saber encender una llama de forma segura.

Un ciudadano de Agua puede ser mal agricultor.

Un ciudadano de Silencio puede no conocer técnicas de neutralización.

El dominio se adquiere mediante:

- práctica;
- educación;
- instituciones;
- experimentación;
- herramientas;
- experiencia;
- mentores;
- consecuencias vividas.

## 2.3 Afinidad y linaje son independientes

Ningún linaje fuerza una afinidad.

Son válidos:

```text
Ardhen de Aire
Eirune de Fuego
Kovari de Agua
Myrven de Tierra
Vaelun de Silencio
Orveth de Éter
Caelith de Tierra
Theryn de Agua
```

La afinidad no cambia el linaje corporal y el linaje no bloquea manifestaciones elementales futuras.

## 2.4 Los elementos no son morales

No existe una afinidad intrínsecamente:

- buena;
- malvada;
- civilizada;
- salvaje;
- regenerativa;
- extractiva.

Cada afinidad admite usos regenerativos, extractivos, defensivos, ofensivos, productivos y destructivos.

## 2.5 Los elementos no forman un sistema rígido de debilidades

No utilizar una rueda universal del tipo:

```text
Agua vence a Fuego
Fuego vence a Tierra
Tierra vence a Aire
```

Las interacciones dependen de:

- escala;
- contexto;
- estado ambiental;
- material;
- técnica;
- intensidad;
- duración;
- preparación;
- combinación con otros procesos.

El Agua puede apagar Fuego, pero también puede convertirse en vapor, transportar calor o provocar una explosión de presión.

## 2.6 Toda consecuencia debe ser explícita

La afinidad puede ser profunda sin convertirse en niebla mística.

El jugador debe poder inspeccionar:

- qué produjo un efecto;
- quién lo produjo;
- qué estadística intervino;
- qué herramienta lo canalizó;
- cuánto desgaste generó;
- qué estado ambiental modificó;
- qué coste o riesgo apareció.

---

# 3. Nombres canónicos e identificadores

## 3.1 Afinidades canónicas

| Identificador recomendado | Nombre de UI | Principio central |
|---|---|---|
| `Earth` | Tierra | sostener, comprimir y estructurar |
| `Water` | Agua | regular, transportar y adaptar |
| `Fire` | Fuego | transformar, consumir y acelerar |
| `Air` | Aire | desplazar, propagar y liberar |
| `Aether` | Éter | conectar, transmitir y alterar relaciones |
| `Silence` | Silencio | aislar, estabilizar y neutralizar resonancia |

`Ground` no debe utilizarse como identificador técnico si el proyecto ya utiliza `Earth`.

## 3.2 Migración desde `None` o `Neutral`

Silencio reemplaza conceptualmente la idea de “sin afinidad”.

No representa ausencia de capacidad.

Representa afinidad hacia:

- aislamiento;
- amortiguación;
- precisión sin interferencia;
- separación de procesos;
- neutralización;
- estabilidad basal.

Si el código actual persiste `None` o `Neutral`, mantener compatibilidad de lectura durante la migración:

```text
None
Neutral
→ Silence
```

Los nuevos guardados deben utilizar un único identificador canónico.

---

# 4. Relación con el Cubo Kovari

Las seis afinidades corresponden a las seis caras del Cubo.

| Cara del Cubo | Afinidad |
|---|---|
| Cuerpo | Tierra |
| Vínculo | Éter |
| Estabilidad | Agua |
| Impulso | Fuego |
| Dominio | Silencio |
| Alcance | Aire |

Esta relación no significa que solo una estadística participe en cada manifestación.

La cara define el **principio primario**. Los demás atributos modifican su forma de expresión.

## Ejemplo: ciudadano de Fuego

- Impulso favorece aceleración e intensidad inicial.
- Vínculo favorece transmisión hacia otros objetivos o herramientas.
- Estabilidad permite sostener calor sin colapsar.
- Dominio mejora precisión térmica.
- Alcance permite propagar o proyectar el efecto.
- Cuerpo permite soportar esfuerzos físicos asociados.

## Ejemplo: ciudadano de Silencio

- Dominio favorece precisión, separación y neutralización.
- Estabilidad permite mantener zonas aisladas.
- Vínculo determina qué tan finamente puede cortar o regular conexiones.
- Alcance determina cobertura.
- Impulso determina rapidez de activación.
- Cuerpo permite aplicar el aislamiento sobre materiales o impactos físicos.

El Cubo explica **cómo** se expresa una afinidad. No reemplaza la afinidad ni la reduce a una sola estadística.

---

# 5. Perfil elemental del ciudadano

## 5.1 Datos persistentes mínimos

```text
PrimaryAffinity
ElementalMasteryByAffinity
ElementalTechniques
ElementalExperience
```

La primera versión puede persistir solamente:

```text
PrimaryAffinity
```

El resto puede añadirse cuando exista un sistema consumidor real.

## 5.2 Estado runtime

Durante combate, trabajo o expedición pueden existir:

```text
CurrentElementalLoad
CurrentElementalFatigue
ActiveElementalEffects
TemporaryResonanceModifiers
```

No convertir estos valores en atributos permanentes del onboarding.

## 5.3 Afinidades secundarias

No son necesarias para el primer slice.

La arquitectura puede permitir que un ciudadano aprenda técnicas relacionadas con otras afinidades sin cambiar su afinidad primaria.

Una posible evolución futura:

```text
PrimaryAffinity
LearnedAffinities
CrossAffinityMastery
```

La afinidad primaria representa facilidad natural, no exclusividad.

---

# 6. Formas de manifestación

Toda técnica elemental debe pertenecer a una o más formas de uso.

## 6.1 Manifestación interna

El ciudadano altera temporalmente su propio cuerpo o condición.

Ejemplos:

- reforzar postura con Tierra;
- regular temperatura con Agua;
- acelerar respuesta con Fuego;
- aligerar desplazamiento con Aire;
- conectar percepción mediante Éter;
- aislar dolor o interferencia con Silencio.

## 6.2 Manifestación canalizada

La afinidad atraviesa una herramienta, arma, armadura, edificio o dispositivo.

Depende de:

- resonancia del equipo;
- tolerancia elemental;
- condición;
- materiales;
- entrenamiento;
- duración.

## 6.3 Manifestación ambiental

La afinidad altera un proceso existente en el mundo.

No debe crear materia o energía infinita.

Ejemplos:

- Tierra reorganiza o estabiliza material disponible;
- Agua regula humedad o transporte de fluidos existentes;
- Fuego acelera una transformación con combustible o energía disponible;
- Aire desplaza gases, calor, partículas o presión;
- Éter conecta, transmite o modifica relaciones existentes;
- Silencio aísla, amortigua o interrumpe intercambios.

## 6.4 Manifestación cooperativa

Dos o más ciudadanos, herramientas o instalaciones pueden combinar afinidades.

La combinación debe producir un proceso explicable, no un hechizo arbitrario desbloqueado por color.

---

# 7. Tierra

## 7.1 Principio

Tierra representa:

- estructura;
- masa;
- cohesión;
- presión;
- soporte;
- contacto material;
- permanencia física.

No es solamente roca.

Puede manifestarse en:

- suelo;
- minerales;
- cerámica;
- metal;
- hueso;
- cimentación;
- compactación;
- distribución de cargas.

## 7.2 Manifestación ambiental

Puede intervenir en:

- estabilidad del suelo;
- erosión;
- compactación;
- deslizamientos;
- retención de nutrientes;
- sedimentación;
- fracturas;
- disponibilidad mineral.

### Uso regenerativo

- restaurar estructura de suelo degradado;
- estabilizar pendientes;
- reconstruir terrazas;
- reducir erosión;
- devolver soporte a raíces;
- consolidar caminos sin sellar completamente el terreno.

### Uso extractivo

- acelerar excavación;
- concentrar minerales;
- fracturar roca;
- compactar superficies industriales;
- aumentar rendimiento de canteras;
- sostener infraestructura de extracción intensiva.

### Riesgos

- compactación excesiva;
- pérdida de porosidad;
- fractura;
- hundimiento;
- aumento de peso;
- rigidez;
- bloqueo de ciclos vivos.

## 7.3 Agricultura y clima

Tierra puede ayudar a:

- conservar estructura del suelo;
- formar terrazas;
- controlar erosión;
- distribuir raíces;
- recuperar parcelas colapsadas.

No genera fertilidad por sí sola.

Un suelo perfectamente estructurado puede seguir careciendo de:

- agua;
- nutrientes;
- biodiversidad;
- microorganismos;
- materia orgánica.

## 7.4 Arquitectura e industria

Aplicaciones:

- cimentación;
- refuerzo;
- evaluación de cargas;
- estabilización temporal;
- moldeado de materiales;
- minería;
- cerámica;
- metalurgia asistida;
- control de vibraciones estructurales.

Una intervención de Tierra no reemplaza diseño Ardhen, materiales ni mano de obra.

## 7.5 Salud

Puede intervenir en:

- inmovilización;
- soporte;
- presión controlada;
- rehabilitación estructural;
- asistencia sobre huesos o postura.

No debe convertirse en curación instantánea.

Riesgos:

- rigidez;
- compresión;
- daño por presión;
- reducción de movilidad;
- sobrecarga física.

## 7.6 Expediciones

Aporta posibilidades como:

- estabilizar terreno;
- asegurar campamentos;
- detectar diferencias de densidad;
- reforzar pasos;
- soportar cargas;
- contener derrumbes;
- abrir rutas materiales con coste y desgaste.

## 7.7 Combate

Puede expresarse mediante:

- aumento de estabilidad física;
- protección estructural;
- impacto;
- interrupción;
- barreras;
- presión;
- control de terreno.

No debe equivaler automáticamente a “tanque”.

Un ciudadano de Tierra puede utilizarla para daño, precisión, soporte, movilidad sobre superficies o fabricación.

## 7.8 Respuesta material

Riesgos para equipo:

- impacto repetido;
- vibración;
- compresión;
- deformación;
- acumulación de masa;
- fractura en puntos de unión.

---

# 8. Agua

## 8.1 Principio

Agua representa:

- regulación;
- continuidad;
- transporte;
- adaptación;
- absorción;
- intercambio térmico;
- circulación.

No es solamente líquido visible.

Puede manifestarse en:

- humedad;
- circulación corporal;
- disolución;
- enfriamiento;
- limpieza;
- transporte de nutrientes;
- control de temperatura;
- cambios de estado.

## 8.2 Manifestación ambiental

Puede intervenir en:

- cuencas;
- humedad;
- drenaje;
- inundación;
- sequía;
- contaminación;
- transporte de sedimentos;
- estabilidad térmica.

### Uso regenerativo

- recuperar humedad del suelo;
- distribuir agua;
- limpiar contaminantes cuando existe un destino seguro;
- restaurar ciclos hídricos;
- enfriar zonas degradadas;
- mantener reservas.

### Uso extractivo

- drenaje intensivo;
- lavado de minerales;
- refrigeración de producción;
- transporte de materiales;
- bombeo acelerado;
- concentración de recursos disueltos.

### Riesgos

- saturación;
- corrosión;
- erosión;
- contaminación transportada;
- proliferación de enfermedades;
- pérdida de tensión en materiales;
- inundación.

## 8.3 Agricultura y clima

Aplicaciones:

- riego;
- drenaje;
- transporte de nutrientes;
- regulación térmica;
- recuperación de plantas;
- control de humedad.

El Agua no reemplaza suelo, semillas, polinizadores ni conocimiento agrícola.

Demasiada Agua puede producir:

- raíces asfixiadas;
- hongos;
- erosión;
- pérdida de nutrientes;
- plagas;
- contaminación de acuíferos.

## 8.4 Arquitectura e industria

Aplicaciones:

- sistemas hidráulicos;
- refrigeración;
- limpieza;
- procesamiento;
- control de polvo;
- extinción;
- transporte por canales;
- almacenamiento térmico.

Riesgos de diseño:

- filtración;
- presión;
- corrosión;
- humedad persistente;
- degradación de cimientos.

## 8.5 Salud

Puede contribuir a:

- hidratación;
- regulación térmica;
- limpieza;
- circulación;
- reducción de inflamación;
- recuperación gradual.

No debe ser sinónimo universal de sanación.

No recompone automáticamente:

- huesos;
- órganos;
- tejidos destruidos;
- enfermedades;
- pérdida de sangre.

## 8.6 Expediciones

Aporta posibilidades como:

- purificar o evaluar agua;
- gestionar reservas;
- atravesar zonas húmedas;
- regular temperatura;
- limpiar equipo;
- controlar barro, vapor o escarcha cuando existan las condiciones.

## 8.7 Combate

Puede expresarse mediante:

- recuperación;
- absorción;
- limpieza de estados;
- reducción de temperatura;
- control de movimiento;
- presión;
- persistencia;
- adaptación defensiva.

También puede producir daño mediante:

- presión;
- impacto;
- frío derivado;
- arrastre;
- saturación;
- interferencia respiratoria.

## 8.8 Respuesta material

Riesgos para equipo:

- corrosión;
- hinchamiento;
- pérdida de tensión;
- lubricación no deseada;
- contaminación;
- debilitamiento de adhesivos;
- cambios térmicos.

---

# 9. Fuego

## 9.1 Principio

Fuego representa:

- transformación;
- aceleración;
- consumo;
- calor;
- liberación de energía;
- reacción;
- cambio irreversible.

No es solamente una llama visible.

Puede manifestarse en:

- combustión;
- cocción;
- fundición;
- esterilización;
- fermentación acelerada;
- metabolismo;
- presión térmica;
- reacciones químicas.

## 9.2 Manifestación ambiental

Puede intervenir en:

- combustible acumulado;
- incendios;
- temperatura;
- ceniza;
- renovación de ciertos ecosistemas;
- emisiones;
- transformación de suelo.

### Uso regenerativo

- quemas controladas;
- reducción de combustible peligroso;
- esterilización localizada;
- apertura de semillas dependientes del calor;
- reciclaje térmico;
- calefacción eficiente.

### Uso extractivo

- fundición intensiva;
- producción acelerada;
- desmonte;
- transformación rápida de materiales;
- generación térmica;
- cocción masiva;
- respuesta urgente a escasez.

### Riesgos

- propagación;
- agotamiento de combustible;
- humo;
- pérdida de humedad;
- deformación;
- contaminación;
- daño irreversible;
- aceleración fuera de control.

## 9.3 Agricultura y clima

Aplicaciones:

- control de plagas mediante calor;
- cocción y conservación;
- calefacción de invernaderos;
- quemas controladas;
- producción de carbón o ceniza.

Un exceso puede:

- destruir microorganismos;
- secar el suelo;
- consumir reservas;
- alterar el clima local;
- eliminar diversidad.

## 9.4 Arquitectura e industria

Aplicaciones:

- hornos;
- cocción de cerámica;
- metalurgia;
- calefacción;
- tratamiento de materiales;
- esterilización;
- producción energética.

Requiere:

- combustible;
- ventilación;
- disipación;
- materiales tolerantes;
- control;
- prevención de incendios.

## 9.5 Salud

Puede contribuir a:

- cauterización;
- esterilización;
- mantenimiento de temperatura;
- activación metabólica controlada.

Riesgos:

- quemaduras;
- deshidratación;
- daño tisular;
- fiebre;
- inflamación;
- consumo acelerado de reservas corporales.

## 9.6 Expediciones

Aporta posibilidades como:

- calor;
- cocción;
- señalización;
- iluminación;
- esterilización;
- despeje controlado;
- transformación de materiales encontrados.

Su uso consume recursos y puede revelar la posición del grupo.

## 9.7 Combate

Puede expresarse mediante:

- daño periódico;
- aceleración;
- presión ofensiva;
- consumo de defensas;
- transformación de estados;
- aumento temporal de velocidad;
- explosión de recursos acumulados.

No debe equivaler siempre a mayor daño directo.

Puede utilizarse defensivamente para:

- crear perímetros;
- esterilizar;
- disuadir;
- cauterizar;
- acelerar recuperación a cambio de fatiga.

## 9.8 Respuesta material

Riesgos para equipo:

- pérdida de temple;
- deformación;
- carbonización;
- expansión térmica;
- debilitamiento de uniones;
- consumo de lubricantes;
- daño a cuerdas y fibras.

---

# 10. Aire

## 10.1 Principio

Aire representa:

- movimiento;
- presión;
- propagación;
- intercambio gaseoso;
- distancia;
- ventilación;
- liberación.

No es solamente viento.

Puede manifestarse en:

- respiración;
- presión;
- sonido;
- clima;
- transporte de partículas;
- secado;
- sustentación;
- dispersión.

## 10.2 Manifestación ambiental

Puede intervenir en:

- circulación atmosférica;
- temperatura;
- humedad;
- polen;
- semillas;
- contaminación;
- incendios;
- erosión;
- tormentas.

### Uso regenerativo

- ventilación;
- dispersión controlada de calor;
- recuperación de calidad del aire;
- transporte de semillas o polen;
- secado preventivo;
- regulación microclimática.

### Uso extractivo

- molinos;
- transporte neumático;
- secado acelerado;
- ventilación industrial;
- aumento de combustión;
- concentración o separación de partículas.

### Riesgos

- propagación de humo, plagas o enfermedad;
- erosión;
- desecación;
- pérdida de calor;
- turbulencia;
- amplificación de incendios;
- colapso por presión.

## 10.3 Agricultura y clima

Aplicaciones:

- ventilación;
- polinización;
- control de humedad;
- secado de cosechas;
- dispersión de semillas;
- protección frente a heladas mediante circulación.

Un exceso puede:

- secar cultivos;
- erosionar suelo;
- dispersar plagas;
- romper plantas;
- acelerar incendios.

## 10.4 Arquitectura e industria

Aplicaciones:

- ventilación;
- control de humo;
- secado;
- molinos;
- presión;
- transporte;
- acústica;
- refrigeración.

Los edificios deben responder a:

- cargas de viento;
- turbulencia;
- presión diferencial;
- entrada de polvo;
- propagación de fuego.

## 10.5 Salud

Puede contribuir a:

- respiración;
- ventilación;
- despeje de partículas;
- control de aerosoles;
- regulación térmica.

Riesgos:

- hiperventilación;
- desecación;
- dispersión de patógenos;
- enfriamiento;
- presión dañina;
- dificultad respiratoria.

## 10.6 Expediciones

Aporta posibilidades como:

- detectar corrientes;
- anticipar clima;
- ventilar refugios;
- dispersar humo;
- ampliar señales;
- asistir proyectiles;
- reducir o aumentar exposición sonora.

## 10.7 Combate

Puede expresarse mediante:

- velocidad;
- alcance;
- desplazamiento;
- interrupción;
- evasión;
- propagación;
- presión;
- modificación de proyectiles.

No debe equivaler automáticamente a agilidad.

Puede utilizarse para soporte, control, defensa o canalización a distancia.

## 10.8 Respuesta material

Riesgos para equipo:

- torsión;
- vibración;
- desalineación;
- fatiga de materiales;
- pérdida de tensión;
- erosión por partículas;
- enfriamiento desigual.

---

# 11. Éter

## 11.1 Principio

Éter representa:

- relación;
- conexión;
- transmisión;
- continuidad entre entidades separadas;
- resonancia;
- información;
- interacción astral.

No debe tratarse como “magia genérica” ni como elemento superior.

Éter no crea automáticamente aquello que conecta.

Necesita:

- dos o más estados;
- un origen y un destino;
- una relación existente o construible;
- un medio;
- control;
- tolerancia a interferencia.

## 11.2 Manifestación ambiental

Puede intervenir en:

- anomalías;
- ecos astrales;
- vínculos entre regiones;
- transmisión de señales;
- sincronización de procesos;
- memoria residual;
- redes biológicas o artificiales.

### Uso regenerativo

- reconectar ciclos interrumpidos;
- restaurar comunicación entre sistemas;
- coordinar recuperación;
- detectar desequilibrios;
- conservar memoria ambiental;
- compartir carga de forma controlada.

### Uso extractivo

- transferencia remota;
- concentración de energía o recursos;
- sincronización de producción;
- control de redes;
- canalización intensiva;
- explotación de anomalías.

### Riesgos

- interferencia;
- propagación de fallos;
- resonancia en cascada;
- pérdida de identidad;
- contaminación entre sistemas;
- dependencia de red;
- inestabilidad astral.

## 11.3 Agricultura y clima

Éter no acelera plantas por sí solo.

Puede ayudar a:

- detectar relaciones entre suelo, agua y cultivo;
- sincronizar sistemas de riego;
- transmitir señales de alerta;
- observar redes vivas;
- coordinar polinización o recuperación;
- conectar sensores e instituciones.

Un uso excesivo puede hacer que una red completa comparta el mismo fallo.

## 11.4 Arquitectura e industria

Aplicaciones:

- transmisión de señales;
- coordinación de dispositivos;
- instrumentación;
- redes de control;
- distribución de carga;
- sincronización;
- acceso a mecanismos astrales.

No reemplaza infraestructura física.

Una conexión sin soporte Ardhen, comprensión Caelith o mantenimiento Kovari puede fallar de forma espectacularmente educativa.

## 11.5 Salud

Puede intervenir en:

- coordinación nerviosa;
- comunicación entre sistemas corporales;
- seguimiento de estados;
- transferencia controlada;
- estabilización de identidad tras exposición astral.

Riesgos:

- dolor compartido;
- interferencia sensorial;
- pérdida de límites;
- transferencia de estados dañinos;
- dependencia;
- desorientación.

No debe permitir transferencia ilimitada de heridas o vida.

## 11.6 Expediciones

Aporta posibilidades como:

- detectar anomalías;
- marcar relaciones entre lugares;
- mantener comunicación;
- registrar ecos;
- sincronizar al grupo;
- interactuar con ruinas o fenómenos astrales;
- compartir información bajo restricciones.

## 11.7 Combate

Puede expresarse mediante:

- transferencia;
- enlace;
- escudos compartidos;
- redistribución de efectos;
- aceleración de habilidades coordinadas;
- interrupción de conexiones;
- propagación controlada;
- manipulación de estados.

No debe ser el elemento que hace todo mejor.

Cada enlace crea una dependencia o un riesgo de propagación.

## 11.8 Respuesta material

Riesgos para equipo:

- interferencia;
- desincronización;
- alteración de propiedades;
- pérdida de calibración;
- eco residual;
- sobrecarga de conexiones;
- contaminación de datos o memoria.

---

# 12. Silencio

## 12.1 Principio

Silencio representa:

- aislamiento;
- amortiguación;
- neutralización;
- separación;
- precisión basal;
- ausencia controlada de resonancia;
- preservación de límites.

No es ausencia de afinidad.

No es antimagia universal.

No destruye automáticamente cualquier manifestación elemental.

Silencio crea condiciones donde un proceso puede:

- dejar de propagarse;
- reducir interferencia;
- ser observado de forma aislada;
- conservarse;
- estabilizarse;
- terminar sin contaminar otros sistemas.

## 12.2 Manifestación ambiental

Puede intervenir en:

- cuarentena;
- zonas de amortiguación;
- contención de contaminación;
- control de ruido o vibración;
- conservación;
- interrupción de propagación;
- estabilización de anomalías.

### Uso regenerativo

- aislar una plaga;
- permitir descanso de una parcela;
- crear reservas protegidas;
- contener contaminación;
- estabilizar procesos frágiles;
- preservar semillas, alimentos o memoria.

### Uso extractivo

- crear entornos industriales controlados;
- eliminar interferencias;
- trabajar materiales con precisión;
- suprimir respuestas ambientales durante una operación;
- aislar residuos;
- mantener procesos intensivos separados del exterior.

### Riesgos

- esterilidad;
- estancamiento;
- pérdida de señales útiles;
- aislamiento prolongado;
- supresión de recuperación;
- acumulación oculta de problemas;
- ruptura de redes vivas.

## 12.3 Agricultura y clima

Aplicaciones:

- cuarentena de cultivos;
- almacenamiento de semillas;
- cámaras de conservación;
- control de propagación de plagas;
- aislamiento térmico o químico;
- parcelas en descanso.

Un exceso puede:

- impedir polinización;
- reducir circulación;
- aislar microorganismos beneficiosos;
- ocultar síntomas;
- crear zonas estériles.

## 12.4 Arquitectura e industria

Aplicaciones:

- aislamiento;
- amortiguación;
- cámaras limpias;
- calibración;
- contención;
- archivos protegidos;
- control acústico;
- separación de procesos peligrosos.

Silencio es especialmente útil para:

- instrumentos precisos;
- talleres Kovari;
- archivos Caelith;
- depósitos Orveth;
- hospitales;
- zonas de cuarentena.

## 12.5 Salud

Puede contribuir a:

- aislamiento de agentes dañinos;
- reducción de estímulos;
- contención de estados;
- estabilización temporal;
- descanso;
- procedimientos de precisión.

Riesgos:

- ocultar dolor o síntomas;
- reducir comunicación corporal;
- prolongar estados sin resolverlos;
- entumecimiento;
- aislamiento psicológico o sensorial.

No debe equivaler a curación ni inmunidad.

## 12.6 Expediciones

Aporta posibilidades como:

- atravesar anomalías con menor interferencia;
- contener hallazgos peligrosos;
- crear campamentos discretos;
- reducir señales;
- preservar suministros;
- estabilizar herramientas;
- aislar a un integrante contaminado.

## 12.7 Combate

Puede expresarse mediante:

- resistencia elemental;
- disipación;
- reducción de propagación;
- interrupción;
- precisión;
- protección frente a estados;
- eliminación de efectos;
- estabilización de equipo.

No debe cancelar sin coste todas las afinidades.

Su eficacia depende de:

- Dominio;
- duración;
- cobertura;
- intensidad rival;
- estabilidad;
- equipo;
- carga acumulada.

## 12.8 Respuesta material

Silencio produce menos sobrecarga elemental convencional, pero puede provocar:

- pérdida de resonancia útil;
- rigidez de sistemas;
- aislamiento excesivo;
- incapacidad para transmitir señales;
- degradación de componentes dependientes de flujo;
- acumulación de energía no liberada.

---

# 13. Ejes opuestos del Cubo

Las caras opuestas representan tensiones, no enemigos naturales.

## 13.1 Tierra ↔ Éter

```text
Materia ↔ Relación
Soporte ↔ Conexión
Presencia ↔ Transmisión
```

Tierra pregunta:

> ¿Qué sostiene físicamente este proceso?

Éter pregunta:

> ¿Qué relaciones permiten que este proceso continúe entre entidades separadas?

### Conflicto posible

Una red Etérica muy eficiente puede superar la capacidad material de sus soportes.

Una estructura de Tierra demasiado cerrada puede impedir conexión, adaptación o transmisión.

### Sinergia posible

- redes con soporte físico estable;
- puentes que distribuyen carga e información;
- edificios coordinados;
- armaduras que reparten impacto;
- infraestructura astral anclada.

## 13.2 Agua ↔ Fuego

```text
Regulación ↔ Aceleración
Continuidad ↔ Transformación
Absorción ↔ Liberación
```

Agua pregunta:

> ¿Cómo se mantiene y distribuye el proceso?

Fuego pregunta:

> ¿Cómo se transforma con rapidez?

### Conflicto posible

- enfriamiento contra calentamiento;
- conservación contra consumo;
- regulación contra reacción acelerada.

### Sinergia posible

- vapor;
- esterilización;
- cocción;
- control térmico;
- producción energética;
- recuperación metabólica controlada.

## 13.3 Silencio ↔ Aire

```text
Aislamiento ↔ Propagación
Precisión ↔ Cobertura
Contención ↔ Movimiento
```

Silencio pregunta:

> ¿Qué debe dejar de transmitirse?

Aire pregunta:

> ¿Qué debe desplazarse o alcanzar otro lugar?

### Conflicto posible

- zonas aisladas frente a circulación;
- contención frente a ventilación;
- precisión frente a dispersión.

### Sinergia posible

- ventilación dirigida;
- cuarentenas con flujo controlado;
- propagación selectiva;
- filtración;
- señales de largo alcance sin ruido.

---

# 14. Interacciones combinadas

Las combinaciones no son recetas de hechizos fijas. Son relaciones sistémicas posibles.

| Combinación | Posibilidades | Riesgos |
|---|---|---|
| Tierra + Agua | suelo fértil, barro, sedimentación, cimentación húmeda | erosión, compactación, deslizamiento |
| Tierra + Fuego | cerámica, fundición, tratamiento térmico | fractura, pérdida de temple, contaminación |
| Tierra + Aire | control de polvo, erosión dirigida, sustentación de partículas | abrasión, tormentas de polvo, desecación |
| Tierra + Éter | infraestructura conectada, distribución de carga | propagación de fallos estructurales |
| Tierra + Silencio | aislamiento físico, cámaras estables, amortiguación | rigidez, sellado excesivo |
| Agua + Fuego | vapor, esterilización, regulación térmica | explosión, quemaduras, presión |
| Agua + Aire | clima, niebla, secado, dispersión de humedad | tormentas, enfermedad, pérdida de agua |
| Agua + Éter | redes de riego, transmisión de estados, sensores | contaminación distribuida |
| Agua + Silencio | conservación, cuarentena líquida, contención | estancamiento, proliferación oculta |
| Fuego + Aire | combustión, propulsión, secado, señales | incendio acelerado, humo, pérdida de control |
| Fuego + Éter | transferencia térmica, activación coordinada | sobrecarga en cascada |
| Fuego + Silencio | hornos aislados, cauterización precisa | acumulación térmica, fallo sin aviso |
| Aire + Éter | comunicación, señales, coordinación a distancia | interferencia, propagación de errores |
| Aire + Silencio | filtración, rutas limpias, difusión selectiva | bloqueo de circulación o presión peligrosa |
| Éter + Silencio | estabilización de anomalías, canales protegidos | corte de vínculos útiles, aislamiento identitario |

---

# 15. Relación con regeneración y extracción

Cada afinidad puede contribuir a ambos extremos.

| Afinidad | Expresión regenerativa | Expresión extractiva |
|---|---|---|
| Tierra | restaurar soporte, reducir erosión, recuperar suelo | fracturar, compactar, minar, sostener canteras |
| Agua | reponer ciclos, limpiar, distribuir, regular | bombear, drenar, lavar, refrigerar producción |
| Fuego | quema controlada, esterilizar, reciclar | fundir, acelerar, consumir, despejar |
| Aire | ventilar, dispersar calor, polinizar | secar, transportar, intensificar combustión |
| Éter | reconectar sistemas, observar redes, coordinar recuperación | transferir, concentrar, sincronizar producción |
| Silencio | aislar daño, conservar, permitir descanso | suprimir interferencia, contener residuos, estabilizar industria |

## 15.1 No crear una barra elemental de moralidad

La ciudad no debe ganar “puntos buenos” por utilizar Agua o Tierra.

Tampoco debe ganar “puntos malos” por utilizar Fuego o Silencio.

El impacto se calcula mediante consecuencias reales:

- consumo;
- recuperación;
- contaminación;
- desgaste;
- diversidad;
- estabilidad;
- producción;
- tiempo;
- riesgo;
- dependencia.

## 15.2 Estado elemental del territorio

No todas las regiones necesitan seis barras visibles.

El sistema puede almacenar solamente las variables relevantes para cada parcela o región.

Modelo conceptual:

```text
EarthState
WaterState
FireState
AirState
AetherState
SilenceState
```

Cada estado puede representar presión o condición, no cantidad de “maná”.

Ejemplos:

```text
EarthState: compactación y estabilidad
WaterState: disponibilidad y saturación
FireState: temperatura y combustible
AirState: circulación y turbulencia
AetherState: coherencia e interferencia
SilenceState: aislamiento y estancamiento
```

---

# 16. Integración con equipamiento

## 16.1 Propiedades obligatorias

Cada pieza relevante puede definir:

```text
Weight
Demand
MaxIntegrity
CurrentCondition
ElementalResonance
ElementalTolerance
```

## 16.2 Resonancia elemental

Mide la eficiencia con que una pieza transmite una afinidad.

Puede existir por elemento:

```text
EarthResonance
WaterResonance
FireResonance
AirResonance
AetherResonance
SilenceResonance
```

No implica que la pieza produzca el elemento.

## 16.3 Tolerancia elemental

Mide cuánta carga puede soportar antes de:

- perder condición;
- deformarse;
- fallar;
- interferir;
- reducir eficiencia;
- acumular daño permanente.

La primera versión puede utilizar una tolerancia general.

Una evolución futura puede utilizar tolerancia por elemento.

## 16.4 Fórmula conceptual de expresión

```text
Capacidad del ciudadano
× dominio técnico
× resonancia del equipo
× condición actual
× contexto
= efecto elemental real
```

## 16.5 Fórmula conceptual de desgaste

```text
Carga generada
- tolerancia disponible
+ vulnerabilidad material
+ duración
= desgaste elemental adicional
```

No fijar coeficientes definitivos en este guideline.

## 16.6 Equipo sin canalización

Una pieza con resonancia baja puede seguir siendo excelente físicamente.

Ejemplo:

- martillo robusto;
- gran integridad;
- buen peso para el usuario;
- baja resonancia con Aire.

El ciudadano conserva su capacidad física, pero expresa peor la afinidad a través de esa herramienta.

---

# 17. Traducción al combate automático

La afinidad no sustituye estadísticas explícitas.

Debe modificar o producir valores observables como:

```text
ElementalDamageByType
ElementalResistanceByType
ElementalPenetrationByType
ElementalDodgeByType
ElementalLoad
ElementalStatusChance
ElementalStatusResistance
ElementalEffectDuration
ElementalPropagation
```

No todas deben implementarse en el primer slice.

## 17.1 Firma de cada afinidad en combate

| Afinidad | Tendencias posibles |
|---|---|
| Tierra | estructura, impacto, interrupción, estabilidad, barrera |
| Agua | regulación, recuperación, absorción, limpieza, control sostenido |
| Fuego | transformación, aceleración, consumo, daño periódico, presión |
| Aire | velocidad, alcance, propagación, desplazamiento, evasión |
| Éter | enlace, transferencia, sincronización, interferencia, efectos compartidos |
| Silencio | aislamiento, disipación, precisión, resistencia, neutralización |

Estas son tendencias, no roles.

## 17.2 Ritmo Theryn

Los eventos elementales pueden tener representación musical:

- Tierra: impacto grave y sostenido;
- Agua: continuidad, capas fluidas y regulación;
- Fuego: ataques cortos, aceleración y tensión;
- Aire: desplazamiento, apertura y propagación;
- Éter: ecos, enlaces y capas sincronizadas;
- Silencio: cortes, espacios, amortiguación y ausencia controlada.

La música comunica el estado, pero no reemplaza las estadísticas.

---

# 18. Integración con sistemas de ciudad

## 18.1 Kovari

El Cubo explica:

- predisposición;
- estadísticas;
- control;
- expresión del ciudadano;
- compatibilidad con herramientas.

## 18.2 Ardhen

Los Anclajes utilizan afinidades para evaluar:

- carga;
- temperatura;
- humedad;
- vibración;
- ventilación;
- interferencia;
- aislamiento;
- mantenimiento.

## 18.3 Eirune

La Corola utiliza afinidades para modelar:

- suelo;
- agua;
- clima;
- redes vivas;
- ciclos;
- contaminación;
- regeneración;
- presión extractiva.

## 18.4 Vaelun

La Brújula utiliza afinidades para:

- peligros de ruta;
- clima;
- anomalías;
- terreno;
- campamentos;
- visibilidad;
- abastecimiento;
- regreso.

## 18.5 Orveth

El Relicario puede registrar:

- materiales resonantes;
- reservas sensibles;
- condiciones de almacenamiento;
- costes de mantenimiento;
- riesgos de transporte;
- valor de herramientas especializadas.

## 18.6 Caelith

El Ciclo permite:

- investigar manifestaciones;
- validar interacciones;
- descubrir riesgos;
- mejorar técnicas;
- registrar tolerancias;
- revisar conocimientos obsoletos.

## 18.7 Myrven

Las Máscaras pueden intervenir cuando la afinidad afecta:

- acceso a profesiones reguladas;
- responsabilidad por daños;
- licencias;
- ciudadanía;
- discriminación;
- representación;
- secretos técnicos;
- diplomacia.

La afinidad no debe convertirse automáticamente en casta social.

## 18.8 Theryn

El Octagrama convierte estados elementales en:

- identidad sonora;
- música ambiental;
- ritmo de combate;
- señales de peligro;
- memoria musical;
- variaciones de ciudad y expedición.

---

# 19. Expediciones y territorio

Cada región puede contener:

- condiciones elementales;
- materiales resonantes;
- riesgos;
- oportunidades;
- anomalías;
- requerimientos de preparación.

Ejemplo:

```text
Ruta volcánica

Temperatura alta
Aire inestable
Materiales con tolerancia de Fuego requerida
Agua necesaria para regulación
Silencio útil para cámaras cerradas
Éter interferido
```

La preparación puede incluir:

- ciudadanos;
- herramientas;
- protección;
- suministros;
- repuestos;
- técnicas;
- rutas alternativas.

No bloquear una expedición porque el grupo carece de una afinidad concreta.

La afinidad debe ofrecer estrategias, no llaves biológicas obligatorias.

---

# 20. Recursos y materiales

Los recursos pueden responder a afinidades sin convertirse en objetos encantados aleatorios.

Perfil posible:

```text
MaterialId
PhysicalProperties
ElementalResonanceProfile
ElementalToleranceProfile
ElementalVulnerabilities
ProcessingRequirements
```

Ejemplos:

- una fibra puede resonar bien con Aire, pero perder tensión con Agua;
- una cerámica puede tolerar Fuego, pero fracturarse bajo Tierra intensa;
- un metal puede transmitir Éter, pero acumular interferencia;
- un compuesto puede aislar mediante Silencio, pero retener demasiado calor.

La calidad de fabricación modifica la relación entre:

- peso;
- exigencia;
- integridad;
- resonancia;
- tolerancia;
- facilidad de reparación.

---

# 21. Salud y exposición elemental

La salud debe diferenciar:

- herida física;
- enfermedad;
- fatiga;
- carga elemental;
- contaminación;
- estado psicológico;
- daño de equipo.

Posibles estados:

```text
Burn
Saturation
Compression
Disorientation
Interference
Suppression
```

Los nombres finales deben adaptarse al lenguaje de Ravatha.

## 21.1 Tratamiento causal

Una exposición elemental puede requerir:

- descanso;
- aislamiento;
- hidratación;
- enfriamiento;
- estabilización;
- ventilación;
- desintonización;
- personal especializado;
- equipo;
- medicamentos;
- tiempo.

No resolver todas las exposiciones con una poción universal.

---

# 22. Presentación visual

Las afinidades deben ser identificables sin depender exclusivamente del color.

## Tierra

- peso;
- formas compactas;
- estratos;
- fracturas;
- partículas densas.

## Agua

- continuidad;
- ondas;
- gotas;
- transición;
- deformación fluida.

## Fuego

- expansión;
- consumo;
- pulsos;
- calor;
- bordes inestables.

## Aire

- líneas de flujo;
- partículas desplazadas;
- presión;
- estelas;
- apertura.

## Éter

- conexiones;
- nodos;
- ecos;
- duplicación;
- transmisión.

## Silencio

- interrupción;
- vacío controlado;
- bordes limpios;
- amortiguación;
- desaparición de ruido visual.

La UI debe respetar las guías existentes de iconografía, tipografía y pixel art.

---

# 23. Presentación sonora

Las afinidades pueden influir en:

- textura;
- ataque;
- duración;
- silencio;
- reverberación;
- densidad;
- ritmo.

No asignar un instrumento único e inmutable a cada afinidad.

Tendencias sugeridas:

| Afinidad | Firma sonora |
|---|---|
| Tierra | impactos, cuerpo, resonancia baja |
| Agua | continuidad, modulación, flujo |
| Fuego | transientes, aceleración, saturación |
| Aire | movimiento estéreo, respiración, apertura |
| Éter | ecos, capas enlazadas, armónicos |
| Silencio | cortes, amortiguación, espacio negativo |

El sistema Theryn puede combinar estas firmas con:

- estado ambiental;
- composición de la ciudad;
- formación;
- habilidades;
- ritmo de combate;
- acontecimientos históricos.

---

# 24. Arquitectura técnica recomendada

No asumir nombres de carpetas, nodos o clases sin inspeccionar el repositorio.

Separar:

```text
Definiciones de contenido
Dominio elemental
Cálculo de estadísticas
Estado runtime
Equipamiento
Ambiente
Persistencia
Presentación
Audio
```

## 24.1 Identificadores estables

No guardar índices visuales.

Utilizar IDs estables para:

```text
AffinityId
TechniqueId
EffectId
MaterialId
EnvironmentalInteractionId
```

## 24.2 Modelo conceptual

```csharp
public enum ElementalAffinityId
{
    Earth,
    Water,
    Fire,
    Air,
    Aether,
    Silence
}

public sealed record CitizenElementalProfile(
    ElementalAffinityId PrimaryAffinity,
    IReadOnlyDictionary<ElementalAffinityId, int> MasteryByAffinity);

public sealed record ElementalResponse(
    float Resonance,
    float Tolerance);

public sealed record EquipmentElementalProfile(
    IReadOnlyDictionary<ElementalAffinityId, ElementalResponse> Responses);
```

Este código es conceptual.

Los tipos finales deben respetar la arquitectura y convenciones actuales.

## 24.3 Contenido configurable

No codificar interacciones completas dentro de vistas o nodos UI.

Las técnicas y respuestas deberían poder definirse mediante recursos, datos o configuraciones inspeccionables.

Ejemplo conceptual:

```text
Technique
Affinity
ManifestationType
RequiredMastery
RelevantStats
EquipmentRequirements
EnvironmentalInputs
Outputs
Costs
Load
Cooldown
VisualCue
AudioCue
```

## 24.4 Recalcular desde fuentes

Las estadísticas derivadas deben reconstruirse a partir de:

- ciudadano;
- Cubo;
- experiencia;
- técnica;
- equipo;
- condición;
- entorno;
- estados temporales.

No restar manualmente contribuciones anteriores al cambiar equipo o configuración.

---

# 25. Persistencia y migración

Guardar como mínimo:

```text
PrimaryAffinity
```

Cuando se implementen:

```text
MasteryByAffinity
KnownTechniques
ElementalExposureHistory
```

No persistir estadísticas derivadas que puedan recalcularse, salvo que exista una razón de rendimiento o compatibilidad claramente documentada.

## Migración

- mapear `None` y `Neutral` a `Silence`;
- conservar respuestas del onboarding mediante IDs;
- no repetir onboarding;
- no cambiar el linaje;
- no generar afinidad aleatoria para guardados válidos;
- registrar versiones del perfil cuando sea necesario.

---

# 26. UI y transparencia

## 26.1 Perfil del ciudadano

Mostrar:

```text
Afinidad primaria
Dominio elemental
Técnicas conocidas
Carga actual cuando corresponda
Resistencias relevantes
```

## 26.2 Equipamiento

Mostrar:

```text
Resonancia con afinidad del usuario
Tolerancia
Condición
Desgaste esperado
Riesgos materiales
```

## 26.3 Ambiente

Mostrar causas concretas.

Evitar:

```text
Afinidad ambiental: 67 %
```

Preferir:

```text
Suelo compactado
Humedad elevada
Circulación baja
Interferencia etérica moderada
Zona de aislamiento activa
```

## 26.4 Desglose

Ejemplo:

```text
Daño de Aire: 42

Afinidad personal             +18
Alcance                       +11
Dominio técnico                +7
Resonancia del arco            +9
Condición de cuerda            -3
Turbulencia local              +2
Fatiga                         -2
```

La fórmula exacta puede permanecer abstraída, pero las fuentes deben ser visibles.

---

# 27. Fases de implementación

## Fase 0: consolidación

- adoptar nombres canónicos;
- definir `Silence`;
- documentar migración desde `None`;
- fijar relación con el Cubo;
- crear casos dorados del onboarding.

## Fase 1: perfil del ciudadano

- persistir afinidad;
- mostrar afinidad en ficha;
- conservar scoring actual;
- no añadir técnicas automáticas.

## Fase 2: equipamiento

- implementar resonancia;
- implementar tolerancia;
- conectar condición y desgaste;
- mostrar compatibilidad con el ciudadano.

## Fase 3: combate automático

- implementar una manifestación clara por afinidad;
- mostrar daño, resistencia, carga y estados;
- integrar señales visuales y musicales básicas;
- evitar una rueda rígida de debilidades.

## Fase 4: ambiente y expediciones

- añadir condiciones elementales regionales;
- integrar preparación de rutas;
- añadir interacciones materiales;
- conectar regeneración y extracción.

## Fase 5: ciudad

- agricultura;
- arquitectura;
- producción;
- salud;
- almacenamiento;
- investigación;
- regulación.

## Fase 6: profundidad cultural

- instituciones;
- técnicas regionales;
- conflictos;
- regulación;
- música reactiva;
- memoria histórica;
- contenido avanzado.

---

# 28. Pruebas mínimas

## Onboarding

1. Cada afinidad puede obtenerse.
2. Silencio no se trata como resultado vacío.
3. Cambiar una respuesta recalcula desde cero.
4. El linaje continúa independiente.
5. El scoring actual mantiene resultados conocidos.

## Ciudadano

6. La afinidad no desbloquea una profesión.
7. La afinidad no otorga una preferencia de arma.
8. Un ciudadano sin entrenamiento conserva su afinidad, pero no técnicas avanzadas.
9. Cualquier linaje admite cualquier afinidad.

## Equipamiento

10. El arma no otorga daño elemental base.
11. Resonancia modifica eficiencia.
12. Tolerancia modifica sobrecarga y desgaste.
13. Condición modifica rendimiento.
14. Materiales diferentes responden de forma distinta.

## Combate

15. Los efectos son explícitos.
16. Silencio no cancela todo sin coste.
17. Éter no funciona como elemento superior.
18. Agua no es siempre curación.
19. Fuego no es siempre daño.
20. Tierra no obliga a defensa.
21. Aire no obliga a evasión.

## Ambiente

22. Cada afinidad admite uso regenerativo y extractivo.
23. No existe moralidad elemental automática.
24. Las consecuencias dependen de recursos y contexto.
25. Las acciones no crean materia infinita.
26. Los estados ambientales explican su causa.

## Persistencia

27. `None` y `Neutral` migran a `Silence`.
28. No se repite onboarding.
29. No se duplica el fundador.
30. Las estadísticas derivadas se reconstruyen correctamente.

---

# 29. Criterios de aceptación

La integración se considera correctamente definida cuando:

1. existen seis afinidades canónicas;
2. Silencio reemplaza la ausencia de afinidad como concepto jugable;
3. las afinidades se mantienen independientes del linaje;
4. el onboarding asigna afinidad, pero no dominio;
5. cada afinidad tiene significado fuera del combate;
6. cada afinidad participa en regeneración y extracción;
7. el equipamiento canaliza, limita y se desgasta;
8. las armas no otorgan poder elemental base;
9. resonancia y tolerancia son propiedades distintas;
10. el Cubo explica la forma de expresión;
11. las estadísticas finales son explícitas;
12. no existe una rueda elemental rígida;
13. las interacciones dependen de contexto material;
14. ciudad y expediciones pueden utilizar las afinidades;
15. la arquitectura separa contenido, dominio, runtime y presentación;
16. existen pruebas de scoring, migración, desgaste e independencia de linaje;
17. las afinidades pueden crecer sin convertirse en clases;
18. la música Theryn puede representar sus estados sin sustituir métricas;
19. el sistema no exige arte final para comenzar;
20. cada efecto puede rastrearse hasta ciudadano, técnica, herramienta y entorno.

---

# 30. Guardarraíles

No hacer:

- seis clases elementales;
- armas que contengan el poder principal;
- daño base elemental otorgado por objetos;
- linajes bloqueados a elementos;
- Silencio como personaje sin magia;
- Éter como elemento todopoderoso;
- Agua como curación automática;
- Fuego como daño automático;
- Tierra como tanque automático;
- Aire como velocidad automática;
- una rueda universal de ventajas;
- una barra elemental moral;
- producción sin recursos;
- curación instantánea sin consecuencias;
- efectos ambientales sin estados explícitos;
- lógica de scoring dentro de vistas;
- IDs basados en índices visuales;
- fórmulas imposibles de inspeccionar;
- contenido que obligue al jugador a elegir una build durante el onboarding.

---

# 31. Resumen ejecutivo

Las seis afinidades son formas de relación con los procesos de Ravatha:

```text
Tierra
estructura la materia

Agua
regula la continuidad

Fuego
acelera la transformación

Aire
propaga el movimiento

Éter
conecta estados separados

Silencio
aísla y estabiliza la resonancia
```

El ciudadano aporta:

```text
afinidad
capacidad
control
experiencia
intención
```

El Cubo determina:

```text
cómo se expresa esa afinidad
```

El equipo determina:

```text
cómo se canaliza
cuánto exige
cuánto tolera
cuánto se desgasta
```

El ambiente determina:

```text
qué proceso existe
qué recursos están disponibles
qué consecuencias aparecen
```

La ciudad determina:

```text
qué conocimientos, instituciones y herramientas permiten utilizarla
```

> La afinidad no decide qué será un ciudadano. Define qué clase de relación con el mundo le resulta más natural aprender a sostener.
