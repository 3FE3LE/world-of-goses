# Dirección de audio

> Guía estética. Describe cómo debe sonar World of Goses y con qué criterios se
> juzga un sonido. Hoy el juego no tiene buses, streams ni reproductores
> conectados; la integración de la arquitectura de reproducción y del primer
> paquete sonoro está trazada en
> [#34](https://github.com/3FE3LE/world-of-goses/issues/34).

## Qué es

Construir una identidad sonora coherente con el pixel art sin reducir todo a pitidos genéricos.

El audio debe sentirse sintético, nítido, limitado deliberadamente, reconocible y cómodo durante sesiones largas.

## 8-bit y 16-bit como estética

La identidad retro no exige guardar archivos en 8 bits de profundidad.

Recomendación:

- Diseñar sonidos con síntesis tipo PSG, FM, wavetable o ruido.
- Exportar en formatos modernos.
- Usar WAV para efectos breves.
- Usar Ogg Vorbis para música y ambientes largos.

## Capas sonoras

### UI

- Hover discreto.
- Confirmación.
- Cancelación.
- Error.
- Pestaña.
- Apertura y cierre.
- Selección de parcela.
- Recurso ganado.

### Ciudad macro

- Viento.
- Actividad lejana.
- Campanas.
- Talleres.
- Agua.
- Mercados.
- Construcción.

### Edificios

Cada estancia necesita un loop sutil:

- Mina: impactos, grava, madera y poleas.
- Granja: viento, herramientas, animales y agua.
- Hospital: instrumentos suaves, pasos y actividad contenida.
- Taller: metal, fuelles y mecanismos.

### Expediciones

- Pasos.
- Ataques.
- Habilidades.
- Buffs.
- Heridas.
- Teleportación.
- Ambientes.
- Transiciones.
- Retirada.
- Victoria.

## Lenguaje retro

Usar de forma controlada:

- Onda cuadrada.
- Onda triangular.
- Ruido.
- Pulsos cortos.
- Arpegios.
- FM ligera.
- Wavetable.
- Percusión sintética.

Evitar exceso de agudos, sonidos constantes, loops demasiado breves y compresión destructiva como sustituto del diseño.

## Identidad por linaje

- **Ardhen:** percusión seca, golpes graves, ritmos regulares y resonancia mineral.
- **Eirune:** pulsos orgánicos, wavetable suave, respiración y ciclos.
- **Kovari:** FM, chasquidos mecánicos, segmentos y glitches controlados.
- **Myrven:** capas duplicadas, ecos cortos, cambios de voz y silencios.
- **Vaelun:** melodías abiertas, viento y pulsos de viaje.
- **Orveth:** patrones medidos, campanas sintéticas y motivos de intercambio.
- **Caelith:** arpegios complejos, timbres cristalinos y motivos conectados.
- **Theryn:** pulsos, percusión colectiva y capas que se sincronizan.

## Variación

- Variar pitch ligeramente.
- Tener varias muestras por acción frecuente.
- Aleatorizar dentro de una familia.
- Aplicar cooldown.
- Reducir sonidos fuera de foco.
- Mantener control de volumen.

## Distribución de buses

```text
Master
├── Music
├── Ambience
├── UI
├── City
├── Buildings
├── Expeditions
├── Voices
└── Critical
```

## Licencias

Prioridad:

1. CC0.
2. Licencias permisivas con atribución clara.
3. Assets creados internamente.

Registrar:

```text
asset
source
author
license
original_file
modified
usage
replacement_required
```

## Pipeline

```text
Fuente o sintetizador
↓
Edición
↓
Normalización y recorte
↓
WAV u OGG
↓
Godot AudioStream
↓
AudioStreamPlayer o AudioStreamPlayer2D
↓
Bus y reglas de reproducción
```

