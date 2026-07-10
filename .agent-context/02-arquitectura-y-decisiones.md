# Arquitectura, decisiones y descartes

## Mapa de procesos

```text
RightKeyboard.exe (residente WinForms/Win32)
├─ Program: instancia única y evento de cierre
├─ TrayApplicationContext: orquestación y NotifyIcon
├─ RawInputWindow: WM_INPUT + WM_INPUT_DEVICE_CHANGE
├─ KeyboardDevicesCollection + DeviceIdentityResolver
├─ Configuration: única autoridad de preferences.json
├─ SettingsIpcServer: pipe local v1
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

WinUI no abre `preferences.json`. Usa `SettingsIpcProtocol` v1 por named pipe limitado al usuario actual. El servidor ejecuta las mutaciones en el `SynchronizationContext` del núcleo. Esto evita dos escritores y carreras entre Raw Input y UI.

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

### Materiales reales; no Acrylic simulado sobre GDI

La ruta DWM/WinForms produjo transparencia y contraste defectuosos. WinUI usa `DesktopAcrylicBackdrop` con fallback `MicaBackdrop`; WinForms queda sólido. Se descartó seguir extendiendo DWM sobre controles GDI. `FluentWindowStyler` existe solo para el fallback.

### Instalador por usuario y autocontenido

Inno Setup con `PrivilegesRequired=lowest` instala bajo `%LOCALAPPDATA%`, incluye .NET y Windows App SDK y no descarga runtimes. Se prefirió a instalar runtime global (normalmente requiere administrador). MSIX quedó descartado por ahora por firma, actualización fuera de Store, inicio automático y migración. Véase `docs/distribucion-1.5.md`.

### ReadyToRun solo para el frontend

Beta 7 activa `PublishReadyToRun=true` en restore/publish de WinUI para reducir arranque en frío; aumenta tamaño. No se mantiene un proceso UI caliente porque dañaría el objetivo de reposo liviano.

### Diagnóstico sin Serilog durante betas

Se implementó una cola acotada y un escritor JSONL propio para no bloquear Raw Input ni añadir dependencias. La estable 1.5 debe sacar controles, IPC y logger del producto público. La dirección acordada es un componente/proyecto opcional activado por build, no una rama diagnóstica permanente que diverja. Conservar un logger nulo o abstracción mínima en el núcleo; el artefacto público no debe incluir UI, marcador ni archivos de diagnóstico.

## Decisiones históricas, no vigentes

- «Mantener toda la UI en WinForms» de `ROADMAP.md`/`docs/continuacion-1.5.md`: superada por beta 2.
- Renderer Fluent propio y DWM sobre WinForms: conservados solo como fallback, no dirección de producto.
- Frontend WinUI framework-dependent: el instalador actual publica ambos ejecutables autocontenidos.
- Acción **Limpiar preferencias** en bandeja: retirada antes de `1.5.0`; queda en Configuración.

## Dependencias frágiles entre componentes

- `RightKeyboard.WinUI` referencia el proyecto ejecutable `RightKeyboard` para reutilizar DTO, `VersionPresentation` y tipos internos mediante `InternalsVisibleTo`. Es deuda conocida; extraer contratos/modelo compartidos evitaría ese acoplamiento.
- El servidor de pipe acepta una conexión por vez y cada polling de actividad crea una conexión nueva cada 500 ms. Es suficiente hoy, no es un bus general.
- El inventario se refresca al iniciar y tras `WM_INPUT_DEVICE_CHANGE` con debounce de 200 ms. Beta 7 dejó de refrescar SetupAPI al abrir UI para reducir latencia; no deshacerlo sin evidencia de inventario obsoleto.
