# Limitaciones conocidas de RightKeyboard 1.5

## Primera pulsación de un dispositivo nuevo

Un teclado que todavía no tiene preferencia se incorpora al flujo de configuración solo después de una tecla utilizable. Las teclas modificadoras o auxiliares —por ejemplo `Ctrl`, `Alt`, `Shift`, `Fn` y varias teclas multimedia— no abren por sí solas el selector. Esta restricción es deliberada: evita que colecciones HID auxiliares, combinaciones de mouse y eventos sintéticos se presenten como teclados nuevos.

Una vez asociado el teclado, esas pulsaciones sí permiten recuperar y aplicar una distribución ya conocida.

## Reconexión y cambio de puerto USB

RightKeyboard prioriza el `ContainerId` que entrega Windows. Si el dispositivo o su controlador conserva ese identificador entre puertos, la identidad no cambia. Algunos teclados, hubs y controladores entregan un `InstanceId` distinto por puerto; en ese caso la aplicación usa una huella de fabricante, nombre y hardware para recuperar la preferencia cuando hay una sola coincidencia inequívoca.

La recuperación conservadora evita confundir dos teclados idénticos. Si hay varios dispositivos con la misma huella o preferencias contradictorias, RightKeyboard no adivina cuál es cuál y puede pedir configuración nuevamente. Después de reconocer de forma inequívoca una identidad nueva, la asociación queda aprendida para ese puerto.

## Configuración abierta

La escritura dentro de Configuración no debe lanzar el selector de teclado. Raw Input continúa activo para aplicar preferencias conocidas y observar dispositivos, pero un dispositivo todavía sin asociación queda pendiente hasta cerrar Configuración o elegirlo explícitamente en la lista.

Esta conducta debe comprobarse físicamente con alias que incluyan letras, números, espacios, acentos y correcciones con `Backspace`.
