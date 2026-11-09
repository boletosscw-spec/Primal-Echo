from levelkit import Level
# Nivel 2 · Templo de NÉMESIS (más vertical, ruinas de piedra)
L = Level(461, 40)
f, gr, pl, put = L.fill, L.ground, L.plat, L.put
def ruin(x0, x1, top): f(x0, x1, 0, top, '=')

f(0, 1, 0, 40); f(460, 461, 0, 40)

# A · Entrada alta
ruin(0, 30, 30); put(3, 30, 'P'); put(14, 30, 'E'); put(24, 30, 'K'); put(22, 31, 'o')

# B · Pozo de las espinas
f(56, 58, 8, 40, '=')
ruin(30, 58, 3)
L.back(30, 56, 3, 30)
for x in range(30, 50): put(x, 3, "^")
pl(30, 36, 26); pl(42, 48, 22); pl(32, 38, 18); put(33, 19, 'o')
pl(44, 50, 14); pl(33, 39, 10); pl(46, 52, 6)
put(40, 20, 'W')

# C · Galería del templo
f(58, 150, 12, 40, '=')
L.back(58, 150, 3, 12)
ruin(58, 70, 3); put(64, 3, 'E')
for a, b in ((71, 74), (76, 79), (81, 84)):
    f(a, b, 2, 3, '%')
put(77, 7, 'W')
ruin(84, 110, 3)
put(90, 3, 'K')
for x in (95, 96, 97): put(x, 3, '^')
put(104, 3, 'B')
ruin(120, 150, 3)                   # foso de 10 (110–120)
put(126, 3, 'E')
ruin(132, 138, 7); put(135, 7, 'o')
put(144, 3, 'E'); put(139, 3, 'B'); put(96, 8, 'W')
put(148, 3, 'C')

# D · Ascenso por géiseres
ruin(150, 200, 3)
put(154, 2, 'J')
f(158, 164, 10, 12, '='); put(162, 11, 'J')
put(165, 19, 'W')
f(167, 173, 19, 21, '=')
pl(176, 180, 23)

# E · Cornisa alta
f(183, 214, 24, 26, '='); put(186, 26, 'C'); put(195, 26, 'B'); put(206, 26, 'E')
f(222, 240, 24, 26, '='); put(230, 26, 'E')          # hueco de 8
for a, b in ((242, 245), (249, 252), (256, 259)):
    f(a, b, 25, 26, '%')
put(250, 29, 'W')
f(262, 282, 24, 26, '=')
put(268, 26, '^'); put(269, 26, '^'); put(276, 26, 'K')

# F · Bajada a la cámara
ruin(282, 288, 23); ruin(288, 294, 18); ruin(294, 300, 13); ruin(300, 306, 8)
put(291, 18, 'E')
pl(283, 286, 26); put(284, 27, 'o')
ruin(306, 370, 3); put(364, 3, 'E')
put(308, 3, 'C')

# G · Cámara de la emboscada
f(310, 360, 14, 40, '=')
L.back(310, 360, 3, 14)
for y in range(3, 14): put(313, y, 'G'); put(357, y, 'G')
put(320, 3, 'E'); put(335, 3, 'E'); put(325, 3, 'B'); put(345, 3, 'B')
put(327, 10, 'W'); put(340, 9, 'W')
pl(322, 326, 6); pl(338, 342, 6); pl(330, 334, 9); put(332, 10, "o")

# H · Abismo de las avispas y cumbre
pl(374, 377, 5); pl(382, 385, 7); pl(390, 393, 5)
put(379, 10, 'W'); put(388, 11, 'W')
ruin(400, 410, 6); put(405, 6, 'K')
f(410, 430, 0, 10, '='); put(420, 10, 'E')
put(428, 9, 'J')
f(433, 460, 0, 18, '=')
put(442, 18, 'B'); put(449, 18, 'E'); put(455, 18, 'X')

META = """@titulo NIVEL 2 · TEMPLO DE NÉMESIS
@subtitulo Las máquinas de NÉMESIS custodian el templo.
@completado NIVEL 2 COMPLETADO
@tema templo
@checkpoint Galería del Templo
@checkpoint Cornisa del Eco
@checkpoint Umbral de la Cámara
@cartel 12 35 Baja por las cornisas: el fondo está lleno de espinas
@cartel 156 9 Géiser: déjate llevar y luego salta
@cartel 218 30 Doble salto
@cartel 318 12 ¡Emboscada!
@cartel 452 22 SALIDA"""

if __name__ == '__main__':
    open('Nivel_02_Templo_Nemesis.txt', 'w', encoding='utf-8').write(META + '\n---\n' + L.text() + '\n')
