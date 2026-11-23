# Arte del Nivel 2 · Base de NÉMESIS

La primera mitad del nivel usa el arte del Nivel 1 (bosque de musgo). La base tecnológica usa piezas
**generadas por código** (`Tools/niveles/arte_base2.py`) en `Assets/Arte/Nivel2/Estaticos/`. Para cambiarlas por arte final,
pídeselas a ChatGPT con el bloque de estilo de siempre y **reemplaza el PNG con el mismo nombre y tamaño**
(no toques el `.meta`). Después vuelve a construir el Nivel 2 (crea el archivo `Logs/construir_nivel2_musgo.flag`).

Bloque de estilo para pegar primero:
```
Estilo: pixel art 2D pintado para un plataformas tipo Hollow Knight: Silksong, formas con contorno oscuro y sombreado suave.
Una base tecnológica antigua de una IA (NÉMESIS) que la selva está reclamando: acero azul grisáceo, remaches, luces cian,
algo de óxido y musgo. Fondo TRANSPARENTE (o magenta #FF00FF sólido para recortar), sin texto, vista lateral.
```

| Archivo | Tamaño (px) | Qué pedir |
|---|---|---|
| `base_bloque.png` | 512 × 320 | Pieza de 9 cortes para pisos y techos: borde de placas de metal de **48 px** en los 4 lados (arriba más claro, con remaches y una línea cian) y **centro totalmente negro**. El borde de arriba debe poder repetirse a lo ancho sin costura. |
| `base_fondo.png` | 512 × 512 | Pared interior oscura con paneles, juntas y una tubería; **debe repetirse sin costura** en las 4 direcciones; poco contraste. |
| `base_plat.png` | 420 × 120 | Plataforma de rejilla de metal con placa arriba y celosía abajo, dos soportes. |
| `base_tubo.png` | 512 × 90 | Tubería horizontal con bridas; que se repita a lo ancho. |
| `base_pantalla.png` | 220 × 170 | Monitor antiguo con pantalla cian brillante y una gráfica. |
| `base_servidor.png` | 170 × 340 | Rack de servidores con luces pequeñas (cian, verde, rojo). |
| `base_cable.png` | 360 × 220 | Dos o tres cables gruesos colgando en curva desde arriba. |
| `base_rejilla.png` | 200 × 130 | Rejilla de ventilación con remaches. |
| `base_columna.png` | 150 × 620 | Columna de metal con anillos remachados y una línea cian. |
| `base_alarma.png` | 90 × 90 | Lámpara de alarma roja (domo) con base. |
| `base_electrico.png` | 300 × 90 | Piso electrificado: placa con picos y chispas cian. |
| `base_tanque` (animación) | 170 × 380 por cuadro | Tanque de vidrio con líquido verde azulado y una cría de dinosaurio enroscada adentro; burbujas que suben. |
| `base_consola.png` | 280 × 170 | Consola de control: teclas de colores, perillas, dos pantallitas y una palanca roja. |
| `base_logo.png` | 220 × 220 | Emblema de NÉMESIS: un ojo rojo dentro de un hexágono de metal. |
| `base_puerta_fondo.png` | 320 × 420 | Compuerta blindada sellada (fondo), con franjas amarillas y luz roja al centro. |
| `base_raiz.png` | 360 × 300 | Raíces gruesas que entran por el techo y cuelgan, con musgo. |
| `base_franja.png` | 512 × 36 | Franja de peligro amarilla y negra (que se repita a lo ancho). |
| `base_viga.png` | 512 × 70 | Viga de celosía para el techo (que se repita a lo ancho). |
| `base_caja.png` / `base_barril.png` | 150 × 120 / 90 × 130 | Caja militar y barril con símbolo de peligro. |
| `base_lampara.png` | 80 × 160 | Lámpara colgante con foco cálido. |
| `base_engranaje.png` | 320 × 320 | Engrane enorme (se usa oscuro, al fondo). |
| `base_antena.png` | 260 × 900 | Torre de antena con platos y luz roja arriba (se ve a lo lejos desde el bosque). |
| `base_escombro.png` | 260 × 120 | Montón de chatarra con musgo. |
| `base_tuberia_v.png` | 90 × 512 | Tubería vertical (que se repita a lo alto). |

Animaciones en `Assets/Arte/Nivel2/Animaciones/` (hoja de cuadros en una o dos filas, 2 px entre cuadros) y su primer cuadro
como `<nombre>_f0.png` en `Estaticos`: `base_monitor` (8), `base_rack` (6), `base_baliza` (8), `base_tanque` (12),
`base_chispas` (10), `base_vapor` (12), `base_ventilador` (8). Si reemplazas una hoja, conserva el tamaño de cuadro y el número de cuadros.
Todo este arte lo genera `Tools/niveles/arte_base2.py`.

## Aeris (jefe)

Hojas de animación en `Assets/Resources/Jefes/Aeris/` (frames en **una sola fila**, mirando a la **derecha**,
fondo magenta para recortar). Usa la hoja de personaje de Aeris que ya le pediste a ChatGPT como referencia:

`Vuelo`(8) · `Aleteo`(6) · `Apuntar`(4) · `Picada`(4) · `Clavado`(6) · `Huevo`(5) · `Plumas`(5) · `CargarRayo`(6) · `Danado`(8) · `Muerte`(8)

Opcionales: `Pluma.png`, `HuevoMina.png`. Las animaciones *Panel* y *Agarre* de la lista vieja ya no se usan.
`Compuerta.png` y `Escombro.png` de esa carpeta son provisionales (la compuerta mide 150 × 620).

Mientras falten las hojas, Aeris se ve como un bloque gris y la pelea ya se puede jugar.
Cuando ChatGPT las termine, avísame: les quito el magenta y las acomodo como hice con Atlas.
