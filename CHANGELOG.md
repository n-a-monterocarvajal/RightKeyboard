# Registro de cambios

Todos los cambios relevantes del proyecto se documentan en este archivo y se describen en español.

## [1.5.0] - En desarrollo

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
