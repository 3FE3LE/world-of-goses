param(
    [string]$Repo = "3FE3LE/world-of-goses"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw "GitHub CLI (gh) no está instalado."
}

gh auth status 2>$null | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "gh no está autenticado. Ejecuta: gh auth login"
}

function Repair-WogIssue {
    param(
        [Parameter(Mandatory=$true)][int]$Number,
        [Parameter(Mandatory=$true)][string]$Title,
        [Parameter(Mandatory=$true)][string]$Body
    )

    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        [System.IO.File]::WriteAllText(
            $tmp,
            $Body,
            [System.Text.UTF8Encoding]::new($false)
        )

        gh issue edit $Number --repo $Repo --title $Title --body-file $tmp | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw "No se pudo reparar #$Number"
        }

        Write-Host "REPAIRED #$Number  $Title" -ForegroundColor Green
    }
    finally {
        Remove-Item $tmp -Force -ErrorAction SilentlyContinue
    }
}


$title = @'
[EG-5C] Consolidar agricultura, parcelas 2–3 y segundo ciclo jugable
'@
$body = @'
## Contexto
La apertura ya tiene onboarding, primera noche, recursos iniciales, Founding Site, Cultivation Site, resource expeditions y Spirit Trail. El backlog documental todavía conserva EG-5C como el siguiente incremento real: consolidar el asentamiento después del primer regreso.

## Objetivo
Cerrar el segundo ciclo jugable sin reset y demostrar que la apertura deja de ser una secuencia lineal de tutorial para convertirse en un loop urbano repetible.

## Alcance
- Segundo y tercer plot/parcela dentro del contrato territorial vigente.
- Forestry gate ya existente, capacidad y herramienta cuando corresponda.
- Consolidación de Farm/Cultivation y horizonte de Food.
- Fabricación/uso de la herramienta necesaria desde el source of truth actual.
- Segundo ciclo completo sin reset/debug.
- Save/load y avance offline equivalentes.
- Firma visual de recursos, placement y estados relevantes que todavía no estén cubiertos por fixtures actuales.

## Criterios de aceptación
- [ ] Un slot limpio puede completar el flujo post-Spirit-Trail y llegar al segundo ciclo sin reset ni comandos de debug.
- [ ] El jugador puede habilitar y usar las parcelas adicionales previstas por el diseño vigente.
- [ ] La consolidación agrícola produce una decisión material significativa sobre Food/capacidad/producción.
- [ ] Las reglas live y offline producen resultados equivalentes en los límites de fase relevantes.
- [ ] Save/load conserva el progreso sin duplicar oportunidades, recursos ni parcelas.
- [ ] Las interacciones visuales críticas están cubiertas por fixtures o firma humana reproducible.
- [ ] Build, suite completa y headless boot quedan verdes.

## No hacer
- No introducir todavía profundidad amplia de combate.
- No recuperar Parcel 9 ni la frontera territorial histórica.
- No crear un segundo sistema de producción o inventario paralelo.
'@
Repair-WogIssue -Number 32 -Title $title.Trim() -Body $body.Trim()


$title = @'
[EG-6] Calibrar y firmar la apertura completa
'@
$body = @'
## Contexto
Tras EG-5C, la apertura necesita una pasada final de calibración y evidencia end-to-end. La antigua deuda M-14 mezclaba harness, firmas humanas y aceptación del vertical slice; este issue concentra únicamente la verificación que siga siendo relevante sobre el producto actual.

## Objetivo
Firmar la apertura completa como un flujo reproducible, entendible y jugable desde un slot limpio.

## Alcance
- Playthrough fresco desde onboarding hasta completar el segundo ciclo.
- Tiempos de espera, ritmo y economía inicial.
- Recolección, construcción, Spirit Trail, combate, regreso y consolidación.
- Relanzamientos/save-load en puntos críticos.
- Equivalencia live/offline.
- UI normal, navegación real, input real y geometría visible.
- Registro de cualquier blocker como issue separado cuando no pertenezca a calibración.

## Criterios de aceptación
- [ ] Existe un recorrido documentado y reproducible desde slot vacío hasta segundo ciclo.
- [ ] Ningún paso obligatorio depende de reload, debug, fixture-only state o conocimiento invisible.
- [ ] Las esperas iniciales están calibradas y justificadas por decisiones reales.
- [ ] Save/load y offline no cambian el resultado lógico del opening.
- [ ] Los fixtures críticos usan condiciones observables y verifican geometría/estado real.
- [ ] La matriz visual necesaria para la apertura tiene firma reproducible.
- [ ] Build, suite completa, validators y headless boot quedan verdes.

## No hacer
- No convertir EG-6 en un roadmap de sistemas futuros.
- No añadir features nuevas para “hacer más completa” la apertura.
'@
Repair-WogIssue -Number 33 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Audio] Integrar arquitectura de reproducción y primer paquete sonoro
'@
$body = @'
## Contexto
La dirección de audio ya está definida a nivel estético y de pipeline, pero el juego necesita una primera integración runtime real en Godot.

## Objetivo
Conectar una arquitectura de audio mínima, mantenible y compatible con la identidad retro del proyecto.

## Alcance
- Definir/configurar buses necesarios para el slice actual.
- Conectar reproducción de UI, ciudad y expedición mediante AudioStreamPlayer/AudioStreamPlayer2D cuando corresponda.
- Integrar el primer paquete de SFX/música/ambiente que ya tenga assets disponibles.
- Volúmenes/cooldowns/variación donde sean necesarios.
- Evitar que Presentation conozca rutas o reglas dispersas sin una costura central razonable.
- Mantener licencias/origen de assets documentados.

## Criterios de aceptación
- [ ] Existe un árbol de buses runtime coherente con las necesidades actuales.
- [ ] UI, ciudad y expedición pueden disparar audio por una costura estable.
- [ ] Al menos un tema/ambiente y el paquete inicial de SFX elegido se reproducen en juego.
- [ ] Cambiar de ciudad a expedición no crea players/loops duplicados.
- [ ] Los eventos frecuentes tienen protección contra spam cuando aplique.
- [ ] Headless/tests no dependen de dispositivo de audio.
- [ ] La guideline de audio deja de funcionar como checklist de implementación.

## No hacer
- No construir un middleware de audio genérico.
- No implementar todavía voice acting ni audio adaptativo profundo sin consumidor.
'@
Repair-WogIssue -Number 34 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Territory] Diseñar expansión territorial sobre el macro actual
'@
$body = @'
## Contexto
La implementación histórica de Parcel 9 y la frontera desbloqueable quedó superada. La necesidad de expansión territorial sigue siendo válida, pero debe encajar con el Macro actual, su gramática espacial y la escala de ciudad vigente.

## Objetivo
Cerrar el contrato de producto y arquitectura para expandir el terrario/territorio sin recuperar la solución descartada.

## Preguntas que debe resolver
- ¿Qué constituye el borde actual del territorio?
- ¿Cómo se adquiere/desbloquea nueva superficie?
- ¿Qué coste, decisión o consecuencia lo habilita?
- ¿Cómo se representa en el Macro sin segunda autoridad espacial?
- ¿Qué información persiste y qué es sólo presentación?
- ¿Cómo interactúa con parcelas, recursos, rutas y futuras regiones?

## Criterios de aceptación
- [ ] Existe una decisión explícita de dominio y presentación para expansión territorial.
- [ ] Parcel 9 no reaparece como solución por defecto.
- [ ] La solución conserva la gramática espacial actual del Macro.
- [ ] Se identifica el mínimo cambio de persistencia necesario, si existe.
- [ ] La implementación futura puede dividirse en issues concretos sin volver a interpretar documentación antigua.

## No hacer
- No implementar antes de cerrar este contrato.
- No introducir mundo infinito persistente.
'@
Repair-WogIssue -Number 35 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Performance] Revalidar culling y batching del Macro antes de ampliar territorio
'@
$body = @'
## Contexto
Documentación histórica registró capturas costosas del Macro y sugirió culling/batching/MultiMesh. La regla vigente del proyecto es no optimizar sin medir.

## Objetivo
Reperfilar el Macro actual con fixtures representativos y decidir si existe un cuello de botella que justifique trabajo antes de ampliar territorio.

## Alcance
- Medir frame time, draw/load cost y número de entidades/nodos con fixtures reproducibles.
- Comparar viewport/zoom y tamaños de ciudad relevantes.
- Identificar si el cuello está en rendering, creación de nodos, hit rects, navegación u otra capa.
- Sólo si existe evidencia, implementar la optimización mínima necesaria.

## Criterios de aceptación
- [ ] Hay mediciones reproducibles sobre HEAD actual.
- [ ] Se establece un threshold concreto que bloquea o permite expansión territorial.
- [ ] Si no hay problema material, el issue puede cerrarse sin cambios runtime.
- [ ] Si hay problema, la solución se basa en el cuello medido.
- [ ] MultiMesh/batching no se introduce por anticipación.

## No hacer
- No portar todo el renderer a una nueva tecnología sin perfil.
'@
Repair-WogIssue -Number 36 -Title $title.Trim() -Body $body.Trim()


$title = @'
[RPG] Cerrar curva de competencias y coste de progresión 0–20
'@
$body = @'
## Contexto
El sistema canónico define competencias 0–20 y niveles de familiaridad Natural/Familiar/Extranjera, pero la curva y el coste total siguen siendo provisionales.

## Objetivo
Fijar una progresión de competencias utilizable por combate, herramientas y futuras profesiones sin convertir familiaridad en penalización directa de poder.

## Alcance
- Curva XP 0–20.
- Coste acumulado y por nivel.
- Eficiencias 100% / 50% / 10% o su calibración final.
- Contrato de adquisición de XP por uso/evento.
- Fórmula central configurable y auditable.
- Casos de referencia para natural, familiar y extranjera.

## Criterios de aceptación
- [ ] Existe una tabla/fórmula definitiva inicial para 0–20.
- [ ] Natural/Familiar/Extranjera afecta adquisición de XP, no daño ni stats directos.
- [ ] La curva vive en configuración/catálogo central, no duplicada.
- [ ] Tests cubren niveles inicial, medio y máximo y los tres tiers de familiaridad.
- [ ] La documentación matemática describe el contrato vigente sin sección de backlog.

## No hacer
- No diseñar todavía árboles de traits completos.
'@
Repair-WogIssue -Number 37 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Combat] Completar contrato de técnicas, crítico, penetración y resistencias
'@
$body = @'
## Contexto
Las fórmulas actuales ya definen canales físico/elemental, defensa y mitigación, pero todavía faltan contratos que impiden cerrar un modelo de combate más amplio.

## Objetivo
Fijar la primera versión coherente de técnicas y resolución ofensiva/defensiva más allá del Basic Attack tutorial.

## Alcance
- TechniquePhysicalCoefficient y TechniqueElementalCoefficient.
- Reglas y multiplicador de crítico.
- Penetración física y elemental.
- Resistencia a estados físicos/elementales cuando corresponda.
- Orden exacto de aplicación de modificadores.
- Breakdown auditable para UI/tests.

## Criterios de aceptación
- [ ] Una técnica puede declarar y resolver sus coeficientes sin números mágicos en Presentation.
- [ ] Crítico tiene trigger y multiplicador explícitos.
- [ ] Penetración interactúa con mitigación mediante una regla única y testeada.
- [ ] Las resistencias a estados tienen contrato separado del daño si semánticamente corresponde.
- [ ] El resultado puede explicarse paso a paso en tests/read models.
- [ ] No se contradice la autoridad de DerivedStatistics actual.

## No hacer
- No añadir docenas de técnicas de contenido para probar la arquitectura.
'@
Repair-WogIssue -Number 38 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Equipment] Extender PersonalEquipment a armadura, masa, integridad y desgaste
'@
$body = @'
## Contexto
#26 introdujo el primer item real, identidad persistente y una única autoridad de equipment para el arma del Founder. El modelo estadístico ya contempla Helmet/Chest/Legs/Boots/Gloves y menciona masa, integridad y desgaste como futuras propiedades del objeto.

## Objetivo
Extender la costura item-backed existente sin crear una segunda autoridad de loadout.

## Alcance
- Slots de armadura/equipo personal necesarios.
- Item instances persistentes.
- GearSupport por caras del Cubo.
- Masa, integridad y desgaste con contrato explícito.
- Proyección efectiva hacia EquipmentLoadout/DerivedStatistics.
- Equipar/desequipar mediante CitizenEquipmentService o su autoridad vigente.
- Migración de save si cambia schema.

## Criterios de aceptación
- [ ] Todos los slots usan la misma autoridad item-backed.
- [ ] GearSupport se deriva del equipo equipado y no modifica CubeProfile.
- [ ] Masa/integridad/desgaste tienen semántica única y persistencia cuando corresponda.
- [ ] Save/load conserva identidad y estado de los items.
- [ ] Combat/Statistics consumen una proyección, no inventario/UI directamente.
- [ ] No aparecen propiedades paralelas tipo EquippedHelmetFamily en Citizen.

## No hacer
- No inventory UI completa, vendors, rarity, sockets o loot tables salvo consumidor real.
'@
Repair-WogIssue -Number 39 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Health] Derivar ConditionFactor desde la condición real del Citizen
'@
$body = @'
## Contexto
`ConditionFactor` ya participa en las fórmulas derivadas, pero su valor todavía no se deriva de las fuentes de salud/estado del Citizen.

## Objetivo
Convertir ConditionFactor en una proyección determinista del estado real y eliminar su condición de valor arbitrario.

## Alcance
- Heridas.
- Enfermedad cuando exista como estado real.
- Fatiga/stamina.
- Hambre/nutrición cuando el sistema vigente lo permita.
- Reglas de combinación y límites.
- Breakdown explicable.
- Integración con DerivedStatistics.

## Criterios de aceptación
- [ ] El mismo estado del Citizen produce siempre el mismo ConditionFactor.
- [ ] No se persiste ConditionFactor como segunda autoridad.
- [ ] Las fuentes inexistentes todavía no se inventan sólo para completar la fórmula.
- [ ] Cambios de herida/fatiga relevantes invalidan o recalculan la proyección correctamente.
- [ ] Tests cubren saludable, afectación moderada y condición grave.

## No hacer
- No crear un sistema médico completo como side effect.
'@
Repair-WogIssue -Number 40 -Title $title.Trim() -Body $body.Trim()


$title = @'
[City/RPG] Derivar CitySupportFactor desde infraestructura y servicios
'@
$body = @'
## Contexto
`CitySupportFactor` ya forma parte de las fórmulas derivadas, pero todavía es un parámetro conceptual y no una lectura del estado urbano real.

## Objetivo
Derivar el apoyo de ciudad desde sistemas que realmente existan y puedan explicar una consecuencia sobre el Citizen.

## Alcance
- Vivienda/capacidad disponible.
- Alimentación/nutrición cuando exista una métrica real.
- Salud/recuperación si hay infraestructura consumible.
- Políticas/servicios sólo cuando estén implementados.
- Regla de combinación, límites y breakdown.

## Criterios de aceptación
- [ ] CitySupportFactor se calcula desde snapshots/estado real.
- [ ] No se persiste como valor independiente.
- [ ] Factores todavía inexistentes no se simulan con placeholders permanentes.
- [ ] UI/tests pueden explicar qué fuentes aportaron o penalizaron el resultado.
- [ ] El cálculo no requiere recorrer todos los Citizens cada frame.

## No hacer
- No implementar edificios/políticas ficticios sólo para alimentar el factor.
'@
Repair-WogIssue -Number 41 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Elements] Integrar afinidades con materiales, ambiente, ciudad y expediciones
'@
$body = @'
## Contexto
PrimaryAffinity ya es identidad canónica del Citizen. El guideline elemental describe una integración mucho más amplia, pero esa expansión nunca debe vivir como roadmap dentro de documentación canónica.

## Objetivo
Convertir la afinidad de identidad persistente a sistema consumidor real, de forma incremental y explicable.

## Primer alcance recomendado
- Un contrato de interacción entre afinidad, técnica y material/entorno.
- ElementalResonance universal del equipo como canal vigente.
- Carga/tolerancia/condición sólo si existe consumidor concreto.
- Al menos una interacción de expedición o ciudad que dependa de afinidad y tenga consecuencia material observable.
- Breakdown para explicar por qué ocurrió.

## Futuras divisiones
Este issue puede actuar como epic y dividirse en material response, regional conditions, producción, salud o instituciones cuando cada uno tenga alcance implementable.

## Criterios de aceptación
- [ ] PrimaryAffinity deja de ser sólo dato de identidad en al menos un flujo jugable real.
- [ ] La interacción depende de contexto/material/técnica, no de una rueda elemental rígida.
- [ ] `ElementalResonance` se mantiene universal mientras el canon estadístico vigente no cambie.
- [ ] No aparecen `EarthResonance`, `FireResonance`, etc. como segunda definición accidental.
- [ ] Consecuencias y costes son auditables.

## No hacer
- No implementar las antiguas Phase 0–6 como una megafeature.
'@
Repair-WogIssue -Number 42 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Combat] Definir escalado de bestiario y dificultad expedicionaria
'@
$body = @'
## Contexto
El primer encuentro está deliberadamente acotado y protegido por tuning de onboarding. Falta un contrato para que enemigos y expediciones escalen cuando el juego salga de esa protección.

## Objetivo
Definir una función de dificultad legible y controlable que escale bestiario/encuentros sin depender sólo de HP inflado.

## Alcance
- Inputs legítimos de dificultad: etapa/territorio/objetivo/composición u otros que ya existan.
- Curvas de stats y/o composición de enemigos.
- Límites y tiers.
- Relación con recompensa/riesgo.
- Casos de referencia early/mid/future.
- Separar tutorial protection de dificultad ordinaria.

## Criterios de aceptación
- [ ] Una expedición puede resolver un tier/difficulty determinista desde datos explícitos.
- [ ] El opening tutorial no se usa como modelo de dificultad general.
- [ ] El escalado puede explicarse y probarse.
- [ ] No existe escalado oculto dependiente de UI o tiempo real de sesión.
- [ ] Se identifican seams para contenido de bestiario sin acoplarlo a Presentation.
'@
Repair-WogIssue -Number 43 -Title $title.Trim() -Body $body.Trim()


$title = @'
[NPC] Conectar DialogueRunner a un primer diálogo ramificado persistente
'@
$body = @'
## Contexto
`DialogueRunner` ya tiene seam y pruebas, pero no existe todavía un consumidor jugable real.

## Objetivo
Probar el sistema de diálogo con un NPC real y una decisión que sobreviva save/load.

## Alcance
- Un NPC conversable en un flujo jugable.
- Al menos una bifurcación con consecuencia persistente.
- EN/ES.
- Mouse, teclado y gamepad.
- Reentrada/reanudación coherente.
- Snapshot/read model suficiente para UI.

## Criterios de aceptación
- [ ] El jugador puede iniciar y completar el diálogo desde gameplay normal.
- [ ] Una elección cambia un estado persistente observable.
- [ ] Save/load conserva la decisión y no reabre ramas imposibles.
- [ ] Localización y navegación de input están cubiertas.
- [ ] DialogueRunner se mantiene como seam reutilizable sin convertirse en scripting engine general.

## No hacer
- No construir un editor de diálogo completo.
'@
Repair-WogIssue -Number 44 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Citizens] Diseñar envejecimiento, conocimiento y generaciones
'@
$body = @'
## Contexto
El diseño contempla tiempo irreversible, envejecimiento y transmisión de conocimiento, pero todavía no existe un contrato suficientemente cerrado para implementarlo.

## Objetivo
Definir qué significa envejecer en World of Goses y qué información/ventajas/desventajas se transmiten entre generaciones.

## Preguntas
- ¿Qué edades/etapas existen y qué cambia realmente?
- ¿Cómo interactúa con salud, competencias y profesiones?
- ¿Qué conocimiento se conserva, enseña o pierde?
- ¿Cómo entra una nueva generación a la población?
- ¿Qué se persiste por Citizen y qué se deriva del reloj?
- ¿Cómo evitar simular individualmente a miles de personas por frame?

## Criterios de aceptación
- [ ] Existe un contrato de producto con consecuencias jugables.
- [ ] Se identifican autoridades y datos mínimos persistentes.
- [ ] No se confunde envejecimiento con un simple debuff lineal.
- [ ] El resultado puede dividirse en issues implementables posteriores.
'@
Repair-WogIssue -Number 45 -Title $title.Trim() -Body $body.Trim()


$title = @'
[World] Cerrar cosmología común de Ravatha
'@
$body = @'
## Contexto
La cosmología común sigue apareciendo como pregunta abierta y condiciona cómo se explican RaVAtha, la conciencia astral, los linajes, afinidades y materialización.

## Objetivo
Cerrar únicamente el marco cosmológico compartido necesario para que onboarding, linajes y mundo no se contradigan.

## Debe resolver
- Qué es RaVAtha en términos narrativos canónicos.
- Qué significa la conciencia astral y su llegada.
- Qué relación existe entre cuerpo, linaje y reconstrucción.
- Qué puede conocer un personaje dentro del mundo y qué queda como misterio.
- Qué afirmaciones NO son canónicas.

## Criterios de aceptación
- [ ] Existe una versión breve y utilizable por narrativa y world docs.
- [ ] No se convierte en una enciclopedia de lore innecesaria.
- [ ] Onboarding y linajes pueden enlazarla sin repetirla.
- [ ] Las contradicciones previas se eliminan de documentación canónica.
'@
Repair-WogIssue -Number 46 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Environment] Definir nombre y contrato del eje regeneración/extracción
'@
$body = @'
## Contexto
El proyecto conserva un eje ambiental inspirado históricamente en Wakfu/Estasis, pero la documentación evita correctamente convertirlo en moralidad binaria y todavía no ha fijado nombre/contrato final.

## Objetivo
Definir un eje sistémico propio que mida consecuencias regenerativas/extractivas sin etiquetar acciones como “buenas” o “malas”.

## Debe resolver
- Nombre canónico.
- Qué eventos lo modifican.
- Escala: Citizen, parcela, territorio, ciudad o combinación.
- Persistencia y agregación.
- Qué consecuencias jugables habilita.
- Cómo evitar que sea un karma meter.

## Criterios de aceptación
- [ ] El eje tiene semántica material/ecológica explícita.
- [ ] No es moralidad binaria.
- [ ] Los inputs son eventos observables del mundo.
- [ ] Se define qué nivel del modelo es autoridad.
- [ ] Puede dividirse en implementación futura sin rescatar propuestas históricas.
'@
Repair-WogIssue -Number 47 -Title $title.Trim() -Body $body.Trim()


$title = @'
[World] Diseñar primer bioma autoral y primer conflicto sistémico
'@
$body = @'
## Contexto
“Primer bioma” y “primer conflicto sistémico” aparecen como preguntas separadas, pero ambos deben validar juntos qué hace distinta una región de World of Goses más allá del dressing visual.

## Objetivo
Diseñar un primer bioma que combine identidad visual, recursos, riesgos, oportunidades y una tensión sistémica jugable.

## Debe incluir
- Terreno/vegetación/atmósfera.
- Recursos y materiales relevantes.
- Riesgos/condiciones del entorno.
- Qué decisiones de ciudad o expedición provoca.
- Cómo puede interactuar con afinidades sin exigir un sistema elemental completo.
- Un conflicto sistémico concreto y repetible, no una misión guionizada aislada.

## Criterios de aceptación
- [ ] El bioma tiene identidad mecánica además de visual.
- [ ] El conflicto obliga a una decisión con trade-offs.
- [ ] Se identifican datos y sistemas consumidores reales.
- [ ] El resultado puede convertirse en issues de implementación separados.
'@
Repair-WogIssue -Number 48 -Title $title.Trim() -Body $body.Trim()


$title = @'
[City Design] Definir el siguiente estrato social: migración, cultura, política, economía y capacidad
'@
$body = @'
## Contexto
Migración, mezcla cultural, política, economía y capacidad poblacional aparecen hoy como preguntas abiertas independientes, pero implementarlas por separado ahora produciría arquitectura especulativa.

## Objetivo
Definir cuál de estos sistemas debe ser el siguiente estrato social real de la ciudad y qué problema jugable resuelve.

## Alcance de discovery
- Identificar el primer consumidor real.
- Decidir qué concepto debe aterrizar primero.
- Definir relaciones mínimas entre población, capacidad, producción y decisiones colectivas.
- Establecer qué se pospone explícitamente.
- Evitar cinco subsistemas vacíos sin loop que los use.

## Criterios de aceptación
- [ ] Se elige un primer sistema social con motivo jugable concreto.
- [ ] Se definen sus inputs, outputs y autoridad.
- [ ] El resto queda explícitamente fuera de alcance hasta tener consumidor.
- [ ] Se generan issues de implementación únicamente para el slice elegido.
'@
Repair-WogIssue -Number 49 -Title $title.Trim() -Body $body.Trim()


$title = @'
[Performance] Escalar representación visual de citizens sólo cuando el profiler lo exija
'@
$body = @'
## Contexto
La implementación actual de `CitizenSpriteBank`/carriers es suficiente para la escala visible presente. La documentación histórica proponía MultiMesh/batching al superar cierto volumen, pero la optimización debe depender de evidencia.

## Trigger
Más de ~20–25 Citizens visibles o fixtures de 25/50 entidades que demuestren un cuello material.

## Objetivo
Medir primero y cambiar la estrategia de representación sólo si el coste visual actual deja de ser aceptable.

## Alcance
- Fixtures reproducibles de 25 y 50 entidades visibles.
- Frame time y coste de creación/actualización.
- Identificación del cuello real.
- Si aplica, batching/pooling/MultiMesh u otra solución mínima compatible con sprites/pixel art.

## Criterios de aceptación
- [ ] El issue puede cerrarse sin implementación si el profiler no demuestra problema.
- [ ] Si se optimiza, existe A/B medible antes/después.
- [ ] La solución no cambia el modelo persistente de Citizen.
- [ ] No se sacrifica selección/hit-testing/legibilidad sin una alternativa explícita.
'@
Repair-WogIssue -Number 50 -Title $title.Trim() -Body $body.Trim()



Write-Host ""
Write-Host "Verificando títulos..." -ForegroundColor Cyan
gh issue list --repo $Repo --state open --limit 100 --json number,title | Out-String | Write-Host
Write-Host "Listo: #32-#50 reparadas sin cambiar estado ni labels." -ForegroundColor Green
