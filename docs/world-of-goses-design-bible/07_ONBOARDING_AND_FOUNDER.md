# Onboarding y héroe fundador

## Propósito

El onboarding presenta el tono, determina el linaje, crea un fundador profundo y establece tendencias sin producir una build irreversible.

## Evitar preguntas directas

No preguntar:

```text
¿Te gusta minar?
¿Quieres ser sanador?
¿Prefieres espada o arco?
```

## Usar situaciones ambiguas

Ejemplo:

> Una tormenta destruye parte del refugio. Hay heridos, pocos suministros y una discusión sobre qué hacer primero.

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

```text
Linaje
Aptitudes personales
Afinidades profesionales
Afinidades elementales
Preferencias de armas
Rasgos
Orientación política
Postura espiritual
Tolerancia al riesgo
Estilo de liderazgo
```

No usar "resonancia con otro linaje" para explicar aptitudes personales.

## Linaje y personalidad

El linaje establece biología, historia cultural, afinidades, gramática visual e instituciones tempranas.

El test establece cómo el individuo interpreta esa cultura, qué aptitudes destacan, cómo combate y qué valores presenta.

## Elementos y armas

Todavía deben definirse.

No bloquean opciones futuras y no deben revelarse mediante preguntas obvias.

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

Su personalidad no debe convertirse en un bono eterno. Puede morir, retirarse, volverse mito, ser cuestionado o perder relevancia.

## Astral arrival canon

The founder was an astral consciousness travelling with one significant
person whose sex, gender, species, title, and exact relationship remain
undefined. A fracture separates them. The founder retains memory, but RaVAtha
must translate that memory into a mortal body that experiences linear time,
weight, pain, hunger, illness, ageing, material dependence, and death.

The founder does not create a lineage and is not its only member. The body is
reconstructed as one of the existing eight lineages. Lineage names, colours,
emblems, sprites, professions, physical weapons, future citizens, future
cities, politics, and numeric results remain hidden during transit.

## Implemented narrative sequence

The onboarding is one causal scene with twelve scored fragments:

```text
astral travel → fracture → separation → loss of the former body
→ memory translation → mortality → contact with RaVAtha
→ reconstruction → approach → impact
```

Answers determine the person who arrives, never the settlement's future.
Progress is presented as stabilised fragments. Scores stay hidden and every
answer is retained by a stable content id.

After the twelfth fragment, the result reveals only the selected lineage and
founder profile. The unscored final step asks for a 1–32 character founder name
and a Feminine/Masculine body presentation. Body presentation changes the
sprite only. A false final question is interrupted by “Ah. Ya llegamos.” before
the player can answer, followed by the fall into the existing first building
lot and a brief founder title card.

Content definitions, score contributions, answer session, result calculation,
Godot presentation, founder creation, and arrival transition are separate
implementation responsibilities. Replacing placeholder backgrounds, particles,
impact treatment, and title-card art must not change scoring or `Citizen`.

## Transparencia

El cálculo puede estar oculto durante el test, pero el resultado debe explicar linaje, rasgos, afinidades y tendencias mediante texto narrativo.
