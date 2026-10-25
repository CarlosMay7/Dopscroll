# TipToast

Un aviso de primera vez: un post-it amarillo con icono, título en mayúsculas y una frase, que sale una sola vez por partida cuando una mecánica empieza a importar.

## Cuándo usarlo
- Para explicar una mecánica en el momento en que ocurre: batería baja, enchufe al 45%, ojos borrosos, un nicho aburrido, el jefe escribiendo.
- Como mucho uno a la vez y por orden de prioridad: el jefe, la batería, el enchufe, los ojos y el aburrimiento.

## Qué aporta quien lo usa
`TryShowTip(id, glyph, title, body)` en `Tips.cs`. El id se guarda en `SaveData.SeenTips`. Va fijo a la derecha (x = 1360, y = 738, `tip-width`), inclinado +1,5°; se cierra con un clic o a los 9 s. Respeta el ajuste "First-time tips" y el movimiento reducido (sin pop).

## Hacer / no hacer
- El título dice qué pasa ("BLURRY = LESS DOPAMINE"); el cuerpo dice qué hacer y nombra la tecla o el botón.
- No uses avisos para premios o logros: eso es el toast de logros, también `mythic`, arriba a la izquierda.
