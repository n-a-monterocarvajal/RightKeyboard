# Matriz de pruebas del instalador 1.5

El instalador se compila con `scripts\build-installer.ps1`. Los casos marcados como manuales deben ejecutarse en una cuenta estándar de Windows 10 y Windows 11 x64, idealmente también en una máquina limpia sin .NET Desktop Runtime.

La compilación de las alfas se verifica con Inno Setup 6.7.3. Instalación, desinstalación e inicio automático ya fueron aprobados en un host; alpha 5 conserva la misma publicación .NET autocontenida y requiere repetir las comprobaciones visuales y del acceso del menú Inicio.

| Caso | Preparación y acción | Resultado esperado | Tipo |
|---|---|---|---|
| Instalación nueva | Sin instalación ni datos previos; ejecutar el instalador | No aparece UAC; se instala en `%LOCALAPPDATA%\RightKeyboard\app`; la aplicación figura en Aplicaciones instaladas y se inicia | Manual |
| Acceso del menú Inicio | Completar una instalación nueva o una actualización | Aparece **RightKeyboard** en el menú Inicio y abre la aplicación instalada; no se crea un acceso en el escritorio | Manual |
| Equipo sin .NET | Máquina sin .NET Desktop Runtime; instalar y ejecutar | RightKeyboard abre normalmente porque la publicación es autocontenida | Manual |
| Inicio predeterminado | Instalación nueva con opciones predeterminadas | Se crea `HKCU\...\Run\RightKeyboard` y apunta al ejecutable instalado | Manual |
| Inicio desactivado al instalar | Ejecutar con `/MERGETASKS="!startup"` | No se crea la entrada de inicio | Manual |
| Cambio desde RightKeyboard | Desmarcar y volver a marcar la opción en Configuración | El estado visible y la entrada `Run` cambian de forma coherente | Manual |
| Desactivación desde Windows | Deshabilitar RightKeyboard en Aplicaciones de inicio y abrir Configuración | La opción aparece desmarcada; una actualización no vuelve a activarla | Manual |
| Instancia única | Iniciar dos veces `RightKeyboard.exe` | Solo permanece una instancia y un icono en el área de notificación | Manual |
| Actualización con la aplicación abierta | Con preferencias guardadas y RightKeyboard activo, instalar una versión posterior | La aplicación se cierra ordenadamente; se reemplaza solo `app`; `preferences.json` y `exports` se conservan | Manual |
| Actualización con inicio desactivado | Deshabilitar el inicio desde RightKeyboard o Windows y actualizar | El instalador conserva el estado desactivado | Manual |
| Lenguaje de actualización | Ejecutar el instalador sobre una versión existente | El título, la bienvenida y la acción principal dicen **Actualizar**, no **Instalar**; se anuncia que se conservarán preferencias e inicio automático | Manual |
| Reparación/reinstalación | Ejecutar de nuevo la misma versión con preferencias existentes | Los binarios se reinstalan y los datos permanecen sin cambios | Manual |
| Bloqueo de cierre | Impedir que la instancia responda y ejecutar actualización | El instalador no fuerza la terminación ni reemplaza archivos; muestra una instrucción clara | Manual |
| Desinstalación conservadora | Desinstalar y responder **No** al borrado de datos | Se elimina `app` y la entrada `Run`; se conservan preferencias y exportaciones | Manual |
| Desinstalación con borrado | Desinstalar y responder **Sí** | Se elimina toda la raíz `%LOCALAPPDATA%\RightKeyboard` | Manual |
| Desinstalación silenciosa | Ejecutar `unins000.exe /SILENT` y repetir con `/SILENT /BORRARDATOS=1` | La primera conserva datos; la segunda los elimina expresamente | Manual |
| Arquitectura incompatible | Intentar instalar en Windows no compatible con x64 | El instalador rechaza la instalación antes de copiar archivos | Manual |
| Integridad | Ejecutar `Get-FileHash` sobre el instalador | El valor coincide con `RightKeyboard-<versión>-SHA256.txt` | Automatizada por el script |

Antes de aceptar un artefacto, registrar versión de Windows, tipo de cuenta, ruta instalada, resultado, evidencia y cualquier reinicio solicitado. El instalador no debe publicar ni descargar componentes globales.
