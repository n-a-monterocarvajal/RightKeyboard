# Estado actual real

Snapshot actualizado el **2026-08-03** tras corregir el empaquetado de 1.6.0. Contrastar siempre con `git status`, `git log -1 --oneline` y los metadatos de versión del checkout actual.

## Resumen ejecutivo

`1.5.0` es la última versión publicada; el código fuente cierra en `1.6.0`. El camino instalado normal usa un residente WinForms/Win32 (`RightKeyboard.exe`) y un frontend WinUI bajo demanda (`ui/RightKeyboard.WinUI.exe`), ambos apoyados en la biblioteca neutral `RightKeyboard.Shared.dll`.

## Funciona en el código actual

- Instancia única y cierre coordinado mediante mutex/evento local en `RightKeyboard/Program.cs`.
- Captura de teclados con Raw Input en una ventana de mensajes, sin formulario principal oculto (`RawInputWindow`).
- Lectura completa de `RAWKEYBOARD`; liberaciones, modificadores y teclas falsas no inician asignación (`RawKeyboardEvent`).
- Cambio dirigido a la ventana activa mediante `WM_INPUTLANGCHANGEREQUEST`; no se modifica globalmente el idioma (`Win32/API.cs`).
- Identidad priorizando `ContainerId`, luego hash de `InstanceId`, luego hash de ruta; huella separada para recuperación conservadora (`DeviceIdentityResolver`).
- Recuperación de asociación/ignorado por huella solo cuando hay una coincidencia inequívoca (`Configuration.TryGetLayout` e `IsIgnored`).
- Persistencia esquema 5, migraciones de esquemas 2-4 y de `config.txt`, validación estricta y escritura temporal (`Configuration`).
- Selector WinUI con alias, agrupación visual por idioma, distribución, ignorado y fallback WinForms.
- Configuración WinUI para listar, ordenar, renombrar, cambiar distribución, ignorar, olvidar y limpiar. Desde 1.5.10, conexión es la clave primaria del orden y cada fila muestra un indicador verde/gris junto al texto accesible de estado; el respaldo WinForms usa la misma semántica.
- Contratos compartidos de ambos procesos extraídos a `RightKeyboard.Shared`: IPC v2, versión, códigos de salida y modelos de Configuración. WinUI ya no referencia el ejecutable WinForms.
- Seguimiento del teclado pulsado con Configuración abierta; mientras el alias tiene foco no cambia la selección.
- Menú nativo de bandeja limitado a **Configuración**, separador y **Salir** (`NativeTrayMenu`).
- Detección conservadora de no-teclados por nombre y de la firma sintética observada al usar el historial del portapapeles.
- Exclusión por firma HID parcial (Etapa 5): ignorar manualmente un dispositivo con huella vacía registra su firma (`HidSignature`); reconectarlo con otra identidad no reabre el selector si la coincidencia es inequívoca. Esquema de preferencias 4 (`ignoredSignatures`); el 3 migra al guardar.
- Agrupación manual de identidades (Etapa 6): esquema 5 y protocolo IPC v2; un grupo lógico gobierna alias/layout, conserva visibles sus miembros técnicos y al separarlos reaparecen sus preferencias individuales latentes. La recuperación por huella nunca crea membresía.
- Configuración reordenada (Etapa 7): una tarjeta Preferencias reúne Exportar, Importar y Limpiar; Sistema conserva aparte el inicio con Windows y, solo en compilaciones diagnósticas, sus herramientas. La ventana parte de 1080 × 720 y exige un mínimo de 900 × 640 píxeles lógicos ajustados al DPI; el editor ocupa toda la columna derecha, fija Guardar/Olvidar al pie y deja que la lista de dispositivos absorba la reducción de altura mediante desplazamiento.
- Diagnóstico detallado fuera del build normal; el código del logger queda disponible para compilaciones de desarrollo con `RIGHTKEYBOARD_DIAGNOSTICS`.
- Instrumentación de foreground de la Etapa 17: el selector informa tiempos y resultados de activación nativa/foco XAML por IPC, y cada solicitud de distribución clasifica de forma segura el Escritorio frente a otras ventanas sin registrar títulos ni clases arbitrarias.
- Instalador Inno Setup por usuario, autocontenido, sin UAC, acceso en Inicio, inicio automático en instalación nueva y conservación de datos al actualizar.
- La publicación autocontenida copia y comprueba `App.xbf` y `RightKeyboard.WinUI.pri`; los scripts de instalador y portable fallan si el tema WinUI quedaría incompleto.
- Frontend publicado ReadyToRun; el snapshot IPC ya no repite SetupAPI en cada apertura.

## Parcial, no verificado o pendiente

- La optimización de apertura (inventario residente + ReadyToRun) está implementada; faltan mediciones frías/calientes reproducibles y percentiles según `docs/criterios-winui3-1.5.md`.
- Exportación/importación están disponibles en WinUI, pero la portabilidad entre equipos no está certificada.
- La agrupación requiere validación física con dos teclados y cambio de puerto en la estación real; la VM no expone passthrough directo.
- Reconexión/cambio de puerto funciona cuando Windows mantiene identidad o la huella es única; dos teclados idénticos siguen siendo deliberadamente ambiguos.
- La Etapa 7 verificó visualmente en esta VM las variantes normal y `RIGHTKEYBOARD_DIAGNOSTICS` a 100 %, incluidas la composición inicial y la mínima. Quedan pendientes 125 %, DPI mixto/dos monitores, texto ampliado y evidencia física equivalente porque la VM no permite demostrarlos con suficiente fiabilidad.
- Accesibilidad completa, contraste alto, Windows 10, sesión remota, suspensión y ciclos prolongados carecen de evidencia completa en el repo.
- La instrumentación de 1.5.8 está implementada; todavía falta correlacionar sus eventos en una estación física con varias aplicaciones, el Escritorio y teclados reales antes de alterar las heurísticas de foco o declarar un límite de Explorer/Shell.
- Hay CI Windows y `TreatWarningsAsErrors`; las pruebas físicas siguen siendo manuales. La automatización de GitHub Actions (CI, compilación distribuible y Dependabot) es parte del procedimiento normal de validación y mantenimiento; ver `docs/automatizacion-ci-cd.md` y `06-build-pruebas-y-mapa.md`.
- `LICENSE` existe desde el 19 de julio de 2026, con tres capas separadas y la atribución de origen declarada como no verificada. Queda abierto CPOL 3(c): decidir entre notas por archivo o documentar el historial de Git como equivalente. Ver `docs/procedencia-y-licencia.md`.
- Existen tres automatizaciones de GitHub Actions: `.github/workflows/ci.yml` (compila y prueba en Windows en cada push a `master`, cada pull request y manualmente), `.github/workflows/build-package.yml` (compilación distribuible manual o por etiqueta `v*`, produce el instalador + SHA-256 como artefacto de 7 días) y `.github/dependabot.yml` (revisión semanal agrupada de NuGet y GitHub Actions). Su funcionamiento y reglas para agentes están en `docs/automatizacion-ci-cd.md` y `06-build-pruebas-y-mapa.md`. No hay plantillas de issues ni issues versionados; el backlog utilizable está repartido entre documentos y esta carpeta.

## No es placeholder ni mock

No hay servicios falsos en producción. `RightKeyboard.WinUI` usa IPC real, `Configuration` persiste en disco y las APIs Win32 operan contra Windows. Los dobles aparecen solo en pruebas mediante funciones inyectadas o archivos temporales.

## Documentos históricos que no deben tomarse como estado

- `docs/continuacion-1.5.md` describe etapas anteriores (incluye mantener WinForms y no adoptar WinUI) ya superadas.
- `docs/calidad-1.5.md` conserva conteos y riesgos de beta 1; sirve como matriz, no como snapshot.
- `docs/pruebas-visuales-interfaz-1.5.md` mezcla baseline WinForms y controles que no existen en WinUI.
- `docs/arquitectura-fluent-1.5.md` es historia de la ruta WinForms; la arquitectura vigente está en `docs/arquitectura-winui-1.5.md` más el código actual.
- `docs/releases/` son notas de versiones ya publicadas. **No se editan para reflejar trabajo posterior**, ni siquiera para anotar que un pendiente se resolvió después: describen lo que se publicó en su fecha y esa es su utilidad. Si contradicen al código, la contradicción es aparente y se resuelve leyendo su fecha. El estado vigente vive en este archivo, en `ROADMAP.md` y en `docs/plan-1.6.0.md`; una corrección real de una versión publicada va en la nota de la versión siguiente.

## Evidencia automatizada

En el snapshot inicial había **98 pruebas NUnit**; tras la Etapa 20 hay **220 NUnit y 2 pruebas WinUI de árbol visual real**. Las nuevas pruebas fijan el límite del ensamblado compartido y la versión 1.6.0; las pruebas WinUI levantan una aplicación XAML, resuelven el template de `CheckBox` y verifican los indicadores de conexión y sus nombres accesibles. Es una cobertura UI deliberadamente estrecha: la suite aún no cubre extremo a extremo el pipe, ventanas completas, foco/foreground, SetupAPI real, instalador ni hardware.
