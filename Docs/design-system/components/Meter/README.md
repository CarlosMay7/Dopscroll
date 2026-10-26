# Meter

Un medidor del HUD: una pegatina inclinada con etiqueta en mayúsculas y una barra con contorno de tinta (`stroke-bar`), del color de lo que mide.

## Cuándo usarlo
- EYES (`eyes`): relleno `paper`, que parpadea a `danger` por debajo de 25%. Debajo, la línea BLURRY / RUB YOUR EYES con el % que se pierde.
- OUTLET (`fever`): relleno `mythic`, que parpadea a `danger` desde el 80%.
- La batería del teléfono es una barra mini: `cash`, `danger` por debajo de 20% y `eyes` con el rayo mientras carga.

## Qué aporta quien lo usa
`GlowStyle.StickerPanel(color, 12, 4, 12, 8)` + `GlowStyle.StyleBar(bar, fill, ink, radius, 2)`, que devuelve la caja de relleno para recolorearla cada frame sin crear objetos nuevos. La inclinación va con `Tilted(sticker, ±2)`.

## Hacer / no hacer
- El valor va escrito en la etiqueta, no solo en la barra.
- Los parpadeos se apagan con movimiento reducido: queda el color fijo.
