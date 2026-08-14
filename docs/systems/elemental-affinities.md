# Afinidades elementales

## Qué es

`PrimaryAffinity` es la fuerza de Ravatha con la que un ciudadano resuena:
`Earth`, `Water`, `Fire`, `Air`, `Aether` o `Silence`. Es identidad de la
persona, se decide en el onboarding y no cambia. No es un elemento de daño ni
un multiplicador: **decide la naturaleza de una manifestación, no su tamaño**.

## Qué problema jugable resuelve

Da a cada ciudadano una manera propia de intervenir en los procesos del mundo
sin convertirlo en una clase, y da al diseño un vocabulario común entre
onboarding, Cubo, equipo, ambiente, ciudad, salud, expediciones y combate.

## Autoridad

| Concepto | Autoridad |
| --- | --- |
| Las seis afinidades | `ElementalAffinity` |
| Afinidad de una persona | `CitizenProfile` / `FounderOnboardingResult` (persistida) |
| Canal elemental del arma | `WeaponChannelProfile.ElementalResonance` |
| Contribución a estadísticas | `docs/systems/statistics-and-combat.md` |

Las seis afinidades corresponden a las seis caras del Cubo, pero la
correspondencia es **nominal**: la afinidad no se calcula desde el Cubo y el
Cubo no se calcula desde la afinidad. Son ejes independientes
([`kovari-cube.md`](kovari-cube.md), invariante 8).

## Principios

1. **La afinidad pertenece al ciudadano.** La persona es la fuente de
   capacidad, control, aprendizaje, intención, tolerancia a la carga y
   experiencia. Una herramienta puede transmitir mejor o peor una afinidad,
   soportar más o menos carga, conservar o dispersar una manifestación y
   desgastarse al canalizarla — pero **no otorga daño elemental base por
   existir**.
2. **Afinidad no significa dominio.** El onboarding asigna una afinidad, no
   técnicas, control experto, una profesión ni inmunidad al propio elemento.
   Un fundador de Fuego puede no saber encender una llama de forma segura; un
   ciudadano de Agua puede ser mal agricultor. El dominio se adquiere con
   práctica, educación, instituciones, herramientas, mentores y consecuencias
   vividas.
3. **Afinidad y linaje son independientes.** Ningún linaje fuerza una
   afinidad y ninguna afinidad bloquea manifestaciones futuras. Ardhen de
   Aire, Eirune de Fuego, Vaelun de Silencio y Caelith de Tierra son todos
   válidos.
4. **Los elementos no son morales.** Ninguna afinidad es buena, malvada,
   civilizada, salvaje, regenerativa ni extractiva. Cada una admite usos
   regenerativos, extractivos, defensivos, ofensivos, productivos y
   destructivos.
5. **No hay rueda rígida de debilidades.** «Agua vence a Fuego» no es una
   regla del juego. Las interacciones dependen de escala, contexto, estado
   ambiental, material, técnica, intensidad, duración, preparación y
   combinación con otros procesos: el Agua puede apagar el Fuego, y también
   convertirse en vapor, transportar calor o provocar una explosión de
   presión.
6. **Toda consecuencia es explícita.** La afinidad puede ser profunda sin
   volverse niebla mística: el jugador debe poder inspeccionar qué produjo un
   efecto, quién, con qué estadística, canalizado por qué herramienta, con
   cuánto desgaste y con qué coste o riesgo.

## Un solo canal elemental

`ElementalResonance` es **universal**: un único coeficiente por arma que
multiplica la capacidad expresada a través de `Bond`, válido para las seis
afinidades. No existen `EarthResonance`, `WaterResonance` ni ninguna
resonancia por elemento, y no existen `FireDamage` ni `WaterDamage` como
estadísticas separadas.

La expansión hacia tolerancia elemental, estados ambientales por parcela y
manifestaciones de ciudad está trazada en
[#42](https://github.com/3FE3LE/world-of-goses/issues/42).

## Las seis afinidades

Cada bloque dice qué representa la afinidad, en qué puede manifestarse
—porque ninguna se reduce a su imagen obvia— y qué riesgos trae su uso.

### Earth — estructura la materia

Estructura, masa, cohesión, presión, soporte, contacto material, permanencia
física. No es solamente roca: también suelo, minerales, cerámica, metal,
hueso, cimentación, compactación y distribución de cargas.

*Riesgos:* compactación excesiva, pérdida de porosidad, fractura,
hundimiento, aumento de peso, rigidez, bloqueo de ciclos vivos.

### Water — regula la continuidad

Regulación, continuidad, transporte, adaptación, absorción, intercambio
térmico, circulación. No es solamente líquido visible: también humedad,
circulación corporal, disolución, enfriamiento, limpieza, transporte de
nutrientes, control de temperatura y cambios de estado.

*Riesgos:* saturación, corrosión, erosión, contaminación transportada,
proliferación de enfermedades, pérdida de tensión en materiales, inundación.

### Fire — acelera la transformación

Transformación, aceleración, consumo, calor, liberación de energía, reacción,
cambio irreversible. No es solamente una llama visible: también combustión,
cocción, fundición, esterilización, metabolismo, presión térmica y reacciones
químicas.

*Riesgos:* propagación, agotamiento de combustible, humo, pérdida de humedad,
deformación, contaminación, daño irreversible, aceleración fuera de control.

### Air — propaga el movimiento

Movimiento, presión, propagación, intercambio gaseoso, distancia, ventilación,
liberación. No es solamente viento: también respiración, presión, sonido,
clima, transporte de partículas, secado, sustentación y dispersión.

*Riesgos:* propagación de humo, plagas o enfermedad; erosión, desecación,
pérdida de calor, turbulencia, amplificación de incendios, colapso por
presión.

### Aether — conecta estados separados

Relación, conexión, transmisión, continuidad entre entidades separadas,
resonancia, información, interacción astral. **No es magia genérica ni un
elemento superior**, y no crea aquello que conecta: necesita dos o más
estados, un origen y un destino, una relación existente o construible, un
medio, control y tolerancia a la interferencia.

*Riesgos:* interferencia, propagación de fallos, resonancia en cascada,
pérdida de identidad, contaminación entre sistemas, dependencia de red,
inestabilidad astral.

### Silence — preserva los límites

Aislamiento, amortiguación, neutralización, separación, precisión basal,
ausencia controlada de resonancia. **No es ausencia de afinidad** ni
antimagia universal, y no destruye automáticamente ninguna manifestación:
crea condiciones donde un proceso puede dejar de propagarse, reducir
interferencia, ser observado aislado, conservarse, estabilizarse o terminar
sin contaminar otros sistemas.

*Riesgos:* esterilidad, estancamiento, pérdida de señales útiles, aislamiento
prolongado, supresión de recuperación, acumulación oculta de problemas,
ruptura de redes vivas.

## Tensiones, no enemigos

Las caras opuestas del Cubo dan tres tensiones. Ninguna es una relación de
ventaja: cada una admite conflicto y sinergia.

| Tensión | Pregunta de un polo | Pregunta del otro | Conflicto | Sinergia |
| --- | --- | --- | --- | --- |
| Earth ↔ Aether | ¿qué sostiene físicamente este proceso? | ¿qué relaciones lo mantienen entre entidades separadas? | una red eficiente supera la capacidad material de sus soportes; una estructura cerrada impide conexión | redes con soporte estable, puentes que reparten carga e información, infraestructura astral anclada |
| Water ↔ Fire | ¿cómo se mantiene y distribuye? | ¿cómo se transforma con rapidez? | enfriar contra calentar, conservar contra consumir | vapor, esterilización, cocción, control térmico, recuperación metabólica |
| Silence ↔ Air | ¿qué debe dejar de transmitirse? | ¿qué debe alcanzar otro lugar? | zonas aisladas frente a circulación, precisión frente a dispersión | ventilación dirigida, cuarentena con flujo controlado, filtración, señal limpia a distancia |

## Interacciones combinadas

No son recetas de hechizos: son relaciones sistémicas posibles, cada una con
su riesgo.

| Combinación | Posibilidades | Riesgos |
|---|---|---|
| Earth + Water | suelo fértil, sedimentación, cimentación húmeda | erosión, compactación, deslizamiento |
| Earth + Fire | cerámica, fundición, tratamiento térmico | fractura, pérdida de temple, contaminación |
| Earth + Air | control de polvo, erosión dirigida, sustentación de partículas | abrasión, tormentas de polvo, desecación |
| Earth + Aether | infraestructura conectada, distribución de carga | propagación de fallos estructurales |
| Earth + Silence | aislamiento físico, cámaras estables, amortiguación | rigidez, sellado excesivo |
| Water + Fire | vapor, esterilización, regulación térmica | explosión, quemaduras, presión |
| Water + Air | clima, niebla, secado, dispersión de humedad | tormentas, enfermedad, pérdida de agua |
| Water + Aether | redes de riego, transmisión de estados, sensores | contaminación distribuida |
| Water + Silence | conservación, cuarentena líquida, contención | estancamiento, proliferación oculta |
| Fire + Air | combustión, propulsión, secado, señales | incendio acelerado, humo, pérdida de control |
| Fire + Aether | transferencia térmica, activación coordinada | sobrecarga en cascada |
| Fire + Silence | hornos aislados, cauterización precisa | acumulación térmica, fallo sin aviso |
| Air + Aether | comunicación, señales, coordinación a distancia | interferencia, propagación de errores |
| Air + Silence | filtración, rutas limpias, difusión selectiva | bloqueo de circulación, presión peligrosa |
| Aether + Silence | estabilización de anomalías, canales protegidos | corte de vínculos útiles, aislamiento identitario |

## Regeneración y extracción

El eje ambiental de la ciudad no es una barra de moralidad elemental: la
ciudad no gana puntos buenos por usar Agua ni malos por usar Fuego. El impacto
se calcula con consecuencias reales — consumo, recuperación, contaminación,
desgaste, diversidad, estabilidad, producción, tiempo, riesgo y dependencia.
Cada afinidad contribuye a los dos extremos:

| Afinidad | Expresión regenerativa | Expresión extractiva |
|---|---|---|
| Earth | restaurar soporte, reducir erosión, recuperar suelo | fracturar, compactar, minar, sostener canteras |
| Water | reponer ciclos, limpiar, distribuir, regular | bombear, drenar, lavar, refrigerar producción |
| Fire | quema controlada, esterilizar, reciclar | fundir, acelerar, consumir, despejar |
| Air | ventilar, dispersar calor, polinizar | secar, transportar, intensificar combustión |
| Aether | reconectar sistemas, coordinar recuperación | transferir, concentrar, sincronizar producción |
| Silence | aislar daño, conservar, permitir descanso | contener residuos, suprimir interferencia, estabilizar industria |

El nombre y el contrato del eje regeneración/extracción siguen sin decidirse
([#47](https://github.com/3FE3LE/world-of-goses/issues/47)).

## Firma en combate

La afinidad no sustituye estadísticas explícitas; orienta qué tipo de efecto
es natural. Son tendencias, no roles.

| Afinidad | Tendencias |
|---|---|
| Earth | estructura, impacto, interrupción, estabilidad, barrera |
| Water | regulación, recuperación, absorción, limpieza, control sostenido |
| Fire | transformación, aceleración, consumo, daño periódico, presión |
| Air | velocidad, alcance, propagación, desplazamiento, evasión |
| Aether | enlace, transferencia, sincronización, interferencia, efectos compartidos |
| Silence | aislamiento, disipación, precisión, resistencia, neutralización |

## Presentación

Una afinidad debe ser identificable **sin depender del color**:

| Afinidad | Gramática visual | Gramática sonora |
|---|---|---|
| Earth | peso, formas compactas, estratos, fracturas, partículas densas | impacto grave y sostenido |
| Water | continuidad, ondas, gotas, transición, deformación fluida | continuidad, capas fluidas, regulación |
| Fire | expansión, consumo, pulsos, calor, bordes inestables | ataques cortos, aceleración, tensión |
| Air | líneas de flujo, partículas desplazadas, presión, estelas | desplazamiento, apertura, propagación |
| Aether | conexiones, nodos, ecos, duplicación, transmisión | ecos, enlaces, capas sincronizadas |
| Silence | interrupción, vacío controlado, bordes limpios, amortiguación | cortes, espacios, ausencia controlada |

La música comunica estado; no reemplaza las estadísticas. La UI respeta las
guías de iconografía, tipografía y pixel art de
[`../presentation/visual-language.md`](../presentation/visual-language.md).

## Lo que las afinidades no hacen

- Seis clases elementales.
- Armas que contengan el poder principal, o daño elemental base otorgado por
  un objeto.
- Linajes bloqueados a un elemento.
- Silence como «personaje sin magia» ni Aether como elemento todopoderoso.
- Water como curación automática, Fire como daño automático, Earth como tanque
  automático ni Air como velocidad automática.
- Una rueda universal de ventajas ni una barra elemental moral.
- Efectos ambientales sin estados explícitos.
- Fórmulas imposibles de inspeccionar.
- Obligar al jugador a elegir una build durante el onboarding.
