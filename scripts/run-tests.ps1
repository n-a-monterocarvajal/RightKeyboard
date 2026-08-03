param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot

if (-not $NoBuild) {
    & dotnet build (Join-Path $repositoryRoot 'RightKeyboard.sln') -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        throw "La compilación $Configuration falló con código $LASTEXITCODE."
    }
}

& dotnet test (Join-Path $repositoryRoot 'RightKeyboard.NUnit\RightKeyboard.NUnit.csproj') `
    -c $Configuration --no-build --no-restore
if ($LASTEXITCODE -ne 0) {
    throw "La suite NUnit falló con código $LASTEXITCODE."
}

$winUiTests = Join-Path $repositoryRoot (
    "RightKeyboard.WinUI.Tests\bin\x64\$Configuration\net10.0-windows10.0.19041.0\win-x64\" +
    'RightKeyboard.WinUI.Tests.exe')
if (-not (Test-Path -LiteralPath $winUiTests -PathType Leaf)) {
    throw "No se encontró el ejecutor de pruebas WinUI: $winUiTests"
}

$env:TESTINGPLATFORM_TELEMETRY_OPTOUT = '1'
& $winUiTests --minimum-expected-tests 1
if ($LASTEXITCODE -ne 0) {
    throw "La suite WinUI falló con código $LASTEXITCODE."
}
