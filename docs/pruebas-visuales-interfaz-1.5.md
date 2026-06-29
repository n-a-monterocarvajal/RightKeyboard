# Pruebas visuales de la interfaz 1.5

Ejecutar esta lista en un host Windows 11 con al menos un teclado sin preferencia guardada.

## Escalado y temas

- Probar al 100 %, 150 % y 200 %; mover cada diálogo entre monitores con DPI distinto y confirmar que texto, botones y bordes se reescalan sin recortes.
- Repetir en tema claro, oscuro y contraste alto; comprobar texto legible, foco visible y selección distinguible.

## Selector de distribución

- Confirmar que el alias aparece seleccionado y con foco al abrir.
- Recorrer alias, distribuciones y acciones con `Tab`, `Mayús+Tab` y flechas; aceptar con `Entrar` y cancelar con `Esc`.
- Redimensionar hasta el mínimo y verificar que el identificador, la ayuda y las acciones siguen completos; la lista debe desplazarse.
- Probar «Ignorar este dispositivo», cancelar primero la confirmación y luego aceptarla; el dispositivo debe quedar recuperable en Configuración.

## Configuración

- Verificar tarjetas para dispositivos conectados, desconectados, ignorados y sin distribución; un alias largo no debe ocultar el estado.
- Editar alias, distribución y estado ignorado; guardar con el botón y con `Ctrl+S`, cerrar y volver a abrir para comprobar persistencia.
- Confirmar que un dispositivo desconectado sigue siendo editable y que «Olvidar dispositivo» conserva la preferencia al cancelar.
- Navegar toda la ventana solo con teclado y revisar con Narrador que alias, estado, preferencia y propósito de cada control se anuncian claramente.
- Reducir la ventana al mínimo y comprobar que «Exportar», «Importar», inicio con Windows y «Cerrar» no se superponen ni se recortan.

## Bandeja

- Confirmar este orden exacto: Configuración, Limpiar preferencias, separador y Salir.
- Abrir el menú con teclado, recorrerlo con flechas y comprobar el resaltado en los tres temas.
- Hacer doble clic en el icono y confirmar que abre Configuración una sola vez.
- Elegir «Limpiar preferencias», cancelar y comprobar que no cambia nada; repetir aceptando solo con datos de prueba.
