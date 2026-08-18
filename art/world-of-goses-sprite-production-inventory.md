# Inventario de producción de sprites

> Qué proyectos de Pixelorama hay que abrir para reemplazar los placeholders, en
> qué orden, y **cuál es la unidad de reutilización**. Complementa
> [`docs/presentation/art-pipeline.md`](../docs/presentation/art-pipeline.md)
> (mecánica de rutas y nombres) y
> [`docs/presentation/visual-language.md`](../docs/presentation/visual-language.md)
> (dirección visual, rejilla de 32, tres escalas). Cuando este archivo y
> aquéllos discrepen en una ruta o un nombre, **aquéllos ganan**.

Este documento **no** es canon de sistema. Donde el diseño de animación necesita
una regla que el canon todavía no fija, lo dice en §12 en vez de rellenarlo.

---

## 1. Las tres decisiones que acotan el proyecto

### 1.1 Sólo el Weapon Skill Tree toca la animación

| Skill tree | Qué modifica | Coste en frames de cuerpo |
| --- | --- | ---: |
| **Weapon** | moveset, animación, ejecución | **todo el presupuesto** |
| **Physical Expression** | efectos, estados, comportamiento físico | **0** |
| **Elemental Affinity** | efectos, daño, comportamiento elemental | **0** |

Coincide con lo que el canon ya afirma: la afinidad «decide la naturaleza de una
manifestación, no su tamaño» y la expresión física «interpreta el resultado del
canal físico»
([`elemental-affinities.md`](../docs/systems/elemental-affinities.md),
[`statistics-and-combat.md`](../docs/systems/statistics-and-combat.md) §2.3).
Interpretar un resultado ocurre **en el objetivo**; una manifestación es **lo que
sale del arma**. Ninguna de las dos es una postura.

Sin esta regla: `12 armas × 6 expresiones × 6 afinidades × 2` = **864 movesets**.
La combinatoria no se recorta, se **reubica** a la librería de efectos (§9.4),
que es compartida y barata.

### 1.2 Cada arma es un conjunto cerrado

Un látigo, un guantelete, una lanza y un orbe no comparten agarre, guardia,
trayectoria ni recuperación. **No hay slash genérico recoloreado.** La unidad de
reutilización es el

> **Weapon Animation Set** — 12 conjuntos completos, uno por familia.

### 1.3 Una vestimenta por arma, en capa propia, en grises indexados

La vestimenta **no se fusiona** con el cuerpo y el arma. Es una capa aparte, y
hay **una por arma**, no ocho. La identidad de linaje entra por dos vías
distintas y ninguna cuesta frames nuevos:

| Vía | Qué aporta | Cómo |
| --- | --- | --- |
| **Rampa de paleta** | el color del linaje | dato: una textura de 6 × 8 px por linaje (§6.2) |
| **Capa `misc`** | la forma del linaje | pieza **rígida** anclada, 1 por linaje |

Esta decisión es la que hace posible la anterior: sin separar la tela del cuerpo,
una rampa de linaje recolorearía también la piel. **Las dos ideas se necesitan
mutuamente.**

### 1.4 Un solo lienzo: 64 × 64 en las tres escalas

**El ciudadano mide lo mismo en ciudad macro, escena de edificio y expedición.**
Lo único que cambia es que el macro necesita cuatro direcciones y la expedición
una sola espejada. Esto corrige el canon, que llegó a sostener tres tamaños
distintos —4–8 px, 32 × 64 y 64 × 96— contradiciéndose dentro de un mismo
archivo. Ya está aplicado en `visual-language.md` §Tres escalas visuales y en
`art-pipeline.md` §Escalera de tamaños.

Lo que la unificación regala:

- **Un solo cuerpo, un solo juego de proporciones.** Desaparece el problema de
  las dos escalas y con él la tentación de reescalar.
- **La locomoción lateral dibujada para la ciudad sirve a la expedición tal
  cual.** No hay conjunto de viajero aparte: es la dirección lateral del mismo
  set, con el arma guardada como pieza rígida.
- 3 direcciones dibujadas cubren 4 (el lado se espeja), y las mismas sirven para
  el combate.

Lo que cuesta: cada frame de macro tiene el doble de área que en 32 × 64, así que
el bloque de locomoción cuesta más por frame aunque tenga menos frames (59 en
lugar de 79).

Y la proporción con los edificios, que **sale bien sin tocar el 96**: una planta
real son ~2,2 m de techo interior con una persona de ~1,75 m — 80 % contra el techo,
pero sólo 55–65 % contra el volumen exterior, porque fachada y tejado añaden alto.
Un ciudadano de ~56 px dibujados sitúa el edificio de una planta cerca de **96 px**,
que es la cifra heredada. Lo que sí es eje independiente es el **alto del lienzo**,
por dos razones distintas de «se ve bajo»: un edificio de dos plantas, y el plano del
tejado que se abre en las filas cercanas (§9.6). Ambas proporciones se miden y ambas
deben cumplirse: **figura/fachada ≈ 75–85 %, figura/sprite completo ≈ 55–65 %**.

### 1.5 El presupuesto resultante

| Bloque | Frames |
| --- | ---: |
| 12 conjuntos — capa `body` (110 c/u) | 1.320 |
| 12 conjuntos — capa `garment` (110 c/u) | 1.320 |
| Compartido: locomoción, verbos de trabajo, `knockback`, `defeated` (× 2 capas, §14.5) | 360 |
| Piezas rígidas: pelo, arma guardada, `misc`, armadura dura, herramientas (§14.6) | ~512 |
| Librería de efectos | 312 |
| Bestiario (6 arquetipos) | 234 |
| Edificios: 20 fachadas + hasta 42 planos de tejado + 32 kits de bioma (§9.6) | ~94 |
| **Nominal** | **~4.152** |
| **Esfuerzo efectivo** (la capa `garment` cuesta ~50 % de la de `body`; las piezas rígidas son dibujos pequeños) | **~3.100 equivalentes-cuerpo** |

Rampas de paleta: ~24 texturas diminutas. **Cero frames.**

Los 2.640 frames de combate son el **64 % del proyecto artístico**. Eso decide el
orden de producción (§11): no se abren doce conjuntos, se abre uno, se valida en
el motor, y sólo entonces se repite.

---

## 2. El grafo de combate arranca en Weapon Combat Idle

`Idle`, `walk` y `dodge` **no** son la raíz. La raíz de cada conjunto es su
guardia, y todo lo demás sale y vuelve a ella:

```text
        ┌──────────────────────────────────────────────┐
        │  idle / walk / run / carry / climb           │
        │  LOCOMOCIÓN — la misma que la ciudad usa,    │
        │  dirección lateral, arma guardada (§9.2)     │
        └───────────────────────┬──────────────────────┘
                                │ unsheathe  (4f, por arma)
                                ▼
   ┌────────────────────► WEAPON COMBAT IDLE ◄────────────────────┐
   │                       (raíz del conjunto)                    │
   │                               │                              │
   │   ┌─────────┬────────┬────────┼────────┬─────────┬────────┐  │
   │   ▼         ▼        ▼        ▼        ▼         ▼        ▼  │
   │ advance  retreat  basic_a  active   dodge     evade   hurt_l │
   │                     │        │                          │   │
   │            ┌────────┴───┐    ├─ as_expand_1..2           │   │
   │            │  basic_b   │    └─ as_branch_1..2           │   │
   │            │  basic_c   │                          hurt_heavy│
   │            ├─ ba_expand_1..2   (encadenan)               │   │
   │            └─ ba_branch_1..2   (bifurcan en nodo propio) │   │
   │                                                          │   │
   └──────────────── regreso a guardia ───────────────────────┴───┘
                                │
                    knockback ──┤  (compartidos, sin arma)
                    defeated  ──┘  única salida sin regreso
```

Tres consecuencias de producción, todas útiles:

1. **El primer y el último frame de casi todo clip es la guardia.** Se dibuja una
   vez por conjunto y sirve de entrada y salida en unos quince clips. Es el frame
   más rentable del conjunto: si la guardia está floja, el arma entera está floja.
2. **Un empalme no se ajusta a ojo.** El clip arranca literalmente en el frame de
   guardia ya dibujado, así que la transición es exacta por construcción.
3. **`ready` es el puente y además un beat de diseño.** Es el momento en que
   el canon manda que la cámara suelte la rejilla y pase a movimiento continuo
   (`FrameEncounter`, `visual-language.md` §Pixel-motion grammar). Cuatro frames
   compran la entrada al combate.

---

## 3. El contrato de clips

Los doce conjuntos son **dibujos distintos con la misma estructura**. Ahí vive la
reutilización cuando el pixel no se puede reutilizar: se reusa el contrato. Un
conjunto que lo respeta entra en el motor sin código nuevo.

> **Las cifras de frames son un presupuesto, no una especificación.** Nadie ha
> definido todavía de cuántos pasos es un `idle`. Estas cifras existen para que los
> totales signifiquen algo y para que el reparto entre ejes sea comparable; el número
> definitivo de cada clip se fija dibujando el primero. Ver §3.3: parte se **deriva**
> de reglas que el proyecto ya tiene, y parte es decisión abierta. El plan es robusto
> a equivocarse: ±1 frame en cada uno de los 16 clips de las 12 armas mueve el total
> del proyecto un **±5 %**, y no cambia ninguna decisión de este documento.

| Clip | Frames | Qué es |
| --- | ---: | --- |
| `combat_idle` | 4 | guardia del arma — la raíz |
| `ready` | 4 | **puente hacia la guardia desde cualquier estado que no lo sea** — llegar de viaje, o llegar corriendo tras cerrar distancia |
| `combat_advance` | 6 | avance en guardia |
| `combat_retreat` | 6 | retroceso en guardia |
| `basic_a` | 6 | golpe 1 |
| `basic_b` | 5 | golpe 2, arranca en la pose final de `basic_a` |
| `basic_c` | 6 | remate de la cadena |
| `ba_expand_1..2` | 2 × 4 | **encadenan**: golpe nuevo tras el remate |
| `ba_branch_1..2` | 2 × 7 | **bifurcan**: sustituyen una rama, cada uno en **nodo distinto** |
| `active` | 8 | Weapon Active Skill |
| `as_expand_1..2` | 2 × 5 | encadenan |
| `as_branch_1..2` | 2 × 8 | bifurcan, nodos distintos |
| `dodge` | 5 | esquiva con desplazamiento |
| `evade` | 5 | disipación sin desplazamiento |
| `hurt_light` | 3 | impacto que no rompe la guardia |
| `hurt_heavy` | 4 | impacto que la rompe |
| **Total por conjunto** | **110** | × 12 = **1.320** por capa |

**Compartidos entre las doce armas** (§9.2): `knockback` (5) y `defeated` (6), sin
arma en mano. Y el conjunto del viajero.

Reglas del contrato, iguales para los doce:

- Mismos nombres de clip, mismos anclajes, misma línea base, mismo número de
  nodos de bifurcación. **Cambia el dibujo, nunca la interfaz.**
- Vista lateral, una dirección, espejada — el combate es lateral *por
  proyección*, no por recorte (`visual-language.md` §Tres escalas).
- Capas por archivo: `body`, `garment`, `weapon`, `hair`, grupo `anchors`.
  `weapon` va **dentro del conjunto**, registrada frame a frame; `garment` va en
  grises indexados (§6).
- El presupuesto de frames se gasta en el contacto, no en el número de frames
  (§7.4).

### 3.1 Expansión y bifurcación: 2 + 2, en nodos distintos

Cada arma tiene 8 traits que tocan animación — 4 de Basic Attack, 4 de Active
Skill —, repartidos **2 de expansión y 2 de bifurcación** en cada tree.

| Tipo | Qué hace | Coste | Compone con otro del mismo tipo |
| --- | --- | ---: | --- |
| **Expansión** | añade un golpe al final de la cadena | 4–5 f | **sí**, se apilan |
| **Bifurcación** | sustituye una rama por otra más elaborada | 7–8 f | **sólo si cada una vive en un nodo distinto** |

**La regla que hace funcionar el 2 + 2:** cada bifurcación declara **su propio
nodo de rama**. `ba_branch_1` bifurca tras `basic_a`; `ba_branch_2` bifurca tras
`basic_b`. Si las dos bifurcaran en el mismo nodo serían mutuamente excluyentes,
y un jugador que desbloquee ambas desperdicia una — mala sensación de build y
arte pagado que nunca se ve.

Con la regla puesta, los cuatro traits pueden estar activos a la vez y la cadena
resultante es:

```text
basic_a → [branch_1] → basic_b → [branch_2] → basic_c → [expand_1] → [expand_2]
```

**22 frames dibujados producen 16 cadenas distintas** (2⁴ combinaciones de
desbloqueo). Ésa es la respuesta a «qué conviene artísticamente»: el 2 + 2 con
nodos distintos maximiza cadenas distintas por frame dibujado.

Por qué no 4 expansiones: la cadena crece pero no cambia de carácter — «más de lo
mismo». Por qué no 4 bifurcaciones: harían falta cuatro nodos de rama en una
cadena de tres golpes, el doble de frames, y el jugador dejaría de distinguir
cuál está viendo. Las bifurcaciones son donde se ve el **carácter** del arma (la
espiral y el recogido de un látigo frente a un latigazo recto), y dos es donde
eso todavía se lee.

### 3.2 De dónde sale el número de frames de un clip

Dos reglas lo derivan y una cosa queda abierta. Ninguna es cuestión de gusto.

**Ataques: la forma manda.** Un golpe legible tiene cuatro tramos, y el reparto es lo
que fija el número:

```text
anticipación 1  ·  CONTACTO 2  ·  continuación 1  ·  recuperación 2   = 6
```

De ahí el 6 de `basic_a`. El contacto se lleva dos porque es donde va el smear y la
manifestación; la recuperación reutiliza el retorno a guardia. Un golpe **pesado no
lleva más frames: retiene el de contacto** — el tempo es dato en el `SpriteFrames`.

**Locomoción: la cadencia manda, y no es negociable.** El canon fija movimiento
cuantizado a **24 Hz avanzando 4 px por tick** (96 px/s,
`visual-language.md` §Pixel-motion grammar). Eso restringe el ciclo de caminado: si
la zancada dibujada no coincide con lo que la posición avanza, **los pies patinan**.

```text
frames_del_ciclo × ticks_por_frame × 4 px  =  distancia de una zancada completa
```

Para una figura de ~56 px, una zancada completa (dos apoyos) ronda los 48 px, así que
`frames × ticks_por_frame = 12`:

| Reparto | Frames dibujados | Lectura |
| --- | ---: | --- |
| 6 × 2 ticks | 6 | equivale a 12 fps — marcha clásica, la recomendación |
| 4 × 3 ticks | 4 | 8 fps — más barato y más marcado; encaja con la preferencia por movimiento discreto |
| 12 × 1 tick | 12 | 24 fps — fluido, contradice la gramática del proyecto |

**Decidido: `walk` 64 px/s, `run` 96 px/s, y el paso sigue siendo de 4 px.** Lo que
cambia entre los dos no es el tamaño del paso sino la **cadencia**:

| Marcha | Cadencia | Paso | Velocidad |
| --- | ---: | ---: | ---: |
| `walk` | 16 Hz | 4 px | 64 px/s |
| `run` | 24 Hz | 4 px | 96 px/s |

Mantener el paso de 4 px como invariante es lo que preserva la gramática: 64 px/s a
24 Hz saldría a 2,67 px por tick, que rompe el cuanto. Y con una zancada de 48 px por
ciclo, las dos únicas particiones enteras son:

| Frames del ciclo | px por frame | Ticks por frame | fps caminando | fps corriendo |
| ---: | ---: | ---: | ---: | ---: |
| 6 | 8 | 2 | 8 | 12 |
| **12** | **4** | **1** | **16** | **24** |

Las dos plantan el pie a ambas velocidades con **un solo ciclo dibujado**. La de 6
coincide casi exactamente con lo que declara el `metadata.json` de LPC (`walk` 9 a 9
fps, `run` 8 a 12) — buena señal de que el rango es el correcto.

Y la de 12 es a la vez **la opción fluida y el techo**: a velocidad de carrera sale a
un frame por tick, o sea que saturas la cadencia de 24 Hz y no se puede ir más fino
sin romper el movimiento discreto. No hay tentación de pasarse. Recomendación: **12
frames**, que aquí cuesta +36 frames en todo el proyecto.

El bonus de combate por `Impulse` de [#58](https://github.com/3FE3LE/world-of-goses/issues/58)
no cambia el dibujo: la velocidad efectiva varía por persona y lo que varía con ella es
la **cadencia de reproducción**, no el ciclo. Un solo ciclo sirve a cualquier
velocidad — `PacedRouteSteps` ya deriva los pasos de la duración que da el dominio.

**Decidido: `idle` de 4 frames, y más de un estado de idle.** Sin zancada no hay
restricción de cadencia, así que aquí el presupuesto es libre. Los estados extra se
eligen por `AppearanceSeed`, y eso resuelve un problema real que sólo aparece con la
ciudad poblada: **una plaza llena respirando sincronizada delata el truco**. Es de lo
más barato del proyecto — 4 frames por estado — y de lo que más se nota.

El presupuesto del contrato (§3) sigue contando `combat_idle` en 4, que es su
recomendación original: es la raíz del conjunto y está en pantalla durante todo un
combate.

**Y una trampa que no es de fps: la velocidad de simulación.** Más frames por
segundo no dan fluidez — **la fluidez son frames por ciclo**. Ocho frames a 32 fps
son los mismos ocho, cuatro veces más rápido. Así que el número de frames se juzga
a 1×, que es la única velocidad donde alguien mira la marcha; a 4× es avance
rápido y nadie evalúa una zancada.

Y hoy, además, la animación **no se acelera con la velocidad**:
`CityWorldController.SetSimulationSpeed` sólo cambia
`SimulationTickIntervalSeconds = 1 / velocidad`, mientras que
`PixelMotion.CadenceSeconds` (1/24) y `StepPixels` (4) son constantes y el
acumulador se alimenta de `delta` sin escalar. Ver §12.5: es una decisión de
diseño abierta, y la respuesta afecta a la cadencia, no al dibujo.

**Dónde gastar el presupuesto de fluidez.** En `walk` y `run`, no en los ataques:

| Clip | Por qué | Coste de subirlo |
| --- | --- | ---: |
| `walk` / `run` | **transversales**: se dibujan una vez y se ven constantemente | 12 frames en vez de 6 → +36 en todo el proyecto |
| Ataques | el peso viene de **retener el contacto**, no de tener frames | son el eje ×12: +1 frame sale a +24 |

Un `walk` de 12 frames a 1 tick por frame son 12 ticks = 48 px de zancada a 24 fps:
fluido **y** cuadrado con la cadencia. Es la opción fluida y cuesta casi nada.

Referencia que ya tienes en el repositorio: los `metadata.json` de LPC declaran
`walk` 9 frames a 9 fps, `run` 8 a 12, `idle` 2 a 3, `combat_idle` 2 a 4. Es la
cadencia que llevas viendo.

### 3.3 El octágono del HUD

Los 8 lados del `OctagonalSkillSlot` corresponden a los 8 traits: 4 de BA, 4 de
AS. El efecto visual de activación **no** es uno por lado: es **uno por
categoría**, recoloreado por rampa.

| Categoría | Lados | Clip |
| --- | ---: | --- |
| BA · expansión | 2 | `octagon_expand` |
| BA · bifurcación | 2 | `octagon_branch` |
| AS · expansión | 2 | `octagon_expand` + rampa AS |
| AS · bifurcación | 2 | `octagon_branch` + rampa AS |

Dos clips de ~6 frames y dos rampas cubren los ocho lados. Va en `ui/`, §9.8.

---

## 4. Las 12 familias

Emparejamiento familia ↔ expresión y canales del arma: canon
([`statistics-and-combat.md`](../docs/systems/statistics-and-combat.md) §3 y
§4.3). La columna de lenguaje corporal es nota de producción: describe qué hace
que el conjunto valga su coste.

| Expresión | Familia | Fís./Elem. | Lenguaje corporal del conjunto |
| --- | --- | ---: | --- |
| Stunning | Mace | 1.15 / 0.85 | golpe corto y pesado, muñeca cargada, poca extensión |
| Stunning | **Orb** | **0.75 / 1.20** | sin golpe: sustentación, canalización, proyección |
| Bleeding | Sword | 1.10 / 0.90 | arco largo sostenido, filo que continúa el gesto |
| Bleeding | **Daggers** | **1.05 / 0.95** | dos manos independientes, toques rápidos, entrada corta |
| Poisoning | **Bow** | **0.85 / 1.15** | tensado, retención, suelta; el cuerpo se abre y se cierra |
| Poisoning | Darts | 0.80 / 1.20 | lanzamiento de muñeca, sin recuperación larga |
| Paralysis | **Whip** | **0.95 / 1.00** | carga en espiral, restallo, recogida de la cuerda |
| Paralysis | Gauntlets | 1.10 / 0.90 | puños, peso en caderas, guardia cerrada |
| Fracture | **Hammer** | **1.20 / 0.75** | alzado a dos manos, descenso vertical, inercia que arrastra |
| Fracture | Axe | 1.15 / 0.80 | corte diagonal, filo que se clava y se libera |
| Knockdown | **Spear** | **1.10 / 1.00** | estocada lineal, apoyo trasero, alcance |
| Knockdown | Staff | 0.85 / 1.15 | barrido y giro, ambos extremos activos |

En negrita, los perfiles canónicos de referencia. **Spear y Staff comparten
expresión física y no comparten ni un frame**: una estoca y el otro barre. Eso es
lo que el conjunto cerrado compra.

Nivel y rareza del arma **no** producen variante visual: es sub-rampa de paleta
sobre el mismo dibujo. El cambio de arma se resuelve desde inventario.

---

## 5. Los tres ejes de coste

### 5.1 Eje A — Deformante (por frame, el 72 % del proyecto)

Dos capas, no una: `body` y `garment`. 110 frames × 12 armas × 2 capas.

**Regla que protege el presupuesto:** exactamente **dos** capas deformantes. Una
tercera (sobrearmadura, capa) multiplica el bloque otra vez. Todo lo demás sube
al eje B.

**El cuerpo se dibuja completo debajo de la ropa**, incluso donde la vestimenta
siempre lo tapa. Es lo que permite que una armadura futura que descubra más piel
no obligue a redibujar 1.320 frames de cuerpo.

Y el reparto dentro de `garment`: **solo tela deformante** —torso, piernas,
mangas—. Casco, hombrera, brazal, botas y cinturón son **piezas rígidas** (eje B).
Así una armadura nueva es, casi siempre, piezas rígidas nuevas más una rampa: casi
gratis. Sólo una silueta genuinamente distinta pide una capa `garment` nueva.

### 5.2 Eje B — Rígido (por orientación)

Arma guardada, `misc` de linaje, piezas duras de armadura, herramienta de oficio,
accesorio de tocado, arma a escala macro. Se dibuja una vez por orientación
discreta (8–12) y se coloca por **tabla de anclajes** (`head`, `hand_main`,
`hand_off`, `hip`, `back`, `feet`) que cada frame publica.

Las orientaciones son **dibujadas, nunca rotadas por el motor**: `rotation` sobre
pixel art produce el borde sucio que la dirección visual prohíbe.

El atlas de orientaciones **no** aplica al arma de combate: ésa se autora con el
cuerpo, dentro del conjunto.

### 5.3 Eje C — No registrado (compartido, y crítico)

Manifestación elemental, aura de estado, destello de impacto, polvo, estela,
salpicadura. Propio nodo, propio anclaje, **compartido entre los 12 conjuntos, los
8 linajes y el bestiario sin adaptación**.

Con las decisiones de §1, este eje es **el único lugar donde viven las 6
afinidades y las 6 expresiones físicas**. Ya no es un extra de calidad: es la
representación entera de dos de los tres skill trees.

---

## 6. Grises indexados y rampas de paleta

### 6.1 La corrección: no es un filtro de hue

Un desplazamiento de matiz (`hue_rotate`) sobre una imagen en grises **no hace
nada**. Un gris tiene saturación 0, y rotar el matiz de un color sin saturación
devuelve el mismo gris. Montado como filtro de hue, el sistema produciría ocho
linajes idénticos y el fallo sería silencioso.

Lo que sí funciona —y es la técnica estándar para exactamente este problema— es
un **LUT de paleta**: el gris no es un color, es un **índice**, y el shader lo usa
para leer una rampa autorada.

Ventaja añadida sobre el hue: una rampa de verdad **desplaza el matiz a lo largo
de la escala** (sombras más frías, luces más cálidas), que es lo que distingue el
pixel art bueno del teñido plano. Un multiplicado de color da una rampa
monocroma; una rampa autorada da una rampa con vida.

### 6.2 Cómo se autora

La capa `garment` se dibuja en **exactamente 6 grises planos**, con el valor como
índice de sombra, y usa el canal verde como **zona de material**. Un `.tres`/PNG
de **6 × 8 px por linaje** cubre hasta ocho zonas.

Ocho y no cuatro porque la receta del generador LPC ya nombra cinco colores por
linaje —`primary`, `secondary`, `accent`, `skin`, `hair`— y el arma, el contorno y
el `misc` quieren zona propia. Ocho deja margen sin costar nada: la textura entera
son 48 píxeles.

```glsl
shader_type canvas_item;
render_mode unshaded;

uniform sampler2D ramp : filter_nearest, repeat_disable;
uniform int shades = 6;
uniform int zones  = 8;

void fragment() {
    vec4 src = texture(TEXTURE, UV);
    if (src.a < 0.5) { COLOR = vec4(0.0); }
    else {
        float s = floor(src.r * float(shades - 1) + 0.5);   // índice de sombra
        float z = floor(src.g * float(zones  - 1) + 0.5);   // zona de material
        COLOR = vec4(texture(ramp, vec2((s + 0.5) / float(shades),
                                        (z + 0.5) / float(zones))).rgb, 1.0);
    }
}
```

Detalles que no son opcionales:

- `filter_nearest` y `repeat_disable` en la rampa, o el LUT interpola y aparecen
  colores que nadie autoró.
- Umbral de alfa, no mezcla: el pixel art no tiene alfa parcial.
- Los grises deben caer en los centros de texel — con 6 sombras: `0, 51, 102,
  153, 204, 255`. Un gris intermedio cae en el índice equivocado.
- El contorno puede ser la entrada más oscura de la rampa, así que también cambia
  por linaje. Si se prefiere contorno constante, va en zona propia con la misma
  entrada en las ocho rampas.

### 6.3 Medido: dos correcciones que salieron del laboratorio

El POC de [`lab-grayscale-ramp/`](lab-grayscale-ramp/README.md) corrió el LUT sobre
las hojas LPC reales. El mecanismo funciona —round-trip con **2–3,6 % de error
medio**, visualmente indistinguible— y con `skin` y `hair` bloqueados los tres
linajes probados siguen separándose sólo por la prenda. Pero dos cosas cambian
respecto a lo escrito arriba:

**a) Los acentos van en zona propia.** Seis sombras bastan para los materiales
base, no para detalles de 1–3 píxeles: el error máximo (89–132 sobre 255) se
concentra ahí, porque comparten el rango de luminancia de una zona ancha y caen
lejos del centro de su bucket. La corrección es darles zona propia, no subir a 8
sombras — más barato, y el presupuesto de ocho zonas ya lo permite.

**b) Las ocho rampas hay que separarlas en luminosidad y saturación, no sólo en
matiz.** Medido: Caelith y Eirune están a **92°** de matiz en `primary` y separan
**menos** (delta 24,5) que Caelith y Kovari, que están a **5°** y separan **más**
(29,3). Caelith `#405370` y Eirune `#4F7752` son ambos oscuros y desaturados, así
que su delta RGB es pequeño pese al abismo de matiz.

> Dos linajes lejanísimos en matiz pero ambos oscuros y apagados se leen como «los
> dos oscuros y apagados». **La distancia de matiz no es la métrica.**

Eso es una restricción sobre el hito 0: las ocho rampas se diseñan como un
conjunto que se reparte el eje de luminosidad y el de saturación, no eligiendo ocho
matices bonitos por separado.

### 6.4 Lo que sale gratis del mismo shader

| Variante | Cómo | Coste |
| --- | --- | ---: |
| 8 linajes | 8 rampas | 8 texturas de 6 × 8 |
| 3 niveles de desgaste | rampa desaturada y oscurecida | 3 rampas |
| Niveles de material del arma | rampa por nivel | 1 por nivel |
| Bando enemigo / variante de especie | rampa | 1 |
| Estado envenenado, congelado, quemado | rampa temporal en el mismo uniforme | 1 cada uno |

**Una vestimenta, variantes infinitas, y ninguna cuesta un frame.** El instinto
era correcto; sólo el mecanismo tenía que cambiar.

Nada de esto es novedoso, y eso es tranquilizador: es **color indexado con
intercambio de paleta**, la técnica sobre la que funcionaron las eras de 8 y 16
bits enteras. Lo único moderno es hacer la búsqueda en un shader en vez de en la
paleta del hardware.

### 6.5 Hasta dónde se extrapola

La regla que decide si algo entra: **la rampa transporta identidad de material; el
dibujo transporta identidad de forma.** Donde la identidad de un linaje o una
variante es *de qué está hecho*, la rampa lo cubre. Donde es *qué forma tiene*, no.

| Sujeto | ¿Sirve? | Cómo queda |
| --- | --- | --- |
| **Estandarte / bandera / pendón** | **sí, el caso más limpio** | 1 tela dibujada + 8 símbolos (`brace`, `leaf`, `gear`, `split`, `compass`, `seal`, `nodes`, `pulse`) + 8 rampas = 8 estandartes. El símbolo es forma, así que se dibuja; la tela es material, así que se rampea |
| Kits de bioma de edificio | sí | techo, muro y remate como zonas; el color del linaje sale de la rampa y el kit se reduce a formas |
| Marco y chrome de UI por linaje | parcial | el canon ya separa lo que cambia: «paleta, bordes, esquinas, rellenos, sombras, patrones». La **paleta** es gratis con rampas; bordes y patrones son forma y se dibujan |
| Emblemas, sellos, iconos de recurso por nivel | sí | 1 dibujo + rampa por nivel |
| Variantes de especie del bestiario | sí | mismo arquetipo, rampa distinta |
| Estados (envenenado, quemado, congelado) | sí | rampa temporal en el mismo uniforme |
| Niveles de material del arma y desgaste | sí | ya en §6.4 |
| **Suelos de bioma** | **no, y es el límite** | la identidad de un bioma **es su textura**, no su color: `fibras y células` de Eirune contra `piedra y placas` de Ardhen no son el mismo tile recoloreado. Sí sirve **dentro** de un bioma: estación, humedad, desgaste |
| Degradados y color por posición | no | una rampa es por zona, no por píxel: no hay cielos ni arcoíris |
| Sujetos con más de 8 materiales | no | el presupuesto de zonas es el presupuesto |

**Escape hatch, y conviene saber que existe:** la zona 0 es *passthrough* — sus
píxeles salen con su color literal, sin rampa. Sirve para lo que debe ser idéntico
en los ocho linajes (contorno, blanco del ojo, un brillo especular) y para el
píxel que hay que elegir a mano. Es una salida, no un hábito: cada píxel que va a
la zona 0 es un píxel que deja de normalizarse.

### 6.6 Por qué esto vale más que los frames que ahorra

La observación es correcta y probablemente sea el beneficio principal: **la
estrategia no recomienda disciplina de paleta, la impone.**

Cada píxel tiene que caer en uno de 6 índices de sombra de una de 8 zonas. Un
píxel fuera de paleta **no existe**: o codifica a un par válido o no se dibuja. No
es una guía de la que se pueda derivar — es estructuralmente imposible romperla.

Cuatro consecuencias, en orden de importancia:

1. **La consistencia a lo largo de meses queda garantizada, no recordada.** Es la
   causa más común de que un proyecto largo de pixel art en solitario se vea
   irregular: lo dibujado en el mes ocho no casa con lo del mes uno. Aquí no puede
   no casar.
2. **La lógica de sombreado también se comparte.** El índice 0 siempre significa
   la sombra más profunda y el 5 la luz: una prenda dibujada después sombrea
   coherente con una dibujada antes, sin comparar nada.
3. **Un cambio global de dirección de arte cuesta 8 archivos.** ¿Todo el juego más
   cálido? Se editan 8 rampas, no 3.800 frames. Para alguien que va a cambiar de
   opinión —y va a cambiar de opinión— eso es lo más valioso de todo el esquema.
4. **Encaja con lo que el repositorio ya hace.** Es el mismo principio que
   `GroundAtlasProfile` (los datos declaran los roles, el arte declara las formas)
   y la versión pixel de «recolorear por tokens» de la guía de iconografía. La
   rampa es un token de diseño.

El precio: no se puede elegir a mano un color especial para un brillo concreto sin
gastar la zona 0. Es una restricción real y es la que produce el beneficio.

### 6.7 Quién colorea: Godot en ejecución, Pixelorama para ver

Las dos cosas, y por motivos distintos que no se pueden mezclar.

| Dónde | Qué hace | Por qué ahí |
| --- | --- | --- |
| **Godot, en ejecución** | aplica la rampa | es el punto entero del esquema. Si coloreara Pixelorama habría que exportar 8 PNG por sujeto y se pierde todo |
| **Pixelorama, al dibujar** | se dibuja en la **paleta de trabajo** | no se puede juzgar arte en casi-negros |
| **Script, para revisar** | previsualiza los 8 linajes de un `.pxo` exportado | ya existe: es `lab-grayscale-ramp/build-poc.js` reapuntado a la fuente propia en vez de a LPC |

Y la corrección a «dibujar casi en blanco y negro»: **no en grises literales, sino
en una paleta de trabajo donde el matiz sea la zona y el valor sea la sombra.**

La razón es concreta: en grises de verdad, un gris medio de la zona *tela* y un
gris medio de la zona *cuero* son **el mismo píxel** y no hay forma de distinguirlos
a ojo — ni al dibujar, ni al corregir un mes después. Con matiz = zona y valor =
sombra, las dos coordenadas se ven a la vez, y la disciplina de trabajar por
valores antes que por color se conserva igual.

El remapeo de esa paleta de trabajo a la codificación (`R` = sombra, `G` = zona) es
trabajo de `Export-Art.ps1`, no del ojo (§12.6).

### 6.8 Personalizador de fundador y variedad de migrantes

Sí, y es la aplicación con mejor relación valor/coste de todo el esquema: piel,
pelo y ojos como zonas propias significan que elegirlos **no toca un píxel**.

**El gancho ya existe en el dominio, y es determinista.** No hay que construirlo:

| Pieza | Dónde | Qué da |
| --- | --- | --- |
| `Citizen.AppearanceSeed` | `Domain/Citizen.cs` | entero por persona, ya en la entidad |
| `CityWorld.StableAppearanceSeed(name, lineage)` | FNV-1a | estable entre sesiones |
| `MigrantGenerator.ArrivalSeed(citySeed, arrivalTick, citizenId)` + `DeterministicRandom` | `Domain/MigrantGenerator.cs` | RNG determinista que ya elige linaje y género |

La variedad de migrantes es, por tanto, **derivar índices de rampa de una semilla
que ya se calcula**. Y eso importa más de lo que parece: la apariencia tiene que
salir idéntica tras progresión offline y recarga, así que **tiene que venir de la
semilla del dominio y no de un aleatorio en presentación**.

#### Cuatro cosas que hay que saber antes de diseñar la UI

**a) Las zonas no son escasas — corrijo lo dicho arriba.** Escribí «el presupuesto
de zonas es el presupuesto» como si ocho fuera un techo. Ocho fue una elección: el
canal `G` da 256, y pasar la textura de rampa de 6 × 8 a 6 × 16 la lleva de 48 a 96
píxeles. El coste real no es de píxeles, es **editorial**: alguien tiene que decidir
16 colores por preset en vez de 8, y son ocho presets de linaje más los del jugador.

**b) Hacen falta dos fuentes de rampa, no una.** Si la rampa del linaje fija la
piel, elegir tu piel pelea contra la identidad de tu linaje. El reparto que resuelve
el conflicto:

| Fuente | Zonas | Quién la elige |
| --- | --- | --- |
| **Rampa de linaje** | vestimenta, acento, símbolo, materiales | el linaje |
| **Rampa de persona** | piel, pelo, ojos | el jugador, o la semilla |

Dos texturas de rampa en el shader, o una con las filas repartidas por dueño.

**c) Eso fuerza una decisión de canon que no existe.** ¿El linaje determina el
fenotipo, o sólo la cultura material? El placeholder LPC ató `colors.skin` y
`colors.hair` al linaje, pero el pilar del proyecto es que el linaje **no** es
destino — es contexto cualitativo de aprendizaje, no esencia, y
[`citizens.md`](../docs/systems/citizens.md) le prohíbe explícitamente bloquear,
garantizar o poner techos.

Recomendación coherente con ese pilar: **el linaje colorea lo que la gente hace; la
persona colorea a la persona.** Pero es decisión de producto y hay que tomarla
antes de construir la UI del personalizador, no después.

**d) La variedad tiene que estar curada, no ser aleatoria.** Una rampa al azar por
migrante produce el aspecto de «NPC generado»: una multitud donde nada cuaja. Se
autoran N rampas de piel y M de pelo, y la semilla elige entre ellas.

Y la regla técnica que hace que cincuenta variantes parezcan del mismo artista:

> **Las rampas de una misma zona comparten la curva de sombra.** Sólo varían matiz y
> saturación; la progresión de luminancia es la misma.

Si una rampa de «piel clara» tuviera su propia curva de luminancia, rompería la
dirección de luz del proyecto y ese ciudadano se vería pegado. Compartiendo la
curva, cualquier variante sombrea coherente por construcción — es el mismo argumento
del §6.6 aplicado dentro de una zona.

#### Dos límites que no conviene prometer en la UI

**Vestimenta: el color es gratis, la silueta no.** «Rienda suelta a la vestimenta»
vale para la paleta, no para el corte. Una prenda con silueta nueva es una capa
deformante nueva: 110 frames por cada arma afectada. Lo que sí es barato es **muchos
atuendos con pocas siluetas**, cambiando piezas rígidas y rampa (§5.1). Es una
distinción que la UI debe respetar o promete algo que cuesta mil frames.

**Ojos: a esta escala son uno o dos píxeles.** Con una figura de ~56 px de alto, el
color de ojos es prácticamente invisible en el sprite de mundo. Tiene sentido como
zona **para el retrato o el splash**, no para el muñeco. Dar un selector de color de
ojos que no se ve en el juego es peor que no darlo.

### 6.9 El espacio de personalización

**Libertad dentro de un espacio de diseño deliberadamente acotado.** El linaje
establece el marco; la personalización define al individuo. No es un editor RGB.

Nombres canónicos, que difieren de los coloquiales:
`Caelith`, `Myrven`, `Ardhen`, `Orveth`, `Eirune`, `Kovari`, `Vaelun`, `Theryn`
(`LineageId`, `ProfileCatalog.Lineages`).

#### El reparto

| Identidad de linaje (marco) | Identidad individual (elección) |
| --- | --- |
| gama de cabello permitida (3–4) | peinado |
| rango de piel permitido (~4) | tonalidad dentro del rango |
| paleta cultural, símbolos, materiales | paleta de atuendo (3) |

Dos rampas, como estaban: **rampa de vestimenta** (ropa, acentos, símbolos,
materiales) y **rampa del individuo** (piel, pelo, ojos). Una tercera queda abierta
para el futuro y no hace falta ahora.

#### El presupuesto de datos, y el de arte

| Bloque | Cuenta | Coste |
| --- | ---: | --- |
| Rampas de cabello | 8 linajes × 4 | 32 filas de 6 px |
| Rampas de piel | 8 × 4 | 32 filas |
| Rampas de atuendo | conjunto global (~6); cada linaje **ofrece 3** | 6 filas |
| **Datos totales** | **~70 rampas** | despreciable |
| Peinados | 8 linajes × 2 géneros × **1** característico = 16, más **12 unisex** = **28** | ~280 sprites rígidos |

Lo que importa no es el tamaño de la biblioteca sino **cuántas opciones ve un jugador
concreto**: el de su linaje y género más los 12 unisex = **13 peinados**.

Y la métrica de variedad útil no es el total del mundo, es **si dos ciudadanos en
pantalla se parecen**. `4 cabello × 4 piel × 3 atuendo` = 48 combinaciones de paleta,
× 13 peinados = **624 aspectos por linaje**. En una ciudad de 50 habitantes repartidos
entre ocho linajes salen unas 130 parejas del mismo linaje, así que la expectativa es
**menos de un par idéntico por ciudad**. Sobra de largo.

#### Por qué 16 de linaje + 12 unisex y no al contrario

**La gama de cabello ya marca el linaje.** Un Ardhen tiene cuatro castaños y no puede
tener el pelo blanco; un Myrven va en pastel. Con el color cerrado por linaje, la
*forma* del peinado no tiene que cargar sola con la identidad — que es lo que pedía el
punto 6 del espec: «peinados independientes del linaje siempre que sea visualmente
viable».

Por eso el reparto favorece lo transversal: un característico por linaje y género
basta para anclar la silueta, y los doce unisex dan la elección. Sale más barato que
concentrarlo en el linaje **y** ofrece más opciones al jugador.

Si al dibujarlos resulta que la silueta sí aporta identidad que el color no da, subir
a dos por linaje es **añadir 8 peinados**, no rehacer nada.

#### El pelo se clasifica por rigidez, no por género — y de eso depende todo

Es el único número de este espec que puede hundir el proyecto, así que va primero:

| Si el pelo es… | Coste de **un** peinado | 28 peinados |
| --- | ---: | ---: |
| **rígido** (anclado a `head`, ~10 orientaciones) | ~10 sprites pequeños | **~280 sprites** |
| **deformante** (redibujado por frame) | 110 f × 12 armas = 1.320 | **36.960** |

Un factor de **130×**. Por tanto la clasificación que decide la viabilidad no es
masculino / femenino / unisex —que es cultural— sino:

- **Clase R, rígida:** corto, rapado, recogido, trenzado tirante, cubierto. Anclado
  a `head` con índice de orientación por frame; **un solo juego de orientaciones
  sirve a las doce armas**, porque el pelo sigue a la cabeza y la cabeza la da la
  tabla de anclajes. Aquí deben caer ~25 de los 28.
- **Clase D, deformante:** melena suelta que se mueve, coleta que oscila. No hay
  atajo: o se redibuja o se acepta que no se mueve. **Techo duro de 2–4 en total**,
  compartidos, y sólo con movimiento propio en los clips donde se note.

La biblioteca de 28 es perfectamente asequible. La biblioteca de 28 **con melenas
sueltas** no lo es. La división masculino/femenino/unisex puede convivir encima como
etiqueta de oferta, pero no es la que se presupuesta.

#### Coherencia por construcción, no por tabla de compatibilidad

El punto 4 del espec —reglas de compatibilidad entre cabello, piel y atuendo— tiene
una trampa: son 48 combinaciones por linaje, 384 en total, y una tabla de pares
crece al cuadrado y se queda obsoleta con la primera paleta nueva.

La alternativa es hacer el espacio **cerrado por construcción**:

1. Cada linaje declara una **familia de paleta**: un conjunto pequeño y cerrado de
   matices.
2. Todas sus rampas de pelo y piel salen de esa familia.
3. Todas las rampas de una zona **comparten la curva de sombra** (§6.8 d).

Con eso, *todas* las combinaciones de ese linaje son coherentes y no hay tabla que
mantener.

Queda un solo punto de fricción real: el atuendo es **independiente del linaje**
(punto 3), así que puede chocar con el pelo o la piel de un linaje concreto. Para eso
basta **una regla numérica, automatizable**, en vez de una matriz:

> **Suelo de contraste entre zonas adyacentes.** Pelo contra atuendo y piel contra
> atuendo deben superar un delta mínimo de luminancia. Por debajo, la combinación no
> se ofrece.

Es medible: el POC del laboratorio ya calcula deltas por canal entre zonas, así que
la misma métrica sirve de puerta. Una regla que una máquina comprueba envejece mucho
mejor que 384 juicios escritos a mano.

#### La colisión que hay que resolver: Myrven rompe la curva compartida

Los pasteles de Myrven **no caben en la curva de sombra compartida**. Pastel
significa valor alto y saturación baja *en todo el rango* — incluido el extremo
oscuro. Una curva que va de sombra profunda a luz, aplicada a un rosa pastel, deja
de ser pastel en cuanto entra en sombra.

Y Myrven es además el peor caso del suelo de contraste: pelo pastel claro contra piel
clara tiene un delta de luminancia bajo por definición.

Dos salidas:

| Opción | Consecuencia |
| --- | --- |
| **Una segunda curva «clara» declarada como dato**, compartida por quien la necesite | recomendada: es una excepción explícita y auditable, no libertad por linaje |
| Mantener una sola curva | los pasteles de Myrven quedan a valor medio, no pastel de verdad |

Lo que **no** debe hacerse es dar curva propia a cada linaje: ahí se pierde la
coherencia que el §6.6 compra, y con ella la razón entera del esquema.

Por eso Myrven es el sujeto de prueba correcto para el hito 0: valida la curva
alternativa **y** el suelo de contraste a la vez.

#### Persistir el índice, nunca el color

El punto 8 —añadir paletas sin rehacer nada— tiene una condición que es fácil de
incumplir y caro de descubrir tarde:

> Un `Citizen` guarda **el índice de su rampa**, no su RGB. Y los índices son
> **append-only: nunca se reordenan**.

Si se guarda el color, revisar una paleta deja a los personajes viejos en colores
retirados. Si se guarda el índice y alguien reordena la lista, los personajes
**cambian de aspecto en silencio**.

Es exactamente la lección que este repositorio ya pagó con los ids de tile: «una
receta que nombra tiles por id empezaría a apuntar a otro arte en silencio, y nada en
el archivo lo diría» (`art-pipeline.md` §4.1). La defensa allí fue el hash y el
lockfile; aquí basta la disciplina de append-only, más el mismo tipo de validación
automática.

#### Dónde vive esto

Las paletas cerradas por linaje son **datos de juego, no arte**: van junto a
`LineageDefinition` / `ProfileCatalog.Lineages`, no en un `.pxo`. Y la elección de un
`Citizen` va en su perfil, derivada de `AppearanceSeed` cuando no la elige el jugador
(§6.8), para que la apariencia sobreviva idéntica a la progresión offline.

---

## 7. Cómo se dibuja para que no quede cutre

1. **Una sola dirección de luz para todo el proyecto.** Fija, alta, ligeramente
   frontal. Es la condición para que una hombrera dibujada una vez encaje en 110
   frames sin repintar sombras.
2. **La guardia se dibuja primero y mejor que nada.** Es entrada y salida de
   quince clips y la primera lectura del arma. Un conjunto se juzga por su guardia.
3. **La identidad de linaje es forma y rampa, nunca tono aplicado a ciegas.** La
   rampa da el color; la capa `misc` da la forma. La tabla §Identidad resumida de
   `visual-language.md` es vocabulario de **formas** (placas, fibras, remaches,
   pliegues, retículas), no de colores.
4. **El presupuesto se gasta en el contacto.** En un golpe de 6 frames: 1
   anticipación, **2 de contacto** con smear y manifestación, 1 de continuación, 2
   de recuperación que vuelven a la guardia. Un golpe se hace pesado **reteniendo
   el frame de contacto** — el tempo es dato en el `SpriteFrames`, no dibujo. Es
   también lo que distingue Mace de Sword sin frames extra.
5. **El smear y la manifestación venden el golpe, y están en el eje barato.** Un
   conjunto de 110 frames se ve rico o pobre según la librería de efectos, no
   según tener 130 frames.
6. **La línea base no se mueve entre frames.** Regla del pipeline (§Anclaje); con
   capas es además la condición de registro.
7. **Las capas se autoran en el mismo `.pxo` que la pose**, nunca a ciegas.

---

## 8. La política de bifurcación

> **Un conjunto no hereda dibujo: hereda contrato.** Bifurcar es declarar que un
> clip del contrato no aplica, o que hace falta uno que el contrato no tiene.

```text
art/source/characters/whip_extra_recoil.pxo
art/source/characters/whip.set.json   ← { "extra": ["extra_recoil"], "omit": [] }
```

| Bifurcación legítima | Cuándo | Coste |
| --- | --- | --- |
| Clip **extra** del arma | el lenguaje corporal lo exige — recogida de cuerda del látigo, sustentación del orbe | 3–6 f |
| Clip **omitido** | el arma no lo puede hacer — un orbe cuyo `combat_retreat` no difiere de su guardia | ahorro |
| Presupuesto redistribuido | `basic_a` de 8 y `basic_b` de 3 en vez de 6 y 5, mismo total | 0 |
| `knockback` / `defeated` propios | un arma cuya derrota merece firma | 11 f |
| Armadura nueva: piezas duras + rampa | lo normal | ~40 f |
| Armadura nueva: silueta distinta | la tela cambia de verdad | 110 f × armas afectadas |
| Segunda complexión corporal | **sólo tras cerrar la primera completa** | copia + reedición |

Lo que **no** es bifurcación legítima: renombrar un clip del contrato, mover la
línea base, o cambiar el número de nodos de bifurcación. Eso no enriquece el
arma, rompe el motor.

**La complexión corporal se bifurca cuando la primera está completa.** Dos
cuerpos a medias cuestan más que uno terminado y su copia, porque cada corrección
se pagaría 1.320 veces.

---

## 9. Inventario de proyectos

Rutas y nombres según `art-pipeline.md` §6.2: fuente
`art/source/<categoría>/<sujeto>_<estado>.pxo`, un sujeto por archivo, sin
gutter, lienzo múltiplo entero de la rejilla de 32.

### 9.1 `characters/` — los 12 Weapon Animation Sets, 64 × 64

16 estados nominales por conjunto (los `expand`/`branch` cuentan 4 + 4).

| Sujeto | Archivos | Frames `body` | Frames `garment` |
| --- | ---: | ---: | ---: |
| `sword`, `daggers`, `mace`, `axe`, `hammer`, `spear`, `staff`, `gauntlets`, `bow`, `darts` | 22 c/u | 110 c/u | 110 c/u |
| `whip` (+ `extra_recoil`) | 23 | ~115 | ~115 |
| `orb` (`retreat` posiblemente omitido) | 21 | ~105 | ~105 |
| **Total** | **~265** | **~1.320** | **~1.320** |

Capas por archivo: `body`, `garment` (grises indexados), `weapon`, `hair`, grupo
`anchors`.

### 9.2 `characters/` — el ciudadano compartido, 64 × 64

Un solo sujeto, `citizen`, que sirve a las tres escalas y a las doce armas. **No
hay conjunto de viajero aparte y no hay habitante de macro aparte.**

| Estados | Direcciones dibujadas | Frames `body` | × 2 capas |
| --- | --- | ---: | ---: |
| `idle` 2, `walk` 6, `run` 6 | 3 (abajo, arriba, lado; el lado se espeja) | 42 | 84 |
| `carry` 4 | 3 | 12 | 24 |
| `climb` 5 | 1 (lado) | 5 | 10 |
| 7 verbos de trabajo × 4 f | 2 | 56 | 112 |
| `knockback` 5, `defeated` 6 — **sin arma** | 1 (lado) | 11 | 22 |
| **Total** | | **126** | **252** |

Tres direcciones dibujadas cubren las cuatro que el macro necesita, y **la
dirección lateral es exactamente la que la expedición usa**. Eso es lo que la
unificación del lienzo (§1.4) compra: la locomoción se dibuja una vez y sirve a la
ciudad, a la escena de edificio y al viaje de expedición.

Sobre esa base, lo que las doce armas comparten es todo este bloque, más el
`ready` de cada una (4 f) como puente hacia su guardia. El arma en viaje es una
**pieza rígida** anclada a `back` o `hip` (§9.3), no locomoción redibujada.

Los verbos de trabajo van en **dos** direcciones: el trabajo ocurre en un puesto de
orientación fija, y una figura de frente o de espaldas cubre lo que la escena
necesita («la lectura la da la silueta, no el rasgo»). Los 7 verbos mapean las 12
familias profesionales de [`citizens.md`](../docs/systems/citizens.md) por gesto, no
por nombre; el oficio concreto lo dice su marca rígida.

### 9.3 `items/` — piezas rígidas

| Sujeto | Contenido | Frames |
| --- | --- | ---: |
| `weapon_stowed` | 12 familias × 3 orientaciones (espalda, cadera, mano baja) | 36 |
| `lineage_misc` | 8 linajes × 10 orientaciones | 80 |
| `hair_rigid` | ~25 peinados de clase R × 10 orientaciones (§6.9) | ~250 |
| `hair_deforming` | 2–4 melenas de clase D, **techo duro**, capa deformante | ver §6.9 |
| `armor_hard` | casco, hombrera, brazal, botas × 10 orientaciones | 40 |
| `weapon_macro` | 12 familias a escala macro + icono de inventario | 24 |
| `tool_orientations` | 7 herramientas de verbo × 8 | 56 |
| `profession_marks` | 12 accesorios de oficio | 12 |
| `wear_masks` | roce y desgarro, 4 zonas × 2 niveles | 8 |
| **Total** | | **256** |

Bioma y desgaste **no** producen frames: bioma es rampa más pieza `misc`;
desgaste es rampa oscurecida más máscara en zonas fijas.

### 9.4 `effects/` — la librería compartida

**El proyecto de mayor apalancamiento, y ahora obligatorio**: es donde viven
enteros el Physical Expression Skill Tree y el Elemental Affinity Skill Tree.

| Sujeto | Contenido | Frames |
| --- | --- | ---: |
| `manifest_<afinidad>` | 6 afinidades × 6 trayectorias × 6 f | 216 |
| `status_<estado>` | 6 expresiones físicas como aura × 5 f | 30 |
| `impact_physical` | 6 trayectorias × 6 f | 36 |
| `ambient` | polvo, salpicadura, destello de evasión, recogida | ~30 |
| **Total** | | **~312** |

Seis trayectorias, no doce: una manifestación que no sigue el arma se ve pegada
encima, pero no hace falta una por familia.

> **Estas seis no son animaciones de cuerpo.** Son la **forma del camino que recorre
> la manifestación**, y viven en `effects/`. Los clips de cuerpo son sólo los del
> contrato del §3 (`basic_a/b/c`, `active`, `*_expand`, `*_branch`, `dodge`, `evade`,
> `hurt_*`, `combat_*`). Se llamaban `sweep` / `thrust` / `overhead` / `launch` /
> `burst` / `lash` y se renombraron precisamente porque **`thrust`, `slash` y
> `shoot` son nombres de poses LPC** y la colisión de vocabulario invitaba a
> confundir una cosa con la otra.

| Trayectoria | Familias |
| --- | --- |
| `arc` arco horizontal | Sword, Mace, Axe, Staff, Gauntlets |
| `line` recta que avanza | Spear, Daggers, Sword |
| `fall` descenso vertical | Hammer, Axe |
| `flight` proyectil que viaja | Bow, Darts, Orb |
| `bloom` radial desde el cuerpo | Orb, Gauntlets |
| `coil` curva que restalla | Whip |

La gramática visual de cada afinidad está fijada en
[`elemental-affinities.md`](../docs/systems/elemental-affinities.md)
§Presentación y es **identificable sin color** (Earth: estratos y fracturas;
Water: ondas y deformación fluida; Fire: pulsos y bordes inestables; Air: líneas
de flujo y estelas; Aether: nodos y ecos; Silence: interrupción y bordes limpios)
— justamente lo que permite compartir el clip entre rampas.

Las seis expresiones físicas son aura sobre postura existente, no clip de cuerpo:

| Expresión | Postura | Aura |
| --- | --- | --- |
| `Stunning` | `hurt_heavy` retenido | interrupción, ventana elemental abierta |
| `Knockdown` | `knockback` | polvo; la única que desplaza de verdad |
| `Paralysis` | frame retenido de `combat_advance` | marcha interrumpida |
| `Bleeding` | `combat_idle` | goteo acumulativo por stacks |
| `Poisoning` | `combat_idle` + rampa desviada | aura persistente, no acumula |
| `Fracture` | `hurt_heavy` | destello de ventana física |

Que `Paralysis` sea un frame de avance retenido no es atajo: es lo que el canon
describe («frena mucho el desplazamiento») y se lee mejor que un clip propio,
porque el jugador reconoce la marcha cortada.

### 9.5 `creatures/` — bestiario

Contrato **recortado**: una criatura no tiene Weapon Skill Tree, ni traits, ni
oficio.

| Arquetipo | Clips | Frames |
| --- | --- | ---: |
| `biped`, `quadruped`, `serpentine`, `insectoid`, `flyer`, `amorphous` | `idle` 4, `move` 6, `attack_a` 6, `attack_b` 6, `hurt` 3, `stagger` 4, `knockback` 5, `defeated` 5 | 39 c/u |
| **Total** | | **234** |

Las especies son **silueta más rampa** sobre su arquetipo, no proyectos nuevos.
Estados e impactos vienen de §9.4 sin adaptación, porque el contrato usa los
mismos nombres de clip.

El bestiario está trazado en
[#43](https://github.com/3FE3LE/world-of-goses/issues/43): **no autorar criaturas
concretas antes de que ese issue fije qué existe.** Los seis arquetipos sí — son
vocabulario corporal, no especies.

### 9.6 `buildings/` — 96 × 96 y 192 × 96

Un archivo por tipo, **fases como frames**:

| Sujeto | `BuildingKind` | Huella | Fases |
| --- | --- | --- | ---: |
| `home` | `Home` | 96 × 96 | 3 |
| `farm` | `Farm` | 192 × 96 | 3 |
| `quarry` | `Quarry` | 96 × 96 | 3 |
| `smithy` | `Smithy` | 96 × 96 | 3 |
| `town_hall` | `TownHall` | 192 × 96 | 3 |
| `potion_lab` | `PotionLab` | 96 × 96 | 3 |
| `cultivation_site` | `CultivationSite` | 96 × 96 | 2 |
| **Total** | | | **20** |

**96 × 96 para una planta, y el ancho sale de la huella.** El alto sólo se separa de
la huella cuando el sujeto es más alto —dos plantas— o cuando el tejado necesita
sitio. El `96 × 128` que este documento predijo antes salía de suponer que el plano
del tejado se abre mucho en las filas cercanas; **con un solo row de parcelas no hay
filas cercanas y la suposición no aplica.** Queda como medición (§13 E1), no como
número.

#### Frames de perspectiva, no de animación

Un edificio no se anima: **tiene una serie de frames por profundidad**. El código
reescala por fila (`HorizontalScale`, `ProjectedRowScreenY`), pero un reescalado
uniforme no cambia el **escorzo del tejado**. La cámara mira hacia abajo, así que
cuanto más cerca está un edificio, más se abre su plano superior:

```text
  FILA LEJANA            FILA MEDIA             FILA CERCANA
  (arriba en pantalla)                          (abajo en pantalla)

      ▁▁▁▁▁                 ╱▔▔▔▔╲                ╱▔▔▔▔▔▔╲
     ┌─────┐              ╱ tejado ╲            ╱  tejado  ╲     ← el plano
     │     │             ┌──────────┐          ╱   abierto   ╲      crece
     │fach.│             │  fachada │         ┌───────────────┐
     └─────┘             └──────────┘         │    fachada    │
                                              └───────────────┘
  tejado casi de canto   tejado a medias      tejado casi de frente
```

Escalar el frame de la fila lejana no produce el de la cercana: en uno el tejado
mide dos píxeles de alto y en el otro treinta. **Es información que no está en el
sprite lejano**, así que ninguna transformación la puede inventar.

Lo que sí escala bien es la fachada: cambia de tamaño, no de ángulo. De ahí el
reparto.

El reparto es el mismo eje A/eje B aplicado a arquitectura:

| Parte | Cuántas veces se dibuja | Quién la transforma |
| --- | --- | --- |
| **Fachada** | una vez por (edificio, fase) | el código, con la escala de la fila |
| **Plano de tejado** | una vez por (edificio, fase con tejado, **fila de descanso**) | autorado ya escorzado; el código sólo le aplica la escala |

El número de filas acota todo, y **lo acota la ventana de cámara, no el mundo**: un
mundo de veinte filas sigue mostrando un edificio en unas cuatro profundidades de
descanso. Hoy `MacroViewConstants.DefaultWorldParcelRows = 2` y el canon describe una
ventana de ~4 filas, así que la serie son **3 o 4 frames**, a medir contra el renderer
en el hito 3.

El cambio de frame ocurre durante la transición de calle, que ya es «una animación
breve y cuantizada (varios pasos, no un tween continuo)» —
`MacroViewConstants.TransitionSteps = 10` —, así que un intercambio de frame a mitad
de transición encaja en la gramática en vez de delatarse.

#### El eje es lateral, no de profundidad — medido

Escribí que la serie de frames iba por fila de profundidad. **Es al revés**, y la
proyección lo demuestra. Medido de `StreetDepthProjection` (`LotUnitPx` 96,
`ForeshorteningRatio` 58/90, `VerticalDepthFactor` 0,90, `HorizontalDepthFactor`
0,88, `HorizonY` 200, `BaseY` 580, `CenterX` 640):

| Profundidad | Y en pantalla | Separación de fila | Ancho del lote | Compresión | Borde lateral |
| ---: | ---: | ---: | ---: | ---: | ---: |
| −2 | 710,6 | 68,74 | 124,0 | 0,716 | 83,82° |
| −1 | 641,9 | 61,87 | 109,1 | 0,644 | 83,96° |
| **0** | **580,0** | **55,68** | **96,0** | **0,580** | **84,09°** |
| 1 | 524,3 | 50,11 | 84,5 | 0,522 | 84,22° |
| 2 | 474,2 | 45,10 | 74,3 | 0,470 | 84,35° |
| 3 | 429,1 | 40,59 | 65,4 | 0,423 | 84,48° |

#### La inclinación del canto lateral, y por qué **no** se dibuja

La regla, en una frase: **sobre un lote de fondo, un canto que corre en profundidad se
corre horizontalmente el 12 % de su distancia al centro de pantalla mientras baja
55,68 px.** De ahí:

```text
ángulo desde la vertical = atan(0,12 · x / 55,68)      x = px desde el centro
```

| x | ángulo | | x | ángulo |
| ---: | ---: | --- | ---: | ---: |
| 16 | 2,0° | | 144 | **17,2°** ← borde de una fila de 3 parcelas |
| 48 | **5,9°** ← borde de la parcela central | | 213 | 24,7° ← borde de pantalla a zoom 3 |
| 96 | 11,7° | | 320 | 34,6° ← borde de pantalla a zoom 2 |
| 128 | 15,4° | | 640 | 54,1° ← borde de pantalla a zoom 1 |

El ángulo depende **sólo de `x`**, no del zoom: un zoom uniforme escala `Δx` y `Δy`
igual. Lo que el zoom cambia es **cuánto `x` cabe en pantalla**, y por eso a zoom 2 se
ven cantos de más de 30° en el borde: no es que la deformación crezca, es que se ve más
lejos del centro.

La inclinación cae ligeramente con la profundidad, factor `(0,88/0,90)^d` — **menos de
medio grado por fila**, así que a efectos de dibujo es constante.

**Y aun así no se dibuja ninguna variante lateral.** El argumento que lo cierra no es
de coste, es de límite: para que un muro lateral se vea hay que hacerle sitio en el
lienzo, y eso **desplaza la puerta**. La puerta es un **ancla de interacción**, no
decoración: si se mueve por variante, o el ancla queda mal o hay que moverla también, y
entonces un punto del dominio pasa a depender del arte. Eso es exactamente lo que la
arquitectura prohíbe.

Así que: **una fachada frontal, y el límite de la construcción es su huella** — 96 de
ancho para un lote estándar, sin reserva para el costado. Los ángulos de la tabla
quedan como referencia para que las líneas de un tejado o de una rampa **casen con el
terreno**, que es para lo que sirven, no para ladear el edificio.

Lo que sí resuelve los casos malos es **política de cámara, no arte**: si la cámara
mantiene centrada la fila enfocada, el `x` máximo es la propia media anchura de la fila
—144 px, 17,2°— y nunca el borde de pantalla. Gratis.

#### Los tres estados de tejado

Decidido, y encaja con los cortes que el código ya tiene
(`NearClipDepth = −3`, `FarClipDepth = 11`):

| Estado | Profundidades relativas | Tejado |
| --- | --- | --- |
| `roof_near` | −3 … −1 (las franjas **delante** de la enfocada) | el más abierto |
| `roof_mid` | 0, 1, 2 (la enfocada y las dos siguientes) | punto medio |
| `roof_far` | 3 … 11 | de canto |

Tres planos por edificio y por fase con tejado. Conteo: **20 fachadas + hasta 42 planos
de tejado** (7 edificios × 2 fases con tejado × 3 estados; `cultivation_site` no lleva
tejado, así que en la práctica son menos).

#### Entonces el eje que sí se dibuja es la profundidad

Y sobrevive a la objeción de la puerta: **la profundidad cambia la relación
tejado/fachada sin mover la puerta de sitio.** El ancla horizontal es la misma en todos
los frames de la serie.

Con un solo row de parcelas la cámara **sí** se mueve en profundidad —la navegación
escalonada por calles existe—, así que ese row se ve desde varias profundidades
relativas y su tejado se abre y se cierra. El número de frames es cuántas
profundidades de descanso puede ocupar, y es lo que hay que medir (§13 E2).

Los dos números para que el tejado case con el terreno: la **compresión del suelo en la
fila enfocada es 0,580** —un lote de 96 de fondo se dibuja como banda de 55,68 px— y
cada fila hacia el horizonte multiplica la separación por 0,90 y el ancho por 0,88.

**El cubo 3D con caras de textura pixel art es exactamente el 2.5D que no quieres**: el
canon lo prohíbe por nombre — «2D puro (sprites/tiles reescalados por código, **no un
motor 3D ni geometría extruida**)».

Y **Octopath Traveler II no es una referencia alcanzable aquí**: su HD-2D son sprites
2D dentro de una escena **genuinamente 3D** —dioramas con geometría, cámara inclinada y
desenfoque tilt-shift—. Es decir, la parte que produce esa sensación de volumen es
precisamente la que este proyecto descartó. No hay versión 2D pura de Octopath; hay
otra dirección, que es la que el macro ya tiene.

#### Conteo, con un solo row de 3 parcelas

Con **una** fila de parcelas no hay serie de profundidad: el edificio descansa a una
sola profundidad y necesita **un** plano de tejado.

| Bloque | Cálculo | Canvases |
| --- | --- | ---: |
| Fachadas con su tejado | 7 edificios × fases (3, salvo `cultivation_site` con 2) | 20 |
| Variantes laterales de inclinación | **a medir** (§13 E2): cuántos desplazamientos cuantizados hay | 0 al arrancar |
| **Total de arranque** | | **20** |

El código de hoy declara `DefaultWorldParcelColumns = 5` y
`DefaultWorldParcelRows = 2`; el arranque que quieres es **3 × 1**.

Los cinco primeros son el arranque; los dos siguientes completan lo que
`BuildingKind` declara y llevan el conteo a 7. **Para llegar a 9 falta que el
dominio tenga dos tipos más**: el enum tiene `Quarry, Farm, Smithy, PotionLab,
Home, Forest, TownHall, CultivationSite`, y `Forest` es terreno.

`biome_kit_<linaje>`: 8 × (techo, muro, remate, pieza distintiva) = **32**. Los
edificios también pueden usar el shader de rampa (§6), lo que reduce el kit a
formas y deja el color al linaje.

Empieza con **una fachada con su tejado, sin variantes**, y mide contra el renderer
cuánto se inclina el canto en el desplazamiento lateral máximo de una fila de tres
parcelas. Si a esa distancia del centro la inclinación no se lee, no hay variantes que
dibujar y el bloque se queda en 20.

### 9.7 `terrain/`, `environments/`, `emblems/`

| Sujeto | Estado |
| --- | --- |
| `eirune_ground` | **existe** (`.pxo`, hoja, `.tiles.json`) — referencia del formato |
| `<linaje>_ground` × 7 | pendiente; un `GroundAtlasProfile` por bioma |
| `<linaje>_props` × 8 | pendiente; **tileset propio en el mismo archivo** con `tile_size` mayor (pipeline §4.3) |
| `environments/<bioma>` | 3 capas de parallax × 8 biomas; **un solo bioma en el primer hito** |
| `emblems/` | existen `wog_emblem`, `kovari`, `vaelun`; faltan 6 linajes × (32 y 64) |
| `emblems/banner` | **1 tela** en grises indexados + los 8 símbolos + 8 rampas = 8 estandartes. El símbolo es forma y se dibuja; la tela es material y se rampea (§6.5) |

Dos trampas ya pagadas: el número de columnas es parte de la identidad del atlas
(cambiarlo renumera todo), y un tile de relleno debe **tilear consigo mismo** — la
discontinuidad medida entre bordes opuestos debe quedar cerca de 0, no de 18–61.

### 9.8 `ui/`

| Sujeto | Contenido | Frames |
| --- | --- | ---: |
| `octagon_expand` | activación de lado, categoría expansión | 6 |
| `octagon_branch` | activación de lado, categoría bifurcación | 6 |

Dos rampas (BA / AS) cubren los ocho lados del `OctagonalSkillSlot`.

---

## 10. Flujo de trabajo

### 10.1 Vertical, no horizontal

**Una arma completa de punta a punta antes de tocar la segunda.** No lo básico de
todas.

La razón no es preferencia, es dónde está el riesgo. Hacer «lo básico de todas»
—doce guardias, doce `basic_a`— produce doce medias verdades y **no enseña nada
verificable**: no se puede meter en el motor, no se puede probar la composición de
traits, no se puede medir si el LUT de rampa funciona, y cuando aparezca el primer
fallo de contrato hay que corregirlo doce veces. Un conjunto entero, en cambio,
recorre el pipeline completo —autorado, export por capas, rampa, anclajes,
`SpriteFrames`, composición de cadena, FX— y **cada fallo se paga una vez**.

Y hay un argumento de aprendizaje: el segundo conjunto se dibuja mucho mejor que el
primero, y el duodécimo mucho mejor que el segundo. Ese aprendizaje sólo ocurre
terminando, no empezando doce veces.

### 10.2 Qué es transversal y qué se paga doce veces

| Bloque | Se dibuja | Frames |
| --- | --- | ---: |
| Locomoción y verbos de trabajo (§9.2) | **una vez** | 252 |
| `knockback` / `defeated` | **una vez** (dentro de los 252) | — |
| Piezas rígidas: arma guardada, `misc` de linaje, armadura dura, macro (§9.3) | **una vez** | 256 |
| Librería de efectos (§9.4) | **una vez** | 312 |
| Rampas de paleta | **una vez** | 0 (datos) |
| **Transversal, total** | | **820** |
| Conjunto de arma: `body` 110 + `garment` 110 | **× 12** | 220 c/u |
| **Por arma, total** | | **2.640** |

Reparto del proyecto de personaje: **24 % transversal, 76 % por arma**. Ésa es la
cifra que decide el flujo — no hay un eje transversal grande esperando a ser
explotado, así que la única palanca real es que el contrato de la primera arma sea
correcto.

### 10.3 Lo que cuesta la primera arma frente a la duodécima

La primera arrastra el mínimo transversal que hace falta para verla funcionando; la
duodécima no arrastra nada.

| Concepto | Primera arma | Duodécima |
| --- | ---: | ---: |
| Locomoción lateral mínima (`idle` 2, `walk` 6, `run` 6, `climb` 5, `carry` 4, `knockback` 5, `defeated` 6 = 34) × 2 capas | 68 | 0 |
| Conjunto del arma (`body` 110 + `garment` 110) | 220 | 220 |
| Piezas rígidas mínimas (arma guardada 3, `misc` de un linaje 10) | 13 | 0 |
| FX mínimos (2 trayectorias × manifestación 6 + impacto 6) | 24 | 0–12 |
| **Total** | **325** | **220–232** |

El sobrecoste de arrancar es **~105 frames, un 32 % de la primera arma**. No hace
falta la locomoción en cuatro direcciones ni los verbos de trabajo para validar el
combate: eso entra en el hito 5, con la ciudad.

### 10.4 Los archivos de una arma, de inicio a fin

Para `sword`, medido contra el contrato del §3 y la convención
`<sujeto>_<estado>.pxo`:

| Estado | Archivos |
| --- | ---: |
| `combat_idle`, `ready`, `combat_advance`, `combat_retreat` | 4 |
| `basic_a`, `basic_b`, `basic_c` | 3 |
| `ba_expand_1`, `ba_expand_2`, `ba_branch_1`, `ba_branch_2` | 4 |
| `active`, `as_expand_1`, `as_expand_2`, `as_branch_1`, `as_branch_2` | 5 |
| `dodge`, `evade`, `hurt_light`, `hurt_heavy` | 4 |
| **Total** | **20 `.pxo`** |

Cada uno con las capas `body`, `garment`, `weapon`, `hair` y el grupo `anchors`
dentro, así que **20 archivos contienen 110 frames × 4 capas**. `knockback`,
`defeated` y la locomoción **no** están aquí: son del sujeto compartido.

## 11. Orden de producción

| Hito | Qué | Por qué |
| --- | --- | --- |
| **0** | Curva de sombra compartida + la alternativa «clara»; familias de paleta de los 8 linajes (~70 rampas, §6.9); dirección de luz; paleta de trabajo del `garment` y su remapeo. **Validado con Myrven**, que estresa la curva alternativa y el suelo de contraste a la vez, regenerando el conjunto LPC en grises (§12.4) | Todo lo demás las asume. Cambiarlas después repinta el proyecto. Y el LUT del §6 hay que verlo sobre volumen real de arte antes de dibujar 1.320 frames en grises, no sobre un sprite de prueba. |
| **1** | **`sword` completo** (110 `body` + 110 `garment`) + `traveller` + `ready` + `manifest_fire` + 1 `misc` de linaje + tabla de anclajes | El corte vertical. Valida contrato, registro de dos capas deformantes, LUT de rampa, composición de traits y FX **en el motor** a ~280 frames de coste, no a 2.900. Espada porque es la familia convencional: si el contrato falla aquí, falla por el contrato y no por el arma. |
| **2** | **`daggers`** | Cierra **Bleeding**: primera expresión física jugable de punta a punta, el onboarding puede ofrecer una elección real (§12.3). Y prueba que dos familias de la misma expresión se leen distintas. |
| **3** | 5 edificios × fases + 1 kit de bioma; validar desplazamiento lateral | Responde la pregunta abierta con lo mínimo y decide si hace falta la variante a tres cuartos. |
| **4** | **`whip` + `gauntlets`** | **Paralysis** completa, y el peor caso del contrato: un arma flexible y unos puños sin arma visible. Si el template aguanta esto, aguanta las ocho restantes. Adelantado a propósito. |
| **5** | `dweller` macro + 12 marcas de oficio + máscaras de desgaste | La escala que el jugador mira el 90 % del tiempo. |
| **6** | Librería de efectos completa (§9.4) | Sube el techo de todo lo dibujado sin tocarlo, y es la representación entera de dos skill trees. |
| **7** | Las 8 familias restantes, por pares de expresión | Repetición mecánica: el contrato ya sobrevivió a espada, daga, látigo y guantelete. |
| **8** | 6 arquetipos de bestiario | Heredan contrato y FX. |
| **9** | Biomas: 7 suelos, 8 props, 8 kits, 6 emblemas; `environments/` restantes | Ancho, no profundo. Paralelizable. |

Los hitos **0, 1, 2 y 4 no se reordenan**. El 0 porque el shader de rampa
condiciona cómo se autora cada frame; los otros tres porque son la validación del
contrato, y su valor entero está en ocurrir antes del hito 7.

El agrupamiento por pares de expresión no es estético: el arma inicial se elige
«entre las dos familias naturales» de la expresión física del `Citizen` (canon
§3), así que una expresión a medias es una elección de onboarding a medias.

---

## 12. Lo que falta decidir, y lo que la sustitución implica

### 12.1 Resuelto — estructura de traits

2 expansiones + 2 bifurcaciones por tree, **cada bifurcación en su propio nodo de
rama** (§3.1). 22 frames por tree, 16 cadenas distintas.

Queda una pregunta acoplada: **¿una bifurcación puede cambiar la trayectoria?**
Si convierte un `arc` en un `fall`, cambia el clip de manifestación asociado
y toca la tabla de §9.4. Conviene resolverlo antes del hito 7, no antes del 1.

### 12.2 Resuelto — `knockback` y `defeated` compartidos

Compartidos entre las doce armas y **sin arma en mano**. Ahorra **121 frames** por
capa (242 con `garment`) y elimina la necesidad de anclar el arma en esos clips.
Reversible por arma como bifurcación (§8) si alguna derrota merece firma propia.

`hurt_light` y `hurt_heavy` siguen por arma: ahí la guardia todavía existe y es
donde el arma se reconoce.

### 12.3 Producto — la consecuencia en onboarding

Con conjuntos cerrados, **el juego sólo puede equipar las armas dibujadas**. El
arma inicial se elige entre las dos familias naturales de la expresión física, y
la expresión se decide en onboarding: hasta el hito 7 no existen las doce.

Recomendación: **restringir la oferta de onboarding** a las expresiones cuyo par
esté completo (Bleeding tras el hito 2; Bleeding y Paralysis tras el hito 4). La
alternativa —un conjunto `unarmed` de reserva— cuesta un conjunto entero y
contradice el principio. Elegir antes del hito 2.

### 12.4 LPC: laboratorio primero, retirada después

LPC **se descarta**, sin discusión. Pero antes de descartarlo sirve para lo único
que no se puede validar dibujando: comprobar el LUT de rampa sobre volumen real de
arte, en vez de sobre un sprite de prueba.

#### Por qué la regeneración en grises es viable

No es una apuesta: la receta del generador ya está parametrizada exactamente por
donde el modelo nuevo necesita cortar.
`art/world-of-goses-lpc-lineages-reproducible-v2/source/recipes/lineages.json`
declara, por linaje:

```json
"colors":   { "primary", "secondary", "accent", "skin", "hair" },
"profiles": { "symbol", "accessories", "back", "female_hair_back", "weapon" }
```

Es decir, el linaje ya es **cinco colores más cuatro piezas de forma**. El mapeo al
modelo nuevo es directo:

| Campo de la receta | Adónde va |
| --- | --- |
| `colors.primary/secondary/accent` | zonas de material de la capa `garment` |
| `colors.skin` | zona de la capa `body` |
| `colors.hair` | zona de la capa `hair` |
| `profiles.symbol/accessories/back` | capa **`misc`** rígida, 1 por linaje |

Y lo decisivo: el generador **compone desde capas nombradas con paletas
declaradas**, así que el índice de sombra y la zona de material se **derivan de la
identidad de la capa**, no se adivinan de los píxeles. Eso hace la conversión
determinista y reversible, que es justo lo que una cuantización perceptual a 6
grises no sería — aplicada a plano sobre el compuesto, piel y tela colapsarían en
los mismos grises y quedaría ilegible.

Procedimiento: sustituir los bloques `colors` de la receta por el juego fijo de
grises indexados, ejecutar `build.ps1`, y convertir los 8 bloques `colors` en 8
texturas de rampa. Ocho renders pasan a ser un render y ocho rampas.

#### La prueba piloto: qué linajes y con qué controles

Medidos los ocho `colors.primary` de la receta por matiz:

| Linaje | `primary` | Matiz aprox. |
| --- | --- | ---: |
| ardhen | `#72685D` | 37° (casi gris) |
| eirune | `#4F7752` | 124° |
| vaelun | `#3E6E78` | 189° |
| **kovari** | `#515D69` | **211°** |
| **caelith** | `#405370` | **216°** |
| myrven | `#674D78` | 279° |
| theryn | `#784052` | 342° |
| orveth | `#6B4142` | 359° |

Caelith y Eirune **no son el par cercano**: 216° contra 124° son 92° de separación.
Lo que sí colisiona entre ellos es una zona cruzada — el `secondary` de Eirune
(`#3F8580`, 177°) contra el `accent` de Caelith (`#75BDC0`, 182°), a 5°.

Los dos son buenas pruebas, de cosas distintas:

| Par | Qué mide | Por qué importa |
| --- | --- | --- |
| **Caelith + Eirune** | colisión **cruzada de zonas**: un material comparte matiz mientras los demás no | es el fallo realista — no dos linajes gemelos, sino dos que se pisan en una prenda |
| **Caelith + Kovari** | colisión **frontal**: 211° contra 216°, ambos azules desaturados de luminosidad parecida | el peor caso absoluto. Si sobrevive esto, sobrevive todo |

Recomendación: **corre los tres.** Añadir Kovari es una línea más en la receta —es un
script, no trabajo de dibujo— y cubres los dos modos de fallo en vez de uno.

**Y el control que decide si la prueba vale algo:** `skin` y `hair` diferencian los
linajes por sí solos. El pelo de Caelith es `#BDB5A7` (ceniza claro) y el de Eirune
`#304B35` (verde oscuro) — con eso puesto, los dos se distinguen aunque la rampa de
prenda no haga nada, y concluirías «funciona» cuando el pelo hizo el trabajo.

Por tanto: **fija `skin` y `hair` a la misma rampa en los tres linajes** y evalúa sólo
lo que consigue la prenda. Y evalúa **dos veces, con y sin la capa `misc`**, para
separar cuánto aporta el color y cuánto la forma — que es exactamente el reparto que
§7.3 afirma y que esta prueba puede confirmar o desmentir.

#### Qué valida y qué no

El laboratorio prueba **una sola cosa: la estrategia de color.** Todo lo demás queda
fuera de su alcance por definición, y eso está bien — no se evalúa la apariencia de
un arte que se descarta.

| Valida | Queda fuera de alcance |
| --- | --- |
| El shader del §6 sobre 11.648 PNG reales | El contrato de conjuntos: los clips LPC son `slash`/`thrust` genéricos |
| Si 6 sombras bastan, o hacen falta 7 | El lenguaje corporal por arma, que es el 70 % del proyecto |
| Si el canal de zona aguanta 5+ materiales | Las tablas de anclaje |
| Si el contorno lee bien recoloreado por rampa | La apariencia a 64 × 64 (las celdas son 128 × 128), irrelevante aquí |
| El flujo de autorar rampas frente a re-renderizar | |

Si el LUT funciona en el laboratorio, se aplica **desde el primer `.pxo`** en
Pixelorama. Eso es el ahorro real: no descubrir a mitad de la espada que la capa
`garment` había que autorarla de otra manera.

Dos cautelas. La primera: **regenerar en grises no lava la procedencia.** Sigue
siendo arte derivado de LPC, así que la atribución OGA-BY / CC-BY-SA / GPL sigue
vigente mientras el conjunto esté en el árbol — y se queda en el árbol como
laboratorio, no se borra. La segunda: la salida del laboratorio **no se promociona a
`game/assets/`** — el propio generador ya establece que su wrapper de variantes
«nunca escribe en `game/assets`».

#### Lo que implica la retirada

Medido en el repositorio, no estimado.

**Superficie de retirada.** `game/assets/characters/lineages/` contiene **23.921
archivos, 11.648 PNG, 208 `.tscn`, 208 `.tres`, 154 MB**. Son 8 linajes × 13
variantes de apariencia × 2 complexiones.

Retirada significa **fin de su usabilidad en el juego, no borrado del árbol**: el
conjunto se queda como laboratorio de color. Consecuencias de eso: los 154 MB siguen
en el repositorio (y de todos modos seguirían en el historial de git aunque se
borraran, sin reescribir historia), y la atribución LPC sigue vigente mientras estén.
Lo que sí desaparece es que el runtime los cargue: `CharacterVisualRegistry` deja de
resolver esas 208 rutas, y su carpeta pasa a ser material de referencia, no
`game/assets` activo. Cuando el laboratorio deje de aportar, moverlos fuera de
`game/assets/` es un cambio de una línea de ruta.

**Código que cambia de forma, no de detalle.**

- `game/scripts/visual/LineageSpritePlayer.cs` — su `AnimationState` enumera
  `Slash`, `Thrust`, `Halfslash`, `Backslash`, `Shoot`, `Spellcast`: nombres
  **genéricos de cuerpo** heredados de las hojas LPC, es decir exactamente el
  modelo que §1.2 descarta. `PlaySlash` / `PlayThrust` / `PlayHalfslash` /
  `PlayBackslash` son API pública y desaparecen. Lo sustituye un
  `WeaponAnimationSet` que resuelva **familia + estado de combate → clip**, más
  la composición de la cadena a partir de los traits activos (§3.1), que hoy no
  tiene dónde vivir.
- `game/scripts/visual/CharacterVisualRegistry.cs` — indexa por
  `(LineageId, AppearanceVariantId, CharacterBodyVariant)` → 208 rutas de escena.
  El modelo nuevo indexa por **familia de arma** para combate y aplica **linaje
  como rampa**, no como ruta. Es otra clave, no un ajuste.
  `tests/WorldofGoses.Tests/CharacterVisualRegistryTests.cs` asienta sobre ella.
- `game/scripts/Ui/CombatantView.cs` es el punto de consumo: hoy dibuja
  rectángulos en `_Draw` y opcionalmente hospeda un `LineageSpritePlayer`. Es
  donde se enchufa el conjunto del hito 1 y donde se mide si el contrato funciona.
- `CitizenSpriteBank.cs`, `CitizenSpriteCarrier.cs`, `VisibleWorkerSlots.cs`,
  `ExpeditionStage.cs`, `MacroStreetLiveView.cs` consumen la misma clave.

**Una fuga de capa que conviene cerrar en el mismo cambio.**
`AppearanceVariantId` vive en `game/scripts/Domain/` y enumera **familias
profesionales** (`Extraction`, `Construction`, … `Arts`). Es una **clave de arte
dentro del dominio**, y viaja por cinco snapshots de `Application`
(`CityMacroSnapshot`, `RosterSnapshot`, `ExpeditionLiveSnapshot`,
`BuildingDetailSnapshot`, `HeroProfileSnapshot`). Además, `SetAppearanceVariant`
**no tiene ni una llamada**: todo `Citizen` queda en `Standard`, así que 12 de las
13 variantes y 192 de las 208 escenas son inalcanzables hoy. Bajo el modelo nuevo
el tipo debe morir o convertirse en un concepto de dominio de verdad —qué lleva
puesto el ciudadano— en vez de una ruta de asset.

**Buena noticia: no hay migración de saves.** No encontré serialización de
`Citizen` (ni `JsonSerializer` sobre entidades, ni el `WorldSave.CurrentVersion`
que menciona `CLAUDE.md` como símbolo presente en `game/scripts/`); el
`AppearanceSeed` es un entero derivado y estable de nombre + linaje
(`CityWorld.StableAppearanceSeed`) y nada escribe `AppearanceVariant`. Por tanto
retirar LPC **no cruza una versión de esquema**. Conviene confirmarlo contra el
escritor de persistencia real antes de dar el paso — es lo único de esta lista que
no pude cerrar leyendo el código.

**Simplificación legal, y es real.** Las bases LPC declaran combinaciones de
**OGA-BY 3.0, CC-BY-SA 3.0 y GPL 3.0** (`docs/presentation/licenses/`). CC-BY-SA
es *share-alike* sobre arte derivado, y
`docs/presentation/licensing-and-attribution.md` reconoce que el ciclo `slash` fue
«compuesto sobre poses LPC» — o sea, derivado. Sustituir todo el conjunto elimina
esa obligación para el arte de personaje.

Con una cautela: **no borrar la atribución mientras quede un solo asset
derivado**. En particular `art/exports/characters/splash/` (16 imágenes) se
generó usando las hojas idle de LPC como referencia visual
(`art-pipeline.md` §5.2). Si esos splashes sobreviven a la sustitución, la
atribución sobrevive con ellos.

**Geometría y metadatos.** Celda 128 × 128 → 64 × 64; línea base `[64, 126]` →
nueva. Los `metadata.json` por linaje y `appearance_manifest.json` quedan obsoletos.

Y dos constantes que el cambio de canon (§1.4) deja desalineadas:
`PresentationConstants.DetailedCitizenWidth` y `DetailedCitizenHeight` valen **128**
—de donde `VisibleWorkerSlotWidth/Height` los derivan— y pasan a 64;
`MacroCitizenSize = 6` es el punto de placeholder del macro y desaparece junto con
el grupo `macro_citizen_dot`. Conviene notar que estas dos constantes son la prueba
de que las cifras del canon nunca se implementaron: el código nunca dibujó un
habitante de 32 × 64 ni de 4–8 px, sino uno de 128 × 128 y un punto de 6 px. La
corrección a 64 × 64 acerca el canon al código, no lo aleja.

**Documentación que cambia en el mismo incremento** (regla: la documentación sigue
a la arquitectura): `art-pipeline.md` §5.1 —que documenta como canon las 16
escenas, las 14 animaciones y las celdas de 128 × 128—, §5.2, `asset-inventory.md`,
`licensing-and-attribution.md` y `docs/presentation/MANIFEST.json`.

**Regresión visual.** Todas las fixtures de `tools/Capture-VisualMatrix.ps1`
cambian; la matriz completa sólo en modo `RELEASE`.

### 12.5 Diseño — qué hace la velocidad 4× con la locomoción

Hoy no hace nada: `SetSimulationSpeed` sólo acorta
`SimulationTickIntervalSeconds`, y la cadencia visual sigue en 24 Hz × 4 px = 96 px/s
(`PixelMotion`, alimentado por `delta` sin escalar). Como el paso es un
`MoveToward` hacia la posición autoritativa, a 4× **el objetivo se mueve cuatro veces
más rápido que el sprite** y la figura queda persiguiéndose a sí misma.

Dos salidas, y sólo una es compatible con la gramática:

| Opción | Qué pasa |
| --- | --- |
| Escalar la **frecuencia** de tick: 96 Hz × 4 px | correcto. Es avance rápido honesto y el paso sigue siendo de 4 px |
| Escalar el **tamaño** del paso: 24 Hz × 16 px | **resucita el tirón** que el cambio de 12 Hz/8 px a 24 Hz/4 px arregló el 2026-08-06, y por la misma razón |

No es una decisión de arte: el número de frames se autora a 1× de todas formas. Pero
conviene cerrarla antes del hito 5, cuando la ciudad se llene de caminantes.

### 12.6 Herramienta — cuatro huecos

1. **`Export-Art.ps1` no lee personajes.** Sólo despacha `terrain`. Faltan los dos
   `CATEGORY DISPATCH` para `characters`: lector de frames, export por capa, y
   escritura de `<sujeto>_<estado>.frames.json` con fps, loop y tabla de anclajes.
2. **Remapeo de la paleta de trabajo a grises indexados** (§6.5). Sin esto, la capa
   `garment` se dibuja a ciegas.
3. **La tabla de anclajes no tiene formato.** Propuesta: píxeles guía de color
   reservado en una capa `anchors` del `.pxo`, que el export traduce a coordenadas
   por frame. Se autora dibujando, no editando JSON.
4. **Validador de contrato.** No existía porque lo actual son placeholders
   autogenerados cuya semántica artística nadie controla — con arte propio, deja de
   ser opcional. `Test-WeaponAnimationSets.ps1` debe comprobar, como mínimo:
   presencia de los 16 clips del contrato; que cada bifurcación declare un nodo
   distinto; estabilidad de la línea base entre frames; que la capa `garment` use
   sólo los 6 grises y las zonas sancionadas; y que cada ancla exista en todos
   los frames del clip. Se paga desde el segundo conjunto, y a partir del hito 7 es
   lo único que hace segura la repetición.

---

## 13. Plan de acción

Lo que sigue son las acciones concretas, en orden de dependencia. Cada bloque es
material de issue: el trabajo abierto vive en GitHub, no aquí (`CLAUDE.md` §0).

### Bloque A — Cerrar decisiones antes de dibujar (nada depende de arte)

| # | Acción | Estado |
| --- | --- | --- |
| A1 | **El linaje fija el rango; la persona elige dentro del rango.** No era «linaje o persona»: el espec de §6.9 ya lo resolvió con las gamas cerradas de cabello y piel por linaje | **cerrado** |
| A2 | **`walk` 64 px/s, `run` 96 px/s.** El invariante es el paso de 4 px; lo que cambia es la cadencia — 16 Hz caminando, 24 Hz corriendo. El bonus de combate por `Impulse` sale de [#58](https://github.com/3FE3LE/world-of-goses/issues/58) | **cerrado**, ver §3.2 |
| A3 | **`idle` de 4 frames, y varios estados de idle.** Sin zancada no hay restricción; los estados extra se eligen por `AppearanceSeed` para que la ciudad no respire sincronizada | **cerrado** |
| A4 | **Qué hace el 4×**: escalar la frecuencia de tick, nunca el tamaño del paso (§12.5) | abierto |
| A5 | **¿Una bifurcación puede cambiar la trayectoria?** (§12.1) | abierto; decidir antes del hito 7 |
| A6 | **Oferta de onboarding**: restringirla a las expresiones cuyo par de armas esté dibujado (§12.3) | abierto; decidir antes del hito 2 |

### Bloque B — Hito 0: el sistema de color, sin dibujar nada definitivo

| # | Acción | Criterio de aceptación |
| --- | --- | --- |
| B1 | Curva de sombra compartida (6 pasos) + **la curva «clara»** alternativa | ambas declaradas como dato, no por linaje (§6.9) |
| B2 | Familias de paleta de los 8 linajes: 4 rampas de cabello + 4 de piel cada uno, y ~6 de atuendo globales | ~70 rampas; separadas en **luminosidad y saturación**, no sólo matiz (§6.3 b) |
| B3 | Paleta de trabajo: **matiz = zona, valor = sombra**, y su remapeo a `R`/`G` | dibujar en ella es legible; el export codifica (§6.7) |
| B4 | Regla de **suelo de contraste** entre zonas adyacentes, automatizada | reutiliza la métrica de delta del POC (§6.9) |
| B5 | Validar B1–B4 **con Myrven** regenerando el laboratorio | Myrven estresa la curva clara y el suelo de contraste a la vez |
| B6 | Reservar **zona 0 como passthrough** y documentar cuándo se usa | contorno y el píxel elegido a mano (§6.5) |

### Bloque C — Herramienta, en paralelo a B

| # | Acción |
| --- | --- |
| C1 | `Export-Art.ps1`: los dos `CATEGORY DISPATCH` de `characters` — lector de frames, export por capa, `<sujeto>_<estado>.frames.json` con fps, loop y anclajes |
| C2 | Formato de la **tabla de anclajes**: píxeles guía en una capa `anchors`, traducidos por el export |
| C3 | Remapeo de la paleta de trabajo (B3) dentro del exportador |
| C4 | `Test-WeaponAnimationSets.ps1`: 16 clips presentes, bifurcaciones en nodos distintos, línea base estable, sólo grises y zonas sancionadas, anclas en todos los frames |
| C5 | Shader del §6.2 en Godot con **dos fuentes de rampa** (linaje y persona) |

### Bloque D — Hito 1: el corte vertical

Una sola arma de punta a punta. ~325 frames, de los cuales ~105 son transversales que
no se vuelven a pagar (§10.3).

| # | Acción |
| --- | --- |
| D1 | `sword` completo: 20 `.pxo`, 110 frames × capas `body` / `garment` / `weapon` / `hair` (§10.4) |
| D2 | Locomoción lateral mínima: `idle`, `walk`, `run`, `climb`, `carry`, `knockback`, `defeated` |
| D3 | 1 arma guardada + 1 `misc` de linaje + 1 peinado rígido, como piezas ancladas |
| D4 | `manifest_fire` en `arc` y `line`, más `impact_physical` de esas dos trayectorias |
| D5 | `WeaponAnimationSet` en código: familia + estado → clip, y composición de la cadena desde los traits activos |
| D6 | Enchufarlo en `CombatantView` y **verificarlo en ejecución** |

### Bloque E — Lo que se decide midiendo, no antes

| # | Acción | Qué mide |
| --- | --- | --- |
| E1 | Confirmar **96 × 96** para una planta poniendo un ciudadano al lado | figura/fachada 75–85 %, figura/sprite 55–65 % (§1.4). El `96 × 128` que escribí salía de suponer que el tejado se abre en filas cercanas; con un solo row de parcelas no aplica |
| E2 | **Cuántas profundidades de descanso** puede ocupar la fila enfocada mientras la cámara pana en profundidad | fija los frames de la serie de tejado. El eje lateral queda descartado por la puerta (§9.6) |
| E2b | Política de cámara: **mantener centrada la fila enfocada** | acota el canto lateral a 17,2° en vez de 34–54°, sin dibujar nada |
| E3 | Ajustar el mundo de arranque a **3 × 1** | hoy `DefaultWorldParcelColumns = 5`, `DefaultWorldParcelRows = 2` |

### Bloque F — Retirada de LPC, cuando el laboratorio deje de aportar

| # | Acción |
| --- | --- |
| F1 | Sustituir `AnimationState` (`Slash`/`Thrust`/`Halfslash`/`Backslash`/`Shoot`/`Spellcast`) y su API pública |
| F2 | Reindexar `CharacterVisualRegistry`: de `(linaje, variante, complexión)` a familia de arma + rampa |
| F3 | Resolver `AppearanceVariantId`, que es una clave de arte en `Domain/` sin un solo llamante de `SetAppearanceVariant` |
| F4 | Persistencia: guardar **índice de rampa, nunca RGB**; índices append-only (§6.9) |
| F5 | Constantes: `DetailedCitizenWidth/Height` 128 → 64; retirar `MacroCitizenSize` y `macro_citizen_dot` |
| F6 | Documentación en el mismo incremento: `art-pipeline.md` §5.1–5.2, `asset-inventory.md`, `licensing-and-attribution.md`, `MANIFEST.json` |
| F7 | Atribución: **no retirarla mientras quede un derivado**, incluidos los 16 splash de `art/exports/characters/splash/` |
| F8 | Mover el laboratorio fuera de `game/assets/` |

---

## 14. Especificación de archivos, capas y nombres

Lo que hay que tener decidido antes de abrir el primer `.pxo`. Rutas y patrones según
`art-pipeline.md` §6.2; esta sección sólo lo instancia para el árbol de combate.

### 14.1 Las 8 zonas, fijas

El índice va en el canal `G` (§6.2). Este reparto es contrato: cambiarlo después
invalida todo lo dibujado.

| Zona | Nombre | Rampa que la colorea |
| ---: | --- | --- |
| 0 | `passthrough` | **ninguna** — color literal: contorno, blanco del ojo, especular |
| 1 | `skin` | rampa de **persona** |
| 2 | `hair` | rampa de **persona** |
| 3 | `eyes` | rampa de **persona** |
| 4 | `cloth` | rampa de **atuendo** |
| 5 | `leather` | rampa de **atuendo** |
| 6 | `metal` | rampa de **atuendo** |
| 7 | `accent` | rampa de **atuendo** — zona propia por el hallazgo de §6.3 a |

Seis sombras por zona, en los grises `0, 51, 102, 153, 204, 255` (§6.2). La textura de
rampa es 6 × 8.

### 14.2 Capas dentro de un `.pxo` de clip de arma

De abajo arriba. Los nombres son exactos: el exportador los busca.

| Capa | Contenido | Se exporta |
| --- | --- | --- |
| `anchors` | píxeles guía de color reservado (§14.4) | **no** como arte; se lee y se traduce |
| `weapon_back` | la parte del arma **detrás** del cuerpo | sí |
| `body` | anatomía y piel — zonas 0, 1, 3 | sí |
| `garment` | tela, cuero y metal del torso y las piernas — zonas 4, 5, 6, 7 | sí |
| `weapon_front` | la parte del arma **delante** del cuerpo | sí |

**El pelo no está aquí.** Es pieza rígida anclada a `head` (§6.9), así que vive en
`items/` y un solo juego de orientaciones sirve a las doce armas. Es un cambio respecto
a lo que este documento decía antes, y es el que hace asequible la biblioteca.

El arma se parte en `weapon_back` / `weapon_front` porque un bastón o una lanza cruzan
el cuerpo: sin las dos capas no hay forma de que el cuerpo quede en medio.

### 14.3 Los 20 `.pxo` de un arma

`art/source/characters/<familia>_<estado>.pxo`. Para `sword`:

| # | Archivo | Frames |
| ---: | --- | ---: |
| 1 | `sword_combat_idle.pxo` | 4 |
| 2 | `sword_ready.pxo` | 4 |
| 3 | `sword_combat_advance.pxo` | 6 |
| 4 | `sword_combat_retreat.pxo` | 6 |
| 5 | `sword_basic_a.pxo` | 6 |
| 6 | `sword_basic_b.pxo` | 5 |
| 7 | `sword_basic_c.pxo` | 6 |
| 8–9 | `sword_ba_expand_1.pxo`, `_2.pxo` | 4 + 4 |
| 10–11 | `sword_ba_branch_1.pxo`, `_2.pxo` | 7 + 7 |
| 12 | `sword_active.pxo` | 8 |
| 13–14 | `sword_as_expand_1.pxo`, `_2.pxo` | 5 + 5 |
| 15–16 | `sword_as_branch_1.pxo`, `_2.pxo` | 8 + 8 |
| 17 | `sword_dodge.pxo` | 5 |
| 18 | `sword_evade.pxo` | 5 |
| 19 | `sword_hurt_light.pxo` | 3 |
| 20 | `sword_hurt_heavy.pxo` | 4 |
| | **Total** | **110** |

`ba_branch_1` bifurca tras `basic_a`; `ba_branch_2` tras `basic_b` (§3.1, nodos
distintos). Los dos `expand` encadenan tras `basic_c`.

### 14.4 La capa `anchors`

Cada frame publica un registro por ancla: **posición y índice de orientación**. La
orientación es imprescindible y se me había pasado — sin ella una pieza rígida no puede
seguir el giro de la cabeza durante un mandoble.

| Ancla | Para qué | Lleva orientación |
| --- | --- | --- |
| `head` | pelo, casco, tocado | **sí** |
| `hand_main` | objeto en mano cuando no se autora en el clip | sí |
| `hand_off` | mano secundaria, escudo | sí |
| `hip` | cinturón, arma guardada | sí |
| `back` | mochila, arma guardada, raíz de capa | sí |
| `feet` | polvo, pisadas | no |
| `fx_origin` | **dónde y con qué dirección engancha la manifestación** (`arc`, `line`, `fall`, `flight`, `bloom`, `coil`) | sí |

`fx_origin` es lo que evita que la manifestación flote pegada encima en vez de salir
del arma.

Se autora dibujando: un píxel de color reservado por ancla en la capa `anchors`, y el
exportador lo traduce a coordenadas por frame (§12.6 C2).

### 14.5 Los archivos compartidos

`art/source/characters/citizen_<estado>.pxo` — 64 × 64, sirven a las tres escalas y a
las doce armas. Direcciones dibujadas: `down`, `up`, `side` (el lado se espeja).

| Estado | Direcciones | Frames por dir. | Total |
| --- | --- | ---: | ---: |
| `idle_a`, `idle_b` (dos estados, §3.2) | 3 | 4 | 24 |
| `walk` | 3 | 12 | 36 |
| `run` | 3 | 12 | 36 |
| `carry` | 3 | 4 | 12 |
| `climb` | 1 (`side`) | 5 | 5 |
| 7 verbos de trabajo | 2 | 4 | 56 |
| `knockback`, `defeated` (sin arma) | 1 (`side`) | 5 + 6 | 11 |
| **Total capa `body`** | | | **180** |

Con la capa `garment`, **360**. Sube respecto al presupuesto anterior (252) por el ciclo
de 12 frames y el `idle` de 4 con dos estados — que es exactamente donde §3.2 dice que
conviene gastar, porque es transversal y se ve constantemente.

`walk` y `run` son **dos gaits distintos**, no el mismo a otra velocidad: cambia la
actitud del cuerpo, la zancada y la fase aérea. Lo que sirve a cualquier velocidad es
un ciclo **dentro de** su gait.

Y `run` **no es aplazable**: la regla acordada es correr en ciudad a más de 3 casillas,
en expedición durante la marcha forzada, y **en combate siempre que haya que cerrar
distancia para entrar en rango**. Con ese uso se ve constantemente, así que sus 12
frames × 3 direcciones × 2 capas entran en el bloque transversal del hito 1.

#### Cerrar distancia en combate no cuesta clips nuevos

Se corre **con el arma en bajo**, no en guardia, y se levanta a guardia al llegar:

```text
fuera de rango  →  run (cuerpo compartido) + weapon_carry en `hand_main`
      llega     →  ready (4 f)
                →  combat_idle
```

Cuerpo: el `run` compartido, que ya está pagado. Arma: `weapon_carry_<familia>`,
3 orientaciones — un arma llevada en bajo mientras se corre apenas bascula. **Coste
total: 36 sprites pequeños.**

La alternativa —un `combat_charge` por arma— serían 12 × ~10 f × 2 capas = **240
frames**. Se ahorran ~204 dibujando un arma en tres posiciones.

`combat_advance` conserva su trabajo: el paso corto **en guardia**, dentro del umbral.
Así el umbral de 3 casillas gobierna ciudad y combate con la misma regla, en vez de dos.

### 14.6 Las piezas rígidas

`art/source/items/<sujeto>.pxo`, un archivo por sujeto, una orientación por frame.

| Sujeto | Frames | Ancla |
| --- | ---: | --- |
| `hair_<nombre>` × 28 | 10 c/u = 280 | `head` |
| `weapon_stowed_<familia>` × 12 | 3 c/u = 36 | `back`, `hip` |
| `weapon_carry_<familia>` × 12 | 3 c/u = 36 | `hand_main` — arma en bajo mientras se corre |
| `lineage_misc_<linaje>` × 8 | 10 c/u = 80 | varía |
| `armor_hard_<pieza>` × 4 | 10 c/u = 40 | `head`, `hip`, `back` |
| `tool_<verbo>` × 7 | 8 c/u = 56 | `hand_main` |
| `profession_mark` × 12 | 12 | `head`, `back` |
| `wear_mask` | 8 | — |

### 14.7 Orden de composición en ejecución

De abajo arriba. Es lo que define la variabilidad de un personaje: **ninguna
combinación necesita arte nuevo**.

| # | Capa | Origen | Rampa |
| ---: | --- | --- | --- |
| 1 | `weapon_back` | clip del arma | atuendo |
| 2 | `body` | clip del arma (o `citizen_*` fuera de combate) | persona |
| 3 | `garment` | mismo clip | atuendo |
| 4 | `armor_hard` | `items/`, anclado | atuendo |
| 5 | `hair` | `items/`, ancla `head` + orientación | persona |
| 6 | `headwear` | `items/`, ancla `head` | atuendo |
| 7 | `lineage_misc` | `items/`, anclado | atuendo |
| 8 | `weapon_front` | clip del arma | atuendo |
| 9 | `fx` | `effects/`, ancla `fx_origin` | propia |

El pelo va **debajo** del tocado: un casco tapa el pelo, no al contrario.

### 14.8 Nombres de export y de recurso

| Artefacto | Patrón |
| --- | --- |
| Fuente | `art/source/characters/sword_basic_a.pxo` |
| Hoja por capa | `art/exports/characters/sword_basic_a_<capa>_sheet.png` |
| Metadatos | `art/exports/characters/sword_basic_a.frames.json` — fps, loop, anclajes por frame |
| Importado | `game/assets/characters/sword/` |
| Recurso Godot | `SpriteFrames_Sword_BasicA` |
| Rampa | `art/palettes/ramp_<linaje>.png`, `ramp_person_<id>.png` |

Los ids de rampa son **append-only y no se reordenan nunca** (§6.9): un `Citizen`
guarda el índice, no el color.
