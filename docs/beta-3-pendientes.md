# Pendientes para RightKeyboard 1.5.0-beta.3

Registrados durante la validación física de `1.5.0-beta.2`.

## Selector de distribución

- Corregir el primer elemento vacío que aparece con un fondo rojizo al desplegar la lista de distribuciones en Configuración.
- Revisar plantillas, estados de selección y contraste del selector en temas claro y oscuro.

## Configuración y actividad de teclado — corregido en desarrollo

- Mientras el campo de alias tiene foco, el feedback de Raw Input ya no cambia selección, desplaza la lista ni impide la escritura.
- Los dispositivos se ordenan por utilidad: conectados configurados, conectados sin configurar, desconectados configurados, desconectados sin configurar e ignorados al final; dentro de cada grupo, por nombre.

## Diálogos

- Aplicar radios Fluent a los botones internos **Limpiar** y **Cancelar** del diálogo de confirmación. El contenedor ya está redondeado, pero los botones generados por `ContentDialog` conservan esquinas rectas.

## Activación del selector automático

- La ventana WinUI del flujo disparado por una tecla aparece al frente, pero puede no adquirir activación y foco inmediatamente.
- Validar `AppWindow`, restauración, `SetForegroundWindow`/permisos de activación entre procesos y foco inicial sobre el campo de nombre o la lista.
- No usar modo siempre visible ni robar foco cuando el selector se abra por una acción manual.

## Criterio de cierre

Repetir estos casos en tema claro y oscuro, 100/125/150 % de escala y al menos dos equipos físicos antes de promover `1.5.0` estable.
