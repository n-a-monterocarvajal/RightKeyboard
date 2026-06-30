# Pruebas visuales de la interfaz 1.5

Ejecutar esta lista en un host Windows 11 con al menos un teclado sin preferencia guardada.

## Escalado y temas

- Probar al 100 %, 150 % y 200 %; mover cada diálogo entre monitores con DPI distinto y confirmar que texto, botones y bordes se reescalan sin recortes.
- Repetir en tema claro, oscuro y contraste alto; comprobar texto legible, foco visible y selección distinguible.

## Materiales en host físico

- Usar Windows 11 22H2 o posterior, fuera de una VM: Configuración debe mostrar Mica y el selector Mica Alt; al desactivar transparencia ambos deben conservar un fondo sólido legible.
- Abrir el menú de bandeja sobre fondos claros y oscuros: debe mostrar Desktop Acrylic mientras está abierto y un color sólido equivalente si Windows desactiva el efecto.
- Activar y desactivar cada ventana; Mica debe reflejar el estado activo sin parpadeos ni zonas transparentes sin pintar.
- Comprobar esquinas sin maximizar ni acoplar la ventana. Repetir maximizada y acoplada, donde Windows puede retirar el redondeo por diseño.
- Repetir con ahorro de batería y contraste alto; no debe forzarse transparencia y todos los textos, bordes y focos deben conservar contraste.
- Ejecutar en Windows 10 22H2: no debe haber Mica/Acrylic, pero la interfaz, el tema, DPI, teclado y lector de pantalla deben seguir operativos.
- Registrar compilación de Windows, GPU, estado de transparencia y capturas. En VM, ausencia de transparencia o esquinas no implica fallo si el fallback es sólido.

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
- Usar **Limpiar preferencias** dentro de Configuración: cancelar debe conservar todos los datos; aceptar debe vaciar alias, distribuciones e ignorados y persistir exactamente igual que la bandeja.

## Bandeja

- Confirmar este orden exacto: Configuración, Limpiar preferencias, separador y Salir.
- Abrir el menú con teclado, recorrerlo con flechas y comprobar el resaltado en los tres temas.
- Hacer doble clic en el icono y confirmar que abre Configuración una sola vez.
- Elegir «Limpiar preferencias», cancelar y comprobar que no cambia nada; repetir aceptando solo con datos de prueba.
