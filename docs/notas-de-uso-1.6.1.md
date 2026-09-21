# Notas de uso — RightKeyboard 1.6.1

Observaciones recogidas al usar 1.6.1 ya publicada. No son notas de publicación (esas viven, inmutables, en `docs/releases/`) ni bugs ya triados: son un cuaderno de campo que un agente puede leer cuando busca pendientes y articular en un plan cuando se requiera.

Cada punto describe lo observado, lo que se sabe del código y lo que quedaría por decidir o hacer. Al convertir uno en trabajo real, trasládese al backlog (`.agent-context/05-siguientes-pasos.md`), a `ROADMAP.md` o a un plan de versión según corresponda, y déjese aquí la referencia.

## 1. Las identidades agrupadas pasan a llamarse «Teclado sin nombre»

**Tipo:** defecto de presentación, con una parte de microcopia.

**Síntoma:** al agrupar, la parte subordinada de la fila pasa a denominarse «Teclado sin nombre». No siempre es así —lo agrupado puede ser un control o un mouse ignorado— y además no está claro que deba quedar «sin nombre».

**Qué decía el código:** el nombre viene de `DeviceIdentityResolver.BuildDisplayName`, que caía a `"Teclado sin nombre"` cuando Windows solo ofrecía nombres genéricos. Ese es el *nombre detectado* y se guarda en las preferencias. La fila subordinada lo mostraba tal cual —`DisplayName = device.DetectedName` en WinUI y el equivalente en el respaldo WinForms—, descartando tanto el alias como el identificador técnico.

**Lo que la revisión añadió al síntoma original:** si se agrupan dos identidades débilmente identificadas —el mismo aparato en dos puertos, que es exactamente el caso para el que existe la agrupación— las dos filas subordinadas se leían igual y eran **indistinguibles entre sí**. El único dato que las separa, el identificador técnico, era el que no se mostraba. Y el sustantivo no solo fallaba dentro de los grupos: un presentador ignorado suelto también se leía «Teclado sin nombre». La app, además, no puede saberlo: `DeviceClassifier` clasifica por el nombre, así que ante un HID sin nombre no tiene señal alguna.

**Riesgo que condicionaba el arreglo:** ese literal no era solo presentación. `BuildFingerprint` hashea el nombre mostrado, de modo que renombrarlo a secas habría cambiado la huella de justo estos dispositivos mal nombrados y roto en silencio la recuperación de ignorado y de distribución al cambiar de puerto.

**Resuelto en 1.6.2.** La fila subordinada se nombra por lo que la identifica: el identificador técnico cuando el nombre detectado es genérico, el nombre detectado cuando es útil, y en ese segundo caso el identificador acompaña a «Identidad técnica» en la línea de estado para que dos miembros del mismo modelo sigan distinguiéndose. El sustantivo neutro «Dispositivo sin nombre» se aplica en toda la app, y la huella se sigue calculando con el literal anterior, con un valor dorado en las pruebas que impide cambiarlo por descuido. Decisión y alcance en [`plan-1.7.0.md`](plan-1.7.0.md#etapa-3--nombres-de-identidad-técnica-162).
