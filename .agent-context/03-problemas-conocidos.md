# Problemas conocidos y riesgos

Prioridades: **P0** compromete privacidad/datos o estabilidad crítica; **P1** debería resolverse en 1.5.x; **P2** mejora posterior o limitación asumida.

## Resuelto en 1.5.0 — diagnóstico fuera del producto público

**Estado:** el build normal ya no muestra «Diagnóstico detallado» ni «Abrir registros», el núcleo no construye `DiagnosticLogger` y las acciones IPC devuelven error si se invocan fuera de una compilación diagnóstica.

**Ubicación:** `RightKeyboard/TrayApplicationContext.cs`, `DiagnosticLogger.cs`, `SettingsIpcServer.cs` y `RightKeyboard.WinUI/SettingsWindow.xaml.cs`.

**Cómo reactivarlo:** compilar con el símbolo `RIGHTKEYBOARD_DIAGNOSTICS`. No publicar esa variante como release estable de usuario final.

## Resuelto en 1.5.0 — el diagnóstico ya no registra una tecla concreta en el caso sintético

`entrada_sintetica_excluida` dejó de incluir `keyboardEvent.VirtualKey`; conserva solo señales booleanas o categóricas seguras. Mantener esta regla si se amplía la variante diagnóstica.

Agregar una prueba directa sobre el evento JSON sería buena mejora, pero ya no es bloqueante de publicación.

## P1 — funciones disponibles solo en fallback, no en WinUI

**Síntoma:** exportar, importar y cambiar inicio automático existen en `SettingsDialog` (fallback), pero no en `SettingsWindow` ni en el protocolo IPC. En una instalación normal el usuario no puede invocarlas.

**Causa raíz:** la migración WinUI se integró por fases y nunca migró estas tres operaciones.

**Acción:** diseñar comandos IPC validados para exportar/importar/startup, añadir controles WinUI y pruebas. No permitir que WinUI escriba archivos o registro directamente si rompe la autoridad del núcleo.

## Implementado, pendiente de validación física — exclusión por firma HID parcial (Etapa 5)

**Síntoma original:** presentadores USB, mouse avanzados o interfaces virtuales pueden reportarse ante Windows como teclados HID normales y abrir el selector. Evidencia: presentador Baseus con `VID=2571`, `PID=4104`, interfaz `00`, colección `01`, capacidades de teclado y `clearlyNonKeyboard=false` (`Dispositivo F7E55424`).

**Estado:** ignorar manualmente un dispositivo con huella vacía registra su firma HID parcial (`HidSignature`, esquema 4). Una identidad nueva con esa firma se suprime sin selector solo si su huella es vacía, hay exactamente una coincidencia conectada y ninguna preferencia con distribución comparte la firma. Reversible desde la UI actual (reactivar/olvidar). El diagnóstico explica registro, aplicación y bloqueo.

**Pendiente:** validar en la estación física con el Baseus real (confirmar si su huella es vacía; si no lo es, la recuperación por huella ya cubría su reconexión y la firma cubre el resto de HID débiles) y ejecutar el cambio de puerto de la matriz.

## P1 — foco del selector depende de heurísticas Win32

`AllowSetForegroundWindow`, `AttachThreadInput`, `SetForegroundWindow`, `SetFocus`, un pulso topmost temporal y un retry a 180 ms intentan cooperar con las reglas de foco de Windows. La ventana puede estar delante sin que el `TextBox` tenga foco. No convertirla en topmost permanente. Instrumentar tiempos/resultado de foreground y probar varias aplicaciones antes de cambiar la secuencia.

## P1 — el fallback no cubre un frontend que arranca y luego cae

`TryLaunchWinUiSettings` y `TryLaunchWinUiSelector` solo abren WinForms si no encuentran el ejecutable o `Process.Start` falla. Si WinUI crea proceso y después lanza una excepción, Configuración muestra su ventana de error y el selector simplemente termina; el residente no reabre automáticamente el fallback. La afirmación más amplia de recuperación en documentos de arquitectura no coincide con el código. Diseñar una señal de «lista/interactiva» o código de salida distinguible antes de prometer fallback ante crash.

## Implementado, pendiente de validación física — agrupación manual de identidades (Etapa 6)

Si no hay `ContainerId` persistente, cambia `InstanceId` y existen dos dispositivos con igual huella o asociaciones conflictivas, RightKeyboard sigue sin adivinar automáticamente. La Etapa 6 añade en Configuración WinUI una operación manual para agrupar las identidades que el usuario reconoce como el mismo teclado.

El grupo lógico tiene un solo alias/layout efectivo y muestra debajo sus identidades técnicas. Separar restaura las preferencias individuales que quedaron latentes, y la recuperación por huella puede reutilizar la distribución sin crear membresía. No se admiten miembros ignorados y no existe fusión automática para dispositivos ambiguos.

**Pendiente:** validar en la estación física con dos teclados, reconexión y cambio de puerto. Esta VM no dispone de passthrough directo de dispositivos.

## Implementado, pendiente de validación DPI ampliada — disposición y mínimo de Configuración (Etapa 7)

La Configuración WinUI reúne Exportar, Importar y Limpiar en Preferencias, y mantiene aparte el inicio con Windows en Sistema, sin alterar la lista jerárquica de grupos e identidades técnicas. `SettingsWindow` impone 900 × 640 píxeles lógicos como mínimo mediante `WM_GETMINMAXINFO`, recalculado con el DPI de la ventana. El editor conserva una altura operativa, fija Guardar/Olvidar fuera de su scroll y la lista de dispositivos absorbe la reducción vertical. La variante diagnóstica añade sus controles dentro de Sistema; el build normal no crea esa subsección ni reserva su espacio.

**Evidencia disponible:** inspección visual de ambas variantes a 100 % en la VM, al tamaño inicial y al mínimo; Guardar/Olvidar visibles, controles sin solapamiento y lista de dispositivos desplazable. También se comprobó el estado rojo de hover de Limpiar sin ejecutar la acción destructiva. **Pendiente:** repetir a 125 %, con dos monitores/DPI mixto y texto ampliado en una estación que permita evidencia reproducible.

## P1 — portabilidad sin certificar

El esquema puede resolver layouts por identificador o nombre, pero no hay prueba entre dos equipos reales. Además, al faltar un layout, `LoadImport` conserva el dispositivo pero elimina esa asociación de la configuración candidata y solo emite advertencia; comprobar que esta semántica es la deseada antes de llamarla «preferencia pendiente».

## P1 — licencia ausente

El fork heredado no contiene `LICENSE`. No presentar 1.5 como jurídicamente lista para distribución/contribuciones sin resolver procedencia y licencia.

Procedencia ya trazada el 19 de julio de 2026 en `docs/procedencia-y-licencia.md`: el origen es el artículo de CodeProject núm. 20994 (2007), vía `agabor` → `gmcouto` → `mnivet`, y ninguno de los cuatro repositorios declara licencia. Falta confirmar autor y pie de licencia del artículo en la instantánea del archivo web, porque CodeProject cerró. Hasta tener ese dato no se puede elegir la licencia de salida: si el origen es CPOL, la obra derivada arrastra sus términos.

## P2 — primera detección no admite solo modificadores/auxiliares

`RawKeyboardEvent.CanStartMapping` exige key-down utilizable y no modificador. `Ctrl`, `Shift`, `Alt`, Windows, `Fn` y varias multimedia no abren el selector por sí solas; una vez configurado sí aplican la asociación. Es deliberado para evitar falsos positivos. Véase `docs/limitaciones-conocidas-1.5.md`.

## P2 — cobertura automatizada limitada

No hay pruebas de `SettingsIpcServer` extremo a extremo, StartupManager/registro, SetupAPI real, ventana foreground, WinUI, instalador o migración real de `config.txt`. `ConfigurationTests.Version2File_IsMigratedInMemory` no cubre el archivo legado 1.4. Tampoco hay CI ni advertencias como error.

## Limitaciones aceptadas

- Windows 10 y entornos sin composición pueden usar fondo sólido.
- Dos teclados idénticos solo se distinguen si Windows expone identidad suficiente.
- La UI WinUI tarda en frío por cargar .NET + Windows App SDK; el objetivo es medir y optimizar, no mantenerla residente.
- RightKeyboard no instala distribuciones de teclado.
