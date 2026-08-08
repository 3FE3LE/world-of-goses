# Changelog

Narrative history of what the connected game gained, lost or reshaped, one
entry per increment. It answers *how we got here*; three neighbours answer
other questions and must not be duplicated into this file:

- `docs/CURRENT_STATUS.md` — what the code does **today** and what is approved
  next.
- `docs/ai/CURRENT_DEVELOPMENT_STATE.md` — the do-not-regress inventory.
- `docs/session-state/STATE.txt` — the generated, machine-measured baseline of
  the session in progress.

**Contract.** Every session that produces a commit adds or extends the entry
for its increment, in the same commit. An entry states what a player can now
do that they could not before, the schema range it crossed, and the measured
baseline — not a list of touched files, which `git log` already owns.

Entries dated before 2026-08-03 were reconstructed from commit subjects when
this file was introduced. They are deliberately thin: their detail was never
written down at the time, and inventing it now would be fabrication. Read
their commits for the real content.

---

## Un solo pack de UI en el juego, y la medida real de lo que ese pack puede dar

**2026-08-07** · schema v32 (sin cambio) · EG-5

`ButtonWarning` venía del kit viejo `art/Kenney/`: un tile de 16×16 escalado 3×
a 48×48, con un borde tres veces más gordo que la pizarra nativa de 32×32 que
usan `ButtonText` y `ButtonPrimary` justo al lado. La guía de iconografía prohíbe
explícitamente *"mezclar componentes de varios paquetes de UI sin ajustar
previamente su estilo"*, y esa mezcla vivía dentro de una misma familia de
botones. El rojo destructivo y el verde de progreso pasan al pack Pixel
Adventure, nativos, y con eso **`game/assets/ui/kenney/` deja de tener un solo
consumidor y se borra entero** — los cinco archivos muertos que arrastraba
(`ancient_brown`, `ancient_grey`, `grey`, `grey_pressed`, `green_pressed`) se van
con él en vez de podarse uno a uno.

Lo que un jugador ve: el botón destructivo ya no desentona junto a los demás. Lo
que el repositorio gana es más grande que eso, y es una medición.

### Connected

- **7 de 504 tiles importados.** `Tiles/Small tiles/Thick outline/tile_0071` →
  `red`, `tile_0070` → `red_outlined` (el contorno blanco que el pack ya trae
  como estado resaltado, ahora el `hover`), `tile_0075` → `green`.
  `red_pressed` reutiliza `red.png` con `modulate_color`, así que no se promovió
  ningún asset para un cambio de brillo. `content_margin` sigue siendo 16/4: no
  se movió ninguna métrica de layout.
- **`tools/New-KenneyContactSheet.ps1`.** El pack no trae nombres semánticos —
  todo es `tile_NNNN.png`, sin XML— así que un tile sólo se puede identificar
  mirándolo. El script compone los tiles con vecino más cercano y rotula cada
  uno con su índice sobre la propia rejilla del pack, de modo que
  `índice = fila * columnas + columna` se sostiene y una promoción puede citar un
  índice real en lugar de una suposición.
- **Los tiles Large son 9-slice; los Small no.** `slate_raised_dark.png` es
  1020/1024 opaco y llena su lienzo, luego `texture_margin = 8` es correcto. Los
  Small son sprites de ~10×10 centrados en un lienzo de 16×16 con 3 px de
  relleno transparente: con `texture_margin = 4` el corte parte el borde y deja
  el anillo de luz interior dentro del centro que se tilea, y eso se ve como una
  rejilla de puntos repetida. El valor correcto es **6**, que apoya el centro
  sobre el interior uniforme de 4×4. Verificado a 1280×720 y 1920×1080.
- **El pack no tiene ningún tile oscuro.** El centro opaco más oscuro de los 91
  tiles Large es luminancia **114**; `StyleBoxFlat_panel` es 17 y
  `StyleBoxFlat_panel_elevated` es 11. Las piezas de pizarra tienen 4–6 tonos
  distintos, así que oscurecer una con `modulate_color` hasta el valor del
  proyecto comprime su rango tonal a ~7/255 — indistinguible de un relleno
  plano. **Este pack puede dar botones, chips, casillas y widgets pequeños;
  no puede dar las superficies oscuras de panel de este juego.** Por eso
  `OverlayPanel`, `Panel`, `PanelCard`, `ScrollContainer` y `StatusStrip` siguen
  siendo `StyleBoxFlat`, ahora con la justificación concreta escrita en
  `ASSET_INVENTORY.md` en lugar de por omisión.
- **`PanelAction`, `PanelIdle` y `PanelWarning` fuera del tema.** Las tres
  resolvían al *mismo* stylebox que `PanelCard` y ninguna escena ni script las
  referenciaba: prometían una distinción que el tema no entregaba, de forma que
  un panel `PanelWarning` se veía exactamente igual que una tarjeta neutra.
- **`UI_PATTERNS.md` §5 decía algo falso.** Afirmaba que el tema registra un tipo
  base `Button` con valores por defecto; no existe, y un `Button` sin variación
  cae a la fuente y los styleboxes grises del motor. Queda corregido y anotado:
  registrarlo es una red de seguridad real, pero también da márgenes de
  contenido nuevos a cualquier botón sin anotar —un cambio de tamaño— así que
  pertenece a un pase que pueda revisar las superficies afectadas.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido del
snapshot JSON de `VerticalLoopPersistenceTests`, previo a este cambio). Arranque
headless limpio. Contexto de agentes 448/448. Localización válida: 922
identificadores de plantilla y 283 claves de runtime, sin claves nuevas.
Importaciones de fuentes pixel válidas. Capturas reales a **1280×720 y
1920×1080** de `pause-menu-reset` (el rojo nuevo junto al botón pizarra, con el
anillo de foco dorado), `expedition-idle` y `construction-scroll`.

**No verificado:** el relleno verde de `ProgressBar` no se vio en vivo — ningún
fixture del estado inicial muestra una barra con progreso. Su geometría es la
misma que la del rojo, ya comprobada, pero queda como comprobación manual, junto
con el aplastamiento de los cortes cuando la razón de llenado es menor que los
márgenes.

## La navegación deja de cruzar la pantalla y se recoge en un rail

**2026-08-07** · schema v32 (sin cambio) · EG-5

`MacroActions` era una franja de 42 px de borde a borde justo bajo la barra de
estado: gastaba el ancho completo del viewport para siete botones y partía el
mundo en horizontal. Pasa a ser `NavigationRail`, un grupo vertical arriba a la
izquierda que se ajusta a su propio contenido.

Lo que un jugador ve: la ciudad recupera la franja superior entera y el mundo se
lee de un borde al otro. La navegación sigue permanentemente visible, ahora en
una esquina, con el marco de bisel del composite.

### Connected

- **`Ui/NavigationRail.cs`.** Posee su propia estructura y devuelve los botones
  como propiedades tipadas, así que `MacroStreetLiveView` —4576 líneas— guarda
  **una** ruta al rail en lugar de cinco rutas literales del tipo
  `"../MacroActions/Actions/PoliciesButton"`. Decidir qué abre cada botón sigue
  siendo del macro view: el rail es cromo, y el cromo no sabe qué es una pantalla.
- **Resolver los hijos al acceder, no en `_Ready`.** El macro view precede al rail
  en `CityPrototype.tscn` y Godot inicializa los hermanos en orden de árbol, así
  que cachear los botones en el `_Ready` del rail devolvía null y **rompía el
  arranque**. Reordenar la escena habría arreglado ese consumidor y habría dejado
  la trampa puesta para el siguiente.
- **El rail no se ensancha a mayor resolución, y no puede.** `project.godot` usa
  `stretch/mode=canvas_items` sobre una base 16:9 de 1280×720, de modo que
  1920×1080 es **el mismo** viewport lógico dibujado a 1.5×:
  `GetVisibleRect().Size.X` vale 1280 en las dos medidas oficiales de revisión. No
  hay espacio extra al que expandirse. Se quitó el código responsive que lo
  intentaba en vez de dejarlo como adorno muerto.
- **`users.svg` y `camera.svg` promovidos.** El rail dibujaba el héroe, el censo y
  el modo de cámara con el mismo `user.svg`. Con etiquetas de texto se toleraba;
  al recoger el rail a solo iconos, tres acciones sin relación quedaban como el
  mismo glifo repetido. Son navegación genérica, que la guía de iconografía asigna
  a Pixelarticons, así que el modelo de tres capas funciona como está previsto.
- **`IconButton.ShowLabel`.** Colapsa el botón a su icono conservando
  `ButtonText`. Vive en `IconButton` y no en el rail porque `SetIconAndLabel`
  tiene otros consumidores —el macro view reescribe los botones de construcción y
  cámara según su estado— y aplicar el colapso desde fuera habría hecho que la
  siguiente de esas escrituras restaurara el texto en silencio.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido).
Arranque headless limpio. Contexto de agentes 448/448. Localización 922/283.
Importaciones de fuentes pixel válidas.

**Verificado con clics reales**, no leyendo código: un clic en el botón de
construcción del rail abre el panel *y* el segundo botón cambia a su glifo de
cierre **sin** recuperar la etiqueta, que es la prueba de que `ShowLabel` aguanta
las mutaciones del macro view; un clic en un árbol del mundo sigue seleccionándolo
y poblando el panel de selección; el menú de pausa sigue encontrando su botón tras
cambiar la ruta del nodo. Capturas a 1280×720 y 1920×1080.

**Pendiente de esta fase:** el `ContextInspector` y el `ActionDock` del plan no
existen todavía. `SelectionInfoPanel` sigue construyéndose en runtime y sondeando
`_Process`, y el footer de colocación sigue siendo un `new Button` sin superficie.

## Los paneles ganan marco autoral sin ceder la paleta

**2026-08-07** · schema v32 (sin cambio) · EG-5

La entrada anterior midió que el pack no tiene ningún tile oscuro y dejó los
paneles en `StyleBoxFlat` con esa justificación. Queda una salida que la medición
no cerraba: el pack **sí** trae tiles cuyo centro es completamente transparente
(`tile_0008`, `tile_0009`, `tile_0019`, `tile_0032` en el set Large). Si el
relleno del proyecto se hornea dentro de ese marco, el resultado conserva el
marco de píxel autoral *y* la paleta — algo que ni el tile crudo ni un
`modulate_color` podían dar.

Lo que un jugador ve: el borde de los paneles pasa de una línea plana a un bisel
de 3–4 tonos con el ornamento de esquina del pack, y el interior sigue siendo el
mismo casi-negro de antes.

### Connected

- **`tools/New-CompositeStylebox.ps1`.** Toma un tile de marco hueco, inunda el
  interior *encerrado* con un color de relleno y remapea los tonos del marco sobre
  una rampa del proyecto. Dos reglas hacen que el resultado sea exacto y no
  aproximado: el relleno parte del centro y se detiene en el marco, así que los
  píxeles transparentes de fuera de una esquina redondeada siguen transparentes y
  el tile conserva su silueta; y el remapeo es uno a uno por luminancia, de modo
  que la rampa debe traer exactamente tantos colores como tonos tenga el tile —
  un desajuste es un error, no un ajuste silencioso al más cercano.
- **`panel_card` y `panel_elevated`.** `Panel`/`PanelCard` toman `tile_0008` con
  relleno `14,17,23,246` y rampa tostada; `OverlayPanel` toma `tile_0009` con
  relleno `9,11,16,251` y rampa dorada. Ambos rellenos y ambos tonos de borde son
  los valores que ya llevaba el `StyleBoxFlat` anterior, así que **la paleta no se
  movió**, y el dorado sigue reservado al panel elevado como decidió el pase del
  2026-08-06. `content_margin` sigue en 14/12 y 18/16: tampoco se movió ninguna
  métrica.
- **Cada PNG generado lleva su `.recipe.json`** con el tile de origen, el relleno,
  el mapeo de tonos y el recuento de píxeles interiores, así que el asset es
  reproducible desde el repositorio. Es el mismo patrón que ya siguen los paneles
  de linaje generados bajo `game/assets/ui/lineages/<linaje>/panel/`.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido).
Arranque headless limpio. Contexto de agentes 448/448. Localización 922/283, sin
claves nuevas. Capturas reales a 1280×720 y 1920×1080 de `expedition-idle`,
`construction-scroll` y `shelter-resources`, más un recorte a 6× de la esquina
del modal para leer el bisel.

**Dos consecuencias que no son ganancia neta y quedan anotadas:**
`OverlayPanel` **pierde su sombra** — `StyleBoxTexture` no tiene `shadow_size` y
la caja plana llevaba una difusa de 8 px; para un proyecto cuyos invariantes
piden píxel puro sin antialiasing la sombra difusa ya era discutible, pero es un
cambio real. Y **el composite no llega a los paneles que sobrescriben el tema**:
17 de las 24 llamadas a `AddThemeStyleboxOverride` del repositorio aplican
`LineageThemeRegistry.GetStyleBox("panel")` directamente sobre el
`PanelContainer`, y un override local gana al tema —
`ResourceInventoryPanel` en la vista del refugio es el caso visible. Unificar el
tematizado por linaje con el tema, en vez de sobrescribirlo, es el trabajo
arquitectónico que queda.

## La cara Dominio del cubo deja de llamarse Mastery y los migrantes dejan de ser gemelos

**2026-08-07** · schema v31 → v32 · EG-5

El Cubo Kovari arrastraba `Mastery` como nombre de su tercera cara —en disco,
en los DTOs, en la UI— y el nombre se reservaba para los tiers de aprendizaje
de familias de arma que están a punto de entrar en dominio. Si el cubo y los
tiers hubieran coincidido leyendo `Mastery`, dos cosas distintas habrían
compartido nombre justo cuando la segunda empezara a existir. La cara pasa a
`Dominio` antes de que llegue la colisión.

Y bajo `60/60/60` con desempate `Body` primero, **todo ciudadano no fundador
era Fracture si su linaje era Body y Poisoning si era Bond**: las seis
expresiones físicas, dos alcanzables. Migrar después no servía: ese es el M-29
que `DEC-0018` dejó abierto.

### Connected

- **`CubeScoring.GenerateOrdinaryProfile(lineage, seed)`.** FNV-1a por eje
  sobre el vértice del linaje, delta en `[-8, +8]`, aplicado vía
  `ApplyContribution` — el mismo clamp y el mismo invariante de pareja del
  onboarding. La misma constante vive en
  `NaturalResourceLayoutPlanner.StableScore` y `CityWorld.StableAppearanceSeed`,
  así que el vocabulario de hashes del repositorio es uno solo. Un mismo
  `(linaje, seed)` produce siempre el mismo cubo; dos seeds distintos
  producen cubos distintos; cada linaje alcanza exactamente sus tres
  expresiones en un barrido y ninguna de las opuestas.
- **`CitizenProfile.TryCreate` con cubo explícito.** Nuevo overload
  opcional: `cubeProfile` por defecto sigue siendo el vértice, así
  `TestHelpers.NewProfile()` no se mueve y ningún test heredado cambia su
  cubo. Sólo `CreateMigrantProfile` lo pasa.
- **Descorrelación nombre / linaje.** El índice de nombre usaba
  `(seed - 2) % 8` y el de linaje `seed % 8`: misma longitud, mismo
  desfase, así que el id 2 siempre era Inara y siempre del mismo linaje.
  Ahora `CityWorld.MigrantNameForSeed(seed) = MigrantNames[(seed * 11 + 3) % 8]`,
  descorrelado en fase y reutilizable desde el test.
- **Rename `Mastery` → `Domain` en código, disco y UI.** El ctor,
  la propiedad y el `nameof` del mensaje de validación de
  `FounderCubeProfile` pasan al nombre canónico. `CubeScoring.MasteryValueId`
  pasa a `DomainValueId` con valor `"Domain"`, `WithMastery` a `WithDomain`,
  las ramas `"mastery"` del switch a `"domain"`. Las 16 referencias del
  `FounderNarrativeCatalog` siguen al renombrado. Los cuatro sitios de
  `WorldPersistence` que escribían `cube.Mastery` (captura y restauración
  en `MigrateV28ToV29` y `MigrateV29ToV30`) leen y escriben `Domain` desde
  la fuente canónica. `CitizenNatureText.cs` deja de imprimir el literal
  inglés `"Mastery"` y la ficha de fundación, el perfil del héroe y el
  panel de ciudadanos leen `cube.Domain`. El comentario obsoleto de
  `CubeExpression.cs` que documentaba el alias se quita: ya no hay alias.

### Schema

**v31 → v32, un paso.** El campo en disco del cubo se llama `Domain`. La
clave vieja `Mastery` se conserva una versión como puente nullable
(`FounderCubeProfileSave.Mastery`) para que un save v31 deserializado por
el código nuevo no pierda el cubo del fundador — el dato se pierde al
deserializar, antes de que la migración corra, así que el puente vive en
el DTO, no en el migrador. `MigrateV31ToV32` recorre los ciudadanos,
copia el puente a `Domain` y lo deja en `null`. Una partida que pasó por
`MigrateV29ToV30` antes del rename sigue funcionando: esa migración lee
`savedCube.Mastery ?? savedCube.Domain` (puente nullable, fallback al
campo canónico) y reconstruye la `FounderCubeProfile` que usaba el código
nuevo. **El cubo del fundador es idéntico antes y después.**

### Decisión

`DEC-0019` cierra M-29 y fija el rename. `DEC-0018` anota la resolución.

### Reshaped

- `DEC-0018` (Cube decides physical expression) gana la nota de que
  `M-29` quedó resuelto por `DEC-0019` el 2026-08-07.
- `13_KOVARI_CUBE.md` § *Estrategia y fallback* deja de afirmar
  "60/40 por eje" para todo no fundador: el nuevo fallback llama a
  `GenerateOrdinaryProfile(lineage, id)` y mantiene el invariante del
  sobre.
- `16_LINEAGES_KOVARI.md` § *Familias de armas y el vértice Kovari*
  se marca **superada 2026-08-07**: la sugerencia de armas por vértice
  quedó descartada cuando `DEC-0018` derivó el tier del ciudadano del
  cubo persistido y `DEC-0019` introdujo la variación `±8` que rompe el
  empate de tres bandas. Una tabla por vértice sería una lista
  redundante con el atlas de `bible/22_STATISTICS_PROGRESSION_AND_COMBAT_FORMULAS.md`.
- `07_ONBOARDING_AND_FOUNDER.md` snippet del `FounderOnboardingResult`
  cambia el parámetro `int Mastery` por `int Domain`.

### Verified

Build 0 errores / 0 advertencias. Tests **973 / 974** (1 omitido conocido
del snapshot JSON de `VerticalLoopPersistenceTests`). Localización: el
`msgstr` bajo `msgid "Dominio"` pasa de `"Mastery"` a `"Domain"`; la
clave no cambia, ninguna traducción queda vacía, ningún marcador ni
placeholder se mueve. La carga con captura manual de un save v31 escrito
con la clave `"Mastery"` migra limpiamente al campo `Domain` con el cubo
idéntico y el puente `null`. Agent context: 448 / 448. Arranque
headless limpio.

**La fixture `migrant-cube` fotografiaba al fundador.** Se capturó y se
leyó la imagen: decía `1/0 ciudadanos alojados · 0 no héroes`. El mundo
de la fixture no tiene Shelter, así que `AvailableHousing == 0`,
`TryAcceptPendingProspect()` devolvía `AtCapacity` y un `else` silencioso
seleccionaba al fundador — cuyo cubo es el vértice desnudo, es decir
exactamente lo que este cambio existe para mover. La fixture leía como
prueba de lo contrario de lo que probaba. Ahora monta su mundo con el
idioma de `ShowShelterResourcesForVisualRegression` (Shelter para
alojamiento, Town Hall para hospedar, prospecto hospedado y aceptado por
las APIs reales), **no tiene fallback** y cada precondición que falle
emite `GD.PushError`. La captura verificada muestra `2/3 alojados · 1 no
héroes`, con `Tovan` (Kovari) seleccionado y expresión física
**Sangrado** — no Fractura, que es lo que daría el vértice.

Dos correcciones menores en el mismo paso: el baseline de localización
del `TO_DO` afirmaba `908 / 296` contra los `922 / 283` medidos —la
medición manda, §5.1— y `MigrantNameForSeed` indexaba con aritmética
`unchecked` con signo, de modo que un seed lo bastante grande daba un
índice negativo; ahora mezcla sin signo.

Lo que queda pendiente: un jugador con una partida guardada antes del
rename verá **exactamente la misma ciudad** al cargar — M-29 ya no se
introduce retroactivamente en una ciudad existente, sólo en los
migrantes que lleguen a partir de ahora. Esa consecuencia ya estaba
anunciada en la propia nota de M-29.

---

## La expresión física deja de ser la afinidad con otro nombre

**2026-08-07** · schema v31 (sin cambio) · EG-5

Un fundador de Fuego era siempre Aturdimiento y aprendía siempre Maza y Orbe.
No era una regla de diseño: era un atajo. La biblia publica **una tabla de tres
columnas** —cara del Cubo, afinidad elemental, expresión física— y la
implementación la leyó como una cadena, derivando la expresión de la afinidad.
Dos ejes independientes colapsados en uno, y treinta de las treinta y seis
combinaciones que el propio roadmap describe perdidas.

Ahora la expresión sale de la **cara más alta del `CubeProfile`** y la afinidad
va por su cuenta. Un Ardhen puede ser Fractura con Fuego, Parálisis con Aire o
Sangrado con Éter. La derivación es función pura del cubo persistido: el mismo
cubo responde siempre lo mismo y no se guarda ninguna copia que pueda
contradecirlo.

Cada linaje admite exactamente tres expresiones y cada expresión pertenece a
exactamente cuatro linajes. Eso no se impone con una lista de exclusión: bajo
`60/40` con el tope `±8` una cara favorecida vive en `52–68` y su opuesta en
`32–48`, así que la más alta es siempre una de las tres favorecidas. Los
empates se resuelven por el orden canónico explícito `Body, Bond, Stability,
Impulse, Domain, Reach`, nunca por orden de enum, diccionario o carga.

Sobre esa base, **aprender un arma tiene tres velocidades** en vez de dos
(`DEC-0018`): `100 %` para las dos familias de la propia expresión, `50 %` para
las cuatro de las otras dos expresiones que el vértice del linaje alcanza, y
`10 %` para las seis restantes. El nivel escala **sólo la adquisición de
experiencia**: quien llega a Espada 20 con una familia extranjera tiene Espada
20, y pelea igual que cualquiera. La dificultad estaba en llegar.

**Sin migración.** La expresión nunca se persistió y todo ciudadano ya guardaba
su cubo desde v30, así que cambiar la derivación bastó. Consecuencia real y
buscada: una partida existente **carga con expresiones distintas** a las de
ayer. Nada en disco cambia; el valor obsoleto no sobrevive porque nunca se
guardó.

De paso, dos incoherencias que el corte destapó: `EnemyCatalog` construía la
naturaleza desde la afinidad ignorando el `Expression` que la definición ya
traía, y un test llamaba "natural" a un arma que en realidad era familiar de
linaje —seguía verde porque `0.50 > 0.10 × 2`—.

---

## La biblia recupera sus capítulos huérfanos

**2026-08-07** · schema v31 (sin cambio)

**Un jugador no nota nada.** Esta entrada existe porque el contrato la pide, no
porque el juego haya cambiado: no se tocó dominio, escena ni guardado. Lo que
cambió es dónde vive el canon, que es lo que decide si la próxima sesión lo
encuentra.

Cuatro documentos estaban numerados como capítulos de la biblia pero vivían en
la raíz de `docs/`, y tres de esos números ya estaban ocupados dentro de la
biblia. La colisión más cara: `19_FIRST_NIGHT_AND_FIRE_SPIRIT` — aceptado como
canónico el día anterior bajo DEC-0014 — competía con `19_LINEAGES_ORVETH`.
Ahora las afinidades elementales ocupan el **11**, el hueco que la biblia había
dejado; las estadísticas y fórmulas de combate pasan a **22**; la primera noche
pasa a **23**, y es el único capítulo que el código cita por nombre, así que el
renombrado arrastró catorce archivos de `game/` y `tests/`. El cuarto,
`13_EXPEDITIONS_AND_COMBAT_INTEGRATION_ROADMAP`, **no** entró en la biblia:
ordena trabajo por dependencias, dice cuándo y no qué, y pierde el número. El
número de capítulo queda declarado como identidad estable: nunca se reutiliza
ni se reordena.

La causa de que los cuatro sobrevivieran años en la raíz era que `docs/README.md`
no los mencionaba, junto con otros seis documentos. El índice ahora está
completo y la regla de autoridad 7 lo declara: un documento que existe y no está
indexado es invisible. `scripts/docs/classify.ps1` la hace cumplir, y falla
también cuando un documento no está clasificado o cuando el registro nombra un
archivo que ya no existe. Ese registro, `scripts/docs/classification.json`, es
la fase 2 de la migración y está escrito a mano a propósito: decidir si un
bloque es canon, especificación, roadmap o backlog es un juicio que se revisa
línea por línea, no un patrón que se infiere. 265 documentos clasificados, y
cuatro quedan marcados `split` o `merge` como propuestas sin programar —
`ARCHITECTURE.md` y el capítulo 10 mezclan descripción con roadmap,
`UI_AUDIT.md` mezcla checklist con historial, y `CURRENT_DEVELOPMENT_STATE.md`
es el tercer documento que responde qué está construido.

De paso, los cinco enlaces rotos que el inventario de la fase 1 había
reportado resultaron ser fantasmas: el extractor leía `MIGRATIONS[v](data)`
dentro de un bloque de código como si fuera un enlace Markdown. El inventario
ahora ignora el código antes de buscar enlaces: 129 enlaces, **0 rotos**.

### Las cuatro propuestas, resueltas — y dos de ellas no eran ciertas

El detector de marcadores buscaba subcadenas. Contaba «deferred disposal» como
trabajo diferido, «future attachments» como plan, y encontraba `owed` dentro de
`allowed` y de `reflowed`. Sobre esos conteos se acusó a `docs/ARCHITECTURE.md`
de mezclar arquitectura con roadmap. Con coincidencia por palabra completa el
repositorio pasa de 124 documentos con backlog a 84, y los 14 que le quedan a
`ARCHITECTURE.md` son todos el adjetivo «future» dentro de prosa descriptiva.
**No se divide**: describe el código que existe, de principio a fin. Su defecto
real es otro y ahora tiene ficha propia (`M-28`): el §8 narra migraciones hasta
la v28 mientras el código va por la v31.

El capítulo 10 sí lo era, y su propio título lo admitía. Se quedó el canon
—stack, separación de capas, reglas de simulación, pixel perfect, cámara
caminable, guardarraíles— y se archivó el plan. El mapa de escenas que
proponía describía `scenes/city/`, `scenes/buildings/MineDetailView.tscn`,
`scenes/gardens/`: hoy el proyecto tiene diecisiete `.tscn` y ninguno se llama
así. Presentarlo como canon lo convertía en una instrucción equivocada. La
secuencia de quince pasos se archivó en vez de reubicarse en un roadmap vivo,
porque el orden vigente es EG-0→EG-6 y dos secuencias canónicas compitiendo son
peores que un plan viejo. Tres «preguntas abiertas» que en realidad eran trabajo
con dueño bajaron a `TO_DO.md` como `M-27`. El archivo perdió `AND_ROADMAP` del
nombre; el número de capítulo no cambió.

`UI_AUDIT.md` mezclaba un checklist reutilizable con el registro de lo firmado.
El checklist y la regla de cierre se fueron a `VISUAL_REGRESSION.md`, que ya
era dueño del contrato de firma humana; el audit se queda con estado, evidencia,
deuda de presentación e historial. Sus veintiséis marcadores sí eran señal real
—casillas sin marcar— y leían como un backlog sin dueño. De paso desaparece el
encuadre en VS-5, descartado el 2026-07-31.

`CURRENT_DEVELOPMENT_STATE.md` no se fusionó entero: sólo se le quitó lo que
duplicaba. Su tabla de cabecera afirmaba 730 pruebas y esquema v28 contra 914 y
v31 medidos. Un número copiado a mano es un número que va a estar mal, así que
se borró en vez de corregirse; ahora el archivo no contiene ninguno y remite a
`STATE.txt`. Lo que queda —el inventario de no-regresión— no lo tiene ningún
otro documento.

Y una cosa que el corte destapó y era peor que todo lo anterior: cuatro skills
canónicos mandaban a leer `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`, borrado hace una
semana, y a usar su tabla de huecos `G0`–`G7` como criterios de aceptación. Un
agente que cargara `vertical-slice-validation` recibía cuatro punteros a la
nada. Ahora apuntan al proposal §3, §15 y §17, que es donde viven los criterios.

Baseline medido: build 0 errores / 0 avisos, **914 pruebas pasan** (1 omitida),
arranque headless limpio, 448 verificaciones de contexto de agentes, 922
identificadores de plantilla y 283 claves en runtime.

## Profundidad real en el macro y suelo por bioma

**2026-08-06** · schema v31 (sin cambio)

Un ciudadano se dibujaba **siempre** por delante de todo el mundo, incluso de un
árbol más cercano a cámara. No era un fallo de ordenación sino dos ejes
distintos: la vista pintaba terreno, edificios y árboles con comandos inmediatos
en su propio `_Draw()`, mientras los ciudadanos son nodos hijos — y en Godot un
`CanvasItem` emite lo suyo antes de dibujar a sus hijos. Nunca se comparaban.

Ahora los obstáculos viven en una capa por calle cuyo `ZIndex` sale de la
**misma función** que usan los ciudadanos. El suelo se queda en el padre, que es
lo correcto: el terreno siempre va detrás. La vista entera baja a
`OverlayLayers.WorldDepthBase` para que ese rango de índices no adelante al HUD.

Y el suelo deja de rotar `street % 3` entre hierba, tierra y piedra — el motivo
de que leyera como bandas arbitrarias. Cada ciudad dibuja el **bioma del sitio
donde cayó su fundador** (`DEC-0017`), ocho paletas, una por linaje, y solo
paleta: ninguna receta, tasa ni recurso cambia, así que el linaje sigue sin
conferir ventaja.

Tres cosas que solo se vieron capturando: al dar lienzo a los helpers cambié las
firmas y no los cuerpos, y **todos los árboles desaparecieron** aunque compilaba;
los nuevos z de banda adelantaron a la Crónica; y el primer hash de variante
producía **rayas horizontales**, porque `tileIndex * 3` se anula con tres
variantes y la elección colapsaba sobre la fila.

Lo que no entra: la dispersión de flores. Los tiles con flores del pack son
piezas de autotile y cada una arrastra una esquina del material vecino, así que
al repetirlas se ve el corte. Hace falta arte de props con fondo transparente.

Sobre eso, la lección que costó tres intentos: la hoja tiene **dos cosas que
parecen relleno**. El bloque de muestras sólidas (cols 5-9, filas 0-1) sí es
plano. Los bloques de *parche* de color son autotiles de 5×3 cuyas columnas 0-1
son las cuatro esquinas interiores —cada una con la muesca del material de
debajo— y cuyas columnas 2-4 son un blob redondeado de 3×3. **Solo el centro
del blob es apto para repetir.** Al tomar un id nuevo hay que renderizar su
bloque entero, no el tile suelto: la muesca es invisible a un solo tile y
evidente en cuanto se repite en una banda.

**El grano del movimiento se afina sin dejar de ser escalonado.** La locomoción
pasa de 8 px a 12 Hz a **4 px a 24 Hz**: misma velocidad efectiva (96 px/s, y se
deriva de las dos constantes, así que no puede desincronizarse) con la mitad de
salto visual. Hubo que duplicar los contadores de paso del paneo en profundidad
y del zoom de entrada a edificio, que avanzan una fracción fija por tick y si no
se habrían acelerado al doble — "suavizar el desplazamiento" habría vuelto la
cámara más brusca. Y el peldaño de las escaleras trapezoidales del terreno baja
de 4 px a **2 px**. En ningún caso se introduce interpolación: el movimiento
sigue siendo discreto y los bordes siguen encajados en rejilla de píxel entero.

Los ocho biomas se pueden revisar sin rejugar el onboarding, con el fixture
`biome-<linaje>`. Hacía falta porque el linaje del fundador lo *infiere* el
scorer, no se elige. Y de paso los fixtures dejan de construir su fundador con
`CitizenProfile.TryCreate` y una lista inventada de aptitudes, profesiones y
rasgos: un fundador no empieza con nada de eso —`CreateFounder` pasa arrays
vacíos— así que una ciudad de prueba ya no nace pre-cualificada.

Baseline medido: build 0 errores / 0 advertencias; tests 914 pasando, 1 omitido;
boot headless correcto; 922 IDs de plantilla y 283 claves de runtime; agent
context 448/448.

---

## El tiempo deja de poder detenerse

**2026-08-06** · schema v31 (sin cambio)

La ciudad avanza con el juego cerrado. Un botón que congelaba el reloj discutía
con esa premisa, y abrir el menú ESC era además una forma de comprar tiempo
gratis. Fuera los dos: el control de play/pausa desaparece de la barra de estado
y el menú de la ciudad ya no congela la simulación al abrirse — el mundo sigue
corriendo detrás del scrim.

Queda el **multiplicador de velocidad**, que es el control que sí tiene sentido:
quien quiera que la ciudad se asiente la baja de ritmo en vez de detenerla. La
biblia reserva sitio en la barra para "tiempo, velocidad, alertas y acciones
globales", así que la velocidad sigue cumpliendo ese contrato.

Consecuencia que hay que saber: **desaparece el autoguardado al pausar.** Se
disparaba en la transición corriendo→pausado, y sin pausa esa rama no se alcanza.
El autoguardado periódico de tres minutos y el de cierre siguen intactos;
`ARCHITECTURE.md` y `CURRENT_STATUS.md` lo prometían "on close/pause" y ya no.

El ejemplo canónico de `-NormalizedClicks` en la matriz visual apuntaba
justamente al botón borrado. Se sustituyó, y se dejó anotada la lección: unas
coordenadas solo son tan duraderas como el control al que apuntan — al quitar el
botón, ese clic habría caído en panel vacío y la captura habría "pasado" igual.

---

## Identidad visual: fuera el amarillo, sprites en el mundo y el espíritu con voz propia

**2026-08-06** · schema v31 (sin cambio)

Casi todos los botones del juego eran amarillos, y nadie lo había decidido. Era
la suma de dos valores por defecto: `ButtonText` —la variación que usa el 80 %
de los botones— apuntaba a `kenney/9-slice/yellow.tres`, y el fallback de
`LineageThemeRegistry` apuntaba al mismo archivo mientras el linaje activo
arrancaba como `"default"`, **un id que no era clave del diccionario de
linajes**. Todo panel construido antes de que exista un héroe caía por ahí; y
como la mayoría de consumidores aplican el stylebox una sola vez en `_Ready`,
se quedaban amarillos el resto de la sesión.

Ahora la superficie neutra es **pizarra oscura**, promocionada del pack CC0
*UI Pack – Pixel Adventure* de Kenney: cuatro tiles de 32×32, con hover y
disabled resueltos por `modulate` sobre los mismos PNG para no promocionar
assets que no aportan forma nueva. El dorado queda solo para estado —foco,
borde de panel elevado, fragmentos estabilizados—; el verde conserva su
semántico de éxito y el rojo el destructivo. El texto de botón pasa a crema
sobre la superficie oscura. **Ninguna métrica de layout se movió**: los
`content_margin` siguen siendo 16/4, y las capturas del onboarding antes y
después lo confirman. La biblia permite que un reskin cambie paleta, bordes y
rellenos, pero no tamaños mínimos ni jerarquía.

Las acciones se eligen ahora **por rol** y no por apariencia:
`PrimaryActionButton`, `SecondaryActionButton` y `DangerActionButton`. Ese es
el motivo de que este cambio de piel haya sido una edición del tema y no una
auditoría de treinta puntos de construcción.

De paso cae un defecto de capas que este trabajo destapó: `FirstNightScene`
alojaba la noche en un `CanvasLayer = 50`, pero `OverlayLayers` es un catálogo
de `ZIndex` y un `CanvasLayer` gana a cualquier `ZIndex`. El espíritu y su
banda se dibujaban por encima del onboarding (80), del menú de pausa (100) y
del Notifier. Ahora son raíces de canvas normales en
`OverlayLayers.Tutorial`, y la captura de pausa lo demuestra: el espíritu
queda bajo el scrim.

**El mundo deja de ser rectángulos de color.** Ramas, fibra vegetal, piedra
pequeña y comida silvestre eran cuatro `DrawRect` planos con colores a pelo, que
a distancia de macro leían todos como el mismo cuadrado. Ahora son sprites del
atlas roguelike: matorral seco, brote verde, escombro gris y arbusto con bayas.
Las coordenadas del atlas vivían triplicadas —con la fila y la columna
re-escritas como literales justo debajo de las constantes que ya las
contenían— y ahora están en un único `TerrainAtlas`. La verificación importó:
el primer tile que elegí para la piedra resultó ser una pila de lingotes de
plata que renderizaba azul, exactamente la clase de error que el repo ya tenía
registrada con los tiles de agua en vez de árboles.

**Los cursores son de verdad contextuales.** El puntero era un SVG de 24 px que
leía blando contra el pixel art, y el cursor "interactivo" era esa misma flecha
reteñida: al pasar por un botón salía una flecha más clara, no una mano. Ahora
son dos glifos distintos del pack de cursores, teñidos por linaje como manda la
biblia (recolorear por tokens, sin deformar geometría).

**El espíritu de fuego habla desde el mundo.** Era un anillo de dieciséis
puntos con un triángulo, clavado a la pantalla porque su posición solo se
recalculaba al cambiar de etapa. Ahora es una llama que parpadea en la cadencia
de 12 Hz del proyecto y se re-proyecta cada frame, y su voz es una **burbuja
anclada sobre él** en lugar de una banda al pie: la banda nunca se había visto
en ejecución hasta ayer, y al verse resultó ser una barra de subtítulos con el
personaje que enseña en otra parte de la pantalla. `DEC-0016` supersede a
`DEC-0014` §3 y explica por qué. La burbuja entera es el confirmar; no hay
botón compitiendo. Y las superficies de la noche se ocultan fuera del macro,
porque estaban dibujándose sobre el panel de una vista de edificio.

**Las vistas de detalle tienen suelo.** Eran una textura de panel repetida y
teñida por el acento del linaje; ahora son tiles de terreno reales dibujados
ortogonalmente a escala entera ×4 (16 → 64, el `BaseUnit` del proyecto), sin el
`Modulate` que enturbiaba el color.

**Y el espíritu por fin enseña.** Se callaba tras dos frases: las dos etapas en
las que la noche espera a que construyas devuelven `null` del catálogo a
propósito, y nada llenaba ese hueco, así que el "tutorial orgánico" no llegaba
a enseñar nada. Ahora la burbuja muestra una directiva —"Necesitas 3 ramas y 2
piedras pequeñas para levantar una hoguera"— con las cantidades **interpoladas
de `FoundingSiteRules.InputsFor`**, nunca escritas a mano, como exige
`DEC-0014` §4: si cambia la receta, el tutorial no puede mentir.

Tres correcciones más de la misma sesión de prueba: la burbuja se anclaba al
borde superior cuando no cabía arriba y caía **encima** del hablante, tapando
al espíritu — ahora vuelca debajo y la cola gira para seguir apuntándole; la
burbuja seguía visible al abrir Construcción, porque ningún modal cambia
`Selection` y vigilar solo la selección no bastaba (ahora también escucha al
`ModalHost`); y el cursor mostraba un hacha para cualquier recurso, cuando el
pack tiene herramientas distintas — ahora hacha para madera, pico para piedra y
mano para lo que se recoge del suelo.

**Y el mundo recupera su escala.** El sprite del habitante en el macro estaba
al 25 %, una decisión tomada cuando el terreno era provisional y bastaba con
que una persona "se notara"; contra el terreno acabado leía como un enano.
Ahora está al 50 %, una fracción limpia para que el muestreo nearest siga
cayendo en píxeles enteros. Y los árboles medían lo mismo que un arbusto de
bayas — no por venir de packs distintos, que ya vienen del mismo atlas, sino
porque todos los recursos usaban el mismo rect cuadrado. El árbol pasa a su
forma de dos tiles: la copa crece hacia arriba fuera del rect y el tronco
conserva la línea de suelo, así que la huella de la parcela y el área de clic
no cambian.

Dos arreglos más del segundo playtest: la burbuja de apertura dibujaba una cola
apuntando al suelo vacío, porque en `Manifested` el espíritu todavía no está
presente — la narración ahora va sin cola, y solo las etapas con espíritu tienen
hablante. Y las brasas dejaron de ser un cuadrilátero de alambre que un
playtester confundió con un sigilo de linaje: ahora son el sprite de hoguera del
atlas, teñido hacia ceniza.

**El espíritu deja de narrarse a sí mismo.** Los seis nodos del catálogo
declaraban `SpeakerId = "fire_spirit"`, incluidos los cinco escritos como
narración en tercera persona *sobre* él; el globo se los atribuía, así que el
espíritu decía cosas como "El espíritu se detiene, sorprendido". Ahora existe un
hablante `narrator` y solo `shelter_built` se pronuncia en voz alta. La
narración se pinta centrada, sin cola y un tono más apagada; el habla conserva
el nivel `DialogText` y la cola sobre el espíritu. El test que existía afirmaba
la atribución errónea y se corrigió; otro nuevo vigila que la noche conserve
**las dos** voces, para que la separación no se colapse en silencio.

Lo que sigue pendiente: el espíritu es una llama autoral, no un sprite — no hay
ninguna llama exenta en los tres packs y recortarla de un brasero sería editar
a mano un PNG exportado, que el pipeline prohíbe. Y cinco de los seis diálogos
autorales están escritos como **narración en tercera persona sobre** el espíritu
("El espíritu se detiene, sorprendido"), no como habla suya: la banda inferior
original los presentaba bien, pero un globo de diálogo los atribuye a alguien
que no los dice. Clasificar la voz nodo a nodo son 48 claves y es decisión de
`narrative-lore`, no de presentación.

Baseline medido: build 0 errores / 0 advertencias; tests 913 pasando, 1
omitido; 918 IDs de plantilla y 283 claves de runtime; specimen tipográfico con
`TitleCropColorCount = 2` (sin franja gris); capturas de `astral-start`,
`firstnight-manifested`, `pause-menu`, `migrant`, `policies`, `offline-report`
y `macro-current` a 1280×720 y 1920×1080.

---

## README alineado con EG-5

**2026-08-06** · schema v31 (sin cambio)

El `README.md` llevaba tiempo describiendo una realidad que el repo
ya no es: todavía hablaba de "complete hero-onboarding profile for
one citizen and zero buildings, followed by an explicitly authorised
Basic Shelter construction project" y citaba 232 / 345 tests como
si fueran la línea base actual. La primera obra ya no es un Basic
Shelter libre — el fundador entra con la noche autoral (espíritu de
fuego, módulo a módulo, `00:00` → `06:00`) y el primer edificio
crece dentro del Founding Site como Campfire → Bedroll/Cache →
Canopy. El Cubo Kovari, el slice vertical de combate, las
expediciones de recursos sobre la rejilla dinámica, el catálogo
`firstnight.*` y la SpiritTrailSearch ya están conectados; un
visitante que se quede en el `README` se queda con la versión vieja.

Este increment no toca gameplay, schema ni catálogo. Solo reescribe
las cuatro secciones que el repo había superado:

- El **status header** ahora declara EG-5, la apertura EG-A0
  (onboarding Kovari, primera noche, Founding Site, Cultivation
  Site, SpiritTrail/FallenWood, combate determinista, reclutamiento
  por Town Hall) y la baseline medida hoy (schema v31, **913
  passing**, 1 omitido).
- La sección **13 (First prototype scope)** ya no enumera el
  Basic Shelter libre como primer proyecto, sino la cadena de la
  noche autoral y el ciclo del Founding Site, junto con el Cubo
  Kovari, las expediciones de recursos y el slice de combate.
- La sección **14 (Short initial roadmap)** incorpora la secuencia
  `EG-0 → EG-6` definida por el proposal §15 y separa lo cerrado
  de lo activo (EG-5) y lo pendiente (EG-6).
- La sección **15 (Founding hero and first night)** deja de
  presentar al Basic Shelter como primera decisión de obra y
  apunta a `docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md` y al
  acceptance test EG-A0 del proposal §17.

Lo demás del archivo (game vision, pilares, stack, plataforma,
flujo de arte, convenciones, licencia, contribución) sigue
vigente.

Baseline medido en este increment: `dotnet build` 0 errores /
0 advertencias (reconfirmado por el snapshot Full del commit);
tests 913 / 914 (1 omitido); agent context 448 / 448; 918 IDs de
plantilla y 283 claves de runtime.

---

## El onboarding cabe en la pantalla y la forma reconstruida se presenta como ficha

**2026-08-06** · schema v31 (sin cambio)

La primera pantalla que ve un jugador nuevo estaba rota por abajo. El paso de
identidad medía unos 775 px contra un presupuesto de 656, y como el contenedor
no tenía ningún hijo que se expandiera verticalmente ni un `ScrollContainer`,
Godot no recortaba ni desplazaba: el pie con `Atrás` y `Conservar este nombre`
simplemente salía de la pantalla. El jugador podía nombrar a su fundador y no
tener forma visible de confirmarlo.

Alrededor de ese fallo había desperdicio: cuatro botones de opción forzados a
66 px cuando su altura natural son 34, suelos de 92 px y 52 px en dos etiquetas
que a menudo no mostraban nada, y un selector de presentación corporal heredado
de un helper con `ExpandFill` que, anidado en una columna expansiva, daba dos
botones de ~510 px — el 81 % del ancho para una elección binaria.

Ahora el onboarding **cabe por construcción** en los tres pasos, medido contra
las cadenas más largas de ambos catálogos: 506 px en las preguntas, 567 en el
nombrado y 510 en la ficha. Dos espaciadores expansivos absorben la holgura, y
las filas vacías se ocultan en vez de vaciarse, porque una etiqueta vacía sigue
reclamando su altura mínima y su separación.

Lo que el jugador gana además de poder terminar: la elección seleccionada se
reconoce por un glifo de confirmación y no solo por el color, y las cuatro
opciones comparten un `ButtonGroup`, así que teclado y mando las recorren como
un único control. La presentación corporal es ahora un control compacto de
304 px. Y la forma reconstruida se lee en **su propia pantalla**: el bloque de
once líneas que se apretaba bajo el campo de nombre es una ficha con linaje,
afinidad, expresión física y los tres ejes del Cubo como barras de dos polos,
cada una con sus dos enteros impresos. Siguiendo la biblia (§Pantalla final del
onboarding), la ficha deja de nombrar familias de arma: "arma preferida" está en
su lista de *no mostrar* y el código llevaba tiempo desviado de ese contrato.

Dos filas se reservan en vez de ocultarse: la consecuencia inmediata durante las
preguntas y el aviso de validación durante el nombrado. Son las dos que aparecen
y desaparecen *como respuesta a que el jugador actúe en esa misma pantalla*, así
que ocultarlas era correcto para el espacio y erróneo para la sensación —
elegir una opción reajustaba la columna y movía el texto que se estaba leyendo.
Comprobado con dos capturas idénticas salvo el clic: solo difieren el contador,
el fragmento encendido, el foco, el botón elegido, la línea de consecuencia y el
pie; título y narrativa son idénticos píxel a píxel a 1280×720 y a 1920×1080.

Lo que el jugador todavía ve mal: la barra de estado y la fila de acciones de la
ciudad siguen visibles a través del velo astral traslúcido en los dos últimos
pasos. Es anterior a este cambio —`HeroAccessButton` ya se oculta durante el
onboarding, `CityStatusPanel` y `MacroActions` nunca lo hicieron— y queda como
el único defecto de composición sin firmar en la matriz visual.

Baseline medido: build 0 errores / 0 advertencias; tests 913 pasando, 1 omitido;
918 IDs de plantilla y 283 claves de runtime; capturas de `astral-start`,
`astral-ground`, `astral-identity` y el nuevo `astral-founder-card` a 1280×720 y
1920×1080.

---

## La primera noche del fundador deja de ser un bloqueo sin explicación

**2026-08-05** · schema v30 → v31

Un playtest se detuvo al minuto uno: talar un árbol pedía un hacha primitiva y
nada decía cómo obtenerla. El hacha existía —receta, gate, incluso botón— pero
enterrada en el panel de detalle de un refugio que todavía no existía, al final
de una cadena que nadie había explicado. El único tutorial del juego, tres
tarjetas modales, pedía "1 wood de los Forest plots" cuando la primera obra
cuesta tres ramas y dos piedras, y describía chips de la barra de estado
eliminados hacía semanas.

Lo que faltaba no era un sistema: era una causa aprendida. Este increment pone
en pie el estado de dominio de una **primera noche jugable** entre las `00:00` y
las `06:00`, donde un espíritu de fuego enseñará por qué la materia del suelo
importa. Un mundo nuevo ya nacía en el tick 0, que es Día 1 `00:00` y es noche,
así que la secuencia no necesita mover el reloj ni una segunda escena de
despertar. Nueve etapas avanzan **solo** por hechos del mundo —un módulo
terminado, un nodo de diálogo cerrado— nunca por un temporizador: nadie puede
perder el tutorial por leer despacio.

Tres decisiones sostienen eso. El tick **nunca se congela**, porque congelarlo
pararía la construcción y crearía la circularidad de no poder cumplir el hito
que el reloj detenido mide; en su lugar el amanecer es la transición de etapa, la
hora mostrada se estanca en `05:59` en vez de saltar al concluir, y la noche
difiere el calendario entero — ni ración ni frontera de día mientras corre, lo
que protege al jugador lento que cruce el tick 1200. La posición del espíritu
**no se persiste**: se deriva de la etapa, como los anclajes de edificios
derivan de su placement, porque la ciudad no guarda coordenadas visuales
autoritativas. Y el Bedroll gana por fin significado mecánico: sin él la noche
se niega a dormir, cuando hasta ahora era solo coste, trabajo y prerrequisito del
Canopy.

El camino se abrió retirando cinco defectos que la noche habría exhibido de
inmediato. El peor: al terminar el Campfire el fundador quedaba comprometido con
una obra sin trabajo activo, así que dejaba de estar disponible y el botón de
recolectar se apagaba con "fundador no disponible" — exactamente cuando toca
recoger la fibra del refugio. Solo el Canopy liberaba contribuidores; ahora lo
hace cualquier módulo, y autorizar el siguiente re-moviliza al fundador, que es
lo que la cadena venía sosteniendo por accidente. Dos superficies mentían al leer
el reloj crudo en vez de la regla de jornada, y durante toda la apertura la
interfaz anunciaba trabajo detenido mientras el fundador construía. Una API
pública drenaba madera sin comprobar el hacha. Y el reinicio suave escribía en
disco una ciudad sin recursos de suelo, cuyo Campfire era impagable.

Los saves existentes entran con la noche **ya concluida**: esas ciudades pasaron
su apertura y meterlas en la secuencia las atraparía tras hitos que no pueden
volver a cumplir. No hay regalos ni retro-tutorial.

Lo que el jugador todavía no ve: el espíritu, sus diálogos y el motivo de la
primera expedición. Quedan como fases 2 a 4 en `TO_DO.md` §3. El contrato que
las gobierna está en `docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`, y mantiene
separados los tres niveles de guía — noche autoral, directivas derivadas del
estado real, y Camino de solo lectura — sin fusionarlos en una lista de misiones.

Baseline medido: build 0 errores / 0 advertencias; tests 879 pasando, 1 omitido
(el snapshot JSON conocido); boot headless correcto; 852 IDs de plantilla y 295
claves de runtime; 442 checks de agent context.

---

## La primera noche del fundador se vuelve jugable y motiva la primera expedición

**2026-08-06** · schema v31 (sin bump) · EG-5

Tras la introducción del estado de dominio en schema v31, la apertura
EG-A0 deja de ser una lista de pasos: la noche del fundador es ahora
una secuencia autoral de 00:00 a 06:00, guiada por un espíritu de fuego
que enseña por qué importan las ramas, las piedras y la fibra. El
jugador ya no tiene que adivinar la cadena (fogata → refugio →
primera salida); el espíritu explica la causa y la noche avanza
módulo a módulo.

### Connected

- **Diálogo del espíritu** (`Domain/FireSpiritDialogueCatalog.cs`):
  seis nodos principales (`Manifested`, `SpiritArrived`,
  `CampfireBuilt`, `ShelterBuilt`, `OtherLightTold`, `Sleeping`) con
  ocho variantes de cuerpo por linaje (48 claves `firstnight.*`).
  Las cantidades del diálogo se interpolan en runtime desde
  `FoundingSiteRules.InputsFor(module)`, de modo que un cambio de
  receta no puede volver a desfasar la guía.
- **Banda de diálogo no modal** (`FirstNightDialogueStrip.cs`) en
  `OverlayLayers.Tutorial = 50`. El strip captura clicks sólo en su
  rectángulo inferior; el resto del mundo sigue jugable mientras el
  jugador lee.
- **Espíritu como entidad visual** (`FireSpiritVisual.cs`): un anillo
  de 16 puntos y un glyph triangular, posición derivada del stage,
  nunca persistida. Antes de `CampfireBuilt` junto al fundador;
  después, sobre la fogata.
- **Brasas post-amanecer** (`FirstNightEmbers.cs`): un pequeño
  cuadrilátero naranja translúcido queda sobre la fogata cuando
  `Stage == Concluded` y `SpiritDeparted` está en el chronicle.
- **Comentario contextual** (`FirstNightContextCommentary.cs`):
  cuando el fundador recoge `Branches`/`SmallStone`/`PlantFiber`
  cerca del fogón o refugio durante la noche, el espíritu comenta
  efímeramente vía `Notifier`.
- **Event causal al amanecer**: nuevo `WorldEventKind.SpiritDeparted`
  se emite una sola vez en el cruce `Sleeping` → `Concluded`, y se
  persiste como evento significativo.
- **Motivación de la primera expedición**:
  `ResourceOpportunityKind.SpiritTrailSearch` con la misma curva de
  retorno que `FallenWoodSearch` (4 / 6 / 8 Wood) y duración 180
  ticks. El botón se desbloquea sólo cuando `SpiritDeparted` está en
  el log, vía `ExpeditionPlanningSnapshot.SpiritTrailUnlocked`.
- **Documentación canónica**: `docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`
  pasa de "Propuesta canónica" a "Aceptada" (DEC-0014). El bloque
  "First night" se añade a `CROSS_DOMAIN_INVARIANTS.md` y la ruta
  "First night / fire spirit" se añade a `CONTEXT_MAP.md`. Cuatro
  fixtures nuevos en la matriz visual.

### Schema

**No schema bump.** `SpiritTrailSearch` es un nuevo
`ResourceOpportunityKind` que se serializa como string
(`Enum.TryParse` ya es tolerante para valores nuevos en saves
previos). `SpiritDeparted` aparece sólo en saves que ejecuten la
secuencia completa de la noche; `WorldEventRetention.IsSignificant`
lo acepta sin migración adicional. El estado de noche en sí
(`FirstNightSave`) ya estaba en v31 desde el incremento anterior.

### Verified

- Build 0 / 0 (warnings 0).
- Tests 911 / 912 (1 omitido conocido de `VerticalLoopPersistenceTests`).
- Localization: 907 IDs plantilla, 295 claves de runtime, ningún
  dígito literal bajo el prefijo `firstnight.*` en EN ni ES.
- Agent context: 442 / 442 checks.
- `messages.pot` regenerado limpio.

## Primer circuito vertical de combate automático y expediciones

**2026-08-05**

Tres ciudadanos persistentes ya pueden equiparse, salir, pelear dos veces
automáticamente, elegir una ruta y volver con consecuencias escritas sobre las
mismas personas. El circuito completo del roadmap —`Citizen` → preparación →
estadísticas derivadas → técnicas con coeficiente físico y elemental → combate →
decisión de ruta → segundo encuentro → destino → regreso → persistencia— se
resuelve entero dentro del dominio, sin escena, sin `_Process` y de forma
reproducible desde una semilla.

Una técnica convierte potencia de canal en acción mediante dos coeficientes cuya
suma es un presupuesto fijo. Esa única invariante es la que obliga a que una
evolución **redistribuya** en lugar de regalar poder: subir el lado físico cuesta
exactamente lo que baja el elemental, y se revalida al aplicarla. El contenido son
tres grupos de módulos que se combinan —cuatro familias de arma, dos expresiones
físicas, las seis afinidades—, nunca una habilidad por combinación: doce
definiciones, no cuarenta y ocho. Stunning y Knockdown son funcionales; Knockdown
altera exposición y disponibilidad, no posición, porque este combate no tiene
dónde moverse.

La competencia gana por fin su curva de experiencia, centralizada y configurable,
con nivel derivado de la experiencia acumulada, techo de aprendizaje y la
penalización de familia extranjera al 10 % aplicada **al aprendizaje**, nunca al
resultado de la técnica. `Survival` entra como competencia profesional, exenta de
esa penalización. El `ConditionFactor` se deriva de causas persistentes —vida,
fatiga, heridas— y expone esas causas para la telemetría en lugar de asignarse a
mano.

La potencia de canal sigue llamándose potencia. Ningún campo de la telemetría la
llama daño, y ninguna estadística persistente nueva de daño físico o elemental
existe.

Deuda reconocida: el `Expedition` de EG-4 sigue siendo un temporizador de recursos
independiente; unificarlo con esta corrida de combate es el siguiente paso, no
parte de este slice. La presentación es un panel de depuración, no una pantalla de
preparación, y no hay economía de equipamiento todavía.

Baseline medido: build con 0 errores y 0 warnings; 858 pruebas aprobadas, 0
fallidas, 1 omitida (859 total); arranque headless limpio; 442 checks de contexto
aprobados y 0 fallidos; 856 IDs de localización y 337 claves runtime.

---

## La fogata se puede autorizar, construir y guardar; el Cubo llega al panel de ciudadanos

**2026-08-05**

Tres defectos de EG-5 quedaban entre el jugador y su primera fogata.

Autorizar la fogata fuera del horario laboral asignaba al fundador y no
construía nada. El campamento fundacional debe ignorar la jornada 08:00–16:00
—no hay refugio al que volver y así lo declaraba `CURRENT_STATUS.md`—, pero la
excepción sólo estaba aplicada en la simulación por tick. La movilización al
asignar, la confirmación de llegada, la frontera día/noche y la reanudación de
órdenes permanentes seguían consultando el reloj crudo. El resultado: el
fundador quedaba **en casa**, la obra se quedaba en `WorkersInTransit` y ningún
error se registraba en ninguna parte. La regla pasa a tener un único dueño,
`CityWorld.IsLaborTime()`, y las cinco compuertas la leen; ahora el fundador
sale hacia la obra a cualquier hora, la llegada no se revierte y cruzar las
16:00 no lo retira del sitio. Que la jornada deba empezar con el Ayuntamiento en
lugar del refugio queda como pregunta de producto, sin decidir aquí.

Y una obra detenida ya dice por qué. La tira de estado mostraba `Obra 0/180` sin
más: el motivo existía en el dominio (`ConstructionStopCause`) y el panel de
construcción lo describía, pero la única superficie siempre visible no lo leía
—los edificios sí exponían el suyo, las obras no—. Ahora el chip añade el motivo
y su tooltip lo explica: falta elegir módulo, contribuyente agotado, en camino,
faltan materiales, nadie contribuye. Con eso a la vista se diagnostica el caso
que parecía una obra muerta: la fogata **estaba construida** y el sitio esperaba
que el jugador eligiera el módulo siguiente, imposible de pagar con una rama en
el inventario. El juego no estaba roto; estaba mudo.

Y elegir ese módulo ya es posible de verdad. La interfaz existía —los botones de
petate, depósito y copa— pero desactivada y sin decir por qué: el coste vivía en
el snapshot y ninguna etiqueta lo mostraba. Ahora cada opción se lista con lo que
pide y lo que hay (`Petate: 2 ramas (disponible: 1) + 3 fibra vegetal
(disponible: 0)`), y el tooltip del botón detalla lo que falta. La carga del
fundador pasa a estar plegada por defecto: desplegada llenaba el cuerpo del panel
y empujaba fuera de vista la fase, el estado y justamente esos costes. Sigue a un
clic y su cabecera ya indica cuántos tipos se transportan.

La expresión física deja de ser invisible. El onboarding la decide junto con la
afinidad elemental, pero sólo la afinidad se mostraba: ni el perfil del héroe, ni
la carta de llegada del fundador, ni la carta final del onboarding la nombraban.
Ahora las tres la muestran, con las dos familias de arma que implica y una nota
de que las naturales entrenan a ritmo completo y el resto a una décima parte. La
carta de llegada además cumple por fin lo que DEC-0013 le exige —afinidad y los
tres ejes del Cubo— en una versión compacta de dos líneas, acorde a los dos
segundos que dura en pantalla. No hay pantalla de equipamiento porque no hay
equipamiento: `SetEquipmentLoadout` sólo se invoca desde pruebas y no existe
catálogo de armas. Lo que se muestra son afinidades de aprendizaje, no un
inventario, y el texto lo dice.

Talar un nodo y construir sobre el suelo que dejaba rompía el guardado. El
dominio ya trataba una unidad agotada como suelo libre —oculta su sprite y
`FrontageState` devuelve `Available`—, pero el validador de guardado contaba
toda posición autorizada como bloqueada para siempre. Las cuatro compuertas de
emplazamiento aceptaban la obra y el guardado posterior lanzaba; `TrySaveNow`
convertía la excepción en un aviso, así que la ciudad seguía viva **sin
guardar** hasta que el jugador cerraba y perdía el emplazamiento. Ahora el
validador filtra las unidades sin reserva y coincide con el dominio: el jugador
puede construir donde despejó, y la partida persiste. El esquema no cambia
—sigue en `v30`— porque la forma del JSON es la misma y la regla sólo **amplía**
lo que valida; los saves que hoy se rechazan vuelven a cargar. Como consecuencia,
devolver carga al suelo ya no puede resucitar un recurso bajo un edificio: el
parche elige la unidad elegible menos abastecida y lo que rechaza regresa al
inventario, de modo que ninguna carga se destruye.

Seleccionar al fundador en el panel de ciudadanos lanzaba
`ArgumentOutOfRangeException`. DEC-0013 dejó al fundador sin afinidades
profesionales y el panel seguía indexándolas por posición. En lugar de tapar el
hueco, el panel ahora muestra la identidad que el ciudadano sí tiene: naturaleza
de combate —afinidad, **expresión física** y las dos familias de arma naturales
que implica— y los tres pares del Cubo con su firma de linaje. La expresión
física no se veía en ninguna pantalla hasta ahora. El detalle de prospectos del
Ayuntamiento comparte el mismo bloque, lo que además retira una llamada que
lanzaba con un estilo de combate vacío. Las estadísticas derivadas siguen fuera:
exigen arma equipada y condición resuelta, y ninguna tiene origen todavía.

La firma de linaje viajaba como clave dinámica y no existía en ningún catálogo,
así que la build inglesa la mostraba en español. Ya está traducida, y una prueba
fija las ocho firmas en ambos catálogos porque el validador no puede ver una
clave que se construye en tiempo de ejecución.

Baseline medido: build con 0 errores y 0 warnings; 816 pruebas aprobadas, 0
fallidas, 1 omitida (817 total); arranque headless limpio; 442 checks de
contexto aprobados y 0 fallidos; 856 IDs de localización y 337 claves runtime.
La referencia rota a los capítulos de linaje en `CONTEXT_MAP.md`, heredada del
incremento anterior, quedó corregida para que la validación vuelva a 0 fallos.

---

## Cubo Kovari y primera derivación auditable de estadísticas

**2026-08-04**

El onboarding del fundador ahora conserva su linaje canónico, afinidad
elemental, memoria narrativa y perfil del Cubo Kovari. Al terminar, el jugador
ve los tres pares del Cubo como tendencias narrativas —sin porcentajes planos—
junto con el linaje y la afinidad. El scoring histórico de linaje continúa
decidiendo el resultado mientras el cubo se calcula en paralelo en modo sombra.

Cada `Citizen` dispone además de naturaleza de combate inmutable, competencia
por familia de arma, canales del arma, apoyos temporales de las cinco piezas de
armadura y condición resuelta. La capa de dominio puede solicitar bajo demanda
potencias física y elemental, vida, defensas, mitigaciones, regeneración,
curación y stats de tempo con un desglose auditable; equipar o retirar objetos
no muta el Cubo persistido. Afinidad y expresión física describen la
manifestación, pero no multiplican los canales.

El esquema cruza `v28 -> v29 -> v30`: primero incorpora el resultado canónico
del onboarding y después las fuentes persistentes de estadísticas. Saves
antiguos reconstruyen el Cubo desde el vértice 60/40 y conservan su afinidad;
la ausencia se normaliza a Silencio sin repetir el onboarding. Un Citizen sano
recibe condición neutral; uno herido queda explícitamente sin resolver para no
inventar una regla futura entre heridas y condición.

Baseline medido: build con 0 errores y 0 warnings; 794 pruebas aprobadas, 0
fallidas, 1 omitida (795 total); arranque headless limpio; 814 IDs de
localización y 329 claves runtime. La validación de contexto conserva 432
checks aprobados y 9 fallidos por referencias/mirrors ya desincronizados. La
captura se omitió tras reproducir un bloqueo del pipe de salida de Godot; el
snapshot Full se completó con `-SkipCapture`.

---

## Session state and changelog contract

**2026-08-03**

Infrastructure, not gameplay. `docs/session-state/` now holds a generated
`STATE.txt` and a dated `1280×720` frame of the city, and this file exists.

The problem it solves: `CURRENT_STATUS.md` and
`docs/ai/CURRENT_DEVELOPMENT_STATE.md` are written by hand and had drifted to
728 and 721 passing tests against a real 730, and to 761 template IDs against a
real 804. Both were corrected against the measurement in the same change.

`tools/New-SessionSnapshot.ps1 -Mode Fast` runs from a `SessionStart` hook and
reads git and source only, so it cannot delay a session start. `-Mode Full`
measures build, tests, headless boot, agent context and catalogs, and drives
the existing visual harness for the screenshot; it is what runs before a
session's first commit. Neither mode can abort a session: a failing probe is
recorded as a failing probe and the rest still run. Unverified fields say "not
measured this session" instead of restating the previous run.

Rule added to `CLAUDE.md` §3 / §5.1 and `AGENTS.md` §3 / §5.1 — the hook covers
Claude Code, the written rule is the only trigger under Codex.

---

## Author guard: no AI agent may appear as a contributor

**2026-08-03**

Nine commits carried a `Co-Authored-By: Claude <noreply@anthropic.com>`
trailer; three of them additionally had Claude as the **author and
committer**, which would have surfaced Claude as a GitHub contributor with
its own avatar. The remote `origin` was configured but had no branches, so
rewriting history was cheap; that window will not stay open after the first
push.

`git filter-branch` with `--all` was run once:

- `noreply@anthropic.com` is reassigned to the repository owner
  (`3l33f3@gmail.com`) wherever it appeared as `GIT_AUTHOR_*` or
  `GIT_COMMITTER_*`.
- `Co-Authored-By:` and AI-domain `Signed-off-by:` trailers are stripped
  from every message. Other body text is left alone.
- Original commit dates and content are preserved (`git diff
  refs/original/refs/heads/main HEAD --stat` is empty).

Prose in `CLAUDE.md` and `AGENTS.md` had carried the rule already. Prose
failed; prose alone is a request, not a guard. The repository now carries
`.githooks/commit-msg`, which rejects:

- Any `Co-Authored-By:` or `Signed-off-by:` trailer.
- Any `Generated with …` notice naming an AI agent.
- The robot marker `🤖`.
- Any author or committer identity whose email or display name matches an
  AI agent (anthropic.com, openai.com, GitHub-managed copilot addresses,
  or names like `Claude` / `Codex` / `Copilot`).

`tools/Install-AuthorGuardHook.ps1` points `core.hooksPath` at
`.githooks`, idempotent and safe to re-run. The snapshot script runs it on
every `-Mode Full` and reports the resulting state on its `Author guard`
line. The override `git commit --no-verify` exists; using it requires a
written reason in the final report.

The full pre-rewrite history is preserved in
`%TEMP%\wog-authorship-backup\pre-authorship-rewrite.bundle` for the day
something needs to be cross-checked, and `git reflog` still points to the
pre-rewrite `refs/original/*` copies until they expire.

---

## EG-4 — resource expeditions on a dynamic frontage grid

**2026-08-03 · `2d949f6c`**

### Connected

- The Campfire and the Cache each expose one finite Food and Wood opportunity.
  Dispatch reserves supply, opportunity and bounded return capacity; completion
  depletes it; cancellation and retreat release it.
- Mature-tree Wood requires the durable Primitive Axe, crafted at the Shelter
  from 1 Branch + 1 Small Stone and kept in its tool set. The first forestry
  capability is a made object instead of a free verb.
- Gathering rejects full storage before movement or drain, and treats a repeated
  request for an exhausted unit idempotently.
- Resource quantities left the status bar. They progress contextually from
  founder cargo in Construction, through the Founding Cache, to the Shelter's
  collapsible inventory.

### Reshaped

- The fixed nine-lot parcel partition became continuous frontage rows. A
  resource unit occupies only its own frontage cell instead of claiming the
  surrounding 3×3 lot; buildings reserve explicit column intervals guarded by
  persisted corridors; resources and constructions share one obstacle-footprint
  contract, of which trees are one case.
- Fresh cities expose three horizontal available parcels. No locked frontier is
  rendered or reconnoitred while expansion and its terrarium boundary language
  stay under design. Legacy parcel records are preserved.

### Schema

v24 → v28, one migration per seam.

| Version | Migration |
| --- | --- |
| v25 | Continuous frontage rows and persisted protected corridors. |
| v26 | Deterministic resource-unit positions that do not claim whole lots. |
| v27 | Finite Food/Wood opportunities, their expedition reservation and bounded return capacity. |
| v28 | The durable tool set, without granting tools to migrated saves. |

### Baseline

`dotnet build` 0 errors / 0 warnings · `dotnet test` 730 passed, 1 skipped ·
headless boot clean · agent context 437 checks · schema v28.

### Direction

Recorded in `docs/world-of-goses-design-bible/12_DYNAMIC_FRONTAGE_PLOTS_AND_CORRIDORS.md`,
which supersedes the rigid nine-lot partition previously described in chapter 03.

---

## Doc consolidation — Ravatha, Cubo Kovari y onboarding

**2026-08-04**

Documentation only. No code, schema, tests, build, baseline or
catalog numbers change in this commit; it captures the consolidation of
22 delivered docs (the `ravatha_lore_package`, the
`RAVATHA_LINEAGE_SYSTEM_GUIDELINES` and the
`KOVARI_CUBE_ONBOARDING_INTEGRATION_GUIDELINE`) into the canonical
design bible.

### Connected

- `bible/13_KOVARI_CUBE.md` — single source of truth for the cube
  mechanics: geometry, the three axes (Cuerpo/Vínculo,
  Estabilidad/Impulso, Dominio/Alcance) with their six canonical stat
  names and cultural aliases, the eight lineage vertices, the six
  elemental affinities (Tierra, Éter, Agua, Fuego, Neutra/Silencio,
  Aire) as independent cube faces, derived stats with explicit
  breakdown, equipment as channel-and-demand (Weight, Demand,
  MaxIntegrity, CurrentCondition, ElementalResonance,
  ElementalTolerance, WearProfile), shadow-mode coexistence with the
  current lineage scoring, migration and fallback rules.
- `bible/14-21_LINEAGES_*.md` — one chapter per lineage (Ardhen,
  Eirune, Kovari, Myrven, Vaelun, Orveth, Caelith, Theryn), each with
  §1 Cultura, §2 Sistema jugable, §3 Firma sistémica and
  §4 Vértice del Cubo. The eight line signatures are canonized:
  Anclaje, Corola, Reconfiguración, Rumbo, Custodia, Adaptación,
  Resonancia, Síntesis.
- `bible/06_LINEAGES.md` rewritten as a one-table index that links to
  the eight lineage chapters and to `bible/13_KOVARI_CUBE.md`.
- `bible/07_ONBOARDING_AND_FOUNDER.md` Result section reduced to
  `FounderOnboardingResult { Lineage, ElementalAffinity, CubeProfile,
  NarrativeMemory }`; the prologue's seven scenes (Before the Sky,
  Interference, Separation, Sky of Ravatha, Descent, Impact, Wait)
  are added as canonical narrative sequence.
- Agent and skill routing updated to point at the bible: the
  `lineages-and-cultures`, `narrative-lore` and `citizens-rpg`
  skills, the `narrative-lore` agent, and `docs/ai/CONTEXT_MAP.md`
  routes `Onboarding`, `Founder`, `Lineages` and `Narrative`.

### Reshaped

- The three delivered packages are no longer canonical. They live
  under `docs/_archive/ravatha-source-2026-08-04/` as a historical
  source, including the two `.zip` originals. The README in the
  archive maps every archived file to its bible destination.
- `DEC-0013` is added to `docs/ai/DECISION_LOG.md` and records:
  onboarding output is the cube profile only (no Traits,
  WeaponPreferences, ProfessionalAffinities, CombatStyle,
  PoliticalOrientation, SpiritualPosture, LeadershipStyle or
  RiskProfile); six canonical stat names; 60/40 base + ±8 onboarding
  range; six elemental affinities as cube faces; equipment is channel
  not power; eight line signatures; lore + systems consolidated into
  bible/13-21 with the original packages archived.

### Schema

None. No `WorldSave` version bump; no persisted field changes. The
migration and fallback rules in `bible/13_KOVARI_CUBE.md` are
forward-looking and apply when the cube schema is introduced.

### Baseline

Unchanged from EG-4. Build, tests, headless boot, agent-context
validation, schema version and locale catalogs are not modified by
this commit.

---

## Reconstructed history

Thin entries, recovered from commit subjects only. See each commit for content.

| Date | Commit | Subject |
| --- | --- | --- |
| 2026-07-31 | `9fc2542c` | Implement early-game resource and cultivation progression |
| 2026-07-31 | `124df29a` | Discard VS-5 and ship EG-1 resource seam |
| 2026-07-30 | `6e11a5a7` | Record why the splash working copies stay untracked |
| 2026-07-30 | `d2881bb4` | Track the AI-generated splash art as redraw reference |
| 2026-07-30 | `0fd7b55c` | Add EG-0 opening measurement, ambient day/night tint and the splash hero view |
| 2026-07-30 | `fc0bf57f` | Re-spread the Ardhen/Orveth/Vaelun accents and derive splash palettes |
| 2026-07-29 | `86db1355` | Stabilize persistent first playable loop |
| 2026-07-29 | `f2d066c8` | Add agent-context infrastructure for Codex and Claude Code |
| 2026-07-28 | `13525c96` | Advance VS-1/VS-2 vertical slice and fix macro-view/pathfinding bugs |
| 2026-07-28 | `41b7699c` | Stabilize the first playable city loop |
| 2026-07-27 | `23791ca0` | Complete localization sweep, real navmesh routing, biome terrain, ambient citizens, expedition FSM, and real frame profiling |
| 2026-07-26 | `1a5774af` | Migrate macro city view to pseudo-3D street perspective |
| 2026-07-26 | `d7eec26c` | Stabilize terrain and localization foundations |
| 2026-07-24 | `d0fd51d3` | Add astral founder flow and polish city UI |
| 2026-07-23 | `91268ddb` | Add persistent parcel resource gameplay |
| 2026-07-23 | `89c70981` | Integrate precomposed appearance variants across 192 bundles |
| 2026-07-22 | `b409d248` | Split status bar into PlayPause + Speed, forest gatherability, auto-release workers |

Earlier than 2026-07-22, `git log` is the only record.
