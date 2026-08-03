[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PublishDirectory
)

# Una compilación normal deja estos recursos junto al ejecutable WinUI, pero una
# regresión de publicación puede omitirlos silenciosamente. Sin ambos archivos la
# aplicación abre, aunque los controles ya no usan íntegramente su tema WinUI.
$ErrorActionPreference = 'Stop'
$requiredResources = @(
    'App.xbf',
    'RightKeyboard.WinUI.pri'
)

foreach ($resource in $requiredResources) {
    $path = Join-Path $PublishDirectory $resource
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "La publicación WinUI está incompleta: falta '$resource' en '$PublishDirectory'."
    }

    if ((Get-Item -LiteralPath $path).Length -le 0) {
        throw "La publicación WinUI está incompleta: '$resource' está vacío."
    }
}

Write-Host 'Recursos WinUI verificados: App.xbf y RightKeyboard.WinUI.pri.'
