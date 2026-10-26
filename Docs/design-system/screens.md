# Pantallas

Cómo está compuesta cada pantalla del juego. Las posiciones están en el lienzo base de 1920×1080; las capturas están en el grupo de recursos **Screens**.

## Pantalla de bloqueo (menú principal)

Fondo `lock-ground`, con un brillo radial de `screen-glow` al 16% detrás del teléfono.

- **Columna izquierda** (x = 108, y = 160):
  - Wordmark DOOM en `dopamine` y SCROLL en `eyes`, estilo `wordmark` con contorno y sombra de tinta, inclinados (`tilt-logo-doom`, `tilt-logo-scroll`).
  - Chip INCREMENTAL en `cash` (`tilt-chip`).
  - Tarjeta LAST SAVE en `wall`: kicker `sticker` en `mythic`, título 24/900 en `paper`, detalle `body-l` en `muted`.
  - Pie `footer` en `locked-line`.
- **Teléfono centrado** (`phone-app-width`, fondo `lock-sky`):
  - Planeta (`lock-planet`), luna y estrellas (`moon`).
  - Fecha (`lock-date`) y reloj `lock-clock` fijo en 11:59.
  - Cuatro notificaciones (burbujas `white` al 92%, `radius-bubble`, baldosa de 41px): Mom, Slacc, Doomscroll y Screen Time. Con el texto al 125% o más solo quedan Mom y Screen Time.
- **Abajo dentro del teléfono:**
  - CTA SWIPE UP TO SCROLL en `dopamine` (`cta-xl` con `shadow-sticker-lift`), cuya línea inferior dice qué pasará: "Continue night 5", "Back to the shop" o "Start night 1".
  - Tres botones redondos: Settings, Album y Quit.
  - Enlace "Start a new game", que abre un diálogo de confirmación dentro del teléfono.
- **Derecha:** dos post-its con consejos aleatorios y el chip SAFE SCARE MODE IN SETTINGS.

## Escritorio (la noche)

- **HUD:**
  - Pegatina NIGHT N/30 (`eyes`) arriba a la izquierda.
  - Contador de dopamina (`counter`) con la racha debajo.
  - Dinero (`cash`), factura (`fever` o `danger`) y menú arriba a la derecha.
- **Teléfono del escritorio:** pestañas Following / For You, batería (rayo `eyes` al cargar), chips de efecto (BLURRY, BORED) bajo la insignia del vídeo, rail de iconos, título del vídeo y barra de progreso con la línea verde de recompensa.
- **Izquierda:** la ventana (`night-sky` → `dawn`), post-its y el rack de teléfonos con su botón de rol.
- **Abajo a la izquierda:** el medidor OUTLET (`fever`), el botón PLUG IN / CHARGING [C] y la regleta con el cable dibujado: suelto en la mesa, o hasta el teléfono y en `eyes` con corriente al cargar.
- **Abajo, en el centro-izquierda:** el medidor EYES (`eyes`), con la línea BLURRY / RUB YOUR EYES y el porcentaje que se pierde; debajo, BRIGHT y EYE DROPS.
- **Derecha:** el portátil, con Slacc (`slacc-*`), las barras de nicho (que se apagan hacia `locked-line` al aburrirse y muestran una "z") y el log en `terminal`.
- **Avisos de primera vez:** un post-it `mythic` en x = 1360, y = 738, que se cierra con un clic o a los 9 s y aparece una vez por partida.

## Screen Time (informe de la noche)

Teléfono `app-paper`, centrado; se desliza a x = 150 al abrir la tienda.

1. Título `app-title` y subtítulo con la noche y cómo acabó.
2. El héroe: una pegatina inclinada (`tilt-card`) con un número `banner` y una frase. `dopamine` normal, `danger` si hubo corte, `grass` tras tocar pasto y `mythic` al ganar.
3. Tarjeta de filas (`radius-report`, divisores `app-line`): vídeos, mejor racha, dopamina, tu feed, sueldo (`app-good`) y factura (`app-bad`).
4. Nota a pie en `app-muted`.
5. Abajo, OPEN THE SHOP (`mythic`) y el botón de tocar pasto (`grass`), que pide un segundo toque.

## PRYM (la tienda)

Teléfono `app-cream`, de `phone-shop-width`, en x = 720.

- **Cabecera:** PRYM (`brand`), "next-night delivery" y chips de dopamina, dinero y GP. Si no caben, bajan de línea.
- **Pestañas:** Mind, Desk, Snacks, Rack y Grass. La activa va en `dopamine` (`grass` en Grass).
- **Tarjetas:** ver el componente ShopCard.
- **CTA:** START NIGHT N (`eyes`), o POWER CUT · TOUCH GRASS deshabilitado.
- **A la derecha:** un post-it con un consejo según la partida y el panel ARRIVING TOMORROW (`wall`) con una pegatina por compra.

## Ajustes y Álbum

- **Fondo:** teléfono `app-paper` sobre `dim-app`.
- **Ajustes:**
  - Secciones SOUND, SCREEN y COMFORT: etiqueta `sticker-s` en `app-muted` y tarjeta `white` con borde.
  - Cada fila es etiqueta + pista + control (toggle, slider o segmentado).
- **Álbum:**
  - Pestañas Mythics, Trophies y Stats.
  - Tarjetas encontradas: pegatina `white` con baldosa de color.
  - Pendientes: borde `locked-line` y candado.

## Pausa

- Capa `dim-pause` con una tarjeta `wall` inclinada (`tilt-card`).
- Título PAUSED `banner` en `paper`, línea de estado en `muted`.
- Botones RESUME (`eyes`, Esc), SETTINGS y MAIN MENU (`paper`) y SAVE & QUIT (`danger`).
