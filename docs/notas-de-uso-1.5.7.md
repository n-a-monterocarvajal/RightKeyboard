# Notas de uso — RightKeyboard 1.5.7

Observaciones recogidas al usar la versión 1.5.7. No son notas de publicación (esas viven, inmutables, en `docs/releases/`) ni bugs ya triados: son un cuaderno de campo que un agente puede leer cuando busca pendientes y articular en un plan cuando se requiera.

Cada punto describe lo observado, lo que se sabe del código y lo que quedaría por decidir o hacer. Al convertir uno en trabajo real, trasládese al backlog (`.agent-context/05-siguientes-pasos.md`), a `ROADMAP.md` o a `docs/plan-1.6.0.md` según corresponda, y déjese aquí la referencia.

## 1. El Escritorio de Windows no parece disparar el cambio de distribución

**Tipo:** duda de comportamiento a confirmar, no necesariamente una limitación deliberada.

**Síntoma reportado:** con el Escritorio de Windows en foco (por ejemplo, tras minimizar todo y hacer clic sobre los iconos), presionar una tecla letra activa el salto rápido a un archivo cuyo nombre empieza con esa letra, pero la distribución de teclado no cambia como sí ocurre al escribir en un cuadro de texto. La pregunta original era si se trata de una limitación técnica esperada.

**Qué dice el código revisado (sin poder reproducirlo en esta VM sin GUI de Windows):**

- `RawInputWindow` registra el teclado con `RIDEV_INPUTSINK` (`API.RegisterKeyboardInput`, `RightKeyboard/Win32/API.cs`), que según la documentación de Win32 entrega `WM_INPUT` aunque la ventana de RightKeyboard no tenga el foco. La recepción del evento no debería depender de qué ventana esté activa.
- `RawKeyboardEvent.CanStartMapping` (`RightKeyboard/RawKeyboardEvent.cs`) solo excluye teclas modificadoras/auxiliares; una letra normal cumple la condición sin importar la ventana activa.
- `TrayApplicationContext.OnKeyboardInput` no consulta en ningún punto qué ventana tiene el foco antes de decidir si aplica una distribución conocida o abre el selector.
- El cambio de distribución en sí lo hace `ApplyLayout` → `API.RequestForegroundLayout` (`RightKeyboard/TrayApplicationContext.cs:492-498`, `RightKeyboard/Win32/API.cs:269-273`), que llama a `GetForegroundWindow()` y le envía `WM_INPUTLANGCHANGEREQUEST`. No hay ninguna exclusión explícita para el Escritorio.

**Conclusión provisional:** no hay una limitación deliberada en el código para el Escritorio; si el cambio no se nota, la causa más probable está en cómo la ventana que devuelve `GetForegroundWindow()` en ese momento (`Progman`, `WorkerW` o el `SysListView32` de los iconos, según la configuración de Explorer) atiende —o ignora— `WM_INPUTLANGCHANGEREQUEST`, no en que RightKeyboard decida no intentarlo.

**Pendiente:** 1.5.8 añadió la instrumentación necesaria. En la estación física, con el diagnóstico activado (`RIGHTKEYBOARD_DIAGNOSTICS`), verificar si `entrada_recibida` y `distribucion_aplicada` se registran al presionar una letra con el Escritorio en foco; el segundo evento indica ahora `foregroundTarget` y `outcome`. Si la petición se registra como enviada pero el idioma visible no cambia, el límite está del lado de Explorer/Shell y conviene documentarlo en `docs/limitaciones-conocidas-1.5.md`. Si no se registra nada, revisar si algo distinto a lo leído aquí está filtrando el evento.

**Promovido:** incorporado a la etapa 17 (1.5.8) de [`docs/plan-1.6.0.md`](plan-1.6.0.md#etapa-17--instrumentar-el-foco-del-selector-y-el-escritorio-158), junto a la instrumentación de foco del selector ya planeada ahí.

## 2. Casillas de verificación siguen con ángulos rectos después de un tercer abordaje

**Tipo:** defecto visual persistente, no resuelto pese a varios intentos.

**Síntoma:** en la Configuración WinUI, los `CheckBox` («Iniciar con Windows», «Diagnóstico detallado», «Ignorar eventos de este dispositivo») siguen mostrando el glifo con ángulos rectos tanto en la VM como en la estación física con 1.5.7, mientras que el resto de controles sí usa esquinas redondeadas (`CornerRadius(8)`). Dato relevante para el diagnóstico: en algún momento del arreglo, un build de prueba mostró el glifo redondeado en la VM, pero esa apariencia no se sostuvo hasta la versión publicada ni se repite ahora en la misma VM.

**Historial de abordajes:**

1. `ROADMAP.md` (revisión del 19 de julio de 2026) identificó el defecto y propuso la causa: el radio del glifo no vendría de la propiedad `CornerRadius` del control sino de un recurso de tema, con `CheckBoxCornerRadius` como nombre propuesto.
2. `d48214c` (1.5.5, «Pulir visualmente el panel de Configuración») implementó esa hipótesis: `root.Resources["CheckBoxCornerRadius"] = new CornerRadius(...)`.
3. `604aeb6` (1.5.5.1, «Corregir el pulido visual de Configuración») descartó la clave anterior y la sustituyó por `ApplyRoundedCheckBoxResources` (`RightKeyboard.WinUI/SettingsWindow.xaml.cs:525-533`), que sobrescribe `checkBox.Resources["ControlCornerRadius"]` además de `checkBox.CornerRadius`. El comentario en el código y el mensaje del commit afirman explícitamente que `CheckBoxCornerRadius` «no forma parte de ese template y no tenía efecto» y que este segundo intento sí aplica «el radio consumido por la plantilla real de CheckBox». El commit se dio por válido con 187 pruebas y CI de Windows en verde.

Esta nota de campo de 1.5.7 es la evidencia de que el segundo intento tampoco se sostiene fuera del momento puntual en que se observó funcionando.

**Por qué la suite no lo detecta:** `SettingsPanelVisualContractTests.CheckBox_UsaUnRadioProporcionadoAlRestoDeControles` (`RightKeyboard.NUnit/SettingsPanelVisualContractTests.cs`) compara dos constantes numéricas (`CheckBoxGlyphCornerRadius` contra `ControlCornerRadius / 2`); no instancia un `CheckBox`, no resuelve su `ControlTemplate` ni mide el `Rectangle` real del glifo. La prueba pasa siempre que las constantes no cambien, sin importar si el recurso tiene algún efecto visual. Ninguno de los tres abordajes tuvo, hasta ahora, una prueba que ejerza el árbol visual real.

**Hipótesis sin confirmar para un cuarto intento:** en la versión de Windows App SDK usada por el proyecto (`Microsoft.WindowsAppSDK` 2.3.1, `RightKeyboard.WinUI/RightKeyboard.WinUI.csproj`), el `Rectangle` del glifo dentro del `ControlTemplate` por defecto de `CheckBox` podría tener `RadiusX`/`RadiusY` fijos en el propio XAML del tema, sin enlazarlos a ningún `ThemeResource`; en ese caso ninguna clave puesta en `Resources` a nivel de instancia cambiaría el resultado, y la única vía sería copiar y sobrescribir el `ControlTemplate` completo del control. Tampoco se ha descartado que el efecto observado una vez en un build de prueba y ausente después se deba a una diferencia entre esa compilación y la publicada (Debug/Release, resources.pri regenerado, o caché de composición de la sesión) más que a un cambio de código.

**Pendiente:** decidir si se justifica una cuarta iteración inspeccionando el `ControlTemplate` resuelto en tiempo de ejecución (o el XAML de referencia de esa versión de Windows App SDK), añadir antes una prueba que sí ejerza el árbol visual para no repetir un «arreglo» que solo cambia constantes, o aceptar el ángulo recto como límite conocido de esta versión de WinUI y ajustar `ROADMAP.md` en consecuencia.

**Promovido:** incorporado como etapa 18 (1.5.9) de [`docs/plan-1.6.0.md`](plan-1.6.0.md#etapa-18--casillas-de-verificación-prueba-de-árbol-visual-y-cuarto-intento-159), con la prueba de árbol visual como paso previo obligatorio al cuarto intento.
