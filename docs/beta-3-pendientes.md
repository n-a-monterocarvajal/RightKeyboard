# Pendientes para RightKeyboard 1.5.0-beta.3

Registrados durante la validación física de `1.5.0-beta.2`.

## Selector de distribución — corregido en beta 3

- Corregido el primer elemento vacío que aparecía con un fondo rojizo al desplegar la lista de distribuciones en Configuración.
- Se revisaron las plantillas y estados básicos; contraste claro/oscuro continúa dentro de la validación física.

## Configuración y actividad de teclado — corregido en desarrollo

- Mientras el campo de alias tiene foco, el feedback de Raw Input ya no cambia selección, desplaza la lista ni impide la escritura.
- Los dispositivos se ordenan por utilidad: conectados configurados, conectados sin configurar, desconectados configurados, desconectados sin configurar e ignorados al final; dentro de cada grupo, por nombre.

## Diálogos — corregido en beta 3

- Las confirmaciones usan ahora una capa modal propia con botones y contenedor Fluent redondeados.

## Activación del selector automático — corregido en beta 3

- El núcleo transfiere permiso de primer plano al frontend; el selector se restaura, activa y enfoca el campo de nombre.
- No se usa modo siempre visible.

## Criterio de cierre

Repetir estos casos en tema claro y oscuro, 100/125/150 % de escala y al menos dos equipos físicos antes de promover `1.5.0` estable.

## Revisión de textos — pendiente posterior

- Auditar títulos, ayudas y estados de toda la interfaz para eliminar formulaciones excesivamente descriptivas, técnicas o «tipo máquina».
- Priorizar microcopy breve y orientado a la acción, sin perder información necesaria para diagnóstico y accesibilidad.
- Revisar conjuntamente Configuración, selector, confirmaciones, mensajes de error, instalador y menú de bandeja antes de `1.5.0` estable.
