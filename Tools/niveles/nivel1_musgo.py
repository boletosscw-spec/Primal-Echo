# Nivel 1 · Valle del Eco (Bosque del Musgo). Genera Assets/Niveles/Mapas/Nivel_01_Musgo.json
# Unidades del mundo: 1 = 32 px del juego. Kael mide ~2.5 de alto.
# Salto ~2.7 de alto / ~6 de largo · doble salto ~4.9 de alto / ~9 de largo.
import json, random, sys, os

W, H = 524, 40
rng = random.Random(20261120)
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..', 'Assets', 'Niveles', 'Mapas', 'Nivel_01_Musgo.json')
ART = json.load(open(os.path.join(os.path.dirname(os.path.abspath(__file__)), 'arte_dict.json')))
EST, ANI = ART['estaticos'], ART['animaciones']

D = dict(titulo='NIVEL 1 · VALLE DEL ECO',
         subtitulo='Sigue la señal que capta Kiro y consigue la llave de la base de NÉMESIS.',
         completado='NIVEL 1 COMPLETADO', siguiente='Nivel_02_Templo_Nemesis',
         ancho=W, alto=H, bloques=[], techos=[], plataformas=[], sprites=[], anims=[], luces=[],
         peligros=[], rebotes=[], slimes=[], centinelas=[], npcs=[], narraciones=[], checkpoints=[], frutas=[],
         chips=[], fragmentos=[], cuevas=[], fondoCueva=[])
CYAN = (0.35, 0.9, 1.0)

# ───────── helpers ─────────
def G(x0, x1, top, estilo='musgo'):
    D['bloques'].append(dict(x0=x0, x1=x1, y0=-30, y1=top, estilo=estilo))

def TECHO(x0, x1, bottom, estilo='cueva'):
    D['techos'].append(dict(x0=x0, x1=x1, y0=bottom, y1=H + 12, estilo=estilo))

def P(x, y, s, esc=1.0, uno=True, flip=False, raices=True):
    D['plataformas'].append(dict(x=x, y=y, s=s, esc=esc, unSentido=uno, flip=flip))
    w = EST[s]['w'] * esc
    if raices and not s.startswith('cueva'): raices_de(x, y, s, esc)
    return (x - w / 2 * 0.85, x + w / 2 * 0.85, y)

def raices_de(x, y, s, esc):
    """Musgo y hojas que cuelgan de la parte de abajo de una plataforma: siempre bajo el centro
    (las orillas son redondas) y metidos detrás de la plataforma, para que nunca queden flotando."""
    info = EST[s]
    w, h = info['w'] * esc, info['h'] * esc
    abajo = y + info['surf'] * esc - h            # borde inferior del dibujo
    n = 1 if w < 3 else (2 if w < 5 else 3)
    xs = [x] if n == 1 else [x - w * 0.18, x + w * 0.18] if n == 2 else [x - w * 0.24, x, x + w * 0.24]
    for cx in xs:
        c = rng.choice(['colg_hoja_3', 'colg_hoja_4', 'colg_hoja_1', 'colg_musgo_0', 'colg_musgo_1'])
        e = min(rng.uniform(0.45, 0.7), (w * 0.5 / n) / EST[c]['w'] * 1.6)
        S(c, cx + rng.uniform(-0.15, 0.15), abajo + h * 0.4, e, orden=0, ancla='arriba', flip=rng.random() < .5)

def S(s, x, y, esc=1.0, orden=3, par=0.0, flip=False, flipY=False, rot=0.0, col=(1, 1, 1), a=1.0, ancla='abajo'):
    D['sprites'].append(dict(s=s, x=round(x, 3), y=round(y, 3), esc=round(esc, 3), orden=orden, par=par,
                             flipX=flip, flipY=flipY, rot=rot, r=col[0], g=col[1], b=col[2], a=a, ancla=ancla))

def A(s, x, y, esc=1.0, orden=4, fps=0.0, flip=False, col=(1, 1, 1), a=1.0, par=0.0):
    D['anims'].append(dict(s=s, x=round(x, 3), y=round(y, 3), esc=round(esc, 3), orden=orden,
                           fps=fps or ANI[s]['fps'], flipX=flip, r=col[0], g=col[1], b=col[2], a=a, par=par))

def LUZ(x, y, col=CYAN, i=0.8, rad=3.0, pulso=True):
    D['luces'].append(dict(x=x, y=y, r=col[0], g=col[1], b=col[2], i=i, rad=rad, pulso=pulso))

def ESPINAS(x, y, ancho, regresa=True):
    """Fila de lianas espinosas sobre el suelo de un foso (y = suelo)."""
    xs = x
    while xs < x + ancho - 1:
        s = rng.choice(['espina_3', 'espina_4', 'espina_5'])
        w = EST[s]['w'] * 0.8
        S(s, xs + w / 2, y - 0.35, 0.8, orden=6, flip=rng.random() < 0.5)
        xs += w * 0.8
    D['peligros'].append(dict(x=x + ancho / 2, y=y + 0.55, w=ancho - 0.4, h=1.1, regresa=regresa))

def PELIGRO_SPRITE(s, x, y, esc, w, h, anim=False, flip=False, regresa=False):
    if anim: A(s, x, y, esc, orden=6, flip=flip)
    else: S(s, x, y, esc, orden=6, flip=flip)
    D['peligros'].append(dict(x=x, y=y + h / 2, w=w, h=h, regresa=regresa))

def REBOTE(x, y, esc=1.0, fuerza=21.0):
    D['rebotes'].append(dict(x=x, y=y, esc=esc, fuerza=fuerza))
    LUZ(x, y + 0.9, CYAN, 0.9, 2.6)

def CENTINELA(tipo, x, y):
    """Centinela biomecánico de NÉMESIS (dinosaurio): 'terrestre' en suelo firme o 'volador'."""
    D['centinelas'].append(dict(tipo=tipo, x=x, y=y, min=0, max=0))

def NPC(x, y, lineas, desaparece, flip=True, kiroX=0.0):
    """Orin. Líneas que empiezan con 'Kael:' salen sobre Kael; '#REPARAR' = Kael repara a Kiro (en kiroX)."""
    D['npcs'].append(dict(x=x, y=y, esc=1.63, flipX=flip, desaparece=desaparece, lineas=lineas, radio=6.5, kiroX=kiroX))

def NARRA(x, y, texto, ancho=3.0, alto=8.0, dur=4.5):
    D['narraciones'].append(dict(x=x, y=y, ancho=ancho, alto=alto, texto=texto, dur=dur))

def CP(x, y, nombre):
    D['checkpoints'].append(dict(x=x, y=y, nombre=nombre))

def FRUTA(x, y): D['frutas'].append(dict(x=x, y=y))
def CHIP(x, y, t, txt): D['chips'].append(dict(x=x, y=y, titulo=t, texto=txt))
def ORBE(x, y): D['fragmentos'].append(dict(x=x, y=y))

# ───────── decoración automática (no repetitiva: semilla + mezcla por zona) ─────────
PASTOS = ['pasto_0', 'pasto_1', 'pasto_2', 'pasto_3', 'pasto_5']
PLANTAS = ['planta_1', 'planta_2', 'planta_3', 'planta_4', 'planta_5', 'planta_6', 'planta_7', 'planta_viento']
HIERBAS = ['hierba_0', 'hierba_1', 'hierba_2', 'hierba_3', 'hierba_4', 'hierba_5', 'mata_0', 'mata_1', 'tallo_0', 'tallo_1', 'tallo_2', 'tallo_3']
ROCAS = ['roca_0', 'roca_1', 'roca_2']
CUEVA_PIEDRAS = ['cueva_piedra_%d' % i for i in range(6)] + ['cueva_pena_%d' % i for i in range(5)]
libres = []   # tramos donde no debe ir decoración voluminosa (npc, checkpoints, peligros)

def reservado(x):
    return any(a <= x <= b for a, b in libres)

def ancho(s, esc):
    return EST[s if s in EST else s + '_f0']['w'] * esc

_ult = []
def elegir(lista):
    """Elige al azar sin repetir ninguna de las dos últimas piezas."""
    op = [o for o in lista if o not in _ult[-2:]] or lista
    c = rng.choice(op); _ult.append(c)
    return c

def cabe(s, x, esc, x0, x1, margen=0.15):
    w = ancho(s, esc) * 0.42                     # los dibujos tienen aire a los lados
    return x - w >= x0 + margen and x + w <= x1 - margen

def vestir_suelo(x0, x1, y, zona):
    x = x0 + rng.uniform(0.4, 1.2)
    while x < x1 - 0.4:
        r = rng.random()
        if zona == 'cueva':
            if r < 0.4:
                c, e = elegir(CUEVA_PIEDRAS), rng.uniform(0.3, 0.6)
                if cabe(c, x, e, x0, x1): S(c, x, y - 0.15, e, orden=rng.choice([2, 3, 21]), flip=rng.random() < .5, col=(0.75, 0.7, 0.72))
            elif r < 0.6:
                c, e = elegir(['planta_4', 'planta_6', 'planta_5']), rng.uniform(0.6, 0.9)
                if cabe(c, x, e, x0, x1): A(c, x, y - 0.1, e, orden=3, col=(0.55, 0.8, 0.75))
            elif r < 0.68:
                c, e = 'cueva_estalagmita_%d' % rng.randrange(4), rng.uniform(0.4, 0.7)
                if cabe(c, x, e, x0, x1): S(c, x, y - 0.25, e, orden=2, col=(0.7, 0.65, 0.68))
            x += rng.uniform(1.2, 2.6)
            continue
        if r < 0.42:
            c, e = elegir(PASTOS), rng.uniform(0.8, 1.2)
            if cabe(c, x, e, x0, x1): A(c, x, y - 0.12, e, orden=rng.choice([3, 3, 4, 21]), flip=rng.random() < .5)
        elif r < 0.62:
            c, e = elegir(PLANTAS), rng.uniform(0.75, 1.1)
            if cabe(c, x, e, x0, x1): A(c, x, y - 0.12, e, orden=rng.choice([3, 4]), flip=rng.random() < .5)
        elif r < 0.82:
            c, e = elegir(HIERBAS), rng.uniform(0.7, 1.05)
            if cabe(c, x, e, x0, x1): S(c, x, y - 0.1, e, orden=rng.choice([3, 4, 21]), flip=rng.random() < .5)
        elif r < 0.9 and not reservado(x):
            c, e = elegir(ROCAS), rng.uniform(0.36, 0.55)
            if cabe(c, x, e, x0, x1, 0.6):
                S(c, x, y - 0.3, e, orden=2, flip=rng.random() < .5, col=(0.82, 0.9, 0.85))
                x += 2.0
        if zona == 'santuario' and rng.random() < 0.35:
            c, e = rng.choice(['flor_azul_0', 'flor_azul_1']), rng.uniform(0.55, 0.8)
            if cabe(c, x + 0.6, e, x0, x1):
                A(c, x + 0.6, y - 0.1, e, orden=3, flip=rng.random() < .5)
                LUZ(x + 0.4, y + 2.0, CYAN, 0.55, 2.4)
        x += rng.uniform(0.9, 2.2)
    borde_musgo(x0, x1, y, zona)

def borde_musgo(x0, x1, y, zona):
    """Fila tupida de brotes y hierba baja sobre la orilla del suelo: tapa la costura del
    bloque de musgo para que no se note que la textura se repite."""
    if zona == 'cueva':
        x = x0 + rng.uniform(0.2, 0.8)
        while x < x1 - 0.3:
            c, e = elegir(['cueva_piedra_0', 'cueva_piedra_1', 'cueva_piedra_3', 'cueva_piedras_a', 'cueva_piedras_b']), rng.uniform(0.2, 0.34)
            if cabe(c, x, e, x0, x1, 0.05): S(c, x, y - 0.2, e, orden=2, flip=rng.random() < .5, col=(0.6, 0.56, 0.6))
            x += rng.uniform(0.9, 1.9)
        return
    x = x0 + rng.uniform(0.15, 0.5)
    while x < x1 - 0.2:
        c = elegir(['brote_0', 'brote_1', 'brote_2', 'hierba_0', 'hierba_4', 'hierba_2', 'hierba_5'])
        e = rng.uniform(0.45, 0.75)
        if cabe(c, x, e, x0, x1, 0.05):
            S(c, x, y - 0.14, e, orden=rng.choice([2, 2, 3]), flip=rng.random() < .5,
              col=rng.choice([(1, 1, 1), (0.85, 0.95, 0.9), (0.75, 0.88, 0.82)]))
        x += rng.uniform(0.45, 0.95)

def colgantes(x0, x1, y, zona, densidad=0.55):
    x = x0 + rng.uniform(0.3, 1.0)
    while x < x1 - 0.3:
        if rng.random() < densidad:
            if zona == 'cueva':
                s = rng.choice(['cueva_estalactita_0', 'cueva_estalactita_1', 'cueva_estalactita_2', 'cueva_estalactita_3', 'cueva_estalactitas_4'])
                S(s, x, y + 0.25, rng.uniform(0.35, 0.75), orden=2, ancla='arriba', col=(0.7, 0.64, 0.66))
            else:
                s = rng.choice(['colg_hoja_0', 'colg_hoja_1', 'colg_hoja_2', 'colg_hoja_3', 'colg_hoja_4', 'colg_musgo_0', 'colg_musgo_1'])
                S(s, x, y + 0.3, rng.uniform(0.45, 0.8), orden=rng.choice([1, 2]), ancla='arriba', flip=rng.random() < .5)
        x += rng.uniform(0.9, 2.0)

def fondo_lejano(x0, x1, zona):
    """Siluetas lejanas. Siempre con la base muy por debajo del suelo para que, con el
    parallax, nunca se vea dónde terminan."""
    x = x0
    while x < x1:
        if zona == 'cueva':
            # consume el aleatorio igual que la versión anterior para no mover la decoración del resto del nivel
            while x < x1:
                rng.randrange(12, 16); rng.uniform(-5, -2.5); rng.uniform(1.3, 1.8); rng.random(); x += rng.uniform(5, 8)
            fondo_cueva(x0, x1)
            return
        break                                   # en el bosque ya hay profundidad de sobra con el fondo parallax

def fondo_cueva(x0, x1):
    """Fondo de la cueva en 4 capas de profundidad (de la más lejana a la más cercana).
    Colores muy oscuros: la oscuridad de la cueva tapa casi todo y la linterna de Kiro lo va
    revelando. Usa su propio generador aleatorio para no mover la decoración del resto del nivel."""
    r = random.Random(4417)
    a0, a1 = x0 + 4, x1 - 4                       # margen: con parallax nada se asoma fuera de la cueva
    # Capa 1 · lo más lejano: peñascos enormes abajo y estalactitas gigantes arriba
    x = a0
    while x < a1:
        S('cueva_pena_%d' % r.randrange(4), x, r.uniform(-3.5, -1.5), r.uniform(2.4, 3.4), orden=-49, par=0.32,
          col=(0.055, 0.05, 0.07), flip=r.random() < .5)
        x += r.uniform(4.5, 7.5)
    x = a0 + r.uniform(0, 3)
    while x < a1:
        S('cueva_estalactita_%d' % r.randrange(4), x, r.uniform(21, 24), r.uniform(2.0, 3.2), orden=-49, par=0.32,
          col=(0.05, 0.045, 0.065), ancla='arriba', flip=r.random() < .5)
        x += r.uniform(3.5, 6.5)
    # Capa 2 · pilares de roca y paredes
    x = a0 + r.uniform(2, 6)
    while x < a1:
        S('plat_columna', x, r.uniform(-4, -2), r.uniform(2.2, 2.8), orden=-46, par=0.2,
          col=(0.09, 0.08, 0.11), flip=r.random() < .5)
        x += r.uniform(11, 17)
    x = a0
    while x < a1:
        S('cueva_rocas_%d' % r.randrange(8, 12), x, r.uniform(-4.5, -2.5), r.uniform(1.4, 1.9), orden=-45, par=0.2,
          col=(0.12, 0.1, 0.13), flip=r.random() < .5)
        S('cueva_estalactitas_4', x + r.uniform(-1, 1), r.uniform(19, 22), r.uniform(1.6, 2.4), orden=-45, par=0.2,
          col=(0.1, 0.09, 0.12), ancla='arriba', flip=r.random() < .5)
        x += r.uniform(5, 8)
    # Capa 3 · rocas grandes, estalagmitas y cristales apagados (Kiro los hace brillar al pasar)
    x = a0 + r.uniform(0, 4)
    while x < a1:
        S('cueva_roca_grande_%s' % r.choice('05'), x, r.uniform(-1.5, 0.5), r.uniform(0.75, 1.15), orden=-42, par=0.1,
          col=(0.17, 0.15, 0.19), flip=r.random() < .5)
        S('cueva_estalagmita_%d' % r.randrange(4), x + r.uniform(2.5, 4.5), r.uniform(-0.5, 1.5), r.uniform(0.9, 1.4),
          orden=-41, par=0.1, col=(0.2, 0.18, 0.22))
        if r.random() < 0.45:
            S('cristal_llave', x + r.uniform(-2, 2), r.uniform(1.5, 4), r.uniform(0.35, 0.6), orden=-41, par=0.1,
              col=(0.25, 0.6, 0.65), a=0.7, rot=r.uniform(-25, 25))
        x += r.uniform(7, 11)
    # Capa 4 · bruma fría a ras de suelo
    x = a0
    while x < a1:
        S('bruma', x, r.uniform(2, 5), r.uniform(1.4, 2.0), orden=-40, par=0.05, col=(0.45, 0.6, 0.75), a=0.18, ancla='centro')
        x += r.uniform(10, 16)

def primer_plano(x0, x1, zona):
    x = x0 + rng.uniform(0, 6)
    while x < x1:
        if zona == 'cueva':
            S('cueva_estalagmita_%d' % rng.randrange(4), x, rng.uniform(-3, -1), rng.uniform(1.2, 1.8), orden=24, par=-0.2, col=(0.05, 0.04, 0.045))
        else:
            s = rng.choice(['hoja_curva_0', 'hoja_curva_1', 'hoja_curva_2'])     # helechos en primer plano, solo las puntas asoman abajo
            S(s, x, rng.uniform(-3.0, -2.2), rng.uniform(1.2, 1.6), orden=24, par=-0.15, col=(0.03, 0.06, 0.05), flip=rng.random() < .5)
        x += rng.uniform(14, 24)

def rayos(xs):
    for x in xs:
        S('rayo_luz', x, 30, rng.uniform(1.2, 1.7), orden=-8, rot=rng.uniform(-22, -14), a=rng.uniform(0.35, 0.55), ancla='arriba')

def bruma(x, y, esc=1.0):
    S('bruma', x, y, esc, orden=8, a=0.45, ancla='centro')

# ═════════ ZONA 1 · CLARO DEL DESPERTAR (0–72) ═════════
G(-12, 2, 32)
G(2, 30, 5)
G(30, 44, 6)
G(44, 72, 5)
libres += [(10, 16), (45, 56)]
CP(13, 5, 'Claro del Despertar')
A('flor_azul_0', 11.2, 4.9, 0.9, orden=3); LUZ(11.5, 7.2, CYAN, 0.9, 3.5)
S('roca_1', 53.5, 4.75, 0.55, orden=2, flip=True)
NPC(51.5, 5, [
    'Alto. ¿Viste una onda cian desde el risco?',
    'Soy Orin. Reparaba los sensores de NÉMESIS. Este pequeño todavía reconoce su señal.',
    'Lo saqué de entre los escombros. Tiene un contacto suelto y me tiemblan las manos.',
    'Kael: Vigila tu escáner. Yo ajusto el contacto.',
    '#REPARAR',
    'Ya se mueve. Espera... está buscando de dónde vino la onda.',
    'Kael: Kiro. ¿Te parece? Vamos a seguir esa señal.',
    'Marcó el este. Sigue el valle. Te espero en la entrada de la cueva.'], desaparece=True, kiroX=49.5)
CHIP(37, 6, 'REGISTRO 01 · DIARIO DE KAEL',
     'Desde el risco vi una luz en el valle. Si viene de NEMESIS, tengo que saber qué es.')
NARRA(62, 5, 'KIRO emite un pitido: capta una señal de NEMESIS hacia el este.')
a, b, y = P(23.5, 9.2, 'plat_bola', 0.9)
ORBE(23.5, 10.4)
NARRA(8.5, 5, 'Kael: La luz venía de allá abajo... tiene que estar al este.')
NARRA(29, 5, 'Kael: Huellas frescas... alguien más anda por aquí.')
vestir_suelo(2, 30, 5, 'musgo'); vestir_suelo(30, 44, 6, 'musgo'); vestir_suelo(44, 72, 5, 'musgo')
S('roca_0', 2.5, 4.7, 0.6, orden=2, col=(0.8, 0.9, 0.85))
rayos([18, 46, 63])

# ═════════ ZONA 2 · RAÍCES COLGANTES (72–150) ═════════
G(72, 96, 1)
ESPINAS(72.3, 1, 23.4)
bruma(84, 2.2, 1.6)
P(77, 6.5, 'plat_cuadro', 1.0)
P(83, 7.5, 'plat_larga_a', 0.55)
P(91, 6.5, 'plat_bola', 1.0)
NARRA(70, 5, 'Las espinas brillantes te lastiman. Cruza saltando de raíz en raíz.', dur=4)
G(96, 120, 5)
# (sin enemigos en el Nivel 1: solo ATLAS) CENTINELA('terrestre', 107, 5)
REBOTE(115.5, 5)
NARRA(112.5, 5, 'Pisa los hongos azules para salir disparado hacia arriba.', dur=4)
P(119, 12.5, 'plat_larga_b', 0.55)
ORBE(119, 13.8)
FRUTA(121, 13.4)
G(120, 128, 7)
G(128, 150, 6)
vestir_suelo(96, 113, 5, 'musgo'); vestir_suelo(120, 128, 7, 'musgo'); vestir_suelo(128, 150, 6, 'musgo')
rayos([104, 134])

# ═════════ ZONA 3 · EL GRAN TRONCO (150–236) ═════════
libres += [(143, 149)]
CP(146, 6, 'Raíces Colgantes')
A('flor_azul_1', 144.3, 5.9, 0.85, orden=3); LUZ(144.5, 8.0, CYAN, 0.9, 3.5)
G(150, 158, 8)
G(158, 166, 10)
G(166, 188, 4)                      # hondonada: si caes, el hongo te regresa
REBOTE(175.5, 4)
P(170, 13, 'plat_columna', 1.68, raices=False)        # pilar que sale del fondo de la hondonada
P(176, 15.5, 'plat_cuadro', 1.0)
P(182.5, 17, 'plat_larga_a', 0.55)
NARRA(160, 10, 'Kael: Hay que subir. Desde lo alto del tronco se verá el camino.')
G(188, 206, 18)
CHIP(194, 18, 'REGISTRO 02 · ORIN',
     'Las máquinas de NEMESIS talaron este bosque para construir su BASE. El musgo volvió a crecer sobre el acero.')
P(199.5, 22.2, 'plat_bola', 0.9)
ORBE(199.5, 23.4)
NARRA(192, 18, 'Kael: Desde aquí se ven las antenas de la base... NEMESIS está allá.')
# (sin enemigos en el Nivel 1: solo ATLAS) CENTINELA('volador', 203, 22.5)
G(206, 214, 14)
G(214, 222, 10)
G(222, 236, 7)
FRUTA(230, 7)
vestir_suelo(150, 158, 8, 'musgo'); vestir_suelo(158, 166, 10, 'musgo'); vestir_suelo(166, 173, 4, 'musgo')
vestir_suelo(178, 188, 4, 'musgo'); vestir_suelo(188, 206, 18, 'musgo'); vestir_suelo(206, 214, 14, 'musgo')
vestir_suelo(214, 222, 10, 'musgo'); vestir_suelo(222, 236, 7, 'musgo')
rayos([172, 196, 219])

# ═════════ ZONA 4 · PANTANO DE ESPINAS (236–312) ═════════
libres += [(229, 235)]
CP(232, 7, 'El Gran Tronco')
A('flor_azul_0', 233.8, 6.9, 0.85, orden=3, flip=True); LUZ(234, 9, CYAN, 0.9, 3.5)
G(236, 262, 1)
ESPINAS(236.3, 1, 25.4)
bruma(249, 2.2, 1.7)
P(240.5, 7.5, 'plat_bola', 1.0)
P(246, 8.5, 'plat_larga_a', 0.5)
P(252.5, 8.0, 'plat_bolita', 1.0)
P(258, 7.5, 'plat_cuadro', 1.0)
NARRA(238, 7, 'Kael: Las espinas brillan... los desechos de la base envenenaron las raíces.')
G(262, 276, 6)
libres += [(266, 271)]
PELIGRO_SPRITE('planta_veneno', 268.5, 6 - 0.1, 0.9, 1.4, 1.2, anim=True)
LUZ(268.5, 7, (0.5, 1.0, 0.4), 0.6, 2.5)
G(276, 300, 5)
libres += [(280, 285)]
PELIGRO_SPRITE('espina_5', 282.5, 5 - 0.3, 0.7, 2.0, 0.9)
# (sin enemigos en el Nivel 1: solo ATLAS) CENTINELA('terrestre', 293, 5)
G(300, 312, 6)
vestir_suelo(262, 266, 6, 'musgo'); vestir_suelo(271, 276, 6, 'musgo'); vestir_suelo(276, 280, 5, 'musgo')
vestir_suelo(285, 300, 5, 'musgo')
rayos([270, 292])

# ═════════ ZONA 5 · CUEVA SUSURRANTE (312–420) ═════════
libres += [(300, 311)]
CP(302, 6, 'Boca de la Cueva')
A('flor_azul_1', 300.6, 5.9, 0.85, orden=3); LUZ(300.8, 8, CYAN, 0.9, 3.5)
FRUTA(309, 6)
NPC(306.5, 6, [
    'Llegaste. Kiro te trajo directo hasta aquí.',
    'Mi refugio queda a la salida de esta cueva; ahí guardo la llave.',
    'Pero mira esa grieta: hay que arrastrarse para pasar. Con estas rodillas yo ya no quepo.',
    'Kael: ¿Y tú cómo vas a llegar?',
    'Por el risco. Es más largo, pero conozco el camino. Te espero del otro lado.',
    'Una cosa... desde que Kiro despertó, algo respira allá adentro. Kiro, alumbra bien.'], desaparece=True)
NARRA(309.6, 6, 'Mantén ABAJO para AGACHARTE y pasar por la grieta.', ancho=2.5, dur=5)
D['cuevas'].append(dict(x0=316, x1=416, y0=-5, y1=30))
D['fondoCueva'].append(dict(x0=312, x1=420, y0=-6, y1=24))
# la boca de la cueva es una GRIETA BAJA: solo se pasa agachado (Orin no cabe y rodea por el risco)
G(312, 319, 6, 'cueva'); TECHO(310.5, 319, 7.75); G(319, 330, 5, 'cueva'); TECHO(319, 330, 13)
S('colg_musgo_0', 311.0, 7.95, 0.5, orden=3, ancla='arriba'); S('colg_hoja_3', 311.7, 7.95, 0.45, orden=3, ancla='arriba', flip=True)
S('cueva_piedra_3', 310.2, 5.85, 0.3, orden=3, col=(0.6, 0.56, 0.6))
G(330, 372, 3, 'cueva'); TECHO(330, 372, 11.5)   # arena de ATLAS (x 337-365): suelo plano, sin foso
# (sin enemigos en el Nivel 1: solo ATLAS) CENTINELA('terrestre', 341, 3)
G(372, 380, 5, 'cueva'); TECHO(372, 396, 15)
G(380, 396, 3, 'cueva')
REBOTE(385, 3)
P(388.5, 10.5, 'cueva_plat_a', 0.8)
ORBE(388.5, 11.8)
G(396, 404, 6, 'cueva'); TECHO(396, 420, 18)
G(404, 412, 9, 'cueva')
G(412, 420, 11, 'cueva')
CHIP(376, 5, 'REGISTRO 03 · VOZ EN LA CUEVA',
     'Una voz metálica repite: "Para salvar la Tierra, la humanidad debe desaparecer." Es NEMESIS.')
NARRA(333, 3, 'Kael: Esta oscuridad... escucho una voz en las paredes.')
NARRA(374, 5, 'NEMESIS (eco): "Regresa, humano. Aquí no queda nada para ti."', dur=5)
for x in (334, 347, 362, 378, 391, 408):
    A(rng.choice(['flor_azul_0', 'flor_azul_1']), x, 3 if x < 372 else (5 if x < 380 else (3 if x < 396 else 9)), 0.6, orden=3)
for x, y in ((334, 3), (347, 3), (362, 3), (378, 5), (391, 3), (408, 9)):
    LUZ(x, y + 1.6, CYAN, 0.9, 3.2)
S('cueva_roca_grande_0', 343, 2.8, 0.5, orden=2, col=(0.7, 0.65, 0.68))
S('cueva_roca_grande_5', 399, 5.8, 0.55, orden=2, col=(0.7, 0.65, 0.68))
vestir_suelo(312, 330, 5, 'cueva'); vestir_suelo(330, 352, 3, 'cueva'); vestir_suelo(357, 372, 3, 'cueva')
_rng = rng; rng = random.Random(352); vestir_suelo(352, 357, 3, 'cueva'); rng = _rng   # el foso rellenado (aleatorio aparte para no mover lo demás)
vestir_suelo(372, 380, 5, 'cueva'); vestir_suelo(396, 404, 6, 'cueva'); vestir_suelo(404, 420, 9, 'cueva')
colgantes(314, 330, 13, 'cueva', 0.7); colgantes(330, 372, 11.5, 'cueva', 0.7); colgantes(372, 396, 15, 'cueva', 0.7); colgantes(396, 420, 18, 'cueva', 0.6)

# ═════════ ZONA 6 · REFUGIO DE ORIN (420–524) ═════════
G(420, 436, 11)
G(436, 450, 8)
libres += [(438, 443), (476, 492), (504, 516)]
CP(440, 8, 'Refugio de Orin')
FRUTA(447, 8)
NARRA(424, 11, 'KIRO emite un pitido: la señal de la base es más fuerte aquí.')
G(450, 470, 7)
REBOTE(466, 7)
P(471, 15, 'plat_larga_c', 0.45)
ORBE(471, 16.3)
G(470, 500, 7)
S('cueva_pena_3', 487, 6.8, 0.55, orden=2, col=(0.65, 0.75, 0.7))           # pedestal de la llave
D['llave'] = dict(x=487, y=9.6)
LUZ(487, 9.6, CYAN, 1.3, 4.5, pulso=True)
NPC(482, 7, [
    '¡Kael! Sentí el temblor desde aquí... ¿saliste de ahí tú solo?',
    'Kael: No estaba solo. Kiro me cubrió la espalda.',
    'Era uno de los primeros centinelas. Creí que NÉMESIS los había dejado oxidarse.',
    'Ese cristal abre la base. Lo guardé todos estos años por si alguien se atrevía.',
    'Llévatelo. Y Kael... allá adentro hay cosas peores que lo de la cueva.',
    'Kael: Entonces vamos, Kiro.'], desaparece=False, flip=False)
G(500, 516, 7)
G(516, 536, 32)
D['meta'] = dict(x=511, y=7)
# salida: túnel de raíces en la pared, con musgo que cuelga de la orilla del muro
A('flor_azul_1', 507.2, 6.9, 0.95, orden=3); A('planta_5', 505.6, 6.9, 0.9, orden=4, flip=True)
S('roca_1', 515.2, 6.75, 0.5, orden=2, col=(0.8, 0.9, 0.85))
for cy, c in ((14.5, 'colg_musgo_0'), (19.5, 'colg_hoja_0'), (25, 'colg_musgo_1')):
    S(c, 516.35, cy, 0.8, orden=2, ancla='arriba')
LUZ(511, 9, CYAN, 1.6, 6.5)
NARRA(500, 7, 'El túnel de raíces lleva a la base de NEMESIS. Necesitas la llave.', dur=4)
vestir_suelo(420, 436, 11, 'santuario'); vestir_suelo(436, 450, 8, 'santuario'); vestir_suelo(450, 465, 7, 'santuario')
vestir_suelo(470, 500, 7, 'santuario'); vestir_suelo(500, 506, 7, 'santuario')
rayos([430, 458, 480, 498])

# capas de fondo y primer plano en todo el nivel
fondo_lejano(-5, 312, 'musgo'); fondo_lejano(312, 420, 'cueva'); fondo_lejano(420, W + 10, 'musgo')
primer_plano(0, 312, 'musgo'); primer_plano(312, 420, 'cueva'); primer_plano(420, W, 'musgo')


# ───────── sprites que de verdad existen en Assets/Arte/Valle_del_Eco (y Templo_de_Nemesis) ─────────
# arte_dict.json lista más piezas de las que se exportaron: las que faltan se cambian por una parecida
# para que el constructor no deje huecos ("Falta el sprite ...").
EXISTEN = set('''abismo bruma colg_hoja_0 colg_hoja_1 colg_hoja_2 colg_hoja_3 colg_hoja_4 colg_musgo_0 colg_musgo_1 cristal_llave
cueva_bloque cueva_estalactita_0 cueva_estalactita_1 cueva_estalactita_2 cueva_estalactita_3 cueva_estalactitas_4
cueva_estalagmita_0 cueva_estalagmita_1 cueva_estalagmita_2 cueva_estalagmita_3 cueva_pena_0 cueva_pena_1 cueva_pena_2 cueva_pena_3
cueva_piedra_0 cueva_piedra_2 cueva_piedra_3 cueva_piedra_4 cueva_piedra_5 cueva_plat_a cueva_roca_grande_0 cueva_roca_grande_5
cueva_rocas_0 cueva_rocas_1 cueva_rocas_3 cueva_rocas_4 cueva_rocas_5 cueva_rocas_6 cueva_rocas_7 cueva_rocas_8 cueva_rocas_9
cueva_rocas_10 cueva_rocas_11 espina_3 espina_4 espina_5 flor_azul_0_f0 flor_azul_1_f0 halo hierba_0 hierba_2 hierba_3 hierba_4
hierba_5 hoja_curva_0 hoja_curva_1 hoja_curva_2 hongo_rebote_f0 liana_0 liana_1 liana_2 liana_3 loma_0 loma_1 loma_2 loma_3 loma_4
loma_5 loma_6 mago_quieto_f0 mata_0 mata_1 musgo_bloque negro orbe_eco orin_diseno_v2 pasto_0_f0 pasto_1_f0 pasto_2_f0 pasto_3_f0
pasto_5_f0 planta_1_f0 planta_2_f0 planta_3_f0 planta_4_f0 planta_5_f0 planta_6_f0 planta_7_f0 planta_veneno_f0 planta_viento_f0
plat_bola plat_bolita plat_columna plat_cuadro plat_larga_a plat_larga_b plat_larga_c rayo_luz roca_0 roca_1 roca_2 silueta_0
silueta_1 silueta_2 silueta_3 silueta_4 silueta_5 slime_naranja_f0 slime_verde_f0 tallo_0 tallo_1 tallo_2
base_bloque base_plat base_fondo base_tubo base_pantalla base_servidor base_cable base_rejilla base_columna base_alarma base_electrico'''.split())
REEMPLAZO = {'brote_0': 'hierba_0', 'brote_1': 'hierba_4', 'brote_2': 'hierba_2', 'hierba_1': 'hierba_3', 'tallo_3': 'tallo_2',
             'cueva_piedra_1': 'cueva_piedra_0', 'cueva_piedras_a': 'cueva_piedra_3', 'cueva_piedras_b': 'cueva_piedra_2',
             'cueva_pena_4': 'cueva_pena_3', 'cueva_rocas_2': 'cueva_rocas_3', 'plat_larga_d': 'plat_larga_c', 'plat_tira': 'plat_larga_a'}

def corregir_sprites():
    faltan = set()
    for lista, sufijo in ((D['sprites'], ''), (D['plataformas'], '')):
        for s in lista:
            n = s['s']
            if n in EXISTEN: continue
            if n in REEMPLAZO: s['s'] = REEMPLAZO[n]
            elif n.startswith('cueva_roca_grande_'): s['s'] = 'cueva_roca_grande_' + ('0' if int(n.rsplit('_', 1)[1]) % 2 == 0 else '5')
            elif n.startswith('cueva_rocas_'): s['s'] = 'cueva_rocas_' + str(8 + int(n.rsplit('_', 1)[1]) % 4)
            else: faltan.add(n)
    for a in D['anims']:
        if a['s'] + '_f0' not in EXISTEN: faltan.add(a['s'] + '_f0')
    if faltan: print('AVISO: faltan sprites:', sorted(faltan))

D['inicio'] = dict(x=7, y=5)
corregir_sprites()
json.dump(D, open(OUT, 'w'), ensure_ascii=False, indent=0)
print('bloques', len(D['bloques']), 'plataformas', len(D['plataformas']), 'sprites', len(D['sprites']),
      'anims', len(D['anims']), 'luces', len(D['luces']), 'centinelas', len(D['centinelas']), 'npcs', len(D['npcs']))
