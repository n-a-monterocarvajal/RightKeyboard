# Convenciones y trampas específicas

## Reglas obligatorias

1. **Español:** documentación, comentarios que explican decisiones, UI, errores, notas y descripciones nuevas se escriben en español. Tipos, métodos, propiedades y APIs siguen convenciones .NET/Win32 en inglés (`CONTRIBUTING.md`).
2. **El núcleo persiste:** ninguna UI WinUI debe abrir/escribir `preferences.json`; toda mutación pasa por IPC y `Configuration` en el hilo del núcleo.
3. **Rutas HID opacas:** no extraer identidad cortando strings de rutas PnP. Usar SetupAPI/ContainerId/InstanceId. Solo `HidSignature` extrae tokens públicos no únicos (VID/PID/MI/COL/enumerador), y únicamente para diagnóstico y para la exclusión por firma tras un ignorado manual — nunca como identidad.
4. **Recuperación conservadora:** nunca seleccionar arbitrariamente entre varias preferencias con la misma huella.
5. **Raw Input caliente:** no hacer I/O, enumeración SetupAPI, espera, logging síncrono ni trabajo UI en cada pulsación.
6. **`DefWindowProc` recibe `WM_INPUT`:** `RawInputWindow.WndProc` debe llamar siempre a `base.WndProc` para liberar recursos internos de Windows.
7. **Sin elevación:** manifiestos `asInvoker`; instalación y estado bajo usuario actual.
8. **Bandeja desde 1.5.0:** solo Configuración y Salir. Limpieza global vive en Configuración.
9. **No topmost permanente:** el selector puede usar un pulso temporal para activación, nunca quedar «siempre visible».
10. **Cambios de esquema:** validar, migrar explícitamente y escribir de forma atómica. Un esquema futuro se rechaza; no reinterpretarlo. Vigente: esquema 4 (`ignoredSignatures` + `signature` por dispositivo); el 3 migra en memoria y se reescribe como 4 al guardar.
11. **Firmas HID conservadoras:** una firma solo se registra tras ignorado manual de un dispositivo con huella vacía, solo se aplica con una única coincidencia conectada y sin distribuciones que la compartan, y no puede quedar huérfana: reactivar o asignar distribución la retira; `Forget` la retira si era el último portador.

## Costumbres actuales, no políticas impuestas por tooling

- C# usa namespaces de archivo, nullable e implicit usings; tipos `PascalCase`, campos/locales `camelCase`.
- Se prefieren records/record structs para DTO y datos inmutables.
- Pruebas NUnit se nombran `Acción_Condición_Resultado` o en español descriptivo.
- Se usan colecciones `[]` y pattern matching moderno de C#.
- No existe `.editorconfig`, analizador adicional, CI ni `TreatWarningsAsErrors`; por tanto estas son costumbres observadas, no validación automática.
- Commits históricos están en español y son breves.

## Trampas que ya causaron regresiones

### Recursos WinUI tienen un solo padre

Una misma instancia de `ResourceDictionary`, `Brush` cuando actúa como elemento, u otro `DependencyObject` no puede insertarse en dos padres. Reutilizar el mismo diccionario para temas produjo `COMException 0x800F1000: Element is already the child of another element` al construir Configuración. Crear instancias separadas por `Light`, `Dark` y `Default`.

### Plantilla interna del botón X de TextBox

Los estados usan `TextControlButtonBackground*` y `TextControlButtonForeground*`. Hacer transparente el fondo evita que el pressed rectangular desborde, pero `TextFillColorTertiaryBrush` puede volver invisible la X. Mantener recursos por tema y probar pasivo/hover/pressed en ambos temas. Ubicación: `SettingsWindow.ApplyRoundedTextBoxResources`; también la usa el selector.

### Storyboards programáticos

Beta 5/5.1 cerraba Configuración al detener/reiniciar un `Storyboard` que no había comenzado correctamente. Se cambió a Microsoft.UI.Composition con `try/catch` y estado explícito. No volver al patrón anterior solo para una microanimación.

### Overlay modal

La animación de salida debe terminar antes de `contentRoot.Children.Remove(overlay)`. El overlay usa margen negativo para cubrir cliente y barra personalizada, foco cíclico y Escape. Cambios de grid/row span pueden volver a dejar franjas activas.

### Foco no equivale a foreground

Una ventana puede estar al frente sin que el alias tenga foco. No usar solo `GetForegroundWindow()` como prueba de éxito; volver a solicitar foco XAML. Las restricciones varían según la aplicación foreground.

### Alias y seguimiento Raw Input

Mientras `AliasTextBox.FocusState != Unfocused`, `PollActivityAsync` puede actualizar texto de actividad pero no selección/scroll/foco. Enter guarda y entrega foco a la lista; Escape restaura y sale. Un timeout por carácter ya demostró ser insuficiente.

### Inventario y rendimiento

`KeyboardDevicesCollection` ya se actualiza por `WM_INPUT_DEVICE_CHANGE`. Reintroducir `devices.Refresh()` en `SettingsIpcServer.CreateSnapshot` vuelve a pagar SetupAPI en cada apertura y retrasa Configuración/Selector.

### Layouts y ventana foreground

`API.GetKeyboardLayoutDescription` activa temporalmente layouts para leer su nombre y restaura el anterior en `finally`. No usar ese método para cambiar la distribución de usuario. El cambio funcional es `RequestForegroundLayout`.

### Configuración en memoria

`TouchDevice` actualiza metadatos y `LastSeenUtc`. `Clear` y `ApplyImport` persisten primero y reemplazan memoria después; conservar ese orden para no divergir ante fallo de disco. `MergeFrom` sustituye por completo el modo de una identidad importada.

### Logging

La cola es acotada y `DropWrite`: perder diagnóstico es preferible a frenar entrada. El logger no debe registrar alias, rutas completas, caracteres ni virtual keys. En build normal, no instanciar `DiagnosticLogger`; usar `RIGHTKEYBOARD_DIAGNOSTICS` para variantes de host.

### Fallback no es el producto visual principal

`SettingsDialog`, `LayoutSelectionDialog` y `FluentWindowStyler` siguen compilando porque son recuperación. No aplicar allí una función y asumir que WinUI la obtuvo. Toda función visible normal requiere UI WinUI + IPC + núcleo.

## No hacer

- No añadir servicios, drivers o hooks globales para resolver un problema de UI.
- No cargar Windows App SDK dentro del residente ni mantener WinUI caliente sin mediciones y decisión explícita.
- No simular Acrylic/Mica con capturas, alpha GDI o DWM extendido sobre controles WinForms.
- No derivar identidad estable de handle Raw Input, índice USB, puerto o VID/PID solamente.
- No autoignorar todo dispositivo sin VID/PID, nombre o huella.
- No introducir acceso directo a registro/archivos desde WinUI si el núcleo ya es autoridad.
- No publicar logs o preferencias de una máquina de prueba; contienen identificadores técnicos aunque algunos estén anonimizados.
- No borrar el fallback hasta que instalación/recuperación y funciones migradas estén certificadas.
- No editar artefactos bajo `artifacts/`; siempre regenerarlos con scripts.
- No promover versión solo porque NUnit esté verde: foco, DPI, hardware e instalador requieren host.
