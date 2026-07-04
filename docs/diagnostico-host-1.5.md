# Diagnóstico en pruebas de host

Desde la beta 4 existe un registro diagnóstico circular y opcional, implementado sin dependencias externas para mantener liviano el proceso residente.

El registro incluye:

- versión de la aplicación, Windows y arquitectura;
- cambios del inventario mediante identificadores anonimizados y los códigos públicos VID/PID/interfaz HID;
- clase de entrada, sin guardar la tecla concreta;
- decisiones de asignación, recuperación por huella, exclusión e inicio del selector;
- contexto para distinguir una identidad nueva de otra ya configurada.

No incluye caracteres pulsados, alias elegidos, nombres detectados, rutas PnP completas ni contenido de archivos importados. Conserva hasta tres archivos de 512 KiB en `%LOCALAPPDATA%\RightKeyboard\logs`.

En Configuración se puede activar **Diagnóstico detallado** y usar **Abrir registros**. El modo detallado está desactivado por defecto. Para investigar una incidencia, se activa, se reproduce una vez y se desactiva. Los archivos nunca se transmiten automáticamente.
