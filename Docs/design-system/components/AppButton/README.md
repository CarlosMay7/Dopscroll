# AppButton

El botón dentro de una app clara del teléfono: es un StickerButton cuyo estado deshabilitado pasa a ser un chip plano `app-line` con texto `app-muted`, en vez del relleno oscuro del escritorio.

## Cuándo usarlo
- Chips de precio en la tienda: dopamina en `dopamine`, dinero en `cash`, GP en `grass`.
- CTAs de ancho completo (`cta`, `radius-card`): OPEN THE SHOP en `mythic`, START NIGHT en `eyes` y el prestigio en `grass`.
- Botones de las apps: Back, pestañas y opciones de los ajustes.

## Qué aporta quien lo usa
`StyleAppButton(button, bg, font, fontSize, radius = 16, padX = 14, padY = 10)`. Los textos largos (el prestigio, START NIGHT) llevan `AutowrapMode = WordSmart` para que al 150% bajen de línea.

## Estados
- Deshabilitado: no se puede pagar, MAX, READY o POWER CUT.
- "FREE" cuando el precio es 0.
- Seleccionado (pestaña o opción): fondo de señal; sin seleccionar: `white`.

## Hacer / no hacer
- Un solo CTA de ancho completo por zona de la pantalla.
- El color del chip de precio es la moneda, nunca la categoría.
