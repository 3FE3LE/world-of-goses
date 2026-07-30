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

### Prioridad del slice

Hasta cerrar VS-5 no se inicia profundidad nueva. Solo se admiten:

1. Correcciones encontradas durante el recorrido humano del primer loop.
2. Evidencia necesaria para firmar los 17 criterios de aceptación.
3. Limpieza documental o técnica que no cambie el producto.

### Baseline vigente

- Fecha de alineación: **2026-07-29**.
- Slice activo: **VS-5 — firma y repetición**.
- Próximo trabajo aprobado: **terminar el diagnóstico VS-5 y abrir EG-0 como
  prerrequisito de cierre**.
- Save: **schema v19**.
- Build: **0 errores / 0 advertencias**.
- Tests: **553 / 553**.
- Arranque Godot headless: correcto.
- Fuente de verdad del slice: `docs/CURRENT_STATUS.md` y
  `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`.

## 1. Resumen

| Estado | Crítica | Alta | Media | Baja |
| --- | ---: | ---: | ---: | ---: |
| En curso | 1 | 0 | 1 | 0 |
| Pendiente | 0 | 0 | 2 | 0 |
| Necesita reanálisis | 0 | 2 | 0 | 0 |
| Diferido por trigger | 0 | 2 | 1 | 0 |
| Bloqueado | 0 | 0 | 0 | 0 |

## 2. Milestone activo — primer loop completo y repetible

Flujo objetivo:

```text
Onboarding → gathering → Shelter/Farm/Quarry → prospecto/reclutamiento
→ asignación y presión de Food → preparación de expedición
→ salida/encuentro/objetivo o retirada/regreso
→ herida y territorio → tratamiento/nueva decisión
→ guardado/carga → segunda expedición sin reset
```

### Estado por fase

| Fase | Estado | Evidencia vigente |
| --- | --- | --- |
| VS-0 — ciudad causal | Hecho | Orden/compromiso, tránsito visible, stock lleno, descanso y equivalencia offline. |
| VS-1 — reclutamiento y coste de oportunidad | Implementado; firma pendiente | Ayuntamiento, prospecto expedicionario, vivienda, disponibilidad explicada y ración diaria de Food. |
| VS-2 — expedición mínima | Hecho | Equipo de 1–2 citizens, suministros, retirada, encuentro determinista, objetivo/retorno y persistencia. |
| VS-3 — consecuencias y territorio | Hecho | Herida persistente, tratamiento Shelter/Food/tiempo y parcela de cuatro estados. |
| VS-4 — persistencia | Hecho | Schema v19; reload por fases y tratamiento; resolución exacta una vez. |
| VS-5 — firma y repetición | En curso | Recorrido humano iniciado; G1 reabierto y faltan relanzamientos visibles. |

### 🔴 VS-5 — Firma humana y repetición

- **Estado:** En curso.
- **Prioridad:** Crítica.
- **Afecta:** flujo completo de `CityPrototype.tscn`, save principal,
  `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`, `docs/VISUAL_REGRESSION.md`.
- **Prueba automatizada existente:**
  `VerticalSliceRepetitionTests.RecoveredCity_CanCompleteSecondExpeditionWithoutReset`.
- **Trabajo restante:**
  1. Empezar con un slot limpio y completar onboarding.
  2. Recolectar Wood y construir Shelter, Farm y Quarry mediante UI normal.
  3. Construir Ayuntamiento, obtener un prospecto por expedición y aceptarlo
     con vivienda disponible.
  4. Asignar y retirar varios citizens; verificar razones de indisponibilidad.
  5. Observar varios días y confirmar que la ración de Food crea una decisión
     legible sin bloquear el bootstrap.
  6. Preparar una expedición con citizens reales, suministros y postura de
     retirada.
  7. Ver salida, encuentro, objetivo o retirada, regreso y resumen causal.
  8. Verificar herida, tratamiento y desbloqueo territorial.
  9. Cerrar y relanzar a mitad de expedición.
  10. Cerrar y relanzar a mitad de tratamiento.
  11. Iniciar un segundo ciclo sin reset ni herramientas de depuración.
  12. Firmar contención 1280×720 y 1920×1080, además de foco por teclado y
      gamepad en las superficies usadas.
- **Aceptación:** los 17 criterios de
  `docs/FIRST_PLAYABLE_LOOP_AUDIT.md` pasan sin editor, fixtures o comandos de
  depuración.
- **Regla:** cualquier bug hallado se corrige y se vuelve a recorrer desde el
  último límite persistente relevante.

#### Checkpoint para la próxima sesión

- Slot limpio recorrido mediante UI hasta Shelter, Farm, Quarry, Town Hall,
  prospecto `Inara`, dos citizens y primera `Community Contact` completada.
- Verificados: selección sin camera-follow automático; WASD/flechas solo cámara;
  gathering, construcción, entrada y producción visibles; Quarry sin bloqueo en
  puerta; wheel de panel sin zoom del mapa; espera en casa explicada por stock
  lleno.
- La primera expedición completa dura ahora 600 ticks (4 horas simuladas,
  10 minutos a 1x, 2,5 minutos a 4x). Prueba:
  `ExpeditionTeamTests.FirstLoopTemplates_LastFourSimulatedHours`.
- G1 queda **reabierto**: Farm alcanzó 60 Food y dos residentes consumen 2 Food
  al día; no existe una decisión alimentaria significativa.
- **Primera acción al reanudar:** desasignar al fundador de Quarry, reunir
  2 Wood, pausar, despachar `Reconnaissance` y cerrar/reabrir mientras siga en
  `Outbound`. Después completar encuentro/regreso, herida, tratamiento con
  relanzamiento y segunda repetición.
- Cuando termine el diagnóstico restante, iniciar EG-0 desde
  `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md`; la firma final de VS-5
  se repite sobre ese early game corregido.

### Estado de los 17 criterios

| # | Criterio abreviado | Estado |
| ---: | --- | --- |
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

### 🟡 M-14 — Matriz de regresión visual

- **Estado:** En curso como contrato transversal.
- **Prioridad:** Media.
- **Afecta:** `tools/Capture-VisualMatrix.ps1`,
  `docs/VISUAL_REGRESSION.md`, escenas/UI tocadas por cada cambio.
- **Hecho:** harness read-only con medidas reales en 1280×720 y 1920×1080,
  manifiesto, frame-time del engine y fixtures de las superficies principales.
- **Pendiente para VS-5:** navegación/foco completo por teclado y gamepad,
  close paths usados en el recorrido y firma humana de estados que no pueden
  probarse headless.
- **Aceptación:** ningún cambio de UI se cierra solo con boot headless.

## 4. Pendientes después de VS-5

### 🟡 M-25 — Feedback causal de importancia grande

- **Estado:** primer corte implementado.
- **Afecta:** `UiMotion`, `ModalHost`, Chronicle, retornos, construcción y
  llegada de citizens.
- **Pendiente:** firma humana y feedback grande coherente para obra completada,
  regreso expedicionario y llegada/aceptación de citizen.
- **Aceptación:** vuelve a reposo, no bloquea input, respeta reduced-motion
  futuro y no modifica dominio ni ticks.

### 🟡 M-12 — Exclusión de overlays transitorios

- **Estado:** Pendiente.
- **Afecta:** `Notifier.cs`, `TutorialOverlay.cs`, `OfflineReportPanel.cs`.
- **Problema:** toast, error, tutorial y Chronicle poseen posiciones de forma
  independiente; pueden coincidir en pantalla.
- **Dirección:** un host con slots/prioridad solo si VS-5 reproduce el solape o
  el siguiente slice necesita overlays simultáneos.
- **Aceptación:** save toast + error + tutorial no se solapan ni capturan input
  incorrectamente.

## 5. Necesita reanálisis

### 🟠 H-26 — Huellas, corredores y navegación

- **Estado:** Necesita reanálisis.
- **Motivo:** el primer modelo de parcelas/huellas existe, pero la vista plana
  que originó parte del trabajo fue retirada. La vista jugable usa
  `MacroStreetLiveView`, `StreetRoutePlanner` y `NavigationServer2D`.
- **Reanálisis requerido:** definir una sola correspondencia entre huella de
  dominio, anclas del edificio, obstáculos de la perspectiva y categorías
  pasillo/camino/calle. No mantener dos mallas autoritativas.
- **Pendiente comprobable:** entrada frontal alcanzable; corredores conectados;
  ningún camino válido termina aislado o cruza obstáculos.
- **No iniciar antes de VS-5** salvo que bloquee el recorrido humano.

### 🟠 H-32 — Cierre de la perspectiva por calles

- **Estado:** Implementación principal terminada; necesita reanálisis de cierre.
- **Hecho:** es la única vista macro jugable; construcción, selección, gather,
  ciudadanos, cámara, detalle y expediciones están conectados.
- **Pendiente:** confirmar en juego real el paso entre hileras de árboles, la
  visibilidad durante gather y que no quede ninguna dependencia funcional de la
  vista plana retirada.
- **Aceptación:** ciudad completa jugable desde la perspectiva, sin rutas que
  atraviesen obstáculos, teletransportes o carriers duplicados.

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

### Seguimiento S-1

| Subítem | Estado/trigger |
| --- | --- |
| S-1.1 i18n | Hecho para UI actual; mantener EN/ES y validador. |
| S-1.2 NavigationServer2D | Primer corte hecho; reconciliar dentro de H-26. |
| S-1.3 terreno perspectiva | Primer corte hecho; no forzar TileMap sobre trapecios proyectados. |
| S-1.4 MultiMesh | Diferido al trigger de H-30. |
| S-1.5 FSM | Seam mínimo hecho; ampliar solo con comportamiento autónomo real. |
| S-1.6 diálogos | Seam mínimo hecho; ampliar con H-31. |
| S-1.7 profiler | Hecho; mantener medición real del engine en matrices. |

## 7. Hechas recientes

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

### 🟠 H-29 — Terreno ortogonal de la vista plana

- **Cierre:** Superado el 2026-07-29.
- **Motivo:** `MacroStreetLiveView` es la única representación macro jugable.
  Consolidar `OrthogonalParcelTerrain` ya no mejora el producto actual. La
  necesidad vigente de terreno/obstáculos queda cubierta por H-26/H-32.

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
- Criterios del loop: `docs/FIRST_PLAYABLE_LOOP_AUDIT.md`.
- Validación contra visión: `docs/VALIDATION.md`.
- Persistencia/rutinas: `docs/CITIZEN_OFFLINE_ROUTINE_AUDIT.md`.
- Matriz visual: `docs/VISUAL_REGRESSION.md`.
- Decisiones: `docs/ai/DECISION_LOG.md`.
