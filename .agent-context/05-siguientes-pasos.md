# Backlog vigente y priorizado

Este backlog sustituye para continuidad técnica el orden histórico de `ROADMAP.md` y `docs/continuacion-1.5.md`. No sustituye las matrices de aceptación externas.

## P0 — antes de producir una candidata estable

### 1. Separar diagnóstico del producto público

**Trabajo:** crear una abstracción mínima en núcleo y mover implementación, acciones IPC y controles UI a un proyecto/componente opcional habilitado por propiedad de build. El build normal/instalador estable debe usar implementación nula y no contener controles, marcador ni carpeta de logs.

**Criterios:**

- `build-installer.ps1` sin opción diagnóstica no empaqueta ensamblado/código/UI de diagnóstico;
- un script o propiedad explícita produce build de host con capacidad equivalente;
- no se usa una rama permanente para conservar diagnóstico;
- retirar `VirtualKey` de los detalles registrados y agregar prueba de privacidad;
- ambas variantes pasan la suite y la función Raw Input mantiene latencia.

### 2. Completar la Configuración WinUI

**Trabajo:** migrar Exportar, Importar y «Iniciar con Windows» desde `SettingsDialog`. Añadir operaciones IPC; el núcleo ejecuta validación, respaldo, persistencia y registro.

**Criterios:**

- las tres funciones están disponibles en la ventana normal instalada;
- combinar/reemplazar muestra advertencias y crea respaldo;
- cancelar/fallar no muta configuración;
- el estado de inicio refleja `StartupApproved` y no se reactiva silenciosamente;
- fallback y WinUI tienen semántica equivalente;
- pruebas de protocolo y núcleo cubren éxito/error.

### 3. Resolver estado legal

**Trabajo:** identificar licencia del upstream y añadir archivo/atribución compatibles antes de llamar estable al fork.

## P1 — cierre de beta 7 / candidata

### 4. Validar los ajustes visuales/foco finales

Ejecutar en dos equipos físicos al menos temas claro/oscuro, pasivo-hover-pressed de la X, fade-in/fade-out, selector desde Firefox/otra app, alias y dos teclados. Registrar commit exacto `c70b5d5` o el posterior que resulte.

**Criterio:** cero crash; X visible y contenida; overlay no desaparece en seco; selector delante y alias con foco consistentemente.

### 5. Medir rendimiento beta 7

Seguir `docs/criterios-winui3-1.5.md`: diez aperturas frías/calientes, mediana/P95, mismo host y build Release. Comparar ReadyToRun y tamaño del instalador.

**Criterio objetivo:** mediana ≤ 1500 ms frío / 750 ms caliente, P95 ≤ 2500 ms; si falla, documentar causa y decisión. No mantener UI residente como atajo sin aceptar coste en reposo.

### 6. Ejecutar matriz física crítica

Priorizar FIS-01 a FIS-08, FIS-10, FIS-11 y FIS-15 de `docs/calidad-1.5.md`, actualizando resultados reales, no los estados de beta 1. Incluir Windows 10/11, cuenta estándar, dos teclados, cambio de puerto, MX Master 3S, actualización y desinstalación.

### 7. Revisar microcopia

Reducir textos «tipo máquina» en Configuración, selector, errores e instalador. Conservar identificador técnico como información secundaria y nombres accesibles completos.

### 8. Alinear documentación humana

Actualizar README (bandeja y funciones realmente accesibles), CHANGELOG beta 7, ROADMAP vigente, matrices y borrador RC. No hacer esto hasta decidir/migrar las funciones WinUI para evitar otra contradicción.

## P2 — robustez y mantenimiento

### 9. Ampliar pruebas automatizadas

- migración real `config.txt` 1.4;
- recuperación de ignorado por huella y preferencias conflictivas;
- servidor/cliente IPC extremo a extremo y solicitud dañada;
- StartupManager con abstracción de registro;
- exclusión sintética sin datos de tecla;
- invariantes de orden de dispositivos;
- pruebas UI automatizables para overlay/foco cuando la infraestructura lo permita.

### 10. Añadir CI y política de warnings

Workflow Windows para restore/build/test, SDK fijado por `global.json`; considerar `TreatWarningsAsErrors` después de limpiar baseline. El instalador puede permanecer job manual por Inno Setup/licencia.

### 11. Extraer contratos compartidos

Mover DTO de IPC, `VersionPresentation` y modelos compartidos a una biblioteca para que WinUI no referencie el ejecutable WinForms. Mantener compatibilidad del protocolo o incrementar versión.

### 12. Revisar semántica de importación portable

Probar equipo A→B, layout ausente y dispositivo desconectado. Decidir si una asociación no resoluble debe conservarse como pendiente en vez de descartarse con warning.

## Puerta propuesta

No preparar `1.5.0-rc.1` hasta completar P0. No publicar `1.5.0` hasta al menos 72 horas de RC, matriz física obligatoria aprobada, documentación humana coherente, hash reproducible y aprobación humana explícita.
