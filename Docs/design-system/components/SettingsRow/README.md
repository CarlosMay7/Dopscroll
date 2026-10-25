# SettingsRow

Una sección de ajustes: una etiqueta Bungee en `app-muted` sobre una tarjeta blanca con borde de tinta de 3px (`radius-panel`, sin sombra), con filas de etiqueta + pista + control.

## Cuándo usarlo
- En Ajustes (SOUND, SCREEN, COMFORT) y en Album > Stats, para listas de etiqueta y valor.
- Los controles son Toggle, la fila segmentada, el slider (`GlowStyle.StyleSlider`: pista `app-line`, relleno del color del canal, pomo blanco) o un valor en Bungee.

## Qué aporta quien lo usa
`AddSettingsSection(content, "TITLE")` devuelve el contenedor de filas, y `SettingsRow(rows, label, hint, control)` añade una fila. Los volúmenes usan `AddVolumeRow`, que guarda el ajuste al soltar el slider.

## Hacer / no hacer
- La pista explica la consecuencia en una frase ("Off can feel snappier, but may tear.").
- Todo ajuste se aplica al momento y se guarda en `user://settings.cfg`, sin botón de guardar.
