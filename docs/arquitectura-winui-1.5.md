# Arquitectura de migración a WinUI 3

Fecha: 30 de junio de 2026. Base: `v1.5.0-beta.1` (`dfb1556`).

## Decisión

Configuración y, en la fase siguiente, el selector serán ventanas WinUI 3 de un frontend separado que se inicia bajo demanda. El proceso `RightKeyboard.exe` conserva WinForms únicamente para `NotifyIcon`, Raw Input, coordinación de instancia y fallback estable.

No se alojará una isla XAML dentro del residente. Cargar WinUI, Windows App SDK, el dispatcher XAML y sus recursos dentro de `RightKeyboard.exe` aumentaría el consumo durante toda la sesión y no ofrece una frontera práctica para descargar el framework después de cerrar la ventana. Un proceso separado permite terminar y liberar todo ese coste al cerrar la experiencia visual.

## Primera fase

- `RightKeyboard.exe` sigue siendo el producto funcional y no cambia su flujo predeterminado.
- `RightKeyboard.WinUI.exe` es un prototipo no empaquetado de Configuración.
- El prototipo usa controles WinUI 3, tema del sistema, `MicaBackdrop`, escalado y automatización de interfaz nativos.
- Esta fase ya fue superada por el IPC de la segunda fase; el prototipo dejó de acceder directamente a `Configuration`.
- Alias, distribución, ignorado, olvido y limpieza ya son funcionales; importación, exportación e inicio automático permanecen en el fallback WinForms hasta que el núcleo pueda ejecutarlos por IPC.
- La interfaz WinForms sólida permanece como fallback y no vuelve a extender DWM sobre controles GDI.

## Despliegue del prototipo

Se usa Windows App SDK 2.2 estable, compatible desde Windows 10 1809. El proyecto es no empaquetado (`WindowsPackageType=None`) y autocontenido respecto de Windows App SDK para medir una copia reproducible sin instalar runtime global; durante el prototipo, .NET permanece framework-dependent para poder reutilizar el ensamblado del núcleo sin publicar una segunda copia completa de .NET. Esta elección es provisional: Microsoft advierte que el modo autocontenido aumenta tamaño, arranque y memoria al no compartir páginas de código. Antes de integrar el frontend en el instalador se comparará con el modo dependiente del framework y su instalador de runtime.

Fuentes oficiales:

- [Versiones y compatibilidad de Windows App SDK](https://learn.microsoft.com/windows/apps/get-started/versioning-overview)
- [Desempaquetar una aplicación WinUI](https://learn.microsoft.com/windows/apps/package-and-deploy/unpackage-winui-app)
- [Despliegue framework-dependent y autocontenido](https://learn.microsoft.com/windows/apps/package-and-deploy/deploy-overview)
- [Despliegue autocontenido](https://learn.microsoft.com/windows/apps/package-and-deploy/self-contained-deploy/deploy-self-contained-apps)
- [Materiales Mica y Acrylic](https://learn.microsoft.com/windows/apps/develop/ui/system-backdrops)

## Propiedad del estado

El núcleo es la autoridad y expone un protocolo local versionado:

1. el núcleo entrega un snapshot de dispositivos, distribuciones y preferencias;
2. WinUI devuelve comandos explícitos (guardar dispositivo, limpiar, importar o cambiar inicio automático);
3. el núcleo valida, persiste y responde con el estado resultante;
4. si el frontend no arranca o termina inesperadamente, se abre la Configuración WinForms.

No se escribe `preferences.json` desde dos procesos simultáneamente.

## Segunda fase: IPC integrado

El núcleo expone un canal local versionado mediante `NamedPipeServerStream`, limitado al usuario actual. El frontend solicita instantáneas y envía comandos explícitos para guardar, olvidar o limpiar; nunca abre ni escribe `preferences.json` directamente. Cada comando se valida y ejecuta en el hilo de interfaz del núcleo, que sigue siendo la única autoridad de persistencia y enumeración Raw Input.

La opción **Configuración** de la bandeja inicia `RightKeyboard.WinUI.exe` bajo demanda. Solo se admite una ventana a la vez. Si el ejecutable no está instalado o no puede iniciarse, se conserva el diálogo WinForms como fallback funcional.

## Riesgos abiertos

- tamaño de Windows App SDK autocontenido y tiempo de arranque en frío;
- consumo del proceso WinUI con Mica activo e inactivo;
- medición final del instalador Inno Setup con las dos aplicaciones autocontenidas;
- foco/activación entre `NotifyIcon` y el proceso frontend;
- extracción futura de `Configuration` y sus DTO a un ensamblado compartido para que el frontend no referencie el ejecutable WinForms;
- comportamiento de Mica y esquinas en Windows 10, VM, contraste alto y transparencia desactivada.

## Medición inicial del prototipo

Mediciones en la VM de desarrollo, compilación x64 del 30 de junio de 2026:

| Medida | Resultado |
|---|---:|
| Publicación Release, Windows App SDK autocontenido | 330 archivos; 144.786.636 bytes |
| Publicación Release, Windows App SDK compartido | 52 archivos; 80.597.316 bytes, más el runtime externo |
| Salida Debug autocontenida | 333 archivos; 146.167.487 bytes |
| Primera ventana visible, segundo arranque observado | 2.004 ms |
| Working set con Configuración abierta | 130.732.032 bytes |
| Memoria privada con Configuración abierta | 38.604.800 bytes |
| Hilos con Configuración abierta | 25 |

El núcleo no referencia `Microsoft.WindowsAppSDK`; por tanto, su inicio y consumo residente no cambian en esta fase. Al cerrar `RightKeyboard.WinUI.exe`, toda la memoria del frontend se libera. Las cifras de arranque son orientativas: quedan pendientes una medición fría repetible y la comparación en hardware físico.

El delta de disco es demasiado alto para integrar sin más el modo autocontenido. La siguiente fase debe evaluar el runtime compartido instalado por Inno Setup o reducir dependencias del metapaquete cuando Microsoft documente una referencia modular compatible con WinUI.
