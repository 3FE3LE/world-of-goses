#requires -Version 7.0
<#
.SYNOPSIS
    Instala skills curadas para desarrollar juegos con Godot 4 y C#/.NET.

.DESCRIPTION
    Usa el CLI universal de skills.sh (npx skills) para instalar:
      - Skills específicas de Godot.
      - Skills transversales útiles para gameplay, input, guardado y rendimiento.
      - Una skill local que obliga al agente a priorizar C# sobre GDScript.

    Diseñado para PowerShell 7 en Windows 11.

.EXAMPLE
    pwsh ./Install-GodotDotNetSkills.ps1

.EXAMPLE
    pwsh ./Install-GodotDotNetSkills.ps1 -Agent cursor -Preset Recommended

.EXAMPLE
    pwsh ./Install-GodotDotNetSkills.ps1 -Agent codex,cursor -Global

.EXAMPLE
    pwsh ./Install-GodotDotNetSkills.ps1 -Preset AllGodot -ListOnly
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet("Minimal", "Recommended", "AllGodot", "FullRepo")]
    [string] $Preset = "Recommended",

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string[]] $Agent = @("codex"),

    [Parameter()]
    [switch] $Global,

    [Parameter()]
    [switch] $ListOnly,

    [Parameter()]
    [switch] $DisableTelemetry,

    [Parameter()]
    [switch] $UseSymlinks,

    [Parameter()]
    [switch] $SkipDotNetPolicy
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$Repo = "gamedev-skills/awesome-gamedev-agent-skills"
$RouterSource = "https://github.com/gamedev-skills/awesome-gamedev-agent-skills/tree/main/router"

$GodotSkills = @(
    "godot-gdscript",
    "godot-nodes-scenes",
    "godot-signals-groups",
    "godot-2d-movement",
    "godot-tilemap",
    "godot-physics",
    "godot-ui-control",
    "godot-animation",
    "godot-shaders",
    "godot-3d-essentials",
    "godot-resources",
    "godot-audio",
    "godot-multiplayer",
    "godot-export",
    "godot-csharp"
)

$MinimalSkills = @(
    "godot-csharp",
    "godot-nodes-scenes",
    "godot-signals-groups",
    "godot-physics",
    "godot-resources",
    "godot-export"
)

$RecommendedSkills = @(
    # Motor y lenguaje
    "godot-csharp",
    "godot-gdscript", # Útil para traducir documentación; la policy local obliga a producir C#.
    "godot-nodes-scenes",
    "godot-signals-groups",
    "godot-resources",

    # Gameplay y presentación
    "godot-2d-movement",
    "godot-tilemap",
    "godot-physics",
    "godot-ui-control",
    "godot-animation",
    "godot-shaders",
    "godot-3d-essentials",
    "godot-audio",
    "godot-multiplayer",
    "godot-export",

    # Capacidades transversales
    "input-systems",
    "save-systems",
    "performance-optimization",
    "game-ai",
    "camera-systems",
    "game-ui-ux",
    "game-feel",
    "physics-tuning"
)

function Write-Section {
    param([Parameter(Mandatory)][string] $Message)

    Write-Host ""
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Resolve-Npx {
    $candidate = Get-Command "npx.cmd" -ErrorAction SilentlyContinue

    if (-not $candidate) {
        $candidate = Get-Command "npx" -ErrorAction SilentlyContinue
    }

    if (-not $candidate) {
        throw @"
No encontré npx.

Instala Node.js LTS y abre una terminal nueva:
  winget install OpenJS.NodeJS.LTS

Luego verifica:
  node --version
  npm --version
  npx --version
"@
    }

    return $candidate.Source
}

function Invoke-NpxSkills {
    param(
        [Parameter(Mandatory)][string[]] $Arguments
    )

    Write-Host ""
    Write-Host "npx $($Arguments -join ' ')" -ForegroundColor DarkGray

    & $script:NpxPath @Arguments

    if ($LASTEXITCODE -ne 0) {
        throw "El comando npx terminó con código $LASTEXITCODE."
    }
}

function Get-InstallArguments {
    param(
        [Parameter(Mandatory)][string] $Source,
        [string[]] $Skills = @()
    )

    $arguments = @("--yes", "skills", "add", $Source)

    foreach ($skill in $Skills) {
        $arguments += @("--skill", $skill)
    }

    foreach ($targetAgent in $Agent) {
        $arguments += @("--agent", $targetAgent)
    }

    if ($Global) {
        $arguments += "--global"
    }

    # En Windows, copiar es más predecible que crear symlinks sin Developer Mode.
    if (-not $UseSymlinks) {
        $arguments += "--copy"
    }

    $arguments += "--yes"

    return $arguments
}

function Test-ProjectContext {
    if ($Global) {
        return
    }

    $projectFile = Join-Path (Get-Location) "project.godot"

    if (-not (Test-Path $projectFile)) {
        Write-Warning @"
No veo un project.godot en:
  $(Get-Location)

La instalación será local a esta carpeta. Lo normal es ejecutar el script desde
la raíz del proyecto Godot, o usar -Global.
"@
    }
}

function Test-DotNetEnvironment {
    Write-Section "Comprobando .NET y el proyecto"

    $dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue

    if (-not $dotnet) {
        Write-Warning @"
No encontré dotnet en PATH.
Para Godot moderno con C#, instala un SDK vigente, preferiblemente .NET 8 x64
(.NET 9 si vas a exportar a Android con versiones actuales de Godot).
"@
        return
    }

    $sdks = @(& dotnet --list-sdks 2>$null)

    if ($LASTEXITCODE -ne 0) {
        Write-Warning "No pude consultar los SDK de .NET instalados."
        return
    }

    if ($sdks.Count -eq 0) {
        Write-Warning "dotnet existe, pero no devolvió SDK instalados."
    }
    else {
        Write-Host "SDK instalados:" -ForegroundColor Green
        $sdks | ForEach-Object { Write-Host "  $_" }

        $hasModernSdk = $sdks | Where-Object { $_ -match '^(8|9|1[0-9])\.' }

        if (-not $hasModernSdk) {
            Write-Warning @"
No detecté .NET 8 o superior. .NET 7 ya no es una base adecuada para una
instalación moderna de Godot 4 con C#.
"@
        }
    }

    $project = Get-ChildItem -Path (Get-Location) -Filter "*.csproj" -File |
        Select-Object -First 1

    if (-not $project) {
        Write-Host "No encontré .csproj; puede generarse al crear el primer script C#." `
            -ForegroundColor DarkGray
        return
    }

    $content = Get-Content -Path $project.FullName -Raw

    if ($content -match '<TargetFramework>\s*(?<tfm>[^<]+)\s*</TargetFramework>') {
        $targetFramework = $Matches.tfm.Trim()
        Write-Host "TargetFramework detectado: $targetFramework" -ForegroundColor Green

        if ($targetFramework -eq "net7.0") {
            Write-Warning @"
Tu proyecto apunta a net7.0. Conviene migrarlo a net8.0 si tu versión de Godot
lo soporta. Hazlo en una rama y confirma que la versión de Godot.NET.Sdk sea
compatible antes de tocar producción.
"@
        }
    }
}

function Install-DotNetProjectPolicy {
    if ($SkipDotNetPolicy) {
        return
    }

    Write-Section "Instalando la policy local Godot C#/.NET"

    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
        "godot-dotnet-skills-" + [guid]::NewGuid().ToString("N")
    )
    $skillDirectory = Join-Path $tempRoot "godot-dotnet-project"
    $skillFile = Join-Path $skillDirectory "SKILL.md"

    New-Item -Path $skillDirectory -ItemType Directory -Force | Out-Null

    $policy = @'
---
name: godot-dotnet-project
description: Enforce C#/.NET-first implementation rules for this Godot 4 project. Use whenever creating, changing, reviewing, debugging, testing, or documenting gameplay code, nodes, scenes, resources, signals, editor tooling, builds, or exports.
---

# Godot 4 C#/.NET project policy

## Language

- Produce runtime and editor scripts in C# unless the user explicitly requests GDScript.
- Treat GDScript examples from documentation or other skills as conceptual references and translate them into idiomatic Godot C#.
- Never add a `.gd` implementation merely because an upstream example uses GDScript.
- Use the Godot editor build with .NET support.

## C# conventions

- Node scripts must be `partial` classes inheriting the appropriate Godot type.
- Match the C# file name and class name.
- Use PascalCase lifecycle overrides such as `_Ready`, `_Process`, and `_PhysicsProcess`.
- Prefer typed node references, `[Export]` properties or fields, C# events/signals, and `StringName` where repeated engine lookups matter.
- Avoid `GetNode` calls every frame; cache dependencies during initialization.
- Use nullable reference types deliberately and validate required scene dependencies early.
- Prefer composition, small nodes, resources for data, and explicit signals over giant inheritance trees or global singleton dumping grounds.

## Version policy

- Do not target `net7.0` for a modern Godot project.
- Prefer `net8.0` for current desktop Godot 4 .NET projects unless the installed Godot version requires something newer.
- Verify `Godot.NET.Sdk`, TargetFramework, and export platform requirements before changing the project file.
- Do not silently upgrade Godot, the SDK, NuGet packages, or the target framework.

## Verification

After meaningful code changes:

1. Run `dotnet build`.
2. Fix compiler warnings introduced by the change.
3. Run available automated tests.
4. When Godot is available on PATH, run an appropriate headless/import/project check.
5. Report anything that could not be executed instead of claiming success.

## Scene and resource safety

- Preserve node paths, owner relationships, signal connections, exported names, and resource UIDs.
- Avoid broad textual rewrites of `.tscn`, `.tres`, or `.res` files.
- Prefer focused changes and validate the project after modifying serialized Godot files.
'@

    Set-Content -Path $skillFile -Value $policy -Encoding utf8NoBOM

    try {
        $arguments = Get-InstallArguments `
            -Source $tempRoot `
            -Skills @("godot-dotnet-project")

        Invoke-NpxSkills -Arguments $arguments
    }
    finally {
        Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($DisableTelemetry) {
    $env:DISABLE_TELEMETRY = "1"
}

Write-Section "Preparando instalación"
Write-Host "Preset: $Preset"
Write-Host "Agentes: $($Agent -join ', ')"
Write-Host "Ámbito: $(if ($Global) { 'Global' } else { 'Proyecto actual' })"
Write-Host "Método: $(if ($UseSymlinks) { 'Symlinks' } else { 'Copias (recomendado en Windows)' })"

$script:NpxPath = Resolve-Npx
Test-ProjectContext
Test-DotNetEnvironment

if ($ListOnly) {
    Write-Section "Skills disponibles en el repositorio"
    Invoke-NpxSkills -Arguments @(
        "--yes",
        "skills",
        "add",
        $Repo,
        "--list"
    )

    Write-Host ""
    Write-Host "No se instaló nada porque usaste -ListOnly." -ForegroundColor Yellow
    exit 0
}

switch ($Preset) {
    "Minimal" {
        $selectedSkills = $MinimalSkills
    }

    "Recommended" {
        $selectedSkills = $RecommendedSkills
    }

    "AllGodot" {
        $selectedSkills = $GodotSkills
    }

    "FullRepo" {
        # Instala todas las skills del repo, pero solo en los agentes indicados.
        $selectedSkills = @("*")
    }

    default {
        throw "Preset no soportado: $Preset"
    }
}

Write-Section "Instalando el router de game development"
$routerArguments = Get-InstallArguments -Source $RouterSource
Invoke-NpxSkills -Arguments $routerArguments

Write-Section "Instalando skills del repositorio"
Write-Host "Repositorio: $Repo"
Write-Host "Skills solicitadas: $($selectedSkills.Count)"

$installArguments = Get-InstallArguments `
    -Source $Repo `
    -Skills $selectedSkills

Invoke-NpxSkills -Arguments $installArguments
Install-DotNetProjectPolicy

Write-Section "Verificación"
Invoke-NpxSkills -Arguments @("--yes", "skills", "list")

Write-Host ""
Write-Host "Instalación terminada." -ForegroundColor Green
Write-Host "Para actualizar después:" -ForegroundColor Cyan
Write-Host "  npx --yes skills update -y"
