# Build, ejecución, pruebas y mapa

Todos los comandos se ejecutan en PowerShell desde la raíz del repositorio.

## Requisitos de estación

- Windows 10/11 x64.
- .NET SDK **10.0.301**; `global.json` permite el último patch compatible.
- Inno Setup 7.0+ para instalador (validado con 7.0.2; la línea 6.7.3 fue la anterior de referencia).
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
  -IsccPath 'C:\ruta\a\Inno Setup 7\ISCC.exe'
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

## Validación en GitHub Actions

GitHub Actions forma parte del procedimiento **normal** de validación del repositorio, no de un extra opcional. Documentación técnica completa: `docs/automatizacion-ci-cd.md`.

Workflows vigentes:

| Workflow | Archivo | Se ejecuta | Qué hace |
|---|---|---|---|
| CI | `.github/workflows/ci.yml` | push a `master`, cada pull request, manual (`workflow_dispatch`) | `dotnet restore` + `dotnet build -c Release` (solución completa; WinUI mapea a x64) + `dotnet test`. Advertencias como errores. Sin artefactos. |
| Compilación distribuible | `.github/workflows/build-package.yml` | manual (`workflow_dispatch`) o etiqueta `v*` | Entrada `artifact`: `installer` (por defecto, `scripts/build-installer.ps1` → instalador Inno Setup) o `zip` (`scripts/build-portable-zip.ps1` → ZIP portable autocontenido para pruebas). Ambas suben binario + `-SHA256.txt` (retención 7 días). Entrada `publish=yes` (solo con `installer`): un job aparte con `contents: write` crea la GitHub Release `vX.Y.Z` con los dos assets. |
| Dependabot | `.github/dependabot.yml` | semanal (lunes) | PRs agrupados de NuGet y GitHub Actions. |

Reglas obligatorias para agentes:

1. CI es parte del procedimiento normal de validación; no es opcional.
2. Antes de dar por terminada una modificación relevante, revisar el estado del workflow de CI de la rama o PR.
3. Todo cambio en código, proyectos, dependencias, recursos, manifiestos o configuración de compilación exige verificar la ejecución de CI correspondiente.
4. Una validación local satisfactoria **no** sustituye la comprobación en un runner limpio de GitHub Actions.
5. Los fallos de CI no se ignoran ni se ocultan: se corrigen, o se documentan con precisión si dependen de una restricción externa (permisos, facturación, dependencia externa). No modificar el código de la aplicación para enmascarar un fallo de CI.
6. Para obtener una salida limpia y descargable desde GitHub, usar el workflow «Compilación distribuible» (manual o etiqueta `v*`), no el CI.
7. Los artefactos de Actions son temporales (7 días) y no sustituyen una versión formal ni una GitHub Release.
8. Dependabot es parte del mantenimiento ordinario de dependencias.
9. Las actualizaciones de Dependabot se revisan y se validan por CI antes de integrarse.
10. No añadir matrices, ejecuciones programadas, artefactos permanentes ni jobs nuevos sin evaluar antes su impacto en el consumo de minutos (repositorio privado).
11. Los cambios en arquitectura, versión de .NET, Windows App SDK, empaquetado, firma o distribución deben reflejarse también en los workflows y en la documentación (`docs/automatizacion-ci-cd.md` y esta carpeta).
12. Nombre, ubicación, finalidad y disparadores de cada workflow quedan en la tabla anterior para no reconstruirlos desde los logs.

Limitación registrada: el instalador y los binarios se generan **sin firmar**; no hay firma Authenticode ni MSIX. Los secretos y pasos necesarios para añadirla están descritos en `docs/automatizacion-ci-cd.md`.

## Publicación

No publicar por inferencia. Requiere solicitud humana explícita. Secuencia esperada:

1. árbol limpio y commit exacto en `master` (CHANGELOG fechado con la versión);
2. build/test Release verde en CI;
3. compilación distribuible en modalidad `installer` con `publish=yes` sobre `master`;
   el job `publish` crea el tag `vX.Y.Z`, sube el instalador + `-SHA256.txt` y redacta
   las notas desde el CHANGELOG (Release firmada por `github-actions[bot]`);
4. verificar la Release, el tag, los dos assets y el SHA-256 publicado.

Alternativa manual (como hasta 1.5.5.1): compilar el `installer` sin `publish`, descargar
los dos assets del artefacto y crear la Release a mano. Nota histórica: la línea 1.5.x se
publicó como Release estable (no prerelease). Antes de 1.5.5.2 la Release se creaba a mano;
desde 1.5.5.2 el workflow puede crearla con `publish=yes`.

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
| `RightKeyboard/FrontendExitCodes.cs` | Contrato de código de salida del frontend y decisión de fallback |
| `RightKeyboard/SettingsDialog.cs` | Configuración WinForms fallback |
| `RightKeyboard/LayoutSelectionDialog.cs` | Selector WinForms fallback |
| `RightKeyboard.WinUI/App.xaml.cs` | Arranque del frontend y señal de fallo por código de salida |
| `RightKeyboard.WinUI/SettingsWindow.xaml.cs` | Configuración WinUI normal |
| `RightKeyboard.WinUI/LayoutSelectionWindow.cs` | Selector WinUI normal y foco |
| `RightKeyboard.WinUI/SettingsIpcClient.cs` | Cliente named pipe |
| `RightKeyboard.NUnit/` | 161 pruebas unitarias/interop/DTO |
| `installer/RightKeyboard.iss` | Instalación/actualización/desinstalación por usuario |
| `scripts/` | Publicación, instalador, SHA-256, prototipo histórico |
| `.github/workflows/` | CI (`ci.yml`) y compilación distribuible (`build-package.yml`) |
| `.github/dependabot.yml` | Mantenimiento semanal de NuGet y GitHub Actions |
| `docs/automatizacion-ci-cd.md` | Documentación técnica de los workflows |
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
