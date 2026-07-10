[CmdletBinding()]
param(
    [ValidateSet('SelfContained', 'FrameworkDependent')]
    [string] $Mode = 'SelfContained',
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repositoryRoot 'RightKeyboard.WinUI\RightKeyboard.WinUI.csproj'
$modeDirectory = if ($Mode -eq 'SelfContained') { 'self-contained' } else { 'framework-dependent' }
$outputDirectory = Join-Path $repositoryRoot "artifacts\winui-prototype\$modeDirectory"
$windowsAppSdkSelfContained = if ($Mode -eq 'SelfContained') { 'true' } else { 'false' }

if (Test-Path -LiteralPath $outputDirectory) {
    Remove-Item -LiteralPath $outputDirectory -Recurse -Force
}

dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet restore para el prototipo WinUI.' }

dotnet publish $projectPath `
    --configuration $Configuration `
    --no-restore `
    --output $outputDirectory `
    -p:WindowsAppSDKSelfContained=$windowsAppSdkSelfContained `
    -p:DebugSymbols=false `
    -p:DebugType=None
if ($LASTEXITCODE -ne 0) { throw 'Falló dotnet publish para el prototipo WinUI.' }

$files = Get-ChildItem -LiteralPath $outputDirectory -Recurse -File
$size = ($files | Measure-Object Length -Sum).Sum
Write-Host "Prototipo WinUI listo: $outputDirectory"
Write-Host "Modo: $Mode; archivos: $($files.Count); bytes: $size"
