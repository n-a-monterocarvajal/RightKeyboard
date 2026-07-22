# Automatización con GitHub Actions

Documentación técnica de los workflows de GitHub Actions del repositorio. El
contexto operativo para agentes vive en `.agent-context/` (en especial
`06-build-pruebas-y-mapa.md`); este documento describe qué hace cada
automatización, cuándo se ejecuta y sus límites.

Diseño orientado a buena relación costo/beneficio: el repositorio es privado y
cada minuto de Actions se factura, por lo que los workflows evitan matrices,
compilaciones programadas, artefactos permanentes y jobs redundantes.

## Resumen

| Workflow | Archivo | Disparadores | Runner | Resultado |
|---|---|---|---|---|
| CI | `.github/workflows/ci.yml` | push a `master`, pull request, manual | `windows-latest` | Restaura, compila Release y ejecuta pruebas. Sin artefactos. |
| Compilación distribuible | `.github/workflows/build-package.yml` | manual, etiquetas `v*` | `windows-latest` | Instalador Inno Setup (o ZIP portable) + SHA-256 como artefacto descargable (7 días). |
| Dependabot | `.github/dependabot.yml` | semanal (lunes) | — | Pull requests agrupados de NuGet y GitHub Actions. |

## 1. Integración continua (`ci.yml`)

### Qué valida

1. Restaura la solución (`dotnet restore RightKeyboard.sln`).
2. Compila **la solución completa en `Release`** (`dotnet build -c Release`). La
   solución mapea `Release|Any CPU` → `Release|x64` para el proyecto WinUI, de
   modo que una sola compilación cubre el núcleo WinForms, el frontend WinUI y
   las pruebas. `Directory.Build.props` activa `TreatWarningsAsErrors`, por lo
   que cualquier advertencia detiene el build (política AUT-15).
3. Ejecuta la suite NUnit (`dotnet test RightKeyboard.NUnit`).

Un fallo de restauración, compilación o pruebas hace fallar el workflow de forma
visible.

### Cuándo se ejecuta

- Al abrir o actualizar un **pull request**.
- Al hacer **push a `master`** (la rama principal real del repositorio).
- **Manualmente** desde la pestaña *Actions* (`workflow_dispatch`).

Se aplican filtros `paths-ignore` que solo omiten cambios incapaces de romper la
compilación: documentación (`**/*.md`, `docs/**`, `.agent-context/**`), `LICENSE`
y metadatos de Git. Cualquier cambio en código, proyectos, paquetes, manifiestos,
recursos, configuración de compilación, pruebas o en el propio workflow **sí**
dispara CI.

### Entorno

- Runner **Windows** estándar (`windows-latest`); no hay Linux, macOS ni matrices.
- SDK de .NET fijado por `global.json` (`10.0.301`, roll-forward al último patch).
- Configuración `Release`, arquitectura x64 (heredada del mapeo de la solución).

### Medidas de ahorro de minutos

- `concurrency` con `cancel-in-progress: true`: una ejecución nueva cancela las
  anteriores obsoletas de la misma rama o pull request.
- `timeout-minutes: 30` como límite de seguridad.
- Un único job (restore + build + test) para no multiplicar el redondeo de
  minutos por job.
- Caché del almacén global de NuGet (`actions/cache`), invalidada por hash de los
  `*.csproj` y `global.json`.
- Permisos mínimos: `contents: read`.
- Sin compilaciones programadas ni matrices; **no** genera artefactos.

## 2. Compilación distribuible (`build-package.yml`)

### Cómo iniciarla

- **Manual:** pestaña *Actions* → *Compilación distribuible* → *Run workflow*.
  Entradas:
  - `version` (opcional): si se deja vacía se usa la del proyecto
    (`RightKeyboard/RightKeyboard.csproj`).
  - `artifact`: `installer` (por defecto) o `zip`.
  - `publish`: `no` (por defecto) o `yes`. Con `yes` (solo válido con
    `artifact=installer`) se crea además una **GitHub Release** — ver «Publicación».
- **Por etiqueta:** al empujar una etiqueta `v*` (p. ej. `v1.5.5.2`), la versión
  se deriva del nombre de la etiqueta sin el prefijo `v` y se genera el `installer`
  (no publica Release: la publicación se pide explícitamente con `publish=yes`).

No se ejecuta en cada commit ni en cada pull request.

### Modalidades

- **`installer`** (vía normal de distribución): ejecuta `scripts/build-installer.ps1`
  y produce el instalador Inno Setup por usuario + SHA-256.
- **`zip`** (artefacto de diagnóstico): ejecuta `scripts/build-portable-zip.ps1` y
  produce un **ZIP portable autocontenido** (`RightKeyboard-<versión>-win-x64-portable.zip`)
  + SHA-256, sin instalador. Sirve para probar una compilación en una estación que no
  es la de desarrollo: se descomprime y se ejecuta `RightKeyboard.exe` sin instalar
  .NET ni el Windows App SDK. No sustituye al instalador ni a una versión formal
  (coherente con `docs/distribucion-1.5.md`).

### Qué artefacto genera y dónde se descarga

Realiza una compilación limpia en `Release`. En ambas modalidades publica núcleo y
WinUI autocontenidos para `win-x64` (ReadyToRun) en una sola carpeta compartida y
calcula el SHA-256; la diferencia es el empaquetado final.

Salida publicada como artefacto de la ejecución de Actions, descargable desde la
página de la ejecución (sección *Artifacts*):

- Modalidad `installer` (`scripts/build-installer.ps1`) → artefacto
  `RightKeyboard-<versión>-win-x64-installer`, con:
  - `RightKeyboard-<versión>-Setup.exe`
  - `RightKeyboard-<versión>-SHA256.txt`
- Modalidad `zip` (`scripts/build-portable-zip.ps1`) → artefacto
  `RightKeyboard-<versión>-win-x64-portable`, con:
  - `RightKeyboard-<versión>-win-x64-portable.zip`
  - `RightKeyboard-<versión>-win-x64-portable-SHA256.txt`

Cada nombre identifica aplicación, versión, arquitectura (x64) y modalidad.

### Cuánto se conserva

**7 días** (`retention-days: 7`). Es una compilación de conveniencia, no una
versión formal: los artefactos de Actions son temporales y no sustituyen una
GitHub Release.

### Firma, MSIX y Authenticode (limitaciones)

- El instalador y los binarios se generan **sin firmar**. Sirven para pruebas y
  distribución interna; Windows SmartScreen puede advertir sobre un ejecutable
  sin firma.
- No se incorporan certificados, claves privadas ni secretos al repositorio, ni
  se desactivan verificaciones de seguridad.
- El modelo de distribución vigente es **no empaquetado** (`WindowsPackageType=None`,
  Windows App SDK autocontenido). No se produce un paquete MSIX. La adopción de
  MSIX sigue en evaluación (ver `docs/distribucion-1.5.md`).
- Para añadir firma más adelante haría falta, como mínimo:
  - **Authenticode** (firmar `Setup.exe` y los `.exe` publicados): un certificado
    de firma de código válido (idealmente EV/OV), almacenado como secreto del
    repositorio (p. ej. `SIGNING_CERT_BASE64` y `SIGNING_CERT_PASSWORD`), y un
    paso con `signtool.exe` antes de empaquetar y antes de calcular el SHA-256.
  - **MSIX**: además de lo anterior, un proyecto de empaquetado MSIX, un
    certificado cuyo *subject* coincida con `Publisher` del manifiesto y firmar el
    `.msix` resultante. Requiere decidir antes la estrategia de actualización
    fuera de Microsoft Store.

### Publicación de la GitHub Release

Con `publish=yes` (y `artifact=installer`), un **job aparte** (`publish`) crea la
Release tras compilar el instalador:

- Corre solo cuando se pide explícitamente; nunca en un push de etiqueta ni en el
  build ordinario. Por defecto no publica.
- Está aislado del job de compilación para acotar el privilegio: el build es de
  solo lectura y **solo este job** tiene `contents: write`. Usa el `GITHUB_TOKEN`
  del runner (`gh release create`), sin tokens personales ni secretos.
- Crea el tag `vX.Y.Z` sobre el commit compilado y adjunta los dos assets
  (`RightKeyboard-<versión>-Setup.exe` y `-SHA256.txt`). Las notas salen de la
  sección de esa versión en `CHANGELOG.md` más el resumen de descargas y el SHA-256
  real del instalador. La Release aparece firmada por `github-actions[bot]`.
- Si el tag/Release ya existe, el job falla en vez de recrearlo.

Publicar la Release es deliberado y explícito; el versionado sigue las convenciones
descritas en `.agent-context/06-build-pruebas-y-mapa.md`, sección «Publicación».
La firma Authenticode/MSIX del instalador publicado sigue pendiente (ver arriba).

## 3. Dependabot (`dependabot.yml`)

Revisiones **semanales** (lunes) para dos ecosistemas:

- **NuGet** (`nuget`): los `PackageReference` de los tres proyectos. No hay
  `Directory.Packages.props`, así que resuelve por proyecto.
- **GitHub Actions** (`github-actions`): las acciones usadas por los workflows.

Cada ecosistema usa un **grupo** propio (`nuget` y `github-actions`) para reunir
sus actualizaciones en un solo pull request y reducir ejecuciones de CI. Límite de
5 pull requests abiertos por ecosistema. No hay revisiones diarias ni
fragmentación excesiva.

Las actualizaciones propuestas por Dependabot deben revisarse y validarse mediante
CI antes de integrarse.

## 4. Seguridad de los workflows

- Permisos de solo lectura por defecto (`contents: read`). La única excepción es el
  job `publish` de la compilación distribuible, que necesita `contents: write` para
  crear la Release; ese permiso está acotado a ese job y solo corre con `publish=yes`.
- No se incorporan tokens personales ni se imprimen secretos; la publicación usa el
  `GITHUB_TOKEN` del runner.
- Solo acciones oficiales (`actions/checkout`, `actions/setup-dotnet`,
  `actions/cache`, `actions/upload-artifact`, `actions/download-artifact`), fijadas
  por etiqueta de versión mayor (`@v4`), coherente con la convención previa del
  repositorio.
- No se habilita CodeQL ni servicios de seguridad con licenciamiento adicional.

## 5. Limitaciones actuales y tareas futuras

- **Pruebas físicas:** la suite NUnit no cubre extremo a extremo el pipe IPC, la
  UI WinUI, foco/foreground, SetupAPI real, el registro de inicio, el instalador
  ni el hardware. Esa validación sigue siendo manual (ver `docs/calidad-1.5.md`).
  No se añadieron pruebas artificiales para llenar ese hueco.
- **Sin firma:** ver la sección de firma; queda pendiente decidir e integrar
  Authenticode y/o MSIX con sus secretos.
- **GitHub Release:** se crea desde Actions bajo demanda (`publish=yes`), con el
  instalador sin firmar. La firma del instalador publicado queda pendiente.
- **Verificación del runner:** la compilación real debe pasar en un runner limpio
  de GitHub Actions; un build local correcto no lo sustituye. Cualquier cambio en
  la arquitectura, versión de .NET, Windows App SDK, empaquetado, firma o
  distribución debe reflejarse en estos workflows y en esta documentación.
