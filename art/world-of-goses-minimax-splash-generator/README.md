# World of Goses: MiniMax splash generator

Genera los 16 splash arts usando los sprites individuales ya integrados en el
proyecto de Godot.

## Solo necesitas

- Python 3.11 o superior.
- PowerShell.
- Tu `MINIMAX_API_KEY`.

`run.ps1` crea el entorno virtual e instala las dependencias automáticamente.

## Instalación

En este repositorio la carpeta ya vive dentro de `/art`. La raíz del proyecto
Godot es `game/`:

```text
<proyecto>/
├── art/
│   └── world-of-goses-minimax-splash-generator/
└── game/
    ├── project.godot
    └── assets/characters/lineages/...
```

## Validar sin gastar

```powershell
cd .\art\world-of-goses-minimax-splash-generator
.\run.ps1 -DryRun -All -ProjectRoot ..\..\game
```

Debe encontrar las 16 referencias y no hace llamadas a MiniMax.

## Probar un personaje

```powershell
.\run.ps1 -ProjectRoot ..\..\game
```

La primera ejecución crea `.venv`, instala `requests` y `Pillow`, solicita la
API key de forma oculta y genera únicamente `ardhen_male`.

También puedes definir la clave para la terminal actual:

```powershell
$env:MINIMAX_API_KEY = "tu-api-key"
.\run.ps1
```

## Generar los 16

```powershell
.\run.ps1 -All -ProjectRoot ..\..\game
```

Solicita escribir `GENERATE 16` antes de realizar las llamadas pagadas.

```powershell
.\run.ps1 -All -Yes -ProjectRoot ..\..\game
```

omite esa confirmación.

## Generar uno concreto

```powershell
.\run.ps1 -Only eirune_female -ProjectRoot ..\..\game
.\run.ps1 -Only kovari_male -ProjectRoot ..\..\game
.\run.ps1 -Only theryn_female -ProjectRoot ..\..\game
```

## Resultados

```text
game/art/generated/standardized_lineage_characters/
├── ardhen/
│   ├── male.png
│   └── female.png
├── ...
├── theryn/
│   ├── male.png
│   └── female.png
└── manifest.json
```

Los PNG existentes se omiten. Para regenerar uno:

```powershell
.\run.ps1 -Only ardhen_male -Force -ProjectRoot ..\..\game
```

## Qué hace con las referencias

Para cada personaje toma:

```text
game/assets/characters/lineages/<linaje>/<gender>/textures/idle_down_128.png
```

Extrae el primer frame de 128 × 128, recorta la transparencia, lo amplía con
nearest-neighbor y lo coloca sobre un fondo neutro de su paleta. Después lo
convierte a una Data URL Base64 para enviarlo dentro de la petición.

No necesitas R2, Supabase, hosting ni URLs públicas.

## prompts.json

Incluye:

- prompt base compartido;
- las 16 poses y entornos;
- paleta por linaje;
- ruta de cada referencia;
- seed reproducible;
- resolución 1024 × 1280;
- `prompt_optimizer: false`.

El script valida el máximo de 1.500 caracteres antes de llamar a MiniMax.

## Proyecto Godot en otra ruta

Pasa siempre la carpeta que contiene `project.godot`. Para este repositorio es
`C:\dev\world-of-goses\game`, no la raíz del repositorio.

## Control de versiones

No agregues a Git `.venv/`, `.env`, `game/art/generated/`, archivos temporales
ni `*.import`. Los resultados son material conceptual local hasta que sean
revisados y promovidos de forma intencional por el pipeline de arte.

## Seguridad

La API key no se guarda. No la agregues al JSON, al código ni a Git.
