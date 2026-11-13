from levelkit import Level
# Tutorial · Claro del Despertar: una mecánica por tramo, sin caídas que encierren al jugador.
L = Level(170, 30)
gr, pl, put = L.ground, L.plat, L.put
L.fill(0, 3, 0, 30); L.fill(167, 170, 0, 30)

gr(3, 23, 8); put(6, 8, 'P')            # moverse
gr(23, 35, 10); gr(35, 38, 8); gr(38, 48, 10)            # salto: escalón de 2 y hueco de 3
put(36, 13, 'o')
gr(48, 61, 14); L.fill(48, 52, 0, 14, '='); put(55, 14, 'C'); put(57, 14, 'F')       # doble salto: muro de 4
gr(61, 63, 12); gr(63, 65, 10); gr(65, 71, 8)            # dash: hueco de 10 con escalones de reintento
put(72, 15, 'o'); put(73, 15, 'o')         # premio al cruzar
gr(71, 85, 14)
gr(85, 87, 12); gr(87, 89, 10); gr(89, 140, 8)
pl(92, 97, 9); pl(98, 103, 11); put(100, 13, 'o')        # ramas
put(109, 8, 'E')                       # lanza
put(116, 8, 'C'); put(125, 9, 'W')                      # arco: volador quieto a la altura del disparo recto
for x in range(130, 134): put(x, 8, '^')                 # pogo (opcional)
put(130, 11, 'o'); put(133, 11, 'o')
put(138, 7, 'J')                                         # géiser: risco de 7
gr(140, 147, 15, '='); put(143, 15, 'C')
gr(147, 153, 8); pl(147, 153, 14, '%'); put(148, 7, 'J') # puente que se derrumba, con géiser de rescate
put(150, 17, 'o')
gr(153, 167, 15, '='); put(163, 15, 'X')

META = """@titulo TUTORIAL · CLARO DEL DESPERTAR
@subtitulo Kael despierta. Aprende a moverte antes de entrar al valle.
@completado TUTORIAL COMPLETADO
@siguiente Nivel_01_Valle_del_Eco
@tema jungla
@llave no
@arboles no
@checkpoint Claro del Despertar
@checkpoint Sendero del Cazador
@checkpoint Umbral del Valle"""

if __name__ == '__main__':
    open('Nivel_00_Tutorial.txt', 'w', encoding='utf-8').write(META + '\n---\n' + L.text() + '\n')
