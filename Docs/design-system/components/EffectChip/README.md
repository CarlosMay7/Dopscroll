# EffectChip

Un chip pequeño sobre el vídeo que dice, con un número, por qué el teléfono 1 está ganando menos ahora mismo.

## Cuándo usarlo
- BLURRY −X% (`eyes`) cuando los ojos están borrosos (≥1%).
- BORED −X% (`algo-track`) cuando el nicho del vídeo aburre (≥5%).
- Si hay insignia de vídeo (anuncio, Mythic), los chips bajan a y = 142; si no, van en y = 100.

## Qué aporta quien lo usa
`EffectChip(bg, out Label label)` y `RefreshEffectChips(primary)` en `Desk.cs`. El porcentaje sale de `RunFormulas.BlurDopamineMultiplier` y de `NicheTolerance.Boredom`.

## Hacer / no hacer
- Siempre con número.
- Cada efecto que reste dopamina en el teléfono tiene su chip; no lo escondas solo en el log.
