[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$fontImports = @(
    "game/assets/ui/fonts/GeistPixel-Regular-VariableFont_ELSH.ttf.import",
    "game/assets/ui/fonts/Jersey10-Regular.ttf.import",
    "game/assets/ui/fonts/PixelifySans-Regular.ttf.import"
)
$requiredSettings = @(
    "antialiasing=0",
    "generate_mipmaps=false",
    "multichannel_signed_distance_field=false",
    "subpixel_positioning=0",
    "oversampling=0.0"
)
$errors = New-Object System.Collections.Generic.List[string]

foreach ($relativePath in $fontImports) {
    $path = Join-Path $repoRoot $relativePath
    $content = Get-Content -Raw -LiteralPath $path
    foreach ($setting in $requiredSettings) {
        if ($content -notmatch "(?m)^$([regex]::Escape($setting))\r?$") {
            $errors.Add("$relativePath must contain '$setting'.")
        }
    }
}

$projectSettings = Get-Content -Raw -LiteralPath (Join-Path $repoRoot "game/project.godot")
if ($projectSettings -notmatch '(?m)^textures/canvas_textures/default_texture_filter=0\r?$') {
    $errors.Add("game/project.godot must keep the default canvas texture filter on Nearest (0).")
}

if ($errors.Count -gt 0) {
    $errors | ForEach-Object { Write-Error $_ }
    exit 1
}

Write-Output "Pixel font imports valid: $($fontImports.Count) fonts, solid rasterization, nearest filter."
