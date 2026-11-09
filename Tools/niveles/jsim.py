# Validador de mapas para Primal 1.1: simula PlayerController (salto, doble salto, dash, gravedad estilo Hollow Knight).
import math, sys, heapq
from collections import deque

G = 9.81 * 3.0
JUMP, DJUMP = 12.5, 11.5
FALL_M, LOW_M, APEX_T, APEX_M, MAXFALL = 1.8, 2.2, 1.8, 0.65, 18.0
SPEED, ACC, AIR = 8.0, 75.0, 65.0
DASH_V, DASH_T = 18.0, 0.12
W2, HGT = 0.225, 1.35
DT = 0.02
LIFT, LIFT_H = 12.0, 5   # géiser: columna de 5 tiles que empuja a 12 u/s

def load(path):
    rows = [l.rstrip('\n') for l in open(path, encoding='utf-8')]
    i = rows.index('---')
    grid = [r for r in rows[i + 1:] if r]
    w = max(len(r) for r in grid); grid = [r.ljust(w) for r in grid]; h = len(grid)
    return [[grid[h - 1 - y][x] for x in range(w)] for y in range(h)], w, h

class Map:
    def __init__(s, c, w, h):
        s.c, s.w, s.h = c, w, h
        s.lift = set()
        for y in range(h):
            for x in range(w):
                if c[y][x] == 'J':
                    for k in range(1, LIFT_H + 1):
                        if y + k < h and s.solid(x, y + k): break
                        s.lift.add((x, y + k))
    def at(s, x, y):
        if x < 0 or x >= s.w: return '#'
        if y < 0 or y >= s.h: return ' '
        return s.c[y][x]
    def solid(s, x, y): return s.at(x, y) in '#=%J'
    def oneway(s, x, y): return s.at(x, y) == '-'
    def hazard(s, x, y): return s.at(x, y) == '^'

def cells(x, y):
    for cx in range(math.floor(x - W2 + 1e-4), math.floor(x + W2 - 1e-4) + 1):
        for cy in range(math.floor(y + 1e-4), math.floor(y + HGT - 1e-4) + 1):
            yield cx, cy

def hit_solid(m, x, y): return any(m.solid(cx, cy) for cx, cy in cells(x, y))
def hit_hazard(m, x, y): return any(m.hazard(cx, cy) and y < cy + 0.7 for cx, cy in cells(x, y))
def in_lift(m, x, y): return (math.floor(x), math.floor(y + 0.3)) in m.lift

def simulate(m, x, y, vx, vy, d, hold, dj, dash, steer, walk=False, go=0.0):
    y0 = y; t = 0.0; used_dj = dj is None; dash_end = -1; used_dash = dash is None
    while t < 5:
        t += DT
        dd = d if go <= t < steer else 0
        if not used_dj and t >= dj:
            used_dj = True; vy = DJUMP; vx = dd * SPEED
        if not used_dash and t >= dash:
            used_dash = True; dash_end = t + DASH_T
        if t < dash_end:
            vx, vy_eff, vy = d * DASH_V, 0.0, 0.0
        else:
            vx += max(-AIR * DT, min(AIR * DT, dd * SPEED - vx))
            held = t < hold
            mult = FALL_M if vy < 0 else (APEX_M if (held and vy < APEX_T) else (1.0 if held else LOW_M))
            vy = max(-MAXFALL, vy - G * mult * DT)
            if in_lift(m, x, y): vy = max(vy, LIFT)
            vy_eff = vy
        nx = x + vx * DT
        if hit_solid(m, nx, y): nx = x; vx = 0
        x = nx
        ny = y + vy_eff * DT
        if vy_eff > 0:
            if hit_solid(m, x, ny): vy = 0; ny = y
        elif vy_eff < 0:
            landed = None
            for cx in range(math.floor(x - W2 + 1e-4), math.floor(x + W2 - 1e-4) + 1):
                for cy in range(math.floor(ny), math.floor(y) + 1):
                    if cy + 1 <= y + 1e-6 and cy + 1 > ny and (m.solid(cx, cy) or m.oneway(cx, cy)):
                        landed = max(landed or -99, cy + 1)
            if landed is not None:
                if walk and abs(landed - y0) < 1e-3:
                    y = landed; vy = 0
                    if vx == 0 and t > 0.3: return None
                    continue
                y = landed
                if hit_hazard(m, x, y): return 'death'
                return (math.floor(x), y)
        y = ny
        if hit_hazard(m, x, y): return 'death'
        if y < -2: return 'death'
    return None

def standable(m, x, y):
    return (m.solid(x, y - 1) or m.oneway(x, y - 1)) and not m.solid(x, y) and not m.solid(x, y + 1) and not m.hazard(x, y)

VARIANTS = []
for hold in (0.1, 99):
    for dj in (None, 0.25, 0.5):
        for dash in (None, 0.3):
            for steer in (0.25, 99):
                VARIANTS.append((hold, dj, dash, steer))

def graph(m):
    nodes = [(x, y) for y in range(1, m.h) for x in range(m.w) if standable(m, x, y)]
    ns = set(nodes); E = {n: set() for n in nodes}
    for (x, y) in nodes:
        for dx in (-1, 1):
            if (x + dx, y) in ns: E[(x, y)].add((x + dx, y))
        for d in (-1, 1):
            for sx in (x + 0.5, x + 0.5 + 0.35 * d):
                for (hold, dj, dash, steer) in VARIANTS:
                    r = simulate(m, sx, y, d * SPEED, JUMP, d, hold, dj, dash, steer)
                    if isinstance(r, tuple) and r in ns and r != (x, y): E[(x, y)].add(r)
            for dj in (None, 0.3):
                r = simulate(m, x + 0.5, y, d * SPEED, 0.0, d, 0, dj, None, 99, walk=True)
                if isinstance(r, tuple) and r in ns: E[(x, y)].add(r)
            if (x, y) in m.lift:  # parado dentro del géiser: sube y luego se dirige
                for go in (0.2, 0.4, 0.6):
                    for dj in (None, 0.6, 0.8, 1.0):
                        for dash in (None, 0.9):
                            r = simulate(m, x + 0.5, y, 0, LIFT, d, 99, dj, dash, 99, go=go)
                            if isinstance(r, tuple) and r in ns: E[(x, y)].add(r)
    return nodes, E

def reach(E, s):
    seen = {s}; q = deque([s])
    while q:
        n = q.popleft()
        for t in E.get(n, ()):
            if t not in seen: seen.add(t); q.append(t)
    return seen

def find(m, ch): return [(x, y) for y in range(m.h) for x in range(m.w) if m.at(x, y) == ch]

def near(ns, x, y, depth=4):
    for dy in range(depth):
        if (x, y - dy) in ns: return (x, y - dy)
    return None

def check(path, estimate=True):
    c, w, h = load(path); m = Map(c, w, h)
    nodes, E = graph(m); ns = set(nodes)
    P = near(ns, *find(m, 'P')[0]); seen = reach(E, P); ok = True
    for ch, name in (('X', 'salida'), ('C', 'checkpoint'), ('o', 'fragmento')):
        for (x, y) in find(m, ch):
            n = near(ns, x, y, 6)
            if n is None or n not in seen:
                hit = any((x + dx, y - dy) in seen for dx in (-1, 0, 1) for dy in range(0, 5))
                if not hit: print(f'  !! {name} ({x},{y}) NO alcanzable'); ok = False
    X = near(ns, *find(m, 'X')[0]); rev = {}
    for a, ts in E.items():
        for t in ts: rev.setdefault(t, set()).add(a)
    canexit = reach(rev, X)
    stuck = sorted(n for n in seen if n not in canexit)
    if stuck: ok = False; print(f'  !! {len(stuck)} atascos, ej: {stuck[:10]}')
    msg = ''
    if estimate:
        d = {P: 0}; q = [(0, P)]
        while q:
            t, n = heapq.heappop(q)
            if n == X: break
            if t > d[n]: continue
            for k in E[n]:
                cst = 1 / 7.0 if (k[1] == n[1] and abs(k[0] - n[0]) == 1) else 0.35 + abs(k[0] - n[0]) / 8 + abs(k[1] - n[1]) * 0.05
                if t + cst < d.get(k, 1e9): d[k] = t + cst; heapq.heappush(q, (t + cst, k))
        cnt = {k: len(find(m, k)) for k in 'EBWKo'}
        fight = cnt['E'] * 2.5 + cnt['B'] * 6 + cnt['W'] * 2 + cnt['K'] * 1.5
        tot = d.get(X, 0) + fight + 20 * len(find(m, 'G')) / max(1, len(find(m, 'G')))
        msg = f" | mov {d.get(X,0):.0f}s, enemigos E{cnt['E']} B{cnt['B']} W{cnt['W']} K{cnt['K']}, fragmentos {cnt['o']} -> experto ~{tot/60:.1f} min, normal ~{1.6*tot/60:.1f}-{2.1*tot/60:.1f} min"
    print(f'{path}: {w}x{h} alcanzables {len(seen)}/{len(nodes)} ->', 'OK' if ok else 'REVISAR', msg)
    return ok

if __name__ == '__main__':
    sys.exit(0 if all([check(p) for p in sys.argv[1:]]) else 1)
