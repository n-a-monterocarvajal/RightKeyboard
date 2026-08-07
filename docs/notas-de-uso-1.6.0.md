# Notas de uso — RightKeyboard 1.6.0

Observaciones recogidas al probar 1.6.0 como artifact, antes de su publicación. No son notas de publicación (esas viven, inmutables, en `docs/releases/`) ni bugs ya triados: son un cuaderno de campo que un agente puede leer cuando busca pendientes y articular en un plan cuando se requiera.

Cada punto describe lo observado, lo que se sabe del código y lo que quedaría por decidir o hacer. Al convertir uno en trabajo real, trasládese al backlog (`.agent-context/05-siguientes-pasos.md`), a `ROADMAP.md` o a un plan de versión según corresponda, y déjese aquí la referencia.

## 1. Falta padding entre el indicador de selección y el texto de la fila

**Tipo:** defecto visual menor.

**Síntoma:** en «Dispositivos detectados», la fila seleccionada de la lista (captura: la primera «Teclado sin nombre» dentro de «Escritorio») muestra la barra nativa de selección de `ListViewItem` pegada al nombre del dispositivo y a su estado de conexión, sin margen entre ambos.

**Qué dice el código:** `CreateDeviceItem` fija `Padding = new Thickness(10, 8, 10, 8)` sobre el contenido de cada fila (`RightKeyboard.WinUI/SettingsWindow.xaml.cs:1062`). Ese padding es el mismo a izquierda que en los otros tres lados y no compensa el ancho de la barra de selección que WinUI dibuja fuera del contenido cuando la fila está seleccionada.

**Pendiente:** aumentar el padding izquierdo del contenido de la fila (o el margen del contenedor) para separar visualmente el indicador nativo del texto, sin afectar filas no seleccionadas ni la jerarquía de grupo/identidad técnica anidada.

## 2. Refresco automático de dispositivos conectados/desconectados

**Tipo:** mejora de comportamiento.

**Síntoma:** con la ventana de Configuración abierta, conectar o desconectar un teclado no actualiza la lista; hay que usar «Recargar» manualmente.

**Qué dice el código:** el botón «Recargar» (`ReloadDevicesButton`, etapa 14/1.5.5) dispara la única vía de actualización de la lista; no hay una suscripción a eventos de llegada/salida de dispositivo mientras la ventana está abierta.

**Pendiente:** decidir el mecanismo de detección en vivo (por ejemplo `WM_DEVICECHANGE` o el mismo camino de Raw Input que ya usa el núcleo) y cómo conservarlo coherente con la continuidad de edición ya existente (selección, cambios pendientes y desplazamiento se preservan al recargar, etapa 19/1.5.10) para que un refresco automático no descarte trabajo en curso a mitad de edición.

## 3. Cambiar el material de fondo a Acrylic nativo y radios de botones a Fluent nativo

**Tipo:** mejora visual, con una discrepancia a confirmar antes de tocar código.

**Síntoma reportado:** se pide reemplazar el fondo Mica actual por Acrylic nativo, y que los radios de todos los botones usen el valor Fluent nativo en vez de uno fijo.

**Qué dice el código:** `TryEnableBackdrop` (`RightKeyboard.WinUI/SettingsWindow.xaml.cs:646-663`) ya intenta `DesktopAcrylicBackdrop` primero y solo cae a `MicaBackdrop` si el Acrylic falla, y a `null` si ambos fallan. Es decir, el código actual **ya prioriza Acrylic**; si en la práctica se ve un material tipo Mica, la causa probable es que `DesktopAcrylicBackdrop` está lanzando una excepción en este entorno (¿VM sin composición de escritorio completa, política de energía, modo de ahorro?) y cae al `catch`, no que el código pida Mica a propósito. Sobre los botones, `ApplyFluentResources` fija `button.CornerRadius = new CornerRadius(8)` explícitamente (línea 634) en vez de heredar `ControlCornerRadius`/el recurso de tema del sistema.

**Pendiente:** antes de cambiar código, confirmar con diagnóstico si `DesktopAcrylicBackdrop` está fallando en la estación de prueba (y por qué) o si el fondo visto es Acrylic y simplemente se percibe como Mica. Para los botones, evaluar quitar el `CornerRadius` fijo y dejar que hereden el recurso de tema (`ControlCornerRadius`) como ya hacen las casillas desde 1.5.9, salvo que exista una razón deliberada para el valor fijo de 8.

## 4. Ancho mínimo al abrir la ventana

**Tipo:** mejora de comportamiento.

**Síntoma:** la ventana no abre al ancho mínimo operativo; requiere redimensionar manualmente.

**Pendiente:** fijar el tamaño inicial de `SettingsWindow` al mínimo operativo declarado (el mismo que ya se valida en 1.5.5 a 900×640, `docs/plan-1.6.0.md` etapa 14) en vez de heredar el tamaño por defecto de WinUI.

## 5. Textos de la cabecera y de ayuda contextual

**Tipo:** corrección de copy.

**Puntos:**

- El subtítulo bajo «Teclados y preferencias» dice «Administra los teclados detectados.» (`RightKeyboard.WinUI/SettingsWindow.xaml.cs:167`) y debería decir «Administra los dispositivos detectados. Asigna a los teclados la distribución deseada.» — más preciso porque la lista también incluye ratones u otros HID no-teclado que se pueden ignorar/agrupar.
- La línea «Entrada detectada: …» (`RightKeyboard.WinUI/SettingsWindow.xaml.cs:841-842`) debería mostrarse completamente en cursiva.
- El aviso «· La identificación se reanudará al dejar de escribir.» (línea 177) debería ir también en cursiva; evaluar reemplazar el «·» que lo antecede por un glifo nativo de información en vez de un separador tipográfico.
- El texto del panel de edición «Edita el grupo lógico o el teclado seleccionado. Las identidades técnicas agrupadas permanecen visibles en la lista.» (línea 383) debería precisar «… en la lista de dispositivos detectados.».

**Pendiente:** aplicar los cuatro cambios de texto y el estilo cursiva donde corresponda; decidir el glifo de información a usar si se reemplaza el «·».

## 6. Separador entre la ayuda del panel y «Nombre para este teclado»

**Tipo:** mejora visual.

**Síntoma:** el texto de ayuda «Edita el grupo lógico o el teclado seleccionado. …» (línea 383) queda pegado al campo «Nombre para este teclado» (línea 384) sin padding ni separador visual entre ambos bloques.

**Pendiente:** agregar espaciado y un separador (línea o `Spacing` de sección) entre la ayuda contextual del panel «Dispositivo seleccionado» y el primer campo editable, coherente con la jerarquía visual del resto del panel.
