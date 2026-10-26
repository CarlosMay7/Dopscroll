# 3 AM Glow · design system

Copia en archivos del design system publicado en https://claude.ai/artifact/GhH8b8TuEwhFLF1X61Wwuy (privado: solo lo abre quien tenga acceso).

- `tokens.json`: colores, tipografía, espaciado, radios, sombras, inclinaciones, tamaños y opacidades. Deben coincidir con `Src/Presentation/UI/GlowStyle.cs`; si cambias uno, cambia el otro.
- `screens.md`: cómo está compuesta cada pantalla.
- `components/<Nombre>/README.md`: cuándo usar cada componente y qué función del código lo construye. `preview.html` es una recreación estática en HTML; se abre en el navegador.
- `assets/Icons`: los iconos de `GlowGlyph` exportados a SVG. `assets/Screens`: capturas de referencia.
- Las fuentes son las del juego, en `Assets/Fonts`.

---

3 AM Glow es la dirección de arte de Doomscroll Incremental: la habitación está oscura y tranquila, el teléfono es la única luz y cada pieza de interfaz es una pegatina (borde de tinta de 3px, sombra dura debajo, esquinas redondeadas). Todo lo que se añada al juego sigue estas reglas; los valores exactos están en `tokens.json` y en `Src/Presentation/UI/GlowStyle.cs`, y deben coincidir.

## Principios

1. **El teléfono brilla, la habitación no.** Fondos de la noche en `wall`, `desk` y `lock-ground`; la luz viene de `screen-glow` alrededor de los teléfonos. No pongas colores de señal a gran tamaño en la habitación salvo un estado (fiebre, apagón).
2. **Cada cosa interactiva o importante es una pegatina.** Borde `stroke-sticker` en `ink`, sombra `shadow-sticker`, radio según su papel. Lo que no es pegatina (texto suelto, barras finas, el fondo dibujado) no lleva borde ni sombra.
3. **Un color = un significado.** El rosa siempre es dopamina, el lima siempre es dinero, el cian siempre son los ojos y la carga. Nunca uses un color de señal como decoración con otro significado.
4. **Los teléfonos de las apps son claros, la noche es oscura.** Dentro de una app (Screen Time, PRYM, Ajustes, Álbum) el fondo es `app-paper` o `app-cream` y las tarjetas `white`; fuera, todo es noche.

## Voz y textos

- **El juego habla en inglés**, en segunda persona, corto y con humor seco sobre la adicción al scroll. Nada de emojis en la interfaz (solo el log del portátil los usa).
- **Tres registros, tres tipografías:**
  - MAYÚSCULAS en Bungee para el HUD, botones y etiquetas: `SWIPE UP TO SCROLL`, `START NIGHT 6`, `PLUG IN [C]`, `BILL TOMORROW`.
  - Frases normales en Nunito dentro de las apps: "Swipe sooner and still collect the full reward.", "Night 5 report · dawn at 6:00 AM".
  - minúsculas a mano en VT323 para post-its y el portátil: "bought tonight = on your desk tomorrow night", "the battery won't last the night. press C to plug in."
- **Las teclas van entre corchetes** en el botón que activan: `REPLY TO BOSS [UP]`, `PLUG IN [C]`.
- **El separador es el punto medio** (` · `): "Touch grass now · +13 GP", "x6 · 5 fevers".
- **Los números grandes** usan `FormatBig` (213K, 1.24M) y el dinero `FormatCash` ($110.5).
- **Ejemplos de tono:** "more scrolling than last night. Proud of you?", "Nothing yet. Your cart is as empty as your sleep schedule.", "PHONE #1 IS DYING".

## Color

**Colores de señal** (texto siempre en `ink` encima):

| Token | Significa | Dónde |
| --- | --- | --- |
| `dopamine` | dopamina, likes, acción principal | contador, pestaña Mind, precios en dopamina, SWIPE UP |
| `cash` | dinero, sí/encendido | badge de dinero, precios en $, toggles ON, POST |
| `eyes` | ojos, carga, empezar | medidor de ojos, cargador, START NIGHT, pestaña Desk |
| `mythic` | lo raro, consejos | Mythics, post-its, avisos, OPEN THE SHOP, Snacks |
| `fever` | fiebre, enchufe, avisos de factura | banner de fiebre, medidor del enchufe, BILL |
| `danger` | peligro, borrar, sin batería | apagón, factura hoy, DELETE AND START |
| `grass` | tocar pasto | botón de prestigio, pestaña Grass, chip GP |
| `violet` | el rack | pestaña Rack, rol Mythic Radar |

- Sobre `wall` y `desk`: texto en `paper` o `muted`. Sobre `lock-sky`: blanco.
- Dentro de las apps: títulos y valores en `ink`, texto secundario en `app-text`, leyendas en `app-muted`, ganancias en `app-good` y pérdidas en `app-bad`.
- Un botón deshabilitado es `desk-edge` en la noche y `app-line` dentro de una app, con el texto en `muted` o `app-muted`.
- Todo lo bloqueado lleva borde `locked-line` sobre `app-cream`, con un candado. Nunca uses `locked-line` como color de texto.
- Las capas que oscurecen van en `ink` con estas opacidades: `dim-report` detrás del informe y la tienda (el escritorio sigue visible), `dim-app` detrás de Ajustes y Álbum, `dim-pause` detrás de la pausa.

## Tipografía

- **Bungee** (`display`) para todo lo que se lee de un vistazo: números, botones, etiquetas, títulos de pegatina. Solo mayúsculas. El tamaño mínimo es `sticker-xs` (12px).
- **Nunito** (`body`), siempre a peso 800 (`GlowStyle.Body`) o 900 (`GlowStyle.BodyBlack`), para el texto de las apps y los párrafos. 17px (`body`) es el tamaño por defecto del tema.
- **VT323** (`mono`) solo para post-its, el portátil (Slacc) y el pie del menú.
- Usa los estilos de `type.groups`. Si necesitas un tamaño nuevo, añádelo primero a los tokens.
- Los números grandes llevan contorno de tinta (`outline_size`): 14px en el contador y 12px en los "+X" voladores.
- **Tamaño de texto (ajustes 100/125/150%):** `GlowStyle.ApplyTextScale` solo agranda las fuentes de 22px o menos; los titulares no crecen. Todo texto de lectura nuevo debe crearse con `GlowStyle.MakeLabel` o dentro de un nodo al que luego se aplica `ApplyCurrentTextScale`. Las etiquetas largas llevan autowrap (`WrapLabel`) para que al 150% bajen de línea en vez de desbordar.

## Pegatinas (forma)

- **La receta** es `GlowStyle.Sticker(bg, radius, border, shadow, padX, padY)`: fondo plano, borde `stroke-sticker` de tinta y sombra `shadow-sticker` sin desenfoque. Los paneles usan `StickerPanel` y los botones `StyleStickerButton`.
- **Estados de un botón:**
  - *Hover:* el fondo se aclara un 14%.
  - *Pulsado:* la pegatina se hunde (`shadow-sticker-pressed`) y el contenido baja 3px.
  - *Deshabilitado:* `desk-edge` en la noche; `app-line` sin sombra dentro de una app (`StyleAppButton`).
  - *Foco:* sin anillo. Los botones del juego no toman foco; los de menú sí.
- **Radios por papel:** `radius-chip` en chips, toggles y pegatinas del HUD; `radius-sticker` en botones; `radius-card` en tarjetas y CTAs; `radius-panel` en secciones; `radius-bubble` en burbujas y diálogos. Los post-its son cuadrados.
- **Inclinación:** las pegatinas sueltas se inclinan un poco (`tilt-*`, entre 1 y 5 grados) para parecer pegadas a mano. Los contenedores de Godot resetean la rotación, así que una pegatina inclinada dentro de un `VBox`/`HBox` va envuelta con `Tilted(control, grados)`. Las listas, tarjetas y botones de las apps nunca se inclinan.

## Teléfonos

- **La carcasa es una sola** (`BuildPhoneShell`):
  - Cuerpo `ink` con `radius-app-phone` y `shadow-phone-glow`.
  - Pantalla recortada con `radius-app-screen`, 14px hacia dentro.
  - `phone-app-width` × `phone-app-height`; `phone-shop-width` para PRYM.
- **Dentro de una app,** la columna empieza a `space-56` del borde superior y a `space-24` de los lados. El orden es:
  1. Botón Back.
  2. Título `app-title`.
  3. Subtítulo `body` en `app-muted`.
  4. Contenido con scroll.
  5. CTA de ancho completo abajo.
- **El teléfono del escritorio** es otro: `desk-phone-width`, `radius-desk-phone`, `shadow-desk-phone-glow`, y `shadow-fever-glow` con borde `dopamine` de 6px durante la fiebre.
- **Fondos del vídeo:** van por nicho (`video-cats`, `video-gaming`…) y por tipo (`video-ad`, `video-mythic`, `video-screamer`, `video-off`).

## Layout

- **Lienzo base de 1920×1080.** `Place(parent, control, HAnchor, x, y, w, h)` ancla cada grupo a la izquierda, al centro o a la derecha para pantallas más anchas.
- **Zonas del escritorio:**
  - Arriba a la izquierda, la noche.
  - A la izquierda, la ventana y el rack (y = 532).
  - Abajo a la izquierda, el enchufe, el cargador y la regleta (y ≈ 862–1060).
  - En el centro, el teléfono y el contador.
  - Abajo, en el centro-izquierda, los ojos.
  - A la derecha, el portátil.
  - Abajo a la derecha, el reloj y los avisos (`tip-width`, x = 1360, y = 738).
  - Arriba a la derecha, el dinero, la factura y el menú.
- **Nunca tapes** el teléfono, el rack ni el enchufe con un aviso o un post-it.
- **Pantallas de teléfono:** centrado, x = 708. Con la tienda abierta, el informe va en x = 150, PRYM en x = 720 y las notas a la derecha (x ≥ 1340).
- **Espaciado:** usa los tokens `space-*` en los `separation` de los contenedores, no márgenes sueltos.

## Movimiento

- Las animaciones son cortas, de 0,25 a 0,35 s, con `Back` o `Cubic` en `Out`: un pop al aparecer un aviso, el informe deslizándose a la izquierda al abrir la tienda, un rebote del contador.
- Lo continuo es ambiente: el ventilador gira, la corriente fluye por el cable al cargar, el banner de fiebre late y lo que necesita atención parpadea (batería baja, enchufe al 80%).
- **Movimiento reducido** (Ajustes) apaga los pops, sacudidas, números voladores, latidos y deslizamientos. Todo componente nuevo con animación debe consultar `ReducedMotion`.

## Iconografía

- **Los iconos son trazos de línea** dibujados por código en `GlowGlyph` sobre una rejilla de 24, con trazo `stroke-glyph`, puntas redondas y un solo color (`ink`). El grupo **Icons** tiene cada uno exportado a SVG con la misma geometría.
- **Normalmente van sobre una baldosa:** pegatina cuadrada con radio del 27% del lado, borde de 3px y color de la categoría (Mind `dopamine`, Desk `eyes`, Snacks `mythic`, Rack `violet`, Grass `grass`).
- **Variantes:** baldosa redonda translúcida y trazo blanco en los botones de la pantalla de bloqueo; sin baldosa y en `eyes` para el rayo de carga.
- **Un icono nuevo** se añade a `GlowGlyph.DrawGlyph` y, si lo usa la tienda, a `BalanceConfig.KnownIcons`.
- **No hay logo dibujado:** la marca es el wordmark tipográfico DOOM / SCROLL en Bungee con el chip INCREMENTAL.

## Accesibilidad

- Todo par de texto de este sistema supera 4,5:1. Las notas de cada token dicen sobre qué fondos va.
- **Un estado nunca se marca solo con color:** lleva palabra o número (BLURRY −17%, BORED −15%, POWER CUT, MAX, READY).
- **Ajustes:**
  - Música y efectos.
  - Pantalla completa y vsync.
  - Tamaño de texto.
  - Movimiento reducido.
  - Safe Screamer (sustos convertidos en una tarjeta tranquila).
  - Avisos de primera vez (con "Show tips again").
  - Todo se guarda en `user://settings.cfg`, aparte de la partida.
- **Las teclas** se muestran en el propio botón y funcionan siempre: Esc pausa y vuelve atrás.

## En el código

- **`GlowStyle.cs`:** paleta, fuentes (`Display`, `Body`, `BodyBlack`, `Mono`), `Sticker`, `StickerPanel`, `StyleStickerButton`, `Flat`, `StyleBar`, `StyleSlider`, `MakeLabel`, `ApplyTextScale`, `FormatBig`, `FormatCash`.
- **`GlowGlyph.cs`:** los iconos.
- **`DeskSceneController.Menus.cs`:** `BuildPhoneShell`, `StyleAppButton`, `Chip`, `PostIt`, `Tilted`, `ShopCard`, `LockedCard`, `Toggle`, `SettingsRow`, `AddReportRow`.
- **`DeskSceneController.Tips.cs`:** los avisos.
- **`DeskSceneController.Desk.cs`:** el escritorio y el HUD.
- **`DeskEnvironmentBackdrop.cs`:** la habitación dibujada, con la regleta y el cable.
- **Datos de la tienda:** el nombre, la descripción, el icono y la categoría de cada mejora vienen de `Data/balance.json`.

## Hacer y no hacer

**Hacer**
- Reutiliza las fábricas de `GlowStyle` y los constructores de `Menus.cs`; un componente nuevo empieza copiando el más parecido.
- Escribe los colores como `GlowStyle.<Nombre>` y añade a `GlowStyle` cualquier color nuevo antes de usarlo.
- Revisa cada pantalla nueva al 150% de texto y con movimiento reducido.

**No hacer**
- Degradados, sombras difuminadas en pegatinas, bordes de color distinto a la tinta.
- Colores de señal fuera de su significado, o texto blanco sobre ellos.
- Emojis en la interfaz, o tamaños de fuente fuera de la escala.
- Pegatinas sin inclinar en el escritorio, o inclinadas dentro de una lista.
