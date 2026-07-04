# Pendientes para RightKeyboard 1.5.0 beta 6

Observaciones registradas durante la validación física de `1.5.0-beta.5`. No están implementadas todavía.

## Bloqueo al editar el alias — corregido en desarrollo

- `1.5.0-beta.5` podía cerrarse al primer cambio del campo «Nombre para este teclado» porque intentaba detener la animación del aviso antes de haber iniciado su `Storyboard`.
- La animación mantiene ahora estado explícito: solo se detiene después de iniciarse y puede reiniciarse con cada edición.

## Capa modal de «Limpiar preferencias»

- La capa de oscurecimiento debe cubrir toda la superficie cliente, incluida la barra de título personalizada y el pie de la ventana, sin dejar franjas visualmente activas.
- Revisar la relación entre `ExtendsContentIntoTitleBar`, la cuadrícula raíz y el área reservada para los botones nativos de la ventana.
- Incorporar una transición Fluent de entrada y salida: atenuación de la capa y aparición/desaparición suave del panel, preferentemente combinando opacidad y una escala muy leve.
- La animación de cierre debe completarse antes de retirar el overlay del árbol visual.
- Mantener bloqueo de interacción, foco contenido dentro del diálogo, Escape para cancelar y compatibilidad con la preferencia de animaciones reducidas de Windows.
- Validar el resultado en temas claro y oscuro, Acrylic/Mica/fallback sólido y escalas 100/125/150 %.

## Versión en la barra de título

- Añadir debajo de «RightKeyboard» una segunda línea de menor jerarquía con la versión efectiva de la aplicación, incluidas las etiquetas preliminares; por ejemplo, `1.5.0 beta 6`.
- Obtener el texto desde los metadatos del ensamblado o una única fuente de versión, sin duplicar una cadena manual en la interfaz.
- Usar tipografía y contraste secundarios, manteniendo alineación con el icono y espacio suficiente respecto de los controles nativos de minimizar, maximizar y cerrar.
- Aplicar el mismo patrón a Configuración y al selector automático para conservar coherencia.

## Identificación del teclado activo

- Ampliar «Entrada detectada» con el identificador técnico corto del dispositivo para distinguir teclados que todavía comparten el nombre «Teclado sin nombre».
- Formato previsto: `Entrada detectada: Teclado sin nombre · Dispositivo 62D6EDB4`.
- No mostrar la ruta PnP completa ni otros identificadores extensos o sensibles.
- Conservar el alias como información principal cuando exista, usando el identificador corto como referencia secundaria estable dentro de la sesión.

## Microcopia de Configuración

- Sustituir «Administra los teclados conocidos sin interrumpir la detección en segundo plano.» por «Administra los teclados detectados.»
- Incluir este cambio dentro de la revisión general de textos pendiente antes de la versión estable.
