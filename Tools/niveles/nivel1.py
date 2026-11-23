from pathlib import Path

from levelkit import Level


L = Level(448, 32)


def stone(x0, x1, top):
    L.ground(x0, x1, top, '=')


# Refugio de Kiro: espacio seguro para el primer registro y una rama opcional.
L.ground(0, 24, 3)
L.ground(24, 37, 4)
L.ground(37, 63, 3)
L.fill(0, 1, 0, L.h)
L.plat(25, 32, 6)
L.put(8, 3, 'P')
L.put(14, 3, 'C')
L.put(19, 3, 'M')
L.put(28, 7, 'o')
L.put(47, 3, 'E')
L.put(58, 3, 'F')

# Cuenca de los Ecos: los pilares separan los saltos y hay un descanso al desembarcar.
L.ground(63, 74, 5)
stone(80, 88, 5)
L.plat(91, 95, 3)
L.ground(96, 140, 4)
L.put(85, 5, 'o')
L.put(99, 4, 'F')
L.put(102, 4, 'C')
L.plat(104, 109, 6)
L.plat(111, 117, 9)
L.fill(119, 126, 10, 12, '=')
L.plat(128, 135, 8)
L.put(123, 12, 'o')
L.put(119, 6, 'W')
L.put(130, 4, '^')
L.put(131, 4, '^')

# Árbol del Eco: el dosel evita la patrulla y recompensa contar los saltos.
L.ground(140, 163, 5)
L.ground(163, 178, 6)
L.ground(178, 207, 8)
L.put(153, 5, 'E')
L.plat(142, 148, 7)
L.plat(150, 156, 9)
L.fill(158, 165, 10, 12, '=')
L.plat(168, 173, 12)
L.plat(176, 183, 11)
L.put(162, 12, 'o')
L.put(173, 6, 'F')
L.put(183, 14, 'W')
L.put(188, 8, 'M')

# Paso del repetidor: puente rápido e inestable; la ruta inferior ofrece un géiser y una escalera.
stone(207, 212, 7)
stone(212, 231, 5)
stone(231, 249, 2)
stone(249, 256, 6)
L.ground(256, 263, 5)
L.ground(263, 280, 4)
L.put(215, 5, 'C')
L.put(219, 5, 'F')
L.put(227, 5, 'M')
for left, right in ((231, 235), (237, 241), (243, 247)):
    L.plat(left, right, 6, '%')
L.put(245, 7, 'o')
L.put(235, 2, '^')
L.put(236, 2, '^')
for x in range(241, 243):
    L.put(x, 1, 'J')
L.fill(245, 249, 2, 4, '=')

# Cueva de las Raíces: la boca empieza bajo las raíces y desemboca en una bóveda.
stone(280, 293, 4)
stone(293, 300, 5)
stone(300, 309, 7)
stone(309, 366, 5)
L.ground(366, 379, 5)
for left, right, floor in (
    (266, 269, 16), (269, 274, 14), (274, 281, 13),
    (281, 290, 12), (290, 297, 13), (297, 306, 15),
    (306, 316, 16), (316, 324, 14), (324, 363, 15),
    (363, 367, 16), (367, 371, 17),
):
    L.fill(left, right, floor, L.h, '=')
L.put(291, 4, 'E')
L.put(295, 5, '^')
L.plat(303, 308, 9)
L.put(306, 10, 'o')
L.put(315, 5, 'M')
L.put(324, 5, 'C')
L.put(327, 5, 'F')

# Estación de vigilancia: dos centinelas complementarios y cobertura a dos alturas.
for y in range(5, 15):
    L.put(332, y, 'G')
    L.put(361, y, 'G')
L.plat(337, 342, 7)
L.plat(352, 357, 8)
L.put(346, 5, 'B')
L.put(354, 8, 'W')

# Mirador: vuelve la luz, se ve el templo y la credencial queda junto a la salida.
L.ground(379, 387, 7)
L.ground(387, 394, 9)
L.plat(395, 399, 5)
L.ground(400, 410, 8)
L.ground(410, 419, 10)
L.ground(419, 426, 11)
L.ground(426, 448, 12)
L.fill(447, 448, 0, L.h)
L.plat(410, 417, 13)
L.put(384, 7, 'C')
L.put(414, 14, 'o')
L.put(422, 11, 'M')
L.put(434, 12, 'X')

META = """@titulo NIVEL 1 · VALLE DEL ECO
@subtitulo Sigue la señal de Kiro y recupera la llave del Templo de NÉMESIS.
@completado SEÑAL RECUPERADA
@siguiente Nivel_02_Templo_Nemesis
@tema jungla
@arboles no
@cueva_valle 270 278 366 376
@checkpoint Refugio de Kiro
@checkpoint Cuenca de los Ecos
@checkpoint Paso del Repetidor
@checkpoint Cueva de las Raíces
@checkpoint Senda del Mirador
@cartel 13 8 REFUGIO DE KIRO
@cartel 69 10 Cruza por los pilares
@cartel 144 10 El dosel conduce al repetidor
@cartel 225 9 Paso superior inestable
@cartel 265 9 CUEVA DE LAS RAÍCES
@cartel 325 9 Puesto de vigilancia\\nReabastece antes de entrar
@cartel 433 16 Acceso al templo
@chip REGISTRO 01|La Tierra llego a su limite. Encendimos a NEMESIS para que restaurara el equilibrio. Le dimos control total del valle.
@chip REGISTRO 02|Los centinelas ya no obedecen. NEMESIS los rediseno con forma de bestias antiguas. Dice que asi el valle se defiende solo.
@chip REPETIDOR DEL VALLE|Kiro reconoce la frecuencia. Antes del apagón, esta antena comunicaba los refugios con una estación bajo las raíces.
@chip ORDEN DE NÉMESIS|Los centinelas del valle vigilan el acceso a la estación. Toda credencial autorizada se ha retirado al mirador.
@chip COORDENADAS DEL TEMPLO|Kiro descifra la ruta a la base. La llave sigue conectada al terminal del mirador. Recógela antes de cruzar.
@reliquia BarrioEnRuinas 18 -1.5 34
@reliquia ArcoRuinado 13 2.8 9.7
@reliquia AntenaCaida 20 3 8.5
@reliquia RestosMecanicos 37 3 4.8
@reliquia RaizTecnologica 64 4.8 13
@reliquia CristalesEnergia 83 5 2.9
@reliquia RestosMecanicos 89 -1.4 6.6
@reliquia ColumnaAncestral 104 4 3.5
@reliquia NidoMecanico 124 12 4.6
@reliquia ArbolDelEco 159 5 24
@reliquia ReleAncestral 188 8 6
@reliquia TorreDeLasRaices 205 2.5 24
@reliquia ArcoRuinado 217 5 10
@reliquia TerminalAncestral 227 5 4.7
@reliquia ColumnaAncestral 232.6 2 2.2
@reliquia RestosMecanicos 238 2 4.4
@reliquia ColumnaAncestral 245.3 3.9 1.6
@reliquia RestosMecanicos 252 6 5.7
@reliquia RaizTecnologica 272 3.8 17
@reliquia HongosDeLuz 279 4 4.6
@reliquia FosilDinorobot 289 4 9.5
@reliquia CristalesEnergia 303 7 4.7
@reliquia HongosDeLuz 314 5 5.4
@reliquia ColumnaAncestral 321 4.8 2.6
@reliquia ReleAncestral 346 5 12
@reliquia ColumnaAncestral 334 5 3.8
@reliquia ColumnaAncestral 360 5 3.8
@reliquia HongosDeLuz 366 5 5.6
@reliquia ArcoRuinado 381 7 9
@reliquia ArbolDelEco 401 8 15
@reliquia BarrioEnRuinas 421 4 43
@reliquia TorreDeLasRaices 442 3 24
@reliquia TerminalAncestral 426 11.7 5.6
@reliquia ArcoRuinado 435 11.8 10.5"""


if __name__ == '__main__':
    maps = Path(__file__).resolve().parents[2] / 'Assets' / 'Niveles' / 'Mapas'
    (maps / 'Nivel_01_Valle_del_Eco.txt').write_text(
        f'{META}\n---\n{L.text()}\n', encoding='utf-8')
