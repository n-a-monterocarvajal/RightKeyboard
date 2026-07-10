# Distribución e instalación de RightKeyboard 1.5

Este documento define el modelo de distribución previsto para la versión 1.5. Complementa la especificación de interfaz y configuración de [continuacion-1.5.md](continuacion-1.5.md).

## Objetivos

- Publicar RightKeyboard como aplicación instalada, no como dos paquetes ZIP.
- No requerir permisos administrativos para la instalación normal.
- No exigir que el usuario instale manualmente .NET.
- Mantener binarios, preferencias y exportaciones bajo una sola raíz propiedad de RightKeyboard.
- Activar por defecto el inicio con Windows, con control tanto desde RightKeyboard como desde Windows.
- Dejar preparada la base para exportar, importar y respaldar preferencias en una actualización posterior de la línea 1.5.

## Instalador recomendado

La primera implementación se realizará con [Inno Setup](https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm) y `PrivilegesRequired=lowest`. El instalador incluirá una publicación autocontenida de .NET 10 para `win-x64`.

Esta combinación es la opción preferida porque:

- Inno Setup puede instalar en modo no administrativo y sin mostrar UAC;
- una [publicación autocontenida de .NET](https://learn.microsoft.com/dotnet/core/versions/selection) incluye su runtime y no depende de uno instalado globalmente;
- instalar el [.NET Desktop Runtime para todo el sistema](https://learn.microsoft.com/es-es/dotnet/core/install/windows) requiere normalmente permisos administrativos;
- evita que RightKeyboard descargue o modifique componentes globales durante la instalación;
- produce un único instalador convencional y una desinstalación registrada en Windows.

La detección e instalación de un runtime ausente se estudiará como alternativa para una compilación dependiente del framework, pero no será el flujo principal mientras contradiga el objetivo de instalación sin elevación.

## Comprobaciones del instalador

Antes de instalar o actualizar:

- verificar una versión compatible de Windows y arquitectura x64;
- comprobar espacio disponible;
- detectar una instancia activa de RightKeyboard y cerrarla de forma segura;
- localizar preferencias de 1.4 y de las alphas de 1.5;
- preservar configuración, alias, ignorados y copias exportadas;
- impedir que una actualización borre datos del usuario.

El repositorio deberá incluir:

- el script fuente del instalador;
- un script reproducible que publique la aplicación y compile el instalador;
- verificación SHA-256;
- pruebas de instalación nueva, actualización, reparación y desinstalación.

## Raíz única por usuario

La estructura propuesta es:

```text
%LOCALAPPDATA%\RightKeyboard\
├─ app\
│  └─ RightKeyboard.exe y archivos de runtime
├─ preferences.json
└─ exports\
```

- El instalador administra `app\`.
- RightKeyboard administra `preferences.json` y `exports\`.
- Las actualizaciones reemplazan solamente `app\`.
- La desinstalación ofrece conservar preferencias o eliminarlas expresamente.
- La migración desde `%LOCALAPPDATA%\RightKeyboard\config.txt` permanece compatible.

Esta separación mantiene todo bajo una sola raíz sin mezclar datos persistentes con archivos que una actualización puede reemplazar.

## Inicio con Windows

El instalador mostrará la casilla **Iniciar RightKeyboard con Windows**, activada por defecto.

Para una aplicación Win32 instalada por usuario, la primera implementación utilizará la clave documentada por Windows:

```text
HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run
```

La [clave `Run`](https://learn.microsoft.com/windows/win32/setupapi/run-and-runonce-registry-keys) ejecuta la aplicación cada vez que el usuario inicia sesión. Al usar `HKEY_CURRENT_USER` no requiere permisos administrativos.

Requisitos de comportamiento:

- el estado se puede cambiar desde la futura ventana **Configuración**;
- debe aparecer y poder deshabilitarse en las aplicaciones de inicio de Windows;
- una actualización conserva la elección existente;
- la desinstalación elimina la entrada;
- si Windows o el usuario lo deshabilitan externamente, RightKeyboard refleja el estado real y no lo reactiva silenciosamente;
- una instalación desatendida podrá desactivar esta opción expresamente.

## Evaluación de MSIX

[MSIX](https://learn.microsoft.com/windows/apps/package-and-deploy/packaging/) ofrece instalación por usuario, identidad de paquete y una integración más limpia con Windows. Se evaluará como segunda opción.

No se adoptará hasta resolver satisfactoriamente:

- firma del paquete distribuido desde GitHub;
- actualizaciones fuera de Microsoft Store;
- comportamiento de Raw Input y del inicio automático;
- acceso y migración de preferencias existentes;
- impacto sobre la simplicidad de compilación y contribución.

## Exportación de configuración futura

La ventana **Configuración** deberá incluir una acción **Exportar** en una actualización posterior. En `1.5.0`, la Configuración WinUI no expone todavía exportación/importación.

El archivo exportado:

- será JSON con versión de esquema;
- incluirá alias, nombres detectados, distribuciones e ignorados;
- no incluirá handles temporales ni rutas HID internas;
- propondrá el nombre `RightKeyboard-preferencias-AAAA-MM-DD.json`;
- podrá guardarse en una ubicación elegida o en `exports\`;
- no contendrá secretos ni datos ajenos a RightKeyboard.

## Importación de configuración futura

La acción **Importar** debe implementarse posteriormente con estos criterios:

- validar la estructura y versión antes de modificar datos;
- mostrar un resumen previo de cambios;
- ofrecer **Combinar** o **Reemplazar**;
- crear una copia de seguridad automática antes de aplicar;
- resolver dispositivos mediante identidad y huella persistente;
- conservar como pendientes los dispositivos que aún no existen en el equipo;
- informar asociaciones o distribuciones que no pudieron resolverse;
- no instalar ni eliminar distribuciones de Windows.

El formato exportado será un contrato portable. Importar no equivaldrá a copiar ciegamente el archivo interno `preferences.json`.

## Publicación final

La release estable 1.5 publicará:

- `RightKeyboard-1.5.0-Setup.exe` como descarga principal;
- `RightKeyboard-1.5.0-SHA256.txt`;
- los archivos de código fuente generados por GitHub.

Los ZIP autocontenido y dependiente del framework dejarán de ser descargas públicas normales. Se podrá conservar una compilación portable solamente como artefacto interno de diagnóstico.

## Criterios de aceptación

- Instalar, actualizar y desinstalar desde una cuenta estándar sin UAC.
- Ejecutar en un equipo limpio sin .NET Desktop Runtime preinstalado.
- Conservar preferencias durante una actualización.
- Mostrar RightKeyboard como aplicación instalada.
- Iniciar con Windows por defecto y permitir desactivarlo desde ambos lugares.
- Exportar e importar configuración con vista previa y respaldo cuando esa función sea incorporada a la Configuración WinUI.
- Desinstalar binarios sin borrar preferencias salvo confirmación expresa.
