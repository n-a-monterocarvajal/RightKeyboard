# RightKeyboard

RightKeyboard asocia cada teclado físico conectado a Windows con una distribución de teclado distinta. Al escribir, la aplicación detecta qué dispositivo generó la pulsación y solicita a la ventana activa que use la distribución elegida para ese teclado.

La aplicación se ejecuta en segundo plano y su única interfaz permanente es el icono del área de notificación de Windows. El menú secundario contiene estas acciones:

- **Configuración**: administra nombres, distribuciones, dispositivos ignorados y limpieza de preferencias.
- **Salir**: detiene RightKeyboard.

## Requisitos

- Windows 10 o Windows 11 de 64 bits.
- El instalador oficial incluye .NET 10 y no requiere un runtime global.
- .NET 10 SDK para compilar el proyecto.

Las distribuciones que se quieran utilizar deben estar instaladas previamente en **Configuración > Hora e idioma > Idioma y región > Opciones de idioma > Teclados**. RightKeyboard no instala ni modifica la lista de distribuciones de Windows.

## Uso

1. Inicia `RightKeyboard.exe`.
2. Presiona una tecla normal en un teclado que todavía no tenga una preferencia.
3. Selecciona la distribución correspondiente y pulsa **Aceptar**.
4. Repite el proceso para los demás teclados.

Desde la versión 1.5, las preferencias se guardan en `%LOCALAPPDATA%\RightKeyboard\preferences.json` y sobreviven al reinicio de la aplicación. En una instalación habitual, la ruta completa tiene esta forma:

```text
C:\Users\<usuario>\AppData\Local\RightKeyboard\preferences.json
```

La opción **Limpiar preferencias** vacía las asociaciones y la lista de dispositivos ignorados. No modifica las distribuciones instaladas en Windows. Si existe un `config.txt` creado por la versión 1.4, RightKeyboard lo migra automáticamente al nuevo formato.

El instalador activa de forma predeterminada el inicio con Windows para el usuario actual, sin requerir permisos administrativos. Se puede cambiar posteriormente desde **Configuración** o desde las aplicaciones de inicio de Windows.

Cerrar el selector sin aceptar no crea una asociación. Las pulsaciones de modificadores, las liberaciones de tecla y los eventos de teclado sintéticos no abren el selector. El selector permite asignar un nombre al dispositivo, agrupa las distribuciones por idioma y permite ignorar periféricos que publican entradas de teclado sin ser teclados, como ciertos mouse con botones avanzados.

Desde **Configuración** se pueden editar preferencias sin repetir el flujo de detección: renombrar dispositivos, cambiar distribución, ignorar periféricos ambiguos, olvidar dispositivos y limpiar preferencias. La exportación/importación de configuración queda planificada para una actualización posterior de la línea 1.5.

> **Estado de la versión 1.5:** `1.5.0` es la versión estable inicial de la línea 1.5. La matriz de validación y los pendientes de mantenimiento siguen en [docs/calidad-1.5.md](docs/calidad-1.5.md) y [ROADMAP.md](ROADMAP.md).

El contrato del esquema, las validaciones, las rutas y el alcance exacto de **Limpiar preferencias** se documentan en [Preferencias y portabilidad de RightKeyboard 1.5](docs/preferencias-1.5.md).

## Compilación y pruebas

```powershell
dotnet restore RightKeyboard.sln
dotnet build RightKeyboard.sln --configuration Release
dotnet test RightKeyboard.sln --configuration Release
scripts\build-installer.ps1
```

La aplicación se genera en `RightKeyboard\bin\Release\net10.0-windows\`.
El último comando publica para `win-x64`, compila el instalador con Inno Setup 7.0 o posterior y genera su archivo SHA-256 bajo `artifacts\installer`. Si `ISCC.exe` no está en una ubicación conocida, se puede indicar mediante `ISCC_PATH` o el parámetro `-IsccPath`.

## Implementación

- Usa Raw Input (`WM_INPUT`) para distinguir el dispositivo físico sin instalar servicios, controladores ni hooks globales.
- Lee la estructura completa `RAWKEYBOARD` y actúa solo en eventos de pulsación.
- Agrupa las distintas funciones HID de un teclado mediante el `ContainerId` de Plug and Play. Esto evita pedir otra distribución cuando una combinación `Fn` se presenta como otra colección del mismo dispositivo.
- Conserva una huella de modelo como respaldo para recuperar asociaciones cuando Windows cambia el identificador de un dispositivo al reconectarlo.
- Actualiza el inventario al recibir notificaciones de conexión o desconexión de Raw Input.
- Clasifica de forma conservadora periféricos claramente no-teclado y permite ignorar manualmente los casos ambiguos.
- Solicita el cambio con `WM_INPUTLANGCHANGEREQUEST` únicamente a la ventana activa. No cambia el idioma predeterminado global ni difunde mensajes a todas las aplicaciones.
- No mantiene un formulario principal oculto: utiliza una ventana exclusiva para mensajes y un icono de notificación.

La opción de Windows **Permitir usar un método de entrada diferente para cada ventana de aplicación** puede hacer que Windows recuerde estados propios por ventana. RightKeyboard respeta ese modelo y vuelve a solicitar la distribución asociada cuando llega una pulsación desde cada teclado.

## Origen y estado legal

Este fork conserva el trabajo previo de los autores y colaboradores de RightKeyboard. El historial recibido no incluye un archivo `LICENSE`; antes de distribuir binarios o aceptar contribuciones externas conviene aclarar y documentar la licencia aplicable.

## Idioma del proyecto

La documentación, las notas de cambios, los mensajes visibles para el usuario y las descripciones de cambios nuevos se escriben en español. Consulta [CONTRIBUTING.md](CONTRIBUTING.md) antes de aportar cambios.

Las mejoras previstas para la siguiente versión se mantienen en [ROADMAP.md](ROADMAP.md).
