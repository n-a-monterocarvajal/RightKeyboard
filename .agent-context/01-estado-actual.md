# Estado actual real

Snapshot actualizado el **2026-07-19** durante la Etapa 6. Contrastar siempre con `git status`, `git log -1 --oneline` y los metadatos de versión del checkout actual.

## Resumen ejecutivo

`1.5.0` es la versión estable inicial de la línea 1.5. Las etapas 1-6 posteriores añadieron CI estricto, paridad de exportación/importación/inicio en WinUI, diagnóstico explicable, exclusión conservadora por firma HID y agrupación manual de identidades. El camino instalado normal usa un residente WinForms/Win32 (`RightKeyboard.exe`) y un frontend WinUI bajo demanda (`ui/RightKeyboard.WinUI.exe`).

## Funciona en el código actual

- Instancia única y cierre coordinado mediante mutex/evento local en `RightKeyboard/Program.cs`.
- Captura de teclados con Raw Input en una ventana de mensajes, sin formulario principal oculto (`RawInputWindow`).
- Lectura completa de `RAWKEYBOARD`; liberaciones, modificadores y teclas falsas no inician asignación (`RawKeyboardEvent`).
- Cambio dirigido a la ventana activa mediante `WM_INPUTLANGCHANGEREQUEST`; no se modifica globalmente el idioma (`Win32/API.cs`).
- Identidad priorizando `ContainerId`, luego hash de `InstanceId`, luego hash de ruta; huella separada para recuperación conservadora (`DeviceIdentityResolver`).
- Recuperación de asociación/ignorado por huella solo cuando hay una coincidencia inequívoca (`Configuration.TryGetLayout` e `IsIgnored`).
- Persistencia esquema 5, migraciones de esquemas 2-4 y de `config.txt`, validación estricta y escritura temporal (`Configuration`).
- Selector WinUI con alias, agrupación visual por idioma, distribución, ignorado y fallback WinForms.
- Configuración WinUI para listar, ordenar, renombrar, cambiar distribución, ignorar, olvidar y limpiar.
- Seguimiento del teclado pulsado con Configuración abierta; mientras el alias tiene foco no cambia la selección.
- Menú nativo de bandeja limitado a **Configuración**, separador y **Salir** (`NativeTrayMenu`).
- Detección conservadora de no-teclados por nombre y de la firma sintética observada al usar el historial del portapapeles.
- Exclusión por firma HID parcial (Etapa 5): ignorar manualmente un dispositivo con huella vacía registra su firma (`HidSignature`); reconectarlo con otra identidad no reabre el selector si la coincidencia es inequívoca. Esquema de preferencias 4 (`ignoredSignatures`); el 3 migra al guardar.
- Agrupación manual de identidades (Etapa 6): esquema 5 y protocolo IPC v2; un grupo lógico gobierna alias/layout, conserva visibles sus miembros técnicos y al separarlos reaparecen sus preferencias individuales latentes. La recuperación por huella nunca crea membresía.
- Diagnóstico detallado fuera del build normal; el código del logger queda disponible para compilaciones de desarrollo con `RIGHTKEYBOARD_DIAGNOSTICS`.
- Instalador Inno Setup por usuario, autocontenido, sin UAC, acceso en Inicio, inicio automático en instalación nueva y conservación de datos al actualizar.
- Frontend publicado ReadyToRun; el snapshot IPC ya no repite SetupAPI en cada apertura.

## Parcial, no verificado o pendiente

- La optimización de apertura (inventario residente + ReadyToRun) está implementada; faltan mediciones frías/calientes reproducibles y percentiles según `docs/criterios-winui3-1.5.md`.
- Exportación/importación están disponibles en WinUI, pero la portabilidad entre equipos no está certificada.
- La agrupación requiere validación física con dos teclados y cambio de puerto en la estación real; la VM no expone passthrough directo.
- Reconexión/cambio de puerto funciona cuando Windows mantiene identidad o la huella es única; dos teclados idénticos siguen siendo deliberadamente ambiguos.
- Accesibilidad, DPI mixto, contraste alto, Windows 10, sesión remota, suspensión y ciclos prolongados carecen de evidencia completa en el repo.
- Hay CI Windows y `TreatWarningsAsErrors`; las pruebas físicas siguen siendo manuales.
- No existe `LICENSE`; distribuir o aceptar contribuciones externas mantiene riesgo legal.
- No hay archivos de issues versionados ni configuración `.github`; el backlog utilizable está repartido entre documentos y esta carpeta.

## No es placeholder ni mock

No hay servicios falsos en producción. `RightKeyboard.WinUI` usa IPC real, `Configuration` persiste en disco y las APIs Win32 operan contra Windows. Los dobles aparecen solo en pruebas mediante funciones inyectadas o archivos temporales.

## Documentos históricos que no deben tomarse como estado

- `docs/continuacion-1.5.md` describe etapas anteriores (incluye mantener WinForms y no adoptar WinUI) ya superadas.
- `docs/calidad-1.5.md` conserva conteos y riesgos de beta 1; sirve como matriz, no como snapshot.
- `docs/pruebas-visuales-interfaz-1.5.md` mezcla baseline WinForms y controles que no existen en WinUI.
- `docs/arquitectura-fluent-1.5.md` es historia de la ruta WinForms; la arquitectura vigente está en `docs/arquitectura-winui-1.5.md` más el código actual.

## Evidencia automatizada

En este snapshot había **98 pruebas NUnit**; tras las etapas 1-6 hay **161**. Las nuevas pruebas de Etapa 6 cubren esquema 5, round-trip de grupos, preferencia lógica, separación reversible, rechazo de ignorados, ausencia de membresía automática y contrato IPC v2. No cubren extremo a extremo el pipe, UI WinUI, foco/foreground, SetupAPI real, registro de inicio, instalador ni hardware.
