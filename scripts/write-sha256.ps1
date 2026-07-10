[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $FilePath,

    [string] $OutputPath
)

$ErrorActionPreference = 'Stop'
$resolvedFile = (Resolve-Path -LiteralPath $FilePath).Path

if (-not $OutputPath) {
    $directory = Split-Path -Parent $resolvedFile
    $name = [IO.Path]::GetFileNameWithoutExtension($resolvedFile)
    $OutputPath = Join-Path $directory ($name -replace '-Setup$', '-SHA256')
    $OutputPath += '.txt'
}

$hash = (Get-FileHash -LiteralPath $resolvedFile -Algorithm SHA256).Hash.ToLowerInvariant()
$line = "$hash  $([IO.Path]::GetFileName($resolvedFile))`n"
[IO.File]::WriteAllText($OutputPath, $line, [Text.UTF8Encoding]::new($false))

Write-Host "SHA-256: $hash"
Write-Host "Archivo: $OutputPath"
