# Expediciones

## Propósito

Las expediciones permiten explorar, abrir rutas, revelar recursos, encontrar planos, descubrir tecnologías, contactar poblaciones, eliminar amenazas y habilitar parcelas.

## Presentación

Vista lateral con ilusión de desplazamiento. El grupo permanece aproximadamente estable mientras se mueven terreno, fondos, vegetación, obstáculos y enemigos.

Las expediciones contienen los sprites y animaciones más detallados.

## Estructura

```text
Ciudad
→ trayecto
→ bosque
→ enemigo
→ evento
→ aldea
→ mazmorra
→ destino
→ regreso
```

Los segmentos pueden ser caminos, obstáculos, combates, ruinas, eventos sociales, aldeas, campamentos, recursos excepcionales o mazmorras.

## Ida y regreso

La expedición no termina visualmente al alcanzar el objetivo. Debe regresar o activar retorno de emergencia.

El regreso puede encontrar enemigos persistentes, rutas modificadas, escasez, heridos y pérdida de carga.

## Preparación

El jugador configura:

- Miembros.
- Formación.
- Posiciones.
- Roles.
- Habilidades automáticas.
- Prioridades.
- Equipo.
- Suministros.
- Retirada.
- Protección de heridos.
- Objetivo.

## Combate

No hay control directo de movimiento. El resultado depende de equipo, competencias, linajes, experiencia, formación, salud, recursos, decisiones y entorno.

## Equipo

- Se fabrica en la ciudad.
- Tiene calidad según planos, materiales y producción.
- Puede perderse.
- La carga solo regresa si el grupo logra transportarla.

## Derrota

Los habitantes vivos regresan sin equipo y con sus heridas. La ciudad debe tratarlos.

## Héroes

Solo los ciudadanos incorporados como héroes participan. Un ciudadano común puede ser elegido, entrenar, sobrevivir y acumular reconocimiento.

## Territorio

La expedición aumenta conocimiento, seguridad y acceso. Al completarse, trabajadores civiles pueden operar mediante rutas seguras sin representación expedicionaria detallada.

## Animaciones prioritarias

```text
idle
walk
run
attack_basic
skill
support
heal
hit
injured
downed
carry
teleport
victory
retreat
```
