[CmdletBinding()]
param(
    [string] $Version,
    [string] $Configuration = 'Release',
    [string] $IsccPath = $env:ISCC_PATH
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'RightKeyboard\RightKeyboard.csproj'
$winUiProjectPath = Join-Path $repositoryRoot 'RightKeyboard.WinUI\RightKeyboard.WinUI.csproj'
$installerScript = Join-Path $repositoryRoot 'installer\RightKeyboard.iss'
$publishDirectory = Join-Path $repositoryRoot 'artifacts\publish\win-x64'
$installerDirectory = Join-Path $repositoryRoot 'artifacts\installer'

if (-not $Version) {
    [xml] $project = Get-Content -LiteralPath $projectPath
    $Version = [string] $project.Project.PropertyGroup.Version
}

if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?$') {
    throw "La versión '$Version' no tiene un formato válido."
}

if (Test-Path -LiteralPath $publishDirectory) {
    Remove-Item -LiteralPath $publishDirectory -Recurse -Force
}
if (Test-Path -LiteralPath $installerDirectory) {
    Remove-Item -LiteralPath $installerDirectory -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDirectory, $installerDirectory -Force | Out-Null

dotnet restore $projectPath --runtime win-x64
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet restore.' }

dotnet publish $projectPath `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --no-restore `
    --output $publishDirectory `
    -p:Version=$Version `
    -p:ContinuousIntegrationBuild=true `
    -p:DebugSymbols=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet publish.' }

$winUiDirectory = Join-Path $publishDirectory 'ui'
dotnet restore $winUiProjectPath --runtime win-x64 -p:PublishReadyToRun=true
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet restore para WinUI.' }

dotnet publish $winUiProjectPath `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --no-restore `
    --output $winUiDirectory `
    -p:Version=$Version `
    -p:WindowsAppSDKSelfContained=true `
    -p:PublishReadyToRun=true `
    -p:ContinuousIntegrationBuild=true `
    -p:DebugSymbols=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet publish para WinUI.' }

$candidates = @(
    $IsccPath,
    (Get-Command ISCC.exe -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source -ErrorAction SilentlyContinue),
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe')
) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -Unique

$compiler = $candidates | Select-Object -First 1
if (-not $compiler) {
    throw "La publicación autocontenida quedó lista en '$publishDirectory', pero no se encontró ISCC.exe. Instale Inno Setup 6 o defina ISCC_PATH y vuelva a ejecutar este script."
}

& $compiler "/DPublishDir=$publishDirectory" "/DOutputDir=$installerDirectory" "/DAppVersion=$Version" $installerScript
if ($LASTEXITCODE -ne 0) { throw 'Falló la compilación del instalador con Inno Setup.' }

$setupPath = Join-Path $installerDirectory "RightKeyboard-$Version-Setup.exe"
if (-not (Test-Path -LiteralPath $setupPath)) {
    throw "Inno Setup no generó el archivo esperado: $setupPath"
}

& (Join-Path $PSScriptRoot 'write-sha256.ps1') -FilePath $setupPath
Write-Host "Instalador listo: $setupPath"
