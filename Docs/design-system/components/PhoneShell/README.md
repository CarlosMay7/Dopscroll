# PhoneShell

El teléfono donde vive cada menú: cuerpo `ink` con `radius-app-phone` y `shadow-phone-glow`, y una pantalla 14px hacia dentro con `radius-app-screen` que recorta todo su contenido.

## Cuándo usarlo
- Cada pantalla fuera de la noche es una app dentro de un teléfono: pantalla de bloqueo (`lock-sky`), Screen Time, Ajustes y Álbum (`app-paper`) y PRYM (`app-cream`, `phone-shop-width`).
- El teléfono del escritorio es un componente aparte (456×912, `radius-desk-phone`, brillo de fiebre).

## Qué aporta quien lo usa
`BuildPhoneShell(parent, x, width, screenColor, out Panel body)` devuelve la pantalla. Dentro van `AppColumn(screen, gap, top)` (inset 24 a los lados, 56–64 arriba, 28 abajo), el contenido y un CTA de ancho completo al final. Para moverlo, anima `body.Position` (el informe se desliza de x = 708 a x = 150).

## Hacer / no hacer
- Una sola app por teléfono.
- Los diálogos (start over?) van dentro del teléfono, sobre `dim-dialog`, nunca como ventana suelta.
