# Preferencias y portabilidad de RightKeyboard 1.5

Este documento define el contrato persistente del esquema 5 y las operaciones que pueden modificarlo. El archivo es portable entre instalaciones de RightKeyboard, pero sus distribuciones solo se activan si también están instaladas en el Windows de destino.

## Rutas de datos

| Dato | Ruta | Comportamiento |
|---|---|---|
| Preferencias activas | `%LOCALAPPDATA%\RightKeyboard\preferences.json` | RightKeyboard lo lee al iniciar y lo reemplaza mediante una escritura temporal. |
| Configuración 1.4 | `%LOCALAPPDATA%\RightKeyboard\config.txt` | Solo se migra cuando todavía no existe `preferences.json`. No se modifica ni elimina. |
| Respaldos automáticos | `%LOCALAPPDATA%\RightKeyboard\exports\RightKeyboard-respaldo-*.json` | Se crea uno antes de aplicar cada importación. Dos respaldos nunca reutilizan el mismo nombre. |
| Exportaciones manuales | Ruta elegida por el usuario | Usan el mismo esquema 5 validado. |

Los archivos temporales de guardado se crean junto al destino y se eliminan al terminar o fallar la operación.

## Contrato del esquema 5

La raíz es un objeto JSON con estas propiedades:

- `version`: entero con valor `5`;
- `devices`: inventario de dispositivos recordados;
- `mappings`: asociaciones entre una identidad de dispositivo y una distribución;
- `ignoredDeviceIds`: identidades que no deben activar una distribución;
- `ignoredSignatures`: firmas HID parciales registradas tras un ignorado manual seguro;
- `groups`: preferencias lógicas y membresías creadas manualmente por el usuario.

Cada elemento de `devices` conserva:

- `identity`: identidad persistente y clave lógica, comparada sin distinguir mayúsculas;
- `fingerprint`: huella de modelo usada solo para recuperación no ambigua;
- `signature`: firma HID parcial canónica opcional; no es una identidad;
- `detectedName`: último nombre informado por Windows;
- `customName`: alias opcional del usuario;
- `technicalId`: identificador técnico visible para diagnóstico;
- `lastSeenUtc`: fecha y hora UTC de la última detección.

Una asociación contiene la identidad, el identificador hexadecimal de la distribución y sus nombres de idioma y distribución. Los nombres permiten resolver la distribución en otro equipo cuando su identificador no coincide. Un dispositivo puede tener una asociación individual latente mientras pertenece a un grupo, pero nunca puede estar asociado e ignorado simultáneamente.

Cada grupo contiene un `id`, un `displayName`, una distribución opcional y al menos dos `memberIdentities` distintas. Una identidad pertenece como máximo a un grupo y no puede estar ignorada mientras sea miembro. El alias/layout del grupo reemplaza de forma efectiva —sin borrar— las preferencias individuales; al separar, reaparecen exactamente los valores anteriores. La membresía solo cambia por una operación manual o por importación explícita: la recuperación por huella y por firma nunca fusiona identidades.

## Carga y migraciones

- Un documento sin `version` se interpreta como esquema 2 para conservar compatibilidad con la primera alpha de 1.5.
- El esquema 2 migra asociaciones, ignorados, huella y nombre detectado. Como no contenía alias, identificador técnico ni última detección, esos campos quedan vacíos y la migración registra como última detección el momento de carga.
- Los esquemas 3 y 4 migran en memoria; el 3 no aporta firmas y ninguno aporta grupos. El siguiente guardado escribe esquema 5.
- `config.txt` de 1.4 se migra únicamente en el arranque normal y solo si no existe el JSON activo.
- Los esquemas anteriores al 2 y posteriores al 5 se rechazan. Una versión futura nunca se intenta leer como el esquema vigente.
- Las colecciones `null` se tratan como vacías. JSON inválido, tipos incompatibles, identidades vacías, duplicados sin distinguir mayúsculas, referencias inexistentes, membresías múltiples y estados contradictorios se rechazan antes de modificar preferencias.
- Si una distribución válida del archivo no está instalada, se conserva el dispositivo sin asociación y la importación presenta una advertencia.

## Contrato para la interfaz

La interfaz trabaja sobre la misma instancia de `Configuration`; no debe recrearla ni editar sus colecciones para aplicar cambios:

- `TouchDevice` actualiza nombre detectado, huella, identificador técnico y última detección sin perder el alias;
- `UpdatePreference` edita alias, distribución y estado ignorado de un dispositivo existente;
- `GroupDevices` crea un grupo o añade una identidad no agrupada a uno existente; la identidad gobernante aporta el alias/layout;
- `Ungroup` separa una identidad y disuelve el grupo cuando queda un solo miembro;
- `Forget` elimina únicamente el dispositivo indicado y sus estados asociados;
- `LoadImport` valida un archivo y devuelve la configuración candidata junto con advertencias, sin modificar el estado activo;
- `ApplyImport` crea el respaldo, guarda la configuración candidata y solo entonces actualiza la instancia en memoria;
- `Clear` persiste primero una configuración vacía y solo después vacía la instancia en memoria.

Si el guardado de una importación o limpieza falla, la instancia activa queda intacta.

## Combinar, reemplazar, exportar y respaldar

**Combinar** conserva los dispositivos locales ausentes de la importación. Cuando una identidad existe en ambos lados, el registro importado reemplaza por completo alias, metadatos y modo del registro local; un dispositivo importado sin asociación ni estado ignorado queda también sin modo.

**Reemplazar** construye el estado únicamente con los dispositivos importados.

Ambos modos validan primero la configuración candidata. Antes de persistirla crean un respaldo del estado actual. El guardado usa un archivo temporal único y reemplaza el destino; la memoria solo cambia después de que ese reemplazo termina correctamente.

La exportación incluye identidades técnicas y huellas necesarias para reconocer dispositivos, pero no handles de Raw Input, rutas temporales ni secretos. Esos identificadores pueden revelar el modelo o identificador Plug and Play del periférico, por lo que conviene revisar el JSON antes de compartirlo públicamente.

## Comportamiento de «Limpiar preferencias»

La acción guarda un esquema 5 válido con `devices`, `mappings`, `ignoredDeviceIds`, `ignoredSignatures` y `groups` vacíos. Por lo tanto elimina:

- alias y nombres detectados recordados;
- identidades, huellas, identificadores técnicos y últimas detecciones;
- asociaciones de distribución;
- decisiones de ignorar dispositivos.
- firmas ignoradas y grupos lógicos.

No elimina distribuciones de Windows, respaldos, exportaciones, la preferencia de inicio con Windows ni el antiguo `config.txt`. Como conserva un `preferences.json` vacío, el archivo legado no se vuelve a migrar en el siguiente arranque.
