# Primal Echo: El mundo despertó

Videojuego 2D de plataformas y acción en pixel art, hecho en **Unity 6 (6000.0.38f1)**, con jugabilidad estilo Hollow Knight. Kael explora un mundo donde NÉMESIS, una IA, decidió eliminar a la humanidad para salvar la Tierra.

## Abrir el proyecto
1. Instala Unity **6000.0.38f1** con Unity Hub.
2. Unity Hub → *Add project from disk* → selecciona esta carpeta.
3. Abre `Assets/Scenes/Nivel_01_Valle_del_Eco.unity` y pulsa Play.

## Niveles
- **Nivel 1 · Valle del Eco** y **Nivel 2 · Templo de NÉMESIS**: los mapas base están en `Assets/Niveles/*.txt`.
- Menú **Primal Echo → Niveles** para generar o regenerar (tus cambios de tiles se conservan).
- Leyenda y reglas de diseño en `Assets/Niveles/LEEME_Niveles.md`.

## Cinemática de intro
- Menú **Primal Echo → Cinemática → Generar cinemática de intro**: crea `Assets/Scenes/Cinematica_Intro.unity` y el Timeline `Assets/Cinematicas/Intro_Primal_Echo.playable`, y la pone como primera escena del juego.
- Cuenta lo que pasa primero: NÉMESIS decide eliminar a la humanidad, la naturaleza recupera las ciudades y Kael repara a Kiro. Al terminar (o con Espacio / A) carga el Nivel 1.
- Se edita en **Window → Sequencing → Timeline** con el objeto `Cinematica_Intro` seleccionado. Pistas propias (`Assets/Scripts/Cinematica`): **Narración** (texto del clip), **Plano** (imagen con paneo y zoom), **Fundido a negro** y **Deslizar**.

## Controles
| Acción | Teclado | Control Xbox |
| --- | --- | --- |
| Moverse | A/D o flechas | Stick / cruceta |
| Saltar / doble salto | Espacio | A |
| Lanza (arriba/abajo para dirigir, pogo en el aire) | J / Z / clic izq. | X |
| Dash | Shift / C | RB / RT |
| Arco (una flecha) | K / X / clic der. | Y |
| Fijar apuntado (8 direcciones) | Ctrl | LT |
| Bloquear | L / F | LB |
