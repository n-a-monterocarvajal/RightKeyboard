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

## Confirmado

El artículo declara **Code Project Open License (CPOL) 1.02** y su autor es **Antoine Aubry**. Confirmado el 19 de julio de 2026 sobre la instantánea archivada, dado que el sitio original ya no opera.

Con ese dato se resolvió la licencia del proyecto. El resultado está en el archivo `LICENSE` de la raíz, estructurado en tres capas: la obra original de 2007 bajo CPOL 1.02, los forks intermedios sin licencia declarada, y los cambios de este fork bajo MIT. Ninguna capa relicencia a otra.

Las obligaciones de CPOL que sobreviven a la licencia MIT de los aportes nuevos son las secciones 3(c), 5(a), 5(d) y 6. La de mayor consecuencia práctica es 5(d): la obra no puede venderse, arrendarse ni alquilarse por sí sola, con independencia de que los aportes propios sean MIT. CPOL además no está aprobada por la OSI y su compatibilidad con licencias copyleft es discutida, lo que conviene tener presente si alguna vez se integra código de terceros bajo esas licencias.

## Cumplimiento pendiente de CPOL 3(c)

La sección 3(c) de CPOL exige «a prominent notice in each changed file stating how, when and where You changed that file». Los archivos heredados de la obra original se han reescrito ampliamente a lo largo de las líneas 1.4 y 1.5 sin incorporar esas notas: hoy la trazabilidad de los cambios descansa únicamente en el historial de Git.

Conviene decidir si se añaden esas notas a los archivos que descienden del volcado original, o si se documenta el historial de Git como mecanismo equivalente. Es el único punto de CPOL que este repositorio no atiende de forma explícita.

## Situación de la atribución

Los forks intermedios `gmcouto` y `mnivet` enlazan el artículo de origen en su README. Este fork había perdido esa referencia: su sección «Origen y estado legal» mencionaba «el trabajo previo de los autores y colaboradores de RightKeyboard» sin nombrar la fuente. La referencia se restauró el 19 de julio de 2026, con independencia de cómo se resuelva la licencia, porque atribuir correctamente no depende de esa decisión.

## Consecuencia operativa

Publicar binarios y aceptar contribuciones externas siguen bloqueados. El criterio registrado en `.agent-context/03-problemas-conocidos.md` es no presentar la línea 1.5 como jurídicamente lista para distribución sin resolver esto; conviene notar que `1.5.0` ya se promovió como estable pese a ese criterio.
