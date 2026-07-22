[CmdletBinding()]
param(
    [string] $Version,
    [string] $Configuration = 'Release'
)

# Compila una salida portable autocontenida (win-x64) de RightKeyboard y la comprime
# en un ZIP para pruebas rápidas en una estación que no es la de desarrollo, sin pasar
# por el instalador Inno Setup. No sustituye la vía normal de distribución (el instalador
# de scripts/build-installer.ps1): es un artefacto de diagnóstico, coherente con
# docs/distribucion-1.5.md.
#
# Los pasos de publicación replican deliberadamente los de scripts/build-installer.ps1
# para que el ZIP se comporte igual que la app instalada. Si cambian allí, cámbialos aquí.

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'RightKeyboard\RightKeyboard.csproj'
$winUiProjectPath = Join-Path $repositoryRoot 'RightKeyboard.WinUI\RightKeyboard.WinUI.csproj'
$publishDirectory = Join-Path $repositoryRoot 'artifacts\portable\win-x64'
$outputDirectory = Join-Path $repositoryRoot 'artifacts\portable'

if (-not $Version) {
    [xml] $project = Get-Content -LiteralPath $projectPath
    $Version = [string] $project.Project.PropertyGroup.Version
}

if ($Version -notmatch '^\d+\.\d+\.\d+(?:\.\d+)?(?:-[0-9A-Za-z.-]+)?$') {
    throw "La versión '$Version' no tiene un formato válido."
}

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null

dotnet restore $projectPath --runtime win-x64 -p:PublishReadyToRun=true
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet restore.' }

dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --no-restore `
    --output $publishDirectory `
    -p:Version=$Version `
    -p:PublishReadyToRun=true `
    -p:ContinuousIntegrationBuild=true `
    -p:DebugSymbols=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet publish.' }

dotnet restore $winUiProjectPath --runtime win-x64 -p:PublishReadyToRun=true
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet restore para WinUI.' }

# Ambas aplicaciones se publican en la misma carpeta para compartir una sola copia del
# runtime .NET, igual que el instalador.
dotnet publish $winUiProjectPath `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --no-restore `
    --output $publishDirectory `
    -p:Version=$Version `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishReadyToRun=true `
    -p:ContinuousIntegrationBuild=true `
    -p:DebugSymbols=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet publish para WinUI.' }

# El despliegue autocontenido del Windows App SDK copia el runtime de Windows ML aunque el
# proyecto no lo use (ver la nota en scripts/build-installer.ps1). Se elimina tras publicar.
$machineLearningBinaries = @(
    'onnxruntime.dll',
    'DirectML.dll',
    'Microsoft.ML.OnnxRuntime.dll',
    'Microsoft.Windows.AI.MachineLearning.dll',
    'Microsoft.Windows.AI.MachineLearning.Projection.dll'
)
$removedBytes = 0
foreach ($binary in $machineLearningBinaries) {
    $path = Join-Path $publishDirectory $binary
    if (Test-Path -LiteralPath $path) {
        $removedBytes += (Get-Item -LiteralPath $path).Length
        Remove-Item -LiteralPath $path -Force
    }
}
Write-Host ('Runtime de Windows ML descartado: {0:N1} MB.' -f ($removedBytes / 1MB))

$zipPath = Join-Path $outputDirectory "RightKeyboard-$Version-win-x64-portable.zip"
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}
Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $zipPath -CompressionLevel Optimal
if (-not (Test-Path -LiteralPath $zipPath)) {
    throw "No se generó el ZIP esperado: $zipPath"
}

$shaPath = Join-Path $outputDirectory "RightKeyboard-$Version-win-x64-portable-SHA256.txt"
& (Join-Path $PSScriptRoot 'write-sha256.ps1') -FilePath $zipPath -OutputPath $shaPath
Write-Host "ZIP portable listo: $zipPath"
