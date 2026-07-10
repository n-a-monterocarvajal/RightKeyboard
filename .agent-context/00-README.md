# Contexto autónomo para agentes de IA

Fecha del snapshot: **2026-07-09**. Rama: `codex/version-1.5`, versión de proyecto `1.5.0`.

Esta carpeta contiene continuidad técnica, no documentación de usuario. Un agente que llega sin memoria previa debe leerla completa antes de cambiar código. Después debe contrastar el estado de Git y volver a ejecutar las comprobaciones indicadas: este snapshot no sustituye la evidencia del checkout actual.

## Qué es RightKeyboard

RightKeyboard es una utilidad de productividad para Windows 10/11 x64 destinada a personas que usan simultáneamente teclados físicos con distribuciones distintas. El residente identifica mediante Raw Input qué dispositivo produjo una pulsación y solicita a la ventana activa la distribución asociada a ese teclado. Se ejecuta en segundo plano, sin controlador ni hook global, y deja la interfaz moderna en un proceso WinUI separado que solo se inicia al configurar o administrar dispositivos. Las preferencias son por usuario y se guardan en `%LOCALAPPDATA%\RightKeyboard\preferences.json`.

## Orden de lectura

1. `01-estado-actual.md`: verdad operativa del checkout, funciones disponibles y diferencias con documentos históricos.
2. `02-arquitectura-y-decisiones.md`: procesos, flujo de entrada, persistencia, IPC y alternativas descartadas.
3. `03-problemas-conocidos.md`: defectos, limitaciones y riesgos priorizados con ubicación en código.
4. `04-convenciones-y-trampas.md`: reglas obligatorias, costumbres y zonas frágiles.
5. `05-siguientes-pasos.md`: backlog vigente con criterios de aceptación.
6. `06-build-pruebas-y-mapa.md`: comandos copiables, requisitos y mapa del repositorio.

## Fuentes externas a esta carpeta que siguen siendo autoritativas

No se duplican especificaciones que ya están bien escritas:

- Contrato del esquema 3 y semántica transaccional: `docs/preferencias-1.5.md` y `RightKeyboard/Configuration.cs`.
- Decisión de separar residente WinForms y frontend WinUI: `docs/arquitectura-winui-1.5.md`.
- Criterios de aceptación de WinUI, rendimiento y pruebas físicas: `docs/criterios-winui3-1.5.md`.
- Modelo de instalación por usuario: `docs/distribucion-1.5.md`, `installer/RightKeyboard.iss` e `installer/PRUEBAS.md`.
- Limitaciones funcionales deliberadas: `docs/limitaciones-conocidas-1.5.md`.

Cuando esos documentos contradicen este snapshot, debe prevalecer primero el código del commit actual y después `01-estado-actual.md`. Muchos documentos públicos conservan contexto histórico y no son una fotografía vigente.

## Respuestas rápidas a la auditoría

| Pregunta | Dónde encontrar la respuesta |
|---|---|
| Producto y público | Este archivo, sección «Qué es RightKeyboard» |
| Arquitectura y motivos | `02-arquitectura-y-decisiones.md` |
| Estado real | `01-estado-actual.md` |
| Bugs, causas y prioridades | `03-problemas-conocidos.md` |
| Convenciones obligatorias y costumbres | `04-convenciones-y-trampas.md` |
| Instalar, compilar, ejecutar y probar | `06-build-pruebas-y-mapa.md` |
| Decisiones descartadas | `02-arquitectura-y-decisiones.md` |
| Backlog vigente | `05-siguientes-pasos.md` |
| Qué no hacer | `04-convenciones-y-trampas.md` |

## Regla de actualización

Al cerrar una beta, RC o estable, actualizar como mínimo la fecha, commit, versión, conteo de pruebas, estado de validación física, problemas abiertos y backlog. No convertir esta carpeta en notas de versión ni copiar aquí manuales para usuarios.
