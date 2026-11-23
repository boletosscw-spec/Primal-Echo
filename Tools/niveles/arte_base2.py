# Arte de la base tecnológica de NÉMESIS (v2, con mucho más detalle).
# Estilo del Nivel 1: contornos oscuros, sombreado suave, interiores que se hunden a negro y musgo
# de hojas redondas que se come el metal. Todo se dibuja al doble y se reduce para bordes suaves.
# uso: python3 arte_base2.py carpeta_estaticos carpeta_animaciones  -> escribe PNGs y arte_base.json
import os, sys, random, math, json
from PIL import Image, ImageDraw, ImageFilter, ImageChops

EST, ANI = sys.argv[1], sys.argv[2]
os.makedirs(EST, exist_ok=True); os.makedirs(ANI, exist_ok=True)
S = 2
LINEA = (10, 13, 20, 255)
AC0 = (22, 28, 38, 255)      # acero muy oscuro
AC1 = (38, 48, 62, 255)
AC2 = (58, 72, 90, 255)
AC3 = (86, 104, 126, 255)
AC4 = (128, 150, 172, 255)
BRI = (178, 200, 218, 255)
CIAN = (70, 240, 255, 255)
CIAN_OSC = (20, 110, 130, 255)
ROJO = (255, 70, 55, 255)
AMBAR = (255, 180, 60, 255)
OXIDO = (110, 66, 40, 255)
OXIDO2 = (150, 92, 52, 255)
VERDE0 = (14, 40, 22, 255)
VERDE1 = (40, 104, 52, 255)
VERDE2 = (92, 178, 92, 255)
VERDE3 = (150, 222, 130, 255)
INFO = {}   # nombre -> dict de tamaño para el JSON de arte


# ───────────────────────── utilidades ─────────────────────────
def L(w, h):
    return Image.new('RGBA', (int(w * S), int(h * S)), (0, 0, 0, 0))


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(4))


def grad(w, h, c0, c1, vertical=True, curva=1.0):
    g = Image.new('RGBA', (w, h))
    px = g.load()
    n = h if vertical else w
    filas = [mix(c0, c1, (i / max(1, n - 1)) ** curva) for i in range(n)]
    d = ImageDraw.Draw(g)
    for i, c in enumerate(filas):
        if vertical: d.line([(0, i), (w, i)], fill=c)
        else: d.line([(i, 0), (i, h)], fill=c)
    return g


def cilindro(w, h, base, vertical=False, brillo=0.55):
    """Gradiente de cilindro: oscuro en los bordes, brillo cerca de un tercio."""
    g = Image.new('RGBA', (w, h))
    d = ImageDraw.Draw(g)
    n = w if vertical else h
    for i in range(n):
        t = i / max(1, n - 1)
        k = math.sin(t * math.pi) ** 0.7
        hl = math.exp(-((t - 0.3) ** 2) / 0.012) * brillo
        c = tuple(min(255, int(base[j] * (0.35 + 0.75 * k) + 255 * hl * 0.35)) for j in range(3)) + (255,)
        if vertical: d.line([(i, 0), (i, h)], fill=c)
        else: d.line([(0, i), (w, i)], fill=c)
    return g


def rellenar(im, mask, fill):
    """Pinta 'fill' (color o imagen) a través de la máscara L."""
    if isinstance(fill, tuple): fill = Image.new('RGBA', im.size, fill)
    elif fill.size != im.size:
        f2 = Image.new('RGBA', im.size, (0, 0, 0, 0)); f2.paste(fill, (0, 0)); fill = f2
    im.alpha_composite(Image.composite(fill, Image.new('RGBA', im.size, (0, 0, 0, 0)), mask))


def mascara(im_size, forma, *args, **kw):
    m = Image.new('L', im_size, 0)
    getattr(ImageDraw.Draw(m), forma)(*args, fill=255, **kw)
    return m


def rect_grad(im, box, c0, c1, vertical=True, radio=0, curva=1.0):
    x0, y0, x1, y1 = [int(v) for v in box]
    if x1 <= x0 or y1 <= y0: return
    m = Image.new('L', im.size, 0)
    ImageDraw.Draw(m).rounded_rectangle([x0, y0, x1, y1], radius=radio, fill=255)
    g = grad(x1 - x0 + 1, y1 - y0 + 1, c0, c1, vertical, curva)
    full = Image.new('RGBA', im.size, (0, 0, 0, 0)); full.paste(g, (x0, y0))
    im.alpha_composite(Image.composite(full, Image.new('RGBA', im.size, (0, 0, 0, 0)), m))


def contorno(im, grosor=3, color=LINEA):
    """Contorno oscuro alrededor de todo lo dibujado (como el arte del Nivel 1)."""
    a = im.split()[3].point(lambda v: 255 if v > 40 else 0)
    k = grosor * 2 + 1
    dil = a.filter(ImageFilter.MaxFilter(k if k % 2 else k + 1))
    base = Image.new('RGBA', im.size, color[:3] + (0,))
    base.putalpha(dil)
    base.alpha_composite(im)
    return base


def textura(im, fuerza=10, semilla=1, escala=4):
    """Grano de pintura sutil donde hay color."""
    r = random.Random(semilla)
    w, h = im.size
    n = Image.new('L', (w // escala + 1, h // escala + 1))
    n.putdata([128 + r.randint(-fuerza, fuerza) for _ in range(n.size[0] * n.size[1])])
    n = n.resize((w, h), Image.BICUBIC)
    rgb = im.convert('RGB')
    add = Image.merge('RGB', (n, n, n))
    out = ImageChops.add(ImageChops.subtract(rgb, Image.new('RGB', im.size, (128, 128, 128))), add, 1, -0) if False else None
    # suma con signo: (c + n - 128)
    r_, g_, b_ = rgb.split()
    def sumar(c):
        return ImageChops.subtract(ImageChops.add(c, n), Image.new('L', im.size, 128))
    res = Image.merge('RGBA', (sumar(r_), sumar(g_), sumar(b_), im.split()[3]))
    return res


def remache(d, x, y, r, base=AC2):
    d.ellipse([x - r, y - r, x + r, y + r], fill=AC0)
    d.ellipse([x - r * 0.8, y - r * 0.8, x + r * 0.8, y + r * 0.8], fill=base)
    d.ellipse([x - r * 0.55, y - r * 0.6, x + r * 0.1, y - r * 0.05], fill=AC4)


def resplandor(im, cx, cy, radio, color, fuerza=0.8):
    g = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(g)
    pasos = 14
    for i in range(pasos, 0, -1):
        t = i / pasos
        a = int(255 * fuerza * (1 - t) ** 1.6)
        rr = radio * t
        d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=color[:3] + (a,))
    g = g.filter(ImageFilter.GaussianBlur(radio * 0.08 + 1))
    im.alpha_composite(g)


def oxido(im, x, y, largo, ancho, semilla):
    """Chorreado de óxido hacia abajo."""
    r = random.Random(semilla)
    capa = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(capa)
    for k in range(r.randint(2, 4)):
        xx = x + r.uniform(-ancho, ancho)
        l = largo * r.uniform(0.4, 1.0)
        w = ancho * r.uniform(0.25, 0.6)
        for i in range(int(l)):
            t = i / max(1, l)
            a = int(150 * (1 - t) ** 1.3)
            d.line([(xx - w * (1 - t * 0.7), y + i), (xx + w * (1 - t * 0.7), y + i)], fill=OXIDO[:3] + (a,))
    im.alpha_composite(capa.filter(ImageFilter.GaussianBlur(1.2)))


def mugre(im, semilla, cantidad=0.25):
    """Manchas oscuras suaves (suciedad / humedad)."""
    r = random.Random(semilla)
    w, h = im.size
    capa = Image.new('RGBA', im.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(capa)
    for _ in range(int(12 * cantidad * (w * h) / 200000) + 2):
        x, y = r.uniform(0, w), r.uniform(0, h)
        rr = r.uniform(w * 0.05, w * 0.18)
        d.ellipse([x - rr, y - rr * 0.6, x + rr, y + rr * 0.6], fill=(5, 8, 12, r.randint(25, 60)))
    capa = capa.filter(ImageFilter.GaussianBlur(w * 0.03 + 2))
    a = ImageChops.multiply(capa.split()[3], im.split()[3])
    capa.putalpha(a)
    im.alpha_composite(capa)


_HOJAS = {}
def hoja(largo, ancho, oscuro, claro):
    k = (int(largo), int(ancho), oscuro, claro)
    if k in _HOJAS: return _HOJAS[k]
    w, h = int(ancho) + 6, int(largo) + 6
    im = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    m = Image.new('L', (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle([3, 3, w - 3, h - 3], radius=int(ancho / 2), fill=255)
    g = grad(w, h, claro, oscuro, True, 1.3)
    im.paste(g, (0, 0), m)
    # brillo lateral
    d = ImageDraw.Draw(im)
    d.line([(w * 0.35, h * 0.15), (w * 0.35, h * 0.55)], fill=VERDE3[:3] + (90,), width=max(1, int(ancho * 0.18)))
    im = contorno(im, 2)
    _HOJAS[k] = im
    return im


def mechon(im, x, y, tam, angulo=-90, n=None, semilla=0, abre=55, oscuro=VERDE0, claro=VERDE2):
    """Mechón de musgo con hojas redondas (como el musgo del Nivel 1). angulo: hacia dónde crece."""
    r = random.Random(semilla)
    tam = tam * 1.5
    n = int((n or max(4, int(tam / 6))) * 1.6)
    for i in range(n):
        a = angulo + r.uniform(-abre, abre)
        largo = tam * r.uniform(0.55, 1.0)
        ancho = max(5, tam * r.uniform(0.2, 0.3))
        h = hoja(largo, ancho, oscuro, claro if r.random() < 0.7 else VERDE3)
        h = h.rotate(-(a + 90), expand=True, resample=Image.BICUBIC)
        dx = math.cos(math.radians(a)) * largo * 0.42
        dy = math.sin(math.radians(a)) * largo * 0.42
        im.alpha_composite(h, (int(x + dx - h.width / 2 + r.uniform(-tam * 0.25, tam * 0.25)), int(y + dy - h.height / 2)))


def guardar(im, nombre, w, h, carpeta=None, **extra):
    out = im.resize((w, h), Image.LANCZOS)
    out.save(os.path.join(carpeta or EST, nombre + '.png'))
    INFO[nombre] = dict(w=round(w / 100, 3), h=round(h / 100, 3), **extra)
    return out


def hoja_anim(nombre, cuadros, fw, fh, fps, cols=None):
    """Junta cuadros (ya del tamaño final) en una hoja con separación de 2 px y guarda el cuadro 0."""
    n = len(cuadros)
    cols = cols or min(n, 8)
    filas = (n + cols - 1) // cols
    pad = 2
    hoja_im = Image.new('RGBA', (cols * (fw + pad), filas * (fh + pad)), (0, 0, 0, 0))
    for i, c in enumerate(cuadros):
        hoja_im.paste(c, ((i % cols) * (fw + pad), (i // cols) * (fh + pad)))
    hoja_im.save(os.path.join(ANI, nombre + '.png'))
    cuadros[0].save(os.path.join(EST, nombre + '_f0.png'))
    INFO['@' + nombre] = dict(n=nombre, cols=cols, filas=filas, cuadros=n, fw=fw, fh=fh, pad=pad, fps=fps,
                              w=round(fw / 100, 3), h=round(fh / 100, 3))


# ───────────────────────── piezas ─────────────────────────
def placa(im, box, semilla, remaches=True, tono=0.0, bisel=6):
    """Placa de acero con bisel, remaches y grano."""
    x0, y0, x1, y1 = box
    c0, c1 = mix(AC3, AC4, tono), mix(AC1, AC2, tono)
    rect_grad(im, box, c0, c1, True, radio=4 * S)
    d = ImageDraw.Draw(im)
    d.rounded_rectangle(box, radius=4 * S, outline=AC0, width=2 * S)
    d.line([(x0 + bisel, y0 + bisel * 0.6), (x1 - bisel, y0 + bisel * 0.6)], fill=BRI[:3] + (120,), width=S)
    d.line([(x0 + bisel, y1 - bisel * 0.6), (x1 - bisel, y1 - bisel * 0.6)], fill=AC0[:3] + (160,), width=S)
    if remaches:
        rr = 3.2 * S
        for (xx, yy) in ((x0 + 9 * S, y0 + 9 * S), (x1 - 9 * S, y0 + 9 * S), (x0 + 9 * S, y1 - 9 * S), (x1 - 9 * S, y1 - 9 * S)):
            remache(d, xx, yy, rr)


def base_bloque():
    """Bloque de 9 cortes (borde 48 px): placas de acero arriba y a los lados, interior que se hunde a negro."""
    W, H, B = 512, 320, 48
    im = L(W, H)
    d = ImageDraw.Draw(im)
    s = S
    # interior negro con un leve azul en los bordes interiores
    rect_grad(im, (0, 0, W * s, H * s), (8, 11, 16, 255), (0, 0, 0, 255))
    # banda superior: placa gruesa con línea cian
    rect_grad(im, (0, 0, W * s, B * s), AC4, AC1, True, curva=0.8)
    d.line([(0, 3 * s), (W * s, 3 * s)], fill=BRI, width=2 * s)
    d.line([(0, B * s - 2 * s), (W * s, B * s - 2 * s)], fill=AC0, width=3 * s)
    d.line([(0, 22 * s), (W * s, 22 * s)], fill=CIAN_OSC, width=4 * s)
    d.line([(0, 22 * s), (W * s, 22 * s)], fill=CIAN[:3] + (200,), width=1 * s)
    for x in range(0, W, 64):
        d.line([(x * s, 6 * s), (x * s, B * s - 4 * s)], fill=AC0, width=2 * s)
        remache(d, (x + 32) * s, 12 * s, 2.6 * s)
        remache(d, (x + 32) * s, 36 * s, 2.6 * s)
    # lados y fondo: placas verticales que se oscurecen hacia el centro
    for lado in (0, 1):
        x0 = 0 if lado == 0 else (W - B) * s
        rect_grad(im, (x0, B * s, x0 + B * s, H * s), AC2 if lado == 0 else AC1, (6, 8, 12, 255) if lado == 0 else AC2, False)
        for y in range(B + 40, H, 70):
            remache(d, x0 + B * s / 2, y * s, 2.4 * s)
    rect_grad(im, (0, (H - B) * s, W * s, H * s), (4, 6, 9, 255), AC1)
    # sombra hacia el interior
    sh = Image.new('L', im.size, 0)
    ImageDraw.Draw(sh).rectangle([B * s, B * s, (W - B) * s, (H - B) * s], fill=255)
    sh = sh.filter(ImageFilter.GaussianBlur(14 * s))
    negro = Image.new('RGBA', im.size, (0, 0, 0, 255))
    im = Image.composite(negro, im, sh)
    d = ImageDraw.Draw(im)
    # óxido bajo la placa superior
    for x in (80, 230, 400):
        oxido(im, x * s, B * s, 30 * s, 8 * s, x)
    im = textura(im, 7, 11)
    guardar(im, 'base_bloque', W, H, borde=48)


def base_fondo():
    """Pared interior que se repite en las 4 direcciones."""
    W = 512
    im = L(W, W)
    s = S
    rect_grad(im, (0, 0, W * s, W * s), (34, 42, 56, 255), (24, 30, 42, 255))
    d = ImageDraw.Draw(im)
    # paneles 2x2 con relieve
    for px in (0, 256):
        for py in (0, 256):
            x0, y0 = px * s + 6 * s, py * s + 6 * s
            x1, y1 = (px + 256) * s - 6 * s, (py + 256) * s - 6 * s
            rect_grad(im, (x0, y0, x1, y1), (44, 54, 70, 255), (30, 37, 50, 255), radio=6 * s)
            d.rounded_rectangle([x0, y0, x1, y1], radius=6 * s, outline=(16, 20, 28, 255), width=3 * s)
            d.line([(x0 + 8 * s, y0 + 4 * s), (x1 - 8 * s, y0 + 4 * s)], fill=(70, 86, 106, 255), width=s)
            # panel interior hundido
            ix0, iy0, ix1, iy1 = x0 + 34 * s, y0 + 40 * s, x1 - 34 * s, y1 - 60 * s
            rect_grad(im, (ix0, iy0, ix1, iy1), (26, 32, 44, 255), (36, 45, 60, 255), radio=4 * s)
            d.rounded_rectangle([ix0, iy0, ix1, iy1], radius=4 * s, outline=(18, 22, 30, 255), width=2 * s)
            for (xx, yy) in ((x0 + 14 * s, y0 + 14 * s), (x1 - 14 * s, y0 + 14 * s), (x0 + 14 * s, y1 - 14 * s), (x1 - 14 * s, y1 - 14 * s)):
                remache(d, xx, yy, 3 * s, (46, 56, 72, 255))
    # rejilla pequeña en un panel, ranuras en otro
    for i in range(6):
        d.rounded_rectangle([(300) * s, (330 + i * 14) * s, (470) * s, (336 + i * 14) * s], radius=2 * s, fill=(12, 15, 21, 255))
    for i in range(3):
        d.line([(50 * s, (90 + i * 10) * s), (200 * s, (90 + i * 10) * s)], fill=(16, 20, 28, 255), width=3 * s)
    # conducto cian tenue que cruza (se repite a lo ancho)
    y = 236 * s
    d.line([(0, y), (W * s, y)], fill=(18, 60, 74, 255), width=6 * s)
    d.line([(0, y), (W * s, y)], fill=(50, 170, 190, 140), width=2 * s)
    # tubería horizontal que se repite
    tub = cilindro(W * s, 26 * s, (60, 74, 92, 255))
    im.alpha_composite(tub, (0, 470 * s))
    for x in (60, 316):
        rect_grad(im, (x * s, 464 * s, (x + 18) * s, 502 * s), AC3, AC1, False, radio=2 * s)
    mugre(im, 5, 0.6)
    for x in (130, 390):
        oxido(im, x * s, 250 * s, 60 * s, 10 * s, x)
    im = textura(im, 6, 21)
    guardar(im, 'base_fondo', W, W)


def base_plat():
    W, H = 420, 120
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    # soportes en diagonal
    for x in (90, 330):
        d.polygon([(x * s - 8 * s, 30 * s), (x * s + 8 * s, 30 * s), (x * s + 30 * s, 112 * s), (x * s + 14 * s, 112 * s)], fill=AC1)
        d.polygon([(x * s - 8 * s, 30 * s), (x * s + 8 * s, 30 * s), (x * s - 14 * s, 112 * s), (x * s - 30 * s, 112 * s)], fill=AC1)
    # celosía bajo la placa
    rect_grad(im, (8 * s, 26 * s, (W - 8) * s, 58 * s), AC2, AC0)
    for x in range(14, W - 20, 26):
        d.line([(x * s, 30 * s), ((x + 20) * s, 54 * s)], fill=AC3, width=3 * s)
        d.line([((x + 20) * s, 30 * s), (x * s, 54 * s)], fill=AC1, width=3 * s)
    d.rectangle([8 * s, 54 * s, (W - 8) * s, 60 * s], fill=AC1)
    # placa superior con rejilla
    rect_grad(im, (0, 6 * s, W * s, 28 * s), AC4, AC2, radio=4 * s)
    for x in range(12, W - 10, 12):
        d.line([(x * s, 10 * s), (x * s, 24 * s)], fill=AC1, width=2 * s)
    d.line([(4 * s, 8 * s), ((W - 4) * s, 8 * s)], fill=BRI, width=s)
    d.line([(6 * s, 18 * s), ((W - 6) * s, 18 * s)], fill=CIAN_OSC, width=2 * s)
    for x in (16, W - 16):
        remache(d, x * s, 17 * s, 3 * s)
    # franja de peligro en las orillas
    for x0 in (0, W - 34):
        for i in range(4):
            d.polygon([((x0 + i * 9) * s, 28 * s), ((x0 + i * 9 + 5) * s, 28 * s), ((x0 + i * 9 + 1) * s, 36 * s), ((x0 + i * 9 - 4) * s, 36 * s)], fill=AMBAR)
    im = contorno(im, 3 * s // 2)
    # musgo colgando de la celosía
    r = random.Random(3)
    for x in (60, 200, 260, 370):
        mechon(im, x * s, 58 * s, 22 * s, 90, n=6, semilla=x)
    im = textura(im, 6, 31)
    guardar(im, 'base_plat', W, H, surf=0.06)


def base_tubo():
    W, H = 512, 90
    im = L(W, H)
    s = S
    tub = cilindro(W * s, 60 * s, (82, 98, 118, 255))
    im.alpha_composite(tub, (0, 15 * s))
    d = ImageDraw.Draw(im)
    d.line([(0, 15 * s), (W * s, 15 * s)], fill=LINEA, width=3 * s)
    d.line([(0, 75 * s), (W * s, 75 * s)], fill=LINEA, width=3 * s)
    for x in (110, 366):
        rect_grad(im, ((x - 14) * s, 6 * s, (x + 14) * s, 84 * s), AC4, AC1, False, radio=4 * s)
        d.rounded_rectangle([(x - 14) * s, 6 * s, (x + 14) * s, 84 * s], radius=4 * s, outline=LINEA, width=2 * s)
        for yy in (14, 45, 76):
            remache(d, x * s, yy * s, 3 * s)
        oxido(im, (x + 18) * s, 70 * s, 18 * s, 6 * s, x)
    # etiqueta con flecha cian
    d.rounded_rectangle([220 * s, 34 * s, 280 * s, 56 * s], radius=3 * s, fill=(20, 60, 70, 255), outline=LINEA, width=s)
    d.polygon([(232 * s, 45 * s), (262 * s, 38 * s), (262 * s, 52 * s)], fill=CIAN)
    im = textura(im, 6, 41)
    guardar(im, 'base_tubo', W, H)


def pantalla_cuadro(fase, ruido_sem, parpadeo=1.0, W=220, H=170):
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    # pie y soporte
    rect_grad(im, (95 * s, 140 * s, 125 * s, 160 * s), AC3, AC1, False)
    rect_grad(im, (70 * s, 156 * s, 150 * s, 168 * s), AC3, AC1, radio=3 * s)
    # carcasa
    rect_grad(im, (6 * s, 6 * s, (W - 6) * s, 146 * s), AC3, AC1, radio=10 * s)
    d.rounded_rectangle([6 * s, 6 * s, (W - 6) * s, 146 * s], radius=10 * s, outline=LINEA, width=2 * s)
    d.line([(16 * s, 10 * s), ((W - 16) * s, 10 * s)], fill=BRI[:3] + (140,), width=s)
    # pantalla
    sx0, sy0, sx1, sy1 = 20 * s, 20 * s, (W - 20) * s, 124 * s
    br = parpadeo
    rect_grad(im, (sx0, sy0, sx1, sy1), (int(20 * br), int(90 * br), int(100 * br), 255), (int(6 * br), int(40 * br), int(50 * br), 255), radio=6 * s)
    # cuadrícula
    for x in range(sx0, sx1, 14 * s): d.line([(x, sy0), (x, sy1)], fill=(40, 150, 160, int(50 * br)), width=1)
    for y in range(sy0, sy1, 14 * s): d.line([(sx0, y), (sx1, y)], fill=(40, 150, 160, int(50 * br)), width=1)
    # onda que avanza
    pts = []
    for i in range(0, sx1 - sx0, 2 * s):
        t = i / (sx1 - sx0)
        v = math.sin((t * 3.0 + fase) * math.pi * 2) * 0.5 + math.sin((t * 7.3 + fase * 2) * math.pi * 2) * 0.25
        pts.append((sx0 + i, (sy0 + sy1) / 2 - 10 * s + v * 22 * s))
    d.line(pts, fill=CIAN[:3] + (int(255 * br),), width=2 * s)
    # texto falso (líneas)
    r = random.Random(ruido_sem)
    for k in range(3):
        l = r.randint(30, 110)
        d.rectangle([sx0 + 8 * s, sy1 - (14 + k * 9) * s, sx0 + (8 + l) * s, sy1 - (11 + k * 9) * s], fill=(120, 240, 240, int(200 * br)))
    # punto rojo de grabación
    if fase % 1 < 0.5: d.ellipse([sx1 - 14 * s, sy0 + 6 * s, sx1 - 6 * s, sy0 + 14 * s], fill=ROJO)
    # líneas de barrido
    for y in range(sy0, sy1, 3 * s): d.line([(sx0, y), (sx1, y)], fill=(0, 0, 0, 40))
    resplandor(im, (sx0 + sx1) / 2, (sy0 + sy1) / 2, 70 * s, CIAN, 0.18 * br)
    # botones
    for i, c in enumerate((ROJO, AMBAR, CIAN)):
        d.ellipse([(30 + i * 16) * s, 130 * s, (38 + i * 16) * s, 138 * s], fill=c)
    d.rectangle([(W - 70) * s, 131 * s, (W - 28) * s, 137 * s], fill=AC0)
    im = contorno(im, 2 * s)
    return im


def base_pantalla():
    guardar(pantalla_cuadro(0.2, 1), 'base_pantalla', 220, 170)
    cuadros = []
    for i in range(8):
        flick = 0.55 if i == 5 else 1.0
        c = pantalla_cuadro(i / 8, 1 + (i // 3), flick).resize((220, 170), Image.LANCZOS)
        cuadros.append(c)
    hoja_anim('base_monitor', cuadros, 220, 170, 8)


def servidor_cuadro(k, W=170, H=340):
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    rect_grad(im, (4 * s, 4 * s, (W - 4) * s, (H - 4) * s), AC2, AC0, False, radio=6 * s)
    d.rounded_rectangle([4 * s, 4 * s, (W - 4) * s, (H - 4) * s], radius=6 * s, outline=LINEA, width=2 * s)
    d.line([(10 * s, 8 * s), ((W - 10) * s, 8 * s)], fill=AC4, width=s)
    r = random.Random(4)
    for i in range(9):
        y0 = (16 + i * 34) * s
        rect_grad(im, (14 * s, y0, (W - 14) * s, y0 + 28 * s), AC3, AC1, radio=3 * s)
        d.rounded_rectangle([14 * s, y0, (W - 14) * s, y0 + 28 * s], radius=3 * s, outline=AC0, width=s)
        for j in range(7):
            d.line([((22 + j * 6) * s, y0 + 6 * s), ((22 + j * 6) * s, y0 + 22 * s)], fill=AC0, width=2 * s)
        # luces que parpadean según el cuadro
        for j in range(3):
            on = ((i * 7 + j * 3 + k * (j + 1)) % 5) < 3
            c = (CIAN, (90, 255, 140, 255), ROJO)[j] if on else AC0
            cx, cy = (W - 46 + j * 12) * s, y0 + 14 * s
            d.ellipse([cx - 3 * s, cy - 3 * s, cx + 3 * s, cy + 3 * s], fill=c)
            if on: resplandor(im, cx, cy, 9 * s, c, 0.35)
    # cables que salen por abajo
    for j, c in enumerate(((30, 30, 34, 255), (60, 20, 20, 255), (20, 40, 60, 255))):
        d.arc([(20 + j * 20) * s, (H - 30) * s, (80 + j * 20) * s, (H + 20) * s], 180, 270, fill=c, width=5 * s)
    im = contorno(im, 2 * s)
    return im


def base_servidor():
    guardar(servidor_cuadro(0), 'base_servidor', 170, 340)
    hoja_anim('base_rack', [servidor_cuadro(k).resize((170, 340), Image.LANCZOS) for k in range(6)], 170, 340, 6)


def base_cable():
    W, H = 360, 220
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    cables = [((20, 0), (340, 0), 150, (28, 30, 36, 255), 10), ((60, 0), (300, 0), 110, (90, 30, 26, 255), 8),
              ((40, 0), (250, 0), 190, (24, 50, 70, 255), 9), ((120, 0), (355, 0), 80, (40, 44, 50, 255), 6)]
    for (a, b, caida, c, w) in cables:
        pts = []
        for i in range(41):
            t = i / 40
            x = a[0] + (b[0] - a[0]) * t
            y = caida * 4 * t * (1 - t)
            pts.append((x * s, (y + 4) * s))
        d.line(pts, fill=LINEA, width=(w + 4) * s, joint='curve')
        d.line(pts, fill=c, width=w * s, joint='curve')
        hl = [(x, y - w * s * 0.25) for x, y in pts]
        d.line(hl, fill=tuple(min(255, v + 60) for v in c[:3]) + (180,), width=max(1, w * s // 4), joint='curve')
    # conectores en el techo
    for x in (20, 60, 40, 120, 250, 300, 340, 355):
        rect_grad(im, ((x - 8) * s, 0, (x + 8) * s, 14 * s), AC4, AC1, False, radio=2 * s)
    # cinta adhesiva amarilla
    d.rectangle([168 * s, 98 * s, 180 * s, 116 * s], fill=AMBAR)
    guardar(contorno(im, s), 'base_cable', W, H)


def base_rejilla():
    W, H = 200, 130
    im = L(W, H)
    s = S
    placa(im, (4 * s, 4 * s, (W - 4) * s, (H - 4) * s), 1)
    d = ImageDraw.Draw(im)
    rect_grad(im, (22 * s, 22 * s, (W - 22) * s, (H - 22) * s), (6, 8, 12, 255), (16, 20, 28, 255))
    for i in range(8):
        y = (26 + i * 10) * s
        rect_grad(im, (24 * s, y, (W - 24) * s, y + 6 * s), AC3, AC1)
    # hojas atoradas en la rejilla
    mechon(im, 150 * s, 96 * s, 18 * s, 100, n=4, semilla=9)
    guardar(contorno(im, 2 * s), 'base_rejilla', W, H)


def base_columna():
    W, H = 150, 620
    im = L(W, H)
    s = S
    col = cilindro(110 * s, H * s, (78, 94, 114, 255), vertical=True)
    im.alpha_composite(col, (20 * s, 0))
    d = ImageDraw.Draw(im)
    d.line([(75 * s, 0), (75 * s, H * s)], fill=CIAN_OSC, width=5 * s)
    d.line([(75 * s, 0), (75 * s, H * s)], fill=CIAN[:3] + (200,), width=2 * s)
    for y in (30, 170, 320, 470, 590):
        rect_grad(im, (6 * s, (y - 14) * s, (W - 6) * s, (y + 14) * s), AC4, AC1, True, radio=4 * s)
        d.rounded_rectangle([6 * s, (y - 14) * s, (W - 6) * s, (y + 14) * s], radius=4 * s, outline=LINEA, width=2 * s)
        for x in (22, 75, 128):
            remache(d, x * s, y * s, 3.2 * s)
        oxido(im, 50 * s, (y + 14) * s, 40 * s, 8 * s, y)
    im = contorno(im, 3 * s // 2)
    # musgo trepando desde abajo
    for k, (x, y) in enumerate(((40, 600), (110, 610), (70, 560), (30, 520))):
        mechon(im, x * s, y * s, 30 * s, -90, n=7, semilla=k + 40)
    mechon(im, 60 * s, 182 * s, 22 * s, 90, n=6, semilla=77)
    im = textura(im, 6, 51)
    guardar(im, 'base_columna', W, H)


def alarma_cuadro(ang, W=90, H=90, encendida=True):
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    rect_grad(im, (14 * s, 62 * s, 76 * s, 84 * s), AC3, AC1, radio=4 * s)
    d.rounded_rectangle([14 * s, 62 * s, 76 * s, 84 * s], radius=4 * s, outline=LINEA, width=2 * s)
    m = mascara(im.size, 'pieslice', [18 * s, 14 * s, 72 * s, 112 * s], 180, 360)
    rellenar(im, m, grad(im.size[0], im.size[1], (255, 120, 100, 255), (150, 20, 15, 255)))
    d.arc([18 * s, 14 * s, 72 * s, 112 * s], 180, 360, fill=LINEA, width=2 * s)
    # haz que gira (brillo dentro del domo)
    if encendida:
        cx = 45 * s + math.cos(ang) * 16 * s
        k = (math.sin(ang) + 1) / 2
        resplandor(im, cx, 42 * s, 26 * s, (255, 230, 200, 255), 0.6 * (0.4 + 0.6 * k))
    d.ellipse([30 * s, 26 * s, 40 * s, 34 * s], fill=(255, 220, 210, 160))
    return contorno(im, s)


def base_alarma():
    guardar(alarma_cuadro(0.0), 'base_alarma', 90, 90)
    hoja_anim('base_baliza', [alarma_cuadro(i / 8 * math.pi * 2).resize((90, 90), Image.LANCZOS) for i in range(8)], 90, 90, 10)


def base_electrico():
    W, H = 300, 90
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    rect_grad(im, (0, 50 * s, W * s, 90 * s), AC3, AC0, radio=4 * s)
    d.line([(0, 52 * s), (W * s, 52 * s)], fill=AMBAR, width=3 * s)
    for i in range(0, W, 30):
        d.polygon([((i + 6) * s, 52 * s), ((i + 15) * s, 22 * s), ((i + 24) * s, 52 * s)], fill=AC2)
        d.polygon([((i + 12) * s, 52 * s), ((i + 15) * s, 22 * s), ((i + 17) * s, 52 * s)], fill=AC4)
        for yy in range(58, 86, 8):
            d.line([((i + 4) * s, yy * s), ((i + 26) * s, yy * s)], fill=(30, 140, 150, 255), width=2 * s)
    im = contorno(im, s)
    r = random.Random(5)
    for k in range(6):
        x = r.uniform(10, W - 10)
        pts = [(x * s, 20 * s)]
        for j in range(5):
            pts.append(((x + r.uniform(-10, 10)) * s, (20 + (j + 1) * 6) * s))
        d = ImageDraw.Draw(im)
        d.line(pts, fill=CIAN, width=2 * s)
        resplandor(im, x * s, 30 * s, 20 * s, CIAN, 0.4)
    guardar(im, 'base_electrico', W, H)


def base_tanque_cuadro(k, W=170, H=380):
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    # base y tapa
    rect_grad(im, (6 * s, 320 * s, (W - 6) * s, 372 * s), AC3, AC0, radio=8 * s)
    rect_grad(im, (12 * s, 12 * s, (W - 12) * s, 52 * s), AC4, AC1, radio=8 * s)
    for x in (30, 85, 140):
        remache(d, x * s, 340 * s, 4 * s); remache(d, x * s, 30 * s, 3.5 * s)
    d.rectangle([40 * s, 348 * s, 130 * s, 362 * s], fill=(14, 40, 46, 255))
    d.rectangle([44 * s, 351 * s, (44 + 20 + 30 * ((k % 6) / 5)) * s, 359 * s], fill=CIAN)
    # tubos en la tapa
    for x in (50, 120):
        d.rectangle([(x - 6) * s, 0, (x + 6) * s, 14 * s], fill=AC2, outline=LINEA, width=s)
    # vidrio con líquido
    gx0, gy0, gx1, gy1 = 22 * s, 52 * s, (W - 22) * s, 320 * s
    rect_grad(im, (gx0, gy0, gx1, gy1), (40, 160, 140, 200), (14, 70, 70, 230), radio=10 * s)
    # silueta del embrión: cría de raptor enroscada (cuerpo curvo, hocico largo, cola que la rodea)
    cx, cy = W / 2 * s, 196 * s + math.sin(k / 12 * math.pi * 2) * 4 * s
    sil = (8, 36, 34, 225)
    d.arc([cx - 46 * s, cy - 40 * s, cx + 40 * s, cy + 46 * s], 200, 470, fill=sil, width=26 * s)     # lomo curvo
    d.ellipse([cx - 30 * s, cy - 18 * s, cx + 26 * s, cy + 34 * s], fill=sil)                       # vientre
    d.polygon([(cx + 6 * s, cy - 50 * s), (cx + 26 * s, cy - 62 * s), (cx + 58 * s, cy - 52 * s),
               (cx + 60 * s, cy - 44 * s), (cx + 30 * s, cy - 36 * s), (cx + 10 * s, cy - 30 * s)], fill=sil)  # cabeza y hocico
    d.line([(cx + 30 * s, cy - 44 * s), (cx + 56 * s, cy - 46 * s)], fill=(30, 90, 84, 200), width=s)        # boca
    for i in range(5):                                                                                 # cresta del lomo
        a = math.radians(210 + i * 28)
        px, py = cx + math.cos(a) * 46 * s, cy + math.sin(a) * 46 * s
        d.polygon([(px, py), (px + math.cos(a) * 9 * s - 4 * s, py + math.sin(a) * 9 * s), (px + 5 * s, py + 2 * s)], fill=sil)
    d.arc([cx - 60 * s, cy - 6 * s, cx + 30 * s, cy + 70 * s], 20, 200, fill=sil, width=10 * s)      # cola enroscada
    d.line([(cx + 4 * s, cy - 6 * s), (cx + 20 * s, cy + 6 * s)], fill=sil, width=6 * s)             # bracito
    d.line([(cx - 10 * s, cy + 30 * s), (cx + 6 * s, cy + 46 * s)], fill=sil, width=9 * s)           # pata
    d.ellipse([cx + 28 * s, cy - 56 * s, cx + 36 * s, cy - 48 * s], fill=(130, 255, 225, 230))       # ojo
    # cables al embrión
    d.line([(cx - 10 * s, gy0), (cx - 6 * s, cy - 20 * s)], fill=(10, 30, 30, 200), width=3 * s)
    # burbujas que suben
    r = random.Random(8)
    for b in range(14):
        bx = r.uniform(gx0 + 10 * s, gx1 - 10 * s)
        vel = r.uniform(0.6, 1.4)
        fase = r.random()
        t = (fase + k / 12 * vel) % 1
        by = gy1 - 10 * s - t * (gy1 - gy0 - 20 * s)
        rr = r.uniform(2, 5) * s
        d.ellipse([bx - rr, by - rr, bx + rr, by + rr], outline=(200, 255, 240, 200), width=s)
    # brillo del vidrio
    d.rounded_rectangle([gx0 + 8 * s, gy0 + 10 * s, gx0 + 18 * s, gy1 - 30 * s], radius=5 * s, fill=(220, 255, 250, 60))
    d.rounded_rectangle([gx0, gy0, gx1, gy1], radius=10 * s, outline=(20, 60, 60, 255), width=3 * s)
    resplandor(im, cx, cy, 90 * s, CIAN, 0.18)
    im = contorno(im, 2 * s)
    return im


def base_tanque():
    hoja_anim('base_tanque', [base_tanque_cuadro(k).resize((170, 380), Image.LANCZOS) for k in range(12)], 170, 380, 8, cols=6)


def base_consola():
    W, H = 280, 170
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    # cuerpo inclinado
    d.polygon([(10 * s, 80 * s), ((W - 10) * s, 80 * s), ((W - 4) * s, 166 * s), (4 * s, 166 * s)], fill=AC1)
    rect_grad(im, (10 * s, 92 * s, (W - 10) * s, 166 * s), AC2, AC0)
    d.polygon([(20 * s, 40 * s), ((W - 20) * s, 40 * s), ((W - 8) * s, 84 * s), (8 * s, 84 * s)], fill=AC3)
    d.line([(20 * s, 40 * s), ((W - 20) * s, 40 * s)], fill=BRI, width=s)
    # teclas y perillas
    r = random.Random(12)
    for i in range(14):
        x = 30 + i * 16
        c = r.choice((AC4, AC4, CIAN, AMBAR, ROJO))
        d.rounded_rectangle([x * s, 58 * s, (x + 11) * s, 66 * s], radius=2 * s, fill=c)
    for i, x in enumerate((50, 110, 170, 230)):
        d.ellipse([(x - 9) * s, 102 * s, (x + 9) * s, 120 * s], fill=AC3, outline=LINEA, width=s)
        a = r.uniform(0, math.pi * 2)
        d.line([(x * s, 111 * s), ((x + math.cos(a) * 7) * s, (111 + math.sin(a) * 7) * s)], fill=LINEA, width=2 * s)
    # pantallitas
    for x in (40, 160):
        rect_grad(im, (x * s, 8 * s, (x + 80) * s, 40 * s), (30, 120, 130, 255), (8, 40, 50, 255), radio=3 * s)
        for k in range(3):
            d.line([((x + 6) * s, (16 + k * 7) * s), ((x + 30 + r.randint(0, 40)) * s, (16 + k * 7) * s)], fill=CIAN, width=s)
        resplandor(im, (x + 40) * s, 24 * s, 50 * s, CIAN, 0.15)
    d.rectangle([118 * s, 18 * s, 124 * s, 40 * s], fill=AC2)
    # palanca roja
    d.line([(250 * s, 96 * s), (262 * s, 76 * s)], fill=AC4, width=3 * s)
    d.ellipse([256 * s, 70 * s, 268 * s, 82 * s], fill=ROJO)
    im = contorno(im, 2 * s)
    mechon(im, 30 * s, 166 * s, 20 * s, -90, n=5, semilla=3)
    guardar(textura(im, 5, 61), 'base_consola', W, H)


def base_logo():
    """Emblema de NÉMESIS: un ojo dentro de un hexágono, en una placa de pared."""
    W = 220
    im = L(W, W)
    s = S
    c = W / 2 * s
    d = ImageDraw.Draw(im)
    hexa = [(c + math.cos(math.radians(a)) * 100 * s, c + math.sin(math.radians(a)) * 100 * s) for a in range(30, 390, 60)]
    m = mascara(im.size, 'polygon', hexa)
    rellenar(im, m, grad(im.size[0], im.size[1], AC3, AC0))
    d.polygon(hexa, outline=LINEA, width=3 * s)
    hexa2 = [(c + math.cos(math.radians(a)) * 84 * s, c + math.sin(math.radians(a)) * 84 * s) for a in range(30, 390, 60)]
    d.polygon(hexa2, outline=(140, 30, 30, 255), width=3 * s)
    # ojo
    d.ellipse([c - 64 * s, c - 30 * s, c + 64 * s, c + 30 * s], fill=(20, 6, 8, 255))
    d.chord([c - 64 * s, c - 60 * s, c + 64 * s, c + 30 * s], 30, 150, fill=(0, 0, 0, 0))
    m2 = Image.new('L', im.size, 0)
    ImageDraw.Draw(m2).ellipse([c - 62 * s, c - 28 * s, c + 62 * s, c + 28 * s], fill=255)
    rellenar(im, m2, (30, 8, 10, 255))
    d.ellipse([c - 24 * s, c - 24 * s, c + 24 * s, c + 24 * s], fill=(200, 30, 25, 255))
    d.ellipse([c - 10 * s, c - 10 * s, c + 10 * s, c + 10 * s], fill=(255, 140, 120, 255))
    resplandor(im, c, c, 60 * s, ROJO, 0.5)
    for a in range(30, 390, 60):
        remache(d, c + math.cos(math.radians(a)) * 92 * s, c + math.sin(math.radians(a)) * 92 * s, 3.5 * s)
    oxido(im, c - 40 * s, c + 70 * s, 40 * s, 10 * s, 3)
    im = contorno(im, 2 * s)
    guardar(textura(im, 5, 71), 'base_logo', W, W)


def base_puerta_fondo():
    W, H = 320, 420
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    rect_grad(im, (0, 0, W * s, H * s), AC3, AC1, radio=8 * s)
    d.rounded_rectangle([0, 0, W * s, H * s], radius=8 * s, outline=LINEA, width=3 * s)
    # hojas de la puerta
    for x0 in (24, 164):
        rect_grad(im, (x0 * s, 30 * s, (x0 + 132) * s, (H - 30) * s), AC2, AC0, False)
        d.rectangle([x0 * s, 30 * s, (x0 + 132) * s, (H - 30) * s], outline=LINEA, width=2 * s)
    d.line([(160 * s, 30 * s), (160 * s, (H - 30) * s)], fill=LINEA, width=4 * s)
    # franjas de peligro diagonales en las hojas
    for k in range(0, 300, 34):
        d.polygon([(24 * s, (60 + k) * s), (156 * s, (40 + k) * s), (156 * s, (54 + k) * s), (24 * s, (74 + k) * s)], fill=(200, 140, 40, 120))
        d.polygon([(164 * s, (40 + k) * s), (296 * s, (60 + k) * s), (296 * s, (74 + k) * s), (164 * s, (54 + k) * s)], fill=(200, 140, 40, 120))
    # cerradura central y luz roja
    d.ellipse([130 * s, 180 * s, 190 * s, 240 * s], fill=AC2, outline=LINEA, width=3 * s)
    d.ellipse([150 * s, 200 * s, 170 * s, 220 * s], fill=ROJO)
    resplandor(im, 160 * s, 210 * s, 50 * s, ROJO, 0.5)
    for x in (12, W - 12):
        for y in range(40, H - 20, 60): remache(d, x * s, y * s, 4 * s)
    mugre(im, 7, 0.8)
    oxido(im, 90 * s, (H - 30) * s, 20 * s, 10 * s, 1)
    im = contorno(im, 2 * s)
    mechon(im, 20 * s, H * s, 30 * s, -80, n=8, semilla=11)
    mechon(im, (W - 30) * s, H * s, 24 * s, -100, n=6, semilla=12)
    guardar(textura(im, 6, 81), 'base_puerta_fondo', W, H)


def base_raiz():
    """Raíces gruesas que se metieron por el techo (vista de frente, cuelgan hacia abajo)."""
    W, H = 360, 300
    im = L(W, H)
    s = S
    r = random.Random(17)
    d = ImageDraw.Draw(im)
    def rama(x, y, ang, grosor, largo, nivel):
        pts = [(x, y)]
        for i in range(int(largo / 8)):
            ang += r.uniform(-0.25, 0.25)
            ang = ang * 0.9 + math.pi / 2 * 0.1
            x += math.cos(ang) * 8 * s; y += math.sin(ang) * 8 * s
            pts.append((x, y))
        for i in range(len(pts) - 1):
            t = i / len(pts)
            g = max(2 * s, grosor * (1 - t * 0.8))
            d.line([pts[i], pts[i + 1]], fill=LINEA, width=int(g + 4 * s))
        for i in range(len(pts) - 1):
            t = i / len(pts)
            g = max(1, grosor * (1 - t * 0.8))
            col = mix((86, 64, 44, 255), (54, 70, 40, 255), t)
            d.line([pts[i], pts[i + 1]], fill=col, width=int(g))
            d.line([(pts[i][0] - g * 0.2, pts[i][1]), (pts[i + 1][0] - g * 0.2, pts[i + 1][1])], fill=(140, 110, 70, 120), width=max(1, int(g * 0.2)))
        if nivel < 2:
            for _ in range(r.randint(1, 2)):
                j = r.randint(len(pts) // 3, len(pts) - 2)
                rama(pts[j][0], pts[j][1], ang + r.choice((-0.8, 0.8)), grosor * 0.55, largo * 0.5, nivel + 1)
    for x in (60, 170, 290):
        rama(x * s, 0, math.pi / 2 + r.uniform(-0.3, 0.3), r.uniform(16, 22) * s, r.uniform(200, 280) * s, 0)
    for x in (60, 170, 290, 110, 230):
        mechon(im, x * s, 6 * s, 26 * s, 90, n=7, semilla=x)
    guardar(textura(im, 6, 91), 'base_raiz', W, H)


def base_franja():
    W, H = 512, 36
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, W * s, H * s], fill=(24, 24, 24, 255))
    for x in range(-40, W + 40, 32):
        d.polygon([(x * s, H * s), ((x + 16) * s, H * s), ((x + 34) * s, 0), ((x + 18) * s, 0)], fill=(214, 160, 44, 255))
    d.line([(0, s), (W * s, s)], fill=(255, 220, 140, 255), width=s)
    d.line([(0, (H - 1) * s), (W * s, (H - 1) * s)], fill=LINEA, width=2 * s)
    mugre(im, 3, 1.2)
    guardar(textura(im, 8, 101), 'base_franja', W, H)


def base_viga():
    W, H = 512, 70
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    rect_grad(im, (0, 0, W * s, 12 * s), AC4, AC2)
    rect_grad(im, (0, 58 * s, W * s, 70 * s), AC3, AC1)
    for x in range(0, W, 64):
        d.line([(x * s, 12 * s), ((x + 32) * s, 58 * s)], fill=AC2, width=7 * s)
        d.line([((x + 32) * s, 58 * s), ((x + 64) * s, 12 * s)], fill=AC2, width=7 * s)
        d.line([(x * s, 12 * s), ((x + 32) * s, 58 * s)], fill=AC4[:3] + (120,), width=2 * s)
        remache(d, (x + 32) * s, 62 * s, 3 * s); remache(d, x * s, 7 * s, 3 * s)
    d.line([(0, 0), (W * s, 0)], fill=LINEA, width=3 * s)
    d.line([(0, 70 * s), (W * s, 70 * s)], fill=LINEA, width=3 * s)
    d.line([(0, 12 * s), (W * s, 12 * s)], fill=LINEA, width=2 * s)
    d.line([(0, 58 * s), (W * s, 58 * s)], fill=LINEA, width=2 * s)
    guardar(textura(im, 6, 111), 'base_viga', W, H)


def base_caja():
    W, H = 150, 120
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    rect_grad(im, (4 * s, 10 * s, (W - 4) * s, (H - 2) * s), (90, 104, 84, 255), (44, 52, 42, 255), radio=4 * s)
    d.rounded_rectangle([4 * s, 10 * s, (W - 4) * s, (H - 2) * s], radius=4 * s, outline=LINEA, width=2 * s)
    rect_grad(im, (0, 4 * s, W * s, 20 * s), AC4, AC2, radio=3 * s)
    d.line([(4 * s, 26 * s), ((W - 4) * s, (H - 8) * s)], fill=(60, 70, 56, 255), width=8 * s)
    d.line([((W - 4) * s, 26 * s), (4 * s, (H - 8) * s)], fill=(60, 70, 56, 255), width=8 * s)
    d.rectangle([50 * s, 50 * s, 100 * s, 76 * s], fill=(214, 160, 44, 255))
    d.polygon([(75 * s, 54 * s), (90 * s, 72 * s), (60 * s, 72 * s)], fill=(30, 30, 30, 255))
    im = contorno(im, 2 * s)
    mechon(im, 120 * s, 10 * s, 18 * s, -90, n=5, semilla=2)
    guardar(textura(im, 6, 121), 'base_caja', W, H)


def base_barril():
    W, H = 90, 130
    im = L(W, H)
    s = S
    b = cilindro(80 * s, 120 * s, (70, 92, 110, 255), vertical=True)
    im.alpha_composite(b, (5 * s, 6 * s))
    d = ImageDraw.Draw(im)
    for y in (22, 66, 110):
        d.rectangle([5 * s, (y - 3) * s, 85 * s, (y + 3) * s], fill=AC1)
    d.ellipse([5 * s, 0, 85 * s, 14 * s], fill=AC3, outline=LINEA, width=s)
    d.ellipse([30 * s, 40 * s, 60 * s, 58 * s], fill=(214, 160, 44, 255))
    d.line([(45 * s, 42 * s), (45 * s, 56 * s)], fill=LINEA, width=2 * s)
    oxido(im, 30 * s, 70 * s, 30 * s, 8 * s, 4)
    guardar(contorno(im, 2 * s), 'base_barril', W, H)


def base_lampara():
    W, H = 80, 160
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    d.line([(40 * s, 0), (40 * s, 100 * s)], fill=(20, 22, 26, 255), width=3 * s)
    d.polygon([(14 * s, 132 * s), (66 * s, 132 * s), (52 * s, 100 * s), (28 * s, 100 * s)], fill=AC2)
    d.polygon([(14 * s, 132 * s), (66 * s, 132 * s), (52 * s, 100 * s), (28 * s, 100 * s)], outline=LINEA)
    d.line([(30 * s, 104 * s), (20 * s, 128 * s)], fill=AC4, width=2 * s)
    d.ellipse([28 * s, 124 * s, 52 * s, 142 * s], fill=(255, 236, 190, 255))
    resplandor(im, 40 * s, 138 * s, 40 * s, (255, 220, 160, 255), 0.6)
    guardar(contorno(im, s), 'base_lampara', W, H)


def base_engranaje():
    W = 320
    im = L(W, W)
    s = S
    c = W / 2 * s
    pts = []
    for i in range(48):
        a = i / 48 * math.pi * 2
        rr = (150 if (i // 2) % 2 == 0 else 128) * s
        pts.append((c + math.cos(a) * rr, c + math.sin(a) * rr))
    m = mascara(im.size, 'polygon', pts)
    rellenar(im, m, grad(im.size[0], im.size[1], AC3, AC1))
    d = ImageDraw.Draw(im)
    d.polygon(pts, outline=LINEA, width=3 * s)
    d.ellipse([c - 100 * s, c - 100 * s, c + 100 * s, c + 100 * s], outline=AC0, width=6 * s)
    for k in range(6):
        a = k / 6 * math.pi * 2
        d.ellipse([c + math.cos(a) * 70 * s - 18 * s, c + math.sin(a) * 70 * s - 18 * s, c + math.cos(a) * 70 * s + 18 * s, c + math.sin(a) * 70 * s + 18 * s], fill=(0, 0, 0, 0))
    hole = Image.new('L', im.size, 0)
    hd = ImageDraw.Draw(hole)
    for k in range(6):
        a = k / 6 * math.pi * 2
        hd.ellipse([c + math.cos(a) * 70 * s - 20 * s, c + math.sin(a) * 70 * s - 20 * s, c + math.cos(a) * 70 * s + 20 * s, c + math.sin(a) * 70 * s + 20 * s], fill=255)
    hd.ellipse([c - 22 * s, c - 22 * s, c + 22 * s, c + 22 * s], fill=255)
    a_ = im.split()[3]
    im.putalpha(ImageChops.subtract(a_, hole))
    d = ImageDraw.Draw(im)
    d.ellipse([c - 36 * s, c - 36 * s, c + 36 * s, c + 36 * s], outline=AC4, width=4 * s)
    guardar(textura(im, 6, 131), 'base_engranaje', W, W)


def base_antena():
    """Torre de antena de la base, para el fondo lejano del bosque."""
    W, H = 260, 900
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    top, bot = 120, H
    for side in (-1, 1):
        d.line([(130 * s + side * 10 * s, top * s), (130 * s + side * 90 * s, bot * s)], fill=AC2, width=7 * s)
    y = top + 20
    k = 0
    while y < bot - 20:
        w0 = 10 + (y - top) / (bot - top) * 80
        w1 = 10 + (y + 60 - top) / (bot - top) * 80
        d.line([(130 * s - w0 * s, y * s), (130 * s + w1 * s, (y + 60) * s)], fill=AC1, width=4 * s)
        d.line([(130 * s + w0 * s, y * s), (130 * s - w1 * s, (y + 60) * s)], fill=AC1, width=4 * s)
        d.line([(130 * s - w0 * s, y * s), (130 * s + w0 * s, y * s)], fill=AC2, width=4 * s)
        y += 60; k += 1
    d.line([(130 * s, 0), (130 * s, top * s)], fill=AC3, width=5 * s)
    # platos
    for (yy, side) in ((260, -1), (420, 1)):
        cx = 130 * s + side * 50 * s
        d.chord([cx - 46 * s, (yy - 46) * s, cx + 46 * s, (yy + 46) * s], 90 if side < 0 else -90, 270 if side < 0 else 90, fill=AC3)
        d.line([(130 * s, yy * s), (cx, yy * s)], fill=AC2, width=4 * s)
    im = contorno(im, 2 * s)
    d = ImageDraw.Draw(im)
    d.ellipse([122 * s, 0, 138 * s, 16 * s], fill=ROJO)
    resplandor(im, 130 * s, 8 * s, 30 * s, ROJO, 0.7)
    # lianas trepando desde abajo
    for k, x in enumerate((70, 190, 110, 160)):
        mechon(im, x * s, H * s, 40 * s, -90, n=10, semilla=k + 90)
    guardar(im, 'base_antena', W, H)


def base_escombro():
    W, H = 260, 120
    im = L(W, H)
    s = S
    d = ImageDraw.Draw(im)
    r = random.Random(23)
    piezas = []
    for i in range(10):
        x = r.uniform(20, W - 20); y = r.uniform(60, 112)
        w = r.uniform(30, 70); h = r.uniform(10, 26); a = r.uniform(-35, 35)
        piezas.append((y, x, w, h, a))
    piezas.sort()
    for (y, x, w, h, a) in piezas:
        p = L(w + 10, h + 10)
        rect_grad(p, (5 * s, 5 * s, (w + 5) * s, (h + 5) * s), r.choice((AC3, AC4, OXIDO2)), AC1, radio=2 * s)
        pd = ImageDraw.Draw(p)
        remache(pd, 12 * s, (h / 2 + 5) * s, 2.5 * s)
        p = p.rotate(a, expand=True, resample=Image.BICUBIC)
        im.alpha_composite(p, (int(x * s - p.width / 2), int(y * s - p.height / 2)))
    im = contorno(im, 2 * s)
    for x in (40, 120, 200):
        mechon(im, x * s, 116 * s, 22 * s, -90, n=6, semilla=x)
    guardar(textura(im, 6, 141), 'base_escombro', W, H)


def base_tuberia_v():
    W, H = 90, 512
    im = L(W, H)
    s = S
    im.alpha_composite(cilindro(60 * s, H * s, (76, 92, 112, 255), vertical=True), (15 * s, 0))
    d = ImageDraw.Draw(im)
    d.line([(15 * s, 0), (15 * s, H * s)], fill=LINEA, width=3 * s)
    d.line([(75 * s, 0), (75 * s, H * s)], fill=LINEA, width=3 * s)
    for y in (100, 356):
        rect_grad(im, (6 * s, (y - 14) * s, 84 * s, (y + 14) * s), AC4, AC1, radio=4 * s)
        d.rounded_rectangle([6 * s, (y - 14) * s, 84 * s, (y + 14) * s], radius=4 * s, outline=LINEA, width=2 * s)
        for x in (16, 45, 74): remache(d, x * s, y * s, 3 * s)
        oxido(im, 40 * s, (y + 14) * s, 50 * s, 8 * s, y)
    guardar(textura(im, 6, 151), 'base_tuberia_v', W, H)


# ── animaciones de ambiente ──
def chispas():
    s = S
    fw, fh = 160, 160
    cuadros = []
    r = random.Random(31)
    parts = [(r.uniform(-1, 1) * 3.2, r.uniform(-4.5, -1.0), r.uniform(0.2, 1.0)) for _ in range(18)]
    for k in range(10):
        im = L(fw, fh)
        d = ImageDraw.Draw(im)
        # punta del cable roto
        d.line([(80 * s, 0), (80 * s, 44 * s)], fill=LINEA, width=12 * s)
        d.line([(80 * s, 0), (80 * s, 44 * s)], fill=(40, 42, 48, 255), width=8 * s)
        d.line([(78 * s, 44 * s), (72 * s, 54 * s)], fill=AMBAR, width=2 * s)
        d.line([(82 * s, 44 * s), (90 * s, 52 * s)], fill=AMBAR, width=2 * s)
        if k < 6:
            t = k / 6
            for (vx, vy, sp) in parts:
                x = 80 + vx * 18 * t * sp * 3
                y = 54 + (-vy * 10 * t + 60 * t * t) * sp * 1.6
                x2 = x - vx * 4; y2 = y - 6
                d.line([(x * s, y * s), (x2 * s, y2 * s)], fill=(255, 240, 180, int(255 * (1 - t))), width=2 * s)
            resplandor(im, 80 * s, 54 * s, (30 - k * 4) * s, (255, 220, 140, 255), 0.8 * (1 - t))
        cuadros.append(im.resize((fw, fh), Image.LANCZOS))
    hoja_anim('base_chispas', cuadros, fw, fh, 14, cols=5)


def vapor():
    s = S
    fw, fh = 160, 260
    cuadros = []
    r = random.Random(41)
    bolas = [(r.uniform(0, 1), r.uniform(-14, 14), r.uniform(18, 34)) for _ in range(14)]
    for k in range(12):
        im = L(fw, fh)
        capa = Image.new('RGBA', im.size, (0, 0, 0, 0))
        d = ImageDraw.Draw(capa)
        for (f, dx, rr) in bolas:
            t = (f + k / 12) % 1
            y = (fh - 20 - t * (fh - 30)) * s
            x = (80 + dx * (0.5 + t) + math.sin(t * 6 + f * 9) * 8) * s
            R = rr * (0.5 + t) * s
            d.ellipse([x - R, y - R, x + R, y + R], fill=(210, 225, 235, int(110 * math.sin(t * math.pi))))
        im.alpha_composite(capa.filter(ImageFilter.GaussianBlur(6 * s)))
        # rejilla de salida
        dd = ImageDraw.Draw(im)
        rect_grad(im, (40 * s, (fh - 14) * s, 120 * s, fh * s), AC3, AC1, radio=3 * s)
        for i in range(5):
            dd.line([((50 + i * 15) * s, (fh - 12) * s), ((50 + i * 15) * s, (fh - 2) * s)], fill=AC0, width=3 * s)
        cuadros.append(im.resize((fw, fh), Image.LANCZOS))
    hoja_anim('base_vapor', cuadros, fw, fh, 10, cols=6)


def ventilador():
    fw = 200
    cuadros = []
    for k in range(8):
        im = L(fw, fw)
        s_ = S
        c = fw / 2 * s_
        placa(im, (4 * s_, 4 * s_, (fw - 4) * s_, (fw - 4) * s_), 1)
        d = ImageDraw.Draw(im)
        d.ellipse([c - 84 * s_, c - 84 * s_, c + 84 * s_, c + 84 * s_], fill=(6, 8, 12, 255), outline=LINEA, width=3 * s_)
        ang0 = k / 8 * (math.pi / 2)
        for b in range(4):
            a = ang0 + b * math.pi / 2
            pts = [(c, c)]
            for t in range(9):
                aa = a + t / 8 * 0.9
                rr = 78 * s_
                pts.append((c + math.cos(aa) * rr, c + math.sin(aa) * rr))
            d.polygon(pts, fill=AC3, outline=AC0)
            d.line([(c, c), (c + math.cos(a + 0.2) * 70 * s_, c + math.sin(a + 0.2) * 70 * s_)], fill=AC4, width=2 * s_)
        d.ellipse([c - 18 * s_, c - 18 * s_, c + 18 * s_, c + 18 * s_], fill=AC2, outline=LINEA, width=2 * s_)
        for i in range(-80, 81, 16):
            d.line([(c - 86 * s_, c + i * s_), (c + 86 * s_, c + i * s_)], fill=(30, 36, 46, 200), width=2 * s_)
        cuadros.append(contorno(im, 2 * s_).resize((fw, fw), Image.LANCZOS))
    hoja_anim('base_ventilador', cuadros, fw, fw, 16)


PIEZAS = [base_bloque, base_fondo, base_plat, base_tubo, base_pantalla, base_servidor, base_cable, base_rejilla,
          base_columna, base_alarma, base_electrico, base_tanque, base_consola, base_logo, base_puerta_fondo, base_raiz,
          base_franja, base_viga, base_caja, base_barril, base_lampara, base_engranaje, base_antena, base_escombro,
          base_tuberia_v, chispas, vapor, ventilador]
solo = sys.argv[3:] if len(sys.argv) > 3 else None
for f in PIEZAS:
    if solo and f.__name__ not in solo: continue
    f(); print('ok', f.__name__)
json.dump(INFO, open(os.path.join(EST, '..', 'arte_base.json'), 'w'), indent=1)
