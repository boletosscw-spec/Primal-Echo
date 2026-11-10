# KIT DE ESCENARIO – PRIMAL ECHO

Referencia: imagen de 800 × 449 píxeles, composición original en una sola capa.

## Contenido
- **01_Parallax/**: 9 planos PNG RGBA a 800×449; importa con Sprite Mode: Single.
- **02_Sprites_Recortados/**: 18 recortes PNG transparentes de árboles, helechos, ruinas, suelo y ramas, con posición en `manifest.json`.
- **03_Tiles_32px/**: 97 piezas individuales originales 32×32, con paleta adaptada a la referencia.
- **04_Atlas/**: atlas sin separación `Tileset_32px.png` (10 columnas × 10 filas) + lista exacta de sus celdas.
- **05_Unity/**: script sencillo de parallax, opcional.
- **06_Vistas_Previas/**: hojas de contacto y preview del resultado.

## Importación en Unity 6
1. Arrastra carpetas con PNG a `Assets/Art/PrimalEcho`.
2. Selecciona el atlas: Texture Type = Sprite (2D and UI), Sprite Mode = Multiple, Filter Mode = Point, Compression = None, Pixels Per Unit = 32. En Sprite Editor: Slice > Grid by Cell Size > 32×32, sin margen ni separación.
3. Alternativamente usa directamente los PNG de `03_Tiles_32px` como Sprite (2D and UI), Single, Filter Point y PPU 32.
4. Crea Tilemap > Rectangular y Tile Palette para disponer suelo, piedra, plataformas, plantas y ruinas.
5. Para parallax, crea 9 objetos `SpriteRenderer` con imágenes `00` a `08`, **todos en la misma posición**, **misma escala y PPU = 32**. Orden de render del más lejano al más cercano: 0 a 8.
6. Adjunta `PrimalEchoParallax.cs` a los objetos y establece factor desde `manifest.json`. Cámara ortográfica inicial sugerida: Size = 7, ya que la imagen mide 449 px de alto y PPU=32. Para layers 01–08: Filter Mode = Bilinear (imagen de referencia no es pixel art puro).
7. El suelo de la imagen es decorativo; añade colliders 2D al Tilemap del terreno real.

## Límites de la extracción
- La imagen suministrada es una ilustración plana: detrás de árboles y plataformas **no existen píxeles recuperables**. Por eso el cielo y bosque distante se reconstruyeron, mientras el terreno, dosel y algunos recortes usan píxeles del original.
- El recorte automático por color produce bordes aproximados, y ciertos recortes pueden conservar zonas indeseadas. Es una base editable, no una separación profesional perfecta.
- La referencia incluye una marca de agua de una fuente externa. Revisa el derecho de uso del material antes de distribuir el juego; las capas reconstruidas de fondo y los tiles nuevos no copian esa marca.
- El tamaño es limitado por los 800×449 píxeles originales: los sprites nuevos de 32×32 fueron dibujados con geometría pixel art compatible con Tilemap.
