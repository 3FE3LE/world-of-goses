# Guía de iconografía

## Objetivo

Definir un sistema de iconografía consistente para la interfaz y los sistemas de juego, manteniendo una estética pixel art clara, legible y escalable.

La iconografía se divide en tres niveles:

1. **Kenney Pixel UI** para componentes y estructura visual.
2. **Pixelarticons** para acciones genéricas y navegación.
3. **Iconografía propia** para sistemas, recursos y contenido del juego.

---

## 1. Kenney Pixel UI

### Función

Kenney Pixel UI sirve como base para los componentes visuales de la interfaz. Su función no es representar conceptos propios del mundo, sino proporcionar la estructura sobre la que se construyen los menús y paneles.

### Usos recomendados

- Marcos.
- Paneles.
- Botones.
- Barras de progreso.
- Sliders.
- Checkboxes.
- Cursores.
- Selectores.
- Ventanas.
- Separadores.
- Estados activos.
- Estados bloqueados.
- Estados seleccionados.
- Fondos de inventario.
- Slots.
- Contenedores de recursos.

### Restricciones

- No utilizar sus elementos como representación principal de recursos o sistemas propios del juego.
- No mezclar componentes de varios paquetes de UI sin ajustar previamente su estilo.
- Evitar elementos que no coincidan con la paleta, proporciones o grosor visual del proyecto.
- Adaptar o recolorear los assets cuando sea necesario para integrarlos al lenguaje visual general.

---

## 2. Pixelarticons

### Función

Pixelarticons cubre acciones universales de interfaz y navegación. Debe utilizarse para conceptos reconocibles que no necesitan una identidad propia dentro del mundo del juego.

### Usos recomendados

- Guardar.
- Cerrar.
- Volver.
- Avanzar.
- Confirmar.
- Cancelar.
- Buscar.
- Configuración.
- Información.
- Ayuda.
- Alertas.
- Calendario.
- Usuario.
- Visibilidad.
- Sonido.
- Música.
- Inventario.
- Mapa.
- Ordenar.
- Filtrar.
- Expandir.
- Contraer.

### Tamaño base

Pixelarticons utiliza una cuadrícula original de `24×24`.

Los tamaños recomendados deben conservar múltiplos enteros:

```text
24×24
48×48
72×72
96×96
```

### Restricciones

- No redibujar o escalar a tamaños fraccionarios.
- No deformar la relación de aspecto.
- No utilizar para representar recursos, linajes, profesiones o edificios específicos.
- Evitar combinarlo con otros paquetes de íconos genéricos sin una normalización visual previa.

---

## 3. Iconografía propia

### Función

La iconografía propia representa la identidad del juego. Debe utilizarse para cualquier concepto conectado directamente con sus sistemas, mundo, economía, narrativa o gameplay.

### Categorías principales

#### Recursos

- Madera.
- Piedra.
- Hierro.
- Carbón.
- Alimentos.
- Agua.
- Medicinas.
- Moneda.
- Herramientas.
- Materiales especiales.

#### Edificios

- Mina.
- Granja.
- Hospital.
- Taller.
- Aduana.
- Refugio.
- Almacén.
- Cuartel.
- Mercado.
- Centro administrativo.

#### Sistemas sociales

- Población.
- Salud.
- Felicidad.
- Seguridad.
- Educación.
- Vivienda.
- Empleo.
- Inmigración.
- Mortalidad.
- Longevidad.

#### Progresión

- Tecnologías.
- Planos.
- Investigación.
- Era.
- Nivel cultural.
- Desarrollo político.
- Desarrollo económico.
- Expansión territorial.

#### Expediciones

- Equipo.
- Heridas.
- Fatiga.
- Moral.
- Botín.
- Retirada.
- Exploración.
- Diplomacia.
- Reclutamiento.
- Subyugación.

#### Personajes y linajes

- Linajes.
- Profesiones.
- Oficios.
- Roles.
- Afinidades.
- Estados.
- Buffs.
- Debuffs.
- Rasgos.
- Especializaciones.

### Restricciones

- No sustituir conceptos centrales del juego con íconos genéricos de terceros.
- No utilizar estilos visuales incompatibles entre categorías.
- Mantener una paleta, iluminación y nivel de detalle compartidos.
- Evitar que el color sea el único indicador de significado.

---

## Jerarquía general

```text
Kenney Pixel UI
└── Componentes, paneles, botones y estructura visual

Pixelarticons
└── Navegación, acciones universales y controles

Iconografía propia
└── Sistemas, recursos, edificios y contenido del juego
```

---

## Escalas recomendadas

### 24×24

Usar para acciones pequeñas y controles secundarios:

- Cerrar.
- Volver.
- Configuración.
- Buscar.
- Filtrar.
- Expandir.
- Contraer.
- Información.

### 32×32

Usar para recursos y estados frecuentes:

- Madera.
- Piedra.
- Alimentos.
- Salud.
- Felicidad.
- Población.
- Seguridad.
- Producción.

### 48×48

Usar para categorías y sistemas importantes:

- Edificios.
- Profesiones.
- Oficios.
- Linajes.
- Tipos de expedición.
- Tecnologías.
- Políticas.

### 64×64

Usar para elementos destacados:

- Eventos.
- Grandes tecnologías.
- Hitos.
- Cambios de era.
- Recompensas.
- Resultados de expedición.
- Desastres.
- Decisiones importantes.

---

## Reglas de escala

1. Escalar siempre mediante múltiplos enteros.
2. Evitar escalas como `1.25`, `1.5` o `1.75`.
3. No utilizar un ícono pequeño ampliado artificialmente si existe una versión diseñada para el tamaño requerido.
4. Mantener el mismo tamaño visual para elementos con la misma importancia.
5. No mezclar íconos de `24×24`, `32×32` y `48×48` dentro de una misma fila sin una razón funcional.
6. Mantener márgenes internos consistentes dentro de cada cuadrícula.
7. Validar los íconos en la resolución real del juego.

---

## Formato de archivos

### Formato principal

Usar PNG con fondo transparente para los assets finales dentro del proyecto.

```text
Formato: PNG
Fondo: transparente
Antialiasing: desactivado
Perfil de color: sRGB
```

### SVG

Los SVG pueden utilizarse como fuente de origen para íconos externos, pero deben exportarse a PNG antes de integrarlos al sistema final cuando sea necesario conservar un resultado pixel-perfect controlado.

### Spritesheets

Utilizar spritesheets cuando:

- Existan múltiples estados de un mismo ícono.
- Los íconos compartan tamaño.
- Formen parte de una misma familia.
- Sea conveniente cargarlos como atlas.

---

## Estados visuales

Cada ícono interactivo debería contemplar, cuando aplique:

```text
Default
Hover
Pressed
Selected
Disabled
Warning
Critical
```

### Reglas

- El estado no debe depender únicamente del color.
- Utilizar cambios de contorno, brillo, contraste, símbolo secundario o fondo.
- Mantener la silueta reconocible en todos los estados.
- Evitar animaciones innecesarias en íconos pequeños.
- Utilizar animaciones breves solo cuando comuniquen una acción o cambio real.

---

## Color y contraste

1. Mantener una paleta limitada.
2. Definir colores semánticos compartidos.
3. Garantizar contraste suficiente contra paneles y fondos.
4. Evitar que dos sistemas distintos utilicen el mismo símbolo y color.
5. Utilizar contornos para conservar legibilidad sobre fondos variables.
6. Probar los íconos en escala de grises para comprobar que la silueta sigue siendo reconocible.

### Colores semánticos sugeridos

```text
Normal
Positivo
Advertencia
Crítico
Bloqueado
Seleccionado
Información
```

La definición exacta de colores debe pertenecer al guideline de paleta y no quedar codificada directamente dentro de cada asset sin una razón técnica.

---

## Dirección artística

La iconografía propia debe compartir:

- Grosor de contorno.
- Dirección de iluminación.
- Nivel de detalle.
- Perspectiva.
- Densidad de píxeles.
- Saturación.
- Contraste.
- Forma de representar materiales.
- Tamaño de márgenes internos.
- Tratamiento de sombras.

### Convenciones recomendadas

```text
Luz principal: superior izquierda
Sombra principal: inferior derecha
Contorno: consistente por categoría
Perspectiva: frontal o tres cuartos, pero no mezclada arbitrariamente
```

Estas convenciones podrán ajustarse cuando se defina la dirección artística definitiva.

---

## Silueta y reconocimiento

Un ícono debe poder reconocerse antes de depender de su color o detalle interno.

### Pruebas mínimas

- Revisarlo en escala de grises.
- Revisarlo a tamaño real.
- Revisarlo sobre fondo claro.
- Revisarlo sobre fondo oscuro.
- Compararlo con otros íconos de su categoría.
- Confirmar que no se confunde con otro sistema.
- Verificar que sigue siendo legible al reducirse.

---

## Nomenclatura de archivos

Usar nombres descriptivos y consistentes.

```text
icon_action_close_24.png
icon_action_settings_24.png
icon_resource_wood_32.png
icon_resource_iron_32.png
icon_building_mine_48.png
icon_building_hospital_48.png
icon_status_warning_24.png
icon_status_critical_24.png
icon_lineage_arden_48.png
```

### Convención

```text
icon_{category}_{name}_{size}.png
```

Para estados:

```text
icon_{category}_{name}_{state}_{size}.png
```

Ejemplo:

```text
icon_action_confirm_default_24.png
icon_action_confirm_hover_24.png
icon_action_confirm_disabled_24.png
```

---

## Estructura sugerida

```text
assets/
└── icons/
    ├── ui/
    │   ├── actions/
    │   ├── navigation/
    │   ├── states/
    │   └── controls/
    ├── resources/
    ├── buildings/
    ├── professions/
    ├── lineages/
    ├── technologies/
    ├── expeditions/
    ├── policies/
    ├── status-effects/
    └── atlases/
```

Los archivos de licencia deberían mantenerse separados:

```text
assets/
└── licenses/
    ├── kenney-cc0.txt
    └── pixelarticons-mit.txt
```

---

## Configuración recomendada en Godot

Para íconos pixel art:

```text
Filter: Nearest
Mipmaps: Disabled
Repeat: Disabled
```

### Uso por nodo

```text
TextureRect
└── Íconos informativos y decorativos

TextureButton
└── Acciones interactivas

Sprite2D
└── Íconos o indicadores dentro del mundo

AtlasTexture
└── Spritesheets y familias de íconos
```

### Reglas de implementación

- No escalar el nodo con valores fraccionarios.
- Ajustar el tamaño mediante dimensiones enteras.
- Mantener `Stretch Mode` compatible con la escala prevista.
- Evitar filtros lineales.
- Revisar el resultado en ventanas redimensionadas.
- Centralizar rutas y nombres cuando los íconos sean reutilizados por varios sistemas.

---

## Accesibilidad

1. No comunicar información únicamente mediante color.
2. Acompañar íconos ambiguos con texto o tooltip.
3. Mantener áreas de interacción mayores que el dibujo visible cuando sea necesario.
4. Evitar símbolos demasiado similares para acciones opuestas.
5. Mantener consistencia entre símbolo y comportamiento.
6. Utilizar etiquetas en acciones críticas.
7. Confirmar acciones destructivas aunque exista un ícono reconocible.

---

## Criterio para usar texto junto a un ícono

### Solo ícono

Aceptable para acciones universales y frecuentes:

- Cerrar.
- Volver.
- Configuración.
- Buscar.
- Sonido.
- Expandir.

### Ícono y texto

Recomendado para:

- Acciones importantes.
- Sistemas propios.
- Acciones destructivas.
- Decisiones.
- Construcción.
- Expediciones.
- Políticas.
- Tecnologías.
- Conceptos que puedan ser ambiguos.

Ejemplo:

```text
[ícono] Iniciar expedición
[ícono] Construir hospital
[ícono] Aprobar política
```

---

## Criterio de aprobación

Antes de integrar un ícono, confirmar:

- Pertenece a la categoría correcta.
- Respeta la cuadrícula definida.
- Conserva el estilo visual.
- Tiene silueta reconocible.
- Funciona sobre fondos claros y oscuros.
- No depende únicamente del color.
- Está exportado sin antialiasing.
- Utiliza escalado entero.
- Su licencia permite el uso previsto.
- Su archivo de licencia está incluido cuando corresponde.

---

## Criterio final

Cada fuente de iconografía cumple una función distinta:

- **Kenney Pixel UI construye la interfaz.**
- **Pixelarticons comunica acciones universales.**
- **La iconografía propia representa el mundo y sus sistemas.**

El objetivo no es crear cada botón desde cero ni llenar el juego de paquetes externos. El sistema debe reutilizar lo genérico y diseñar de forma propia aquello que define la identidad del juego.
