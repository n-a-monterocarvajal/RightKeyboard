# Criterios de aceptación para la migración a WinUI 3

Base de comparación: `v1.5.0-beta.1`. Este documento es el contrato de entrega entre los frentes **Interfaz** y **Calidad y publicación**. No prescribe la implementación interna, salvo la separación necesaria para que el residente de Raw Input no cargue la interfaz mientras está cerrada.

La migración no se considera aceptada por una captura aislada. Debe conservar la función residente, degradarse de forma segura cuando un material no esté disponible y entregar mediciones repetibles en los mismos equipos antes y después del cambio.

## Alcance y puertas obligatorias

La candidata de WinUI 3 debe cumplir simultáneamente:

1. Configuración y selector usan controles WinUI 3; no simulan Acrylic o Mica mediante fondos capturados, opacidad GDI ni colores con apariencia aproximada.
2. El residente, la bandeja y Raw Input pueden permanecer en WinForms o Win32, pero la interfaz WinUI se crea solo bajo demanda y se libera al cerrar la última ventana.
3. Tema, DPI, accesibilidad, instalación por usuario y función de Raw Input igualan o superan `v1.5.0-beta.1`.
4. No se requiere UAC, instalación global del Windows App SDK ni descarga durante instalación o primera ejecución.
5. No hay regresiones bloqueantes o críticas ni crecimiento sostenido de memoria, handles o procesos después de ciclos de apertura y cierre.

## Contrato de entrega para el frente Interfaz

El frente Interfaz debe proporcionar a QA:

- commit exacto y artefacto Release reproducible;
- arquitectura usada —ventana en proceso, proceso de UI separado u otra— y nombres de procesos esperados;
- marcas temporales o registro diagnóstico para solicitud, creación, primera presentación e interactividad de cada ventana;
- forma inequívoca de comprobar cuándo el runtime de UI está cargado y cuándo fue liberado;
- identificadores o nombres de automatización estables para controles interactivos;
- lista de fallbacks por versión de Windows, contraste alto, transparencia desactivada, ahorro de batería y sesión remota;
- inventario de paquetes de Windows App SDK y modo de despliegue incluidos en el instalador;
- resultado automatizado y evidencia manual según la matriz de este documento.

Calidad devolverá cada fila como **Aprobada**, **Fallida**, **Bloqueada por entorno** o **No aplica**, siempre con evidencia. «No se aprecia diferencia» no es evidencia suficiente.

## Criterios por área

### Tema claro, oscuro y cambio en caliente

- Configuración, selector, diálogos, menús, campos, iconos, foco, selección y barras de desplazamiento siguen el tema efectivo del sistema al abrirse.
- Con una ventana abierta, alternar claro/oscuro actualiza todas sus superficies sin reinicio, recreación visible, destello blanco/negro ni pérdida de foco, selección o texto sin guardar.
- Una segunda ventana abierta después del cambio adopta inmediatamente el tema nuevo.
- Contraste alto manda sobre la paleta claro/oscuro. Los colores codificados no ocultan texto, foco, estados deshabilitados ni errores.
- Diez cambios consecutivos de tema no dejan controles con la paleta anterior ni aumentan de forma sostenida procesos, handles o memoria privada.

### Acrylic, Mica y degradación

- Configuración usa Mica nativo como material de ventana de larga duración.
- El selector o superficies transitorias definidas por Interfaz usan Acrylic nativo cuando corresponda; cualquier excepción se documenta con su material de sistema alternativo.
- La validación identifica el backdrop solicitado mediante la API o propiedades de WinUI y además confirma visualmente composición, activación/desactivación y contenido legible. Una captura por sí sola no demuestra que el material sea nativo.
- Con transparencia desactivada, contraste alto, ahorro de batería, sesión remota o plataforma sin soporte, aparece un fondo sólido del sistema sin huecos, contenido anterior, parpadeo ni pérdida funcional.
- Windows 10 puede usar fallback sólido. La ausencia de Mica/Acrylic allí no es fallo; forzar un efecto no compatible sí lo es.

### DPI, tamaño y monitores

- Probar 100 %, 125 %, 150 %, 175 % y 200 %, incluidos dos monitores con escalas distintas.
- Mover lentamente y con atajos cada ventana entre monitores conserva tamaño lógico razonable, nitidez, punto de anclaje y contenido completo.
- No hay texto truncado, controles superpuestos, zonas inaccesibles, doble escalado ni cambio repetitivo de tamaño al cruzar el límite.
- A tamaño mínimo y con texto de Windows al 200 %, las acciones críticas siguen visibles o alcanzables mediante desplazamiento.
- Cerrar y reabrir en el segundo monitor no devuelve la ventana fuera del área de trabajo ni con dimensiones físicas incorrectas.

### Accesibilidad

- Todo control interactivo expone nombre, rol, estado y, cuando corresponda, descripción mediante UI Automation.
- La navegación completa funciona con `Tab`, `Mayús+Tab`, flechas, `Entrar`, `Espacio` y `Esc`; el foco nunca queda atrapado ni desaparece.
- Narrador anuncia dispositivo, conexión, distribución, estado ignorado, selección y propósito de cada acción sin depender solo del color.
- El orden de automatización coincide con el orden visual. Al abrir un diálogo, cambiar de sección o mostrar un error, el foco se mueve a un destino predecible y vuelve al control iniciador al cerrar.
- Contraste alto, tamaño de texto al 200 % y animaciones desactivadas conservan operación completa. Los iconos informativos tienen alternativa textual.
- Accessibility Insights no presenta errores críticos en los flujos de selector, edición, importación, limpieza y cierre.

### Arranque bajo demanda y aislamiento

- Tras iniciar RightKeyboard y esperar cinco minutos sin abrir UI, no existe proceso de interfaz ni módulos de WinUI/Windows App SDK cargados dentro del residente, salvo que la arquitectura aprobada demuestre que su carga no puede diferirse. Cualquier excepción requiere medición y aceptación explícita.
- Abrir Configuración desde bandeja y abrir el selector desde un teclado nuevo crea la UI una sola vez; solicitudes simultáneas no duplican ventanas ni procesos.
- Cerrar la última ventana termina el proceso de UI o libera sus recursos. Dentro de 60 segundos, el residente vuelve al presupuesto de memoria cerrada.
- Un fallo o cierre forzado de la UI no termina Raw Input, no corrompe preferencias y permite abrir una ventana nueva en la siguiente solicitud.
- Objetivo inicial en el host de referencia: mediana de solicitud a interacción menor o igual a 1.500 ms en frío y 750 ms en caliente; P95 menor o igual a 2.500 ms. Si no se cumple, se registra como defecto de rendimiento antes de decidir la puerta de versión.

### Memoria y recursos

Las cifras se comparan en el mismo host, sesión, compilación Release autocontenida y secuencia de acciones. Se registran `PrivateMemorySize64`, conjunto de trabajo, commit, handles, handles USER/GDI y procesos descendientes.

- **Residente cerrado:** mediana tras cinco minutos, con tres ejecuciones limpias. Puerta: memoria privada no supera la beta en más de 5 MiB o 10 %, el valor que sea mayor.
- **UI abierta:** mediana a los 30 segundos con Configuración visible. Objetivo inicial: incremento agregado menor o igual a 120 MiB respecto del residente cerrado.
- **Retorno al cerrar:** a los 60 segundos de cerrar la última ventana, la memoria privada del residente queda dentro de 8 MiB o 15 % de su línea base, el valor que sea mayor, y no queda proceso de UI huérfano.
- **Ciclos:** después de 20 aperturas/cierres, la pendiente entre los últimos cinco ciclos no supera 1 MiB por ciclo y no crecen sostenidamente handles USER/GDI.
- Se informan mediana, máximo y P95; no se elige la mejor lectura. Antivirus, depurador, VM y captura de pantalla deben quedar registrados.

### Instalación y actualización sin UAC

- Instalación nueva desde cuenta estándar no muestra consentimiento ni solicitud de credenciales y mantiene la raíz por usuario.
- El instalador incluye todas las dependencias necesarias; funciona sin .NET Desktop Runtime ni Windows App SDK instalados globalmente y sin conexión a Internet.
- Actualizar desde `v1.5.0-beta.1`, con RightKeyboard abierto y preferencias reales de prueba, conserva configuración, exportaciones, inicio con Windows y acceso del menú Inicio.
- Reparar y desinstalar mantienen las decisiones existentes sobre conservación de datos. No quedan runtime, procesos, accesos o claves administradas por RightKeyboard al elegir borrado total.
- Se registra el aumento de tamaño del instalador y de `app`. Un aumento superior a 150 MiB frente a la beta requiere justificación y aceptación antes de RC; no se oculta eliminando símbolos o archivos necesarios para soporte.

### Regresión funcional de Raw Input

- La matriz FIS-01 a FIS-05, FIS-13 y FIS-15 de [calidad 1.5](calidad-1.5.md) se repite con UI cerrada, durante el arranque de UI, con Configuración abierta y durante su cierre.
- Alternar dos teclados con distribuciones distintas no pierde ni duplica pulsaciones y conserva la ventana activa como destino.
- Liberaciones, modificadores, eventos sintéticos, `Fn`, arrastre con mouse y el MX Master 3S no abren selectores falsos.
- Conectar, desconectar, suspender y reanudar refresca el inventario; la UI no toma propiedad del bucle ni del registro de Raw Input.
- Cerrar o bloquear la UI no detiene el residente. Veinte ciclos de apertura/cierre no duplican registros, iconos de bandeja, callbacks ni selectores.
- Las 81 pruebas de `v1.5.0-beta.1` continúan aprobadas y se agregan pruebas para el límite residente/UI, serialización de solicitudes y recuperación ante cierre de la UI.

## Matriz de ejecución

| ID | Área | Entorno mínimo | Procedimiento resumido | Evidencia | Puerta |
|---|---|---|---|---|---|
| WUI-TEM-01 | Tema inicial | Windows 11, claro y oscuro | Iniciar en cada tema y abrir todas las superficies | Capturas y registro de tema efectivo | Obligatoria |
| WUI-TEM-02 | Cambio en caliente | Windows 11, ventana abierta | Alternar tema 10 veces con datos sin guardar y foco conocido | Video, foco antes/después, recursos | Obligatoria |
| WUI-TEM-03 | Contraste alto | Windows 10/11 | Activar/desactivar contraste alto con UI abierta | Capturas y Accessibility Insights | Obligatoria |
| WUI-MAT-01 | Mica | Windows 11 22H2+, transparencia activa | Abrir/activar/desactivar Configuración | Propiedad/API de backdrop y video | Obligatoria |
| WUI-MAT-02 | Acrylic | Windows 11 22H2+, transparencia activa | Abrir selector/superficie definida y mover contenido posterior | Propiedad/API y video | Obligatoria |
| WUI-MAT-03 | Fallback | Transparencia off, batería, RDP, Windows 10 | Repetir aperturas y cambios de estado | Matriz de entorno y capturas | Obligatoria |
| WUI-DPI-01 | Escala | 100/125/150/175/200 % | Abrir, redimensionar y recorrer cada ventana | Capturas completas por escala | Obligatoria |
| WUI-DPI-02 | DPI mixto | Dos monitores distintos | Mover 10 veces y reabrir en monitor secundario | Video y tamaños lógico/físico | Obligatoria |
| WUI-A11Y-01 | Teclado/UIA | Windows 11 | Completar selector y Configuración sin mouse | Grabación y árbol UIA | Obligatoria |
| WUI-A11Y-02 | Narrador/texto | Narrador, texto 200 % | Ejecutar flujos críticos y provocar un error válido | Audio/video y hallazgos | Obligatoria |
| WUI-LAZY-01 | Reposo | Release, inicio limpio | Esperar 5 min sin UI e inspeccionar procesos/módulos | Lista de procesos/módulos y métricas | Obligatoria |
| WUI-LAZY-02 | Ciclo de vida | Release | Abrir/cerrar 20 veces; forzar cierre una vez | Tiempos, procesos, handles, recuperación | Obligatoria |
| WUI-PERF-01 | Arranque | Disco frío y caliente | 10 aperturas por condición | Mediana/P95 de marcas temporales | Objetivo medido |
| WUI-MEM-01 | Memoria cerrada | Mismo host beta/migración | Tres arranques, muestra a 5 min | CSV con métricas y comparación | Obligatoria |
| WUI-MEM-02 | Memoria abierta/cierre | Mismo host | Medir abierta, a 60 s y tras 20 ciclos | CSV, mediana, máximo, P95 | Obligatoria |
| WUI-INS-01 | Instalación | Cuenta estándar limpia, offline | Instalar y ejecutar sin runtimes globales | Video, registro y archivos instalados | Obligatoria |
| WUI-INS-02 | Actualización | `v1.5.0-beta.1` con datos | Actualizar con aplicación abierta | Comparación de datos, accesos y claves | Obligatoria |
| WUI-RAW-01 | Función principal | Dos teclados físicos | Alternar antes/durante/después de UI | Registro de eventos y resultado visible | Obligatoria |
| WUI-RAW-02 | Falsos positivos | MX Master 3S y combinaciones | Ejecutar entradas con UI cerrada/abierta | Video y ausencia de selector | Obligatoria |
| WUI-RAW-03 | Ciclo del dispositivo | USB/hub, suspensión | Reconectar, cambiar puerto, suspender/reanudar | Inventario y preferencias antes/después | Obligatoria |

## Protocolo de medición

1. Publicar beta y candidata con la misma configuración Release, RID y modo autocontenido.
2. Reiniciar el host o cerrar sesión antes de cada serie; esperar dos minutos sin actividad del instalador o actualizador.
3. Ejecutar tres series para memoria y diez para tiempos. Descartar una muestra solo con causa externa registrada.
4. Capturar cada 5 segundos proceso, PID, memoria privada, conjunto de trabajo, handles y procesos descendientes. Marcar apertura, interacción y cierre con hora monotónica.
5. Para UI abierta usar el mismo conjunto de dispositivos y la misma pantalla de Configuración, sin depurador.
6. Guardar datos brutos en CSV y un resumen Markdown junto al commit probado; no versionar preferencias ni identificadores HID completos.
7. Comparar mediana, máximo y P95 de beta y candidata. Toda desviación de una puerta requiere defecto, causa y aceptación explícita.

## Criterio de salida

La migración queda lista para integrarse cuando todas las filas obligatorias estén aprobadas en Windows 11 y sus fallbacks en Windows 10, las regresiones automatizadas estén verdes, no haya defectos críticos y el informe incluya datos brutos reproducibles. Los objetivos de tiempo o tamaño que no alcancen el valor inicial deben quedar decididos expresamente; no se convierten silenciosamente en aprobados.
