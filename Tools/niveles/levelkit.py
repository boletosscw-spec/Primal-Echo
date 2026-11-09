# Herramientas para dibujar mapas de Primal Echo en texto (y=0 abajo).
class Level:
    def __init__(self, width, height):
        self.w, self.h = width, height
        self.g = [[' '] * width for _ in range(height)]
        self.meta = []
    def fill(self, x0, x1, y0, y1, ch='#'):
        for y in range(max(0, y0), min(self.h, y1)):
            for x in range(max(0, x0), min(self.w, x1)):
                self.g[y][x] = ch
    def ground(self, x0, x1, top, ch='#'):
        self.fill(x0, x1, 0, top, ch)
    def plat(self, x0, x1, row, ch='-'):
        self.fill(x0, x1, row, row + 1, ch)
    def put(self, x, y, ch):
        self.g[y][x] = ch
    def clear(self, x0, x1, y0, y1):
        self.fill(x0, x1, y0, y1, ' ')
    def back(self, x0, x1, y0, y1):
        for y in range(max(0, y0), min(self.h, y1)):
            for x in range(max(0, x0), min(self.w, x1)):
                if self.g[y][x] == ' ': self.g[y][x] = ':'
    def text(self):
        return '\n'.join(''.join(r) for r in reversed(self.g))
