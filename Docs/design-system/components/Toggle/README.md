# Toggle

Un interruptor de ajustes dibujado como pegatina que dice su propio estado: `cash` con ON cuando está encendido y `app-line` plano con OFF cuando está apagado.

## Cuándo usarlo
- Opciones de sí/no en Ajustes: pantalla completa, VSync, movimiento reducido, Safe Screamer y avisos.
- Para elegir entre pocas opciones se usa la fila segmentada: botones `white` y el elegido en `mythic` (tamaño de texto, pestañas del Álbum).

## Qué aporta quien lo usa
`Toggle(bool value, Action<bool> changed)` devuelve el botón; quien lo usa guarda el ajuste (`SaveSettings()`) dentro de `changed`.

## Hacer / no hacer
- La palabra ON/OFF siempre se ve: el estado no depende solo del color.
- Nunca uses un toggle para acciones (borrar partida); eso es un AppButton con confirmación.
