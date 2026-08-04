# Vaelun — La Brújula aplicada a rutas y expediciones

## Estado

Guideline funcional para integrar la tradición Vaelun en el sistema de mapa, rutas, logística y expediciones automáticas de **World of Goses**.

---

# 1. Objetivo

La Brújula debe proporcionar el lenguaje con el que Ravatha representa:

- orientación;
- rutas conocidas y desconocidas;
- distancia práctica;
- coste de viaje;
- abastecimiento;
- riesgo;
- retorno;
- pérdida;
- transferencia de recursos y personas.

La Brújula no convierte a los Vaelun en la única cultura capaz de explorar. Toda ciudad puede organizar expediciones. El conocimiento Vaelun permite comprender y administrar esas expediciones con mayor profundidad.

> Una expedición no termina al encontrar su objetivo. Termina cuando aquello que salió puede regresar, o cuando la ciudad acepta que no lo hará.

---

# 2. Lugar dentro del gameplay

La Brújula explica tres escalas conectadas:

## Escala territorial

- regiones;
- caminos;
- rutas alternativas;
- puntos de interés;
- fronteras;
- pasos bloqueados;
- cambios estacionales;
- zonas de influencia.

## Escala de expedición

- objetivo;
- trayecto de ida;
- trayecto de regreso;
- suministros;
- duración estimada;
- amenazas;
- capacidad de retirada;
- carga recuperable.

## Escala urbana

- caminos internos;
- estaciones de paso;
- almacenes de tránsito;
- puentes;
- accesibilidad;
- tiempos de desplazamiento;
- distribución entre distritos.

---

# 3. La ruta como entidad jugable

Una ruta no debe ser únicamente una línea dibujada en el mapa.

Debe poder conservar propiedades explícitas como:

```text
Origen
Destino
Longitud estimada
Tiempo estimado
Nivel de conocimiento
Transitabilidad
Riesgo conocido
Riesgo desconocido
Consumo esperado
Capacidad de carga
Puntos de descanso
Puntos de retirada
Estado ambiental
Control territorial
```

La ruta puede cambiar sin que el terreno físico se mueva:

- aparece una plaga;
- se derrumba un puente;
- cambia el clima;
- una facción cierra el paso;
- una criatura ocupa un corredor;
- una estación de paso deja de operar;
- una expedición crea un atajo;
- una ruta segura deja de serlo.

---

# 4. Niveles de conocimiento

La Brújula debe distinguir entre lo que existe y lo que la ciudad sabe.

Estados recomendados:

```text
Desconocida
Sospechada
Observada
Recorrida
Cartografiada
Verificada
Desactualizada
```

Una ruta cartografiada no es necesariamente segura. Una ruta verificada puede quedar desactualizada.

El sistema debe evitar revelar con precisión absoluta:

- enemigos no observados;
- recursos aún no encontrados;
- cambios ocurridos desde la última visita;
- consecuencias políticas desconocidas.

Caelith puede mejorar la certeza de las estimaciones, pero Vaelun define la estructura espacial que debe ser estimada.

---

# 5. Planificación de expediciones

Antes de salir, el jugador debe decidir o revisar:

```text
Objetivo
Ruta principal
Ruta alternativa
Integrantes
Carga inicial
Reserva de suministros
Capacidad de regreso
Condición de retirada
Carga prioritaria
Tolerancia de riesgo
```

No debe existir control directo de movimiento durante el combate o el viaje principal.

La preparación determina el comportamiento automático.

## Resumen visible recomendado

```text
Duración estimada: 2,4 días
Suministros: 3,1 días
Carga disponible: 42 kg
Riesgo conocido: medio
Riesgo no evaluado: alto
Retirada disponible: sí
Probabilidad de regreso completo: 71 %
```

Las estimaciones deben mostrar incertidumbre cuando corresponda. No deben fingir omnisciencia matemática.

---

# 6. El regreso como parte del diseño

La Brújula debe impedir que el juego trate el regreso como una pantalla de resultados gratuita.

Durante el retorno pueden ocurrir:

- fatiga acumulada;
- deterioro de armas;
- heridas;
- pérdida de suministros;
- peso adicional por recursos;
- transporte de heridos;
- persecución;
- rutas bloqueadas;
- decisiones de abandono de carga.

Una expedición puede cumplir su objetivo y fracasar en regresar con él.

## Prioridades de retorno

El jugador puede definir un orden como:

```text
1. Ciudadanos vivos
2. Ciudadanos heridos
3. Objeto de misión
4. Equipo recuperable
5. Recursos comunes
```

Las prioridades no garantizan el resultado. Orientan las decisiones automáticas.

---

# 7. Estadísticas y métricas

La Brújula puede utilizar métricas explícitas como:

- velocidad de marcha;
- consumo por distancia;
- consumo por tiempo;
- capacidad de carga;
- conocimiento de ruta;
- detección de desvíos;
- eficiencia de retirada;
- recuperación de orientación;
- resistencia a pérdida;
- transferencia logística;
- tiempo de respuesta desde la ciudad.

Estas estadísticas provienen de ciudadanos, entrenamiento, clima, carga, infraestructura y conocimiento. No nacen mágicamente de portar un objeto Vaelun.

---

# 8. Instituciones Vaelun

El contacto cultural puede habilitar o profundizar:

## Casas del Retorno

- refugios neutrales;
- recuperación de expedicionarios;
- registro de desaparecidos;
- puntos seguros de retirada.

## Colegios de rutas

- entrenamiento de orientación;
- actualización cartográfica;
- preparación logística;
- transferencia de experiencia entre expedicionarios.

## Torres de señal

- reducen incertidumbre local;
- permiten detectar cambios;
- mejoran la coordinación de retorno;
- pueden convertirse en objetivos estratégicos.

## Consejos de frontera

- negocian acceso;
- gestionan peajes;
- reconocen rutas compartidas;
- producen conflictos de soberanía.

Cada institución debe traer costes de construcción, mantenimiento y legitimidad.

---

# 9. Relación con otros sistemas

## Kovari

El Cubo determina las capacidades de quienes viajan. La Brújula determina dónde y durante cuánto tiempo deben expresarlas.

## Ardhen

Los Anclajes construyen puentes, refugios y caminos estables.

## Eirune

La Corola describe clima, suelo, agua y riesgos biológicos de una ruta.

## Orveth

El Relicario regula peajes, reservas, derechos de tránsito y propiedad de la carga.

## Caelith

El Ciclo determina certeza, pronósticos y actualización del conocimiento.

## Myrven

Las Máscaras definen permisos, ciudadanía, representación y relaciones diplomáticas en territorios cruzados.

## Theryn

El Octagrama convierte salida, marcha, peligro, retirada y regreso en una evolución musical perceptible.

---

# 10. Interfaz recomendada

La vista de Brújula puede contener:

- mapa territorial;
- rutas superpuestas;
- estado de conocimiento;
- duración y consumo;
- puntos de retorno;
- riesgos conocidos;
- cambios desde la última visita;
- comparación entre rutas.

No depender únicamente del color.

Usar:

- iconos;
- patrones de línea;
- etiquetas;
- niveles de certeza;
- tooltips con desglose;
- navegación mediante teclado y gamepad.

---

# 11. Límites de diseño

No implementar la Brújula como:

- teletransporte gratuito;
- radar omnisciente;
- bonificación racial exclusiva;
- mapa completamente revelado;
- simple selector de misión;
- sistema donde la ida importa y el regreso se resuelve solo.

---

# 12. Fases de implementación

## Fase 1

- rutas básicas;
- duración;
- suministros;
- ida y regreso;
- condición de retirada.

## Fase 2

- niveles de conocimiento;
- rutas alternativas;
- cambios ambientales;
- puntos de descanso.

## Fase 3

- instituciones Vaelun;
- control territorial;
- rutas compartidas;
- consecuencias diplomáticas.

## Fase 4

- rutas dinámicas;
- estaciones históricas;
- pistas sobre la segunda conciencia;
- memoria persistente de expediciones.

---

# 13. Criterios de aceptación

La integración se considera válida cuando:

1. el jugador prepara ruta de ida y regreso;
2. la carga y los suministros afectan la expedición;
3. el mapa distingue realidad de conocimiento;
4. las estimaciones pueden ser inciertas;
5. las rutas cambian por acontecimientos reales;
6. ciudadanos de cualquier linaje pueden explorar;
7. las instituciones Vaelun profundizan, pero no monopolizan el sistema;
8. el regreso puede fracasar aunque el objetivo haya sido cumplido;
9. la interfaz explica por qué una ruta es recomendable o peligrosa;
10. la Brújula se conecta con ciudad, ambiente, comercio y música.
