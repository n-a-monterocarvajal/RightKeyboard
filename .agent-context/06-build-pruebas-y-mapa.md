# Build, ejecución, pruebas y mapa

Todos los comandos se ejecutan en PowerShell desde la raíz del repositorio.

## Requisitos de estación

- Windows 10/11 x64.
- .NET SDK **10.0.301**; `global.json` permite el último patch compatible.
- Inno Setup 6.3+ para instalador (validado históricamente con 6.7.3).
- Para efectos visuales reales: Windows 11 físico con transparencia activa; una VM puede ocultarlos.

Comprobar:

```powershell
dotnet --info
Get-Command msbuild,dotnet -ErrorAction SilentlyContinue
Get-Command ISCC.exe -ErrorAction SilentlyContinue
```

`msbuild` no es necesario para el flujo normal: los scripts usan `dotnet` e `ISCC.exe`.

## Restaurar, compilar y probar

```powershell
dotnet restore .\RightKeyboard.sln
dotnet build .\RightKeyboard.sln --configuration Release
dotnet test .\RightKeyboard.sln --configuration Release --no-build
```

Tras la Etapa 7: 161/161 pruebas. Si se omite `--no-build`, `dotnet test` recompila.

La puerta de las etapas 6-7 exige además la variante diagnóstica:

```powershell
dotnet build .\RightKeyboard.sln --configuration Release -p:DefineConstants=RIGHTKEYBOARD_DIAGNOSTICS
dotnet test .\RightKeyboard.sln --configuration Release --no-build -p:DefineConstants=RIGHTKEYBOARD_DIAGNOSTICS
```

Para la Etapa 7, comprobar por separado la Configuración normal y la diagnóstica a tamaño inicial y al mínimo de 900 × 640 píxeles lógicos. Registrar la escala efectiva y marcar como **Bloqueada por entorno** —no aprobada por inferencia— cualquier validación de 125 %, DPI mixto o monitor que la VM no permita evidenciar.

## Ejecutar en desarrollo

El residente Debug sabe localizar el frontend Debug en su carpeta de proyecto. Construir ambos primero:

```powershell
dotnet build .\RightKeyboard.sln --configuration Debug
dotnet run --project .\RightKeyboard\RightKeyboard.csproj --configuration Debug
```

El icono aparece en la bandeja. Clic secundario: Configuración/Salir. Para probar selector, usar preferencias vacías y una tecla utilizable de un dispositivo sin asociación.

No ejecutar solo `RightKeyboard.WinUI.exe` como prueba funcional completa: necesita el pipe del residente. Si se ejecuta aislado, puede mostrar error de configuración aunque XAML haya iniciado correctamente.

## Construir instalador reproducible

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1 -Version 1.5.0
```

Si Inno Setup no está en una ruta detectada:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-installer.ps1 `
  -Version 1.5.0 `
  -IsccPath 'C:\ruta\a\Inno Setup 6\ISCC.exe'
```

Salidas ignoradas por Git:

```text
artifacts\publish\win-x64\
artifacts\installer\RightKeyboard-<versión>-Setup.exe
artifacts\installer\RightKeyboard-<versión>-SHA256.txt
```

El script publica núcleo y WinUI autocontenidos; WinUI se restaura/publica con ReadyToRun. No editar la salida manualmente.

## Instalar/probar

Ejecutar el instalador generado sin elevación. Ruta esperada:

```text
%LOCALAPPDATA%\RightKeyboard\app\
```

Datos:

```text
%LOCALAPPDATA%\RightKeyboard\preferences.json
%LOCALAPPDATA%\RightKeyboard\exports\
%LOCALAPPDATA%\RightKeyboard\logs\      (solo variante RIGHTKEYBOARD_DIAGNOSTICS)
```

Pruebas de instalación y parámetros silenciosos: `installer/PRUEBAS.md`. El script Inno usa `/MERGETASKS="!startup"` para instalación nueva sin inicio y `/SILENT /BORRARDATOS=1` para desinstalar eliminando datos.

## Publicación

No publicar por inferencia. Requiere solicitud humana explícita. Secuencia esperada:

1. árbol limpio y commit exacto;
2. build/test Release;
3. instalador + SHA-256 desde ese commit;
4. push de `codex/version-1.5`;
5. tag/release prerelease y dos assets;
6. verificar digest remoto y tag.

## Mapa del repositorio

| Ruta | Responsabilidad |
|---|---|
| `RightKeyboard/Program.cs` | Instancia única, evento de cierre, bucle WinForms |
| `RightKeyboard/TrayApplicationContext.cs` | Orquestación de entrada, bandeja, UI y layout |
| `RightKeyboard/RawInputWindow.cs` | Ventana message-only y eventos Raw Input |
| `RightKeyboard/Win32/` | Interop Raw Input, layouts, foreground |
| `RightKeyboard/DeviceIdentityResolver.cs` | SetupAPI, identidad, nombre y huella |
| `RightKeyboard/KeyboardDevicesCollection.cs` | Inventario handle/path/identidad |
| `RightKeyboard/Configuration.cs` | Esquema 5, grupos lógicos, migración, validación y persistencia |
| `RightKeyboard/HidSignature.cs` | Firma HID parcial: tokens públicos del path y forma canónica |
| `RightKeyboard/SettingsIpcProtocol.cs` | DTO/acciones de pipe v2, incluida agrupación reversible |
| `RightKeyboard/SettingsIpcServer.cs` | Autoridad IPC en núcleo |
| `RightKeyboard/DiagnosticLogger.cs` | Diagnóstico de desarrollo tras `RIGHTKEYBOARD_DIAGNOSTICS` |
| `RightKeyboard/SettingsDialog.cs` | Configuración WinForms fallback |
| `RightKeyboard/LayoutSelectionDialog.cs` | Selector WinForms fallback |
| `RightKeyboard.WinUI/SettingsWindow.xaml.cs` | Configuración WinUI normal |
| `RightKeyboard.WinUI/LayoutSelectionWindow.cs` | Selector WinUI normal y foco |
| `RightKeyboard.WinUI/SettingsIpcClient.cs` | Cliente named pipe |
| `RightKeyboard.NUnit/` | 161 pruebas unitarias/interop/DTO |
| `installer/RightKeyboard.iss` | Instalación/actualización/desinstalación por usuario |
| `scripts/` | Publicación, instalador, SHA-256, prototipo histórico |
| `docs/` | Arquitectura, matrices, historia y notas humanas; varias están desactualizadas |

## Antes de editar

```powershell
git status --short --branch
git log -5 --oneline
dotnet test .\RightKeyboard.sln --configuration Release
```

Después de editar código:

```powershell
dotnet build .\RightKeyboard.sln --configuration Release
dotnet test .\RightKeyboard.sln --configuration Release --no-build
git diff --check
```

Los cambios de Raw Input, identidad, foco, DPI, instalador o hardware exigen además prueba física proporcional; NUnit no basta.
