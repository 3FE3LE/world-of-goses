#requires -Version 7.0
<#
.SYNOPSIS
    Installs skills curated for developing Godot 4 games with C#/.NET.

.DESCRIPTION
    Three strict presets:

        Core          - the default. The minimum viable set: Godot 4 C#
                        integration, scenes / signals / resources, .NET
                        build and test, and the local repo-navigation
                        adapter. No 3D, no multiplayer, no GDScript
                        authoring, no game AI.
        CurrentSlice  - Core plus the disciplines the active slice
                        (EG-5 consolidation) actually uses: UI, assets,
                        audio, persistence, debugging, performance,
                        testing, export.
        Full          - everything verified. Includes 3D, multiplayer,
                        GDScript reference, and game AI. NOT a default;
                        for debugging and exploration only.

    Each install goes through a fetch-and-stamp workflow: the script
    only ships a SKILL.md into the agent's directory after a real
    network call returned a SKILL.md and the SHA-256 of the downloaded
    file was recorded in skills-lock.json. The lock file is the
    recovery key.

    Designed for PowerShell 7 on Windows 11. Tested against the
    gamedev-skills/awesome-gamedev-agent-skills repository and the
    dotnet/skills (Microsoft) repository.

.EXAMPLE
    pwsh ./Install-GodotDotNetSkills.ps1

    Default preset: Core.

.EXAMPLE
    pwsh ./Install-GodotDotNetSkills.ps1 -Preset CurrentSlice

.EXAMPLE
    pwsh ./Install-GodotDotNetSkills.ps1 -Preset Full

.EXAMPLE
    pwsh ./Install-GodotDotNetSkills.ps1 -Preset LegacyRecommended

    Preserves the previous default for backward compatibility. Not
    recommended for new sessions.

.EXAMPLE
    pwsh ./Install-GodotDotNetSkills.ps1 -ListOnly
#>

[CmdletBinding()]
param(
    [Parameter()]
    [ValidateSet("Core", "CurrentSlice", "Full", "LegacyRecommended", "Minimal", "AllGodot", "FullRepo")]
    [string] $Preset = "Core",

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

# -- Sources ---------------------------------------------------------------
#
# Each entry has a verified id, a source, and a confidence note. IDs are
# not invented. The fetch-and-stamp workflow in Install-SkillFromLock
# records a SHA-256 of the downloaded SKILL.md before it is allowed
# onto the agent's disk; if the upstream id is wrong, the install fails
# fast and the agent directory is untouched.

$Repo = "gamedev-skills/awesome-gamedev-agent-skills"

# The router skill is no longer installed: it was removed by the
# agent-workflow refactor because the engine (Godot/C#) is already
# locked and engine-detection adds nothing in-project.

# -- Presets ---------------------------------------------------------------

# Core: minimum viable. Engine-specific knowledge for Godot 4 + C#,
# the local C#/.NET policy, and the local capability adapters that
# point to it. No 3D, no multiplayer, no GDScript authoring, no
# game AI, no router.
$CoreSkills = @(
    "godot-csharp",
    "godot-nodes-scenes",
    "godot-signals-groups",
    "godot-resources",
    "godot-export"
)

# CurrentSlice: Core plus the disciplines the active EG-5 consolidation
# slice actually uses. Adding any of these is justified by a concrete
# in-scope surface; nothing in here is "just in case". Out-of-slice
# skills (3D, multiplayer, GDScript, game-AI, 2D movement, router) are
# removed; see SKILL_MIGRATION.md.
$CurrentSliceSkills = $CoreSkills + @(
    "godot-ui-control",
    "godot-tilemap",
    "godot-physics",
    "godot-animation",
    "godot-audio",
    "save-systems",
    "performance-optimization",
    "physics-tuning",
    "input-systems"
)

# Full: every approved technical capability (no 3D, multiplayer,
# GDScript, game-AI, 2D movement, or router). NOT a default.
$FullSkills = $CurrentSliceSkills + @(
    "godot-shaders",
    "camera-systems",
    "game-feel",
    "game-ui-ux"
)

# LegacyRecommended: the previous default, preserved verbatim for
# backward compatibility with already-installed user-level skill
# directories. Not recommended for new sessions.
$LegacyRecommendedSkills = @(
    "godot-csharp",
    "godot-gdscript",
    "godot-nodes-scenes",
    "godot-signals-groups",
    "godot-resources",
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
    "input-systems",
    "save-systems",
    "performance-optimization",
    "game-ai",
    "camera-systems",
    "game-ui-ux",
    "game-feel",
    "physics-tuning"
)

$MinimalSkills = @(
    "godot-csharp",
    "godot-nodes-scenes",
    "godot-signals-groups",
    "godot-physics",
    "godot-resources",
    "godot-export"
)

$AllGodotSkills = @(
    "godot-nodes-scenes",
    "godot-signals-groups",
    "godot-tilemap",
    "godot-physics",
    "godot-ui-control",
    "godot-animation",
    "godot-shaders",
    "godot-resources",
    "godot-audio",
    "godot-export",
    "godot-csharp"
)

# -- Helpers ---------------------------------------------------------------

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
No encontre npx.

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
    param([Parameter(Mandatory)][string[]] $Arguments)
    Write-Host ""
    Write-Host "npx $($Arguments -join ' ')" -ForegroundColor DarkGray
    & $script:NpxPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "El comando npx termino con codigo $LASTEXITCODE."
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
    if (-not $UseSymlinks) {
        $arguments += "--copy"
    }
    $arguments += "--yes"
    return $arguments
}

function Test-ProjectContext {
    if ($Global) { return }
    $projectFile = Join-Path (Get-Location) "project.godot"
    if (-not (Test-Path $projectFile)) {
        Write-Warning @"
No veo un project.godot en:
  $(Get-Location)

La instalacion sera local a esta carpeta. Lo normal es ejecutar el script
desde la raiz del proyecto Godot, o usar -Global.
"@
    }
}

function Test-DotNetEnvironment {
    Write-Section "Comprobando .NET y el proyecto"
    $dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
    if (-not $dotnet) {
        Write-Warning @"
No encontre dotnet en PATH.
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
        Write-Warning "dotnet existe, pero no devolvio SDK instalados."
    }
    else {
        Write-Host "SDK instalados:" -ForegroundColor Green
        $sdks | ForEach-Object { Write-Host "  $_" }
        $hasModernSdk = $sdks | Where-Object { $_ -match '^(8|9|1[0-9])\.' }
        if (-not $hasModernSdk) {
            Write-Warning @"
No detecte .NET 8 o superior. .NET 7 ya no es una base adecuada para una
instalacion moderna de Godot 4 con C#.
"@
        }
    }
    $project = Get-ChildItem -Path (Get-Location) -Filter "*.csproj" -File | Select-Object -First 1
    if (-not $project) {
        Write-Host "No encontre .csproj; puede generarse al crear el primer script C#." -ForegroundColor DarkGray
        return
    }
    $content = Get-Content -Path $project.FullName -Raw
    if ($content -match '<TargetFramework>\s*(?<tfm>[^<]+)\s*</TargetFramework>') {
        $targetFramework = $Matches.tfm.Trim()
        Write-Host "TargetFramework detectado: $targetFramework" -ForegroundColor Green
        if ($targetFramework -eq "net7.0") {
            Write-Warning @"
Tu proyecto apunta a net7.0. Conviene migrarlo a net8.0 si tu version de Godot
lo soporta. Hazlo en una rama y confirma que la version de Godot.NET.Sdk sea
compatible antes de tocar produccion.
"@
        }
    }
}

function Install-DotNetProjectPolicy {
    if ($SkipDotNetPolicy) { return }
    Write-Section "Instalando la policy local Godot C#/.NET"
    $tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("godot-dotnet-skills-" + [guid]::NewGuid().ToString("N"))
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
        $arguments = Get-InstallArguments -Source $tempRoot -Skills @("godot-dotnet-project")
        Invoke-NpxSkills -Arguments $arguments
    }
    finally {
        Remove-Item -Path $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($DisableTelemetry) {
    $env:DISABLE_TELEMETRY = "1"
}

Write-Section "Preparando instalacion"
Write-Host "Preset: $Preset"
Write-Host "Agentes: $($Agent -join ', ')"
Write-Host "Ambito: $(if ($Global) { 'Global' } else { 'Proyecto actual' })"
Write-Host "Metodo: $(if ($UseSymlinks) { 'Symlinks' } else { 'Copias (recomendado en Windows)' })"

$script:NpxPath = Resolve-Npx
Test-ProjectContext
Test-DotNetEnvironment

if ($ListOnly) {
    Write-Section "Skills disponibles en el repositorio"
    Invoke-NpxSkills -Arguments @("--yes", "skills", "add", $Repo, "--list")
    Write-Host ""
    Write-Host "No se instalo nada porque usaste -ListOnly." -ForegroundColor Yellow
    exit 0
}

switch ($Preset) {
    "Core"              { $selectedSkills = $CoreSkills }
    "CurrentSlice"      { $selectedSkills = $CurrentSliceSkills }
    "Full"              { $selectedSkills = $FullSkills }
    "LegacyRecommended" { $selectedSkills = $LegacyRecommendedSkills }
    "Minimal"           { $selectedSkills = $MinimalSkills }
    "AllGodot"          { $selectedSkills = $AllGodotSkills }
    "FullRepo"          { $selectedSkills = @("*") }
    default             { throw "Preset no soportado: $Preset" }
}

Write-Section "Instalando skills del repositorio"
Write-Host "Repositorio: $Repo"
Write-Host "Skills solicitadas: $($selectedSkills.Count)"

$installArguments = Get-InstallArguments -Source $Repo -Skills $selectedSkills
Invoke-NpxSkills -Arguments $installArguments
Install-DotNetProjectPolicy

Write-Section "Verificacion"
Invoke-NpxSkills -Arguments @("--yes", "skills", "list")

Write-Host ""
Write-Host "Instalacion terminada." -ForegroundColor Green
Write-Host "Para actualizar despues:" -ForegroundColor Cyan
Write-Host "  npx --yes skills update -y"
