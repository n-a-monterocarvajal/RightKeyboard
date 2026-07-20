# Backlog vigente y priorizado

Este backlog sustituye para continuidad técnica el orden histórico de `ROADMAP.md` y `docs/continuacion-1.5.md`. No sustituye las matrices de aceptación externas.

## P1 — mantenimiento recomendado para 1.5.1

### 1. Robustecer diagnóstico de desarrollo y logs

**Trabajo:** revisar campos registrados, asegurar que no se registra contenido de teclas y añadir señales útiles para falsos positivos HID en la variante `RIGHTKEYBOARD_DIAGNOSTICS`.

**Criterios:**

- no se usa una rama permanente para conservar diagnóstico;
- agregar prueba de privacidad para impedir que vuelvan `VirtualKey`, rutas completas, alias o caracteres;
- ambas variantes pasan la suite y la función Raw Input mantiene latencia.

### 2. Mejorar detección preventiva de HID ambiguos — **implementado (Etapa 5), pendiente de validación física**

**Trabajo:** usar los logs extendidos para modelar firmas HID parciales (`VID`, `PID`, interfaz, colección, enumerador y capacidades). Caso concreto: presentador Baseus detectado como `Dispositivo F7E55424`, diagnosticado con `VID=2571` y `PID=4104`.

**Criterios (estado):**

- ignorar manualmente un HID ambiguo puede aplicar también a su firma cuando sea seguro — hecho: solo con huella vacía y firma disponible (`Configuration.Ignore`/`UpdatePreference`);
- cambiar de puerto no debe reabrir selector si la firma ignorada es inequívoca — hecho: regla en `Configuration.IsIgnored` (una coincidencia conectada, sin distribuciones con la firma); falta prueba física de cambio de puerto;
- ningún teclado real se excluye solo por coincidencia débil — hecho por construcción: los dispositivos con huella quedan fuera del sistema de firmas;
- el diagnóstico muestra por qué se aplicó o no la regla — hecho: `firma_registrada/no_registrada/no_aplicada/retirada`, `ignorado_recuperado_por_firma`.

### 3. Agrupar identidades del mismo dispositivo — **implementado (Etapa 6), pendiente de validación física**

**Trabajo:** permitir que la UI anide/fusione manualmente identidades que el usuario reconoce como el mismo teclado conectado en distintos puertos.

**Criterios (estado):**

- operación reversible — hecho: separar recupera las preferencias individuales latentes y disuelve automáticamente un grupo que queda con un solo miembro;
- una distribución/alias gobierna el grupo lógico — hecho: `Configuration.TryGetEffectiveLayout` y `GetDisplayName` priorizan el grupo;
- los miembros del grupo siguen visibles como identidades técnicas secundarias — hecho en `SettingsWindow` mediante encabezado lógico y filas indentadas;
- no hay fusión automática en dispositivos ambiguos — hecho: solo las acciones IPC v2 `group`/`ungroup` cambian membresía; la recuperación por huella nunca lo hace;
- falta ejecutar la matriz física con dos teclados y cambio de puerto en la estación real.

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

### 12. Reordenar la disposición de la Configuración WinUI y definir tamaño mínimo — **implementado (Etapa 7), pendiente de validación DPI ampliada**

**Trabajo:** tras migrar Exportar, Importar e «Iniciar con Windows» (etapas 2–3), esos controles y «Limpiar preferencias» quedaron apilados bajo la lista de dispositivos sin criterio de diseño propio. Reubicarlos con una jerarquía deliberada (p. ej. barra de comandos, sección de preferencias o pie agrupado) y, cuando todo esté acomodado, definir un tamaño mínimo operacional de la ventana.

**Criterios:**

- Exportar, Importar y Limpiar se agrupan como operaciones sobre preferencias; Limpiar conserva confirmación y añade énfasis rojo en hover/pressed — hecho;
- «Iniciar con Windows» permanece en Sistema porque su estado no se exporta, importa ni limpia con las preferencias — hecho;
- «Diagnóstico detallado»/«Abrir registros» siguen apareciendo solo en compilaciones de desarrollo y no condicionan el layout normal — hecho;
- se define y aplica un mínimo de 900 × 640 píxeles lógicos, adaptado al DPI; ambas variantes se verificaron a 100 %, Guardar/Olvidar quedan fijos y la lista de dispositivos conserva desplazamiento — hecho;
- queda pendiente repetir con evidencia suficiente a 125 %, DPI mixto/dos monitores y texto ampliado fuera de esta VM;
- la microcopia afectada y los nombres de automatización de los comandos se ajustaron sin convertir la etapa en una revisión textual completa.

### 13. Revisar semántica de importación portable

Probar equipo A→B, layout ausente y dispositivo desconectado. Decidir si una asociación no resoluble debe conservarse como pendiente en vez de descartarse con warning.

## Observaciones de uso sin triar

Las notas recogidas al usar cada versión viven en `docs/notas-de-uso-<versión>.md` (p. ej. `docs/notas-de-uso-1.5.4.md`), separadas de este backlog porque aún no están priorizadas ni convertidas en criterios de aceptación. Revísalas al planificar: cada punto es candidato a entrar aquí, en `ROADMAP.md` o en `docs/plan-1.6.0.md`. Al promover uno, déjalo referenciado desde el documento de notas para no duplicar el seguimiento.

Pendientes abiertos en `docs/notas-de-uso-1.5.4.md`: desplegable de agrupación vacío (defecto), indicador gráfico de conexión, evaluación de agrupar identidades ignoradas, actualizador en la app y regla de orden «Conectados arriba».

## Puerta propuesta

Para `1.5.1`, priorizar diagnóstico de desarrollo/detección preventiva antes que nuevas superficies UI grandes. Para `1.6`, considerar exportación/importación completa y refactor de contratos compartidos si no se resolvió en 1.5.x.
