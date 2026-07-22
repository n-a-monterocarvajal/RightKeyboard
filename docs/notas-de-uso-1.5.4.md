# Notas de uso — RightKeyboard 1.5.4

Observaciones recogidas al usar la versión 1.5.4. No son notas de publicación (esas viven, inmutables, en `docs/releases/`) ni bugs ya triados: son un cuaderno de campo que un agente puede leer cuando busca pendientes y articular en un plan cuando se requiera.

Cada punto describe lo observado, lo que se sabe del código y lo que quedaría por decidir o hacer. Al convertir uno en trabajo real, trasládese al backlog (`.agent-context/05-siguientes-pasos.md`), a `ROADMAP.md` o a `docs/plan-1.6.0.md` según corresponda, y déjese aquí la referencia.

## 1. El desplegable de «Agrupar con otra identidad» aparece vacío

**Tipo:** defecto. La función de agrupación manual no puede utilizarse.

**Síntoma:** en la Configuración WinUI de 1.5.4 el desplegable «Agrupar con otra identidad» se muestra vacío, sin candidatos seleccionables.

**Sospecha inicial:** revisar el filtrado de candidatos endurecido en 1.5.3 —una identidad ignorada dejó de poder agrupar o aparecer como destino— y la construcción de la lista de destinos en `SettingsWindow` (poblado de `GroupTargetComboBox`, condición `CanBeGroupTarget`) y su acción IPC. Confirmar si el filtro excluye de más o si la lista sencillamente no se está poblando.

**Pendiente:** reproducir con al menos dos identidades no ignoradas, localizar dónde se arma la lista de destinos y validar el arreglo con hardware o identidades simuladas.

## 2. Indicador gráfico de conexión en «Dispositivos detectados»

**Tipo:** mejora de interfaz.

1.5.4 separó en cada fila la conexión, el estado «Ignorado» y la distribución como texto. Falta un elemento gráfico que distinga de un vistazo «Conectado» de «Desconectado»: se propone una «bolita» verde o roja junto a la palabra, según corresponda, tanto en la Configuración WinUI como en el respaldo WinForms.

**Accesibilidad:** el indicador no debe depender solo del color. Conservar el texto de estado y su anuncio para el lector de pantalla, de forma coherente con la separación introducida en 1.5.4.

## 3. Evaluar la agrupación de identidades ignoradas

**Tipo:** decisión de diseño a evaluar.

Hoy la agrupación manual excluye por diseño a las identidades ignoradas: nunca se plantea el caso de «Agrupar» miembros ignorados (ver la restricción «No se admiten miembros ignorados» de la Etapa 6 y el filtrado endurecido en 1.5.3). Es una decisión de nuestra lógica, no una limitación del hardware.

**Caso a evaluar:** un mismo dispositivo ignorado que reaparece con identidades técnicas distintas al conectarse en puertos diferentes en ocasiones diversas —por ejemplo el presentador que se enchufa a dos puertos distintos—. Sin poder agruparlas, cada puerto se ignora por separado y la intención de «ignorar este dispositivo» no se traslada a todas sus identidades.

**Pendiente:** decidir si tiene sentido permitir agrupar identidades ignoradas (o propagar el estado ignorado a un grupo lógico), definir la semántica de alias/distribución/estado para un grupo cuyos miembros están ignorados, y confirmar que no reintroduce selectores falsos ni fusiones automáticas. Relacionado con el punto 1.

## 4. Incluir un actualizador en la app

**Tipo:** capacidad nueva.

Hoy la actualización es manual: descargar y ejecutar el instalador de la nueva versión. Se propone un mecanismo dentro de la app que consulte la última Release publicada, avise al usuario y facilite la descarga/instalación.

**A definir:** origen de la comprobación (por ejemplo, la API de Releases de GitHub), cadencia y opt-in; verificación de integridad con el SHA-256 que ya se publica por versión; y cómo convive con el instalador de Inno Setup y la sustitución de la carpeta compartida del núcleo y la Configuración WinUI. Respetar la restricción CPOL 5(d): la distribución debe seguir siendo gratuita.

## 5. Regla de orden «Conectados arriba» en la lista

**Tipo:** mejora de comportamiento.

La lista tiene una jerarquía lógica implementada en `DeviceSortRank` (`RightKeyboard.WinUI/SettingsWindow.xaml.cs`). El orden actual, de arriba abajo, es: conectado y configurado (0), conectado y sin configurar (1), desconectado y configurado (2), desconectado y sin configurar (3) e **ignorado siempre al final (4), esté conectado o no**.

**Propuesta:** que la conexión sea la clave primaria del orden para *todos* los estados, y que dentro de cada bloque se conserve la jerarquía lógica (conocido y configurado, detectado y sin configurar, ignorado). Resultado, de arriba abajo:

1. Conectado y configurado
2. Conectado y sin configurar
3. Conectado e ignorado
4. Desconectado y configurado
5. Desconectado y sin configurar
6. Desconectado e ignorado

Así, por ejemplo, un ignorado conectado aparece por encima de un conocido y configurado desconectado. El cambio respecto al comportamiento actual es que «ignorado» deja de ir siempre al fondo: pasa a ordenarse primero por conexión y solo después por su rango lógico.

**Pendiente:** ajustar `DeviceSortRank` (los grupos lógicos conservan su ordenación por nombre; revisar cómo hereda el rango un grupo con miembros en estados distintos) y validar visualmente con dispositivos conectados y desconectados en cada categoría.
