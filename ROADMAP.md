# Hoja de ruta

`1.5.0` es la versión estable inicial de la línea 1.5. Los documentos históricos de alpha/beta se conservan en `docs/`, pero este archivo resume el backlog vigente.

## 1.5.1 — robustez de detección y mantenimiento

La siguiente actualización debería concentrarse en mejorar los casos donde un periférico HID se presenta como teclado sin serlo y en reducir trabajo de diagnóstico manual.

### Robustecer diagnóstico y logs

- Revisar qué campos se registran para que el diagnóstico siga siendo útil sin exponer contenido de teclas.
- Añadir señales suficientes para entender falsos positivos de periféricos como presentadores USB, mouse avanzados o interfaces virtuales.
- Mantener el diagnóstico como herramienta de compilación de desarrollo, no como flujo de usuario final.

### Mejorar detección preventiva de no-teclados

- Evaluar reglas conservadoras por firma HID parcial: `VID`, `PID`, interfaz, colección, enumerador y capacidades Raw Input.
- No bloquear automáticamente teclados reales por coincidencias débiles.
- Si una firma se ignora manualmente, estudiar que esa decisión sobreviva a cambios de ruta o puerto USB cuando sea seguro.

### Agrupar identidades del mismo dispositivo

Cuando Windows entrega identidades distintas para el mismo teclado al cambiar de puerto USB, la UI podría permitir anidar o fusionar manualmente esas identidades bajo un mismo dispositivo lógico.

**Criterios iniciales:**

- El usuario puede seleccionar dos o más identidades conocidas y tratarlas como el mismo teclado.
- La asociación de distribución, alias e ignorado se administra en el dispositivo lógico.
- La operación es reversible.
- La app nunca fusiona automáticamente dispositivos ambiguos sin intervención del usuario.

## Observaciones sobre la Configuración WinUI

Revisión del ejecutable de `1.5.0` del 19 de julio de 2026. Cada punto indica qué se verificó en el código.

### Pulido visual

- **Botón Recargar sin icono.** Es un `Button` con texto (`SettingsWindow.xaml.cs`). Debería usar un icono del sistema, conservando nombre accesible y descripción emergente.
- **Casillas de verificación con ángulos rectos.** Los `CheckBox` son los únicos controles que no fijan `CornerRadius`; el resto usa `CornerRadius(8)`. El radio del glifo no proviene de la propiedad del control sino del recurso de tema `CheckBoxCornerRadius`, así que hay que sobrescribir el recurso y no la propiedad.

### Abandono de edición sin guardar

`DeviceList_SelectionChanged` reescribe el editor con la fila nueva sin comprobar nada, y no existe seguimiento de estado sucio en la ventana. Seleccionar otro dispositivo, o cerrar la ventana, descarta alias, distribución e ignorado en silencio. Falta detectar el estado sucio y confirmar antes de perderlo.

### Relación entre Ignorar y Agrupar

La dependencia existe y es unidireccional: ignorar bloquea agrupar en ambos sentidos. Como origen, `SetEditorEnabled` deshabilita el desplegable de agrupación cuando la fila está ignorada; como destino, `CanBeGroupTarget` excluye las filas ignoradas. Por tanto **Ignorar es una precondición y su posición sobre Agrupar es correcta**, pero hoy queda por debajo de Distribución, a la que también gobierna. Conviene situar Ignorar antes de todo lo que condiciona.

Hay además un defecto real: `IgnoredCheckBox_Changed` solo reevalúa `LayoutComboBox.IsEnabled`. Al marcar Ignorar, los controles de agrupación siguen habilitados hasta reseleccionar la fila, de modo que se puede iniciar una agrupación que el núcleo rechaza.

### Pulsaciones sintéticas y falsos dispositivos

Por explorar. `API.TryReadKeyboardEvent` ya descarta los eventos con `Header.Device == 0`, que es el caso habitual de la entrada inyectada con `SendInput`, así que el portapapeles de Windows no debería crear un dispositivo fantasma. `RawKeyboardEvent.HasExtraInformation` se captura pero solo se registra en diagnóstico: no filtra nada. Queda por verificar el caso de teclados virtuales que sí se enumeran como HID real (escritorio remoto, automatización, teclado en pantalla) y decidir si deben poder llegar al selector.

### Nomenclatura de la lista

El encabezado del panel izquierdo debe decir «Dispositivos detectados». La primera revisión lo descartó por error, buscando la cadena exacta en lugar del término: el commit `0bf1758` (4 de julio de 2026, beta 6) ya había cambiado el subtítulo a «Administra los teclados detectados.» y dejó el encabezado sin actualizar. El cambio de microcopia está registrado en `docs/beta-6-pendientes.md`.

«Detectados» describe correctamente el contenido: la lista incluye todo lo que la aplicación ha visto alguna vez, porque `TouchDevice` da de alta la preferencia al enumerar el dispositivo. Por eso comprende desconectados, ignorados y dispositivos que el usuario nunca configuró. Corregir también el diálogo WinForms de respaldo.

Asunto contiguo: en WinUI «Ignorado» sustituye a Conectado/Desconectado, mientras que en WinForms son ejes independientes (`DevicePresentation`). Hoy no se puede saber si un dispositivo ignorado está conectado.

## Orden de ejecución

El orden en que se abordan estos pendientes, junto con la validación física, las mediciones y la licencia, está en [Plan camino a 1.6.0](docs/plan-1.6.0.md), que además lleva el estado de cada etapa.

## Pendientes funcionales de la línea 1.5

- Medir de forma reproducible la latencia de apertura de Configuración y del selector.
- Revisar microtextos de la UI que aún suenan excesivamente técnicos.
- Resolver/documentar la licencia heredada del fork antes de ampliar contribuciones externas.

## Referencias históricas

- [Continuación 1.5](docs/continuacion-1.5.md): especificación y observaciones iniciales de alpha.
- [Criterios WinUI 3](docs/criterios-winui3-1.5.md): contrato técnico usado durante la migración.
- [Calidad 1.5](docs/calidad-1.5.md): matriz de validación acumulada.
- [Notas de releases](docs/releases/): bitácora beta y estable.

## Alcance consolidado en 1.5.0

La versión 1.5 se concentró en identificar mejor cada teclado, conservar sus preferencias tras una reconexión y mejorar el selector sin aumentar innecesariamente el consumo del proceso en segundo plano.

El alcance incluyó además un [instalador por usuario](docs/distribucion-1.5.md) sin elevación, inicio automático activado por defecto, una única raíz de archivos administrados por RightKeyboard y una nueva Configuración WinUI.

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
