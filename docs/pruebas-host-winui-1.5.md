# Pruebas del prototipo WinUI 3 en host

Consultar también [las limitaciones conocidas](limitaciones-conocidas-1.5.md), especialmente primera pulsación, edición de alias y cambio de puerto USB.

Ejecutar `scripts\build-winui-prototype.ps1` y abrir `RightKeyboard.WinUI.exe` fuera de una VM cuando se evalúen materiales.

## Ventana y materiales

- En Windows 11 con transparencia activa, confirmar Mica visible alrededor de las tarjetas y esquinas nativas sin píxeles GDI residuales.
- Desactivar transparencia y activar ahorro de batería: la ventana debe usar el fallback sólido de WinUI sin perder contraste.

## Bandeja y panel de iconos ocultos

- Abrir el panel de iconos ocultos y hacer clic secundario en RightKeyboard.
- Mover el puntero entre **Configuración** y **Salir**: el panel del sistema no debe contraerse mientras se usa el menú.
- Cerrar el menú con `Esc` y haciendo clic fuera; no deben quedar ventanas auxiliares ni zonas de hover persistentes.
- Repetir en tema claro, oscuro y con escalado de 125 % o superior. El menú debe conservar el estilo nativo que entregue Windows.
- Cambiar entre tema claro, oscuro y contraste alto con la ventana abierta; textos, campos, selección, foco y diálogos deben actualizarse.
- Repetir en Windows 10 1809 o posterior: debe abrir con fondo sólido y conservar toda la funcionalidad del prototipo.

## DPI, teclado y accesibilidad

- Mover la ventana entre monitores al 100 %, 150 % y 200 %; verificar reescalado sin recortes y tamaño mínimo utilizable.
- Recorrer lista, alias, distribución, ignorado y acciones con `Tab`, `Mayús+Tab`, flechas, `Entrar` y `Esc`.
- Guardar con `Ctrl+S` y comprobar que el foco no se pierde después de refrescar la lista.
- Con Narrador, confirmar que se anuncian alias, conexión, distribución/ignorado, propósito de campos y botones.

## Datos y aislamiento

- Usar una copia de `preferences.json`; no abrir simultáneamente Configuración WinForms y el prototipo mientras no exista IPC.
- Editar alias, distribución e ignorado; cerrar, reabrir y comprobar persistencia.
- Cancelar y aceptar por separado **Olvidar dispositivo** y **Limpiar preferencias**.
- Cerrar el frontend y confirmar que `RightKeyboard.WinUI.exe` termina mientras `RightKeyboard.exe`, Raw Input y el icono de bandeja continúan activos.
- Simular ausencia o fallo del frontend en la fase de integración: el núcleo deberá abrir Configuración WinForms como fallback.

## Rendimiento y despliegue

- Medir arranque frío y caliente desde la acción de bandeja hasta la primera ventana visible.
- Registrar working set, memoria privada, hilos y GPU con la ventana inactiva y durante cambios de tema.
- Comparar publicaciones `SelfContained` y `FrameworkDependent`, incluyendo el tamaño e instalación del runtime compartido.
- Probar instalación, actualización y desinstalación con ambos ejecutables cuando el frontend se incorpore al instalador.
