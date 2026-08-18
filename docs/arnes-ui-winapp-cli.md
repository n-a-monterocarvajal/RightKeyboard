# Arnés de UI con winapp CLI

Redactado el 18 de agosto de 2026 como traspaso, antes de publicar 1.6.0.
Ejecutado y corregido el 17 de agosto de 2026 sobre una estación Windows real, al
implementar `scripts/ui-harness.ps1`.

La versión original de esta nota se escribió desde una VM Linux y lo advertía: nada de lo
que afirmaba estaba ejecutado. Ahora sí lo está. Las secciones de abajo conservan lo que
resultó cierto y corrigen lo que no; los cambios están marcados para que se vea qué se
creía y qué se midió.

## Por qué antes de 1.6.0

`docs/releases/1.6.0.md` declara varias cosas como **no validadas**, y casi todas son
revisiones visuales que hoy se repiten a mano en cada versión. El historial del repo
justifica automatizarlas: 1.5.5 y 1.5.5.1 pasaron CI en verde mientras el glifo de las
casillas seguía cuadrado en la estación física, y la discrepancia solo se detectó al mirar.
Un arnés no sustituye la revisión visual, pero convierte en repetible la parte que sí puede
afirmarse por propiedades.

## Cómo se usa

```powershell
pwsh -File scripts/ui-harness.ps1
```

Levanta el residente y la ventana, corre las comprobaciones, deja una captura en
`artifacts/ui-harness/` y cierra lo que abrió. Sale 0 si todo se afirmó, 1 si algo falló.

| Parámetro | Para qué |
|---|---|
| `-Configuration` | `Debug` por omisión; `Release` para revisar lo que se publica |
| `-Attach` | Mide una ventana ya abierta en lugar de lanzarla, y no la cierra al terminar |
| `-KeepOpen` | Deja la aplicación viva al terminar, para seguir mirando a mano |
| `-SkipEvidence` | Omite la captura |
| `-EvidenceDirectory` | Cambia dónde se deja la captura |

## Requisito de entorno

**Windows con sesión interactiva.** `winapp ui` opera sobre UI Automation y necesita un
escritorio real.

Verificado en la estación: **winapp 0.6.0**, instalado en
`%LOCALAPPDATA%\Microsoft\WindowsApps`. Sigue en preview público desde enero de 2026;
conviene tenerlo en cuenta antes de convertirlo en puerta de CI.

**Corrección — el SDK fijado no está instalado.** `global.json` pide 10.0.301 con
`rollForward: latestPatch`, y la estación tiene 10.0.400. `latestPatch` no cruza bandas de
características, de modo que `dotnet build` falla antes de empezar. Ambos proyectos compilan
sin errores con 10.0.400 cuando se permite el salto. Hay que reconciliar `global.json` con
lo que se instala en las estaciones, o instalar 10.0.301: mientras tanto, el arnés no puede
partir de una compilación limpia en esta máquina.

## Cómo levantar la ventana

Los tres detalles del traspaso original se confirmaron tal cual:

1. **El residente tiene que estar corriendo.** `RightKeyboard.WinUI.exe` no es autónomo:
   `SettingsWindow` recibe un `SettingsIpcClient` que habla por el named pipe
   `RightKeyboard.Settings.v2` (`RightKeyboard.Shared/SettingsIpcProtocol.cs`).
2. **El residente es de instancia única.** Mutex `Local\RightKeyboard.SingleInstance`
   (`RightKeyboard/Program.cs`). Hay que cerrar cualquier instancia previa —incluida la del
   área de notificación— o el arnés medirá un proceso que no es el suyo.
3. **El mismo ejecutable sirve dos ventanas.** Sin argumentos abre la Configuración; con
   `--select <identidad>` abre el selector de distribución.

El título es `Configuración de RightKeyboard`, confirmado por `list-windows`. Conviene
resolver el HWND una vez y usar `-w`, como hace el arnés.

## Selectores

Los siete `AutomationId` declarados existen y aparecen en `inspect` como slug literal, sin
sufijo. Confirmado sobre el árbol real.

`DetailedDiagnosticsCheckBox` y `OpenDiagnosticsButton` **no aparecieron**: dependen de
`DiagnosticsAvailability.IsAvailable`, que es falso en una compilación normal. Cualquier
comprobación sobre ellos tiene que ser condicional, no obligatoria.

### Corrección importante: los slugs no son estables

El traspaso proponía alcanzar los controles del editor «por slug de `inspect`». **No se
puede.** Los slugs llevan un sufijo derivado del RuntimeId del elemento, que cambia entre
ejecuciones del proceso:

```
cmb-distribucin-f3eb    (primera ejecución)
cmb-distribucin-5dc4    (segunda ejecución, misma versión)
```

winapp además lo valida y rechaza un slug viejo con
`RuntimeId hash doesn't match — the UI may have changed`. El sufijo puede quedar obsoleto
incluso dentro de la misma sesión si la interfaz cambia.

Por eso el arnés **no fija ningún slug** y resuelve todo por nombre y tipo con
`search --json` en el momento de usarlo. Dos consecuencias prácticas:

- Hay que filtrar por `type`: la etiqueta y el control comparten nombre. «Nombre para este
  teclado» devuelve dos coincidencias, un `Text` y un `Edit`.
- `inspect --json` anida el árbol bajo `windows[].elements[]`, con los descendientes en
  `children`. Un recorrido que solo baje por `children` no encuentra nada.

Dado esto, agregar `AutomationId` a los controles del editor deja de ser cosmético: es lo
que permitiría dirigirse a ellos sin depender de textos en español que cambian con el
idioma. Sigue siendo un cambio de una línea por control, y ahora hay una razón mejor.

## Qué cubre el arnés

Las ocho filas del plan original, con lo que se midió al ejecutarlas.

| Validación | Cómo quedó | Contrato |
|---|---|---|
| Textos de subtítulo y descripción | `search` + comparación exacta | `SettingsSubtitle`, `EditorSectionDescription` |
| Aviso de edición y su glifo | `set-value` + `wait-for` | `AliasEditingHint` |
| Tamaño inicial 900 × 640 | bounds de `list-windows`, escalados por DPI | `MinimumWidth`/`MinimumHeight` |
| Ignorar deshabilita Distribución y Agrupar | `invoke` + `get-property IsEnabled` | `SettingsEditorAvailability` |
| Estado de casillas | `get-property ToggleState`, solo lectura | — |
| Guardia de cambios sin guardar | `set-value` → `invoke` cerrar → `wait-for` | `SettingsEditorStateTracker` |
| Filas: conexión, ignorado, distribución | `inspect` + forma del nombre accesible | `DevicePresentation` |
| Radio 8 → 4 y material de fondo | `screenshot` | revisión humana |

Correcciones sobre cómo se creía que se harían:

- **`MinimumWidth`/`MinimumHeight` no están en el contrato visual.** Son constantes privadas
  de `SettingsWindow.xaml.cs`. Y no se pueden afirmar a secas: `ResizeForCurrentDpi` escala
  con `ceil(lógico × dpi / 96)`, de modo que 900 × 640 solo vale al 100 %. El arnés replica
  la cuenta con el DPI real de la ventana.
- **El aviso de edición no se atrapa con `inspect`.** Vive 1400 ms (`activityHintTimer`) y
  su contenedor está colapsado el resto del tiempo, así que ni siquiera está en el árbol.
  Encadenar `set-value` con `wait-for`, que sondea cada 100 ms, lo detecta en ~140 ms. Una
  espera fija de 2 s lo pierde siempre; es lo que hizo fallar el primer intento.
- **No hace falta `focus` previo.** Aunque `AliasTextBox_TextChanged` exige que el cuadro
  tenga foco, `set-value` basta por sí solo.
- **El glifo `InformationGlyph` no es afirmable.** Es un `FontIcon` sin nombre accesible: no
  aparece como elemento buscable. El arnés lo informa como no verificable en vez de fingir
  que lo comprueba.
- **La casilla «Iniciar con Windows» se lee, no se acciona.** Su manejador escribe el
  arranque automático del sistema; un arnés no debe cambiar la configuración de la estación
  donde corre.

### Que falle cuando debe

Comprobado: al cambiar `SettingsSubtitle` en el contrato sin recompilar la aplicación, el
arnés detecta la divergencia y sale 1. Sin esa prueba, ocho comprobaciones en verde no
significan nada.

Los textos esperados se leen del código fuente en cada corrida, no se copian al script. El
contrato es `internal` y no puede referenciarse desde fuera del ensamblado; parsearlo
mantiene una sola fuente de verdad, y hace que un cambio de contrato sin cambio de interfaz
—o al revés— rompa el arnés a propósito.

## Preferir los verbos que no inyectan input

Confirmado en winapp 0.6.0: `inspect`, `search`, `get-property`, `get-value`, `invoke`,
`set-value`, `focus`, `wait-for` y `list-windows` operan por patrones UIA sin tocar ratón ni
teclado. `click`, `send-keys`, `hover`, `drag`, `touch` y `pen` inyectan input real y fallan
en escritorios bloqueados o seguros.

El arnés usa solo la primera familia. La única excepción es `screenshot`, que no es una
aserción sino evidencia, y es lo primero que se puede omitir con `-SkipEvidence` si alguna
vez corre en un runner.

En `--json`, `search` y `wait-for` escriben un envoltorio parseable en stdout y devuelven
código 1 cuando no hay coincidencia o vence el tiempo. Confirmado: `wait-for` devuelve
`found`, `waitedMs`, `element` y `timedOut`.

## Lo que el arnés no resuelve

- **El refresco automático de conexión y desconexión.** Puede verificar que la lista cambió
  *después* del evento, pero no producir una conexión USB real. Solo un hub conmutable por
  software lo automatizaría. Sigue siendo carril C.
- **Si Mica se está viendo.** El diagnóstico `material_fondo` registra que la petición no
  falló, no que el material se renderice; Windows cae a color sólido sin avisar a la
  aplicación. La captura sirve para desempatarlo a ojo.
- **El radio de las esquinas.** Se resuelve del tema en tiempo de ejecución y no se expone
  por UIA. Es el caso que motivó todo esto en 1.5.5, y sigue necesitando ojo humano; lo que
  cambia es que ahora la captura se produce sola en cada corrida.
- **La matriz física** de DPI, varios monitores, cuentas estándar y hardware real. El arnés
  sí es correcto bajo escalado, porque replica la cuenta de DPI, pero eso no es lo mismo que
  haberlo probado en una estación escalada.

## Referencias

- [winapp CLI](https://github.com/microsoft/winappcli) y su [referencia de UI automation](https://learn.microsoft.com/en-us/windows/apps/dev-tools/winapp-cli/ui-automation).
- Contrato visual: `RightKeyboard.Shared/SettingsPanelVisualContract.cs`.
- Pruebas de árbol visual ya existentes: `RightKeyboard.WinUI.Tests/CheckBoxVisualTreeTests.cs`.
