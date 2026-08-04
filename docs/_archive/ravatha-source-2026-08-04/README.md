# Ravatha source — archivo histórico 2026-08-04

Esta carpeta conserva la documentación fuente entregada en bloque durante
la sesión del **2026-08-04** y que sirvió de insumo para la primera
expansión seria del bible en torno a los linajes fundacionales y al Cubo
Kovari.

Su contenido ya está consolidado e integrado en el bible canónico:

| Fuente archivada | Destino canónico en el bible |
| --- | --- |
| `KOVARI_CUBE_ONBOARDING_INTEGRATION_GUIDELINE.md` | [`docs/world-of-goses-design-bible/13_KOVARI_CUBE.md`](../../world-of-goses-design-bible/13_KOVARI_CUBE.md) + [`docs/world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md`](../../world-of-goses-design-bible/07_ONBOARDING_AND_FOUNDER.md) |
| `ravatha_lore_package/00_README.md` | ahora vive en `bible/06_LINEAGES.md` como índice resumen |
| `ravatha_lore_package/01_PROLOGUE_THE_FALL.md` | integrado en `bible/07_ONBOARDING_AND_FOUNDER.md` §"Implemented narrative sequence" |
| `ravatha_lore_package/02_VAELUN_THE_COMPASS.md` | [`bible/18_LINEAGES_VAELUN.md`](../../world-of-goses-design-bible/18_LINEAGES_VAELUN.md) §1 Cultura |
| `ravatha_lore_package/03_EIRUNE_THE_COROLLA.md` | [`bible/15_LINEAGES_EIRUNE.md`](../../world-of-goses-design-bible/15_LINEAGES_EIRUNE.md) §1 Cultura |
| `ravatha_lore_package/04_CAELITH_THE_CYCLE.md` | [`bible/20_LINEAGES_CAELITH.md`](../../world-of-goses-design-bible/20_LINEAGES_CAELITH.md) §1 Cultura |
| `ravatha_lore_package/05_MYRVEN_THE_MASKS.md` | [`bible/17_LINEAGES_MYRVEN.md`](../../world-of-goses-design-bible/17_LINEAGES_MYRVEN.md) §1 Cultura |
| `ravatha_lore_package/06_ORVETH_THE_RELIQUARY.md` | [`bible/19_LINEAGES_ORVETH.md`](../../world-of-goses-design-bible/19_LINEAGES_ORVETH.md) §1 Cultura |
| `ravatha_lore_package/07_THERYN_THE_OCTAGRAM.md` | [`bible/21_LINEAGES_THERYN.md`](../../world-of-goses-design-bible/21_LINEAGES_THERYN.md) §1 Cultura |
| `ravatha_lore_package/08_ARDHEN_THE_ANCHORS.md` | [`bible/14_LINEAGES_ARDHEN.md`](../../world-of-goses-design-bible/14_LINEAGES_ARDHEN.md) §1 Cultura |
| `ravatha_lore_package/09_KOVARI_THE_CUBE.md` | [`bible/16_LINEAGES_KOVARI.md`](../../world-of-goses-design-bible/16_LINEAGES_KOVARI.md) §1 Cultura |
| `ravatha_lore_package/10_KOVARI_CUBE_STATS_SYSTEM.md` | [`bible/13_KOVARI_CUBE.md`](../../world-of-goses-design-bible/13_KOVARI_CUBE.md) |
| `ravatha_lore_package/11_RAVATHA_LORE_COMPENDIUM.md` | extractos integrados en cada `bible/14-21_LINEAGES_*.md` §1 Cultura como **"Compendio Ravatha — entrada regional"** |
| `RAVATHA_LINEAGE_SYSTEM_GUIDELINES/00_README.md` | ahora vive en `bible/06_LINEAGES.md` + introducción de cada bible/14-21 |
| `RAVATHA_LINEAGE_SYSTEM_GUIDELINES/01_VAELUN_*` | [`bible/18_LINEAGES_VAELUN.md`](../../world-of-goses-design-bible/18_LINEAGES_VAELUN.md) §2 Sistema jugable |
| `RAVATHA_LINEAGE_SYSTEM_GUIDELINES/02_EIRUNE_*` | [`bible/15_LINEAGES_EIRUNE.md`](../../world-of-goses-design-bible/15_LINEAGES_EIRUNE.md) §2 Sistema jugable |
| `RAVATHA_LINEAGE_SYSTEM_GUIDELINES/03_CAELITH_*` | [`bible/20_LINEAGES_CAELITH.md`](../../world-of-goses-design-bible/20_LINEAGES_CAELITH.md) §2 Sistema jugable |
| `RAVATHA_LINEAGE_SYSTEM_GUIDELINES/04_MYRVEN_*` | [`bible/17_LINEAGES_MYRVEN.md`](../../world-of-goses-design-bible/17_LINEAGES_MYRVEN.md) §2 Sistema jugable |
| `RAVATHA_LINEAGE_SYSTEM_GUIDELINES/05_ORVETH_*` | [`bible/19_LINEAGES_ORVETH.md`](../../world-of-goses-design-bible/19_LINEAGES_ORVETH.md) §2 Sistema jugable |
| `RAVATHA_LINEAGE_SYSTEM_GUIDELINES/06_THERYN_*` | [`bible/21_LINEAGES_THERYN.md`](../../world-of-goses-design-bible/21_LINEAGES_THERYN.md) §2 Sistema jugable |
| `RAVATHA_LINEAGE_SYSTEM_GUIDELINES/07_ARDHEN_*` | [`bible/14_LINEAGES_ARDHEN.md`](../../world-of-goses-design-bible/14_LINEAGES_ARDHEN.md) §2 Sistema jugable |
| `RAVATHA_LINEAGE_SYSTEM_GUIDELINES/08_KOVARI_*` | [`bible/16_LINEAGES_KOVARI.md`](../../world-of-goses-design-bible/16_LINEAGES_KOVARI.md) §2 Sistema jugable |

## Por qué se conserva

- **Trazabilidad**: cada decisión consolidada puede remontarse a su
  propuesta original fechada.
- **Memoria de diseño**: contiene ideas, dudas y preguntas abiertas que
  pueden retomarse más adelante (por ejemplo, las "preguntas abiertas"
  del prólogo y de cada linaje).
- **Respaldo ante refutación**: si en una sesión futura el bible se
  revisa, esta carpeta permite reconstruir la propuesta previa sin
  pérdida.

## Reglas de uso

- **No editar** los archivos de esta carpeta.
- **No crear** agentes, skills ni rutas que apunten aquí. Las refs
  apuntan al bible.
- Si una sección de la fuente se cita en un PR o review, enlazar **al
  bible**, no al archivo.
- Si una sección se reactiva (porque se decidió distinto a lo que el
  bible consolidó), el movimiento se documenta en `CHANGELOG.md`.

## Adjuntos binarios

Esta carpeta también conserva los `.zip` originales entregados junto
con los paquetes:

- `ravatha_lore_package.zip` — copia exacta del paquete lore original.
- `RAVATHA_LINEAGE_SYSTEM_GUIDELINES.zip` — copia exacta del paquete
  de guidelines original.

Estos zips son redundantes con los directorios extraídos que viven
más arriba; se conservan únicamente para auditoría de entrega.
