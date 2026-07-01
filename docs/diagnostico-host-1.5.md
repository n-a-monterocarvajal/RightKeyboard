# Diagnóstico en pruebas de host

Antes de cerrar 1.5 se incorporará un registro diagnóstico circular y opcional. Serilog es una opción adecuada por sus *sinks* y archivos rotativos, pero la decisión se tomará comparando su coste con una implementación pequeña basada en `Microsoft.Extensions.Logging`.

El registro incluirá:

- versión, compilación de Windows y arquitectura;
- `SystemUsesLightTheme`, `AppsUseLightTheme`, contraste alto y resultado de los atributos DWM;
- conexión, desconexión y clasificación de dispositivos mediante identificadores anonimizados;
- decisiones de asignación, exclusión e inicio del selector;
- excepciones no controladas y errores de persistencia.

No incluirá caracteres pulsados, alias elegidos por el usuario, rutas PnP completas ni contenido de archivos importados. Se conservarán pocos archivos con límite de tamaño dentro de `%LOCALAPPDATA%\RightKeyboard\logs`, habrá una acción **Abrir carpeta de diagnóstico** y el modo detallado estará desactivado por defecto.

Para una versión publicada se conservarán únicamente eventos de inicio, actualización y errores. El trazado detallado se habilitará temporalmente desde Configuración o mediante un argumento de línea de comandos documentado.
