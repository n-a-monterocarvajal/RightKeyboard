# Problemas conocidos y riesgos

Prioridades: **P0** compromete privacidad/datos o estabilidad crítica; **P1** debería resolverse en 1.5.x; **P2** mejora posterior o limitación asumida.

## Resuelto en 1.5.0 — diagnóstico fuera del producto público

**Estado:** el build normal ya no muestra «Diagnóstico detallado» ni «Abrir registros», el núcleo no construye `DiagnosticLogger` y las acciones IPC devuelven error si se invocan fuera de una compilación diagnóstica.

**Ubicación:** `RightKeyboard/TrayApplicationContext.cs`, `DiagnosticLogger.cs`, `SettingsIpcServer.cs` y `RightKeyboard.WinUI/SettingsWindow.xaml.cs`.

**Cómo reactivarlo:** compilar con el símbolo `RIGHTKEYBOARD_DIAGNOSTICS`. No publicar esa variante como release estable de usuario final.

## Resuelto en 1.5.0 — el diagnóstico ya no registra una tecla concreta en el caso sintético

`entrada_sintetica_excluida` dejó de incluir `keyboardEvent.VirtualKey`; conserva solo señales booleanas o categóricas seguras. Mantener esta regla si se amplía la variante diagnóstica.

Agregar una prueba directa sobre el evento JSON sería buena mejora, pero ya no es bloqueante de publicación.

## Resuelto — paridad de exportar, importar e inicio automático en WinUI

**Síntoma original:** exportar, importar y cambiar inicio automático existían en `SettingsDialog` (fallback), pero no en `SettingsWindow` ni en el protocolo IPC. En una instalación normal el usuario no podía invocarlas.

**Estado:** las tres operaciones existen en la Configuración WinUI (`ExportButton_Click`, `ImportButton_Click`, `StartupCheckBox_Click`) y viajan por acciones IPC validadas (`ExportAction`, `ImportPreviewAction`, `ImportApplyAction` y las de startup). El núcleo conserva la autoridad: WinUI solo elige el archivo y muestra la vista previa; `Configuration.Export`, `LoadImport` y `ApplyImport` hacen la escritura y el respaldo. Importar ofrece combinar, reemplazar o cancelar.

**Pendiente:** la validación física de FIS-07, FIS-08 y FIS-09, y la portabilidad entre dos equipos, que sigue registrada abajo como riesgo abierto.

## Implementado, pendiente de validación física — exclusión por firma HID parcial (Etapa 5)

**Síntoma original:** presentadores USB, mouse avanzados o interfaces virtuales pueden reportarse ante Windows como teclados HID normales y abrir el selector. Evidencia: presentador Baseus con `VID=2571`, `PID=4104`, interfaz `00`, colección `01`, capacidades de teclado y `clearlyNonKeyboard=false` (`Dispositivo F7E55424`).

**Estado:** ignorar manualmente un dispositivo con huella vacía registra su firma HID parcial (`HidSignature`, esquema 4). Una identidad nueva con esa firma se suprime sin selector solo si su huella es vacía, hay exactamente una coincidencia conectada y ninguna preferencia con distribución comparte la firma. Reversible desde la UI actual (reactivar/olvidar). El diagnóstico explica registro, aplicación y bloqueo.

**Pendiente:** validar en la estación física con el Baseus real (confirmar si su huella es vacía; si no lo es, la recuperación por huella ya cubría su reconexión y la firma cubre el resto de HID débiles) y ejecutar el cambio de puerto de la matriz.

## P1 — foco del selector depende de heurísticas Win32

`AllowSetForegroundWindow`, `AttachThreadInput`, `SetForegroundWindow`, `SetFocus`, un pulso topmost temporal y un retry a 180 ms intentan cooperar con las reglas de foco de Windows. La ventana puede estar delante sin que el `TextBox` tenga foco. No convertirla en topmost permanente. Instrumentar tiempos/resultado de foreground y probar varias aplicaciones antes de cambiar la secuencia.

## Resuelto en 1.5.7 — el fallback cubre un frontend que arranca y luego cae

**Síntoma original:** `TryLaunchWinUiSettings` y `TryLaunchWinUiSelector` solo abrían WinForms si no encontraban el ejecutable o `Process.Start` fallaba. Si WinUI creaba proceso y después lanzaba una excepción, Configuración mostraba su ventana de error y el selector terminaba sin que el residente reabriese el fallback. La afirmación más amplia de recuperación en los documentos de arquitectura no coincidía con el código.

**Estado:** existe un contrato de código de salida en el núcleo, `FrontendExitCodes` (`Success = 0`, `StartupFailure = 0x52_4B`). El frontend WinUI activa su ventana dentro de un bloque protegido y marca `interactiveReached` solo tras activarla; cualquier excepción antes de ese punto —capturada en `OnLaunched` o vista por `UnhandledException`— termina el proceso con `StartupFailure` en vez de dejar una ventana de error indistinguible. El residente engancha la salida del proceso en ambas superficies (`OnSettingsProcessExited` y `CompleteWinUiSelection`) y, cuando `FrontendExitCodes.ShouldFallBack` es cierto, abre el diálogo WinForms equivalente para la misma superficie. El contrato y la decisión están cubiertos por `FrontendExitCodesTests`.

**Alcance declarado (no se promete de más):** la recuperación se activa con certeza ante un fallo *antes* de que la ventana sea usable. Un cierre normal (`Success`), un fallo posterior a la interacción o una terminación externa del proceso (por ejemplo, matarlo desde el Administrador de tareas) no reabren el fallback, porque no representan «el frontend no llegó a ser usable». La validación visual del respaldo tras un fallo real de arranque queda para el carril C: esta VM no fuerza esa caída.

## Implementado, pendiente de validación física — agrupación manual de identidades (Etapa 6)

Si no hay `ContainerId` persistente, cambia `InstanceId` y existen dos dispositivos con igual huella o asociaciones conflictivas, RightKeyboard sigue sin adivinar automáticamente. La Etapa 6 añade en Configuración WinUI una operación manual para agrupar las identidades que el usuario reconoce como el mismo teclado.

El grupo lógico tiene un solo alias/layout efectivo y muestra debajo sus identidades técnicas. Separar restaura las preferencias individuales que quedaron latentes, y la recuperación por huella puede reutilizar la distribución sin crear membresía. No se admiten miembros ignorados y no existe fusión automática para dispositivos ambiguos.

**Pendiente:** validar en la estación física con dos teclados, reconexión y cambio de puerto. Esta VM no dispone de passthrough directo de dispositivos.

## Implementado, pendiente de validación DPI ampliada — disposición y mínimo de Configuración (Etapa 7)

La Configuración WinUI reúne Exportar, Importar y Limpiar en Preferencias, y mantiene aparte el inicio con Windows en Sistema, sin alterar la lista jerárquica de grupos e identidades técnicas. `SettingsWindow` impone 900 × 640 píxeles lógicos como mínimo mediante `WM_GETMINMAXINFO`, recalculado con el DPI de la ventana. El editor conserva una altura operativa, fija Guardar/Olvidar fuera de su scroll y la lista de dispositivos absorbe la reducción vertical. La variante diagnóstica añade sus controles dentro de Sistema; el build normal no crea esa subsección ni reserva su espacio.

**Evidencia disponible:** inspección visual de ambas variantes a 100 % en la VM, al tamaño inicial y al mínimo; Guardar/Olvidar visibles, controles sin solapamiento y lista de dispositivos desplazable. También se comprobó el estado rojo de hover de Limpiar sin ejecutar la acción destructiva. **Pendiente:** repetir a 125 %, con dos monitores/DPI mixto y texto ampliado en una estación que permita evidencia reproducible.

## P1 — portabilidad sin certificar

El esquema puede resolver layouts por identificador o nombre, pero no hay prueba entre dos equipos reales. Además, al faltar un layout, `LoadImport` conserva el dispositivo pero elimina esa asociación de la configuración candidata y solo emite advertencia; comprobar que esta semántica es la deseada antes de llamarla «preferencia pendiente».

## Licencia — resuelto el 19 de julio de 2026

Existe `LICENSE` en la raíz, con tres capas: obra original de 2007 de Antoine Aubry bajo CPOL 1.02, forks intermedios sin licencia declarada, y cambios de este fork bajo MIT. Procedencia trazada en `docs/procedencia-y-licencia.md`.

Restricción vigente que condiciona la distribución: por CPOL 5(d) la obra **no puede venderse, arrendarse ni alquilarse por sí sola**, aunque los aportes propios sean MIT. Distribuirla gratis sí es compatible.

Queda un punto abierto: CPOL 3(c) pide una nota en cada archivo modificado indicando cómo, cuándo y dónde se cambió. Hoy esa trazabilidad solo está en el historial de Git. Decidir si se añaden las notas o se documenta el historial como equivalente.

## P2 — primera detección no admite solo modificadores/auxiliares

`RawKeyboardEvent.CanStartMapping` exige key-down utilizable y no modificador. `Ctrl`, `Shift`, `Alt`, Windows, `Fn` y varias multimedia no abren el selector por sí solas; una vez configurado sí aplican la asociación. Es deliberado para evitar falsos positivos. Véase `docs/limitaciones-conocidas-1.5.md`.

## P2 — cobertura automatizada limitada

No hay pruebas de `SettingsIpcServer` extremo a extremo, SetupAPI real, ventana foreground, WinUI, instalador o migración real de `config.txt`. `ConfigurationTests.Version2File_IsMigratedInMemory` no cubre el archivo legado 1.4. `StartupManagerTests` sí cubre el registro, sobre una subclave desechable de `HKCU`. Existen CI en Windows (`.github/workflows/ci.yml`) y advertencias como error (`Directory.Build.props`).

## Limitaciones aceptadas

- Windows 10 y entornos sin composición pueden usar fondo sólido.
- Dos teclados idénticos solo se distinguen si Windows expone identidad suficiente.
- La UI WinUI tarda en frío por cargar .NET + Windows App SDK; el objetivo es medir y optimizar, no mantenerla residente.
- RightKeyboard no instala distribuciones de teclado.
