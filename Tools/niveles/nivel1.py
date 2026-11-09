from levelkit import Level
# Nivel 1 · Valle del Eco  (# tierra de jungla, = piedra de ruinas)
L = Level(600, 30)
f, gr, pl, put = L.fill, L.ground, L.plat, L.put
def ruin(x0, x1, top): f(x0, x1, 0, top, '=')

f(0, 1, 0, 30); f(599, 600, 0, 30)

# A · Inicio: aprende salto y doble salto
gr(0, 16, 3); put(3, 3, 'P'); put(9, 3, 'T')
gr(16, 22, 5)
gr(26, 32, 5)                       # hueco de 4
gr(32, 38, 9)                       # muro de 4: doble salto
gr(38, 48, 3)
pl(42, 45, 6); put(43, 7, 'o')

# B · Primer encuentro
gr(48, 96, 3)
put(56, 3, 'E'); put(60, 3, 'T'); put(66, 3, 'K')
put(72, 3, '^'); put(73, 3, '^')
put(80, 3, 'E')
ruin(84, 90, 6); put(87, 6, 'o')

# C · Laguna
gr(104, 110, 3)                     # foso de 8 (96–104): doble salto
pl(114, 118, 4)                     # rama sobre foso de 12 (110–122)
put(116, 8, 'W')
gr(122, 140, 3); put(124, 3, 'T')
put(128, 3, 'C')

# D · Gran árbol y dosel
gr(140, 216, 3); put(142, 3, 'T')
pl(146, 150, 6); pl(152, 156, 10); pl(146, 150, 14); pl(152, 156, 18)
put(151, 13, 'W')
pl(141, 144, 18); put(142, 19, 'o')
f(158, 164, 17, 19); put(161, 19, 'E')
f(170, 178, 17, 19); put(174, 23, 'W')
f(186, 194, 17, 19); put(190, 19, 'E')
pl(199, 203, 18)
f(208, 216, 17, 19)
pl(211, 214, 22); put(212, 23, 'o')
pl(196, 200, 6); pl(201, 205, 10); pl(196, 200, 14)   # para volver a subir si caes
f(216, 220, 0, 17)                  # muro: solo se pasa por arriba
gr(220, 226, 12); gr(226, 232, 7)

# E · Ruinas de piedra
ruin(232, 290, 3); put(234, 3, 'C')
f(240, 268, 8, 30, '=')             # techo bajo
for x in (246, 247, 248): put(x, 3, '^')
put(254, 3, 'K'); put(260, 3, 'B')
put(264, 3, '^'); put(265, 3, '^')
put(276, 3, 'E')
ruin(282, 286, 7); put(284, 7, 'o')
for a, b in ((292, 295), (299, 302), (306, 309), (313, 316)):   # puente que se derrumba
    f(a, b, 2, 3, '%')
put(304, 7, 'W')
gr(318, 326, 3)

# F · Géiseres sobre el abismo
ruin(330, 333, 3)
for x in (330, 331, 332): put(x, 2, 'J')
f(336, 344, 10, 12, '='); put(340, 12, 'E')
f(355, 362, 10, 12, '='); put(358, 12, 'K')   # hueco de 11: doble salto + dash
gr(362, 380, 3)

# G · Cueva de las raíces
put(382, 3, 'C')
gr(380, 440, 3)
f(384, 436, 11, 30)
L.back(380, 440, 3, 11)
put(390, 3, 'E'); put(396, 3, '^'); put(397, 3, '^')
ruin(400, 404, 6); put(402, 9, 'W')
put(412, 3, 'B')
pl(418, 422, 6); put(420, 7, 'o')
put(426, 3, 'K'); put(432, 3, 'E')

# H · Arena de la emboscada
gr(440, 490, 3)
f(440, 490, 13, 30)
L.back(440, 490, 3, 13)
for y in range(3, 13): put(443, y, 'G'); put(487, y, 'G')
put(452, 3, 'E'); put(470, 3, 'E'); put(462, 3, 'B')
put(458, 8, 'W'); put(476, 8, 'W')
pl(450, 454, 6); pl(468, 472, 6); pl(459, 463, 9); put(461, 10, 'o')

# I · Subida al mirador
gr(490, 500, 3)
put(498, 2, 'J')
f(503, 520, 0, 10); put(510, 10, 'E')
for a, b in ((522, 525), (529, 532), (536, 539)):
    f(a, b, 9, 10, '%')
f(545, 560, 0, 10)
put(550, 10, '^'); put(551, 10, '^'); put(556, 10, 'K')
f(560, 566, 0, 14); f(566, 572, 0, 18)
f(572, 599, 0, 18)
put(580, 18, 'B'); put(585, 18, 'T')
put(594, 18, 'X')

META = """@titulo NIVEL 1 · VALLE DEL ECO
@subtitulo El valle despertó. Sigue el eco hasta el mirador.
@completado NIVEL 1 COMPLETADO
@siguiente Nivel_02_Templo_Nemesis
@tema jungla
@checkpoint Orilla de la Laguna
@checkpoint Puerta de las Ruinas
@checkpoint Boca de la Cueva
@cartel 18 9 ESPACIO saltar · otra vez en el aire: DOBLE SALTO
@cartel 50 8 Ataca con la lanza · abajo en el aire: POGO
@cartel 98 8 Foso ancho: usa el doble salto
@cartel 148 22 Trepa por las ramas
@cartel 340 16 Doble salto + DASH
@cartel 446 11 ¡Emboscada!
@cartel 590 22 MIRADOR"""

if __name__ == '__main__':
    open('Nivel_01_Valle_del_Eco.txt', 'w', encoding='utf-8').write(META + '\n---\n' + L.text() + '\n')
