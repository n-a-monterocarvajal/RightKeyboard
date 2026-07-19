# Arquitectura, decisiones y descartes

## Mapa de procesos

```text
RightKeyboard.exe (residente WinForms/Win32)
├─ Program: instancia única y evento de cierre
├─ TrayApplicationContext: orquestación y NotifyIcon
├─ RawInputWindow: WM_INPUT + WM_INPUT_DEVICE_CHANGE
├─ KeyboardDevicesCollection + DeviceIdentityResolver
├─ Configuration: única autoridad de preferences.json
├─ SettingsIpcServer: pipe local v2
└─ inicia bajo demanda → ui/RightKeyboard.WinUI.exe
   ├─ sin argumentos: SettingsWindow
   └─ --select <identity>: LayoutSelectionWindow
```

Si `RightKeyboard.WinUI.exe` falta o no arranca, el residente abre `SettingsDialog` o `LayoutSelectionDialog` (WinForms). El fallback no participa cuando WinUI funciona.

## Flujo de una pulsación

1. `RawInputWindow` registra usage page `0x01`, usage `0x06`, con `RIDEV_INPUTSINK | RIDEV_DEVNOTIFY`.
2. `API.TryReadKeyboardEvent` copia `RAWINPUT/RAWKEYBOARD` sin asignaciones por pulsación.
3. `TrayApplicationContext.OnKeyboardInput` descarta liberaciones y evita reentrada durante selección.
4. Resuelve handle → ruta → identidad/huella/metadatos.
5. Excluye la firma sintética solo con la combinación conservadora actual; después consulta ignorado y clasificación inequívoca.
6. Si existe asociación, solicita esa distribución a la ventana foreground.
7. Si no existe, Configuración abierta solo recibe actividad; cerrada programa el selector con 100 ms de retraso para que la tecla disparadora no termine en el alias.
8. Al cerrar el selector WinUI, el núcleo vuelve a consultar la asociación y la aplica.

## Decisiones estructurales vigentes

### Raw Input, no hooks ni controladores

Raw Input entrega el handle del dispositivo y permite distinguir teclados sin servicio, elevación ni hook global. Los hooks no ofrecen la identidad física necesaria y añadirían superficie de fallo/seguridad. No sustituir esta base salvo una decisión arquitectónica explícita.

### Residente liviano + UI en proceso separado

El núcleo conserva WinForms solo para `NotifyIcon`, ventana de mensajes y fallback. WinUI/Windows App SDK se carga bajo demanda en otro proceso y se libera al cerrar. Se descartó XAML Islands y cargar WinUI dentro del residente porque mantendría runtime, dispatcher y recursos durante toda la sesión. Véase `docs/arquitectura-winui-1.5.md`.

### El núcleo es la única autoridad de estado

WinUI no abre `preferences.json`. Usa `SettingsIpcProtocol` v2 por named pipe limitado al usuario actual. El servidor ejecuta las mutaciones —incluidas agrupación y separación— en el `SynchronizationContext` del núcleo. Esto evita dos escritores y carreras entre Raw Input y UI.

### Los grupos son explícitos; las recuperaciones no crean membresía

El esquema 5 conserva un grupo lógico con alias/layout compartidos y sus identidades técnicas. Las preferencias individuales quedan latentes para poder revertir exactamente la agrupación. `TryGetEffectiveLayout` centraliza la prioridad del grupo; la recuperación conservadora por huella puede copiar una distribución a una identidad nueva, pero nunca la incorpora al grupo. Las firmas HID siguen perteneciendo exclusivamente a la ruta de ignorados.

### Identidad exacta primero; huella solo como respaldo no ambiguo

`ContainerId` distingue instancias y suele sobrevivir al puerto. Si falta, se usa hash de `InstanceId` y finalmente de ruta. La huella (fabricante, nombre útil, hardware IDs ordenados) describe modelo, no instancia: solo recupera estado si hay un dispositivo conectado con esa huella y las preferencias candidatas no contradicen. Se descartó «recordar siempre por VID/PID/nombre» porque confunde dos teclados iguales.

### Rutas PnP opacas

No se recortan ni interpretan para identidad. Solo el diagnóstico extrae tokens públicos/familia de enumerador. SetupAPI es la fuente de `ContainerId`, `InstanceId`, nombres y hardware IDs.

### Clasificación automática conservadora

Los nombres inequívocos de mouse/touchpad/trackball se ignoran automáticamente. Los ambiguos se presentan al usuario. La fuente sintética asociada al historial del portapapeles se excluye únicamente si carece de scan code, huella y capacidades y no pertenece a HID/ACPI/ROOT. Evitar reglas amplias del tipo «sin VID/PID» porque excluirían teclados internos legítimos.

### Cambio local a la ventana activa

Se descartó cambiar la distribución global con `ActivateKeyboardLayout` en cada pulsación. `RequestForegroundLayout` publica `WM_INPUTLANGCHANGEREQUEST` solo a la ventana foreground y respeta el modelo por ventana de Windows.

### Menú Win32 nativo

Después de fallos de repintado acumulativo en `ContextMenuStrip` y flyouts propios que cerraban el panel de iconos ocultos, se eligió `CreatePopupMenu/TrackPopupMenuEx`. En `1.5.0` solo contiene Configuración y Salir. No reintroducir limpieza global allí.

Revisado el 19 de julio de 2026 a raíz del menú de bandeja de las apps de Claude y ChatGPT, que comparten aspecto: ambas son Electron, así que ese menú no es del sistema sino el widget de Chromium dibujado por la aplicación. Reproducirlo exige una ventana propia con acrílico, no un menú: la implementación de referencia (Akhmetov, 2025) es una `Window` WinUI con `ItemsControl` y `DesktopAcrylicBackdrop`.

Se descarta por dos motivos independientes. El primero es de memoria: `MenuFlyout` y cualquier superficie WinUI exigen el Windows App SDK cargado en el proceso que las muestra, es decir dentro del residente y de forma permanente, peor que el frontend bajo demanda que sí se libera al cerrarse. El segundo ya se conocía por experiencia propia: un flyout propio cierra el panel de iconos ocultos, y el autor del artículo reconoce esa misma limitación sin resolverla.

Tampoco hay término medio. Los menús Win32 no reciben acrílico automáticamente en Windows 11 —`AcrylicMenus` existe para engancharlos—, así que la vía nativa actual no renuncia a nada que otra opción sin coste pudiera ofrecer. La decisión se mantiene sin cambios.

### Materiales reales; no Acrylic simulado sobre GDI

La ruta DWM/WinForms produjo transparencia y contraste defectuosos. WinUI usa `DesktopAcrylicBackdrop` con fallback `MicaBackdrop`; WinForms queda sólido. Se descartó seguir extendiendo DWM sobre controles GDI. `FluentWindowStyler` existe solo para el fallback.

### Instalador por usuario y autocontenido

Inno Setup con `PrivilegesRequired=lowest` instala bajo `%LOCALAPPDATA%`, incluye .NET y Windows App SDK y no descarga runtimes. Se prefirió a instalar runtime global (normalmente requiere administrador). MSIX quedó descartado por ahora por firma, actualización fuera de Store, inicio automático y migración. Véase `docs/distribucion-1.5.md`.

Se revisó descargar los runtimes desde el instalador para aligerarlo. Se descartó: tanto el .NET Desktop Runtime como el Windows App Runtime se instalan a nivel de máquina y piden elevación, lo que rompería el objetivo de instalación sin UAC. El mecanismo de descarga de Inno (`DownloadTemporaryFile`, WinHTTP) no cambia esa restricción, y la línea 7 no aportó ninguna vía nativa de Windows para prerrequisitos.

### Carpeta única para núcleo y frontend

Hasta 1.5.0 el frontend se publicaba en `{app}\ui` con su propia copia autocontenida: 217 de los 218 archivos del núcleo eran byte a byte idénticos a los del frontend, es decir, dos copias completas del runtime .NET 10. Ambas aplicaciones se publican ahora en la misma carpeta; los archivos `.deps.json` y `.runtimeconfig.json` son por aplicación y conviven sin conflicto. La publicación pasó de 813 archivos y 407,8 MB a 595 archivos y 299,1 MB.

`TrayApplicationContext.FindWinUiExecutable` busca primero junto al núcleo y conserva `ui\` como respaldo para instalaciones no actualizadas. El instalador elimina `{app}\ui` mediante `[InstallDelete]`, porque `[Files]` no borra lo que sobra al actualizar.

### ReadyToRun para ambos ejecutables

Beta 7 activó `PublishReadyToRun=true` solo en WinUI para reducir arranque en frío. Con la carpeta única el núcleo también lo activa: al compartir carpeta, `RightKeyboard.dll` era el único archivo que difería entre ambas publicaciones y la última en escribirse ganaba. Igualar la opción hace la salida determinista y de paso mejora el arranque del núcleo. No se mantiene un proceso UI caliente porque dañaría el objetivo de reposo liviano.

### Binarios de Windows ML descartados tras publicar

La publicación autocontenida arrastra `onnxruntime.dll`, `DirectML.dll` y las proyecciones de `Microsoft.Windows.AI.MachineLearning`: 40,4 MB que RightKeyboard nunca usa. `WindowsAppSDKMLPassthroughOnnxRuntime=true` no los elimina, ni desde el `.csproj` ni por línea de comandos, porque el despliegue autocontenido del SDK los copia por una vía que ese `.targets` no intercepta (WindowsAppSDK issue 5969).

`build-installer.ps1` los borra después de publicar. Se comprobó en ejecución que el núcleo arranca y que la ventana WinUI se crea sin ellos, y el código no referencia ninguna API de `Windows.AI`. La lista debe retirarse si alguna vez se adopta una de esas APIs.

### Diagnóstico sin Serilog y fuera del build normal

Se implementó una cola acotada y un escritor JSONL propio para no bloquear Raw Input ni añadir dependencias. En `1.5.0` el build normal no instancia el logger, no muestra controles y no acepta acciones IPC de diagnóstico. La capacidad queda detrás del símbolo de compilación `RIGHTKEYBOARD_DIAGNOSTICS`; no mantener una rama diagnóstica permanente que diverja.

## Decisiones históricas, no vigentes

- «Mantener toda la UI en WinForms» de `ROADMAP.md`/`docs/continuacion-1.5.md`: superada por beta 2.
- Renderer Fluent propio y DWM sobre WinForms: conservados solo como fallback, no dirección de producto.
- Frontend WinUI framework-dependent: el instalador actual publica ambos ejecutables autocontenidos.
- Acción **Limpiar preferencias** en bandeja: retirada antes de `1.5.0`; queda en Configuración.

## Dependencias frágiles entre componentes

- `RightKeyboard.WinUI` referencia el proyecto ejecutable `RightKeyboard` para reutilizar DTO, `VersionPresentation` y tipos internos mediante `InternalsVisibleTo`. Es deuda conocida; extraer contratos/modelo compartidos evitaría ese acoplamiento.
- El servidor de pipe acepta una conexión por vez y cada polling de actividad crea una conexión nueva cada 500 ms. Es suficiente hoy, no es un bus general.
- El inventario se refresca al iniciar y tras `WM_INPUT_DEVICE_CHANGE` con debounce de 200 ms. Beta 7 dejó de refrescar SetupAPI al abrir UI para reducir latencia; no deshacerlo sin evidencia de inventario obsoleto.
