# Registro de cambios

Todos los cambios relevantes del proyecto se documentan en este archivo y se describen en español.

## [1.5.0] - En desarrollo

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

### Validación en curso

- Compilación Release con .NET SDK 10.0.301 sin errores ni advertencias.
- Veinticinco pruebas automatizadas correctas en Windows x64.
- La certificación física, el instalador por usuario y los artefactos de publicación siguen pendientes; este estado no corresponde todavía a una candidata de lanzamiento.

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
