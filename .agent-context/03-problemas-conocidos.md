# Problemas conocidos y riesgos

Prioridades: **P0** bloquea publicación estable o compromete privacidad/datos; **P1** debe resolverse o aceptarse antes de 1.5; **P2** mejora posterior o limitación asumida.

## P0 — diagnóstico todavía incluido en el producto público

**Síntoma:** Configuración muestra «Diagnóstico detallado» y «Abrir registros»; el núcleo siempre construye `DiagnosticLogger` y expone tres acciones IPC.

**Ubicación:** `RightKeyboard/TrayApplicationContext.cs`, `DiagnosticLogger.cs`, `SettingsIpcProtocol.cs`, `SettingsIpcServer.cs`, `RightKeyboard.WinUI/SettingsIpcClient.cs` y `SettingsWindow.xaml.cs`.

**Causa:** se añadió para investigar betas en hardware real y aún no se separó.

**Resolución prevista:** extraer implementación y UI a componente/build diagnóstico opcional; el instalador estable no debe contener ni activar esos caminos. No borrar la capacidad de prueba ni mantenerla en una rama divergente.

## P0 — el diagnóstico registra una tecla concreta en un caso

**Síntoma:** la documentación promete no registrar la tecla, pero `entrada_sintetica_excluida` incluye `keyboardEvent.VirtualKey`.

**Ubicación:** `RightKeyboard/TrayApplicationContext.cs`, bloque de `IsLikelySyntheticInputSource`, propiedad anónima `keyboardEvent.VirtualKey`.

**Causa raíz:** se añadió al diagnosticar el dispositivo sintético `64B58A17` y contradice la política posterior de señales booleanas.

**Acción:** retirar ese campo antes de cualquier build diagnóstico futuro; agregar una prueba que inspeccione el JSON/evento o encapsular detalles seguros en un DTO.

## P1 — funciones anunciadas no accesibles desde WinUI

**Síntoma:** exportar, importar y cambiar inicio automático existen en `SettingsDialog` (fallback), pero no en `SettingsWindow` ni en el protocolo IPC. En una instalación normal el usuario no puede invocarlas.

**Causa raíz:** la migración WinUI se integró por fases y nunca migró estas tres operaciones.

**Acción:** diseñar comandos IPC validados para exportar/importar/startup, añadir controles WinUI y pruebas. No permitir que WinUI escriba archivos o registro directamente si rompe la autoridad del núcleo.

## P1 — validación física incompleta del último beta 7

**Elementos concretos pendientes:**

- X de borrado del alias visible en `Pressed`, sin desbordar, claro/oscuro;
- selector al frente **y con foco** en Firefox y aplicaciones normales;
- fade-out de la confirmación antes de retirar el overlay;
- latencia fría/caliente tras ReadyToRun y eliminación del refresh SetupAPI;
- DPI mixto, contraste alto, Windows 10, RDP, Narrador y texto 200 %.

**Ubicación frágil:** `SettingsWindow.ApplyRoundedTextBoxResources`, `SettingsWindow.AnimateOverlayAsync`, `LayoutSelectionWindow.ActivateSelectorWindow/RetryActivation`.

**Causa:** estados de plantilla y restricciones de foreground de Windows no se prueban de forma fiable en VM ni mediante NUnit.

## P1 — foco del selector depende de heurísticas Win32

`AllowSetForegroundWindow`, `AttachThreadInput`, `SetForegroundWindow`, `SetFocus`, un pulso topmost temporal y un retry a 180 ms intentan cooperar con las reglas de foco de Windows. La ventana puede estar delante sin que el `TextBox` tenga foco. No convertirla en topmost permanente. Instrumentar tiempos/resultado de foreground y probar varias aplicaciones antes de cambiar la secuencia.

## P1 — el fallback no cubre un frontend que arranca y luego cae

`TryLaunchWinUiSettings` y `TryLaunchWinUiSelector` solo abren WinForms si no encuentran el ejecutable o `Process.Start` falla. Si WinUI crea proceso y después lanza una excepción, Configuración muestra su ventana de error y el selector simplemente termina; el residente no reabre automáticamente el fallback. La afirmación más amplia de recuperación en documentos de arquitectura no coincide con el código. Diseñar una señal de «lista/interactiva» o código de salida distinguible antes de prometer fallback ante crash.

## P1 — identidad al cambiar puerto puede seguir siendo ambigua

Si no hay `ContainerId` persistente, cambia `InstanceId` y existen dos dispositivos con igual huella o asociaciones conflictivas, RightKeyboard no recupera automáticamente. Es una decisión de seguridad de datos, no un bug simple. Cualquier mejora requiere propiedades adicionales estables; no «arreglar» eligiendo la primera coincidencia.

## P1 — portabilidad sin certificar

El esquema puede resolver layouts por identificador o nombre, pero no hay prueba entre dos equipos reales. Además, al faltar un layout, `LoadImport` conserva el dispositivo pero elimina esa asociación de la configuración candidata y solo emite advertencia; comprobar que esta semántica es la deseada antes de llamarla «preferencia pendiente».

## P1 — licencia ausente

El fork heredado no contiene `LICENSE`. No presentar 1.5 como jurídicamente lista para distribución/contribuciones sin resolver procedencia y licencia.

## P2 — primera detección no admite solo modificadores/auxiliares

`RawKeyboardEvent.CanStartMapping` exige key-down utilizable y no modificador. `Ctrl`, `Shift`, `Alt`, Windows, `Fn` y varias multimedia no abren el selector por sí solas; una vez configurado sí aplican la asociación. Es deliberado para evitar falsos positivos. Véase `docs/limitaciones-conocidas-1.5.md`.

## P2 — cobertura automatizada limitada

No hay pruebas de `SettingsIpcServer` extremo a extremo, StartupManager/registro, SetupAPI real, ventana foreground, WinUI, instalador o migración real de `config.txt`. `ConfigurationTests.Version2File_IsMigratedInMemory` no cubre el archivo legado 1.4. Tampoco hay CI ni advertencias como error.

## P2 — documentos públicos contradictorios

README, CHANGELOG, ROADMAP y matrices mezclan estados. Esta carpeta corrige continuidad, pero la documentación humana deberá auditarse separadamente después; no usar esa limpieza para ocultar pendientes funcionales.

## Limitaciones aceptadas

- Windows 10 y entornos sin composición pueden usar fondo sólido.
- Dos teclados idénticos solo se distinguen si Windows expone identidad suficiente.
- La UI WinUI tarda en frío por cargar .NET + Windows App SDK; el objetivo es medir y optimizar, no mantenerla residente.
- RightKeyboard no instala distribuciones de teclado.
