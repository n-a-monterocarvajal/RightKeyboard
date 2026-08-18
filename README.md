# RightKeyboard

Si usas dos teclados en la misma computadora —el del portátil y uno externo, o uno para cada
idioma—, Windows te obliga a cambiar la distribución a mano cada vez que pasas de uno a otro.
RightKeyboard se encarga de eso.

Cada teclado guarda su propia distribución. Cuando empiezas a escribir, la aplicación reconoce
por cuál dispositivo llegó la pulsación y le pide a la ventana activa que use la que
corresponde. No hay atajos que memorizar ni indicador que vigilar: escribes y la distribución
ya es la correcta.

Vive en segundo plano, y su única presencia permanente es el icono del área de notificación.
Desde su menú se abre **Configuración**, donde está todo lo demás, y **Salir**.

> Versión publicada: **1.6.0**. Los cambios están en el [registro](CHANGELOG.md) y el detalle
> de qué se validó y qué no, en las [notas de esa versión](docs/releases/1.6.0.md). Lo que
> viene después se planifica en [ROADMAP.md](ROADMAP.md) y [docs/plan-1.7.0.md](docs/plan-1.7.0.md).

## Antes de empezar

- Windows 10 o Windows 11 de 64 bits.
- El instalador trae .NET 10 incluido, así que no hace falta instalar ningún runtime aparte.

Un detalle que conviene saber de entrada: **RightKeyboard no instala distribuciones**. Solo
cambia entre las que Windows ya tiene. Si la que quieres usar todavía no está, agrégala primero
en **Configuración > Hora e idioma > Idioma y región > Opciones de idioma > Teclados**.

## Primeros pasos

1. Inicia `RightKeyboard.exe`.
2. Escribe una tecla normal en un teclado que todavía no tenga preferencia.
3. Elige su distribución y acepta.
4. Repite con los demás teclados.

Eso es todo: de ahí en adelante cada teclado recuerda lo suyo.

El selector aparece solo cuando hace falta. No lo abren los modificadores por sí solos, ni
soltar una tecla, ni la entrada sintética —por ejemplo, pegar desde el portapapeles—. Y si lo
cierras sin aceptar, no se crea ninguna asociación.

Ese mismo selector permite ponerle nombre al dispositivo, agrupa las distribuciones por idioma
y deja ignorar periféricos que publican pulsaciones sin ser teclados, como ciertos ratones con
botones avanzados.

El instalador deja activado el inicio con Windows para tu usuario, sin pedir permisos de
administrador. Se puede cambiar después desde **Configuración** o desde las aplicaciones de
inicio de Windows.

## Configuración

Es la ventana donde se corrige todo sin repetir la detección: renombrar dispositivos, cambiar
la distribución, ignorar periféricos ambiguos, agrupar identidades que en realidad son el mismo
teclado, olvidar dispositivos y limpiar preferencias.

También puedes exportar e importar tus preferencias. La importación muestra primero una vista
previa y te deja combinar o reemplazar, y siempre guarda un respaldo antes de tocar nada. Eso
sí: **llevar preferencias de un equipo a otro todavía no está certificado** con pruebas reales.

**Limpiar preferencias** vacía las asociaciones y la lista de ignorados. No toca las
distribuciones que tienes instaladas en Windows.

## Dónde quedan tus preferencias

Desde la versión 1.5 se guardan aquí y sobreviven a reiniciar la aplicación:

```text
C:\Users\<usuario>\AppData\Local\RightKeyboard\preferences.json
```

Si vienes de la versión 1.4 y existe un `config.txt`, RightKeyboard lo migra solo al formato
nuevo. El esquema, las validaciones y el alcance exacto de **Limpiar preferencias** están en
[Preferencias y portabilidad](docs/preferencias-1.5.md).

## Compilar y probar

Hace falta el SDK de .NET 10. El proyecto fija la banda en
[`global.json`](global.json), así que instala la que ese archivo pida: una banda distinta
—por ejemplo 10.0.3xx cuando pide 10.0.4xx— no sirve, aunque sea más nueva.

```powershell
dotnet restore RightKeyboard.sln
dotnet build RightKeyboard.sln --configuration Release
powershell -ExecutionPolicy Bypass -File scripts\run-tests.ps1 -Configuration Release -NoBuild
```

La aplicación queda en `RightKeyboard\bin\Release\net10.0-windows\`.

Para armar el instalador:

```powershell
scripts\build-installer.ps1
```

Publica para `win-x64`, compila con Inno Setup 7 y deja el `.exe` junto a su SHA-256 en
`artifacts\installer`. Si `ISCC.exe` no está en una ruta conocida, indícalo con la variable
`ISCC_PATH` o el parámetro `-IsccPath`.

Hay además un arnés que revisa la ventana de Configuración sobre la aplicación en marcha y
deja una captura para mirar:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\ui-harness.ps1
```

Necesita Windows con sesión interactiva y el [winapp CLI](https://github.com/microsoft/winappcli).
Cubre lo que puede afirmarse por propiedades de automatización; qué cubre y qué no —incluido
un límite que conviene conocer— está en [docs/arnes-ui-winapp-cli.md](docs/arnes-ui-winapp-cli.md).

## Cómo funciona por dentro

La idea de fondo es distinguir el teclado físico sin instalar nada en el sistema. De ahí salen
casi todas las decisiones:

- Usa Raw Input (`WM_INPUT`), que identifica el dispositivo de origen sin servicios,
  controladores ni hooks globales.
- Lee la estructura `RAWKEYBOARD` completa y actúa solo en eventos de pulsación.
- Agrupa las distintas funciones HID de un mismo teclado por el `ContainerId` de Plug and Play.
  Sin eso, una combinación `Fn` puede presentarse como otro dispositivo y volver a preguntar.
- Guarda una huella del modelo como respaldo, para recuperar la asociación cuando Windows
  cambia el identificador de un dispositivo al reconectarlo.
- Actualiza el inventario cuando Raw Input avisa de una conexión o desconexión.
- Descarta de forma conservadora los periféricos que claramente no son teclados, y deja que
  ignores a mano los casos dudosos.
- Pide el cambio con `WM_INPUTLANGCHANGEREQUEST` **solo a la ventana activa**. Nunca cambia el
  idioma global ni difunde el mensaje a todas las aplicaciones.
- No mantiene un formulario principal oculto: le alcanza una ventana de solo mensajes y el
  icono de notificación.

Si tienes activada la opción de Windows **Permitir usar un método de entrada diferente para cada
ventana de aplicación**, Windows recuerda un estado por ventana. RightKeyboard respeta ese
modelo y vuelve a pedir la distribución asociada cuando llega una pulsación de cada teclado.

## Origen y estado legal

Este fork conserva el trabajo previo de los autores y colaboradores de RightKeyboard. El código
llegó aquí desde [gmcouto](https://github.com/gmcouto/RightKeyboard), que lo importó de un
origen externo en enero de 2020, a través de su fork [mnivet](https://github.com/mnivet/RightKeyboard).

Ese origen se atribuye al artículo «Using multiple keyboards with different layouts on the same
machine», publicado por Antoine Aubry en CodeProject el 23 de octubre de 2007 bajo la Code
Project Open License 1.02. La atribución descansa en las declaraciones de gmcouto y de
[agabor](https://github.com/agabor/RightKeyboard) —un tercero independiente que atribuye su
propio código al mismo artículo—, no en una comparación directa: CodeProject dejó de operar y el
artículo ya no está disponible, de modo que **se desconoce si el código heredado coincide con el
que lo acompañaba**. Ante esa incertidumbre el proyecto trata CPOL 1.02 como vinculante.

Ninguno de los repositorios anteriores declara licencia para sus propias modificaciones.

El proyecto se distribuye en tres capas, detalladas en [LICENSE](LICENSE): la obra original de
2007 bajo CPOL 1.02, los forks intermedios sin licencia declarada, y los cambios introducidos en
este repositorio desde el fork bajo licencia MIT. Ninguna capa relicencia a otra.

Consecuencia práctica: por la sección 5(d) de CPOL, **RightKeyboard no puede venderse,
arrendarse ni alquilarse por sí solo**, aunque los aportes de este fork sean MIT. Distribuirlo
gratuitamente es compatible con las tres capas. La investigación de procedencia completa está en
[Procedencia y licencia](docs/procedencia-y-licencia.md).

## Idioma del proyecto

Escribimos en español la documentación, las notas de cambios y todo lo que ve el usuario. Los
nombres de tipos, métodos y APIs quedan en inglés, siguiendo las convenciones de .NET y Win32.
Si vas a aportar algo, pasa antes por [CONTRIBUTING.md](CONTRIBUTING.md).
