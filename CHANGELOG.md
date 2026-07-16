# Registro de cambios

Todos los cambios relevantes del proyecto se documentan en este archivo y se describen en español.

## [Sin publicar]

- Al ignorar manualmente un dispositivo débilmente identificado (huella vacía), la exclusión se extiende a su firma HID parcial (enumerador, VID, PID, interfaz, colección y capacidades): reconectarlo en otro puerto ya no reabre el selector si la coincidencia es inequívoca. La regla nunca opera sobre teclados con huella, exige una sola coincidencia conectada y se desactiva al reactivar o asignar distribución al dispositivo.
- Las preferencias pasan al esquema 4 (`ignoredSignatures` y `signature` por dispositivo). Los archivos de esquema 3 migran automáticamente al guardar; un export de esquema 4 no puede importarse en 1.5.0.
- El diagnóstico de desarrollo explica la regla de firma con eventos nuevos: `firma_registrada`, `firma_no_registrada`, `ignorado_recuperado_por_firma`, `firma_no_aplicada` y `firma_retirada`.
- CI en Windows con advertencias como errores; Exportar/Importar e «Iniciar con Windows» disponibles en la Configuración WinUI.

## [1.5.0] - 2026-07-09

### Versión estable

- Publicación estable inicial de la línea 1.5, basada en la validación física acumulada hasta beta 7.
- Se consolida el núcleo liviano de Raw Input con Configuración y selector WinUI 3 bajo demanda.
- El menú de bandeja queda reducido a **Configuración** y **Salir**; **Limpiar preferencias** vive en Configuración.
- El diagnóstico detallado se retira del build normal y queda como capacidad de desarrollo activable por compilación.
- Quedan planificadas para versiones posteriores la detección preventiva más robusta de periféricos HID ambiguos y la agrupación manual de identidades del mismo dispositivo cuando cambia de puerto USB.

### Beta 6

- La capa modal cubre toda la superficie cliente y usa transiciones de composición al entrar y salir, respetando la preferencia de animaciones de Windows.
- Las barras de título de Configuración y del selector muestran la versión efectiva como información secundaria.
- «Entrada detectada» incluye el identificador técnico corto para distinguir dispositivos todavía sin alias.
- La descripción principal se simplifica a «Administra los teclados detectados.»
- Incluye la corrección de composición validada localmente que evita el cierre al editar el alias.
- El instalador diferencia de forma coherente preparación, progreso y finalización de una actualización.
- El aviso de edición prolonga su duración sin reiniciar el fade en cada carácter y conserva visible la identificación del teclado.
- El selector reintenta una vez su activación después de cargar si Windows conserva el foco anterior.
- El diagnóstico añade familia de ruta, señales de inyección, presencia de código de escaneo y capacidades Raw Input sin registrar la tecla concreta.

### Beta 5.2

- Sustituido el `Storyboard` del aviso de edición por una animación de composición nativa, evitando la excepción WinRT que cerraba Configuración.
- Añadido un fallback protegido: un fallo del efecto visual ya no puede terminar el frontend.

### Beta 5.1

- Corregido el cierre de Configuración al editar por primera vez el alias: la animación del aviso ya no intenta detenerse antes de haberse iniciado.
- Registrados para beta 6 el identificador corto en «Entrada detectada», la microcopia concisa, el overlay modal completo y la versión en la barra de título.

### Beta 5

- El selector automático refuerza su activación y aplica un pulso temporal al frente cuando Windows conserva el foco de otra aplicación.
- El diagnóstico detallado usa una cola asíncrona y escrituras agrupadas, fuera del hilo que cambia la distribución.
- El aviso de edición del alias usa una animación nativa de entrada y salida, reiniciada con cada edición.

### Beta 4

- La edición del alias ya no bloquea el reconocimiento visual mientras el campo conserve foco; solo se inhiben las pulsaciones que escriben texto.
- Diagnóstico detallado opcional, local, circular y anonimizado para investigar identidades HID inesperadas como la observada con `Windows + V`.
- Acciones en Configuración para activar el diagnóstico y abrir su carpeta.
- Tres pruebas nuevas verifican desactivación y anonimización del registro.

### Beta 3

- El feedback del teclado activo ya no interrumpe la edición del alias.
- Dispositivos ordenados por estado funcional; los ignorados quedan al final.
- Corregida la opción vacía que aparecía al desplegar distribuciones.
- Confirmaciones propias con botones y contenedor Fluent redondeados.
- El selector automático recibe permiso de activación y foco inicial.
- Se mantiene pendiente una revisión integral de textos antes de la versión estable.

### Beta 2

- Configuración y selector automático migrados a un frontend WinUI 3 separado y activado bajo demanda.
- Acrylic con fallback a Mica, temas claro/oscuro, tarjetas, controles y barra de título Fluent.
- Iconografía coherente, escalado por monitor y selector agrupado por idioma.
- Menú de bandeja sustituido por un menú Win32 nativo.
- IPC local restringido al usuario; el núcleo conserva la autoridad de preferencias y Raw Input.
- Feedback visual del teclado activo sin interferir con la edición del alias.
- Instalador dual autocontenido por usuario, sin elevación.

### Prototipo WinUI 3 posterior a Beta 1

- Decisión de mantener Raw Input, `NotifyIcon` y coordinación residente en el núcleo WinForms, con un frontend WinUI 3 separado y activado bajo demanda.
- Primer prototipo compilable de Configuración con Windows App SDK 2.2, controles XAML, Mica, tema del sistema, DPI y automatización accesible nativos.
- Edición funcional de alias, distribución, ignorado, olvido y limpieza sobre el modelo `Configuration`, sin sustituir todavía la ventana estable.
- Retirada completa de `DwmExtendFrameIntoClientArea` del camino WinForms; no se simula Acrylic sobre controles GDI.
- Comparación documentada de despliegue autocontenido y runtime compartido, además de arranque y memoria del frontend.

### Beta 1

- Primera versión intermedia pública de la línea 1.5, con identidad persistente, Configuración, alias, dispositivos ignorados, importación/exportación, inicio automático e instalador por usuario.
- Consolidación de las correcciones de Raw Input, combinaciones de teclas, arrastre y dispositivos compuestos desarrolladas durante las alfas.
- Tema claro y oscuro estabilizado con superficies WinForms sólidas; la migración visual completa a WinUI 3 queda pendiente para una entrega posterior.
- El asistente de actualización restaura **Finalizar** en su última página.

### Alpha 9

- El tema de RightKeyboard sigue ahora el modo visible del sistema y de la barra de tareas (`SystemUsesLightTheme`) en configuraciones personalizadas de Windows.
- La preferencia de aplicaciones queda como fallback cuando Windows no informa el modo del sistema.
- Se desactiva el backdrop DWM sobre ventanas WinForms: la composición GDI producía controles transparentes en modo claro. La interfaz usa fondos sólidos hasta su migración a WinUI 3.
- Etiquetas y casillas usan el fondo opaco de su superficie contenedora para asegurar su repintado.
- Se incorpora a la planificación un diagnóstico circular y opcional para pruebas físicas, sin registrar el contenido de las teclas.

### Alpha 8

- Eliminación del segundo motor de tema que repintaba controles WinForms después de aplicar la paleta de RightKeyboard y producía texto blanco sobre superficies claras.
- La barra de título, los bordes y los materiales siguen el tema mediante DWM; toda el área cliente queda bajo una única capa de color.
- Nueva prueba automatizada de contraste para etiquetas, campos y botones en modo claro.

### Alpha 7

- Corrección del tema claro: la paleta propia usa ahora la misma fuente de tema que WinForms, evitando combinar superficies claras con texto oscuro heredado.
- El flujo de actualización cambia también el título de la página, su explicación y el botón principal a **Actualizar**.
- Se documenta que una tecla modificadora aislada todavía no inicia la configuración de un teclado nuevo; resolverlo exige distinguir su liberación de una combinación para no recuperar los disparos accidentales originales.

### Alpha 6

- Corrección del contraste en tema claro y actualización en caliente de la paleta completa.
- Corrección del cierre redondeado derecho y de la altura uniforme del resaltado del menú de bandeja.
- Identificación visual del teclado pulsado dentro de Configuración mediante la indicación **Pulsado ahora**.
- Apertura diferida del selector para impedir que la tecla que lo dispara se escriba en el alias.
- Terminología explícita de actualización en el instalador cuando detecta una versión previa, conservando preferencias e inicio automático.
- La interfaz WinForms actual queda tratada como transición: la adopción genuina de Fluent y Acrylic requiere migrar las ventanas a WinUI 3 antes de cerrar 1.5.

### Alpha 5

- Corrección del repintado acumulativo y de la alineación vertical del menú de bandeja.
- Sustitución de la transparencia GDI inestable del menú por una paleta Fluent sólida y completamente opaca.
- Extensión correcta del marco DWM al área cliente para que Mica y Mica Alt puedan mostrarse alrededor de las tarjetas.
- Actualización inmediata de ventanas, controles, tarjetas y menú al cambiar entre tema claro y oscuro.
- Incorporación de la prueba física de alpha 4 como regresión documentada.

### Alpha 4

- Adopción incremental de Fluent Design sin cargar Windows App SDK en el proceso residente.
- Mica para Configuración, Mica Alt para el selector y primera evaluación de Desktop Acrylic para el menú de bandeja mediante APIs DWM documentadas.
- Fallback sólido en Windows 10, contraste alto, transparencia desactivada, ahorro de batería y entornos virtualizados.
- Tipografía Segoe UI Variable con fallback, esquinas redondeadas, tarjetas y acciones principales con jerarquía coherente con Windows 11.
- Acción **Limpiar preferencias** dentro de Configuración, compartiendo confirmación, persistencia y errores con la bandeja.

### Alpha 3

- Integración de los frentes de identidad, preferencias, interfaz, instalación y calidad sobre una sola rama.
- Lectura de Raw Input sin asignaciones nativas por pulsación y actualización agrupada del inventario de dispositivos.
- Validación estricta e importación transaccional de preferencias, con advertencias para distribuciones ausentes.
- Mejoras de DPI, temas, accesibilidad, foco y navegación por teclado en las interfaces de Windows.
- Instancia única, cierre ordenado para actualizaciones y scripts de instalación por usuario con publicación autocontenida.
- Compilación Release sin errores ni advertencias y sesenta pruebas automatizadas correctas en Windows x64.

### Validación en curso

- La publicación autocontenida, el cierre coordinado y la compilación con Inno Setup 6.7.3 están verificados.
- Los instaladores de las alfas se generan junto con su archivo SHA-256.
- Continúan pendientes las pruebas físicas de hardware, cambio de sesión, actualización y desinstalación.

### Alpha 2

- Nuevo esquema 3 de `preferences.json` con alias, nombre detectado, identificador técnico y última detección.
- Migración automática desde el esquema 2 de la primera alpha.
- Ventana **Configuración** para editar dispositivos conectados y desconectados.
- Cambio selectivo de alias, distribución y estado ignorado, además de olvidar dispositivos individuales.
- Exportación e importación JSON con modos combinar/reemplazar y respaldo automático.
- Validación estricta del esquema 3, rechazo de versiones incompatibles e importación transaccional con advertencias de distribuciones ausentes.
- Control del inicio con Windows desde la configuración.
- Selector rediseñado con grupos y filas seleccionables en lugar del `TreeView` clásico.
- Campo de nombre para identificar teclados que Windows reporta sin nombre.
- Menú de bandeja ampliado con **Configuración** y un renderer visual propio.

### Añadido

- Identificación visible del dispositivo mediante propiedades Plug and Play.
- Selector jerárquico que agrupa las distribuciones debajo de cada idioma.
- Acción **Ignorar este dispositivo** para periféricos compuestos que emiten eventos de teclado.
- Clasificación automática conservadora de mouse, trackballs y touchpads claramente identificables.
- Persistencia de dispositivos ignorados y huellas de modelo en `preferences.json`.
- Notificaciones de conexión y desconexión mediante Raw Input.

### Cambiado

- Recuperación de preferencias tras una reconexión cuando la identidad exacta cambia y la huella coincide sin ambigüedad.
- Migración automática del archivo `config.txt` de la versión 1.4.
- Selector rediseñado con espaciado, accesibilidad, escalado y controles acordes con Windows 11.

## [1.4.0] - 2026-06-29

### Cambios

- Migración desde el formato histórico de .NET Framework a .NET 10 LTS y proyectos SDK.
- Actualización de NUnit, el adaptador de pruebas y Microsoft.NET.Test.Sdk.
- Sustitución del formulario principal oculto por un contexto de aplicación y una ventana de mensajes liviana.
- Traducción de la documentación, del selector y del menú del área de notificación.
- Cambio del menú a las acciones **Limpiar preferencias** y **Salir**.

### Correcciones

- Se ignoran liberaciones de tecla, teclas modificadoras y eventos falsos al decidir si debe abrirse el selector.
- Las colecciones HID pertenecientes al mismo teclado comparten preferencia mediante el identificador de contenedor de Windows.
- Se corrige la lectura del identificador de hilo de la ventana activa, que antes se truncaba a 16 bits.
- El cambio de distribución se dirige a la ventana activa y deja de modificar el idioma predeterminado global en cada pulsación.
- **Limpiar preferencias** persiste el borrado inmediatamente.

### Validación

- Compilación de lanzamiento con .NET 10 sin errores ni advertencias.
- Doce pruebas automatizadas correctas.
- Prueba funcional satisfactoria en una máquina host con varios teclados.
- Confirmado que la función principal permanece intacta y que no se reproducen los falsos selectores iniciales con combinaciones, teclas `Fn` ni interacciones de arrastrar y soltar.

## [1.3.0] - 2020-06-12

- Última versión de la línea heredada recibida por este fork.
- Refactorización inicial para separar responsabilidades del formulario principal.
