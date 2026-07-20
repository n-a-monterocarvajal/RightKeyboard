# Plan de ejecución camino a 1.6.0

Redactado el 19 de julio de 2026, tras cerrar las etapas 1 a 8 del plan inicial.

Este documento es el orden de ejecución y se actualiza al cerrar cada etapa. `ROADMAP.md` sigue siendo el backlog temático: describe **qué** falta; este archivo describe **en qué orden** se aborda y **en qué estado** está.

## Situación de partida

Las etapas 1 a 8 están cerradas y su resultado sigue sin publicar: exclusión por firma HID, agrupación de identidades, esquema 5 de preferencias, rediseño de la Configuración WinUI e instalador aligerado con Inno Setup 7. Ese conjunto es la versión `1.5.1`.

La revisión del ejecutable del 19 de julio de 2026 dejó observaciones de interfaz registradas en `ROADMAP.md`, y un barrido del repositorio mostró que el trabajo pendiente es mayor de lo que ese archivo reflejaba: riesgo legal sin resolver, documentación que contradice al código, dos fragilidades P1 de arquitectura y validación física congelada en beta 1.

## Mecánica de versión

La versión se declara en dos sitios que deben moverse juntos: `RightKeyboard/RightKeyboard.csproj` y `RightKeyboard.WinUI/RightKeyboard.WinUI.csproj`. `scripts/build-installer.ps1` la lee del primero cuando no se pasa `-Version`. Cada etapa cierra con su bump y su entrada en `CHANGELOG.md`.

## Advertencia sobre la publicación

Publicar binarios choca con el carril B: no existe `LICENSE` en el repositorio y el README lo declara públicamente. `1.5.0` ya se promovió como estable pese al criterio explícito de no hacerlo antes de resolver la procedencia. Antes de publicar `1.5.1` hay que decidir si es una publicación pública o solo un corte interno de versión.

## Carril A — etapas de código

Secuenciales. Cada una es una sesión, una rama, un PR y un bump de versión.

| Etapa | Versión | Asunto | Estado |
|---|---|---|---|
| 9 | — | Sincronizar documentación con el código | Pendiente |
| 10 | 1.5.1 | Publicar lo acumulado | Pendiente |
| 11 | 1.5.2 | Guardia de cambios sin guardar | Pendiente |
| 12 | 1.5.3 | Coherencia de Ignorar y Agrupar | Pendiente |
| 13 | 1.5.4 | Nomenclatura y estado de fila | Pendiente |
| 14 | 1.5.5 | Pulido visual del panel | Pendiente |
| 15 | 1.5.6 | Exploración de pulsaciones sintéticas | Pendiente |
| 16 | 1.5.7 | Fallback verificable ante caída del frontend | Pendiente |
| 17 | 1.5.8 | Instrumentar el foco del selector | Pendiente |
| 18 | 1.6.0 | Extraer contratos compartidos y cerrar versión | Pendiente |

### Etapa 9 — Sincronizar documentación con el código

Va primero porque `1.5.1` no debe publicarse con documentación que contradice al producto, empezando por el README público.

- Exportar/importar e «Iniciar con Windows» figuran como pendientes en seis o más lugares, incluido el README, pese a estar implementados.
- `ROADMAP.md` mantiene en «Alcance consolidado» la decisión revertida de conservar WinForms y no adoptar WinUI 3.
- AUT-15 sigue «Pendiente» en `docs/calidad-1.5.md` aunque CI y `TreatWarningsAsErrors` existen.
- Agrupar identidades y detección preventiva aparecen como backlog futuro estando implementadas.
- `.agent-context/01-estado-actual.md` afirma que no hay configuración `.github`.

### Etapa 10 — Publicar 1.5.1

Corte de versión de todo lo acumulado desde `1.5.0`, sin funcionalidad nueva.

- Bump a `1.5.1` en los dos `.csproj`.
- Cerrar la sección `[Sin publicar]` de `CHANGELOG.md` como `[1.5.1]` con fecha.
- Redactar `docs/releases/1.5.1.md` siguiendo la convención de `docs/releases/1.5.0.md`.
- Compilar el instalador y verificar tamaño, contenido y SHA-256.
- Ejecutar la prueba de actualización sobre una instalación previa, que es la que valida el `[InstallDelete]` de la carpeta heredada.

### Etapa 11 — Guardia de cambios sin guardar (1.5.2)

El defecto de mayor impacto: hoy se pierde trabajo en silencio. `DeviceList_SelectionChanged` reescribe el editor con la fila nueva sin comprobar nada, y no existe seguimiento de estado sucio en la ventana.

- Detectar estado sucio comparando el editor contra la fila seleccionada: alias, distribución e ignorado.
- Confirmar antes de perder cambios al cambiar de fila y al cerrar la ventana.
- Reutilizar el patrón de `ContentDialog` que ya existe en `SettingsWindow.xaml.cs`.
- Respetar el campo `applyingEditorState`, que ya distingue la carga programática de la edición del usuario: sin él, poblar el editor marcaría sucio de inmediato.
- Revisar si el diálogo WinForms de respaldo tiene el mismo agujero y decidir si entra aquí o se anota.

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
