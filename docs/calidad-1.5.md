# Calidad y publicación de RightKeyboard 1.5

Este documento es la fuente de verdad para certificar `1.5.0-rc.1` y `1.5.0`. Se actualiza con evidencia reproducible y no sustituye las especificaciones funcionales de [continuación](continuacion-1.5.md) ni de [distribución](distribucion-1.5.md).

## Estado integrado de alpha 5

Integración auditada el 30 de junio de 2026 sobre la rama `codex/version-1.5`, versión `1.5.0-alpha.9`. Los frentes de trabajo están combinados y la certificación física continúa.

| Área | Resultado | Evidencia o brecha |
|---|---|---|
| Restauración | Correcta | `dotnet restore RightKeyboard.sln` con SDK 10.0.301. |
| Compilación Release | Correcta | 0 errores y 0 advertencias. |
| Pruebas automatizadas | Correcta | 81 pruebas NUnit sobre el código integrado. |
| Versionado | Coherente para alpha | Proyecto e instalador `1.5.0-alpha.9`; manifiesto `1.5.0.0`. Debe cambiarse solo al preparar la candidata. |
| Elevación de la aplicación | Correcta en código | El manifiesto declara `asInvoker`; falta comprobar el instalador. |
| Documentación | Revisada | README, CHANGELOG y ROADMAP enlazan este estado. `continuacion-1.5.md` conserva el registro histórico de alpha 1. |
| Notas de versión | Borrador | Existe un borrador para `1.5.0-rc.1`; no debe presentarse como publicación. |
| Instalación y artefactos | Correcta | Inno Setup genera el instalador y su SHA-256. Instalación, desinstalación e inicio automático fueron validados en host; el acceso del menú Inicio se añadió después de la primera prueba. |
| Certificación física | Pendiente | Requiere hardware, cambio de sesión y pruebas en cuenta estándar. |

## Matriz automatizada

| ID | Nivel | Caso | Estado actual | Criterio esperado |
|---|---|---|---|---|
| AUT-01 | Unidad | Pulsación normal, liberación, modificadores y evento sintético | Cubierto | Solo una pulsación normal puede iniciar la asignación. |
| AUT-02 | Unidad | Clasificación conservadora de mouse, touchpad, trackball y teclado ambiguo | Cubierto | El no-teclado inequívoco se excluye; lo ambiguo no. |
| AUT-03 | Unidad | Logitech MX Master 3S por nombre detectado | Cubierto parcialmente | Se clasifica como no-teclado; falta validar el dispositivo real y sus colecciones HID. |
| AUT-04 | Unidad | Guardar/cargar alias, asociación e ignorados en esquema 3 | Cubierto | Los datos sobreviven una recarga. |
| AUT-05 | Unidad | Migración de esquema 2 a esquema 3 | Cubierto | Se conserva la asociación y se rechazan archivos incompatibles, duplicados o contradictorios. |
| AUT-06 | Unidad | Migración desde `config.txt` de 1.4 | Pendiente | Se conservan todas las asociaciones resolubles y se escribe un JSON válido. |
| AUT-07 | Unidad | Reconexión con huella única | Cubierto | Recupera distribución y alias y aprende la identidad nueva. |
| AUT-08 | Unidad | Dos dispositivos con huella igual | Cubierto parcialmente | No adivina una asociación; falta probar asociaciones distintas ya conocidas. |
| AUT-09 | Unidad | Ignorado tras reconexión/cambio de identidad | Pendiente | Recupera el estado solo cuando la coincidencia es inequívoca. |
| AUT-10 | Unidad | Editar, olvidar y limpiar preferencias | Cubierto | Las colecciones quedan coherentes y el borrado persiste. |
| AUT-11 | Unidad | Exportar, combinar, reemplazar y respaldar | Cubierto | La aplicación transaccional crea respaldos únicos y conserva la memoria ante fallos de escritura. |
| AUT-12 | Integración | Importar en otro equipo por huella, con distribuciones ausentes | Pendiente | Resuelve lo posible, conserva pendientes e informa lo no resuelto. |
| AUT-13 | Integración | Clave de inicio por usuario | Pendiente | Crear, leer y quitar `HKCU\\...\\Run` es idempotente y conserva rutas con espacios. |
| AUT-14 | Integración | Cambio de inventario por `WM_INPUT_DEVICE_CHANGE` | Pendiente | Refresca dispositivos sin perder estado ni bloquear entrada. |
| AUT-15 | Build | Release tratada con advertencias como error | Pendiente | La solución compila sin advertencias con una política explícita. |

## Matriz física y de sistema

Registrar para cada ejecución: fecha, commit, Windows, arquitectura, escala/DPI, cuenta, hardware, resultado, captura o registro y responsable.

| ID | Prioridad | Preparación y pasos resumidos | Resultado esperado | Puerta |
|---|---|---|---|---|
| FIS-01 | Crítica | Asignar distribuciones distintas a dos teclados físicos y alternar escritura en Bloc de notas, navegador y una aplicación normal. | Cada ventana activa recibe la distribución del teclado que produjo la pulsación; no cambia el valor global. | RC/estable |
| FIS-02 | Crítica | Desconectar y reconectar un teclado asignado en el mismo puerto, con la aplicación abierta y tras reiniciarla. | No reaparece el selector y se conservan alias y distribución. | RC/estable |
| FIS-03 | Crítica | Reconectar el teclado asignado en otro puerto USB directo y mediante hub. | Se recupera la preferencia cuando existe identidad suficiente; si es ambigua, no se adivina. | RC/estable |
| FIS-04 | Crítica | Conectar simultáneamente dos teclados del mismo modelo, nombrarlos y asignarlos de forma distinta; reiniciar y permutar puertos. | Permanecen diferenciados si Windows expone identidades distintas; cualquier ambigüedad se comunica sin sobrescribir datos. | RC/estable |
| FIS-05 | Alta | Usar botones, rueda y gestos de un Logitech MX Master 3S, incluido su receptor y Bluetooth si están disponibles. | No abre el selector; un teclado real del mismo receptor sigue funcionando. | RC/estable |
| FIS-06 | Alta | Probar 100 %, 125 %, 150 %, 200 % y mover selector/configuración entre monitores con DPI distinto. | Texto, filas, foco y botones permanecen visibles, sin recortes ni saltos de tamaño. | RC/estable |
| FIS-07 | Crítica | Activar/desactivar inicio con Windows desde Configuración, cerrar sesión y reiniciar. Repetir con ruta que contenga espacios. | El estado real se refleja, hay una sola instancia y la elección persiste. | RC/estable |
| FIS-08 | Crítica | Exportar; importar combinando y reemplazando; cancelar; usar JSON inválido; confirmar respaldo. | No hay modificación al cancelar o fallar; cada importación aplicada deja respaldo y resultado coherente. | RC/estable |
| FIS-09 | Crítica | Exportar en equipo A e importar en B, con una distribución ausente y un dispositivo aún desconectado. | Se informa lo no resuelto y se conservan preferencias pendientes sin corrupción. | Estable |
| FIS-10 | Crítica | Instalación nueva autocontenida en cuenta estándar sin .NET Desktop Runtime. Vigilar solicitudes de elevación. | Instala y ejecuta sin UAC ni dependencia externa; aparece en Aplicaciones instaladas. | RC/estable |
| FIS-11 | Crítica | Actualizar desde 1.4 y desde la última alpha con preferencias, alias, ignorados y exportaciones. | Conserva datos, migra una vez y reemplaza solo `app\\`. | RC/estable |
| FIS-12 | Crítica | Reparar y desinstalar, primero conservando datos y luego aceptando eliminarlos. | Elimina binarios y entrada de inicio; los datos solo se borran con confirmación expresa. | Estable |
| FIS-13 | Alta | Suspender/reanudar, conectar/desconectar durante el selector y cambiar de usuario. | No se bloquea, no duplica iconos y refresca el inventario. | Estable |
| FIS-14 | Alta | Navegar selector y configuración solo con teclado; probar alto contraste y Narrador. | Orden de foco lógico, nombres accesibles y contraste legible. | Estable |
| FIS-15 | Alta | Mantener escritura rápida, combinaciones `Ctrl`/`Alt`/`Shift`/Windows/`Fn` y arrastrar con mouse. | No aparecen selectores falsos ni se pierden pulsaciones de forma perceptible. | RC/estable |
| FIS-16 | Media | Observar CPU, memoria y handles durante 30 minutos en reposo y durante 10 minutos de uso. | Sin crecimiento sostenido ni consumo material en reposo frente a 1.4. | Estable |

## Entrada y salida de `1.5.0-rc.1`

### Entrada

- Alcance funcional congelado e integrados los demás frentes sobre una revisión identificada.
- Instalador y scripts reproducibles presentes, con nombre/versionado de candidata y SHA-256.
- Solución limpia: restauración, build Release y pruebas automatizadas correctas, sin advertencias.
- Todas las filas automatizables críticas cubiertas o con excepción documentada.
- README, CHANGELOG, ROADMAP, especificación de distribución y borrador de notas coherentes.
- Sin defectos abiertos de severidad bloqueante o crítica.

### Salida

- FIS-01 a FIS-08, FIS-10, FIS-11 y FIS-15 aprobadas en Windows 10 y Windows 11 x64, con al menos una cuenta estándar.
- Instalación, actualización y primera ejecución sin UAC ni .NET preinstalado verificadas en entorno limpio.
- Dos teclados iguales y MX Master 3S probados físicamente, o excepción explícita con hardware equivalente y riesgo aceptado.
- Sin pérdida de preferencias y sin regresión de la función principal.
- Defectos altos restantes enumerados con mitigación; ninguno afecta instalación, identidad, persistencia o cambio de distribución.

## Entrada y salida de `1.5.0`

### Entrada

- Una candidata cerrada funcionalmente y disponible para prueba durante al menos 72 horas.
- Todos los bloqueantes de la candidata resueltos y repetición selectiva de sus pruebas.
- Artefactos finales generados desde un commit identificable, sin cambios manuales posteriores.

### Salida

- Matriz completa aprobada, incluidas portabilidad entre equipos, desinstalación, accesibilidad y observación de recursos.
- Repetición de build y pruebas desde un checkout limpio del commit final.
- Nombre, versión y hash coincidentes entre ejecutable, instalador, CHANGELOG y notas.
- Notas finales en español, limitaciones conocidas explícitas y pasos de actualización comprobados.
- Aprobación humana de publicación registrada. Publicar, etiquetar y subir artefactos es una acción posterior y separada.

## Riesgos y vacíos abiertos

1. **Bloqueante — distribución:** el instalador se compila de forma reproducible, pero todavía no se ha probado en una cuenta estándar ni se han validado actualización y desinstalación.
2. **Alto — portabilidad:** la importación combina identidades almacenadas; falta demostrar resolución entre equipos por huella y manejo visible de distribuciones no disponibles.
3. **Alto — hardware:** las pruebas unitarias del MX Master 3S usan su nombre, no sus colecciones HID reales.
4. **Alto — inicio:** la instancia única y el cierre coordinado están implementados; faltan pruebas de registro, inicio de sesión y activación predeterminada por el instalador.
5. **Medio — automatización:** no hay configuración de integración continua ni política que convierta advertencias en errores.
6. **Medio — evidencia:** faltan resultados físicos trazables para DPI, accesibilidad, suspensión y consumo.

## Registro de certificación

No completar esta sección hasta que los otros frentes estén integrados.

| Campo | Valor |
|---|---|
| Commit candidato | Pendiente |
| Instalador y SHA-256 | Pendiente |
| Entornos probados | Pendiente |
| Resultado RC | Pendiente |
| Resultado estable | Pendiente |
| Excepciones aceptadas | Ninguna registrada |
