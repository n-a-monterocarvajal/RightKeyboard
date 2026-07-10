# Estado actual real

Snapshot actualizado el **2026-07-09** para promover la rama `codex/version-1.5` a proyecto `1.5.0`. Contrastar siempre con `git status`, `git log -1 --oneline` y los metadatos de versión del checkout actual.

## Resumen ejecutivo

`1.5.0` es la versión estable inicial de la línea 1.5, aprobada por validación física acumulada en VM y equipos host. El camino instalado normal usa un residente WinForms/Win32 (`RightKeyboard.exe`) y un frontend WinUI bajo demanda (`ui/RightKeyboard.WinUI.exe`). El fallback WinForms conserva funciones que todavía no se migraron a WinUI. La próxima etapa no es otra reescritura: es mantenimiento 1.5.x, robustecer diagnóstico, mejorar detección preventiva de periféricos HID ambiguos y evaluar agrupación manual de identidades del mismo dispositivo.

## Funciona en el código actual

- Instancia única y cierre coordinado mediante mutex/evento local en `RightKeyboard/Program.cs`.
- Captura de teclados con Raw Input en una ventana de mensajes, sin formulario principal oculto (`RawInputWindow`).
- Lectura completa de `RAWKEYBOARD`; liberaciones, modificadores y teclas falsas no inician asignación (`RawKeyboardEvent`).
- Cambio dirigido a la ventana activa mediante `WM_INPUTLANGCHANGEREQUEST`; no se modifica globalmente el idioma (`Win32/API.cs`).
- Identidad priorizando `ContainerId`, luego hash de `InstanceId`, luego hash de ruta; huella separada para recuperación conservadora (`DeviceIdentityResolver`).
- Recuperación de asociación/ignorado por huella solo cuando hay una coincidencia inequívoca (`Configuration.TryGetLayout` e `IsIgnored`).
- Persistencia esquema 3, migración de esquema 2 y de `config.txt`, validación estricta y escritura temporal (`Configuration`).
- Selector WinUI con alias, agrupación visual por idioma, distribución, ignorado y fallback WinForms.
- Configuración WinUI para listar, ordenar, renombrar, cambiar distribución, ignorar, olvidar y limpiar.
- Seguimiento del teclado pulsado con Configuración abierta; mientras el alias tiene foco no cambia la selección.
- Menú nativo de bandeja limitado a **Configuración**, separador y **Salir** (`NativeTrayMenu`).
- Detección conservadora de no-teclados por nombre y de la firma sintética observada al usar el historial del portapapeles.
- Diagnóstico local, opcional, circular y asíncrono; queda como herramienta de mantenimiento, no como flujo principal de usuario.
- Instalador Inno Setup por usuario, autocontenido, sin UAC, acceso en Inicio, inicio automático en instalación nueva y conservación de datos al actualizar.
- Frontend publicado ReadyToRun; el snapshot IPC ya no repite SetupAPI en cada apertura.

## Funciona solo en el fallback WinForms, no en la UI instalada normal

`SettingsDialog` contiene **Exportar**, **Importar** y **Iniciar RightKeyboard con Windows**. `SettingsWindow` (WinUI, camino normal cuando existe `ui/RightKeyboard.WinUI.exe`) no contiene esas acciones ni existen comandos IPC para ellas. Por tanto, el README y varias pruebas visuales sobrestiman la Configuración actual. No se debe afirmar que estas funciones están accesibles al usuario normal hasta migrarlas al IPC/WinUI.

El fallback se abre únicamente si el frontend WinUI no existe o no puede iniciarse. No es una segunda interfaz seleccionable por el usuario.

## Parcial, no verificado o pendiente

- La optimización de apertura (inventario residente + ReadyToRun) está implementada; faltan mediciones frías/calientes reproducibles y percentiles según `docs/criterios-winui3-1.5.md`.
- Exportación/importación existen en el modelo y fallback, pero no en WinUI; la portabilidad entre equipos no está certificada.
- Inicio automático funciona en instalador y fallback, pero no se administra desde WinUI.
- Reconexión/cambio de puerto funciona cuando Windows mantiene identidad o la huella es única; dos teclados idénticos siguen siendo deliberadamente ambiguos.
- Accesibilidad, DPI mixto, contraste alto, Windows 10, sesión remota, suspensión y ciclos prolongados carecen de evidencia completa en el repo.
- No hay CI ni `TreatWarningsAsErrors`; la validación es local/manual.
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

En este snapshot hay **98 pruebas NUnit**. Cubren serialización/configuración, identidad y huella, clasificación básica, señales Raw Input, tamaños de interop, presentación WinForms, logger, IPC DTO, menú y versión. No cubren extremo a extremo el pipe, UI WinUI, foco/foreground, SetupAPI real, registro de inicio, instalador ni hardware.
