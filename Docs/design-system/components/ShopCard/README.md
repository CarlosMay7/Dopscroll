# ShopCard

Una tarjeta de la tienda PRYM: pegatina blanca, baldosa con el color de la categoría, nombre y nivel, descripción, efecto "actual → siguiente", una línea roja de lo que falta cuando no se puede comprar y el chip de precio con el color de la moneda.

## Cuándo usarlo
- Cada mejora de la tienda.
- Variantes:
  - Comprable.
  - Faltan recursos: chip `app-line` y línea `app-bad`.
  - Al máximo: MAX, o "owned" si es de un nivel.
  - De una noche: "tonight only" / READY.
  - Bloqueada: borde `locked-line`, candado y "Unlocks on night N.".
- La tarjeta de rol del rack y POST (modo creador) reutilizan `ShopCard` con otra acción.

## Qué aporta quien lo usa
Los datos vienen de `Data/balance.json` (`name`, `desc`, `icon`, `category`, `unlockNight`, `perNight`). El texto del efecto sale de `UpgradeEffects.At(cfg, def, level)`. Se construye con `BuildShopCard(upgrade)`, `ShopCard(icon, tile, name, levelText, desc, effect, shortLine, action)` y `LockedCard(name, condition)`.

## Hacer / no hacer
- El color de la baldosa es la categoría (Mind `dopamine`, Desk `eyes`, Snacks `mythic`, Rack `violet`, Grass `grass`); el del precio, la moneda.
- La descripción no lleva números: los números van en la línea de efecto, calculados desde los datos.
