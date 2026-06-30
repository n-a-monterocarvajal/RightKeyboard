# Decisión arquitectónica Fluent para RightKeyboard 1.5

Fecha: 29 de junio de 2026.

## Decisión

Se mantiene el núcleo residente, Raw Input y la bandeja en WinForms. El selector y Configuración adoptan Fluent de forma incremental mediante materiales y atributos públicos de DWM, una capa visual común y controles WinForms accesibles. No se incorpora todavía WinUI 3 ni XAML Islands.

La aplicación usa:

- Mica para Configuración, una superficie de larga duración;
- Mica Alt para el selector modal, que necesita mayor separación visual;
- un menú Fluent sólido para la bandeja, debido a las limitaciones de repintado del `ContextMenuStrip`;
- esquinas redondeadas, marco oscuro, Segoe UI Variable cuando está disponible y métricas comunes de 4/8/12/16/24 píxeles lógicos.

## Evidencia y motivo

Microsoft indica que Windows App SDK puede modernizar WinForms y que XAML Islands permite incrustar WinUI, pero una aplicación no empaquetada debe inicializar y desplegar el runtime. La distribución autocontenida evita esa dependencia a costa de más tamaño, un arranque más lento y más memoria sin páginas compartidas. Para dos ventanas es un coste permanente o de instalación que no aporta suficiente valor en esta alfa.

Las APIs `DwmSetWindowAttribute`, `DwmExtendFrameIntoClientArea`, `DWM_SYSTEMBACKDROP_TYPE`, `DWMWA_WINDOW_CORNER_PREFERENCE` y `DWMWA_USE_IMMERSIVE_DARK_MODE` están documentadas. `DWMSBT_MAINWINDOW` corresponde a Mica y `DWMSBT_TABBEDWINDOW` a Mica Alt en Windows 11. Esta vía no carga componentes nuevos en reposo y conserva una futura migración por ventana.

Fuentes oficiales:

- [Windows App SDK en proyectos existentes](https://learn.microsoft.com/windows/apps/windows-app-sdk/use-windows-app-sdk-in-existing-project)
- [Despliegue de Windows App SDK](https://learn.microsoft.com/windows/apps/package-and-deploy/deploy-overview)
- [Materiales Mica y Acrylic](https://learn.microsoft.com/windows/apps/develop/ui/system-backdrops)
- [DWM_SYSTEMBACKDROP_TYPE](https://learn.microsoft.com/windows/win32/api/dwmapi/ne-dwmapi-dwm_systembackdrop_type)
- [Esquinas redondeadas en aplicaciones de escritorio](https://learn.microsoft.com/windows/apps/desktop/modernize/ui/apply-rounded-corners)
- [Tipografía de Windows](https://learn.microsoft.com/windows/apps/design/signature-experiences/typography)

## Compatibilidad y degradación controlada

Los materiales de sistema requieren Windows 11 22H2, compilación 22621. En Windows 11 22000 se mantienen marco oscuro y esquinas, pero el fondo es sólido. En Windows 10, contraste alto o si DWM rechaza el atributo, toda la interfaz conserva colores del sistema y la misma funcionalidad.

El propio sistema convierte Mica en un color sólido cuando la transparencia está desactivada, el equipo está en ahorro de batería o el hardware no admite el efecto. Las máquinas virtuales pueden ocultar esquinas y transparencia por diseño.

## Corrección posterior a la prueba de alpha 4

La prueba en hardware físico demostró que omitir el fondo de un `ContextMenuStrip` para mostrar Acrylic dejaba píxeles GDI anteriores sin limpiar. Cada paso del cursor acumulaba el color de selección y podía dejar varias opciones resaltadas. Alpha 5 elimina el material de esa superficie concreta, repinta siempre el fondo completo con colores opacos y centra el texto mediante métricas explícitas.

La misma prueba mostró que establecer `DWM_SYSTEMBACKDROP_TYPE` no era suficiente mientras los contenedores WinForms cubrieran el cliente. Alpha 5 extiende el marco DWM, pero mantiene fondos sólidos donde GDI no compone transparencia de forma fiable. También reaplica la paleta completa al recibir cambios de tema.

La validación local posterior confirmó además que la transparencia entre superficies GDI hijas y el fondo DWM no es uniforme. Por ello alpha 5 conserva una paleta sólida como fallback obligatorio y no se considera todavía la solución visual definitiva. Si el objetivo de 1.5 exige Acrylic visible en toda la interfaz, las ventanas deberán migrarse a WinUI 3/Windows App SDK o ejecutarse como una interfaz WinUI separada del residente; no se seguirá simulando transparencia sobre controles WinForms.

## Evolución posterior

La capa `FluentForm`/`FluentContextMenuStrip` concentra materiales y fallbacks. Si una futura versión requiere controles WinUI genuinos, se podrá sustituir una ventana completa o alojar una isla sin acoplar Windows App SDK al procesamiento Raw Input. Esa decisión deberá incluir mediciones del runtime, arranque y memoria en hardware físico.

## Medición de alpha.4

Publicaciones autocontenidas `win-x64`, Release y sin símbolos:

- baseline `9cc7bf1`: 270 archivos y 122.728.898 bytes;
- alpha.4: 270 archivos y 122.735.554 bytes;
- incremento: 6.656 bytes (0,0054 %), contenido íntegramente en `RightKeyboard.dll`;
- dependencias nuevas: ninguna; no se añadió Windows App SDK ni otro paquete NuGet.

En esta VM, el residente alpha.4 sin ventanas mostró 45,8 MB de conjunto de trabajo y 10,3 MB privados. La vista de Configuración abierta en Debug mostró 69,9 MB y 17,9 MB, respectivamente. Son observaciones puntuales, no límites de rendimiento; queda pendiente repetir Release y comparar memoria en el mismo host físico con transparencia habilitada.
