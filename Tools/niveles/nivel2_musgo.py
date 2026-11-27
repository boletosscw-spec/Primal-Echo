# Nivel 2 · Base de NÉMESIS. Genera Assets/Niveles/Mapas/Nivel_02_Musgo.json
# Primera mitad: bosque de musgo (mismo arte y estilo que el Nivel 1).
# Segunda mitad: base tecnológica (arte en Assets/Arte/Templo_de_Nemesis/Estaticos) con dos jefes:
#   AERIS (pteranodón) en la arena alta y el CENTINELA REX en la sala del núcleo, antes de la salida.
# Unidades del mundo: 1 = 32 px del juego. Kael mide ~2.5 de alto.
# Salto ~2.7 de alto / ~6 de largo · doble salto ~4.9 de alto / ~9 de largo.
import json, random, sys, os

W, H = 330, 40
rng = random.Random(20261122)
AQUI = os.path.dirname(os.path.abspath(__file__))
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(AQUI, '..', '..', 'Assets', 'Niveles', 'Mapas', 'Nivel_02_Musgo.json')
ART = json.load(open(os.path.join(AQUI, 'arte_dict.json')))
EST, ANI = ART['estaticos'], ART['animaciones']
# arte de la base tecnológica (tamaños en unidades, 100 px = 1). Lo genera Tools/niveles/arte_base2.py
EST.update({
    'base_bloque': dict(w=5.12, h=3.2, tipo='deco', borde=48, surf=0.0),
    'base_fondo': dict(w=5.12, h=5.12, tipo='deco'),
    'base_plat': dict(w=4.2, h=1.2, tipo='plat', surf=0.06),
    'base_tubo': dict(w=5.12, h=0.9, tipo='deco'),
    'base_pantalla': dict(w=2.2, h=1.7, tipo='deco'),
    'base_monitor_f0': dict(w=2.2, h=1.7, tipo='deco'),
    'base_servidor': dict(w=1.7, h=3.4, tipo='deco'),
    'base_rack_f0': dict(w=1.7, h=3.4, tipo='deco'),
    'base_cable': dict(w=3.6, h=2.2, tipo='deco'),
    'base_rejilla': dict(w=2.0, h=1.3, tipo='deco'),
    'base_columna': dict(w=1.5, h=6.2, tipo='deco'),
    'base_alarma': dict(w=0.9, h=0.9, tipo='deco'),
    'base_baliza_f0': dict(w=0.9, h=0.9, tipo='deco'),
    'base_electrico': dict(w=3.0, h=0.9, tipo='deco'),
    'base_tanque_f0': dict(w=1.7, h=3.8, tipo='deco'),
    'base_consola': dict(w=2.8, h=1.7, tipo='deco'),
    'base_logo': dict(w=2.2, h=2.2, tipo='deco'),
    'base_puerta_fondo': dict(w=3.2, h=4.2, tipo='deco'),
    'base_raiz': dict(w=3.6, h=3.0, tipo='deco'),
    'base_franja': dict(w=5.12, h=0.36, tipo='deco'),
    'base_viga': dict(w=5.12, h=0.7, tipo='deco'),
    'base_caja': dict(w=1.5, h=1.2, tipo='deco'),
    'base_barril': dict(w=0.9, h=1.3, tipo='deco'),
    'base_lampara': dict(w=0.8, h=1.6, tipo='deco'),
    'base_engranaje': dict(w=3.2, h=3.2, tipo='deco'),
    'base_antena': dict(w=2.6, h=9.0, tipo='deco'),
    'base_escombro': dict(w=2.6, h=1.2, tipo='deco'),
    'base_tuberia_v': dict(w=0.9, h=5.12, tipo='deco'),
    'base_chispas_f0': dict(w=1.6, h=1.6, tipo='deco'),
    'base_vapor_f0': dict(w=1.6, h=2.6, tipo='deco'),
    'base_ventilador_f0': dict(w=2.0, h=2.0, tipo='deco'),
})
ANI.update({
    'base_monitor': {'cols': 8, 'filas': 1, 'cuadros': 8, 'fw': 220, 'fh': 170, 'pad': 2, 'fps': 8, 'w': 2.2, 'h': 1.7},
    'base_rack': {'cols': 6, 'filas': 1, 'cuadros': 6, 'fw': 170, 'fh': 340, 'pad': 2, 'fps': 6, 'w': 1.7, 'h': 3.4},
    'base_baliza': {'cols': 8, 'filas': 1, 'cuadros': 8, 'fw': 90, 'fh': 90, 'pad': 2, 'fps': 10, 'w': 0.9, 'h': 0.9},
    'base_tanque': {'cols': 6, 'filas': 2, 'cuadros': 12, 'fw': 170, 'fh': 380, 'pad': 2, 'fps': 8, 'w': 1.7, 'h': 3.8},
    'base_chispas': {'cols': 5, 'filas': 2, 'cuadros': 10, 'fw': 160, 'fh': 160, 'pad': 2, 'fps': 14, 'w': 1.6, 'h': 1.6},
    'base_vapor': {'cols': 6, 'filas': 2, 'cuadros': 12, 'fw': 160, 'fh': 260, 'pad': 2, 'fps': 10, 'w': 1.6, 'h': 2.6},
    'base_ventilador': {'cols': 8, 'filas': 1, 'cuadros': 8, 'fw': 200, 'fh': 200, 'pad': 2, 'fps': 16, 'w': 2.0, 'h': 2.0},
})

D = dict(titulo='NIVEL 2 · BASE DE NÉMESIS',
         subtitulo='Cruza el bosque y entra a la base de NÉMESIS.',
         completado='NIVEL 2 COMPLETADO', siguiente='', subMeta='Siguiente: NÉMESIS', metaSinLlave=True,
         ancho=W, alto=H, bloques=[], techos=[], plataformas=[], sprites=[], anims=[], luces=[],
         peligros=[], rebotes=[], slimes=[], centinelas=[], npcs=[], narraciones=[], checkpoints=[], frutas=[],
         chips=[], fragmentos=[], cuevas=[], fondoCueva=[], fondoBase=[], puertas=[], jefes=[])
CYAN = (0.35, 0.9, 1.0)

# ───────── helpers ─────────
def G(x0, x1, top, estilo='musgo'):
    D['bloques'].append(dict(x0=x0, x1=x1, y0=-30, y1=top, estilo=estilo))

def TECHO(x0, x1, bottom, estilo='cueva'):
    D['techos'].append(dict(x0=x0, x1=x1, y0=bottom, y1=H + 12, estilo=estilo))

def P(x, y, s, esc=1.0, uno=True, flip=False, raices=True):
    D['plataformas'].append(dict(x=x, y=y, s=s, esc=esc, unSentido=uno, flip=flip))
    w = EST[s]['w'] * esc
    if raices and not s.startswith(('cueva', 'base')): raices_de(x, y, s, esc)
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

def A(s, x, y, esc=1.0, orden=4, fps=0.0, flip=False, col=(1, 1, 1), a=1.0, par=0.0, flipY=False):
    D['anims'].append(dict(s=s, x=round(x, 3), y=round(y, 3), esc=round(esc, 3), orden=orden,
                           fps=fps or ANI[s]['fps'], flipX=flip, flipY=flipY, r=col[0], g=col[1], b=col[2], a=a, par=par))

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
    D['npcs'].append(dict(x=x, y=y, esc=1.25, flipX=flip, desaparece=desaparece, lineas=lineas, radio=6.5, kiroX=kiroX))

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


# ───────── base tecnológica ─────────
ROJO = (1.0, 0.3, 0.25)
CALIDA = (1.0, 0.82, 0.55)
AMBAR = (1.0, 0.7, 0.3)
VERDOSO = (0.45, 1.0, 0.82)
LEJOS = (0.17, 0.21, 0.28)          # tinte de la maquinaria lejana
SOMBRA = (0.035, 0.045, 0.06)       # tinte del primer plano (siluetas casi negras)
rb = random.Random(7331)            # aleatorio propio de la base: no mueve la decoración del bosque


def SA(s, x, y, esc, orden, flip=False, col=(1, 1, 1), a=1.0, par=0.0, arriba=False, centro=False, flipY=False):
    """Animación con ancla flexible (el constructor la ancla abajo)."""
    h = ANI[s]['h'] * esc
    yy = y - h if arriba else (y - h / 2 if centro else y)
    A(s, x, yy, esc, orden=orden, flip=flip, col=col, a=a, par=par, flipY=flipY)


def tira(s, x0, x1, y, esc, orden, col=(1, 1, 1), ancla='centro', par=0.0, a=1.0):
    """Repite una pieza horizontal exactamente de x0 a x1 (tubos, vigas, franjas)."""
    w = EST[s]['w']
    n = max(1, round((x1 - x0) / (w * esc)))
    e = (x1 - x0) / (n * w)
    for i in range(n):
        S(s, x0 + (i + 0.5) * w * e, y, e, orden=orden, ancla=ancla, col=col, par=par, a=a)


def techo_musgo(x0, x1, y, densidad=0.35):
    """Musgo que cuelga del techo de la base: la selva se está metiendo al acero."""
    x = x0 + rb.uniform(0.5, 2.0)
    while x < x1 - 0.5:
        if rb.random() < densidad:
            s = rb.choice(['colg_musgo_0', 'colg_musgo_1', 'colg_hoja_3', 'colg_hoja_4', 'colg_hoja_1'])
            S(s, x, y + 0.2, rb.uniform(0.4, 0.75), orden=rb.choice([-7, 2]), ancla='arriba', flip=rb.random() < .5, col=(0.8, 0.92, 0.85))
        x += rb.uniform(1.2, 3.2)


def lejos_base(x0, x1, piso, techo):
    """Maquinaria enorme al fondo, oscura y con parallax: engranes, columnas, tuberías y tanques."""
    alto = techo - piso
    x = x0 + rb.uniform(1, 4)
    while x < x1 - 2:
        r = rb.random()
        if r < 0.3 and alto > 8:
            e = rb.uniform(1.6, 2.6)
            S('base_engranaje', x, piso + alto * rb.uniform(0.45, 0.7), e, orden=-47, par=0.22, ancla='centro', col=LEJOS, rot=rb.uniform(0, 30))
            x += rb.uniform(5, 8)
        elif r < 0.55:
            S('base_columna', x, piso - 3, rb.uniform(1.5, 2.1), orden=-46, par=0.18, col=LEJOS)
            x += rb.uniform(4, 7)
        elif r < 0.78:
            for k in range(rb.randint(2, 3)):
                S('base_tuberia_v', x + k * 0.75, piso - 2, rb.uniform(1.0, 1.4), orden=-46, par=0.18, col=LEJOS)
            x += rb.uniform(4, 6)
        else:
            S('base_engranaje', x, techo - rb.uniform(0.5, 1.5), rb.uniform(1.0, 1.4), orden=-47, par=0.22, ancla='centro', col=LEJOS, rot=rb.uniform(0, 30))
            x += rb.uniform(4, 6)


def techo_base(x0, x1, techo, musgo=0.35, raices=0.0, lamparas=True, alarmas=True):
    """Lo que cuelga del techo: viga, tubería, franja, cables, lámparas, alarmas, chispas, raíces y musgo."""
    tira('base_viga', x0, x1, techo - 0.33, 0.95, -13, col=(0.82, 0.87, 0.96))
    tira('base_tubo', x0, x1, techo - 0.98, 0.5, -12, col=(0.85, 0.9, 1.0))
    x = x0 + rb.uniform(1.5, 3.5)
    while x < x1 - 1.5:
        r = rb.random()
        if r < 0.3:
            S('base_cable', x, techo - 0.2, rb.uniform(0.55, 0.85), orden=-11, ancla='arriba', flip=rb.random() < .5)
            x += rb.uniform(2.5, 4)
        elif r < 0.52 and lamparas:
            e = rb.uniform(0.7, 0.85)
            S('base_lampara', x, techo - 0.25, e, orden=-10, ancla='arriba')
            LUZ(x, techo - 0.25 - 1.45 * e, CALIDA, 0.75, 3.6, pulso=rb.random() < 0.25)
            x += rb.uniform(4, 6)
        elif r < 0.68 and alarmas:
            S('base_alarma', x, techo - 0.2, 0.5, orden=-10, ancla='arriba', flipY=True)
            LUZ(x, techo - 0.75, ROJO, 0.7, 2.4, pulso=True)
            x += rb.uniform(4, 7)
        elif r < 0.76:
            SA('base_chispas', x, techo - 0.15, 0.65, -9, arriba=True)
            LUZ(x, techo - 1.0, AMBAR, 0.5, 1.8, pulso=True)
            x += rb.uniform(5, 8)
        elif r < 0.76 + raices:
            S('base_raiz', x, techo + 0.15, rb.uniform(0.55, 0.85), orden=-8, ancla='arriba', flip=rb.random() < .5, col=(0.85, 0.9, 0.85))
            x += rb.uniform(3, 5)
        else:
            x += rb.uniform(1.5, 3)
    techo_musgo(x0, x1, techo - 0.1, musgo)


def pared_base(x0, x1, piso, techo, tema):
    """Objetos contra la pared, según el tipo de sala."""
    alto = techo - piso
    # tuberías verticales que bajan por la pared cada tanto
    x = x0 + rb.uniform(2, 5)
    while x < x1 - 1:
        if not reservado(x):
            e = min(1.0, (alto - 0.8) / 5.12)
            S('base_tuberia_v', x, piso - 0.05, e, orden=-21, col=(0.78, 0.84, 0.92))
            if rb.random() < 0.5: S('base_tuberia_v', x + 0.6, piso - 0.05, e * 0.9, orden=-21, col=(0.7, 0.76, 0.86))
        x += rb.uniform(9, 15)
    x = x0 + rb.uniform(1.5, 3)
    while x < x1 - 1.5:
        if reservado(x): x += 1.5; continue
        r = rb.random()
        if tema == 'servidores' and r < 0.5:
            # fila de 2 a 4 racks con luces que parpadean
            n = rb.randint(2, 4)
            e = min(0.78, (alto - 1.6) / 3.4)
            for k in range(n):
                if x + k * 1.32 > x1 - 1: break
                SA('base_rack', x + k * 1.32, piso - 0.05, e, -17, col=(0.85, 0.9, 0.98))
            LUZ(x + n * 0.66, piso + 2.0, CYAN, 0.45, 2.6, pulso=True)
            if alto > 6.5 and rb.random() < 0.6:
                SA('base_monitor', x + n * 0.66 - 0.66, piso + 3.0 * e + 1.2, 0.5, -16, centro=False)
            x += n * 1.32 + rb.uniform(1.5, 3)
        elif r < 0.62:
            e = 0.62
            y = min(piso + 2.7, techo - 1.9)
            SA('base_monitor', x, y - 0.85 * e, e, -15)
            LUZ(x, y, CYAN, 0.55, 2.4, pulso=True)
            x += rb.uniform(3, 4.5)
        elif r < 0.72:
            S('base_consola', x, piso - 0.05, rb.uniform(0.62, 0.75), orden=-14)
            LUZ(x, piso + 1.3, CYAN, 0.4, 2.0, pulso=False)
            x += rb.uniform(3, 4)
        elif r < 0.8:
            e = rb.uniform(0.62, 0.78)
            SA('base_tanque', x, piso - 0.05, e, -18)
            LUZ(x, piso + 1.6, VERDOSO, 0.5, 2.6, pulso=True)
            x += rb.uniform(2.5, 3.5)
        elif r < 0.88:
            SA('base_ventilador', x, min(piso + 3.2, techo - 1.5), rb.uniform(0.5, 0.65), -16, centro=True)
            x += rb.uniform(3, 4)
        elif r < 0.94:
            S('base_rejilla', x, min(piso + 3.3, techo - 1.4), rb.uniform(0.5, 0.65), orden=-16, ancla='centro', col=(0.8, 0.85, 0.92))
            x += rb.uniform(2.5, 3.5)
        else:
            S('base_logo', x, min(piso + 3.4, techo - 1.6), 0.6, orden=-19, ancla='centro')
            LUZ(x, min(piso + 3.4, techo - 1.6), ROJO, 0.5, 2.2, pulso=True)
            x += rb.uniform(3, 4)


def piso_base(x0, x1, piso, musgo=0.3):
    """Cajas, barriles, escombros, vapor y musgo sobre el piso de metal."""
    x = x0 + rb.uniform(1, 2.5)
    while x < x1 - 1:
        if reservado(x): x += 1.0; continue
        r = rb.random()
        if r < 0.16:
            e = rb.uniform(0.55, 0.7)
            S('base_caja', x, piso - 0.05, e, orden=2, flip=rb.random() < .5, col=(0.85, 0.9, 0.85))
            if rb.random() < 0.4: S('base_caja', x + rb.uniform(-0.2, 0.2), piso - 0.05 + 1.2 * e * 0.92, e * 0.85, orden=2, col=(0.8, 0.85, 0.8))
            x += rb.uniform(3, 5)
        elif r < 0.27:
            S('base_barril', x, piso - 0.05, rb.uniform(0.55, 0.68), orden=3, col=(0.85, 0.9, 0.95))
            if rb.random() < 0.5: S('base_barril', x + 0.6, piso - 0.05, 0.58, orden=2, col=(0.75, 0.8, 0.85))
            x += rb.uniform(3, 5)
        elif r < 0.37:
            S('base_escombro', x, piso - 0.12, rb.uniform(0.5, 0.7), orden=3, flip=rb.random() < .5)
            x += rb.uniform(3, 5)
        elif r < 0.45:
            SA('base_vapor', x, piso - 0.05, rb.uniform(0.7, 0.9), 4, a=0.8)
            x += rb.uniform(5, 8)
        else:
            x += rb.uniform(1.0, 2.5)
    # musgo y hierba sobre el metal
    x = x0 + rb.uniform(0.5, 1.5)
    while x < x1 - 0.5:
        if rb.random() < musgo and not reservado(x):
            c = rb.choice(['hierba_0', 'hierba_4', 'hierba_2', 'hierba_5', 'mata_0'])
            S(c, x, piso - 0.12, rb.uniform(0.4, 0.65), orden=rb.choice([2, 3]), flip=rb.random() < .5, col=(0.75, 0.88, 0.8))
        x += rb.uniform(0.8, 2.2)


def frente_base(x0, x1, piso, techo):
    """Siluetas en primer plano (casi negras, se mueven al revés): tubos abajo y cables arriba."""
    x = x0 + rb.uniform(2, 6)
    while x < x1 - 2:
        if rb.random() < 0.5:
            S('base_tubo', x, piso - 1.15, rb.uniform(0.9, 1.2), orden=24, par=-0.15, ancla='centro', col=SOMBRA, rot=rb.uniform(-6, 6))
        else:
            S('base_cable', x, techo + 0.4, rb.uniform(1.0, 1.4), orden=24, par=-0.15, ancla='arriba', col=SOMBRA, flip=rb.random() < .5)
        x += rb.uniform(9, 15)


def sala_base(x0, x1, piso, techo, tema='pasillo', musgo=0.3, raices=0.0, frente=True):
    lejos_base(x0, x1, piso, techo)
    techo_base(x0, x1, techo, musgo=musgo, raices=raices)
    pared_base(x0, x1, piso, techo, tema)
    piso_base(x0, x1, piso, musgo)
    if frente: frente_base(x0, x1, piso, techo)


def chatarra(x, y, s, esc, rot, col=(0.62, 0.72, 0.68)):
    """Restos de metal de la base tirados en el bosque, medio enterrados y con musgo encima."""
    S(s, x, y, esc, orden=2, rot=rot, col=col)
    S(rng.choice(['colg_musgo_0', 'colg_hoja_3']), x + rng.uniform(-0.4, 0.4), y + 0.5, 0.45, orden=3, ancla='arriba')
    S(rng.choice(['brote_0', 'brote_1', 'hierba_0']), x + rng.uniform(-0.8, 0.8), y - 0.1, 0.6, orden=3)


def selva_fondo(x0, x1, suelo):
    """Capas de selva entre el fondo parallax y el suelo: montículos de musgo oscuros, troncos lejanos y lianas."""
    r = random.Random(int(x0 * 7 + 3))
    x = x0 + r.uniform(0, 4)
    while x < x1:
        s = r.choice(['silueta_2', 'silueta_3', 'silueta_4', 'silueta_5', 'loma_1', 'loma_3', 'loma_5'])
        S(s, x, suelo - r.uniform(1.6, 2.4), r.uniform(0.55, 0.85), orden=-30, par=0.12, col=(0.32, 0.45, 0.38), flip=r.random() < .5)
        x += r.uniform(6, 11)
    x = x0 + r.uniform(2, 8)
    while x < x1:
        S(r.choice(['silueta_0', 'silueta_1']), x, suelo - 3, r.uniform(0.75, 1.0), orden=-36, par=0.28, col=(0.2, 0.3, 0.26), a=0.85, flip=r.random() < .5)
        x += r.uniform(18, 28)
    # (Antes aquí iban troncos de liana sueltos en el cielo cada 7-13 tiles: se veían flotando y todos iguales.
    #  Ya no se ponen: lo que cuelga en el bosque sale de las plataformas y las orillas, que sí lo sostienen.)


def antena_lejana(x, suelo, esc, par=0.38):
    """Las antenas de la base asoman entre los árboles a lo lejos (de ahí viene la señal)."""
    S('base_antena', x, suelo - 5, esc, orden=-40, par=par, col=(0.2, 0.26, 0.26))


# ═════════ ZONA 1 · LINDE DEL BOSQUE (0–60) ═════════
G(-12, 2, 32)
G(2, 26, 5)
G(26, 40, 6)
G(40, 60, 5)
libres += [(7, 12)]
CP(9, 5, 'Linde del Bosque')
A('flor_azul_0', 7.4, 4.9, 0.9, orden=3); LUZ(7.7, 7.2, CYAN, 0.9, 3.5)
NARRA(4, 5, 'Kael: La señal viene de esas antenas... la base de NÉMESIS está al este.')
NARRA(30, 6, 'KIRO apunta con su escáner: hay restos de metal entre los árboles.')
P(18, 9.2, 'plat_bola', 0.9)
ORBE(18, 10.4)
FRUTA(34, 6)
vestir_suelo(2, 26, 5, 'musgo'); vestir_suelo(26, 40, 6, 'musgo'); vestir_suelo(40, 60, 5, 'musgo')
chatarra(46.5, 4.9, 'base_tubo', 0.5, -14)
chatarra(55, 4.8, 'base_rejilla', 0.55, 18)
chatarra(23.5, 4.85, 'base_caja', 0.5, -24, col=(0.55, 0.68, 0.6))
S('base_cable', 50.5, 4.75, 0.55, orden=2, rot=172, col=(0.55, 0.65, 0.6), ancla='centro')       # cable tirado entre la hierba
rayos([14, 36, 52])
selva_fondo(0, 60, 5)
antena_lejana(30, 5, 1.25)

# ═════════ ZONA 2 · RUINAS CUBIERTAS (60–140) ═════════
G(60, 72, 5)
G(72, 86, 1)
ESPINAS(72.3, 1, 13.4)
bruma(79, 2.2, 1.4)
P(76, 6.5, 'plat_cuadro', 1.0)
P(82, 7.5, 'plat_bola', 1.0)
NARRA(65, 5, 'Kael: Más espinas... de raíz en raíz otra vez.', dur=4)
G(86, 104, 6)
REBOTE(99, 6)
P(103.5, 12.5, 'plat_larga_b', 0.55)
ORBE(103.5, 13.8)
G(104, 114, 9)
G(114, 140, 7)
libres += [(119, 122), (129, 134)]
CHIP(120, 7, 'REGISTRO 03 · ORIN',
     'La base se selló sola cuando NÉMESIS despertó. Los centinelas que quedaron adentro siguen de guardia.')
CP(131, 7, 'Ruinas Cubiertas')
A('flor_azul_1', 129.3, 6.9, 0.85, orden=3); LUZ(129.5, 9.0, CYAN, 0.9, 3.5)
vestir_suelo(60, 72, 5, 'musgo'); vestir_suelo(86, 98, 6, 'musgo'); vestir_suelo(104, 114, 9, 'musgo'); vestir_suelo(114, 140, 7, 'musgo')
# cada vez más restos de la base conforme te acercas
chatarra(92, 5.85, 'base_columna', 0.32, 78)
chatarra(125, 6.85, 'base_pantalla', 0.5, -20)
chatarra(66.5, 4.9, 'base_escombro', 0.6, 0)
chatarra(89.5, 5.9, 'base_barril', 0.55, 96, col=(0.5, 0.62, 0.55))
chatarra(108.5, 8.9, 'base_tuberia_v', 0.3, 84)
chatarra(136.5, 6.9, 'base_caja', 0.55, 8, col=(0.55, 0.68, 0.6))
S('base_columna', 116.5, 6.4, 0.55, orden=-3, rot=-8, col=(0.5, 0.6, 0.55))                         # pilar caído contra el talud
S('colg_musgo_1', 116.2, 9.6, 0.6, orden=-2, ancla='arriba'); S('colg_hoja_2', 116.9, 8.9, 0.55, orden=-2, ancla='arriba')
S('base_raiz', 117.2, 9.9, 0.55, orden=-2, ancla='arriba', rot=-8)
rayos([92, 124])
selva_fondo(60, 140, 6)
antena_lejana(102, 6, 1.45)
antena_lejana(131, 7, 1.6, par=0.34)

# ═════════ ZONA 3 · ENTRADA DE LA BASE (140–166) ═════════
G(140, 166, 7)
libres += [(161, 166)]
FRUTA(147, 7)
NARRA(146, 7, 'KIRO emite un pitido largo: detrás de esa barrera está la base.')
vestir_suelo(140, 160, 7, 'musgo')
D['puertas'].append(dict(x=163, y=7))
LUZ(163, 9.5, ROJO, 1.0, 4.0)
selva_fondo(140, 160, 7)
# restos amontonados frente a la entrada
chatarra(143.5, 6.9, 'base_escombro', 0.75, 0)
chatarra(151, 6.9, 'base_consola', 0.6, -6, col=(0.5, 0.62, 0.56))
chatarra(156.5, 6.95, 'base_barril', 0.6, 0, col=(0.6, 0.7, 0.66))
S('base_barril', 157.3, 6.95, 0.55, orden=2, rot=82, col=(0.55, 0.65, 0.6))
S('base_franja', 162.5, 7.05, 0.7, orden=3, ancla='abajo')                                          # umbral de la puerta
# fachada: el muro de acero que la selva se está tragando
S('base_franja', 163, 12.85, 0.75, orden=3, ancla='centro')                                         # dintel
S('base_logo', 163.4, 16.0, 0.85, orden=3, ancla='centro'); LUZ(163.4, 16.0, ROJO, 0.7, 2.8, pulso=True)
SA('base_baliza', 160.75, 12.95, 0.55, 3, arriba=True, flipY=True); LUZ(160.75, 12.3, ROJO, 0.9, 3.0, pulso=True)
S('base_lampara', 165.2, 12.95, 0.7, orden=3, ancla='arriba'); LUZ(165.2, 11.9, CALIDA, 0.7, 3.2)
S('base_raiz', 161.3, 21.5, 0.95, orden=3, ancla='arriba', col=(0.85, 0.9, 0.85))
S('base_raiz', 164.6, 19.0, 0.7, orden=3, ancla='arriba', flip=True, col=(0.8, 0.88, 0.82))
for cy, c in ((13.3, 'colg_musgo_0'), (13.3, 'colg_hoja_0'), (13.3, 'colg_musgo_1'), (15.6, 'colg_hoja_2'), (18.2, 'colg_musgo_1')):
    S(c, 160.3 + rng.uniform(0, 1.5), cy, 0.8, orden=3, ancla='arriba')
for yy in (14.2, 17.4, 20.6):                                                                       # remaches grandes de la fachada
    S('base_tuberia_v', 160.45, yy - 1.6, 0.62, orden=2, col=(0.7, 0.78, 0.8))

# ═════════ BASE · ESCLUSA (166–196) ═════════
G(166, 240, 7, 'base')                 # esclusa + arena + servidores: un solo piso, sin costuras
TECHO(160, 196, 13, 'base')
libres += [(170, 175)]
CP(172, 7, 'Esclusa de la Base')
NARRA(168, 7, 'Kael: Todo está cubierto de musgo... esta base lleva años dormida.')
NARRA(189, 7, 'KIRO se esconde detrás de Kael: algo grande se mueve allá arriba.')
# la selva entró por la puerta: raíces y musgo al principio, metal limpio al fondo
sala_base(166, 181, 7, 13, tema='pasillo', musgo=0.65, raices=0.35)
sala_base(181, 196, 7, 13, tema='pasillo', musgo=0.25, raices=0.05)
S('base_puerta_fondo', 186.5, 6.95, 0.88, orden=-20)                                                # compuerta interior, sellada
SA('base_baliza', 186.5, 6.95 + 4.2 * 0.88, 0.5, -19); LUZ(186.5, 11.0, ROJO, 0.9, 3.2, pulso=True)
S('base_franja', 186.5, 7.0, 0.7, orden=3, ancla='abajo')
S('base_logo', 178.5, 10.4, 0.55, orden=-19, ancla='centro'); LUZ(178.5, 10.4, ROJO, 0.45, 2.0, pulso=True)

# ═════════ BASE · ARENA DE AERIS (196–222) ═════════
TECHO(196, 222, 23, 'base')
P(200.5, 11.5, 'base_plat', 0.8)
P(217.5, 11.5, 'base_plat', 0.8)
P(209, 15, 'base_plat', 0.9)
P(201.5, 18.5, 'base_plat', 0.7)
P(216.5, 18.5, 'base_plat', 0.7)
D['jefes'].append(dict(n='Aeris', x=209, y=7))
# hangar alto: engranes gigantes al fondo, el emblema de NÉMESIS y la incubadora de Aeris
S('base_engranaje', 209, 15.5, 2.6, orden=-47, par=0.2, ancla='centro', col=LEJOS, rot=12)
S('base_engranaje', 199.5, 19.5, 1.5, orden=-47, par=0.2, ancla='centro', col=LEJOS, rot=40)
S('base_engranaje', 219.0, 10.5, 1.3, orden=-47, par=0.2, ancla='centro', col=LEJOS, rot=5)
for tx in (198.6, 200.4, 202.2):
    S('base_tuberia_v', tx, 6.0, 3.0, orden=-46, par=0.16, col=LEJOS)
for tx, e in ((216.4, 2.6), (218.9, 3.2)):                                                          # lado derecho distinto al izquierdo
    S('base_tuberia_v', tx, 6.0, e, orden=-46, par=0.16, col=LEJOS)
S('base_columna', 221.0, 4.0, 1.9, orden=-46, par=0.18, col=LEJOS)
S('base_logo', 209, 20.2, 1.35, orden=-19, ancla='centro'); LUZ(209, 20.2, ROJO, 0.9, 4.5, pulso=True)
SA('base_tanque', 203.4, 6.95, 0.72, -18); SA('base_tanque', 205.2, 6.95, 0.64, -18, flip=True)    # incubadoras (una ya vacía)
for k in range(3): SA('base_rack', 212.4 + k * 1.15, 6.95, 0.62, -17, col=(0.8, 0.86, 0.95))         # al otro lado, racks
S('base_caja', 215.9, 6.95, 0.6, orden=-12, col=(0.8, 0.85, 0.8))
LUZ(204.3, 8.4, VERDOSO, 0.7, 3.2, pulso=True); LUZ(213.6, 8.6, CYAN, 0.5, 2.8, pulso=True)
SA('base_monitor', 209, 9.2, 0.9, -15); LUZ(209, 10.0, CYAN, 0.8, 3.2)
S('base_consola', 209, 6.95, 0.75, orden=-14)
for cx in (197.6, 220.4):
    S('base_columna', cx, 7, 0.7, orden=-14, col=(0.66, 0.72, 0.8))
    SA('base_baliza', cx, 7 + 6.2 * 0.7, 0.55, -13); LUZ(cx, 11.9, ROJO, 0.8, 3.0, pulso=True)
SA('base_ventilador', 199.0, 15.0, 0.75, -16, centro=True)
S('base_rejilla', 219.2, 14.2, 0.7, orden=-16, ancla='centro', col=(0.8, 0.85, 0.92))
tira('base_viga', 196, 222, 22.67, 0.95, -13, col=(0.82, 0.87, 0.96))
tira('base_tubo', 196, 222, 22.02, 0.5, -12, col=(0.85, 0.9, 1.0))
for ax in (203.6, 216.8):
    S('base_alarma', ax, 22.8, 0.6, orden=-10, ancla='arriba', flipY=True)
    LUZ(ax, 22.1, ROJO, 0.9, 3.0, pulso=True)
for lx, ly in ((200.2, 22.6), (210.3, 22.6)):
    S('base_lampara', lx, ly, 0.85, orden=-10, ancla='arriba'); LUZ(lx, ly - 1.25, CALIDA, 0.8, 4.0)
# el techo está roto: entran raíces y luz de la selva
S('base_raiz', 205.5, 23.2, 1.0, orden=-8, ancla='arriba', col=(0.85, 0.9, 0.85))
S('base_raiz', 213.2, 23.2, 0.85, orden=-8, ancla='arriba', flip=True, col=(0.85, 0.9, 0.85))
S('rayo_luz', 206.5, 23.0, 1.5, orden=-9, rot=-16, a=0.35, ancla='arriba')
S('rayo_luz', 212.0, 23.0, 1.2, orden=-9, rot=-19, a=0.28, ancla='arriba')
techo_musgo(196, 222, 22.9, 0.55)
piso_base(196, 222, 7, 0.25)
S('base_franja', 202.0, 7.02, 0.6, orden=3, ancla='abajo'); S('base_franja', 216.0, 7.02, 0.6, orden=3, ancla='abajo')
frente_base(196, 222, 7, 23)

# ═════════ BASE · SALA DE SERVIDORES (222–284) ═════════
TECHO(222, 284, 15, 'base')
G(240, 252, 3, 'base')
for ex in (241.5, 244.5, 247.5, 250.5):
    S('base_electrico', ex, 2.9, 1.0, orden=6)
D['peligros'].append(dict(x=246, y=3.55, w=11.4, h=1.1, regresa=True))
LUZ(246, 4.2, CYAN, 1.0, 5.0, pulso=True)
NARRA(236, 7, 'El piso electrificado te lastima. Cruza por las rejillas.', dur=4)
P(244, 9.5, 'base_plat', 0.6)
P(248.5, 10.5, 'base_plat', 0.6)
G(252, 284, 8, 'base')
libres += [(256, 261)]
CP(258, 8, 'Sala de Servidores')
FRUTA(263, 8)
NARRA(253, 8, 'KIRO capta la señal de NÉMESIS: viene de abajo, al fondo de la base.')
NARRA(278, 8, 'Kael: No hay vuelta atrás... algo enorme respira ahí abajo.')
sala_base(222, 240, 7, 15, tema='servidores', musgo=0.2)
sala_base(252, 284, 8, 15, tema='servidores', musgo=0.15)
# el foso eléctrico: racks caídos, chispas y franjas en las orillas
techo_base(240, 252, 15, musgo=0.1, lamparas=False, alarmas=False)
lejos_base(240, 252, 3, 15)
for cx in (242.5, 249.0):
    SA('base_chispas', cx, 14.85, 0.7, -9, arriba=True); LUZ(cx, 14.0, AMBAR, 0.6, 2.0, pulso=True)
S('base_rack_f0', 239.0, 6.2, 0.6, orden=-3, rot=-78, col=(0.6, 0.66, 0.74))                       # rack que se cayó al foso
S('base_franja', 239.2, 7.02, 0.4, orden=3, ancla='abajo'); S('base_franja', 252.9, 8.02, 0.4, orden=3, ancla='abajo')
SA('base_vapor', 251.0, 2.95, 0.8, 4, a=0.6)

# ═════════ BASE · SALA DEL NÚCLEO · CENTINELA REX (284–318) ═════════
G(284, 318, 1, 'base')                 # se baja de un salto: ya no se puede volver
TECHO(284, 318, 13, 'base')
G(318, 340, 32, 'base')
D['meta'] = dict(x=315, y=1)          # el Rex aparece 9 tiles antes de la salida y la cierra hasta vencerlo
# el núcleo: el ojo de NÉMESIS domina la sala y los tanques de los centinelas la rodean
S('base_engranaje', 301, 7.0, 2.4, orden=-47, par=0.2, ancla='centro', col=LEJOS, rot=20)
S('base_logo', 301, 8.3, 1.8, orden=-19, ancla='centro'); LUZ(301, 8.3, ROJO, 1.2, 6.0, pulso=True)
for i, (tx, e) in enumerate(((286.6, 0.85), (288.5, 0.78), (311.8, 0.9))):                         # tanques de distinto tamaño
    SA('base_tanque', tx, 0.95, e, -18, flip=i % 2 == 1)
S('base_tanque_f0', 290.6, 1.62, 0.8, orden=-17, rot=-84, col=(0.55, 0.62, 0.62))                      # uno se cayó y está apagado
for k in range(2): SA('base_rack', 313.6 + k * 1.2, 0.95, 0.7, -17, col=(0.8, 0.86, 0.95))
LUZ(288.6, 2.6, VERDOSO, 0.8, 3.6, pulso=True); LUZ(312.4, 2.6, VERDOSO, 0.8, 3.6, pulso=True)
for cx in (293.5, 308.5):
    S('base_columna', cx, 1, 0.95, orden=-14, col=(0.6, 0.66, 0.76))
    SA('base_baliza', cx, 1 + 6.2 * 0.95, 0.55, -13); LUZ(cx, 7.4, ROJO, 0.9, 3.2, pulso=True)
S('base_consola', 297.0, 0.95, 0.75, orden=-14); S('base_escombro', 305.5, 0.88, 0.75, orden=-14, flip=True)
SA('base_monitor', 297.0, 3.6, 0.6, -15); S('base_pantalla', 305.3, 4.4, 0.5, orden=-15, rot=7, ancla='centro', col=(0.6, 0.66, 0.7))
LUZ(297, 4.1, CYAN, 0.5, 2.4, pulso=True); LUZ(305, 4.1, CYAN, 0.5, 2.4, pulso=True)
SA('base_monitor', 315, 5.2, 1.0, -15); LUZ(315, 6.1, CYAN, 1.0, 4.0)
techo_base(284, 318, 13, musgo=0.4, raices=0.12)
SA('base_vapor', 289.8, 0.95, 0.9, 4, a=0.75)
S('base_escombro', 292.0, 0.88, 0.7, orden=3); S('base_caja', 316.8, 0.95, 0.6, orden=2)
frente_base(284, 318, 1, 13)

# fondo de la base y primer plano del bosque
D['fondoBase'].append(dict(x0=160, x1=W + 12, y0=-6, y1=34))
primer_plano(0, 158, 'musgo')


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
base_alarma base_antena base_baliza_f0 base_barril base_bloque base_cable base_caja base_chispas_f0 base_columna base_consola base_electrico base_engranaje base_escombro base_fondo base_franja base_lampara base_logo base_monitor_f0 base_pantalla base_plat base_puerta_fondo base_rack_f0 base_raiz base_rejilla base_servidor base_tanque_f0 base_tuberia_v base_tubo base_vapor_f0 base_ventilador_f0 base_viga'''.split())
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

D['inicio'] = dict(x=5, y=5)
corregir_sprites()
json.dump(D, open(OUT, 'w'), ensure_ascii=False, indent=0)
print('bloques', len(D['bloques']), 'techos', len(D['techos']), 'plataformas', len(D['plataformas']), 'sprites', len(D['sprites']),
      'anims', len(D['anims']), 'luces', len(D['luces']), 'jefes', D['jefes'])
