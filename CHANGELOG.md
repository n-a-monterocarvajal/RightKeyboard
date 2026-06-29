# Registro de cambios

Todos los cambios relevantes del proyecto se documentan en este archivo y se describen en español.

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
