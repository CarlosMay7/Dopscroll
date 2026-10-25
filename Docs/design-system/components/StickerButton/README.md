# StickerButton

El botón del juego en el escritorio y en los menús: una pegatina plana con borde `ink` de 3px, sombra dura `shadow-sticker` y texto Bungee en `ink`.

## Cuándo usarlo
- Toda acción en el escritorio, en la pausa y en el menú (PLUG IN, EYE DROPS, BRIGHT, RESET BREAKER, RESUME, SAVE & QUIT).
- El CTA principal de la pantalla de bloqueo (SWIPE UP TO SCROLL) es la variante grande: `cta-xl`, `radius-bubble`, `shadow-sticker-lift`.

## Qué aporta quien lo usa
`GlowStyle.StyleStickerButton(button, bg, fg, font, fontSize, radius, padX, padY)`. El fondo es un token de señal según el significado; `fg` siempre es `GlowStyle.Ink`. Valores por defecto: `sticker-s`/18px, `radius-sticker`, 14×8 de relleno. En el escritorio: `sticker-s` (14px), `radius-chip`, 10×6.

## Estados
- Hover: el fondo se aclara 14%.
- Pulsado: `shadow-sticker-pressed` y el texto baja 3px.
- Deshabilitado: `desk-edge` con texto `muted`.
- Activo/encendido: cambia el fondo (PLUG IN en `paper` → CHARGING en `eyes`).

## Hacer / no hacer
- Pon la tecla entre corchetes en el texto: `[C]`, `[UP]`, `[Esc]`.
- Los botones del juego no toman foco (`DisableButtonFocus`); los de menú sí.
- No uses texto blanco ni bordes de color.
- Dentro de un teléfono claro usa AppButton.
