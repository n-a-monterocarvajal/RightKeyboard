# Contribuir a RightKeyboard

## Idioma

Usamos español para:

- documentación y comentarios que expliquen decisiones del proyecto;
- mensajes de la interfaz y errores visibles para el usuario;
- descripciones de commits, ramas, incidencias y solicitudes de cambio;
- notas de versión y registro de cambios.

Los nombres de tipos, métodos y APIs permanecen en inglés para seguir las convenciones de .NET y Win32.

## Comprobaciones antes de proponer un cambio

```powershell
dotnet build RightKeyboard.sln --configuration Release
dotnet test RightKeyboard.sln --configuration Release
```

Los cambios relacionados con entrada deben probarse, como mínimo, con:

- pulsación y liberación de teclas;
- combinaciones con `Ctrl`, `Alt`, `Shift`, `Windows` y `Fn`;
- escritura mientras se arrastran elementos con el mouse;
- dos teclados físicos con distribuciones distintas;
- suspensión, reanudación y reconexión de un teclado.

No se deben interpretar ni recortar manualmente las rutas internas de dispositivos HID. Windows las define como identificadores opacos; para agrupar funciones del mismo dispositivo se utiliza su `ContainerId`.
