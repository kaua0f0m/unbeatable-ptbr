# Compila o plugin e (opcionalmente) instala no jogo.
# Requer BepInEx 5 já instalado na pasta do jogo (para referenciar BepInEx.dll e 0Harmony.dll).
param(
    [string]$Game = "C:\Program Files (x86)\Steam\steamapps\common\UNBEATABLE",
    [switch]$Install
)
$ErrorActionPreference = "Stop"
$root    = $PSScriptRoot
$managed = Join-Path $Game "UNBEATABLE_Data\Managed"
$core    = Join-Path $Game "BepInEx\core"
$csc     = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path (Join-Path $core "BepInEx.dll"))) { throw "BepInEx não encontrado em $core. Instale o BepInEx 5 (x64) primeiro." }

$refs = @(
    (Join-Path $core "BepInEx.dll"), (Join-Path $core "0Harmony.dll"),
    (Join-Path $managed "UnityEngine.dll"), (Join-Path $managed "UnityEngine.CoreModule.dll"),
    (Join-Path $managed "Unity.Localization.dll"), (Join-Path $managed "YarnSpinner.Unity.dll"),
    (Join-Path $managed "YarnSpinner.dll"), (Join-Path $managed "netstandard.dll"),
    (Join-Path $managed "Unity.TextMeshPro.dll"), (Join-Path $managed "UnityEngine.UI.dll"),
    (Join-Path $managed "UnityEngine.TextRenderingModule.dll")
) | ForEach-Object { "/r:`"$_`"" }

$out = Join-Path $root "bin"
New-Item -ItemType Directory -Force $out | Out-Null
& $csc /nologo /target:library /optimize+ /nowarn:1701,1702 "/out:$out\UnbeatablePTBR.dll" @refs (Join-Path $root "src\Plugin.cs")
if ($LASTEXITCODE -ne 0) { throw "Falha na compilação." }
Write-Host "OK: $out\UnbeatablePTBR.dll"

if ($Install) {
    $dest = Join-Path $Game "BepInEx\plugins\UnbeatablePTBR"
    New-Item -ItemType Directory -Force (Join-Path $dest "traducao") | Out-Null
    Copy-Item "$out\UnbeatablePTBR.dll" $dest -Force
    Copy-Item (Join-Path $root "traducao\*.csv") (Join-Path $dest "traducao") -Force
    Write-Host "Instalado em $dest"
}
