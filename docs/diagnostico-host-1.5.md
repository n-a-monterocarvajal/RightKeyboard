# Diagnóstico en pruebas de host

Desde la beta 4 existe un registro diagnóstico circular, implementado sin dependencias externas para mantener liviano el proceso residente. En `1.5.0` estable no forma parte del build normal ni aparece en Configuración. Para pruebas de host debe compilarse explícitamente con el símbolo `RIGHTKEYBOARD_DIAGNOSTICS`.

El registro incluye:

- versión de la aplicación, Windows y arquitectura;
- cambios del inventario mediante identificadores anonimizados y los códigos públicos VID/PID/interfaz HID;
- clase de entrada, sin guardar la tecla concreta;
- presencia de código de escaneo, información adicional y bandera de tecla extendida, expresadas solo como valores booleanos;
- familia no única de la ruta (`HID`, `ACPI`, `ROOT`, etc.) y capacidades generales de teclado informadas por Raw Input;
- decisiones de asignación, recuperación por huella, exclusión e inicio del selector;
- contexto para distinguir una identidad nueva de otra ya configurada;
- el ciclo de vida de la exclusión por firma HID parcial: `firma_registrada` y `firma_no_registrada` (con motivo: `huella_presente`, `sin_vid_pid` o `dispositivo_no_conectado`) al ignorar manualmente, `ignorado_recuperado_por_firma` cuando una identidad nueva se suprime por firma, `firma_no_aplicada` (con motivo: `huella_presente`, `varias_coincidencias_conectadas` o `firma_con_distribucion`) cuando una firma registrada no se aplicó, y `firma_retirada` al reactivar u olvidar al portador. La firma canónica (`enum:HID|vid:2571|pid:4104|mi:00|col:01|caps:…`) se registra en claro porque solo contiene tokens públicos no únicos.

No incluye caracteres pulsados, alias elegidos, nombres detectados, rutas PnP completas ni contenido de archivos importados. En la variante diagnóstica conserva hasta tres archivos de 512 KiB en `%LOCALAPPDATA%\RightKeyboard\logs`.

En una compilación con `RIGHTKEYBOARD_DIAGNOSTICS`, Configuración muestra **Diagnóstico detallado** y **Abrir registros**. El modo detallado está desactivado por defecto. Para investigar una incidencia, se activa, se reproduce una vez y se desactiva. Los archivos nunca se transmiten automáticamente.

## Compilación de desarrollo

No usar esta variante para releases estables de usuario final. Para generar binarios locales de investigación:

```powershell
dotnet build .\RightKeyboard.sln --configuration Release /p:DefineConstants=RIGHTKEYBOARD_DIAGNOSTICS
```

Si se necesita instalador diagnóstico, debe generarse como artefacto interno con nombre distinto al de la release pública y dejando explícito que contiene el modo de registros.
