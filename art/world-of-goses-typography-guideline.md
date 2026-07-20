# Guía tipográfica

## Objetivo

Definir una jerarquía tipográfica clara, consistente y legible para la interfaz del juego, manteniendo una identidad visual inspirada en pixel art sin sacrificar la lectura en textos largos.

El sistema utiliza tres familias tipográficas, cada una con una función específica:

1. **Geist Pixel** para identidad e impacto visual.
2. **Jersey 10** para estructura, navegación y subtítulos.
3. **Pixelify Sans** para lectura y contenido informativo.

---

## 1. Geist Pixel

### Función

Geist Pixel representa el nivel más alto de la jerarquía. Debe utilizarse en elementos que necesiten transmitir identidad, importancia o dramatismo.

### Usos recomendados

- Logo del juego.
- Títulos principales.
- Títulos de pantalla.
- Nombre de la ciudad o asentamiento.
- Eventos globales.
- Cambios de era.
- Resultados importantes.
- Mensajes de victoria, derrota o crisis.
- Contadores grandes o celebratorios.

### Ejemplos

- `NUEVA ERA`
- `EXPEDICIÓN FALLIDA`
- `HAMBRUNA`
- `DÍA 128`
- `+500 HABITANTES`
- `NIVEL 7`

### Tamaños iniciales recomendados

| Uso | Tamaño |
|---|---:|
| Logo | 48–64 px |
| Título principal | 40–48 px |
| Título de pantalla | 32–40 px |
| Mensaje destacado | 32–48 px |

### Restricciones

- No utilizar en párrafos.
- No utilizar en descripciones extensas.
- Evitar su uso en controles pequeños.
- Reservarla para elementos que realmente necesiten destacar.

---

## 2. Jersey 10

### Función

Jersey 10 ocupa el nivel intermedio de la jerarquía. Organiza la interfaz y comunica la estructura de cada pantalla.

### Usos recomendados

- Subtítulos.
- Encabezados de paneles.
- Botones.
- Pestañas.
- Categorías.
- Nombres de edificios.
- Estados cortos.
- Etiquetas principales.
- Secciones de menús.
- Títulos de ventanas modales.

### Ejemplos

- `MINA DE HIERRO`
- `PRODUCCIÓN`
- `POBLACIÓN`
- `RECURSOS`
- `INICIAR EXPEDICIÓN`
- `CONSTRUCCIÓN DETENIDA`

### Tamaños iniciales recomendados

| Uso | Tamaño |
|---|---:|
| Encabezado de panel | 24–28 px |
| Subtítulo | 22–26 px |
| Botón principal | 20–24 px |
| Botón secundario | 18–20 px |
| Etiqueta destacada | 18–22 px |

### Restricciones

- Evitar bloques de texto de más de dos líneas.
- No utilizar como fuente principal para tutoriales o descripciones.
- No usar en tablas densas o estadísticas extensas.
- Mantener frases cortas y directas.

---

## 3. Pixelify Sans

### Función

Pixelify Sans es la fuente principal de lectura. Debe utilizarse en cualquier contenido que requiera claridad, continuidad y comodidad visual.

### Usos recomendados

- Párrafos.
- Descripciones.
- Tooltips.
- Tutoriales.
- Diálogos.
- Eventos narrativos.
- Decisiones y consecuencias.
- Estadísticas acompañadas de texto.
- Información de edificios.
- Detalles de expediciones.
- Mensajes del sistema.
- Tablas y listados.

### Ejemplos

> La mina necesita al menos 12 trabajadores para reanudar la extracción de hierro.

> La falta de alimentos ha reducido temporalmente el crecimiento de la población.

> Producción estimada: 24 unidades por día.

### Tamaños iniciales recomendados

| Uso | Tamaño |
|---|---:|
| Texto principal | 18 px |
| Texto secundario | 16 px |
| Tooltip | 16–18 px |
| Texto destacado | 20 px |
| Tabla o listado | 16–18 px |

### Restricciones

- Evitar tamaños inferiores a 16 px sin pruebas específicas de legibilidad.
- No escalar el nodo visualmente para cambiar el tamaño.
- Modificar directamente la propiedad `Font Size`.
- Usar interlineado suficiente en párrafos de varias líneas.

---

## Jerarquía general

```text
Geist Pixel
└── Identidad, impacto y eventos principales

Jersey 10
└── Navegación, estructura, subtítulos y controles

Pixelify Sans
└── Lectura, contenido, datos y descripciones
```

---

## Variaciones recomendadas para el Theme de Godot

```text
GameTitle
ScreenTitle
EventTitle

PanelTitle
SectionTitle
ButtonText
TabText
BuildingName

BodyText
BodySmall
TooltipText
DialogText
TableText
NumericText
```

### Asignación inicial

| Variación | Fuente | Tamaño inicial |
|---|---|---:|
| `GameTitle` | Geist Pixel | 48 px |
| `ScreenTitle` | Geist Pixel | 36 px |
| `EventTitle` | Geist Pixel | 40 px |
| `PanelTitle` | Jersey 10 | 26 px |
| `SectionTitle` | Jersey 10 | 22 px |
| `ButtonText` | Jersey 10 | 20 px |
| `TabText` | Jersey 10 | 18 px |
| `BuildingName` | Jersey 10 | 22 px |
| `BodyText` | Pixelify Sans | 18 px |
| `BodySmall` | Pixelify Sans | 16 px |
| `TooltipText` | Pixelify Sans | 16 px |
| `DialogText` | Pixelify Sans | 18 px |
| `TableText` | Pixelify Sans | 16 px |
| `NumericText` | Pixelify Sans | 18 px |

---

## Uso de números

Los números deben seguir la función del componente, no una fuente única.

### Pixelify Sans

Utilizar para datos frecuentes o densos:

- Recursos.
- Producción diaria.
- Población.
- Porcentajes.
- Costos.
- Estadísticas.
- Tablas.

```text
Madera: 12.450
Producción: +38/día
Población: 4.823
Eficiencia: 76 %
```

### Geist Pixel

Utilizar para números destacados:

```text
DÍA 128
NIVEL 7
+500 HABITANTES
```

### Jersey 10

Utilizar para números integrados en botones, pestañas o encabezados:

```text
EXPEDICIONES 3
ALERTAS 2
EDIFICIOS 12
```

---

## Reglas de consistencia

1. No utilizar más de una fuente dentro del mismo componente, salvo una excepción justificada.
2. No utilizar Geist Pixel en párrafos.
3. No utilizar Jersey 10 en bloques extensos.
4. Utilizar Pixelify Sans como fuente predeterminada de lectura.
5. Cambiar el tamaño de fuente, no la escala del nodo.
6. Usar tamaños enteros.
7. Evitar escalas fraccionarias como `1.25` o `1.5`.
8. Probar siempre los textos en la resolución real del juego.
9. Mantener suficiente contraste entre texto y fondo.
10. Evitar añadir una cuarta fuente sin una necesidad funcional clara.

---

## Configuración recomendada en Godot

Para conservar el aspecto pixel art:

```text
Antialiasing: Disabled
Subpixel Positioning: Disabled
MSDF: Disabled inicialmente
```

También se recomienda:

- Usar escalado entero siempre que sea posible.
- Evitar transformar el `Control` padre con escalas fraccionarias.
- Configurar las fuentes mediante un recurso global `Theme`.
- Crear variaciones de tipo para cada nivel tipográfico.
- Mantener las fuentes dentro de `res://assets/fonts/`.

### Estructura sugerida

```text
assets/
└── fonts/
    ├── geist-pixel/
    │   ├── GeistPixel-Regular.ttf
    │   └── OFL.txt
    ├── jersey-10/
    │   ├── Jersey10-Regular.ttf
    │   └── OFL.txt
    └── pixelify-sans/
        ├── PixelifySans-Regular.ttf
        └── OFL.txt

ui/
└── themes/
    ├── game_theme.tres
    └── typography/
```

---

## Prueba mínima de caracteres

Antes de aprobar una fuente o tamaño, comprobar:

```text
ABCDEFGHIJKLMNOPQRSTUVWXYZ
abcdefghijklmnopqrstuvwxyz
0123456789

ÁÉÍÓÚ áéíóú
Ññ Üü
¿? ¡!
% + - / :
```

### Texto de prueba

```text
¿La población está satisfecha?
¡Producción aumentada un 25 %!
Médicos disponibles: 4
La mina requiere 12 trabajadores adicionales.
Esperanza de vida: 67 años.
```

---

## Criterio final

Cada fuente debe responder a una necesidad distinta:

- **Geist Pixel atrae la atención.**
- **Jersey 10 organiza la interfaz.**
- **Pixelify Sans permite leerla.**

La identidad visual debe surgir de la jerarquía y la consistencia, no de utilizar tipografías decorativas en todos los elementos.
