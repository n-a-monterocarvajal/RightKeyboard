[CmdletBinding()]
param(
    [string] $Configuration = 'Debug',
    [switch] $Attach,
    [switch] $KeepOpen,
    [switch] $SkipEvidence,
    [string] $EvidenceDirectory
)

# Arnés de revisión visual de la ventana de Configuración, sobre winapp CLI.
#
# Convierte en repetible la parte de la revisión de cada versión que puede afirmarse por
# propiedades de UI Automation, y deja el resto como evidencia para mirar. No sustituye la
# revisión humana: la separa en dos montones, el que una máquina puede sostener y el que no.
#
# Cada aserción se compara contra la fuente de verdad que ya existe en el repositorio
# (SettingsPanelVisualContract, SettingsEditorAvailability, DevicePresentation), leída del
# código en cada corrida. Si alguien cambia el contrato y no la interfaz, el arnés falla:
# esa es la intención, y por eso los textos esperados no se copian aquí.
#
# Sólo usa los verbos de winapp que operan por patrones UIA (inspect, search, get-property,
# get-value, invoke, set-value, wait-for, list-windows). Ninguno inyecta ratón ni teclado,
# de modo que el arnés sigue siendo viable en un runner sin escritorio interactivo salvo por
# la captura de evidencia, que es opcional.

$ErrorActionPreference = 'Stop'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$repositoryRoot = Split-Path -Parent $PSScriptRoot
$contractPath = Join-Path $repositoryRoot 'RightKeyboard.Shared\SettingsPanelVisualContract.cs'
$settingsWindowPath = Join-Path $repositoryRoot 'RightKeyboard.WinUI\SettingsWindow.xaml.cs'
$residentExe = Join-Path $repositoryRoot "RightKeyboard\bin\$Configuration\net10.0-windows\RightKeyboard.exe"

# El frontend cae en bin\<config>\ o en bin\x64\<config>\ según cómo se haya invocado la
# compilación: la solución fija la plataforma x64 y el proyecto suelto no. Probar las dos
# evita un fallo que sólo aparece en Release.
$frontendCandidates = @(
    "RightKeyboard.WinUI\bin\x64\$Configuration\net10.0-windows10.0.19041.0\win-x64\RightKeyboard.WinUI.exe",
    "RightKeyboard.WinUI\bin\$Configuration\net10.0-windows10.0.19041.0\win-x64\RightKeyboard.WinUI.exe"
) | ForEach-Object { Join-Path $repositoryRoot $_ }

$frontendExe = $frontendCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1
if (-not $frontendExe) {
    $frontendExe = $frontendCandidates[0]
}

if (-not $EvidenceDirectory) {
    $EvidenceDirectory = Join-Path $repositoryRoot 'artifacts\ui-harness'
}

$script:results = [System.Collections.Generic.List[object]]::new()

#region Contrato

# El contrato es `internal`, de modo que no puede referenciarse desde fuera del ensamblado.
# Leerlo del código fuente mantiene una sola fuente de verdad: duplicar los textos aquí
# convertiría al arnés en una segunda copia que envejece en silencio.
function Get-ContractStrings {
    param([string] $Path)

    $source = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $values = @{}
    foreach ($entry in [regex]::Matches($source, 'internal\s+const\s+string\s+(\w+)\s*=\s*(.*?);', 'Singleline')) {
        $name = $entry.Groups[1].Value
        $literals = [regex]::Matches($entry.Groups[2].Value, '"((?:[^"\\]|\\.)*)"')
        if ($literals.Count -eq 0) { continue }
        $values[$name] = ($literals | ForEach-Object { $_.Groups[1].Value }) -join ''
    }
    return $values
}

function Get-ContractInt {
    param([string] $Path, [string] $Name)

    $source = Get-Content -LiteralPath $Path -Raw -Encoding UTF8
    $entry = [regex]::Match($source, "const\s+int\s+$Name\s*=\s*(\d+)\s*;")
    if (-not $entry.Success) {
        throw "No se encontró la constante '$Name' en $Path."
    }
    return [int] $entry.Groups[1].Value
}

#endregion

#region winapp

function Invoke-Winapp {
    param(
        [Parameter(Mandatory)][string[]] $Arguments,
        [switch] $AllowFailure
    )

    $output = & winapp @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $text = ($output | Out-String).Trim()

    if ($exitCode -ne 0 -and -not $AllowFailure) {
        throw "winapp $($Arguments -join ' ') falló con código ${exitCode}: $text"
    }

    $parsed = $null
    if ($text) {
        try { $parsed = $text | ConvertFrom-Json } catch { $parsed = $null }
    }

    return [pscustomobject]@{
        ExitCode = $exitCode
        Text     = $text
        Json     = $parsed
    }
}

# Los slugs que devuelve `inspect` llevan un sufijo derivado del RuntimeId del elemento.
# Cambia entre ejecuciones del proceso, y winapp además lo valida: un slug de una corrida
# anterior falla con 'RuntimeId hash doesn't match'. Por eso el arnés no fija ningún slug y
# resuelve todo por nombre y tipo en el momento de usarlo.
function Resolve-Element {
    param(
        [Parameter(Mandatory)][int] $Hwnd,
        [Parameter(Mandatory)][string] $Name,
        [string] $Type,
        [switch] $Optional
    )

    $result = Invoke-Winapp -Arguments @('ui', 'search', $Name, '-w', "$Hwnd", '--json') -AllowFailure
    $found = @()
    if ($result.Json -and $result.Json.matches) {
        $found = @($result.Json.matches | Where-Object { $_.name -eq $Name })
        if ($Type) {
            $found = @($found | Where-Object { $_.type -eq $Type })
        }
    }

    $description = if ($Type) { "'$Name' de tipo $Type" } else { "'$Name'" }

    if ($found.Count -eq 0) {
        if ($Optional) { return $null }
        throw "No se encontró un elemento $description en la ventana $Hwnd."
    }
    if ($found.Count -gt 1) {
        throw "El nombre $description es ambiguo: $($found.Count) coincidencias."
    }

    return $found[0]
}

function Get-ElementProperty {
    param(
        [Parameter(Mandatory)][int] $Hwnd,
        [Parameter(Mandatory)][string] $Selector,
        [Parameter(Mandatory)][string] $Property
    )

    $result = Invoke-Winapp -Arguments @('ui', 'get-property', $Selector, '-w', "$Hwnd", '-p', $Property, '--json')
    return $result.Json.properties.$Property
}

#endregion

#region Resultados

function Add-Result {
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][ValidateSet('pass', 'fail', 'skip', 'evidence')][string] $Status,
        [string] $Detail
    )

    $script:results.Add([pscustomobject]@{
        Name   = $Name
        Status = $Status
        Detail = $Detail
    })

    $prefix = switch ($Status) {
        'pass'     { '  OK   ' }
        'fail'     { ' FALLA ' }
        'skip'     { ' OMITE ' }
        'evidence' { ' MIRAR ' }
    }
    Write-Host "$prefix $Name"
    if ($Detail) {
        Write-Host "        $Detail"
    }
}

function Test-Assertion {
    param(
        [Parameter(Mandatory)][string] $Name,
        [Parameter(Mandatory)][scriptblock] $Assertion
    )

    try {
        $detail = & $Assertion
        Add-Result -Name $Name -Status 'pass' -Detail ($detail | Select-Object -Last 1)
    }
    catch {
        Add-Result -Name $Name -Status 'fail' -Detail $_.Exception.Message
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string] $What)

    if ($Expected -ne $Actual) {
        throw "${What}: se esperaba '$Expected' y se obtuvo '$Actual'."
    }
}

#endregion

#region Ciclo de vida de la aplicación

Add-Type -Namespace RightKeyboardHarness -Name Dpi -MemberDefinition @'
[System.Runtime.InteropServices.DllImport("user32.dll")]
public static extern uint GetDpiForWindow(System.IntPtr hwnd);
'@

function Stop-RightKeyboard {
    # El residente es de instancia única por mutex: un segundo proceso sale de inmediato.
    # Sin este barrido el arnés terminaría midiendo una ventana que no lanzó él, por ejemplo
    # la que ya estuviera abierta desde el área de notificación.
    Get-Process -Name 'RightKeyboard', 'RightKeyboard.WinUI' -ErrorAction SilentlyContinue |
        Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

function Start-RightKeyboard {
    foreach ($path in @($residentExe, $frontendExe)) {
        if (-not (Test-Path -LiteralPath $path)) {
            throw "No existe '$path'. Compilá la solución en configuración $Configuration antes de correr el arnés."
        }
    }

    Stop-RightKeyboard

    # El frontend WinUI no es autónomo: SettingsWindow habla con el residente por el named
    # pipe RightKeyboard.Settings.v2. Sin residente la ventana abre igual, pero sin inventario,
    # y las comprobaciones del editor quedarían sin fila que seleccionar.
    $resident = Start-Process -FilePath $residentExe -PassThru
    Start-Sleep -Seconds 3
    if ($resident.HasExited) {
        throw "El residente salió con código $($resident.ExitCode) en lugar de quedarse corriendo."
    }

    # Sin argumentos abre la Configuración; con --select abriría el selector de distribución.
    Start-Process -FilePath $frontendExe | Out-Null
}

function Wait-SettingsWindow {
    param([int] $TimeoutSeconds = 30)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $result = Invoke-Winapp -Arguments @('ui', 'list-windows', '--json') -AllowFailure
        if ($result.Json) {
            $window = $result.Json |
                Where-Object { $_.processName -eq 'RightKeyboard.WinUI' -and $_.title } |
                Select-Object -First 1
            if ($window) { return $window }
        }
        Start-Sleep -Milliseconds 500
    }
    throw "La ventana de Configuración no apareció en $TimeoutSeconds segundos."
}

#endregion

#region Comprobaciones

function Test-ContractTexts {
    param([int] $Hwnd, [hashtable] $Contract)

    Test-Assertion -Name 'Subtítulo del panel coincide con SettingsSubtitle' -Assertion {
        $element = Resolve-Element -Hwnd $Hwnd -Name $Contract.SettingsSubtitle -Type 'Text'
        Assert-Equal -Expected $Contract.SettingsSubtitle -Actual $element.name -What 'Subtítulo'
        "«$($element.name)»"
    }

    Test-Assertion -Name 'Descripción del editor coincide con EditorSectionDescription' -Assertion {
        $element = Resolve-Element -Hwnd $Hwnd -Name $Contract.EditorSectionDescription -Type 'Text'
        Assert-Equal -Expected $Contract.EditorSectionDescription -Actual $element.name -What 'Descripción del editor'
        "«$($element.name)»"
    }
}

function Test-WindowSize {
    param([int] $Hwnd, $Window)

    Test-Assertion -Name 'Tamaño inicial de la ventana' -Assertion {
        $logicalWidth = Get-ContractInt -Path $settingsWindowPath -Name 'MinimumWidth'
        $logicalHeight = Get-ContractInt -Path $settingsWindowPath -Name 'MinimumHeight'

        # SettingsWindow.ResizeForCurrentDpi redondea hacia arriba con el DPI de la ventana.
        # Afirmar 900x640 a secas sólo funcionaría al 100 %; el arnés replica la misma cuenta
        # para que la comprobación siga siendo válida en una estación escalada.
        $dpi = [RightKeyboardHarness.Dpi]::GetDpiForWindow([System.IntPtr]$Hwnd)
        if ($dpi -lt 96) { $dpi = 96 }
        $expectedWidth = [Math]::Ceiling($logicalWidth * $dpi / 96)
        $expectedHeight = [Math]::Ceiling($logicalHeight * $dpi / 96)

        Assert-Equal -Expected $expectedWidth -Actual $Window.width -What 'Ancho'
        Assert-Equal -Expected $expectedHeight -Actual $Window.height -What 'Alto'
        "$($Window.width)x$($Window.height) a $dpi ppp (lógico ${logicalWidth}x${logicalHeight})"
    }
}

function Get-TreeElements {
    param($Node)

    if ($null -eq $Node) { return }
    if ($Node -is [System.Object[]]) {
        foreach ($item in $Node) { Get-TreeElements -Node $item }
        return
    }
    if ($Node.PSObject.Properties.Name -contains 'type') { $Node }
    # `inspect --json` envuelve el árbol en windows[].elements[]; los descendientes cuelgan
    # de children. Recorrer las tres claves cubre la raíz y cualquier profundidad.
    foreach ($childProperty in @('windows', 'elements', 'children')) {
        if ($Node.PSObject.Properties.Name -contains $childProperty) {
            Get-TreeElements -Node $Node.$childProperty
        }
    }
}

function Test-DeviceRows {
    param([int] $Hwnd)

    Test-Assertion -Name 'Filas de dispositivo con la forma de DevicePresentation' -Assertion {
        $result = Invoke-Winapp -Arguments @('ui', 'inspect', '-w', "$Hwnd", '--json', '--depth', '12') -AllowFailure
        $rows = @()
        if ($result.Json) {
            $rows = @(Get-TreeElements -Node $result.Json | Where-Object { $_.type -eq 'ListItem' })
        }

        if ($rows.Count -eq 0) {
            throw 'La lista de dispositivos detectados no expone ninguna fila. Revisá que el residente esté corriendo y respondiendo por el pipe RightKeyboard.Settings.v2.'
        }

        # GetAccessibleName compone «{nombre}. {partes}.», donde las partes empiezan por el
        # estado de conexión, siguen con «Ignorado» si corresponde y terminan por la
        # distribución o por «Sin distribución».
        $pattern = '^.+\. (Conectado|Desconectado)\.(?: Ignorado\.)? .+\.$'
        foreach ($row in $rows) {
            if ($row.name -notmatch $pattern) {
                throw "La fila «$($row.name)» no sigue la composición de DevicePresentation."
            }
        }
        "$($rows.Count) fila(s), p. ej. «$($rows[0].name)»"
    }
}

function Test-CheckBoxStates {
    param([int] $Hwnd)

    Test-Assertion -Name 'ToggleState de «Iniciar con Windows»' -Assertion {
        # Sólo lectura, deliberadamente: el manejador de esta casilla escribe el arranque
        # automático del sistema. El arnés no debe cambiar la configuración de la estación.
        $state = Get-ElementProperty -Hwnd $Hwnd -Selector 'StartWithWindowsCheckBox' -Property 'ToggleState'
        if ($state -notin @('On', 'Off')) {
            throw "ToggleState devolvió '$state', que no es un estado binario."
        }
        "ToggleState=$state (leída sin modificar)"
    }
}

function Test-IgnoreDisablesEditor {
    param([int] $Hwnd)

    Test-Assertion -Name 'Ignorar deshabilita Distribución y Agrupar' -Assertion {
        $ignored = Resolve-Element -Hwnd $Hwnd -Name 'Ignorar eventos de este dispositivo' -Type 'CheckBox'
        $before = Get-ElementProperty -Hwnd $Hwnd -Selector $ignored.selector -Property 'ToggleState'
        if ($before -ne 'Off') {
            throw "La casilla Ignorar empezó en '$before'; se esperaba 'Off' para poder medir la transición."
        }

        try {
            Invoke-Winapp -Arguments @('ui', 'invoke', $ignored.selector, '-w', "$Hwnd", '--json') | Out-Null
            Start-Sleep -Milliseconds 400

            $layout = Resolve-Element -Hwnd $Hwnd -Name 'Distribución' -Type 'ComboBox'
            $groupTarget = Resolve-Element -Hwnd $Hwnd -Name 'Agrupar con otra identidad' -Type 'ComboBox'

            # SettingsEditorAvailability.Create: con ignored, LayoutEnabled y GroupTargetEnabled
            # caen a false, y GroupButtonEnabled los sigue.
            Assert-Equal -Expected 'False' -Actual (Get-ElementProperty -Hwnd $Hwnd -Selector $layout.selector -Property 'IsEnabled') -What 'Distribución con ignorado'
            Assert-Equal -Expected 'False' -Actual (Get-ElementProperty -Hwnd $Hwnd -Selector $groupTarget.selector -Property 'IsEnabled') -What 'Agrupar con ignorado'
            'ambos combos pasan a deshabilitados, como predice SettingsEditorAvailability'
        }
        finally {
            # Devolver la casilla a su estado inicial: el arnés no debe dejar preferencias
            # cambiadas en la estación donde corre.
            $current = Resolve-Element -Hwnd $Hwnd -Name 'Ignorar eventos de este dispositivo' -Type 'CheckBox' -Optional
            if ($current) {
                $state = Get-ElementProperty -Hwnd $Hwnd -Selector $current.selector -Property 'ToggleState'
                if ($state -eq 'On') {
                    Invoke-Winapp -Arguments @('ui', 'invoke', $current.selector, '-w', "$Hwnd", '--json') -AllowFailure | Out-Null
                }
            }
        }
    }
}

function Test-AliasEditingHint {
    param([int] $Hwnd, [hashtable] $Contract, [string] $OriginalAlias)

    Test-Assertion -Name 'Aviso de edición del alias y su glifo' -Assertion {
        $alias = Resolve-Element -Hwnd $Hwnd -Name 'Nombre para este teclado' -Type 'Edit'

        # El aviso vive 1400 ms (activityHintTimer) y su contenedor está colapsado el resto
        # del tiempo, así que no aparece en el árbol. Un `inspect` después de una espera fija
        # lo pierde: hay que encadenar set-value con wait-for, que sondea cada 100 ms.
        Invoke-Winapp -Arguments @('ui', 'set-value', $alias.selector, "$OriginalAlias ", '-w', "$Hwnd", '--json') | Out-Null
        $wait = Invoke-Winapp -Arguments @('ui', 'wait-for', $Contract.AliasEditingHint, '-w', "$Hwnd", '-t', '1200', '--json') -AllowFailure

        if (-not $wait.Json -or -not $wait.Json.found) {
            throw "El aviso «$($Contract.AliasEditingHint)» no apareció dentro de la ventana de 1400 ms."
        }
        Assert-Equal -Expected $Contract.AliasEditingHint -Actual $wait.Json.element.name -What 'Texto del aviso'

        $glyph = Resolve-Element -Hwnd $Hwnd -Name $Contract.InformationGlyph -Optional
        $glyphDetail = if ($glyph) { 'glifo Info presente en el árbol' } else { 'glifo Info sin nombre accesible, no afirmable por UIA' }
        "detectado en $($wait.Json.waitedMs) ms; $glyphDetail"
    }
}

function Test-UnsavedChangesGuard {
    param([int] $Hwnd, [string] $OriginalAlias)

    Test-Assertion -Name 'Guardia de cambios sin guardar al cerrar' -Assertion {
        $alias = Resolve-Element -Hwnd $Hwnd -Name 'Nombre para este teclado' -Type 'Edit'
        Invoke-Winapp -Arguments @('ui', 'set-value', $alias.selector, "$OriginalAlias (arnés)", '-w', "$Hwnd", '--json') | Out-Null
        Start-Sleep -Milliseconds 300

        $close = Resolve-Element -Hwnd $Hwnd -Name 'Close' -Type 'Button'
        Invoke-Winapp -Arguments @('ui', 'invoke', $close.selector, '-w', "$Hwnd", '--json') | Out-Null

        $wait = Invoke-Winapp -Arguments @('ui', 'wait-for', 'Cambios sin guardar', '-w', "$Hwnd", '-t', '4000', '--json') -AllowFailure
        if (-not $wait.Json -or -not $wait.Json.found) {
            throw 'La ventana se cerró sin pedir confirmación pese a tener cambios sin guardar.'
        }

        # Cancelar deja la ventana abierta para lo que siga.
        $cancel = Resolve-Element -Hwnd $Hwnd -Name 'Cancelar' -Type 'Button'
        Invoke-Winapp -Arguments @('ui', 'invoke', $cancel.selector, '-w', "$Hwnd", '--json') | Out-Null
        Start-Sleep -Milliseconds 400

        # Restaurar el alias devuelve el editor a limpio: si no, el cierre final volvería a
        # toparse con la guardia y dejaría la ventana viva.
        $alias = Resolve-Element -Hwnd $Hwnd -Name 'Nombre para este teclado' -Type 'Edit'
        Invoke-Winapp -Arguments @('ui', 'set-value', $alias.selector, $OriginalAlias, '-w', "$Hwnd", '--json') -AllowFailure | Out-Null
        "diálogo detectado en $($wait.Json.waitedMs) ms y cancelado"
    }
}

function Save-Evidence {
    param([int] $Hwnd)

    # Radio de esquina y material de fondo no son afirmables por propiedades: el radio se
    # resuelve del tema en tiempo de ejecución y Windows cae de Mica a color sólido sin avisar
    # a la aplicación. La captura es evidencia para mirar, no una aserción.
    if (-not (Test-Path -LiteralPath $EvidenceDirectory)) {
        New-Item -ItemType Directory -Path $EvidenceDirectory -Force | Out-Null
    }

    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $target = Join-Path $EvidenceDirectory "configuracion-$stamp.png"
    $result = Invoke-Winapp -Arguments @('ui', 'screenshot', "$Hwnd", '-w', "$Hwnd", '--output', $target, '--json') -AllowFailure

    if ($result.ExitCode -eq 0) {
        Add-Result -Name 'Captura para revisar radio y material' -Status 'evidence' -Detail $target
    }
    else {
        Add-Result -Name 'Captura para revisar radio y material' -Status 'skip' -Detail "winapp screenshot falló: $($result.Text)"
    }
}

#endregion

#region Principal

if (-not (Get-Command 'winapp' -ErrorAction SilentlyContinue)) {
    throw 'winapp no está en el PATH. Instalalo por winget o npm antes de correr el arnés.'
}

$contract = Get-ContractStrings -Path $contractPath
foreach ($required in @('SettingsSubtitle', 'EditorSectionDescription', 'AliasEditingHint', 'InformationGlyph')) {
    if (-not $contract.ContainsKey($required)) {
        throw "SettingsPanelVisualContract no expone '$required'; el arnés está desincronizado del contrato."
    }
}

Write-Host ''
Write-Host "Arnés de UI de RightKeyboard — configuración $Configuration"
Write-Host ''

try {
    if (-not $Attach) {
        Start-RightKeyboard
    }

    $window = Wait-SettingsWindow
    $hwnd = [int] $window.hwnd
    Write-Host "Ventana «$($window.title)» (HWND $hwnd, PID $($window.processId))"
    Write-Host ''

    $aliasElement = Resolve-Element -Hwnd $hwnd -Name 'Nombre para este teclado' -Type 'Edit' -Optional
    $originalAlias = $null
    if (-not $aliasElement) {
        Add-Result -Name 'Editor de dispositivo disponible' -Status 'skip' -Detail 'Ninguna fila seleccionada: las comprobaciones del editor no pueden correr.'
    }
    else {
        $valueResult = Invoke-Winapp -Arguments @('ui', 'get-value', $aliasElement.selector, '-w', "$hwnd", '--json')
        $originalAlias = [string] $valueResult.Json.text
    }

    Test-ContractTexts -Hwnd $hwnd -Contract $contract
    Test-WindowSize -Hwnd $hwnd -Window $window
    Test-DeviceRows -Hwnd $hwnd
    Test-CheckBoxStates -Hwnd $hwnd

    if ($null -ne $originalAlias) {
        Test-IgnoreDisablesEditor -Hwnd $hwnd
        Test-AliasEditingHint -Hwnd $hwnd -Contract $contract -OriginalAlias $originalAlias
        Test-UnsavedChangesGuard -Hwnd $hwnd -OriginalAlias $originalAlias
    }

    if (-not $SkipEvidence) {
        Save-Evidence -Hwnd $hwnd
    }
}
finally {
    if (-not $KeepOpen -and -not $Attach) {
        Stop-RightKeyboard
    }
}

Write-Host ''
$passed = @($script:results | Where-Object { $_.Status -eq 'pass' }).Count
$failed = @($script:results | Where-Object { $_.Status -eq 'fail' }).Count
$skipped = @($script:results | Where-Object { $_.Status -eq 'skip' }).Count
$evidence = @($script:results | Where-Object { $_.Status -eq 'evidence' }).Count

Write-Host "$passed afirmadas, $failed fallidas, $skipped omitidas, $evidence para mirar."
Write-Host ''

if ($failed -gt 0) {
    exit 1
}
exit 0

#endregion
