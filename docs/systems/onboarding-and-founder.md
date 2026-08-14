# Onboarding y fundador

## Qué es

Una escena causal única que va desde el viaje astral hasta la caída en el
solar inicial. Durante ella el jugador contesta doce fragmentos narrativos,
nombra a la persona que llega, ve su perfil y elige el arma con la que su
cuerpo se materializa. El resultado es **un `Citizen` persistente con rol de
héroe**, no una hoja de personaje aparte.

## Qué problema jugable resuelve

Es el primer contacto con el lore y la única fuente de la identidad del
fundador. Tiene que producir una persona con predisposiciones legibles sin
producir una build irreversible ni prometerle al jugador una profesión, una
ideología o un destino.

## Flujo implementado

```text
12 fragmentos puntuados
→ identidad (nombre 1–32 caracteres + presentación corporal)
→ Founder Card (linaje, afinidad, tres ejes del Cubo, resumen)
→ elección de arma (dos familias naturales de su expresión física)
→ pregunta falsa, interrumpida por «Ah. Ya llegamos.»
→ caída sobre el primer solar + materialización del fundador y su arma
→ tarjeta de título
```

La secuencia narrativa que envuelve esos pasos es: viaje astral → fractura →
separación → pérdida del cuerpo anterior → traducción de la memoria →
mortalidad → contacto con RaVAtha → reconstrucción → aproximación → impacto.

## Autoridad

| Concepto | Autoridad |
| --- | --- |
| Contenido de los fragmentos y respuestas | `FounderNarrativeCatalog` |
| Resultado mecánico | `FounderOnboardingResult` |
| Cubo del fundador | `CubeScoring.Recalculate` |
| Expresión física derivada | `CubeExpression.Derive` |
| Familias elegibles | `NaturalWeaponFamilies.For(expression)` |
| Arma materializada | `CitizenEquipmentService.MaterializeStarterWeapon` |
| Petición de creación | `HeroCreationRequest` |
| Presentación | `AstralOnboardingView` (etapas `Question`, `Identity`, `FounderCard`, `WeaponChoice`, `FalseQuestion`) |

## Salida mecánica

```csharp
FounderOnboardingResult(
    LineageId Lineage,
    ElementalAffinity ElementalAffinity,
    FounderCubeProfile CubeProfile,
    FounderNarrativeMemory NarrativeMemory)
```

Y, por separado, la familia elegida viaja en
`HeroCreationRequest.MaterializedWeaponFamily`.

El onboarding produce **eso y nada más**. No produce aptitudes, profesiones,
clases de combate, orientación política, postura espiritual, tolerancia al
riesgo ni estilo de liderazgo. Rasgos, preferencias y competencias aparecen
después, como consecuencia de la vida del ciudadano.

## Invariantes

1. **Doce fragmentos, ningún número visible.** El cálculo puede estar oculto
   mientras se juega; el resultado tiene que poder explicarse. La Founder Card
   muestra exactamente nombre, presentación corporal, sprite, linaje, afinidad
   elemental, los tres ejes del Cubo y un resumen narrativo — y nada de arma
   preferida, profesión recomendada, clase, rol, ideología o rasgo sin
   mecánica.
2. **Las preguntas nunca son directas.** «¿Prefieres espada o arco?» está
   prohibido; la situación ambigua es la herramienta. Cada respuesta puede
   contribuir a varios linajes, a varios polos del Cubo, a una afinidad y a un
   eco narrativo a la vez.
3. **Identidad persistida por id estable.** Se guardan `question_id` y
   `answer_id`, nunca índices visuales.
4. **Recalcular desde cero al cambiar una respuesta.** Se limpian los
   acumuladores y se recorren las respuestas seleccionadas; nunca se resta a
   mano la contribución anterior.
5. **La presentación corporal cambia el sprite y nada más.**
6. **La elección de arma está restringida a la expresión ya derivada.** Sólo
   las dos familias de `NaturalWeaponFamilies` son ofrecidas, y una familia
   fuera de ese conjunto se rechaza sin cambiar nada. La elección llega
   *después* de la Founder Card porque es consecuencia del Cubo, no una
   entrada del cálculo.
7. **El arma materializada es un item real.** `MaterializeStarterWeapon` crea
   un `WeaponItemInstance` con `ItemInstanceId` propio, `WeaponOrigin.FounderMaterialization`
   y su `WeaponChannelProfile`, lo registra en `PersonalEquipment` y lo equipa
   a través de una única autoridad, que republica el `EquipmentLoadout`. No es
   una etiqueta ni un `WeaponPreference`.
8. **Sin elección no hay arma.** Un `HeroCreationRequest` sin familia deja al
   fundador desarmado; nada la inventa por él.
9. **El fundador no funda un linaje.** Su cuerpo se reconstruye como uno de
   los ocho existentes y no es su único miembro. Nombres, colores, emblemas,
   sprites, profesiones y resultados numéricos de los linajes permanecen
   ocultos durante el tránsito.
10. **El fundador es un `Citizen`.** Puede enfermar, herirse, envejecer,
    enseñar, retirarse, ser cuestionado y morir. Su muerte cierra su campaña
    personal, no la ciudad. Su personalidad no puede convertirse en un bono
    eterno.
11. **Ninguna decisión del onboarding prescribe el futuro de la ciudad.**
    Puede influir en tema de UI, estilo arquitectónico, instituciones
    tempranas, prestigio profesional, políticas, cultura expedicionaria y
    relato fundacional — nunca en un multiplicador permanente.

## Restricciones del prólogo

- No nombrar los ocho linajes antes del resultado.
- No mostrar las ocho civilizaciones observando el cielo.
- No confirmar una profecía ni afirmar que el jugador es un elegido.
- No revelar la causa de la separación ni explicar el Centro con texto
  expositivo.
- No mostrar multitudes.
- No convertir la caída en una explosión de escala mundial: el impacto es
  violento y localizado, y el terreno responde como si algo hubiera encajado
  en un lugar preparado o incompleto.
- No introducir bonificaciones numéricas dentro de la escena narrativa.

## Las siete escenas

1. **Antes del cielo.** Pantalla oscura, sin música, ruido estelar profundo.
   Dos luces viajan juntas sin perseguirse. La cámara no determina escala.
2. **Interferencia.** El ruido pierde continuidad; un impacto grave. Aparecen
   filamentos y esquirlas. La pantalla se fragmenta en planos breves y cada
   plano contiene una pregunta que surge de la transición, no de una voz
   reconocible.
3. **La separación.** Entre preguntas hay destellos de la otra presencia. Las
   dos luces intentan cruzar una abertura central; la abertura se cierra. La
   otra presencia desaparece y **no se confirma si fue destruida**.
4. **El cielo de Ravatha.** Desde la materia, las dos luces son cuerpos
   incandescentes que se cruzan sobre una región oscura del centro del
   continente. Una estela continúa; la otra queda fuera de cuadro.
5. **La caída.** El paisaje aparece por fragmentos: cordilleras, cursos de
   agua, caminos antiguos que terminan antes del Centro, ruinas
   semienterradas. El cuerpo se define durante el descenso y el sprite
   completo se revela al tocar tierra.
6. **Impacto.** Sin cráter gigantesco. Unos segundos inmóvil; la primera
   interacción es levantarse, la segunda mirar el cielo. La otra presencia no
   aparece.
7. **Espera.** El fundador no empieza construyendo una casa: construye una
   fogata, que es una señal y no progreso tecnológico. La primera noche
   continúa en [`first-night.md`](first-night.md).

## Lo que el jugador todavía no sabe

Los habitantes de Ravatha vieron dos cuerpos celestes acercarse al Centro y
producir una anomalía, y cada civilización registró un efecto distinto la
misma noche: instrumentos Vaelun que cambiaron de dirección, organismos
Eirune fuera de ciclo, observatorios Caelith que perdieron una fase, artefactos
Kovari en configuraciones imposibles, anclajes Ardhen con carga sin contacto,
ceremonias Theryn en disonancia, máscaras Myrven fracturadas, registros Orveth
alterados. Nada de esto aparece en el prólogo: se descubre al contactar con
esas sociedades.

La segunda conciencia fue refractada. Puede haber caído en otra región, en
otro momento, en otro cuerpo, fragmentada entre varios fenómenos o retenida en
el Centro. El juego debe ofrecer evidencias contradictorias antes de confirmar
una respuesta.

## Función narrativa de la ciudad

El fundador no llega a conquistar ni a cumplir una profecía. Permanece porque
espera:

```text
esperar → encender una señal → sobrevivir → construir refugio
→ recibir a otra persona → formar un asentamiento
→ atraer doctrinas y reclamaciones → fundar una ciudad
```

## Persistencia y compatibilidad

El resultado canónico del onboarding se persiste desde el esquema v29; el arma
del fundador tiene identidad de item desde v35, donde cada `EquipmentLoadout.Weapon`
existente migró a un `WeaponItemInstance` registrado bajo un `ItemInstanceId`.

Un save antiguo genuinamente desarmado **sigue desarmado**: la migración no
inventa un arma que su jugador nunca eligió. Para que ese fundador pueda pelear
su primer encuentro, el combate le presta un canal determinista y no
persistente derivado de la identidad de la expedición
(`ExpeditionCombatSessionFactory.OpeningBaselineFor`). Es compatibilidad, no
equipo: no muta el loadout, no se guarda, y un arma real siempre tiene
precedencia.

Un onboarding nunca se vuelve a ejecutar automáticamente sobre una partida
válida.
