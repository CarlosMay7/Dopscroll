# ReportCard

El informe Screen Time: una pegatina destacada e inclinada con un número grande y una frase, y debajo una tarjeta blanca de filas (etiqueta a la izquierda, valor a la derecha) separadas por divisores `app-line`.

## Cuándo usarlo
- Al terminar cada noche, para contar qué pasó.
- El héroe cambia de color según el resultado: `dopamine` normal, `danger` en un corte de luz, `grass` tras tocar pasto y `mythic` al ganar la campaña.

## Qué aporta quien lo usa
`FillNightReport(...)`, `SetReportHero(color, big, text)` y `AddReportRow(label, value, valueColor, last)`. Las etiquetas de fila hacen autowrap para que al 150% el valor no se corte.

## Hacer / no hacer
- Valores en `ink`; dinero ganado en `app-good` y perdido en `app-bad`.
- El héroe siempre con una frase de tono seco: "Proud of you?".
- No más de 6 filas; lo demás va en la nota al pie.
