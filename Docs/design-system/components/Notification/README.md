# Notification

Una notificación de la pantalla de bloqueo: burbuja blanca al 92% (`bubble`) con `radius-bubble`, la baldosa de la app (letra o glifo), el nombre y la hora, y una línea de mensaje.

## Cuándo usarlo
- Solo en la pantalla de bloqueo, para contar el estado de la partida con humor: Mom (frase por noche), Slacc (el jefe), Doomscroll (Mythics encontrados) y Screen Time (vídeos y tiempo totales).

## Qué aporta quien lo usa
`AddNotification(list, icon, app, when)` devuelve la etiqueta del mensaje. El icono sale de `LetterTile(letra, color)` o `GlyphTile(glifo, color)`, de 41px. Con el texto al 125% o más se ocultan las opcionales (Slacc y Doomscroll).

## Hacer / no hacer
- Un mensaje de una línea, en minúsculas si "lo escribe" una persona (Mom).
- Las notificaciones informan pero no se pueden pulsar.
