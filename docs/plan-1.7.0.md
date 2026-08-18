# Plan de ejecución camino a 1.7.0

Redactado el 18 de agosto de 2026, al cerrar el contenido de 1.6.0.

Este documento es el orden de ejecución de la línea 1.7. `ROADMAP.md` sigue siendo el
backlog temático: describe **qué** falta; este archivo describe **en qué orden** se aborda
y **en qué estado** está. La convención de la línea 1.5 se conserva: cada etapa es una
sesión, una rama, un PR y un bump de versión, con su entrada en `CHANGELOG.md`.

## Situación de partida

`1.6.0` cerró con los contratos compartidos extraídos a `RightKeyboard.Shared` y las seis
observaciones de interfaz de [notas de uso 1.6.0](notas-de-uso-1.6.0.md) resueltas en sus
etapas 21 a 23. Queda pendiente de esa versión la validación física del refresco automático
y de los cambios visuales, registrada en su nota de publicación.

## Carril A — etapas de código

| Etapa | Versión | Asunto | Estado |
|---|---|---|---|
| 1 | 1.7.0 | Verificación de actualizaciones contra GitHub | Pendiente |

### Etapa 1 — Verificación de actualizaciones contra GitHub

Punto 7 de [notas de uso 1.6.0](notas-de-uso-1.6.0.md#7-verificación-de-actualizaciones-contra-el-repositorio-de-github),
diferido desde 1.6.0 por ser una capacidad nueva y no un ajuste de interfaz. Estaba
registrado desde antes como punto 4 de [notas de uso 1.5.4](notas-de-uso-1.5.4.md#4-incluir-un-actualizador-en-la-app),
sin promoverse nunca a un plan.

Hoy la actualización es manual: descargar y ejecutar el instalador de la versión nueva.

#### Estudio del ecosistema

No existe un estándar de facto para actualizadores en .NET/Windows comparable a lo que Inno
Setup es para instaladores. Las opciones evaluadas, de menor a mayor cambio en el
empaquetado actual:

**(a) Ping directo a la API de Releases, sin framework.** Una llamada a
`GET https://api.github.com/repos/n-a-monterocarvajal/rightkeyboard/releases/latest`
(JSON, sin autenticación para repositorios públicos), comparar `tag_name` con
`VersionPresentation` —ya centralizada en `RightKeyboard.Shared` desde 1.6.0— y avisar al
usuario con un enlace a la Release. Cero dependencias nuevas, ningún cambio al instalador ni
al pipeline. Sin descarga asistida ni instalación desatendida, y hay que manejar a mano el
límite de tasa anónimo de GitHub (unas 60 solicitudes por hora y por IP) con caché y
reintentos.

**(b) [NetSparkleUpdater](https://github.com/NetSparkleUpdater/NetSparkle)**, puerto .NET
del Sparkle de macOS. No trae instalador propio ni reemplaza archivos: descarga y ejecuta el
instalador externo que ya exista, de modo que **convive con Inno Setup** sin rehacer
`scripts/build-installer.ps1`. Comprueba contra un *appcast* (XML/JSON), no contra la API de
Releases: el generador `netsparkle-generate-appcast` no publica a GitHub Releases de forma
nativa, así que habría que generar y publicar el appcast aparte, por ejemplo desde CI. Su UI
prearmada cubre WinForms y WPF, no WinUI 3. La instalación silenciosa final depende de los
flags de Inno Setup (`/VERYSILENT`), no de la librería.

**(c) [Velopack](https://github.com/velopack/velopack)**, sucesor activo de Squirrel. Lee
GitHub Releases de forma nativa con `GithubSource`, ofrece paquetes delta e instalación
desatendida (`Setup.exe --silent`, `--installto`). **Trae su propio instalador** —per-user a
`%LocalAppData%` sin elevación, o `.msi` per-machine—, de modo que en la práctica reemplaza a
Inno Setup y el ZIP portable en lugar de convivir con ellos.

**(d) MSIX + App Installer** (`.appinstaller` con `UpdateUri`), el reemplazo oficial de
ClickOnce. Soporta actualización automática en segundo plano incluso fuera de la Store, pero
exige empaquetar como MSIX. ClickOnce se descarta directamente: en 2026 es legado, poco
personalizable y pensado para servidor propio, no para Releases de GitHub.

#### Dato ya confirmado

Las preferencias viven en `%LocalAppData%` (`Configuration.cs`), **no** junto al ejecutable,
así que sobrevivirían a un reemplazo completo de la carpeta de la aplicación. Eso desactiva
la objeción principal contra (c), que exige guardar la configuración fuera del árbol de la
app.

#### Recomendación

Empezar por **(a)**, el ping directo, y tratar la descarga asistida como una decisión
posterior y separada. Es lo que el pedido original describe —«verificación de
actualizaciones»—, reutiliza infraestructura que ya existe (Releases y SHA-256 publicados
por versión, versión compartida centralizada) y no compromete el empaquetado. Si más adelante
se quiere descarga o instalación asistida conservando Inno Setup, (b) es el siguiente paso
natural sin desandar lo hecho.

#### A definir antes de implementar

- Cadencia de la comprobación y si es opt-in, con su casilla en la Configuración.
- Caché y respeto del límite de tasa anónimo; qué hacer cuando GitHub no responde.
- Si se muestra el SHA-256 publicado junto al enlace, para que el usuario pueda verificar la
  descarga a mano.
- Dónde aparece el aviso: Configuración, menú de bandeja, o ambos.
- Respetar CPOL 5(d): la distribución debe seguir siendo gratuita.

## Carriles heredados

Los carriles C (validación física), D (mediciones) y E (cobertura automatizada) de
[`plan-1.6.0.md`](plan-1.6.0.md) siguen abiertos y se arrastran sin cambios, además de la
validación física que 1.6.0 dejó declarada en su nota de publicación.
