# El Cubo Kovari

## Qué es

El Cubo Kovari es la forma en que el juego describe **de qué está hecho un
ciudadano**: tres parejas de valores continuos que dicen en qué se apoya
cuando actúa. No es una clase, no es un nivel y no es un poder que sólo los
Kovari puedan usar. Es el vocabulario cultural que las culturas Kovari
construyeron para describir predisposiciones corporales; los otros linajes
describen las mismas realidades con otros lenguajes (la Brújula de Vaelun, el
Relicario de Orveth, el Ciclo de Caelith).

> El ciudadano produce la capacidad. El equipo define cómo esa capacidad puede
> expresarse, cuánto exige y cuánto tiempo resiste.

## Qué problema jugable resuelve

Da a los ocho linajes una estructura común sin convertirlos en clases:
diferencia a dos ciudadanos de forma legible, alimenta estadísticas
explicables en lugar de un único `Power` opaco, y deja que la vida posterior
—competencias, heridas, equipo— importe más que el punto de partida.

## Autoridad

| Concepto | Autoridad |
| --- | --- |
| Los seis valores de un ciudadano | `FounderCubeProfile` (persistido en `Citizen`) |
| Vértice de un linaje | `CubeScoring.ComputeCubeVertex(LineageId)` |
| Matiz del onboarding | `CubeScoring.Recalculate(lineage, contributions)` |
| Cubo de un ciudadano corriente | `CubeScoring.GenerateOrdinaryProfile(lineage, seed)` |
| Expresión física | `CubeExpression.Derive(FounderCubeProfile)` |
| Familias naturales de arma | `NaturalWeaponFamilies.For(PhysicalExpression)` |
| Valor efectivo con equipo | `EffectiveCubeProfile.From(cube, gearSupport)` (derivado) |

La geometría es la del cubo: **8 vértices** son los ocho linajes, **6 caras**
son los seis valores del perfil, **3 ejes** son las tres parejas. Las 12
aristas son combinaciones intermedias y no tienen representación en código.

## Estructura

```text
FounderCubeProfile
├── Body      ↔ Bond        (¿la acción se apoya en el cuerpo y la materia,
│                            o en el vínculo entre personas y sistemas?)
├── Stability ↔ Impulse     (¿conservar y sostener, o intervenir y provocar?)
└── Domain    ↔ Reach       (¿concentrar en un foco, o extenderse en una red?)
```

Cada polo describe a qué contribuye, no qué prohíbe:

| Polo | Contribuye a |
| --- | --- |
| `Body` | capacidad material, daño físico, vida, defensa física, carga |
| `Bond` | canalización elemental, daño y resistencia elemental, curación, escudos |
| `Stability` | reducción de daño, regeneración, resistencia a estados e interrupción |
| `Impulse` | velocidad de ataque y lanzamiento, enfriamiento, iniciativa, esquiva |
| `Domain` | precisión, crítico, penetración, rendimiento contra un objetivo |
| `Reach` | distancia, área, número de objetivos, propagación, cobertura |

## Invariantes

1. **Cada pareja suma exactamente 100.** El constructor de
   `FounderCubeProfile` lo valida y rechaza el perfil si no cumple, igual que
   rechaza un valor fuera de `0..100`. No existe un perfil "parcial".
2. **El Cubo es identidad del `Citizen`.** Se persiste con la persona; no es
   un buff, ni un estado de sesión, ni una proyección recalculable desde otra
   cosa.
3. **El equipamiento no modifica el Cubo persistido.** El apoyo del equipo se
   suma al leerlo (`EffectiveCubeProfile`), que es un valor derivado y no se
   guarda. Un ciudadano que se desnuda vuelve exactamente a su Cubo.
4. **El linaje fija el vértice; el onboarding lo matiza.** El polo favorecido
   del linaje empieza en `60` y su opuesto en `40`
   (`CubeScoring.VertexHigh` / `VertexLow`). Las respuestas del onboarding
   desplazan cada eje como máximo `±8`
   (`CubeScoring.MaximumOnboardingShift`), así que un polo favorecido queda
   entre `52` y `68` y su opuesto entre `32` y `48`.
5. **El vértice sobrevive al matiz.** Como el desplazamiento no puede cruzar
   el centro, el linaje sigue siendo reconocible en el perfil y
   `CubeScoring.ComputeNearestVertex` devuelve el mismo linaje. Un Ardhen con
   `Body 52` sigue siendo Ardhen.
6. **La expresión física se deriva del Cubo, no de la afinidad.** Es la cara
   más alta del perfil. Función pura: el mismo Cubo siempre da la misma
   expresión.
7. **El desempate es explícito y observable.** `CubeExpression.CanonicalTieOrder`
   publica el orden `Body, Bond, Stability, Impulse, Domain, Reach` y la
   comparación es estricta, así que el resultado no depende del orden de
   iteración de un diccionario ni del orden de declaración de un enum.
8. **`PrimaryAffinity` y `PhysicalExpression` son ejes independientes.** La
   afinidad elemental no se calcula desde el Cubo y no restringe la expresión.
   Un fundador Ardhen puede ser `Fracture` con Fuego, `Paralysis` con Aire o
   `Bleeding` con Éter.
9. **Las coordenadas iniciales no son inmutables.** El linaje permanece; el
   perfil puede evolucionar por entrenamiento, salud, heridas, edad o
   decisiones extraordinarias. Lo que no puede es convertirse en seis clases
   disfrazadas ni bloquear armas, profesiones, elementos o roles.

## Cómo se manifiesta para el jugador

Las tres parejas se muestran en la tarjeta final del onboarding, en la
tarjeta de llegada del fundador y en el perfil del héroe, siempre como los
dos enteros de cada eje. Junto a ellas se muestran la expresión física
derivada y las dos familias de arma que esa expresión hace naturales.

Los números del Cubo **no** se muestran durante las doce elecciones: el
cálculo puede estar oculto mientras se juega, pero el resultado tiene que
poder explicarse.

Cualquier estadística importante que consuma el Cubo debe poder mostrar su
desglose por fuente en lugar de un número final sin origen:

```text
POTENCIA FÍSICA: 84.00

  Body base                 60.00
  Apoyo de equipamiento    +10.00
  Body efectivo             70.00
  Transferencia del arma    ×1.20
  Competencia               ×1.00
  Condición                 ×1.00
  Apoyo de ciudad           ×1.00
  ────────────────────────────────
  Potencia física           84.00
```

## Quién lo consume

- **Estadísticas derivadas** (`docs/systems/statistics-and-combat.md`): cada
  familia de stats lee caras concretas a través de `EffectiveCubeProfile`, con
  `CubeFaceCalculation` conservando base, apoyo y valor efectivo para el
  desglose.
- **Expresión física y armas**: `CubeExpression` → `PhysicalExpression` →
  `NaturalWeaponFamilies`, que es lo que restringe la elección de arma
  materializada del onboarding
  (`docs/systems/onboarding-and-founder.md`).
- **Aprendizaje de armas**: los tres niveles de eficiencia de XP se calculan
  desde la expresión del ciudadano y el vértice de su linaje, no desde una
  tabla por linaje.
- **Onboarding**: produce el Cubo del fundador y nada más
  (`FounderOnboardingResult`).
- **Reclutamiento**: cada migrante recibe un Cubo determinista a partir de
  `(linaje, id)`.

## Los ocho vértices

| Linaje | Eje I | Eje II | Eje III | Lectura |
| --- | --- | --- | --- | --- |
| Ardhen | Body | Stability | Domain | materia estable reunida en un punto de carga |
| Eirune | Body | Stability | Reach | vida preservada mediante redes |
| Kovari | Body | Impulse | Domain | intervención técnica precisa |
| Vaelun | Body | Impulse | Reach | movimiento material a través de rutas |
| Orveth | Bond | Stability | Domain | confianza y valor custodiados en acuerdos |
| Myrven | Bond | Stability | Reach | identidad sostenida por representaciones |
| Theryn | Bond | Impulse | Domain | intensidad colectiva enfocada |
| Caelith | Bond | Impulse | Reach | conocimiento conectado y aplicado en redes |

## De la cara a la expresión y al arma

| Cara más alta | `PhysicalExpression` | Familias naturales |
| --- | --- | --- |
| `Body` | `Fracture` | Hammer, Axe |
| `Bond` | `Poisoning` | Bow, Darts |
| `Stability` | `Paralysis` | Whip, Gauntlets |
| `Impulse` | `Stunning` | Mace, Orb |
| `Domain` | `Bleeding` | Sword, Daggers |
| `Reach` | `Knockdown` | Spear, Staff |

Como el desplazamiento del onboarding no cruza el centro, un linaje sólo
alcanza las **tres** expresiones de sus caras favorecidas
(`CubeExpression.NaturallyAvailableTo`). No hay lista negra: la geometría ya
lo garantiza.

## Ejemplos

**Fundador Ardhen matizado por el onboarding.**

```text
Vértice Ardhen        Body 60/40 Bond · Stability 60/40 Impulse · Domain 60/40 Reach
Respuestas            Body −4 · Stability +3 · Domain −7
Perfil resultante     Body 56/44 Bond · Stability 63/37 Impulse · Domain 53/47 Reach

Cara más alta         Stability (63)
Expresión física      Paralysis
Familias naturales    Whip, Gauntlets
```

El vértice Ardhen sigue siendo el más cercano, pero este fundador no es una
copia estadística de cualquier otro Ardhen: su cara dominante es `Stability`,
no `Body`.

**Ciudadano corriente reclutado.** `GenerateOrdinaryProfile` parte del mismo
vértice y aplica un desplazamiento `±8` por eje derivado con FNV-1a de
`(linaje, id)`. Es determinista —el mismo migrante siempre tiene el mismo
Cubo, sin guardar el desplazamiento— y hace que una población mixta alcance
las seis expresiones físicas en lugar de repetir una sola.

**Un mismo Cubo con dos afinidades.** Dos fundadores Kovari con
`Body 62 / Impulse 58 / Domain 66` tienen los dos la expresión `Bleeding` y
las mismas familias naturales. Si uno resuena con Fuego y el otro con Agua,
su canal elemental se manifiesta de forma distinta sin que ninguna de las dos
sea mejor: la afinidad decide la naturaleza de la manifestación, no un
multiplicador.

## Persistencia

El Cubo se guarda por ciudadano (`FounderCubeProfileSave`): el fundador desde
el esquema v29, todos los ciudadanos desde v30. En v32 la tercera cara pasó a
llamarse `Domain` en disco —el nombre `Mastery` quedó reservado para los
niveles de maestría por familia de arma— y el campo antiguo se conservó un
bump como puente nullable, así que un save anterior carga sin perder el cubo
del fundador.

Nunca se persisten `EffectiveCubeProfile`, la expresión física ni las
familias naturales: son funciones del Cubo, y guardarlas sólo crearía una
copia que puede desviarse. Una ciudad guardada antes de que la expresión
pasara a derivarse del Cubo (2026-08-07) carga con una expresión distinta a
la que tenía; no hizo falta un bump de esquema porque nunca fue un campo.

## Lo que el Cubo no hace

- No otorga penalizaciones permanentes por linaje.
- No bloquea armas, profesiones, elementos ni roles.
- No asigna preferencias de arma durante el onboarding: la elección de arma
  materializada es explícita y sólo se restringe a las dos familias naturales
  de la expresión ya derivada.
- No decide la política, la cultura ni el destino de la ciudad.
- No se resume en un único número de poder.

La armadura todavía no participa: `PersonalEquipment` sólo modela el arma
equipada, y masa, integridad y desgaste están trazados en
[#39](https://github.com/3FE3LE/world-of-goses/issues/39).
