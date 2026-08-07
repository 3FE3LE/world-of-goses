# Secuencia de prototipo original — extraída del capítulo 10

**Archivado:** 2026-08-07
**Origen:** `docs/world-of-goses-design-bible/10_TECHNICAL_ARCHITECTURE_AND_ROADMAP.md`
**Motivo:** el capítulo mezclaba canon técnico con un plan de trabajo. El canon
se quedó; esto es el plan.

> **Histórico. No es la secuencia vigente.** El orden que gobierna hoy es
> EG-0 → EG-1 → EG-2 → EG-3 → EG-4 → EG-5 → EG-6, definido en
> `docs/EARLY_GAME_RESOURCE_AND_EXPEDITION_PROPOSAL.md` §15, y la cola
> accionable vive en `TO_DO.md`. Este documento se conserva para saber qué se
> planeó al conceptualizar el proyecto y contra qué se puede contrastar lo que
> realmente se construyó. No se edita.

## Por qué se archivó y no se actualizó

El mapa de escenas describía una estructura que nunca existió: `scenes/city/`,
`scenes/buildings/MineDetailView.tscn`, `scenes/gardens/`,
`scenes/expeditions/`. En 2026-08-07 el proyecto tiene diecisiete `.tscn` y
ninguno de esos nombres. Mantenerlo dentro de la biblia lo presentaba como
canon — como una descripción de la estructura correcta — cuando era una
propuesta que la implementación descartó.

La secuencia de quince pasos tuvo el mismo problema en sentido inverso: se
cumplió casi entera, pero por una ruta distinta y con otros nombres, y el
proyecto la sustituyó por la secuencia EG del proposal. Dos secuencias
canónicas compitiendo es peor que una sola desactualizada.

---

## Escenas sugeridas

```text
scenes/
├── city/
│   ├── MacroStreetLiveView.tscn
│   ├── PlotView.tscn
│   └── MacroCitizenDot.tscn
├── buildings/
│   ├── BuildingDetailView.tscn
│   ├── MineDetailView.tscn
│   ├── FarmDetailView.tscn
│   └── HospitalDetailView.tscn
├── gardens/
│   └── GardenDetailView.tscn
├── gathering/
│   └── GatheringDetailView.tscn
├── citizens/
│   ├── CitizenDetailedView.tscn
│   └── CitizenPortraitView.tscn
├── expeditions/
│   ├── ExpeditionView.tscn
│   ├── ExpeditionMemberView.tscn
│   └── ExpeditionSegmentView.tscn
└── ui/
```

`MacroStreetLiveView.tscn` represents the walkable camera world described in
"Cámara y mundo caminable", rather than a static view.

## Primer slice

```text
Ciudad macro
→ mina seleccionable
→ escena detallada
→ ciudadanos asignados
→ producción
→ UI temática
→ audio básico
```

### Contenido

- Asentamiento central.
- Mina.
- Granja.
- Actividad macro.
- Panel superior.
- Menú lateral.
- Tema de un linaje.
- Dos trabajadores iniciales.
- Asignación y remoción.
- Producción y almacenamiento.
- Bloqueos visibles.

## Segundo slice

```text
salida
→ caminar
→ enemigo
→ combate automático
→ destino
→ regreso
```

Usar un ciudadano existente convertido en héroe.

## Orden sugerido

1. Ciudad macro y selección.
2. Escena de mina.
3. Asignación.
4. Producción.
5. Afinidad y experiencia.
6. Almacenamiento y bloqueos.
7. Tema visual.
8. Audio básico.
9. Parcela bloqueada.
10. Expedición.
11. Desbloqueo.
12. Retorno herido.
13. Tratamiento.
14. Guardado.
15. Progreso offline.
