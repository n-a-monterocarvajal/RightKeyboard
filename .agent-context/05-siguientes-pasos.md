# Backlog vigente y priorizado

Este backlog sustituye para continuidad técnica el orden histórico de `ROADMAP.md` y `docs/continuacion-1.5.md`. No sustituye las matrices de aceptación externas.

## P1 — mantenimiento recomendado para 1.5.1

### 1. Robustecer diagnóstico y logs

**Trabajo:** revisar campos registrados, asegurar que no se registra contenido de teclas, añadir señales útiles para falsos positivos HID y separar mejor el modo diagnóstico del flujo normal.

**Criterios:**

- no se usa una rama permanente para conservar diagnóstico;
- retirar `VirtualKey` de los detalles registrados y agregar prueba de privacidad;
- ambas variantes pasan la suite y la función Raw Input mantiene latencia.

### 2. Mejorar detección preventiva de HID ambiguos

**Trabajo:** usar los logs extendidos para modelar firmas HID parciales (`VID`, `PID`, interfaz, colección, enumerador y capacidades). Caso concreto: presentador Baseus detectado como `Dispositivo F7E55424`, diagnosticado con `VID=2571` y `PID=4104`.

**Criterios:**

- ignorar manualmente un HID ambiguo puede aplicar también a su firma cuando sea seguro;
- cambiar de puerto no debe reabrir selector si la firma ignorada es inequívoca;
- ningún teclado real se excluye solo por coincidencia débil;
- el diagnóstico muestra por qué se aplicó o no la regla.

### 3. Agrupar identidades del mismo dispositivo

**Trabajo:** permitir que la UI anide/fusione manualmente identidades que el usuario reconoce como el mismo teclado conectado en distintos puertos.

**Criterios:**

- operación reversible;
- una distribución/alias gobierna el grupo lógico;
- los miembros del grupo siguen visibles como identidades técnicas secundarias;
- no hay fusión automática en dispositivos ambiguos.

### 4. Completar la Configuración WinUI

**Trabajo:** migrar Exportar, Importar y «Iniciar con Windows» desde `SettingsDialog`. Añadir operaciones IPC; el núcleo ejecuta validación, respaldo, persistencia y registro.

**Criterios:**

- las tres funciones están disponibles en la ventana normal instalada;
- combinar/reemplazar muestra advertencias y crea respaldo;
- cancelar/fallar no muta configuración;
- el estado de inicio refleja `StartupApproved` y no se reactiva silenciosamente;
- fallback y WinUI tienen semántica equivalente;
- pruebas de protocolo y núcleo cubren éxito/error.

### 5. Resolver estado legal

**Trabajo:** identificar licencia del upstream y añadir archivo/atribución compatibles antes de llamar estable al fork.

## P2 — robustez y mantenimiento

### 6. Medir rendimiento 1.5.x

Seguir `docs/criterios-winui3-1.5.md`: diez aperturas frías/calientes, mediana/P95, mismo host y build Release. Comparar ReadyToRun y tamaño del instalador.

**Criterio objetivo:** mediana ≤ 1500 ms frío / 750 ms caliente, P95 ≤ 2500 ms; si falla, documentar causa y decisión. No mantener UI residente como atajo sin aceptar coste en reposo.

### 7. Ejecutar matriz física ampliada

Priorizar FIS-01 a FIS-08, FIS-10, FIS-11 y FIS-15 de `docs/calidad-1.5.md`, actualizando resultados reales, no los estados de beta 1. Incluir Windows 10/11, cuenta estándar, dos teclados, cambio de puerto, MX Master 3S, actualización y desinstalación.

### 8. Revisar microcopia

Reducir textos «tipo máquina» en Configuración, selector, errores e instalador. Conservar identificador técnico como información secundaria y nombres accesibles completos.

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

Para `1.5.1`, priorizar diagnóstico/detección preventiva antes que nuevas superficies UI grandes. Para `1.6`, considerar exportación/importación completa y refactor de contratos compartidos si no se resolvió en 1.5.x.
