# Laboratorio: LUT de rampa de paleta

Prueba de concepto de la estrategia de color descrita en
[`../world-of-goses-sprite-production-inventory.md`](../world-of-goses-sprite-production-inventory.md)
§6. **No es parte del pipeline** y no produce arte de juego.

Corre sobre las hojas LPC ya compuestas —que se descartan— porque comprueba lo
único que no se puede comprobar dibujando: si un sprite en **grises indexados**
más una **rampa por linaje** reproduce y distingue lo que hoy son ocho renders.

```powershell
node art/lab-grayscale-ramp/build-poc.js
```

Sujeto: `idle_down` (pose frontal, 2 frames, 256 × 128 RGBA8), cuerpos `male` y
`female`, una sola vestimenta, linajes `caelith` · `eirune` · `kovari`.

## Cómo se deriva la zona

La zona de cada píxel **no se adivina del color**. Se deriva del **acuerdo entre
tres linajes** que comparten geometría y difieren en paleta: para cada píxel se
elige la zona que minimiza simultáneamente la distancia de matiz/croma contra los
cinco colores declarados de los tres linajes en
`source/recipes/lineages.json`. Un píxel de `primary` está cerca del `primary` de
Eirune en la hoja de Eirune y del de Caelith en la de Caelith, y esa triple
coincidencia resuelve colisiones que una sola hoja no puede — el `hair` de Eirune
(`#304B35`, 133°) y su `primary` (`#4F7752`, 124°) están a 9°, pero el `hair` de
Caelith es casi neutro y desempata.

Es la versión medible del argumento «la zona viene de la identidad de la capa, no
de los píxeles».

## Salidas

| Archivo | Qué es |
| --- | --- |
| `zoom_<body>.png` | **el que hay que mirar.** Columnas = linajes, filas = experimentos, ×5 |
| `poc_<body>.png` | lo mismo sin ampliar, a resolución nativa |
| `encoded_<body>.png` | la hoja codificada real: `R` = índice de sombra, `G` = zona. Casi negra a la vista; es lo que consume el shader |
| `debug_<body>.png` | mapa de zonas en paleta de trabajo legible, y mapa de sombras |
| `ramp_<lineage>.png` | la textura de rampa, 6 × 8 (6 sombras × 8 zonas) |
| `report.json` | métricas |

Filas de `zoom_<body>.png`:

| Fila | Experimento |
| --- | --- |
| **A** | original LPC |
| **B** | round-trip: codificado a 6 grises y reconstruido con la rampa **empírica** (la media observada por zona y sombra) |
| **C** | reconstruido con rampas construidas **sólo desde los 5 colores declarados** |
| **D** | igual que C con **`skin` y `hair` bloqueados** a la misma rampa en los tres linajes |

La fila D es la única que responde a la pregunta. Sin bloquear `skin` y `hair`
los linajes se distinguen por el pelo —Caelith `#BDB5A7` ceniza contra Eirune
`#304B35` verde oscuro— y se concluiría que la rampa de prenda funciona cuando el
trabajo lo hizo el pelo.

## Resultados

### 1. El mecanismo funciona

| Cuerpo | Error de round-trip (media / máx, sobre 255) |
| --- | --- |
| male | caelith 5,45 / 94 · eirune 6,19 / 103 · kovari 7,62 / 132 |
| female | caelith 5,27 / 89 · eirune 6,14 / 121 · kovari 9,05 / 132 |

Media del 2–3,6 %. La fila B es indistinguible de la A a tamaño nativo. **Seis
sombras bastan para los materiales base.**

### 2. Seis sombras **no** bastan para los acentos

El error máximo de 89–132 se concentra en detalles de 1–3 píxeles: la ramita de
Eirune y los tachones de Kovari desaparecen o se aplanan. Son píxeles cuya
luminancia queda lejos del centro de su bucket porque comparten el rango de
luminancia de una zona ancha.

**Corrección adoptada:** los acentos van en **zona propia**, no compartiendo el
rango de otra. Es más barato que subir a 8 sombras y el presupuesto de zonas
—ocho— ya lo permite.

### 3. Los linajes se distinguen, y está medido

Delta medio por canal entre linajes, **sólo en zonas de prenda**, fila D:

| Par | male | female |
| --- | ---: | ---: |
| caelith / eirune | 24,5 | 24,8 |
| caelith / kovari | 29,3 | 30,8 |
| eirune / kovari | 37,2 | 38,6 |

Con piel y pelo idénticos, los tres siguen leyéndose como linajes distintos: la
prenda sola lo consigue.

### 4. Lo que separa **no es la distancia de matiz** — y esto cambia la guía

Caelith y Eirune están a **92°** de matiz en `primary` y separan **menos** (24,5)
que Caelith y Kovari, que están a **5°** y separan **más** (29,3).

La razón: Caelith `#405370` y Eirune `#4F7752` son ambos **oscuros y
desaturados**, así que su delta RGB es modesto pese al abismo de matiz. Kovari
mete rojos más saturados y cálidos, y eso sí se ve.

**Consecuencia para las ocho rampas: hay que separarlas en luminosidad y
saturación, no sólo en matiz.** Dos linajes lejanísimos en matiz pero ambos
oscuros y apagados se leen como «los dos oscuros y apagados». Es exactamente el
fallo que este laboratorio existía para encontrar, y no se habría visto sin medir.

### 5. La clasificación automática falla donde se predijo

En las filas C y D aparecen marcas de color de acento sobre el pelo: reflejos de
`hair` clasificados como `accent`. Es un límite de **inferir zona desde un
compuesto**, no del LUT. En producción no ocurre, porque la prenda se dibuja en su
propia capa y la zona es un hecho de autorado, no una inferencia. El fallo
confirma el argumento en vez de contradecirlo.

### 6. Las rampas programáticas son un punto de partida, no el destino

Las rampas de la fila C/D se generan por fórmula (oscurecer y enfriar hacia la
sombra, aclarar y calentar hacia la luz) y salen más planas que los colores
elegidos a mano de LPC. La rampa autorada será mejor que esto, no peor: aquí sólo
importaba que el mecanismo transporte la identidad.

## Qué queda fuera de alcance

El contrato de conjuntos por arma, el lenguaje corporal, las tablas de anclaje y
la apariencia a 64 × 64 (las celdas LPC son 128 × 128). Nada de eso se evalúa
aquí, y no hace falta: el arte se descarta.

## Procedencia

Las hojas de entrada derivan de **Universal LPC Spritesheet Character
Generator** (OGA-BY 3.0 · CC-BY-SA 3.0 · GPL 3.0). Codificarlas en grises no lava
la procedencia: la atribución de
[`../../docs/presentation/licensing-and-attribution.md`](../../docs/presentation/licensing-and-attribution.md)
sigue vigente mientras estas hojas estén en el árbol. **Ninguna salida de este
directorio se promociona a `game/assets/`.**
