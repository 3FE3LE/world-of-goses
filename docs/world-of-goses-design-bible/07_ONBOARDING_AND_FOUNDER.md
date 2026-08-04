# Onboarding y héroe fundador

## Propósito

El onboarding presenta el tono, determina el linaje, crea un fundador
profundo y establece predisposiciones sin producir una build
irreversible. La secuencia narrativa es uno de los principales
contactos del jugador con World of Goses y su primera fuente de lore.

El sistema mecánico del resultado vive en
[`bible/13_KOVARI_CUBE.md`](13_KOVARI_CUBE.md) § *Integración con
onboarding*. El detalle narrativo de cada linaje vive en
[`bible/06_LINEAGES.md`](06_LINEAGES.md) y en los capítulos por linaje
[`bible/14-21`](06_LINEAGES.md#dónde-profundizar).

## Evitar preguntas directas

No preguntar:

```text
¿Te gusta minar?
¿Quieres ser sanador?
¿Prefieres espada o arco?
```

## Usar situaciones ambiguas

Ejemplo:

> Una tormenta destruye parte del refugio. Hay heridos, pocos
> suministros y una discusión sobre qué hacer primero.

Posibles respuestas:

- Reforzar la estructura.
- Atender sistemas vitales.
- Desmontar restos y crear una solución temporal.
- Separar testimonios y descubrir información oculta.
- Buscar una ruta alternativa.
- Inventariar y distribuir reservas.
- Investigar por qué falló.
- Calmar al grupo y recuperar coordinación.

Cada respuesta alimenta varios valores ocultos.

## Resultado

El onboarding produce **únicamente** un perfil mecánico del Cubo. **No**
asigna preferencias de armas, profesiones futuras, clases de combate,
orientación política, postura espiritual, tolerancia al riesgo ni
estilo de liderazgo. Esos campos se eliminan del output.

Los rasgos, preferencias, competencias, profesiones y estilos de
combate deben aparecer posteriormente como consecuencia de la vida del
ciudadano (ver `bible/04` § *Cinco capas de competencia*).

### Salida mecánica canónica

```csharp
public sealed record FounderOnboardingResult(
    LineageId Lineage,
    ElementalAffinity ElementalAffinity,
    FounderCubeProfile CubeProfile,
    FounderNarrativeMemory NarrativeMemory
);

public sealed record FounderCubeProfile(
    int Body,
    int Bond,
    int Stability,
    int Impulse,
    int Mastery,
    int Reach
);

public sealed record FounderNarrativeMemory(
    IReadOnlyList<string> AnswerIds,
    string? BelievedFinalWordId,
    string? PreservedDetailId,
    IReadOnlyList<string> EchoIds
);
```

Los nombres exactos deben respetar las convenciones del repositorio
(`bible/04`, `docs/REPOSITORY_CONVENTIONS.md`).

- **`Lineage`** — uno de los ocho linajes.
- **`ElementalAffinity`** — una de las seis caras del cubo: Tierra,
  Éter, Agua, Fuego, Neutra/Silencio, Aire. Independiente del linaje.
- **`CubeProfile`** — coordenadas continuas (no clases) sobre los tres
  ejes del Cubo: Cuerpo↔Vínculo, Estabilidad↔Impulso, Dominio↔Alcance.
  Se ancla en 60/40 por eje desde el vértice del linaje y admite ±8 de
  variación por las respuestas. Las reglas completas viven en
  [`bible/13_KOVARI_CUBE.md`](13_KOVARI_CUBE.md) § *Bonificación
  inicial — modo sombra*.
- **`NarrativeMemory`** — IDs estables de las doce respuestas, más la
  palabra que el fundador creyó escuchar y los ecos narrativos de la
  caída.

### Reglas de cálculo

- **Modo sombra durante la primera integración.** El cubo se calcula en
  paralelo al scoring actual de linaje; el algoritmo actual permanece
  como fuente de verdad hasta demostrar paridad.
- **Recalcular desde cero** cuando el jugador cambie una respuesta.
  Limpiar acumuladores, recorrer respuestas seleccionadas, aplicar
  contribuciones, calcular resultados. **No restar manualmente**
  contribuciones anteriores.
- **IDs estables** (`question_id`, `answer_id`). No usar índices
  visuales como identidad persistente.
- **No** mostrar los números del cubo durante las doce elecciones. El
  resultado revelado en la pantalla final muestra el cubo y la
  afinidad; no muestra porcentajes ni comparaciones numéricas.

### Pantalla final del onboarding

Mostrar:

```text
Nombre
Presentación corporal
Sprite
Linaje
Afinidad elemental
Tres ejes del Cubo (Cuerpo/Vínculo, Estabilidad/Impulso, Dominio/Alcance)
Resumen narrativo breve
```

No mostrar:

```text
Arma preferida
Profesión recomendada
Clase
Rol de expedición
Ideología
Destino político
Rasgos sin mecánica
```

Ejemplo:

```text
AREL

Linaje corporal
ARDHEN

Afinidad
AIRE

PERFIL DE ENCARNACIÓN

Cuerpo       56 / 44 Vínculo
Estabilidad  63 / 37 Impulso
Dominio      53 / 47 Alcance
```

El resumen describe el perfil, no promete una profesión o estilo de
juego.

## Linaje y personalidad

El linaje establece biología, historia cultural, afinidades, gramática
visual e instituciones tempranas. El test establece cómo el individuo
interpreta esa cultura, con qué predisposiciones comienza y qué memorias
arrastra de la caída. **No** prescribe comportamientos políticos ni
preferencias de armas.

## Elementos y armas

Las afinidades elementales se definen por primera vez aquí. Son las
seis caras del cubo: Tierra, Éter, Agua, Fuego, Neutra/Silencio, Aire.
La afinidad no bloquea opciones futuras y no debe revelarse mediante
preguntas obvias.

Las preferencias de armas se forman con el uso durante la vida del
ciudadano. El Cubo puede hacer ciertas familias inicialmente más
compatibles, pero no favoritas.

## Influencia en la ciudad

El fundador afecta:

- Tema inicial de UI.
- Estilo arquitectónico.
- Instituciones tempranas.
- Prestigio profesional.
- Conocimiento.
- Políticas.
- Cultura expedicionaria.
- Relato fundacional.

Su personalidad no debe convertirse en un bono eterno. Puede morir,
retirarse, volverse mito, ser cuestionado o perder relevancia. Esta
regla es invariante (`bible/07` y `CROSS_DOMAIN_INVARIANTS.md`
→ Citizens).

## Astral arrival canon

The founder was an astral consciousness travelling with one significant
person whose sex, gender, species, title, and exact relationship remain
undefined. A fracture separates them. The founder retains memory, but
RaVAtha must translate that memory into a mortal body that experiences
linear time, weight, pain, hunger, illness, ageing, material
dependence, and death.

The founder does not create a lineage and is not its only member. The
body is reconstructed as one of the existing eight lineages. Lineage
names, colours, emblems, sprites, professions, physical weapons, future
citizens, future cities, politics, and numeric results remain hidden
during transit.

## Implemented narrative sequence

The onboarding is one causal scene with twelve scored fragments:

```text
astral travel → fracture → separation → loss of the former body
→ memory translation → mortality → contact with RaVAtha
→ reconstruction → approach → impact
```

Answers determine the person who arrives, never the settlement's future.
Progress is presented as stabilised fragments. Scores stay hidden and
every answer is retained by a stable content id.

After the twelfth fragment, the result reveals only the selected
lineage, elemental affinity, cube profile and the founder's name. The
unscored final step asks for a 1–32 character founder name and a
Feminine/Masculine body presentation. Body presentation changes the
sprite only. A false final question is interrupted by "Ah. Ya llegamos."
before the player can answer, followed by the fall into the existing
first building lot and a brief founder title card.

Content definitions, score contributions, answer session, result
calculation, Godot presentation, founder creation, and arrival
transition are separate implementation responsibilities. Replacing
placeholder backgrounds, particles, impact treatment, and title-card
art must not change scoring or `Citizen`.

### Siete escenas del prólogo

> Esta sección amplía el resumen canónico con el detalle narrativo de
> la secuencia. Fuente original archivada en
> [`docs/_archive/ravatha-source-2026-08-04/ravatha_lore_package/ravatha_lore_package/01_PROLOGUE_THE_FALL.md`](../_archive/ravatha-source-2026-08-04/ravatha_lore_package/ravatha_lore_package/01_PROLOGUE_THE_FALL.md).

1. **Antes del cielo.** Pantalla oscura, sin música. Un ruido estelar
   profundo: textura baja, irregular y distante. Aparecen dos luces
   que viajan juntas, cercanas, sin perseguirse. La cámara no
   determina escala.
2. **Interferencia.** El ruido estelar pierde continuidad: vibración
   breve, después un impacto sonoro grave. Aparecen filamentos y
   esquirlas. Las dos presencias intentan mantenerse juntas; no lo
   consiguen. La pantalla se fragmenta en planos breves. Cada plano
   contiene una pregunta del onboarding. Las preguntas no son
   formuladas por una voz reconocible: surgen de la propia transición.
3. **La separación.** Entre una pregunta y la siguiente aparecen
   destellos de la otra presencia. En el último instante, ambas luces
   intentan cruzar una abertura central; la abertura se cierra o
   cambia de forma. La otra presencia desaparece. **No debe
   confirmarse si fue destruida.**
4. **El cielo de Ravatha.** Desde la perspectiva material, las dos
   luces son ahora cuerpos incandescentes. Sus trayectorias se cruzan
   sobre una región oscura en el centro del continente. Hay un pulso
   que curva nubes, levanta polvo y produce círculos de luz. Una
   estela continúa; la otra queda fuera de cuadro. La cámara sigue
   únicamente a la conciencia del jugador.
5. **La caída.** La velocidad aumenta. Aparece por fragmentos el
   paisaje: cordilleras, cursos de agua, caminos antiguos que terminan
   antes de llegar al Centro, ruinas semienterradas, estructuras
   orientadas hacia el punto de impacto. El cuerpo termina de
   definirse durante el descenso; el sprite completo se revela solo al
   tocar tierra.
6. **Impacto.** No hay cráter gigantesco. El impacto es violento pero
   localizado. El terreno responde como si algo hubiera encajado en un
   lugar preparado o incompleto. Durante algunos segundos el fundador
   permanece inmóvil. La primera interacción es levantarse; la
   segunda, mirar el cielo. La otra presencia no aparece.
7. **Espera.** El personaje encuentra fragmentos, ramas o materiales
   mínimos. **No empieza construyendo una casa.** Construye una fogata.
   La fogata no representa progreso tecnológico; es una señal. La
   primera noche concluye con el fundador sentado frente al fuego,
   mirando alternativamente el cielo y los límites oscuros de la
   región. La cámara se aleja. El punto de luz queda en el centro
   exacto del mapa inicial. Aparece el título: **WORLD OF GOSES**.

### Información que el jugador no conoce durante el prólogo

Los habitantes de Ravatha vieron dos cuerpos celestes aproximarse al
Centro y producir una anomalía. Cada civilización registró efectos
diferentes la misma noche:

- instrumentos Vaelun cambiaron de dirección;
- organismos Eirune reaccionaron fuera de ciclo;
- observatorios Caelith perdieron una fase de sus modelos;
- artefactos Kovari adoptaron configuraciones imposibles;
- anclajes Ardhen registraron una carga sin contacto;
- ceremonias Theryn sufrieron una disonancia;
- máscaras Myrven mostraron fracturas o reflejos contradictorios;
- custodios Orveth detectaron alteraciones en reliquias, medidas o
  registros.

Nada de esto aparece en el prólogo. Se descubre cuando el jugador entra
en contacto con esas sociedades.

La segunda conciencia fue refractada. Puede haber caído en otra región,
en otro momento, dentro de otro cuerpo, fragmentada entre varios
fenómenos, o retenida en el propio Centro. El juego debe proporcionar
evidencias contradictorias antes de confirmar una respuesta.

### Función narrativa de la ciudad

El fundador no llega para conquistar, gobernar ni cumplir una profecía.
Permanece porque espera:

```text
Esperar
→ encender una señal
→ sobrevivir
→ construir refugio
→ recibir a otra persona
→ formar un asentamiento
→ atraer doctrinas y reclamaciones
→ fundar una ciudad
```

La ciudad nace de una decisión íntima y se transforma en un conflicto
continental.

### Vida, muerte y continuidad

El fundador es un `Citizen`, no una entidad separada del mundo. Debe
poder enfermar, sufrir heridas, envejecer, formar relaciones, tener
hijos o adoptar, enseñar, gobernar o retirarse, ser cuestionado, morir.
La muerte del fundador cierra su campaña personal, pero no elimina la
ciudad. La permanencia o inmortalidad puede ofrecerse como una ruta
extraordinaria, con el coste de renunciar a la reproducción y observar
el reemplazo de generaciones.

### Restricciones del prólogo

- **No** nombrar los ocho linajes antes del resultado.
- **No** mostrar las ocho civilizaciones observando el cielo.
- **No** confirmar una profecía.
- **No** afirmar que el jugador sea un elegido.
- **No** revelar la causa de la separación.
- **No** explicar el Centro mediante texto expositivo.
- **No** mostrar multitudes.
- **No** convertir la caída en una explosión destructiva de escala
  mundial.
- **No** introducir bonificaciones numéricas en la escena narrativa.

## Migración y fallback

Los datos antiguos del fundador pueden contener
`Traits`, `WeaponPreferences`, `ProfessionalAffinities`, `CombatStyle`,
`RiskProfile`, `LeadershipStyle`, `PoliticalOrientation` o
`SpiritualPosture`. La estrategia completa de migración y el fallback
están en [`bible/13_KOVARI_CUBE.md`](13_KOVARI_CUBE.md) § *Migración
y fallback*.

## Transparencia

El cálculo puede estar oculto durante el test, pero el resultado debe
explicar el linaje, los tres ejes del cubo, la afinidad y la memoria
narrativa mediante texto narrativo.
