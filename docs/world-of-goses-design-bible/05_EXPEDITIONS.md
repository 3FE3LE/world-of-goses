# Expediciones

## Propósito

Las expediciones permiten explorar, abrir rutas, revelar recursos, encontrar planos, descubrir tecnologías, contactar poblaciones, eliminar amenazas y habilitar parcelas.

## Presentación

La expedición se proyecta sobre la **misma gramática espacial de bandas de
profundidad que la ciudad macro** (ver `docs/ARCHITECTURE.md` §10 "Spatial
grammar"). Compartimos vocabulario de profundidad y muestreo de terreno con
el macro pero **no** el renderer concreto: ciudad y expedición conservan
políticas de cámara independientes.

El recorrido es una franja en perspectiva (no 2.5D real, no 3D): rear depth
bands, banda jugable, foreground band. El grupo permanece aproximadamente
estable mientras el mundo se desplaza bajo él. El infinito del camino es
una ilusión visual lograda con reciclado de segmentos, nunca un mundo
infinito persistido.

Las expediciones contienen los sprites y animaciones más detallados.

La inspiración estructural para la legibilidad de combate es
*Taskbar Hero*: combate automático legible dentro de una franja persistente.
No se copian assets, interfaz, personajes, nombres ni contenido. La
composición final debe ser original y obedecer la identidad visual de este
proyecto.

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

La vanguardia admite como máximo **cuatro `Citizen`** en su dirección futura.
La primera expedición es una excepción de onboarding: participa únicamente el
Founder y los slots 2–4 permanecen visibles pero bloqueados, para enseñar la
capacidad futura sin inventar miembros ni adelantar reclutamiento.

La superficie de habilidades muestra cuatro slots. En el primer slice solo
Skill 1 está conectada; Basic Attack es automática y no ocupa uno de estos
inputs. Las acciones previstas son:

```text
expedition_skill_1
expedition_skill_2
expedition_skill_3
expedition_skill_4
```

Los cuatro slots se representan como octágonos. Cada octágono debe conservar
ocho lados capaces de alojar más adelante un Trait asociado a cada lado. Esta
reserva geométrica no autoriza a implementar Traits todavía.

## Combate

No hay control directo de movimiento. El resultado depende de equipo, competencias, linajes, experiencia, formación, salud, recursos, decisiones y entorno.

El desplazamiento también es automático. Un combatiente avanza solamente
hasta entrar en su `AttackRange`. Una vez puede atacar, no retrocede
voluntariamente para conservar rango: un combatiente ranged no kitea.

Basic Attack se ejecuta automáticamente. Las Active Skills expresan la
intervención configurada del jugador mediante los cuatro inputs previstos; el
primer slice conecta únicamente `expedition_skill_1`.

Knockback puede desplazar a un combatiente aunque ya estuviera en rango.
`Stability` reduce ese desplazamiento y `Impulse` puede aumentar el
desplazamiento producido. Los coeficientes definitivos permanecen abiertos y
deben vivir en dominio determinista, no en la animación.

No forman parte del primer combate visual: Traits, Chains, carroza, acción en
`SPACE`, formación avanzada ni Skills 2–4 funcionales.

## Primer Spirit Trail

El primer combate visual aparece durante los primeros cinco minutos
aproximados de gameplay, dentro de esta secuencia:

```text
Onboarding astral
→ primera noche
→ amanecer / SpiritDeparted
→ Spirit Trail disponible
→ primera expedición del Founder
→ primer encuentro visual
→ continuación hacia el objetivo
→ regreso a la ciudad
```

`SpiritTrailSearch` representa seguir el rastro del espíritu y progresar
narrativamente. Ya no representa `1 Food → Wood`. La primera ruta dura
aproximadamente cuatro horas de mundo y una expedición de esa duración no
consume Food solo por existir. Su recompensa material definitiva permanece
abierta; no debe inventarse una conversión provisional para llenar ese vacío.

La ciudad, el viaje y el combate continúan avanzando en paralelo bajo el único
reloj del mundo. Entrar o salir del stage de expedición no modifica la
velocidad.

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
