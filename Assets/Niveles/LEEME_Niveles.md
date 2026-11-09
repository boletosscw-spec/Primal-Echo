# Niveles de Primal Echo (Primal 1.1)

Cada nivel es un archivo de texto en `Assets/Niveles`. Para generarlos o actualizarlos usa **Primal Echo → Niveles → Generar Nivel 1 y Nivel 2**. El constructor copia `SampleScene` (Kael, cámara, parallax y luz global), repinta el mapa con la **Tile Palette de Jungla** y coloca enemigos y objetos. `SampleScene` no se modifica.

## Leyenda (1 carácter = 1 tile)

| Carácter | Qué es |
| --- | --- |
| `#` | Tierra de jungla con césped (autotiling). |
| `=` | Piedra de ruinas gris. |
| `-` | Rama: se atraviesa saltando desde abajo. |
| `%` | Piedra que se derrumba al pisarla. |
| `J` | Géiser: columna de agua de 5 tiles que levanta a Kael. |
| `^` | Planta espinosa: hace daño y permite pogo con la lanza hacia abajo. |
| `:` | Pared de cueva al fondo, sin colisión. |
| `G` | Compuerta de la arena de emboscada. |
| `E` | Robot centinela: 4 de vida, 2 lanzazos. |
| `B` | Robot pesado: 10 de vida, 2 de daño. |
| `W` | Avispa voladora: 2 de vida. |
| `K` | Planta carnívora fija: 4 de vida. |
| `C` | Checkpoint con farola. Toma los nombres de las líneas `@checkpoint`, en orden. |
| `o` | Fragmento de eco (opcional). |
| `T` | Árbol grande de fondo. |
| `P` / `X` | Inicio / salida. Si hay `@siguiente`, la salida carga ese nivel. |

## Alcance de Kael (valores actuales de PlayerController)

| Movimiento | Alcance |
| --- | --- |
| Salto | ~2,7 tiles de alto, ~6 de largo |
| Doble salto | ~4,9 de alto, ~9–10 de largo |
| Doble salto + dash | ~11–12 de largo |
| Géiser | ~7–9 de alto, más el doble salto |

El salto en pared no es obligatorio en ninguna parte: es un atajo.

Para validar los mapas con la física simulada de Kael y estimar la duración:

```
cd Tools/niveles
python3 jsim.py ../../Assets/Niveles/*.txt
```

| Nivel | Experto | Primera vez |
| --- | --- | --- |
| Nivel 1 · Valle del Eco (600×30) | ~3,1 min | ~5–6,5 min |
| Nivel 2 · Templo de NÉMESIS (461×40) | ~3,0 min | ~5–6,5 min |
