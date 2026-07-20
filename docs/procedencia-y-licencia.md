# Procedencia y estado de licencia

Investigación del 19 de julio de 2026. Este documento reúne lo verificado sobre el origen del código y deja separado lo que aún no se ha podido confirmar. No es asesoría legal.

## Cadena de procedencia

```text
Artículo de CodeProject núm. 20994 (23 de octubre de 2007)
  └─ agabor/RightKeyboard (repositorio creado el 16 de octubre de 2015)
      └─ gmcouto/RightKeyboard (enero de 2020)
          └─ mnivet/RightKeyboard (mayo de 2020)
              └─ n-a-monterocarvajal/RightKeyboard (junio de 2026, este repositorio)
```

El artículo original es «Using multiple keyboards with different layouts on the same machine», publicado en `codeproject.com/Articles/20994/`.

## Evidencia verificada

- El historial de este repositorio empieza el 6 de enero de 2020 con un commit `Initial commit` que contiene **únicamente un README**. El código entra un día después, el 7 de enero, en el commit `2894ddf` («first changes on rightkeyboard») con 1838 líneas y veinte archivos de una sola vez. Es una importación de código preexistente, no desarrollo incremental.
- El `AssemblyInfo.cs` de esa importación declara `AssemblyCopyright("Copyright ©  2007")`, **sin titular**, y `AssemblyCompany("")`. La fecha sitúa el origen trece años antes del historial de GitHub.
- `agabor/RightKeyboard` no es un fork de GitHub (`fork: false`) y su README declara: «This code is based on the work published here», enlazando el artículo de CodeProject. Es una republicación, no la obra original.
- Los README de `gmcouto` y `mnivet` conservan la misma frase de atribución y el mismo enlace.
- **Ninguno de los cuatro repositorios de la cadena contiene un archivo de licencia.** Comprobado mediante la API de GitHub sobre `agabor`, `gmcouto`, `mnivet` y este repositorio: los cuatro responden sin licencia declarada. La ausencia es heredada desde el origen.
- `codeproject.com` dejó de operar: el dominio está aparcado en GoDaddy. La fuente autorizada de los términos originales ya no está disponible en su dirección canónica. Existe una instantánea en el archivo web con fecha del 19 de mayo de 2024.

## Pendiente de confirmar

Dos datos, ambos en la página archivada del artículo:

1. El nombre del autor original.
2. El texto exacto del pie de licencia.

Por qué importa la precisión: CodeProject publicaba por omisión bajo la **Code Project Open License (CPOL)**, pero un artículo podía declarar otros términos, así que la suposición no sustituye a la comprobación. Si el origen resulta ser CPOL, condiciona la licencia de salida de este fork: una obra derivada arrastra esos términos y no puede relicenciarse sin más a una licencia permisiva. CPOL además no está aprobada por la OSI y su compatibilidad con licencias copyleft es discutida.

Mientras estos dos datos no se confirmen, la licencia de salida de RightKeyboard no puede elegirse con fundamento.

## Situación de la atribución

Los forks intermedios `gmcouto` y `mnivet` enlazan el artículo de origen en su README. Este fork había perdido esa referencia: su sección «Origen y estado legal» mencionaba «el trabajo previo de los autores y colaboradores de RightKeyboard» sin nombrar la fuente. La referencia se restauró el 19 de julio de 2026, con independencia de cómo se resuelva la licencia, porque atribuir correctamente no depende de esa decisión.

## Consecuencia operativa

Publicar binarios y aceptar contribuciones externas siguen bloqueados. El criterio registrado en `.agent-context/03-problemas-conocidos.md` es no presentar la línea 1.5 como jurídicamente lista para distribución sin resolver esto; conviene notar que `1.5.0` ya se promovió como estable pese a ese criterio.
