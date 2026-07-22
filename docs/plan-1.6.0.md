# Plan de ejecución camino a 1.6.0

Redactado el 19 de julio de 2026, tras cerrar las etapas 1 a 8 del plan inicial.

Este documento es el orden de ejecución y se actualiza al cerrar cada etapa. `ROADMAP.md` sigue siendo el backlog temático: describe **qué** falta; este archivo describe **en qué orden** se aborda y **en qué estado** está.

## Situación de partida

Las etapas 1 a 8 están cerradas y su resultado sigue sin publicar: exclusión por firma HID, agrupación de identidades, esquema 5 de preferencias, rediseño de la Configuración WinUI e instalador aligerado con Inno Setup 7. Ese conjunto es la versión `1.5.1`.

La revisión del ejecutable del 19 de julio de 2026 dejó observaciones de interfaz registradas en `ROADMAP.md`, y un barrido del repositorio mostró que el trabajo pendiente es mayor de lo que ese archivo reflejaba: riesgo legal sin resolver, documentación que contradice al código, dos fragilidades P1 de arquitectura y validación física congelada en beta 1.

## Mecánica de versión

La versión se declara en dos sitios que deben moverse juntos: `RightKeyboard/RightKeyboard.csproj` y `RightKeyboard.WinUI/RightKeyboard.WinUI.csproj`. `scripts/build-installer.ps1` la lee del primero cuando no se pasa `-Version`. Cada etapa de mantenimiento cierra con su bump, su entrada en `CHANGELOG.md`, una etiqueta `vX.Y.Z` —o `vX.Y.Z.W` para una corrección puntual— y una Release de GitHub con instalador y SHA-256 después del squash y del CI en verde.

## Advertencia sobre la publicación

El bloqueo por licencia se levantó el 19 de julio de 2026: existe `LICENSE` y la procedencia está trazada, según el carril B. Queda la restricción de CPOL 5(d) —no vender, arrendar ni alquilar la obra por sí sola— con la que la publicación gratuita del instalador es compatible. `1.5.1` y las versiones de mantenimiento posteriores se publican gratuitamente también como registro histórico.

## Carril A — etapas de código

Secuenciales. Cada una es una sesión, una rama, un PR y un bump de versión.

| Etapa | Versión | Asunto | Estado |
|---|---|---|---|
| 9 | — | Sincronizar documentación con el código | Completada |
| 10 | 1.5.1 | Publicar lo acumulado | Completada |
| 11 | 1.5.2 | Guardia de cambios sin guardar | Completada |
| 12 | 1.5.3 | Coherencia de Ignorar y Agrupar | Completada |
| 13 | 1.5.4 | Nomenclatura y estado de fila | Completada |
| 14 | 1.5.5 / 1.5.5.1 | Pulido visual del panel | Completada con corrección |
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
- Publicada posteriormente como `v1.5.2` sobre el commit exacto `2f7b93b`, con instalador de 65.786.552 bytes y SHA-256 `73749883eb59ddba9810ca7f66a6aa92c2c52f4dec366c314c8c0601ca158c28`.

### Etapa 12 — Coherencia de Ignorar y Agrupar (1.5.3) · completada el 19 de julio de 2026

Resultado:

- Una evaluación compartida y comprobable distingue dispositivo, grupo lógico y miembro técnico, y calcula la disponibilidad de alias, distribución, Ignorar, Guardar, Olvidar y todas las acciones de agrupación desde el valor actual del editor.
- Marcar «Ignorar» deshabilita inmediatamente Distribución, el selector de destino y el botón Agrupar, y limpia cualquier destino seleccionado que deja de ser válido. Desmarcarlo recupera el selector solo para filas que pueden agrupar y mantiene el botón deshabilitado mientras falte un destino.
- Un dispositivo ignorado no puede ser origen ni destino. La interfaz y la guardia defensiva de `Configuration.GroupDevices` fijan la misma regla sin cambiar la persistencia.
- Las cargas programáticas protegidas no aplican estados transitorios ni ensucian el editor. La guardia de cambios sin guardar sigue comparando alias, distribución e Ignorar con el estado persistido.
- «Ignorar» aparece antes de «Distribución» en WinUI y en el respaldo WinForms, con ayuda accesible que explica que también gobierna la agrupación.
- Ambos proyectos avanzaron conjuntamente a `1.5.3`.

Evidencia local:

- `dotnet test RightKeyboard.sln -c Release`: 176/176 pruebas superadas, sin omitidas; son las 170 anteriores y 6 nuevas para origen, destino, limpieza de selección, recuperación condicionada y carga programática.
- `dotnet build RightKeyboard.sln -c Release --no-restore`: compilación correcta, 0 advertencias y 0 errores.
- Interfaz WinUI real: versión 1.5.3 visible y orden Ignorar, Distribución, Agrupar confirmado. Marcar Ignorar deshabilitó inmediatamente ambos selectores y Agrupar; desmarcarlo recuperó los selectores y mantuvo Agrupar deshabilitado por falta de destino.
- La confirmación de cierre detectó el cambio de Ignorar; cancelar conservó la casilla marcada y todos los estados dependientes. Guardar un alias temporal, restaurar el original y cerrar no produjo una confirmación posterior.
- La VM solo tenía una fila conocida. No fue posible comprobar visualmente una selección real de destino ni cancelar el cambio entre dos filas; ambas interacciones quedan cubiertas por las pruebas y la lógica compartida, pero no se dan por validadas en la interfaz de esta VM.
- `git diff --check`: sin errores.

### Etapa 13 — Nomenclatura y estado de fila (1.5.4) · completada el 20 de julio de 2026

Resultado:

- El encabezado y el nombre accesible de la lista dicen «Dispositivos detectados» en Configuración WinUI y en el respaldo WinForms. Se conserva «recordados» donde describe el inventario persistido y no la detección.
- `DevicePresentation` compone conexión, Ignorado y distribución como ejes independientes. WinUI reutiliza esa regla en `DeviceRow` y WinForms conserva la misma salida, sin cambiar persistencia ni ordenamiento.
- Una fila ignorada muestra si está conectada o desconectada y conserva la distribución asignada. El nombre accesible anuncia los mismos componentes que el resumen visual.
- Ambos proyectos avanzaron conjuntamente a `1.5.4` y el registro de cambios describe el impacto para el usuario.

Evidencia local:

- `dotnet test RightKeyboard.sln -c Release`: 182/182 pruebas superadas, sin omitidas; son las 176 anteriores y 6 casos nuevos para las cuatro combinaciones de conexión e Ignorado, distribución y nombre accesible.
- `dotnet build RightKeyboard.sln -c Release --no-restore`: compilación correcta, 0 advertencias y 0 errores.
- Interfaz WinUI real: versión 1.5.4, encabezado y lista accesible «Dispositivos detectados», y fila `Conectado · Sin distribución`. Tras guardar Ignorar, la misma fila mostró `Conectado · Ignorado` y su nombre accesible anunció ambos estados.
- Marcar Ignorar deshabilitó inmediatamente Distribución y Agrupar. La confirmación de cierre detectó el cambio; cancelar conservó la edición, guardar limpió el estado sucio y la preferencia original quedó restaurada al terminar.
- La VM solo tenía una fila conectada y sin distribución asignada. No fue posible validar visualmente una fila desconectada ni una combinación real con distribución; ambas quedan cubiertas por la regla compartida y las pruebas automatizadas, pero no se dan por validadas visualmente.
- El respaldo WinForms no se abrió visualmente: en esta disposición Release solo se alcanza como fallback desde el menú de bandeja. Su encabezado y filas consumen, respectivamente, el texto actualizado y la misma `DevicePresentation` comprobada.
- Publicación previa al squash: 590 archivos, 271.316.185 bytes, ambos ejecutables con versión de archivo 1.5.4.0; instalador y SHA-256 completos y coincidentes. Se reconstruirán desde el commit fusionado antes de publicar la Release.
- `git diff --check`: sin errores.

### Etapa 14 — Pulido visual del panel (1.5.5) · completada el 21 de julio de 2026

Resultado:

- «Recargar» conserva ese nombre accesible y la ayuda «Recarga la lista de dispositivos detectados.», pero muestra `Symbol.Refresh` en un botón de 36 × 36 píxeles lógicos. El glifo y los estados visuales proceden de WinUI y heredan el tema; el identificador estable es `ReloadDevicesButton`.
- La raíz de Configuración sobrescribe localmente `CheckBoxCornerRadius` con radio 4, proporcionado al radio 8 de los controles del panel. El recurso alcanza Ignorar, Iniciar con Windows y Diagnóstico cuando está compilado, sin depender de `CornerRadius` en cada `CheckBox` ni alterar el selector.
- El respaldo WinForms conserva sus casillas nativas y no tiene un botón Recargar visible equivalente. La inspección de `SettingsDialog` y `FluentWindowStyler` no mostró un cambio paralelo necesario ni una incoherencia nueva.
- Ambos proyectos avanzaron conjuntamente a `1.5.5`; no se modificaron persistencia, agrupación, ordenamiento, semántica de filas ni la guardia de cambios sin guardar.

Evidencia local:

- `dotnet test RightKeyboard.sln -c Release`: 185/185 pruebas superadas, sin omitidas; son las 182 anteriores y 3 nuevas para el contrato visual de nombre, ayuda y proporción de radios.
- `dotnet build RightKeyboard.sln -c Release --no-restore`: compilación correcta, 0 advertencias y 0 errores.
- Interfaz WinUI real en esta VM, en tema claro y oscuro: versión 1.5.5 visible, icono alineado en el encabezado, glifo de Ignorar redondeado en estados marcado y desmarcado, y casilla Iniciar con Windows coherente. Diagnóstico no estaba disponible en esta compilación y no se da por validado visualmente. La VM tampoco aporta evidencia válida de materiales, DPI mixto, varios monitores ni hardware físico.
- UI Automation anunció «Recargar», la descripción completa y `ReloadDevicesButton`. Activar el botón recargó la lista; se observaron los estados normal, hover y foco. La herramienta no permite congelar el instante pulsado, y deshabilitado no aplica porque Recargar permanece habilitado; esos estados no se dan por validados de forma independiente. El tooltip comparte la descripción comprobada por UI Automation, pero su ventana emergente no llegó a quedar visible en la captura y no se da por validada visualmente.
- Marcar Ignorar siguió deshabilitando inmediatamente Distribución, el destino y Agrupar; desmarcarlo restauró el estado anterior sin persistir cambios. Las filas y sus nombres accesibles conservaron conexión, Ignorado y distribución.
- La alineación quedó comprobada a 1080 × 720. Los intentos de arrastre automatizado no llevaron la ventana a 900 × 640, por lo que el tamaño mínimo exacto no se da por validado visualmente en esta sesión.
- Publicación previa al squash: 590 archivos, 271.324.377 bytes, ambos ejecutables con versión de archivo 1.5.5.0; instalador de 65.797.947 bytes y SHA-256 `fc5440c6a9b439d5028ed6b27d4b2afbe0148049e5e8d7a65d3d0321abc288cf`, coincidente con su archivo. Se reconstruirá desde el commit fusionado antes de publicar la Release.
- `git diff --check`: sin errores.

#### Corrección 1.5.5.1 · 22 de julio de 2026

Una revisión posterior en una estación física invalidó dos conclusiones visuales de la VM: las casillas seguían mostrando el glifo con ángulos rectos y el icono de Recargar aparecía recortado y destacado como un botón convencional. `v1.5.5` permanece inmutable como registro de lo publicado; la corrección se entrega como `v1.5.5.1`.

Resultado:

- El template incluido con WinUI 2.2 no consulta `CheckBoxCornerRadius`: inicializa la propiedad desde `ControlCornerRadius` y enlaza esa propiedad con el rectángulo del glifo. Las tres casillas reciben ahora ese recurso en su propio ámbito y fijan el mismo radio 4 en la propiedad consumida por el template.
- `ApplyFluentResources` reescribía el padding cero del botón Recargar con 14 píxeles por lado, dejando solo 8 píxeles útiles y recortando el glifo. Recargar queda fuera de ese lote genérico y usa el glifo Fluent `E72C` de 16 píxeles dentro de un objetivo de 32 × 32, proporcionado al título «Dispositivos detectados».
- Fondo y borde son transparentes en reposo. WinUI conserva el resaltado sutil en hover, pulsado y foco, además del nombre accesible, la ayuda y el comportamiento de recarga.
- Ambos proyectos avanzaron juntos a `1.5.5.1`; el instalador admite ahora versiones de tres o cuatro componentes. La etapa 15 conserva `1.5.6`.

Evidencia local:

- `dotnet test RightKeyboard.sln -c Release`: 187/187 pruebas superadas, sin omitidas; son las 185 anteriores y dos casos nuevos para proporción visual y presentación de la versión de cuatro componentes.
- `dotnet build RightKeyboard.sln -c Release --no-restore`: compilación correcta, 0 advertencias y 0 errores.
- Interfaz WinUI real en esta VM, en temas claro y oscuro: versión 1.5.5.1 visible; icono completo, centrado y transparente en reposo; respuesta sutil al activarlo; UI Automation conserva «Recargar», su descripción y `ReloadDevicesButton`. Ignorar mostró esquinas redondeadas marcado y desmarcado y siguió gobernando Distribución y Agrupar en vivo.
- Diagnóstico no estaba disponible en esta compilación. Su casilla consume el mismo helper, pero no se da por validada visualmente. La corrección tampoco se da por revalidada todavía en una estación física; allí debe repetirse la comprobación que detectó el defecto de 1.5.5.
- Cierre publicado: el PR #26 superó «Build y pruebas (Windows)» y se fusionó mediante squash como `604aeb6dd4e7706f8e79d244db819451e07f79c6`. Desde ese commit se reconstruyeron 590 archivos (271.324.391 bytes), con ambos ejecutables en versión de archivo 1.5.5.1. La etiqueta anotada `v1.5.5.1` resuelve exactamente al squash y la Release estable contiene `RightKeyboard-1.5.5.1-Setup.exe` (65.797.849 bytes) y `RightKeyboard-1.5.5.1-SHA256.txt` (98 bytes). Una descarga posterior independiente confirmó el SHA-256 `26a9a091bf30d97e73ad3cd5b9268c80369a1cb634b086b9c36476b1dad91557` y la coincidencia del archivo de suma.
- `git diff --check`: sin errores.

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
5. Etiqueta de versión y Release de GitHub con instalador y SHA-256 verificados; no mover una etiqueta ya publicada.
6. Actualizar el estado de la etapa en este archivo y la entrada correspondiente en `CHANGELOG.md`.
