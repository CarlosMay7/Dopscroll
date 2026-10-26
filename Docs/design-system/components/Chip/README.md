# Chip

Una etiqueta Bungee pequeña sobre una pegatina sin sombra, para valores y textos cortos que no son botones.

## Cuándo usarlo
- El monedero de la tienda: dopamina, dinero y GP, cada uno en su color.
- Las compras de la noche en ARRIVING TOMORROW: colores rotando e inclinadas.
- El chip INCREMENTAL y SAFE SCARE MODE de la pantalla de bloqueo.

## Qué aporta quien lo usa
`Chip(text, bg, out Label label, fontSize = 17)` en `Menus.cs` (por dentro, `StickerPanel` con sombra 0 y relleno 10×3). Si va dentro de un contenedor y se inclina, envuélvelo con `Tilted()`.

## Hacer / no hacer
- Un chip con un número usa el color de lo que cuenta.
- Si se puede pulsar, no es un chip: es un StickerButton o un AppButton.
