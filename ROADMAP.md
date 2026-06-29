# Hoja de ruta

## Versión 1.5.0 — en desarrollo

La versión 1.5 se concentrará en identificar mejor cada teclado, conservar sus preferencias tras una reconexión y mejorar el selector sin aumentar innecesariamente el consumo del proceso en segundo plano.

La primera prueba host de `1.5.0-alpha.1` confirmó el funcionamiento y aportó observaciones visuales y de administración. La especificación detallada para continuar está en [docs/continuacion-1.5.md](docs/continuacion-1.5.md).

### Identificación visible del teclado

El selector debe indicar qué teclado generó la pulsación. La información se obtendrá de propiedades Plug and Play compatibles con Windows, priorizando:

1. nombre comunicado por el bus o nombre amigable;
2. fabricante y modelo cuando estén disponibles;
3. identificador de contenedor abreviado como dato técnico secundario;
4. una descripción genérica cuando el dispositivo no publique metadatos.

No se mostrará la ruta HID completa como texto principal ni se interpretará su formato interno.

**Criterios de aceptación**

- El diálogo muestra un nombre que permite distinguir razonablemente el teclado pulsado.
- Si Windows no entrega un nombre, se muestra un identificador breve que puede compararse tras una reconexión.
- La obtención de metadatos no bloquea el procesamiento normal de teclas.

### Preferencias persistentes tras desconectar y reconectar

La asociación no dependerá del identificador temporal de Raw Input. Se evaluará una identidad persistente compuesta por propiedades Plug and Play, con el `ContainerId` como primera opción y una huella estable de propiedades del dispositivo como alternativa para teclados que no publican un contenedor persistente.

**Criterios de aceptación**

- Desconectar y volver a conectar el mismo teclado no abre nuevamente el selector.
- Reconectarlo en otro puerto conserva la preferencia cuando el hardware proporciona una identidad suficiente.
- Dos teclados iguales conectados simultáneamente pueden mantener preferencias distintas si Windows permite distinguir sus instancias.
- La configuración 1.4 se migra sin perder asociaciones que todavía puedan resolverse.

### Exclusión de periféricos que se presentan como teclado

Algunos periféricos compuestos, como mouse con botones o combinaciones avanzadas, publican una colección HID de teclado aunque su función principal no sea escribir. Se investigarán las capacidades HID y propiedades Plug and Play del contenedor para descartarlos antes de abrir el selector cuando la clasificación sea inequívoca.

Como mecanismo de respaldo, el selector ofrecerá la acción **Ignorar este dispositivo**. La decisión se guardará con la misma identidad persistente utilizada por las asociaciones de teclado. No se añadirá una tercera opción al menú del área de notificación: **Limpiar preferencias** borrará tanto las asociaciones como la lista de dispositivos ignorados.

**Criterios de aceptación**

- Un periférico claramente clasificable como no-teclado no abre el selector.
- Un dispositivo ambiguo puede marcarse como ignorado desde el propio selector.
- Un dispositivo ignorado no vuelve a abrir el selector tras reiniciar la aplicación, desconectarlo o cambiarlo de puerto cuando su identidad sea estable.
- **Limpiar preferencias** permite recuperar un dispositivo ignorado por error.
- Las reglas automáticas son conservadoras: un teclado real no se excluye solo por pertenecer a un dispositivo compuesto.
- El comportamiento se valida expresamente con periféricos compuestos, incluyendo un Logitech MX Master 3S o un dispositivo equivalente.

### Selector jerárquico de distribuciones

La lista plana se reemplazará por un árbol con esta estructura:

```text
español (Chile)
├─ Latin American
└─ Spanish
español (México)
└─ Latin American
español (España)
└─ Latin American
```

El modelo de datos separará el nombre del idioma del nombre de la distribución, en vez de construir una sola cadena con ambos valores.

**Criterios de aceptación**

- Los idiomas son nodos de primer nivel y no pueden aceptarse como selección final.
- Las distribuciones aparecen como nodos hijos y pueden seleccionarse con doble clic o con **Aceptar**.
- El orden es alfabético y la selección inicial es predecible.
- Navegación completa mediante teclado y lector de pantalla.

### Ajuste visual para Windows 11

Se conservará WinForms y el proceso liviano. No se incorporará WinUI 3 solo para este diálogo. La actualización contemplará:

- espaciado y dimensiones acordes con Windows 11;
- jerarquía tipográfica más clara;
- encabezado con nombre del teclado y texto explicativo;
- `TreeView` con iconos discretos para idioma y distribución;
- escalado correcto por monitor y contraste compatible con temas del sistema;
- botones alineados y una acción de cancelación explícita.

La interfaz visual se cargará únicamente cuando sea necesario configurar un teclado, por lo que no debe aumentar de manera material el consumo en reposo.
