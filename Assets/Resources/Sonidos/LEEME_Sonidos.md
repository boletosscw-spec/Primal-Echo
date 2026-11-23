# Sonidos de Primal Echo

El juego **ya suena**: cada efecto tiene una versión sintetizada por código (`Scripts/Audio/Sintesis.cs`).
Para reemplazar uno por un sonido real, guarda el archivo **en esta carpeta** con el nombre exacto de la tabla.

- Formato: `.wav` u `.ogg` (también sirve `.mp3`). Mono, sin silencio al inicio, volumen normalizado.
- Variantes: agrega `_1`, `_2`, `_3`... (por ejemplo `kael_paso_1.wav`, `kael_paso_2.wav`) y el juego elige una al azar en cada reproducción.
- Si borras un archivo, vuelve el sintetizado. No hay que tocar código.
- Volumen general de efectos: Opciones → **Efectos**.

Los prompts están en inglés porque los generadores de efectos (ElevenLabs Sound Effects, etc.) los entienden mejor.
Pide siempre: *"single sound effect, no music, no voice, dry"* si el generador añade cosas extra.

## Kael

| Archivo | Qué es | Prompt | Duración |
|---|---|---|---|
| `kael_paso` (+ variantes) | Paso sobre musgo/roca | soft footstep on mossy stone, light boots, 2D game | 0.1 s |
| `kael_salto` | Salto | quick light whoosh of a jump, cloth movement, 2D platformer | 0.2 s |
| `kael_doble_salto` | Doble salto | airy magical double jump whoosh with a soft sparkle | 0.3 s |
| `kael_dash` | Dash | fast air dash whoosh, swift and sharp, 2D action game | 0.3 s |
| `kael_aterrizaje` | Caída fuerte | landing thud on stone with a little dust, boots | 0.25 s |
| `kael_lanza` | Lanzazo | spear thrust swish, quick wooden staff swing | 0.15 s |
| `kael_golpe` | La lanza conecta | spear hit impact on metal robot armor, punchy | 0.2 s |
| `kael_arco` | Disparo de arco | bow string release twang with arrow whoosh | 0.3 s |
| `flecha_impacto` | Flecha pega | arrow hitting metal plate, short thunk | 0.15 s |
| `kael_dano` | Recibe daño | young hero gets hit, impact with short pained grunt | 0.3 s |
| `kael_muerte` | Muere | hero defeated, falling impact and fading tone, 2D game | 1 s |
| `kael_rebote` | Rebote con pogo | springy bounce off an enemy, cartoon boing, short | 0.15 s |
| `hongo` | Hongo rebotador | big bouncy mushroom boing, squishy and cartoony | 0.3 s |

## Kiro (robot compañero)

| Archivo | Qué es | Prompt | Duración |
|---|---|---|---|
| `kiro_destello` | Destello | bright magical light flash burst, shimmering chime, energy | 0.8 s |
| `kiro_pulso` | Pulso (con Eco) | deep energy pulse shockwave with electric ring, sci-fi | 0.6 s |
| `kiro_pitido` | Pitido / feliz | cute small robot happy chirp beep | 0.1 s |
| `kiro_alerta` | Alerta | small robot alert double beep, curious | 0.25 s |

## Atlas (tricerátops biomecánico)

| Archivo | Qué es | Prompt | Duración |
|---|---|---|---|
| `atlas_rugido` | Rugido al despertar | huge mechanical dinosaur roar, metal and animal growl, deep | 1.5 s |
| `atlas_rascar` | Rasca el suelo (aviso) | heavy hoof scraping stone ground twice, bull about to charge | 0.8 s |
| `atlas_paso` | Paso pesado | heavy mechanical dinosaur footstep, metal thud on stone | 0.3 s |
| `atlas_choque` | Choca contra la estalactita | massive crash of metal beast into rock, debris | 1 s |
| `atlas_aturdido` | Aturdido | dizzy stunned cartoon sound, wobbling tone with little tweets | 0.9 s |
| `atlas_pisoton` | Pisotón | giant stomp shockwave on stone ground, deep boom | 0.6 s |
| `atlas_escudo_bloqueo` | La flecha rebota en el escudo | energy shield deflect ting, metallic ricochet | 0.35 s |
| `atlas_lanzar` | Lanza rocas | heavy grunt and rocks being flung, whoosh | 0.4 s |
| `atlas_anillo` | Dispara anillo de energía | sci-fi energy ring blast, rising zap | 0.4 s |
| `atlas_escudo_roto` | Se rompe el escudo | energy shield shattering like glass, crystalline | 0.9 s |
| `atlas_muerte` | Muere | giant robot dinosaur powering down, mechanical groan winding down, steam | 2.5 s |

## Aeris (pteranodón biomecánico, Nivel 2)

| Archivo | Qué es | Prompt | Duración |
|---|---|---|---|
| `aeris_chillido` | Chillido (despierta / cambia de fase) | robotic pterodactyl screech, metallic bird scream, echoing | 1.1 s |
| `aeris_aleteo` | Aleteo | large metal wings flapping, mechanical whoosh, three beats | 0.6 s |
| `aeris_apuntar` | Láser de aviso antes de la picada | targeting laser charging up, rising electronic tone | 0.6 s |
| `aeris_picada` | Picada en diagonal | fast diving whoosh, jet-like swoop downward | 0.45 s |
| `aeris_clavado` | Se clava en el piso o la pared | metal beak slamming into steel floor, heavy clank | 0.5 s |
| `aeris_huevo` | Suelta un huevo-mina | small mechanical pop, egg released | 0.15 s |
| `aeris_explosion` | Explota el huevo | small grenade explosion, punchy, short | 0.7 s |
| `aeris_plumas` | Dispara plumas-cuchilla | metal feathers shot like knives, sharp swish | 0.2 s |
| `aeris_viento` | Ráfaga de viento | strong wind gust from giant wings, sustained | 2.2 s |
| `aeris_rayo` | Rayo a ras de suelo | electric beam blast across the floor, zap and buzz | 0.6 s |
| `aeris_muerte` | Muere | robotic bird powering down, falling and crashing into metal | 2.2 s |

## Compuertas de la base

| Archivo | Qué es | Prompt | Duración |
|---|---|---|---|
| `compuerta_retumbo` | Alarma antes de cerrarse | short sci-fi alarm siren, two tones | 0.7 s |
| `compuerta_caida` | La compuerta baja | heavy metal shutter sliding down fast | 0.3 s |
| `compuerta_impacto` | Se cierra de golpe | massive steel door slamming shut, deep metallic boom | 1 s |
| `compuerta_grieta` | Se daña | metal bending and cracking | 0.3 s |
| `compuerta_romper` | Se rompe al vencer al jefe | steel door bursting apart, metal debris clattering | 1 s |

## Rocas, cueva y objetos

| Archivo | Qué es | Prompt | Duración |
|---|---|---|---|
| `roca_impacto` | Roca cae al suelo | rock hitting stone ground, crunchy thud | 0.2 s |
| `roca_romper` | Roca destruida | rock crumbling into pieces, short | 0.35 s |
| `estalactita_retumbo` | Aviso de caída | cave ceiling rumbling, deep low rumble | 0.8 s |
| `estalactita_caida` | Cae | large rock falling through air, whoosh downward | 0.4 s |
| `estalactita_impacto` | Se clava en el suelo | huge stalactite crashing into cave floor, heavy impact, debris | 1.2 s |
| `estalactita_grieta` | Se agrieta | rock cracking, sharp stone fracture | 0.3 s |
| `estalactita_romper` | Se rompe | big stone pillar collapsing into rubble | 1 s |
| `eco_carga` | Golpear roca cian | magical crystal chime, energy pickup sparkle | 0.6 s |
| `objeto_recoger` | Recoger fruta o chip | soft item pickup chime, two notes, 2D game | 0.4 s |
| `llave_recoger` | Recoger la llave / núcleo | important item obtained jingle, bright and short | 1 s |
| `checkpoint` | Encender farola | warm magical lantern lighting up, soft chime | 1.2 s |
| `gota` (+ variantes) | Gotas en la cueva | single water drop in a cave, echo | 0.5 s |

## Ambiente (en bucle, opcionales)

| Archivo | Qué es | Prompt |
|---|---|---|
| `ambiente_cueva` | Fondo dentro de la cueva (reemplaza las gotas sintetizadas) | dark cave ambience loop, distant water drips, low wind, seamless |
| `ambiente_bosque` | Fondo del bosque en el Nivel 1 | jungle forest ambience loop, birds and insects, gentle, seamless |

## Voces sin palabras

Cada línea de diálogo suena con la voz de quien habla. Graba o genera **varias frases cortas sin palabras** (0.5–1.2 s) y guárdalas como variantes:

| Archivos | Personaje | Prompt |
|---|---|---|
| `voz_orin_1` … `voz_orin_5` | Orin, anciano ingeniero | old man murmuring gibberish, short mumbled phrase, warm and tired, no real words |
| `voz_kael_1` … `voz_kael_5` | Kael, joven explorador | young adventurer short gibberish exclamation, curious, no real words |
| `voz_kiro_1` … `voz_kiro_5` | Kiro, robot pequeño | tiny cute robot chattering beeps, expressive, like R2-D2 |
| `voz_nemesis_1` … `voz_nemesis_5` | NÉMESIS, la IA | cold synthetic AI voice, distorted digital gibberish, deep and calm |

Mientras no existan, el juego genera sílabas sintetizadas con el tono de cada personaje.
