# TO DO — Cola operativa

> Backlog vivo del proyecto. Git conserva el historial; este archivo conserva
> únicamente el estado vigente, las tareas accionables y los triggers de trabajo
> diferido. Antes de tomar un ítem se debe releer el código y los documentos que
> figuran en `Afecta`.

## 0. Reglas de mantenimiento

### Estados

| Estado | Uso |
| --- | --- |
| Pendiente | Puede tomarse cuando el slice activo lo permita. |
| En curso | Se está trabajando o falta una firma concreta para cerrarlo. |
| Bloqueado | Requiere una decisión o dependencia externa. |
| Necesita reanálisis | El contexto cambió y la solución anterior puede ser obsoleta. |
| Diferido | Tiene un trigger explícito que todavía no se cumple. |
| Hecho | Cerrado recientemente; se elimina después de dos días calendario. |
| Superado | Ya no aplica; se conserva dos días con el motivo. |

### Prioridad del increment

> **2026-07-31 — cambio de norte histórico.** Se descarta `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`
> (VS-5 y sus 17 criterios). El proyecto aún no tiene las dos capas de
> complejidad (Founding Site + plot lifecycle + resource seam) que pide
> `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` antes de hablar de herida,
> tratamiento y territorios desbloqueables. Wound/recovery/territory loops
> quedaron diferidos hasta EG-2 + EG-3 + EG-5 estables.

> **2026-08-10 — prioridad reanalizada por DEC-0020.** La regla anterior que
> impedía abrir toda profundidad de combate antes de cerrar EG-5/EG-6 queda
> **superada**. La excepción aprobada es estrecha: primero se entrega EG-5V, un
> Spirit Trail Founder-only con encuentro visual automático, continuación al
> objetivo y regreso. La profundidad amplia de combate sigue diferida.

Hasta que la acceptance test de `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`
§17 no se cumpla en un slot limpio nuevo, no se abre profundidad nueva. Solo se
admiten:

1. Correcciones que un playtest de la apertura EG-A0 exponga como bloqueantes.
2. Avance de los increments en el orden vigente de §15:
   EG-5V → EG-5C → EG-6.
3. Limpieza documental o técnica que no cambie el producto.

### Baseline vigente

- Fecha de alineación: **2026-08-10**.
- Slice activo: **EG-5V — Founder Spirit Trail visual vertical**. EG-4 ya cerrado;
  `Branches/PlantFiber/SmallStone/WildFood` en `ResourceType`,
  `SeedStartingOpportunities` siembra EG-A0 en parcels libres, `GatherFromPatch`
  genérico y cap carried de 6 unidades; tests en `Eg1ResourceSeamTests`.
  Founding Site 3×3 con módulos Campfire/Bedroll/Cache/Canopy, capacidad
  6→12→24, mismo ID/parcela y finalización offline. El primer Cultivation Site
  requiere Shelter, 1 Branch + 1 Small Stone, 180 work y produce 5 Food tres
  días después de sembrar 1 Food, con la misma frontera live/offline.
  Primera noche jugable (H-33 + H-34) entregada sin schema bump;
  zoom-in máximo del macro view elevado a 3.0; identidad del fundador
  ya no vive en un `ScrollContainer` (ver §7 2026-08-06).
- Save: **schema v33** (`DEC-0021`): sesión incremental del Spirit Trail con
  paso lógico e historial reproducible de AUTO/manual. v32 (`DEC-0019`) renombró la cara del cubo `Mastery` a
  `Domain` en disco, con puente nullable para que un save v31 no pierda el
  cubo del fundador. v31 primera noche autoral; v30 estadísticas derivadas
  por citizen; v29 onboarding canónico; v28 herramientas durables;
  v27 oportunidades finitas de Food y Wood y capacidad de retorno.
- Build: **0 errores / 0 advertencias**.
- Tests: **1154 / 1155** (1 omitido por brittleness del snapshot JSON en
  `VerticalLoopPersistenceTests.Recovery_ReloadedHalfway`; el comportamiento
  no cambió, sólo los IDs auto-incrementados de eventos).
- Localización: **1049 IDs de plantilla, 324 claves de runtime** válidas.
- Agent context: **474 checks** sin fallos.
- Arranque Godot headless standalone: limpio; el pipeline Full/captura sigue
  intermitente y conserva un fallo previo `-1073741819` como deuda de harness.
- Fuente de verdad: `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` y
  `docs/CURRENT_STATUS.md`.
- La implementación histórica de Parcel 9 y sus tints, conservada más abajo
  como registro, queda **superada** para ciudades nuevas: no se renderiza
  frontera desbloqueable ni se selecciona objetivo territorial hasta definir
  el borde y la expansión del terrario.

## 1. Resumen

| Estado | Crítica | Alta | Media | Baja |
| --- | ---: | ---: | ---: | ---: |
| En curso | 1 | 0 | 1 | 0 |
| Pendiente | 0 | 0 | 1 | 0 |
| Necesita reanálisis | 0 | 0 | 0 | 0 |
| Diferido por trigger | 0 | 2 | 0 | 0 |
| Bloqueado | 0 | 0 | 0 | 0 |

Las dos Altas y la Media en curso eran las fases 2, 3 y 4 de la primera
noche (H-33, H-34, M-26 en §3) — todas entregadas en `main` los días
2026-08-06. La Media que queda en curso es M-26 propiamente, porque su
aceptación exige playtest humano en slot limpio, y eso entra por el
contrato transversal de M-14. M-29 (ciudadano corriente con sólo dos
expresiones) cerró el 2026-08-07 vía `DEC-0019`.

## 2. Increment activo — Apertura EG-A0 (proposal §15)

Flujo objetivo prioritario de la apertura temprana:

```text
Onboarding astral → primera noche
→ amanecer / SpiritDeparted → Spirit Trail disponible
→ primera expedición del Founder
→ primer encuentro visual antes de ~5 min de gameplay
→ continuación hacia el objetivo → regreso a la ciudad
→ consolidación: plots 2–3 + forestry gate + Farm
→ segundo ciclo sin reset
```

### Estado por increment

| Increment | Estado | Evidencia vigente |
| --- | --- | --- |
| EG-0 — medición del early game | Hecho | Schema v20; `eg0-report.txt` se actualiza por save; suspende observer durante Restore para no contar el inventario recargado. |
| EG-1 — resource/storage seam | Hecho | Schema v21; `Branches/PlantFiber/SmallStone/WildFood` en `ResourceType`; `SeedStartingOpportunities` siembra EG-A0; `GatherFromPatch` genérico; cap carried de 6 unidades; tests en `Eg1ResourceSeamTests`. |
| EG-2 — founding site seam | Hecho | Schema v22; Founding Site estable con Campfire → Bedroll/Cache → Canopy, capacidad 6→12→24, origen persistente y equivalencia offline. |
| EG-3 — Food horizon seam | Hecho | Schema v24; primer Cultivation Site completo, Food horizon visible y equivalencia live/offline cubierta por `Eg3CultivationSiteTests`. |
| EG-4 — resource expedition seam | Hecho | Schema v27; oportunidades finitas de Food/Wood, cadena completa, supply/oportunidad/capacidad reservados y equivalencia live/offline. |
| EG-5V — Founder Spirit Trail visual | En curso | Siguiente entrega: un reloj, ciudad/viaje/combate paralelos, Founder-only, cuatro slots visibles (2–4 bloqueados), cuatro skills octogonales (solo Skill 1), encuentro lateral automático, objetivo y regreso. |
| EG-5C — consolidación | Pendiente | Forestry gate ya conectado; conserva segundo/tercer plot + Farm consolidation detrás de EG-5V. |
| EG-6 — calibration/signature | Bloqueado | Espera EG-5V y EG-5C. |

### 🟡 M-14 — Matriz de regresión visual

- **Estado:** En curso como contrato transversal.
- **Prioridad:** Media.
- **Afecta:** `tools/Capture-VisualMatrix.ps1`,
  `docs/VISUAL_REGRESSION.md`, escenas/UI tocadas por cada cambio.
- **Hecho:** harness read-only con medidas reales en 1280×720 y 1920×1080,
  manifiesto, frame-time del engine y fixtures de las superficies principales.
- **Pendiente para EG-1:** contención visual de los nodos Branches/Plant
  Fiber/Small Stone/Wild Food en macro view; firma humana de su comportamiento
  de recogida.
- **Aceptación:** ningún cambio de UI se cierra solo con boot headless.
| 1 | Fundador persistente | Firma humana obtenida |
| 2 | Gathering y tres edificios iniciales | Firma humana obtenida |
| 3 | Reclutamiento restringido | Firma humana obtenida |
| 4 | Asignar/remover citizens | Firma humana obtenida |
| 5 | Producción causal Farm/Quarry | Firma humana obtenida |
| 6 | Presión/consumo significativo | Falló; G1 reabierto para EG-0+ |
| 7 | Compromiso exclusivo | Automatizado |
| 8 | Plan expedicionario real | Automatizado |
| 9 | Fases y regreso | Automatizado |
| 10 | Encuentro determinista y explicable | Automatizado |
| 11 | Regreso cambia al menos dos ejes | Automatizado |
| 12 | Herida persistente | Automatizado |
| 13 | Tratamiento causal | Automatizado |
| 14 | Territorio desbloquea oportunidad | Automatizado |
| 15 | Decisión post-regreso | Implementado; firma humana pendiente |
| 16 | Save/offline exacto por límites | Automatizado; relanzamientos humanos pendientes |
| 17 | Repetición sin reset/debug | Automatizado; firma humana pendiente |

## 3. En curso

### 🔴 H-35 — EG-5V: primer Spirit Trail visual del Founder

- **Estado:** En curso; sesión de combate observable implementada, vertical
  espacial y flujo post-dawn todavía incompletos.
- **Prioridad:** Crítica.
- **Afecta:** `CityWorld`, `Expedition`, `ExpeditionRequest`,
  `ResourceExpeditionRules`, `CombatEncounter`, `ExpeditionPanel`,
  `ExpeditionRail`, nueva `ExpeditionLiveView`, input map, snapshots,
  persistencia/offline y fixtures visuales.
- **Estado real del HEAD:** `ExpeditionLiveView` y sus componentes existen; una
  `CombatSession` propiedad de `CityWorld` avanza por tick mundial, persiste
  comandos/replay en schema v33 y conecta Basic Attack, AUTO/manual Skill 1,
  cooldown, HP, enemigos y outcome. `SpiritTrailSearch` aún dura 180 ticks,
  consume 1 Food y devuelve Wood 4/6/8; `ExpeditionRequest.MaxTeamSize` es 2;
  no existen posición, `AttackRange` ni knockback; `CombatDebugPanel` sigue
  debug-only; y
  `CityWorld.TryStartExpedition` todavía exige Campfire + Cache a toda
  oportunidad de recurso, incluido el Spirit Trail que nace con
  `SpiritDeparted`.
- **Orden interno obligatorio:**
  1. separar `SpiritTrailSearch` del gate genérico Campfire + Cache para que el
     flujo aprobado pueda despacharse tras `SpiritDeparted`, sin relajar el gate
     de las salidas materiales EG-4;
  2. **Cerrado:** integrar expedición y combate sobre el único reloj de mundo,
     con `CombatSession` incremental y replay persistido;
  3. **Cerrado:** mostrar cuatro slots de vanguardia, Founder en 1 y 2–4
     bloqueados;
  4. **Cerrado:** mostrar cuatro skills octogonales y conectar solo
     `expedition_skill_1` (`expedition_skill_2`–`4` quedan reservados);
  5. Basic Attack automática ya conectada; falta avance solo para entrar en
     `AttackRange`, sin kiting; knockback con `Stability` reduciendo e `Impulse`
     aumentando el desplazamiento posible;
  6. continuar tras el encuentro hasta el objetivo y regresar a la ciudad;
  7. demostrar que entrar/salir de `ExpeditionLiveView` conserva 1x/2x/4x y
     que ciudad, viaje y combate avanzan en paralelo sin pausa.
- **Spirit Trail:** aproximadamente cuatro horas de mundo; no consume Food por
  existir; propósito narrativo; recompensa material abierta.
- **Fuera de alcance:** Traits, Chains, carroza, `SPACE`, formación avanzada,
  Skills 2–4 funcionales, proceduralidad y profundidad amplia de combate.
- **Deuda abierta de rendimiento:** con la sesión de combate conectada,
  `expedition-live-early` mide 51–55 ms de `Performance.Monitor.TimeProcess`
  contra un presupuesto de pico de 40 ms; el mismo fixture medía 18,7 ms antes
  de conectarla y `macro-hud-default` mide 6–18 ms en la misma tanda, así que
  el coste es propio de la vista en vivo y no ruido de máquina. Medir antes de
  añadir movimiento, `AttackRange` o knockback: el punto 5 sólo agrega trabajo
  por frame sobre un presupuesto ya excedido.
- **Aceptación:** desde slot limpio, el primer encuentro visual aparece dentro
  de unos cinco minutos de gameplay; la expedición alcanza objetivo y regreso;
  save/load y live/offline conservan límites semánticos y resultados exact-once.

### Primera noche del fundador (`docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md`)

Un playtest expuso que talar un árbol pide un hacha primitiva sin decir cómo
obtenerla, y que el tutorial de tres tarjetas mentía sobre las recetas reales.
La respuesta canónica no es una cadena de tutoriales modales —el proposal §442
la prohíbe y `18_LINEAGES_VAELUN.md:218` refuerza "no simple selector de
misión"— sino una **primera noche jugable** de `00:00` a `06:00` donde un
espíritu de fuego enseña las causas. Fases 0 y 1 cerradas (ver §7); quedan tres.

Los tres niveles de guía permanecen **separados** y no se fusionan en una lista
de misiones: la primera noche es autoral y finita; las directivas derivadas del
proposal §9 son sistémicas y operan después del amanecer; el Camino consultable
es de solo lectura.

#### 🟠 H-33 — Fase 2: espíritu de fuego y diálogo

- **Estado:** Hecho el 2026-08-06. Ver §7.
- **Prioridad:** Alta.
- **Afecta:** `Domain/Dialogue.cs` (contratos), nuevo
  `Domain/FireSpiritDialogueCatalog.cs`, `Domain/FirstNightState.cs`,
  `Prototypes/MacroStreetLiveView.cs`, `CityMacroSnapshot.cs`,
  `Ui/OverlayLayers.cs`, `game/locale/*`.
- **Hecho:** catálogo `FireSpiritDialogueCatalog` con 6 IDs estables y 8
  variantes de cuerpo por linaje (48 claves, todas bajo el prefijo
  `firstnight.*`); banda de diálogo no modal `FirstNightDialogueStrip` en
  `OverlayLayers.Tutorial = 50` con `MouseFilter.Stop` sólo en el strip
  inferior; `FireSpiritVisual` con anillo de 16 puntos y glyph triangular,
  posición derivada de la etapa vía `ProjectDepth`; `FirstNightContextCommentary`
  como `Notifier`-based ephemeral; `IconPaths.Fire` + `game/assets/ui/icons/24/fire.svg`
  promueve el asset (cierra trigger M-22); `[Signal] FirstNightStageChanged`
  en `CityWorldController` + `FirstNightScene` como host del strip + visual;
  `TryOpenFirstNightDialogue` / `TryCloseFirstNightDialogue` expuestos. Sin
  `using Godot` bajo `Domain/`. `DialogueRunner.RunAsync` queda intacto.
- **Aceptación:** cubierta — tests `FirstNightDialogueCatalogTests` (10
  casos), `FirstNightDialogueNoLiteralDigitsTests` (3 casos), build 0/0,
  tests 901/902 antes del bloque M-26.

#### 🟠 H-34 — Fase 3: amanecer y motivo de la primera expedición

- **Estado:** Hecho el 2026-08-06. Ver §7.
- **Reanalizado:** el gate `SpiritDeparted` se conserva; la conversión
  180 ticks + 1 Food → Wood 4/6/8 queda superada por `DEC-0020` y debe
  reemplazarse dentro de H-35.
- **Prioridad:** Alta.
- **Afecta:** `Domain/ResourceOpportunityKind.cs`,
  `Domain/ResourceExpeditionRules.cs`, `ExpeditionPanel.cs` y su `.tscn`,
  `Domain/Persistence/WorldPersistence.cs`, Chronicle.
- **Hecho:** `WorldEventKind.SpiritDeparted` emitido por
  `CityWorld.AdvanceFirstNight` al cruzar `Sleeping → Concluded` y marcado
  significativo en `WorldEventRetention.IsSignificant`. Nuevo
  `ResourceOpportunityKind.SpiritTrailSearch = 2` con cuerpo en
  `ResourceExpeditionRules.Definition` (180 ticks, 1 Food, recompensa
  Wood 4/6/8). Tercer botón `SpiritButton` en `ExpeditionPanel.tscn`,
  cableado en `ExpeditionPanel.cs` y gateado por
  `ExpeditionPlanningSnapshot.SpiritTrailUnlocked` derivado del log.
  Embers primitive (`FirstNightEmbers.cs`) sobre la fogata cuando
  `SpiritDeparted` ya está en el chronicle y la fogata sigue construida.
  Seeding de la oportunidad `SpiritTrailSearch` en el mismo tick del
  amanecer. Sin schema bump — `Enum.TryParse` tolera el nuevo valor y la
  serialización string del kind sigue igual.
- **Fricción resuelta:** `ExpeditionPanel` ahora cablea tres botones por
  `NodePath` (`SpiritTrailObjectiveButtonPath`); el switch del switch
  del `Definition` gana el brazo `SpiritTrailSearch`.
- **Aceptación:** cubierta — tests `FirstNightSpiritDepartedTests` (5 casos)
  + `SpiritTrailOpportunityTests` (7 casos), build 0/0.

#### 🟡 M-26 — Fase 4: documentación canónica y firma visual de la noche

- **Estado:** Cerrado el 2026-08-06. **Firma humana pendiente** vía M-14.
- **Prioridad:** Media.
- **Hecho:** doc 19 → Aceptada, **DEC-0014** registrado, invariantes de la
  primera noche añadidas a `CROSS_DOMAIN_INVARIANTS.md`, ruta "First night /
  fire spirit" y placeholder "Tools and inventory" en `CONTEXT_MAP.md`,
  apertura EG-A0 reescrita en `CURRENT_STATUS.md` y
  `EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`, cuatro fixtures nuevos en
  `VISUAL_REGRESSION.md` (`firstnight-strip`, `firstnight-spirit`,
  `firstnight-embers`, `firstnight-spirit-trail-button`).
- **Pendiente (sólo firma humana):** playtest en slot limpio con el recorrido
  descrito en M-14 → Pendiente para EG-5V / M-26. `verify-clicks-with-real-clicks`
  aplica de lleno: la banda de diálogo y el hit-rect del espíritu son
  superficies nuevas de input, y un ancestro con `MouseFilter.Stop` puede
  tragárselas sin que el código lo delate.

### 🟡 M-14 — Matriz de regresión visual

- **Estado:** En curso como contrato transversal.
- **Prioridad:** Media.
- **Afecta:** `tools/Capture-VisualMatrix.ps1`,
  `docs/VISUAL_REGRESSION.md`, escenas/UI tocadas por cada cambio.
- **Hecho:** harness read-only con medidas reales en 1280×720 y 1920×1080,
  manifiesto, frame-time del engine y fixtures de las superficies principales.
- **Pendiente para EG-5C:** firma humana de recolección idempotente, bloqueo por
  capacidad/hacha, layout disperso de tres parcelas y fabricación del hacha en
  el Shelter; firma del chevron de recursos y del aviso pasajero icono +
  cantidad siguiendo al fundador antes del Cache y sobre el almacén después,
  confirmando que barra global y Chronicle no recuperan contadores rutinarios;
  firma de la retícula de construcción completa y sus estados hover `[OK]`/`[X]`.
- **Aceptación:** ningún cambio de UI se cierra solo con boot headless.

## 4. Pendientes después de EG-2

### 🟡 M-12 — Exclusión de overlays transitorios

- **Estado:** Pendiente.
- **Afecta:** `Notifier.cs`, `OfflineReportPanel.cs`, y la banda de diálogo de
  la primera noche cuando H-33 la construya.
- **Problema:** toast, error, guía y Chronicle poseen posiciones de forma
  independiente; pueden coincidir en pantalla.
- **Dirección:** un host con slots/prioridad solo si un playtest de la apertura
  EG-A0 reproduce el solape, o si el siguiente increment requiere overlays
  simultáneos. `TutorialOverlay.cs` ya no existe (borrado el 2026-08-05), así
  que el tercer participante histórico desapareció; H-33 reintroduce uno con la
  misma ranura `OverlayLayers.Tutorial = 50` y vuelve a hacer el solape posible.
- **Aceptación:** save toast + error + banda de diálogo no se solapan ni
  capturan input incorrectamente.

## 5. Necesita reanálisis

_(Vacío: los reanálisis pendientes se resolvieron o se cerraron como
superados en 2026-07-30; ver §8.)_

## 6. Diferido por trigger

### 🟠 H-30 — Representación masiva de citizens/NPCs

- **Trigger:** más de 20–25 citizens visibles o evidencia del profiler.
- **Estado actual:** `CitizenSpriteBank`/carriers son correctos para la escala
  presente.
- **Antes de implementar:** medir fixtures de 25 y 50 entidades. MultiMesh o
  batching se justifican solo si existe cuello de botella.

### 🟠 H-31 — Primer diálogo ramificado con NPC real

- **Trigger:** primer NPC conversable con una decisión persistente.
- **Estado actual:** `DialogueRunner` tiene seam y tests, pero ningún consumidor
  jugable.
- **Aceptación futura:** EN/ES, mouse/teclado/gamepad, elección persistida,
  reentrada determinista y evento causal; evaluar addon solo con necesidad real.

### 🟡 M-22 — Integración selectiva de assets

- **Trigger:** una necesidad de legibilidad concreta del slice activo.
- **Estado actual:** inventario/licencias documentados; iconos, atlas y cursor
  necesarios ya promovidos.
- **Regla:** no importar paquetes completos ni habilitar Settings/minimap sin
  un slice aprobado.

### 🟡 M-29 — El ciudadano corriente sólo puede tener dos expresiones físicas

- **Estado:** Cerrado el 2026-08-07 vía `DEC-0019`.
- **Cierre:** `CubeScoring.GenerateOrdinaryProfile(lineage, seed)` desplaza
  el vértice `±8` por eje con FNV-1a; el sobre y el invariante de pareja
  siguen siendo los del onboarding. Todo ciudadano no fundador tiene ahora
  un cubo propio y reproducible a partir de `(linaje, id)`, y las seis
  expresiones físicas son alcanzables en una población mixta. La
  generación queda restringida a `CitizenProfile.TryCreate(lineage, …,
  cube, …)` con un cubo explícito: `CreateMigrantProfile` es el único
  llamador en producción y `TestHelpers.NewProfile()` sigue usando el
  vértice puro, así que ningún test heredado cambió su cubo sin pedirlo.
- **Migraciones existentes no se reescriben:** `MigrateV29ToV30` sólo
  rellena el vértice cuando el cubo es nulo, y las ciudades guardadas
  antes de esta fecha conservan su población uniforme; la variación
  aparece sólo en los migrantes que lleguen a partir de ahora. La
  consecuencia ya estaba anunciada en la propia nota de `M-29`.

### 🟡 M-27 — Convenciones técnicas heredadas del capítulo 10

- **Origen:** eran «preguntas abiertas» en
  `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE.md`. No lo eran:
  tienen dueño y respuesta comprobable, así que bajan aquí el 2026-08-07.
- **Trigger:** que se retome elevación de terreno (`H-26` / `S-1.2`) o la
  expansión lateral bloqueada en `S-1.3`.
- **Pendiente:**
  1. Convención de tileset con elevación: cuántos niveles, alto en píxeles por
     nivel, y tiles de rampa/escalera/puente.
  2. Convención de proyección pseudo-3D por calle: factores de achicamiento
     vertical/horizontal por profundidad, cuántas calles visibles a la vez, y
     el número final de calles de la ciudad. Hoy la ventana es de 13 y el
     sobre objetivo 8×9, pero ninguno de los dos está fijado como convención.
  3. Colisión de nombres: «calle» como fila de profundidad de la perspectiva
     macro vs. «calle» de `H-26` como corredor de 2 tiles para navmesh.
     Reconciliar o renombrar; hoy el mismo término significa dos cosas en
     documentos que un agente lee juntos.

### 🟡 M-28 — `ARCHITECTURE.md` §8 se quedó en v28

- **Trigger:** ninguno; es corrección de documentación y puede tomarse cuando
  el slice activo lo permita.
- **Estado actual:** la cadena de migraciones narrada en
  `docs/ARCHITECTURE.md` §8 termina en v28 mientras
  `WorldSave.CurrentVersion` es 31. Los tres pasos que faltan (v28→v29
  onboarding canónico, v29→v30 fuentes de estadísticas derivadas, v30→v31
  primera noche) están descritos en `docs/CURRENT_STATUS.md` §1 pero nunca
  volvieron a la prosa de arquitectura.
- **Aceptación:** §8 narra hasta la versión vigente, o deja de narrar la
  cadena y remite a un único lugar que sí la mantenga.

### Seguimiento S-1

| Subítem | Estado/trigger |
| --- | --- |
| S-1.1 i18n | Hecho para UI actual; mantener EN/ES y validador. |
| S-1.2 NavigationServer2D | Primer corte hecho; reconciliar dentro de H-26. |
| S-1.3 terreno perspectiva | Primer corte hecho; se rechazó el overview estirado. Una única perspectiva y zoom uniforme muestran una ventana móvil de 13 calles sobre el sobre objetivo 8×9. La captura vacía final aún cuesta 15,201 ms: implementar culling/batching lateral y validar navegación por ventanas antes de habilitar expansión; no forzar TileMap sobre trapecios proyectados. |
| S-1.4 MultiMesh | Diferido al trigger de H-30. |
| S-1.5 FSM | Seam mínimo hecho; ampliar solo con comportamiento autónomo real. |
| S-1.6 diálogos | Seam mínimo hecho; ampliar con H-31. |
| S-1.7 profiler | Hecho; mantener medición real del engine en matrices. |

## 7. Hechas recientes

### 2026-08-06

- **H-33 + H-34 + M-26 — Primera noche del fundador jugable.** Tres ítems
  cerrados en una sola tanda de 17 commits en `main`. Lo nuevo, en
  dominio: `FireSpiritDialogueCatalog` con 6 IDs y 8 variantes de cuerpo
  por linaje (48 claves bajo `firstnight.*`), `FirstNightState` / `FirstNightRules`
  / `FirstNightState` sellado y persistido, `Tr.FirstNight` con 55
  claves, `WorldEventKind.SpiritDeparted` significativo, nuevo
  `ResourceOpportunityKind.SpiritTrailSearch` con recompensa Wood.
  Presentación: `FirstNightDialogueStrip` (Control no modal en
  `OverlayLayers.Tutorial=50`), `FireSpiritVisual` (anillo 16 puntos +
  glyph triangular, posición derivada), `FirstNightEmbers` (cuadrilátero
  naranja post-departure), `FirstNightContextCommentary` (helper efímero
  vía Notifier), `FirstNightScene` (host que conecta strip + visual con
  el `[Signal] FirstNightStageChanged`), `IconPaths.Fire` + promoción
  del `fire.svg` (cierra trigger M-22). Expedición: tercer botón
  `SpiritButton` en `ExpeditionPanel.tscn`, gate por
  `ExpeditionPlanningSnapshot.SpiritTrailUnlocked`. Sin schema bump
  (string serialisation + `Enum.TryParse` tolerante). Docs:
  `docs/world-of-goses-design-bible/23_FIRST_NIGHT_AND_FIRE_SPIRIT.md` Propuesta → Aceptada,
  **DEC-0014** en `docs/ai/DECISION_LOG.md`, invariantes en
  `docs/ai/CROSS_DOMAIN_INVARIANTS.md`, ruta "First night / fire spirit" +
  placeholder "Tools and inventory" en `docs/ai/CONTEXT_MAP.md`, 4 fixtures
  nuevos en `docs/VISUAL_REGRESSION.md`, entrada en `CHANGELOG.md`.
  Tests añadidos: `FirstNightDialogueCatalogTests` (10),
  `FirstNightDialogueNoLiteralDigitsTests` (3), `FirstNightSpiritDepartedTests`
  (5), `SpiritTrailOpportunityTests` (7). Build 0/0, tests 913/914.
  **Firma humana pendiente** vía M-14.

### 2026-08-06 — correcciones post-entrega

- **Zoom-in máximo elevado de 2.2 a 3.0** en
  `MacroStreetLiveView.cs:83`. Sin tocar `StreetDepthProjection` (el zoom
  es un `Scale`/`Position` post-proyección alrededor del pivote fijo).
  Test `MaximumZoom_AllowsACloserViewThanThePreviousLimit` actualizado
  en lockstep para no quedar pinned al valor anterior.
- **`AstralOnboardingView` sin `ScrollContainer` para la identidad.**
  El `LineEdit` de nombre, la fila de género y el `LineageSpritePlayer`
  de previsualización ya no viven dentro del scroll; pasan a una fila
  horizontal fija (`HBoxContainer` con sprite a la izquierda y
  nombre+género a la derecha) que cabe en 1280×720 sin scroll. La
  información se reorganiza para que toda la vista —progreso, 12
  fragmentos, título, narrativa, opciones, consecuencia, footer—
  quepa sin necesidad de `ScrollContainer`. Sin impacto en el dominio.

### 2026-08-05

- **M-25 — Feedback causal de importancia grande (primer corte implementado,
  2026-08-05).** `UiMotion.LargeEventSeconds` (0.45s, no bloquea input) +
  `UiMotion.Pulse` + `ModalHost.RevealModal/HideModal` ya cubren obra
  completada (`BuildingPlot._hitOutline`, `MacroBuildingView._outline`),
  guardado (`CityStatusPanel._savedChip`, `OfflineReportPanel` filas nuevas),
  recursos (`ResourceTree`) y entrada de detail view
  (`BuildingDetailView.FadeIn`). El sistema no modifica dominio ni ticks y
  respeta `UiMotion` como gramática de presentación única para que un ajuste
  futuro de reduced-motion toque un solo sitio. Firma humana y coherencia de
  feedback para obra completada, regreso expedicionario y llegada/aceptación
  de citizen quedan abiertas como verificación visual del usuario.

### 2026-08-05

- **Primera noche — Fase 0: cinco defectos confirmados por el playtest.**
  1. **El contribuidor no se liberaba al terminar un módulo intermedio.**
     `CompleteFinishedProjects` solo liberaba en el Canopy; para Campfire,
     Bedroll y Cache hacía `RaiseProjectChanged` y nada más. El fundador quedaba
     con `Commitment.Kind = Construction` sobre una obra sin trabajo activo,
     luego `IsAvailable` falso, luego `NaturalResourceGatherAvailability`
     respondía `HeroUnavailable` y el botón de recolectar quedaba deshabilitado
     — justo cuando toca recoger los materiales del módulo siguiente. El jugador
     tenía que adivinar que debía desasignarse a sí mismo desde el panel.
     `TryAuthorizeFoundingSiteModule` ahora re-moviliza al autorizar: la cadena
     antes se sostenía por el propio bug.
  2. **El chip de "fuera de horario" mentía.** Leía `GameClock.IsDaytime` crudo
     en vez de `IsLaborTime()`, así que anunciaba trabajo detenido durante toda
     la apertura mientras el fundador construía. La señal viaja ahora por
     `CityStatusSnapshot.IsLaborTime`, no por un cálculo de presentación.
  3. **`TryAdvanceQuiescentTicks` ignoraba el bypass.** Mismo lector crudo, y
     además reetiquetaba `AwaitingModule` como `NoWorkers`. Nuevo
     `ResolveQuiescentProjectStopCause` conserva la precedencia de
     `ConstructionSimulation.SimulateTick`, así que el lote no puede reportar
     una causa que el camino por tick nunca produciría.
  4. **`GatherWood` drenaba Wood sin comprobar el hacha.** Sin llamadores de
     gameplay, pero expuesta en `CityWorldController`: cualquier panel con
     referencia al controller podía saltarse el forestry gate. Los dos
     envoltorios se eliminaron y el método de dominio es `internal` (los ~60
     usos son fixtures de test). `ToolGatheringTests` escanea la fuente para que
     nadie lo reexponga.
  5. **`CreateRestartedCityKeepingHero` era asimétrico:** sembraba bosques pero
     no oportunidades. No era un softlock vivo —`TryLoadFromPrimarySlot`
     resiembra tras recargar escena— pero el JSON escrito en disco quedaba sin
     recursos de suelo ni oportunidades, y su Campfire (3 Branches + 2 Small
     Stone pagados por completo al autorizar) era impagable.
  Además: **`TutorialOverlay.cs` borrado** con su nodo, su gancho de captura,
  sus claves y sus fixtures. Sus tres pasos escritos a mano pedían "1 wood de
  los Forest plots" y describían chips de la barra de estado ya eliminados; se
  retiró en vez de reescribirse porque una lista de pasos a mano vuelve a
  desfasarse en el increment siguiente. **`OnboardingView.cs` borrado** (608
  líneas de onboarding legacy sin ningún instanciador). `OnboardingView.tscn`
  **se conserva**: es la escena que lleva el script `AstralOnboardingView`.
- **Primera noche — Fase 1: el estado de dominio (schema v31).**
  `FirstNightStage` con nueve etapas, `FirstNightState` sellado y persistido, y
  `FirstNightRules` con todo lo cuantitativo derivado de `FoundingSiteRules`, de
  modo que un cambio de receta no puede volver a desfasar la guía. Cada avance
  lo dispara un hecho del mundo —módulo completado o nodo de diálogo cerrado—
  nunca un temporizador. Tres decisiones que sostienen el diseño:
  1. **`_tick` nunca se congela.** Congelarlo pararía la construcción, que
     depende de `_tick % ConstructionRules.WorkIntervalTicks`, y crearía la
     circularidad de "no puedes cumplir el hito porque el reloj que lo mide está
     detenido". El amanecer es la transición de etapa; la hora mostrada se
     estanca en `05:59` (`FirstNightState.DisplayedTick`) en vez de saltar al
     concluir; y la noche **difiere el calendario**: ni ración de Food ni cruce
     de frontera día/noche mientras `Stage != Concluded`.
  2. **No se persiste la posición del espíritu.** Se deriva de la etapa, como
     los anclajes de edificios derivan de la placement. La ciudad no guarda
     coordenadas visuales autoritativas y una aparición temporal no es motivo
     para empezar.
  3. **El Bedroll gana su primer significado mecánico:** `HasRestingPlace()`.
     Hasta ahora era solo coste, trabajo y prerrequisito del Canopy, y
     `CitizenLocation.AtHome` es el valor por defecto de un citizen, no prueba
     de que exista refugio. Sin Bedroll la noche se niega a dormir.
  `MigrateV30ToV31` marca **concluida** la noche de todo save existente: esas
  ciudades ya pasaron su apertura y meterlas en la secuencia las atraparía tras
  hitos que no pueden volver a cumplir. Consecuencia descubierta al correr los
  tests: `TestHelpers.NewHeroWorld()` está genuinamente en su primera noche, así
  que tres tests de ración fallaron con razón; se añadió
  `TestHelpers.ConcludeFirstNight` y se aplicó a los constructores que describen
  ciudades ya pasadas su apertura. Las cadenas de migración escritas a mano en
  `ToolGatheringTests`, `DynamicFrontageTests` y `CitizenStatisticsPersistenceTests`
  pasaron a `MigrateToCurrent`, que las hace inmunes al próximo bump.
  16 casos nuevos en `FirstNightTests`. Build 0/0; tests 879/880.

### 2026-07-31

- **EG-1 — resource/storage seam (schema v21).** Cuatro nuevos
  `ResourceType`: `Branches`, `PlantFiber`, `SmallStone`, `WildFood`.
  Cambios en `CityWorld`:
  1. `GatherFromPatch(int patchId, int? unitId, int amount)` —
     drain genérico de cualquier `NaturalResourcePatch`, no sólo los
     Forests. El Forest legacy mantiene `WoodUnitReserves` mirrorado
     para no romper el recipe gate.
  2. `SeedStartingOpportunities()` — siembra EG-A0 en parcels libres:
     14 Branches (7×2), 6 Plant Fiber (3×2), 6 Small Stone (3×2),
     8 Wild Food (4×2). Idempotente y silenciosa cuando no hay
     parcelas libres, así que un save con Home/Farm/Town Hall en
     parcelas 3–6 no se altera retroactivamente.
  3. `CarriedGroundResourceCapacity = 6` — los cuatro recursos nuevos
     comparten un cap de carga de 6 unidades (proposal §4). Wood,
     Stone, Food y compañía ignoran el cap porque van a per-building
     storage. EG-2 reemplaza este cap con la capacidad física vigente:
     carried = 6, Cache = 12, Shelter = 24.
  4. `PatchChanged` event + `WorldEventSubjectKind.Patch` para que el
     presentation layer pueda refrescar overlays de suelo.
  Persistencia: `WorldSave.CurrentVersion = 21` + `MigrateV20ToV21` (sólo
  bump de versión; los nuevos tipos aparecen vía `SeedStartingOpportunities`
  en el Restore). `SeedStartingOpportunities` se llama tanto en
  `TryCompleteOnboarding` como en el path de Restore para que partidas
  nuevas y legacy con parcelas libres tengan los mismos recursos EG-A0.
  8 tests nuevos en `Eg1ResourceSeamTests` cubriendo distribución,
  idempotencia, gather con cap y migración. Build 0/0; tests 663/664.
- **EG-2 — founding site seam (schema v22).** `ConstructionProject` conserva
  un ID/parcela mientras ejecuta Campfire → Bedroll/Cache → Canopy; Bedroll y
  Cache aceptan ambos órdenes y Canopy exige los tres módulos previos. Cada
  módulo usa 180 de trabajo (un cuarto del presupuesto shelter de 720) y paga
  su coste completo al autorizarlo para que el fundador único no quede ocupado
  mientras todavía necesita recoger inputs. Cache eleva el cap conjunto a 12;
  el Home consolidado lo eleva a 24 y persiste los módulos de origen. La
  interfaz ofrece la decisión de módulo sin autoelegirla y una acción explícita
  que devuelve toda la carga al terreno evita los soft-locks de inventarios 6/6
  o 12/12 mal recogidos entre módulos. `MigrateV21ToV22` preserva Homes y Basic
  Shelter legacy sin inventar historia. 11 casos en `Eg2FoundingSiteTests` cubren ambos órdenes,
  recuperación de carga, identidad, capacidades, snapshot, round-trip y
  equivalencia live/offline. Build 0/0; tests 680/681 (1 omitido conocido).
- **EG-3 — Food horizon seam (schema v24).** Un Cultivation Site posterior al
  Shelter consume 1 Branch + 1 Small Stone y 180 de trabajo; sembrar consume
  1 Food, persiste `readyAtTick`, madura exactamente tras 10.800 ticks live u
  offline y cosechar deposita 5 Food. El HUD muestra ración, horizonte y target
  protegido; la macro view distingue Prepared/Sown/Growing/Ready/Spent sin
  depender sólo del color. `MigrateV23ToV24` agrega una lista vacía sin
  inventar parcelas. Diez casos en `Eg3CultivationSiteTests`, interacción real
  y matriz 1280×720/1920×1080 verificadas. Build 0/0; tests 704/705 (1 omitido conocido).
- **Visual de TerritoryState en macro view.** El único parcel bloqueado
  (Parcel 9, `LogicalColumn = 4`) quedaba fuera del área renderizada
  (`WorldParcelColumns = 4`) y, aunque las Available no tenían tint
  distintivo, no había forma de saber que existía territorio
  descubrible. Tres cambios en `MacroStreetLiveView.cs`:
  1. `WorldParcelColumns = 5` para que Parcel 9 entre al viewport.
  2. Nuevo método `DrawParcelTerritoryTints` que pinta una banda
     trapezoidal por columna de parcel, con color según
     `CityParcel.TerritoryState`:
     - `Locked` → opaco oscuro (0.08/0.07/0.05/0.78)
     - `Reconnoitred` → mostaza translúcida (0.86/0.72/0.28/0.32)
     - `RouteSecured` → verde oliva translúcida (0.47/0.62/0.34/0.22)
     - `Available` → sin overlay (terrain tal cual)
     El tint se proyecta sobre la misma grilla perspectiva que el piso,
     así que respeta el offset de cámara lateral y el vanishing point.
  3. Corrección 2026-08-03: el envelope ahora se deriva del snapshot de
     parcelas reales; `(4,1)` no existe y ya no se dibuja como franja/terreno
     fantasma. El tint ocupa la profundidad completa de cada parcela real.
  4. Constantes nuevas en la sección de colores:
     `LockedParcelColor`, `ReconnoitredParcelColor`,
     `RouteSecuredParcelColor`.
  Con esto una expedición `Reconnaissance` FullSuccess avanza Parcel 9
  por tres estados (Locked → Reconnoitred → RouteSecured → Available)
  y el jugador ve el cambio de color en tiempo real. Build + tests
  siguen verdes (655/656).
- **ESC global iterativo + audit de overlay.** Tres cambios en la cadena
  de input para que un solo ESC cierre exactamente un overlay y no se
  quede atrapado al abrir varios a la vez:
  1. `CityPrototype._UnhandledInput` llama a
     `CityWorldController.ReturnToCity()` cuando recibe `ui_cancel` —
     HeroProfileView y BuildingDetailView cierran vía
     `OnSelectionChanged`. Se ejecuta **después** de ModalHost y
     PauseMenu (leaf-first en Godot), así que respeta la prioridad
     existente.
  2. `PauseMenu._UnhandledInput` ya no abre el menú con ESC cuando está
     oculto; sólo lo cierra cuando está visible. El menú conserva su
     botón dedicado. Evita que el primer ESC "se lo coma" el menú de
     pausa y nunca cierre la vista de héroe o el detalle de edificio.
  3. `ModalHost.CompleteClose` ahora descarta el `_content` si ya está
     disposed antes de tocarlo (`!GodotObject.IsInstanceValid(_content)`).
     Antes, una ruta que liberaba el content a mitad de la animación de
     cierre lanzaba `ObjectDisposedException` y dejaba el modal
     visible.
  Auditoría de `OverlayLayers`: las 16 invocaciones están en la capa
  correcta según el contrato documentado en `OverlayLayers.cs`. World=0,
  AmbientTint=5, Hud=6, ContextMenu=8, SelectionInfo=9, Chronicle=10,
  ModalScrim=20, Modal=21, PlacementOverlay=40, Tutorial=50,
  Onboarding=80, FounderArrival=90, PauseAndNotifier=100. El scrim del
  ModalHost está en ModalScrim=20 (debajo del Modal=21), así que el
  panel siempre se ve sobre el scrim. Las vistas que reemplazan el
  mundo (HeroProfile, BuildingDetail) están en Hud=6 — correctamente
  por encima del AmbientTint y por debajo de cualquier modal.
- **`ObjectDisposedException` en `IconButton.OnLineageChanged`.** El
  manejador estático de `LineageThemeRegistry.ActiveLineageChanged` puede
  sobrevivir a la liberación del nodo cuando un wrapper C# queda libre
  sin pasar por `_ExitTree`. El evento se seguía disparando sobre el
  wrapper muerto y `AddThemeColorOverride` reventaba. Fix: descartar el
  evento si `!GodotObject.IsInstanceValid(this) || !IsInsideTree()`. El
  resto de los suscriptores del registro siguen actualizándose.
- **Bypass del workday antes del primer refugio.** El juego arranca con
  el reloj en tick 0 (medianoche) pero la política de horario laboral
  08:00–16:00 estaba activa desde el inicio, así que la primera
  construcción quedaba en `ApplyNightRest` durante 8 horas. La guarda
  de `CityWorld.AdvanceWorldTick` (edificios y proyectos) se cambió a
  `isLaborTime = GameClock.IsDaytime(_tick) || !HasCompletedFirstShelter()`,
  de modo que el fundador puede construir el refugio inicial a cualquier
  hora. Una vez que se completa el primer `BuildingKind.Home`, la
  política 08:00–16:00 entra en vigor normalmente. El bypass alinea el
  juego con el espíritu de EG-A0 (campamento fundacional = labor manual
  de supervivencia, no jornada laboral ciudadana). Helpers nuevos:
  `CityWorld.HasCompletedFirstShelter()`, `TestHelpers.SetTick(world, tick)`.
  Regression tests: `ConstructionTickTests.PreShelter_BeforeFirstDawn_*`
  y `ConstructionTickTests.PostShelter_BeforeFirstDawn_*`. Test
  existente migrado a post-shelter: `Night_RecoversStamina_AndAddsNoProgress`.
- **EG-0 — medición del early game (schema v20).** `EarlyGameMetrics` acumula
  tiempo hasta el primer refugio, recursos recolectados/gastados,
  días-ciudadano ociosos, horizonte de comida y ausencia por expedición.
  `EarlyGameMetricsReport` escribe `eg0-report.txt` junto al save. Dos
  decisiones que sostienen la fiabilidad del dato:
  1. Nada se cuenta por tick: `WorldTimeAdvance` batchea los tramos
     quiescentes, así que un contador por tick subestimaría justo los periodos
     ociosos que esto mide. Todo se registra en eventos de dominio o en la
     frontera del alba, que es el camino compartido por vivo y offline.
  2. El observador del `CityResourceLedger` se **suspende durante `Restore`**.
     Sin eso, cada recarga contabilizaría el inventario entero como recién
     recolectado, lo que ensucia cualquier playtest que verifique inventario.
  Una ciudad migrada desde v19 reporta cero muestras en vez de historia
  inventada. Tests: `EarlyGameMetricsTests`.
- **Rutas de guardado unificadas.** El reporte estaba enganchado a una sola de
  las cuatro rutas (`TrySaveNow`), así que el autosave lo dejaba congelado.
  Todas pasan ahora por `CityWorldController.SaveWorldToPrimarySlot`.
- **Tests de migración desacoplados de `CurrentVersion`.** Ocho ficheros
  encadenaban cada paso a mano y afirmaban `CurrentVersion` tras una migración
  de un solo paso, así que cada bump de schema rompía ocho tests. Ahora usan
  `MigrateToCurrent` para el tramo "ponlo al día" y afirman el número literal
  del paso bajo prueba.
- **Filtro ambiental día/noche.** Curva de dos velocidades (bandas de una hora
  en 05:00–06:00 y 18:00–19:00, tramos largos con deriva lenta, todo
  smoothstep) y **mezcla multiplicativa** en vez de velo con alfa: un overlay
  alfa escala el contraste por `1-alpha` y levanta el negro, así que una noche
  lo bastante notoria aplanaba el mapa en niebla. Tests:
  `TimeOfDayColorTests`.
- **El tinte ya no toca el HUD.** `OverlayLayers` no separaba mundo de
  interfaz —la capa 0 incluía "HUD chips"— así que el HUD quedaba debajo del
  tinte. Slots nuevos `AmbientTint` (5) y `Hud` (6), reclamados por barra de
  estado, barra de navegación, `BuildingDetailView` y `HeroProfileView`; además
  el tinte espeja `MacroStreetLiveView.Visible` como segunda defensa. Tests:
  `OverlayExclusionTests`.
- **Vista del héroe rediseñada** con splash art a altura completa a la
  izquierda y columna de texto scrollable a la derecha
  (`LineageSplashRegistry`). El ancho se calcula desde la proporción de cada
  textura: los modos proporcionales de `TextureRect` lo derivan de una altura
  aún sin resolver en la primera pasada de layout, y el arte cargaba pero nunca
  se dibujaba.
- **Acentos de linaje re-separados.** Ardhen, Orveth y Vaelun compartían una
  franja ámbar de 10° con Orveth y Vaelun a **2°** — sus tintes de UI no eran
  distinguibles. Ahora cobre (~20°), oro (~45°) y caqui (~62°), cada uno más
  cerca de su propia descripción. `tools/New-LineagePalettes.ps1` refleja los
  mismos valores y **se niega a generar** un juego donde dos acentos sean
  indistinguibles: cercanos en tono *y* luminosidad *y* saturación a la vez.
  Caelith y Kovari están a 11° a propósito; se separan por luminosidad.
- **Paletas de splash** en `art/palettes/`: una común de 36 colores, ocho de 28
  por linaje y una derivada de 64 por linaje para trabajar (Pixelorama muestra
  una paleta a la vez). Generadas, no elegidas a mano.

### 2026-07-30

- Refugio (panel detalle + macro): el contador "descansando" del panel leía
  `VisibleWorkerCount + HiddenWorkerCount` (basado en `_assigned`, siempre
  vacío porque el refugio no tiene receta), mientras que los slots muestran
  `VisibleCitizens` (ciudadanos con `CitizenLocation.AtHome`). Slots y resumen
  ya no se contradicen: ambos leen la misma fuente. Se añadió también la regla
  espejo de `ShouldHideHeroInsideShelter` para citizens no fundadores
  (`ShouldHideCitizenAtHome`), de modo que cerrar el detalle del refugio ya
  no los aparca en `anchors.Entrance` (literalmente delante del edificio) —
  se ocultan como el founder. El panel de selección del edificio usa el mismo
  contador coherente. Regression tests:
  `UiSnapshotTests.BuildingDetailSnapshot_HomeCountsCitizensAtHomeNotAssignedWorkers`
  y `MacroStreetLiveViewTests.NonFounderCitizenAtHome_IsHiddenUnlessWanderingOrRecovering`.
- Botón "Enviar" de expedición silencioso: el botón se deshabilitaba sin
  feedback. Se añadió tooltip que explica la causa (sin miembro elegible o
  expedición activa). Localization keys: `ui.expedition.dispatch_no_member_hint`
  y `ui.expedition.dispatch_active_hint`.
- Horario laboral movido a 08:00–16:00 (`GameClock.WorkdayStartTick`: 0 →
  1200). Tres bugs colaterales corregidos:
  1. `TryAdvanceQuiescentTicks` usaba `DayTicks - 1` como fin de fase, lo
     que asumía que el día empezaba en tick 0. Ahora usa
     `NextWorkdayEnd/NextWorkdayStart - 1`.
  2. El mismo método restaba `dayTick` (relativo al día) de
     `lastTickInPhase` (calculado con `NextWorkdayEnd/Start`, absolutos),
     así que al cruzar medianoche el batched-advance "se saltaba" el
     dawn boundary sin disparar mobilisation. Ahora resta `_tick`.
  3. `TestHelpers.NewHeroWorld/NewConstructionWorld/WorldWithHome` ahora
     avanzan al workday vía reflection sobre `_tick` (sin disparar
     mobilisation de food-ration con ciudad recién creada).
  - `VerticalLoopPersistenceTests.Recovery_ReloadedHalfway` queda omitido
    temporalmente porque compara JSON exacto de snapshots, y los IDs
    auto-incrementados de eventos cambian al desplazarse el inicio del
    mundo de 0 a 1200.

### 2026-07-29

- VS-3: herida persistente, tratamiento y territorio.
- VS-4: persistencia/offline exacta del loop en schema v19.
- Persistencia contextual y rutinas ciudadanas: contexto semántico, rutas
  reconstruidas, ocio/espera, Políticas, autosave de tres minutos y cámara
  libre.
- Frontera rueda/zoom: un `ScrollContainer` visible conserva la rueda incluso
  al alcanzar sus límites; el mapa no hace zoom detrás de la UI.

### 2026-07-28

- VS-0: ciudad causal, tránsito, recuperación y un solo carrier visual.
- VS-2: equipo, encuentro, retirada y retorno expedicionario.

## 8. Superadas recientes

### 🔴 Regla de bloqueo de combate hasta cerrar EG-5/EG-6

- **Cierre:** Superada el 2026-08-10 por `DEC-0020`.
- **Motivo:** retrasar todo combate visual dejaba desconectadas la apertura
  narrativa, la expedición existente y la isla de combate determinista. La
  excepción aprobada es únicamente EG-5V end-to-end; la consolidación agrícola
  se conserva como EG-5C y la amplitud de combate continúa diferida.

### 🟠 H-29 — Terreno ortogonal de la vista plana

- **Cierre:** Superado el 2026-07-29.
- **Motivo:** `MacroStreetLiveView` es la única representación macro jugable.
  Consolidar `OrthogonalParcelTerrain` ya no mejora el producto actual. La
  necesidad vigente de terreno/obstáculos queda cubierta por H-26/H-32.

### 🟠 H-26 — Huellas, corredores y navegación

- **Cierre:** Superado el 2026-07-30.
- **Motivo:** la correspondencia entre huella de dominio (`ParcelPlacement` +
  `BuildingFootprintCatalog`), anclas del edificio (`BuildingVisualAnchors`)
  y obstáculos de la perspectiva (`_bandOccupancy` derivada en
  `MacroStreetLiveView.AddBandInterval` desde el mismo plot) usa el
  `Placement` como única fuente. `StreetRoutePlanner` + el guardarraíl de
  cadencia 12 Hz en `_Process` cubren el riesgo de "dos mallas
  autoritativas". Cobertura: `MacroStreetLiveViewTests`,
  `StreetDepthProjectionTests`, `StreetRoutePlannerTests`,
  `MacroInputBoundaryTests` y los fixtures de `Capture-VisualMatrix.ps1`
  que ejercitan gather entre hileras y construcción adyacente.

### 🟠 H-32 — Cierre de la perspectiva por calles

- **Cierre:** Superado el 2026-07-30.
- **Motivo:** la perspectiva por calles es la única vista macro jugable desde
  el cierre de H-29. No existe la vista ortogonal plana contra la cual
  pudiera haber dependencias residuales; el reanálisis ya no aplica. La
  cobertura de `MacroStreetLiveViewTests`, `StreetDepthProjectionTests`,
  `StreetRoutePlannerTests` y `MacroInputBoundaryTests` da por cerrado:
  gather entre hileras, construcción adyacente, entrada/salida del refugio y
  un solo carrier por citizen. El cierre del retorno expedicionario queda
  cerrado por EG-4: Campfire + Cache habilitan las salidas cortas finitas y su
  retorno usa la misma ruta visual.

## 9. Fuera de alcance hasta cerrar el prototipo

- Backend, servidor, base de datos, auth, telemetría, modding o segunda ciudad.
- Mobile, multiplayer, launcher, installer o settings completos.
- Combate completo, equipo, formaciones, mortalidad y generaciones.
- Política, cultura, comercio, economía y ambiente a escala completa.
- Profesiones profundas, instituciones, educación, relaciones y árboles de
  habilidades.
- Arte/audio final, múltiples biomas y grafo territorial amplio.
- Optimización masiva sin evidencia del profiler.

## 10. Referencias

- Estado canónico: `docs/CURRENT_STATUS.md`.
- Criterios del loop: `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`
  §15 (orden) y §17 (test de aceptación). Sustituyen a
  `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`, descartado el 2026-07-31.
- Validación contra visión: `docs/VALIDATION.md`.
- Persistencia/rutinas: `docs/CITIZEN_OFFLINE_ROUTINE_AUDIT.md` (histórico,
  esquema v19).
- Matriz visual y checklist de firma humana: `docs/VISUAL_REGRESSION.md`.
  Lo que ya se firmó: `docs/UI_AUDIT.md`.
- Decisiones: `docs/ai/DECISION_LOG.md`.
