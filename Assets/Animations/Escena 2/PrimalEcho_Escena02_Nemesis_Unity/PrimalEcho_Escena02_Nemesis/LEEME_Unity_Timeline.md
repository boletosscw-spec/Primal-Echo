# PRIMAL ECHO — ESCENA 02: EL JUICIO DE NÉMESIS

Paquete de producción visual para Unity 6 Timeline (2D / Pixel Art).

## Contenido
- `01_Fondos/`: sala completa 1920×1080, versiones azul y roja (tomas listas para cámara).
- `02_Sprites/`: el diseño ORIGINAL del usuario (96×96 px, RGBA/transparente) y 48 frames individuales.
  - `01_Azul_Idle`: 8 frames — reposo y parpadeo cian.
  - `02_Azul_Dialogo`: 12 frames — pulso de voz azul.
  - `03_Transformacion`: 16 frames — eye azul → rojo con glitches.
  - `04_Rojo_Dialogo`: 12 frames — voz en estado hostil rojo.
- `03_SpriteSheets/`: atlas de una fila, 96×96 por celda, sin margen.
- `04_Efectos/`: 8 overlays glitch transparentes, 192×96 px.
- `05_Previas/`: 3 composiciones y GIF de referencia.
- `06_Unity/Editor/`: script de menú para crear clips en Unity automáticamente.

## Importación en Unity
1. Copia esta carpeta dentro de `Assets/PrimalEcho/Escena02_Nemesis`.
2. Abre Tools > Primal Echo > Configurar Nemesis (Pixel Art + Clips).
   El script marca las 48 imágenes como Sprite/Point/PPU96 y crea 4 AnimationClips.
3. Coloca el fondo 1920x1080 bajo NÉMESIS en tu escena 2D.
4. Coloca `Nemesis_Azul_Base_96x96.png` en un SpriteRenderer diferente, encima del fondo.
5. Para alinear con la composición de ejemplo: la miniatura de composición usa un sprite de 480×480 píxeles sobre una escena 1920×1080 en posición (720,225) (coordenadas desde esquina superior izquierda). En Unity ajusta ortográfica/escala según tu encuadre.
6. En Timeline agrega un Animation Track para NÉMESIS y coloca, por orden, los clips Azul Idle, Azul Diálogo, Transformación y Rojo Diálogo.
7. Usa el fondo rojo con un Activation Track o una segunda cámara/objeto y anima opacidad durante la transición. Añade glitch como ParticleSystem o SpriteRenderer superpuesto.
8. Pixel Perfect Camera recomendado, Filter Mode=Point, Compression=None.

## Timing sugerido dentro de la escena (20 segundos)
  00:00–00:03  Azul Idle. Voz: «Fui creada para salvar este mundo...»
  00:03–00:09  Azul Diálogo. Tono frío y analítico.
  00:09–00:13  Transformación (16 frames a 8–12 FPS, más pausas o repeticiones) + luces de alerta.
  00:13–00:20  Rojo Diálogo. «Para que la Tierra sobreviva... su dominio debe llegar a su fin».

Nota: el diseño del sprite es exactamente 96×96 px en todos los cuadros. La sala fue ilustrada por separado; es arte de referencia para esta escena, y no incluye capas de parallax independientes.
