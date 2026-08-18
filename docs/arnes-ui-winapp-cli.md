# Arnés de UI con winapp CLI — traspaso

Redactado el 18 de agosto de 2026, antes de publicar 1.6.0.

Nota de traspaso para la sesión que implemente el arnés. Recoge lo ya verificado sobre el
código para que no haya que re-derivarlo: cómo se levanta la aplicación, qué selectores ya
existen y qué validaciones concretas debería cubrir.

## Por qué antes de 1.6.0

`docs/releases/1.6.0.md` declara varias cosas como **no validadas**, y casi todas son
revisiones visuales que hoy se repiten a mano en cada versión. El historial del repo
justifica automatizarlas: 1.5.5 y 1.5.5.1 pasaron CI en verde mientras el glifo de las
casillas seguía cuadrado en la estación física, y la discrepancia solo se detectó al mirar.
Un arnés no sustituye la revisión visual, pero convierte en repetible la parte que sí puede
afirmarse por propiedades.

## Requisito de entorno

**Windows con sesión interactiva.** `winapp ui` opera sobre UI Automation y necesita un
escritorio real. La VM de desarrollo del proyecto es Linux sin SDK .NET: no sirve ni para
compilar ni para ejecutar el arnés. Esta nota se escribió desde esa VM, de modo que **nada
de lo de aquí está ejecutado** — es lectura de código, no evidencia.

`winapp` se instala por winget o npm. Está en preview público desde enero de 2026; conviene
tenerlo en cuenta antes de convertirlo en puerta de CI.

## Cómo levantar la ventana

Tres detalles que hacen fallar el primer intento si se desconocen:

1. **El residente tiene que estar corriendo.** `RightKeyboard.WinUI.exe` no es autónomo:
   `SettingsWindow` recibe un `SettingsIpcClient` que habla por el named pipe
   `RightKeyboard.Settings.v2` (`RightKeyboard.Shared/SettingsIpcProtocol.cs`). Sin el
   residente, la ventana abre pero no obtiene inventario.
2. **El residente es de instancia única.** Un segundo proceso sale de inmediato por mutex.
   Hay que cerrar cualquier instancia previa —incluida la del área de notificación— o el
   arnés medirá un proceso que no es el suyo. Ya está anotado como paso 2 de la verificación
   en `plan-1.6.0.md`.
3. **El mismo ejecutable sirve dos ventanas.** Sin argumentos abre la Configuración; con
   `--select <identidad>` abre el selector de distribución
   (`RightKeyboard.WinUI/App.xaml.cs`). El residente lo lanza sin argumentos desde
   `TryLaunchWinUiSettings`.

El título de la ventana es `Configuración de RightKeyboard`, utilizable como destino
(`winapp ui inspect -a "Configuración de RightKeyboard"`). Conviene resolver el HWND una vez
con `list-windows` y usar `-w`, que sobrevive a cambios de título.

## Selectores que ya existen

No hace falta instrumentar nada: las etapas 14 y 19 dejaron la superficie de automatización
puesta. `AutomationId` declarados en `RightKeyboard.WinUI/SettingsWindow.xaml.cs`:

| AutomationId | Control |
|---|---|
| `ReloadDevicesButton` | Recargar la lista |
| `ExportPreferencesButton` | Exportar preferencias |
| `ImportPreferencesButton` | Importar preferencias |
| `ClearPreferencesButton` | Limpiar preferencias |
| `StartWithWindowsCheckBox` | Iniciar con Windows |
| `DetailedDiagnosticsCheckBox` | Diagnóstico detallado (solo si está compilado) |
| `OpenDiagnosticsButton` | Abrir registros (solo si está compilado) |

Además, `AutomationProperties.Name`:

- La lista de dispositivos y su encabezado se llaman ambos `Dispositivos detectados`.
- **Cada fila lleva `row.AccessibleName`**, que ya compone conexión, estado ignorado y
  distribución (`DevicePresentation`, en `RightKeyboard.Shared`). Es el selector más útil del
  conjunto: permite afirmar el estado de una fila sin leer píxeles.

Los controles del editor (`AliasTextBox`, `LayoutComboBox`, `GroupTargetComboBox`,
`IgnoredCheckBox`, `SaveButton`, `ForgetButton`, `GroupButton`, `UngroupButton`) **no** tienen
`AutomationId`; se alcanzan por su `Header`/`Content` o por slug de `inspect`. Si el arnés los
necesita seguido, agregarles `AutomationId` es un cambio de una línea cada uno y sería la
única modificación de producción que este trabajo justifica.

## Qué debería cubrir

Mapeado contra lo que `docs/releases/1.6.0.md` declara pendiente. La columna de la derecha es
la fuente de verdad ya existente contra la que afirmar.

| Validación | Cómo | Contrato a comparar |
|---|---|---|
| Textos de subtítulo y ayuda del editor | `inspect --json` | `SettingsPanelVisualContract.SettingsSubtitle`, `EditorSectionDescription` |
| Aviso de edición y su glifo | `set-value` en alias, luego `inspect` | `AliasEditingHint`, `InformationGlyph` |
| Tamaño inicial 900 × 640 | bounds de `inspect` | `MinimumWidth`/`MinimumHeight` |
| Ignorar deshabilita Distribución y Agrupar | `invoke` + `get-property IsEnabled` | `SettingsEditorAvailability` |
| Estado de casillas | `get-property ToggleState` | — |
| Guardia de cambios sin guardar | `set-value` alias → cerrar → `wait-for` diálogo | `SettingsEditorStateTracker` |
| Filas: conexión, ignorado, distribución | `search` por nombre accesible | `DevicePresentation` |
| Radio 8 → 4 y material de fondo | `screenshot` en claro y oscuro | revisión humana |

Las siete primeras filas son afirmables por propiedades y no necesitan ojo humano. La octava
no: un screenshot es evidencia para mirar, no una aserción.

## Preferir los verbos que no inyectan input

`winapp ui` tiene dos familias. `inspect`, `search`, `get-property`, `invoke`, `set-value` y
`wait-for` operan por patrones UIA sin tocar el ratón ni el teclado. `click`, `send-keys`,
`hover`, `drag`, `touch` y `pen` inyectan input real y **fallan rápido**
(`no_interactive_desktop`, `foreground_not_target`) en escritorios bloqueados o seguros.

Construir el arnés sobre la primera familia lo deja utilizable en el runner de CI más
adelante. Reservar la segunda para lo que UIA no pueda accionar.

En `--json`, `search` y `wait-for` escriben un envoltorio parseable en stdout y devuelven
código 1 cuando no hay coincidencia o vence el tiempo — se puede ramificar por
`matchCount`/`timedOut` o por el código de salida.

## Lo que el arnés no resuelve

- **El refresco automático de conexión y desconexión.** Puede verificar que la lista cambió
  *después* del evento, pero no producir una conexión USB real. Solo un hub conmutable por
  software lo automatizaría. Sigue siendo carril C.
- **Si Mica se está viendo.** El diagnóstico `material_fondo` que agregó la etapa 22 registra
  que la petición no falló, no que el material se renderice; Windows cae a color sólido sin
  avisar a la aplicación. Un screenshot comparado contra el fondo de escritorio sí puede
  desempatarlo, pero es juicio humano.
- **La matriz física** de DPI, varios monitores, cuentas estándar y hardware real.

## Referencias

- [winapp CLI](https://github.com/microsoft/winappcli) y su [referencia de UI automation](https://learn.microsoft.com/en-us/windows/apps/dev-tools/winapp-cli/ui-automation).
- Contrato visual: `RightKeyboard.Shared/SettingsPanelVisualContract.cs`.
- Pruebas de árbol visual ya existentes, como precedente de cómo se afirma sobre XAML real:
  `RightKeyboard.WinUI.Tests/CheckBoxVisualTreeTests.cs`.
