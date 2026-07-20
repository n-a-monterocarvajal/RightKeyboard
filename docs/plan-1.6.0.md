# Plan de ejecución camino a 1.6.0

Redactado el 19 de julio de 2026, tras cerrar las etapas 1 a 8 del plan inicial.

Este documento es el orden de ejecución y se actualiza al cerrar cada etapa. `ROADMAP.md` sigue siendo el backlog temático: describe **qué** falta; este archivo describe **en qué orden** se aborda y **en qué estado** está.

## Situación de partida

Las etapas 1 a 8 están cerradas y su resultado sigue sin publicar: exclusión por firma HID, agrupación de identidades, esquema 5 de preferencias, rediseño de la Configuración WinUI e instalador aligerado con Inno Setup 7. Ese conjunto es la versión `1.5.1`.

La revisión del ejecutable del 19 de julio de 2026 dejó observaciones de interfaz registradas en `ROADMAP.md`, y un barrido del repositorio mostró que el trabajo pendiente es mayor de lo que ese archivo reflejaba: riesgo legal sin resolver, documentación que contradice al código, dos fragilidades P1 de arquitectura y validación física congelada en beta 1.

## Mecánica de versión

La versión se declara en dos sitios que deben moverse juntos: `RightKeyboard/RightKeyboard.csproj` y `RightKeyboard.WinUI/RightKeyboard.WinUI.csproj`. `scripts/build-installer.ps1` la lee del primero cuando no se pasa `-Version`. Cada etapa cierra con su bump y su entrada en `CHANGELOG.md`.

## Advertencia sobre la publicación

El bloqueo por licencia se levantó el 19 de julio de 2026: existe `LICENSE` y la procedencia está trazada, según el carril B. Queda la restricción de CPOL 5(d) —no vender, arrendar ni alquilar la obra por sí sola— con la que la publicación gratuita del instalador es compatible. Sigue en pie decidir si `1.5.1` es una publicación pública o solo un corte interno de versión.

## Carril A — etapas de código

Secuenciales. Cada una es una sesión, una rama, un PR y un bump de versión.

| Etapa | Versión | Asunto | Estado |
|---|---|---|---|
| 9 | — | Sincronizar documentación con el código | Completada |
| 10 | 1.5.1 | Publicar lo acumulado | Completada |
| 11 | 1.5.2 | Guardia de cambios sin guardar | Completada |
| 12 | 1.5.3 | Coherencia de Ignorar y Agrupar | Pendiente |
| 13 | 1.5.4 | Nomenclatura y estado de fila | Pendiente |
| 14 | 1.5.5 | Pulido visual del panel | Pendiente |
| 15 | 1.5.6 | Exploración de pulsaciones sintéticas | Pendiente |
| 16 | 1.5.7 | Fallback verificable ante caída del frontend | Pendiente |
| 17 | 1.5.8 | Instrumentar el foco del selector | Pendiente |
| 18 | 1.6.0 | Extraer contratos compartidos y cerrar versión | Pendiente |

### Etapa 9 — Sincronizar documentación con el código · completada el 19 de julio de 2026

Fue primero porque `1.5.1` no debía publicarse con documentación que contradice al producto, empezando por el README público. Sin cambios de código ni bump de versión.

Corregido:

- Exportar/importar e «Iniciar con Windows» figuraban como pendientes en `README.md`, `docs/distribucion-1.5.md`, `docs/calidad-1.5.md` y `.agent-context/03-problemas-conocidos.md` pese a estar implementados. Ahora se describen como disponibles, con la portabilidad entre equipos declarada sin certificar.
- `ROADMAP.md` marca explícitamente como revertida la decisión de conservar WinForms y no adoptar WinUI 3, conservando vigentes los criterios visuales.
- AUT-15 y AUT-13 pasan a «Cubierto» en `docs/calidad-1.5.md`, con la evidencia concreta; el riesgo 5 pasa a resuelto y el 4 baja de Alto a Medio.
- Agrupar identidades y detección preventiva de HID ambiguos se reclasifican de backlog futuro a implementadas con validación física pendiente, en `ROADMAP.md`.
- `.agent-context/01-estado-actual.md` deja de afirmar que no existen `LICENSE` ni configuración `.github`.

Criterio aplicado: lo implementado pero sin validar en hardware se declara así, no como terminado.

`docs/releases/1.5.0.md` queda **sin tocar** de forma deliberada. Las notas de una versión publicada describen lo que se publicó en su fecha y no se editan para reflejar trabajo posterior; su lista de pendientes se lee contra el 9 de julio de 2026. La convención quedó registrada en `.agent-context/01-estado-actual.md`.

Sigue abierto y no lo resuelve esta etapa: la semántica de «preferencia pendiente» al importar con una distribución ausente. `LoadImport` conserva el dispositivo pero descarta la asociación y solo advierte; falta decidir si es lo deseado. Anotado en `docs/distribucion-1.5.md` y en `.agent-context/03-problemas-conocidos.md`.

### Etapa 10 — Publicar 1.5.1 · completada el 19 de julio de 2026

Corte de versión de todo lo acumulado desde `1.5.0`, sin funcionalidad nueva.

- Versión `1.5.1` declarada en los dos `.csproj`, con `CHANGELOG.md` cerrado y notas inmutables en `docs/releases/1.5.1.md`.
- Publicación pública y gratuita aprobada; CPOL 5(d) impide vender, arrendar o alquilar la obra por sí sola, pero no su distribución gratuita.
- Suite Release en verde: 161/161 pruebas, sin omitidas.
- Instalador compilado con Inno Setup 7.0.2: 65.787.471 bytes, publicación de 590 archivos y SHA-256 `93eebe4b551011017555b9a2d79b4d68d5750b6861208bc7c5b1f1bd9d863d99`.
- Actualización aislada desde el instalador 1.5.0 publicado: código 0, sin reinicio, ambos ensamblados en 1.5.1.0, carpeta `ui` heredada eliminada y contenido final idéntico a la publicación. La prueba detectó además una copia DAC versionada del runtime anterior; `[InstallDelete]` la retira antes de copiar la vigente.

Pendientes declarados en la nota de la versión: exclusión por firma HID y agrupación de identidades sin validar con hardware físico, y portabilidad de exportar/importar entre dos equipos sin certificar. La matriz manual completa del instalador en cuentas estándar de Windows 10 y Windows 11 sigue abierta en el carril C.

### Etapa 11 — Guardia de cambios sin guardar (1.5.2) · completada el 19 de julio de 2026

El defecto de mayor impacto: hoy se pierde trabajo en silencio. `DeviceList_SelectionChanged` reescribe el editor con la fila nueva sin comprobar nada, y no existe seguimiento de estado sucio en la ventana.

Resultado:

- El editor compara alias, distribución y estado ignorado con la fila aceptada. Volver exactamente a los tres valores persistidos lo deja limpio, y las cargas protegidas por `applyingEditorState` no generan cambios pendientes.
- La selección WinUI restaura primero la fila anterior mientras muestra la confirmación modal existente. Cancelar conserva tanto la fila como los valores editados; descartar aplica la fila solicitada sin reentradas ni bucles de selección.
- El cierre de WinUI se cancela antes de destruir la ventana y solo se reintenta tras confirmar el descarte. Guardar recarga el estado persistido y evita confirmaciones posteriores.
- El respaldo WinForms tenía el mismo defecto y quedó protegido con el mismo rastreador comprobable, su confirmación nativa y restauración explícita del botón de selección anterior.
- La versión avanzó conjuntamente a `1.5.2` en los dos proyectos y `[Sin publicar]` describe el impacto para el usuario.

Evidencia local:

- `dotnet test RightKeyboard.sln -c Release`: 170/170 pruebas superadas, sin omitidas (161 existentes y 9 nuevas para estado limpio, los tres campos, restauración, carga programática y ambas decisiones).
- `dotnet build RightKeyboard.sln -c Release --no-restore`: compilación correcta, 0 advertencias y 0 errores.
- Interfaz WinUI real: versión 1.5.2 visible; cierre limpio sin diálogo; alias modificado con confirmación; cancelar conservó `Alias temporal etapa 11`; descartar permitió cerrar; guardar un alias temporal, restaurar `Teclado sin nombre` y cerrar no produjo una confirmación posterior.
- La VM solo tenía una fila conocida, por lo que no permitió ejecutar visualmente el cambio entre dos filas. La decisión y la restauración están cubiertas por la lógica automatizada y por la implementación inspeccionada, pero esa interacción concreta queda declarada sin validación visual en esta VM.
- `git diff --check`: sin errores.

### Etapa 12 — Coherencia de Ignorar y Agrupar (1.5.3)

La dependencia ya existe y es unidireccional: ignorar bloquea agrupar como origen (`SetEditorEnabled`) y como destino (`CanBeGroupTarget`). Falta que la interfaz lo respete en vivo y que el orden lo comunique.

- `IgnoredCheckBox_Changed` solo reevalúa la distribución; debe reevaluar el estado completo del editor para que los controles de agrupación se deshabiliten sin reseleccionar la fila.
- Situar Ignorar por encima de Distribución, ya que gobierna tanto a Distribución como a Agrupar.
- Añadir pruebas que fijen la regla «un dispositivo ignorado no agrupa» en ambos sentidos.

### Etapa 13 — Nomenclatura y estado de fila (1.5.4)

- Unificar hacia «detectados» el encabezado de la lista, en la ventana WinUI y en el diálogo WinForms. El subtítulo ya dice «Administra los teclados detectados» desde beta 6; el encabezado quedó sin actualizar.
- Hacer que WinUI trate «Ignorado» como eje independiente de Conectado/Desconectado, siguiendo la semántica ya establecida en `DevicePresentation`. Hoy lo sustituye, de modo que no se puede saber si un dispositivo ignorado está conectado.

### Etapa 14 — Pulido visual del panel (1.5.5)

Va después de la 12 y la 13 porque las tres editan la misma zona.

- Sustituir el texto del botón Recargar por un icono del sistema, conservando nombre accesible y descripción emergente.
- Redondear las casillas sobrescribiendo el recurso de tema `CheckBoxCornerRadius`. La propiedad `CornerRadius` del control no afecta al glifo: los `CheckBox` son los únicos controles que hoy no fijan radio, frente al `CornerRadius(8)` del resto.

### Etapa 15 — Exploración de pulsaciones sintéticas (1.5.6)

Puede cerrarse sin cambios de código; el entregable mínimo es la conclusión documentada.

- `API.TryReadKeyboardEvent` ya descarta `Header.Device == 0`, el caso habitual de `SendInput`, de modo que el portapapeles de Windows no debería generar dispositivos fantasma.
- `RawKeyboardEvent.HasExtraInformation` se captura y solo se registra en diagnóstico; no filtra nada. Decidir si debe filtrar.
- Verificar teclados virtuales que sí se enumeran como HID real: escritorio remoto, automatización y teclado en pantalla.

### Etapa 16 — Fallback verificable ante caída del frontend (1.5.7)

P1 registrado en `.agent-context/03-problemas-conocidos.md`: si WinUI crea proceso y luego lanza una excepción, el residente no reabre el fallback WinForms, y la documentación de arquitectura promete más recuperación de la que el código implementa. Diseñar una señal de «lista/interactiva» o un código de salida distinguible, y alinear la documentación con lo que realmente ocurra.

### Etapa 17 — Instrumentar el foco del selector (1.5.8)

P1 registrado en `.agent-context/03-problemas-conocidos.md`: el foco depende de heurísticas Win32 y la ventana puede quedar delante sin que el cuadro de texto tenga foco. Instrumentar tiempos y resultado de la secuencia foreground y probar con varias aplicaciones antes de tocar el orden. No convertir la ventana en topmost permanente.

### Etapa 18 — Contratos compartidos y cierre de 1.6.0

Refactor diferido a 1.6: mover los DTO de IPC, `VersionPresentation` y modelos compartidos a una biblioteca para que el frontend WinUI no referencie el ejecutable WinForms. Cierra la versión.

## Carril B — Licencia · resuelto el 19 de julio de 2026

Existe `LICENSE` en la raíz, con tres capas declaradas por separado: la obra original de 2007 de Antoine Aubry bajo CPOL 1.02, los forks intermedios sin licencia declarada, y los cambios de este fork bajo MIT. La procedencia está trazada en `docs/procedencia-y-licencia.md` y la atribución restaurada en el README.

Esto desbloquea la etapa 10, con una restricción que hay que respetar: por CPOL 5(d) la obra no puede venderse, arrendarse ni alquilarse por sí sola. La publicación gratuita del instalador sí es compatible.

Queda un punto abierto de cumplimiento: CPOL 3(c) pide una nota en cada archivo modificado indicando cómo, cuándo y dónde se cambió. Hoy esa trazabilidad solo está en el historial de Git. Decidir si se añaden las notas a los archivos que descienden del volcado original o si se documenta el historial como mecanismo equivalente.

## Carril C — Validación física

Requiere hardware y cuentas reales; la VM de desarrollo no puede producir esta evidencia.

- Matriz FIS-01 a FIS-16 de `docs/calidad-1.5.md`, congelada en resultados de beta 1.
- Instalación en cuenta estándar de Windows 10 y 11; es un riesgo declarado bloqueante.
- DPI 125 % y 150 %, DPI mixto, dos monitores y texto ampliado.
- Dos teclados simultáneos y cambio de puerto USB, para cerrar agrupación de identidades y recuperación por firma.
- Dispositivos reales pendientes: presentador Baseus (`VID=2571`, `PID=4104`) y Logitech MX Master 3S por sus colecciones HID, no por nombre.
- Las 17 filas de `installer/PRUEBAS.md` son manuales y ninguna registra resultado.

## Carril D — Mediciones

Protocolo definido en `docs/criterios-winui3-1.5.md` y nunca ejecutado: diez aperturas frías y calientes, mediana y P95, retorno de memoria al cerrar y veinte ciclos sin crecimiento sostenido. Criterio objetivo: mediana ≤ 1500 ms en frío y ≤ 750 ms en caliente, P95 ≤ 2500 ms. Requiere host físico.

## Carril E — Cobertura automatizada

Nueve huecos en `.agent-context/05-siguientes-pasos.md` y los casos AUT-06, AUT-09, AUT-12, AUT-13 y AUT-14 de `docs/calidad-1.5.md`. Destacan la migración real de `config.txt` de 1.4, la recuperación de ignorado por huella, el IPC extremo a extremo con solicitud dañada y `StartupManager` con abstracción de registro. Puede avanzar en paralelo al carril A.

## Verificación por etapa

1. `dotnet test RightKeyboard.sln -c Release`. Son 161 pruebas a fecha de este plan; toda etapa de código debe mantenerlas en verde y las etapas 12 y 13 deben añadir las suyas.
2. Ejecutar la aplicación y comprobar el cambio en la interfaz real. Cerrar antes cualquier instancia en ejecución: el mutex de instancia única hace que un segundo proceso salga de inmediato y produzca un falso negativo.
3. Para cambios que afecten al instalador, ejecutar `scripts\build-installer.ps1` y comprobar tamaño y contenido de la publicación.
4. Rama, PR, CI en verde y merge con squash.
5. Actualizar el estado de la etapa en este archivo y la entrada correspondiente en `CHANGELOG.md`.
