using UnityEngine;

namespace PrimalEcho
{
    // Comparte el atlas bitmap del HUD y conserva el filtro Point al adaptar el menú a la ventana.
    public sealed class BraceletHologram : MonoBehaviour
    {
        private BraceletMenu menu;
        private Texture2D shard, key, star, heart, fruit, core, chip;
        private int fruitIndex = -1;
        public bool IsFruit(int index) => index >= 0 && index == fruitIndex;
        private struct Entry { public Texture2D icon; public string title, description; }
        private readonly System.Collections.Generic.List<Entry> entries = new System.Collections.Generic.List<Entry>();
        private static readonly Color Text = new Color(.85f, 1f, .96f);
        private float opacity;

        public void Configure(BraceletMenu value)
        {
            menu = value;
            shard = Resources.Load<Texture2D>("HUD/icono_fragmento");
            key = Resources.Load<Texture2D>("HUD/icono_llave");
            star = Resources.Load<Texture2D>("HUD/icono_estrella");
            heart = Resources.Load<Texture2D>("HUD/corazon_lleno");
            fruit = Resources.Load<Texture2D>(ItemPickup.IconName(ItemKind.Fruit));
            core = Resources.Load<Texture2D>(ItemPickup.IconName(ItemKind.Core));
            chip = Resources.Load<Texture2D>(ItemPickup.IconName(ItemKind.Chip));
        }

        private void OnGUI()
        {
            if (!menu || !menu.IsActive || !menu.Settings || Event.current.type != EventType.Repaint) return;
            var settings = menu.Settings;
            Matrix4x4 oldMatrix = GUI.matrix;
            Color oldColor = GUI.color;
            int oldDepth = GUI.depth;
            GUI.depth = -1000;
            float p = Mathf.SmoothStep(0f, 1f, menu.Projection);
            Fill(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, settings.backgroundDim * p));
            float scale = Mathf.Min((float)Screen.width / settings.referenceResolution.x,
                (float)Screen.height / settings.referenceResolution.y);
            Vector2 origin = new Vector2((Screen.width - settings.referenceResolution.x * scale) * .5f,
                (Screen.height - settings.referenceResolution.y * scale) * .5f);
            GUI.matrix = Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one * scale);
            if (menu.Projection > 0f && Camera.main)
            {
                Vector3 screen = Camera.main.WorldToScreenPoint(menu.WristPosition);
                Vector2 wrist = (new Vector2(screen.x, Screen.height - screen.y) - origin) / scale;
                Rect full = settings.panelRect;
                float width = Mathf.Lerp(3f, full.width, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.25f, 1f, p)));
                float height = Mathf.Lerp(3f, full.height, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p / .8f)));
                Vector2 anchor = Vector2.Lerp(wrist, new Vector2(full.center.x, full.yMax), p);
                var rect = new Rect(Mathf.Round(anchor.x - width * .5f), Mathf.Round(anchor.y - height), Mathf.Round(width), Mathf.Round(height));
                Color cyan = settings.cyan;
                float flicker = 1f - settings.flicker + settings.flicker * Mathf.Sin(Time.unscaledTime * 17f);
                opacity = flicker;
                Vector2 contact = new Vector2(rect.xMin, Mathf.Clamp(wrist.y - 24f, rect.yMin, rect.yMax));
                Vector2 elbow = new Vector2(wrist.x + 20f, contact.y);
                Line(wrist, elbow, Fade(cyan, .45f));
                Line(elbow, contact, Fade(cyan, .45f));
                Fill(new Rect(wrist.x - 2, wrist.y - 2, 4, 4), cyan);
                Panel(rect, Fade(settings.panel), Fade(cyan, .85f), settings.corner);
                Scanlines(rect);
                if (p > .7f)
                {
                    opacity *= Mathf.InverseLerp(.7f, 1f, p);
                    GUI.BeginGroup(rect);
                    Header(full.width);
                    if (menu.Tab == 0) Upgrades(); else Inventory();
                    Footer(full.height);
                    GUI.EndGroup();
                }
            }
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
            GUI.depth = oldDepth;
        }

        private Color Fade(Color color, float alpha = 1f) => new Color(color.r, color.g, color.b, color.a * opacity * alpha);

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        // Segmentos escalonados de un píxel, incluidas las diagonales.
        private static void Line(Vector2 from, Vector2 to, Color color)
        {
            if (Mathf.Approximately(from.y, to.y))
            {
                Fill(new Rect(Mathf.Round(Mathf.Min(from.x, to.x)), Mathf.Round(from.y), Mathf.Round(Mathf.Abs(to.x - from.x)) + 1, 1), color);
                return;
            }
            if (Mathf.Approximately(from.x, to.x))
            {
                Fill(new Rect(Mathf.Round(from.x), Mathf.Round(Mathf.Min(from.y, to.y)), 1, Mathf.Round(Mathf.Abs(to.y - from.y)) + 1), color);
                return;
            }
            int count = Mathf.CeilToInt(Mathf.Max(Mathf.Abs(to.x - from.x), Mathf.Abs(to.y - from.y)));
            for (int i = 0; i <= count; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, count == 0 ? 0 : (float)i / count);
                Fill(new Rect(Mathf.Round(point.x), Mathf.Round(point.y), 1, 1), color);
            }
        }

        private static void Panel(Rect rect, Color fill, Color border, int corner)
        {
            int cut = Mathf.Max(0, Mathf.Min(corner, Mathf.FloorToInt(Mathf.Min(rect.width, rect.height) * .5f)));
            Fill(new Rect(rect.x, rect.y + cut, rect.width, rect.height - 2 * cut), fill);
            for (int row = 0; row < cut; row++)
            {
                Fill(new Rect(rect.x + cut - row, rect.y + row, rect.width - 2 * (cut - row), 1), fill);
                Fill(new Rect(rect.x + cut - row, rect.yMax - row - 1, rect.width - 2 * (cut - row), 1), fill);
            }
            Vector2[] corners = {
                new Vector2(rect.x + cut, rect.y), new Vector2(rect.xMax - cut, rect.y),
                new Vector2(rect.xMax, rect.y + cut), new Vector2(rect.xMax, rect.yMax - cut),
                new Vector2(rect.xMax - cut, rect.yMax), new Vector2(rect.x + cut, rect.yMax),
                new Vector2(rect.x, rect.yMax - cut), new Vector2(rect.x, rect.y + cut)
            };
            for (int i = 0; i < corners.Length; i++) Line(corners[i], corners[(i + 1) % corners.Length], border);
        }

        private void Scanlines(Rect rect)
        {
            var settings = menu.Settings;
            int spacing = Mathf.Max(1, settings.scanlineSpacing);
            int offset = (int)(Time.unscaledTime * settings.scanlineSpeed) % spacing;
            int cut = Mathf.Min(settings.corner, Mathf.FloorToInt(rect.height * .5f));
            for (int row = offset; row < rect.height; row += spacing)
            {
                float inset = Mathf.Max(1f, cut - Mathf.Min(row, rect.height - row));
                Fill(new Rect(rect.x + inset, rect.y + row, Mathf.Max(0f, rect.width - inset * 2f), 1), Fade(settings.cyan, .07f));
            }
        }

        private void Label(string text, float x, float y, Color color)
        {
            GUI.color = Color.white;
            PixelHudFont.Draw(text, Mathf.Round(x), Mathf.Round(y), 1, Fade(color));
        }

        private void Icon(Texture2D icon, Rect rect, Color color)
        {
            if (!icon) return;
            icon.filterMode = FilterMode.Point;
            GUI.color = Fade(color);
            GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit);
        }

        private void Header(float width)
        {
            var settings = menu.Settings;
            Label("BRAZALETE DE KAEL", 20, 18, Text);
            Icon(shard, new Rect(width - 107, 13, 16, 16), settings.cyan);
            Label(menu.Progress.Fragments.ToString(), width - 83, 18, settings.cyan);
            Tab("MEJORAS", 20, 0);
            Tab("INVENTARIO", 160, 1);
            Line(new Vector2(20, 78), new Vector2(width - 20, 78), Fade(settings.cyan, .4f));
        }

        private void Tab(string title, float x, int index)
        {
            Color color = menu.Tab == index ? menu.Settings.orange : menu.Settings.cyan;
            Panel(new Rect(x, 43, 128, 25), Fade(menu.Settings.cyan, .05f), Fade(color), 4);
            Label(title, x + 14, 52, color);
        }

        private void Upgrades()
        {
            var settings = menu.Settings;
            if (settings.upgrades.Length == 0) { Label("SIN MEJORAS CONFIGURADAS", 22, 110, settings.locked); return; }
            for (int i = 0; i < settings.upgrades.Length; i++)
            {
                var node = settings.upgrades[i];
                if (node.prerequisite < 0 || node.prerequisite >= settings.upgrades.Length) continue;
                Vector2 a = settings.upgrades[node.prerequisite].position;
                Vector2 b = node.position;
                Vector2 mid = new Vector2((a.x + b.x) * .5f, a.y);
                Color color = menu.Progress.Available(i) ? settings.cyan : settings.locked;
                Line(a, mid, Fade(color, .5f));
                Vector2 bend = new Vector2(mid.x, b.y);
                Line(mid, bend, Fade(color, .5f)); Line(bend, b, Fade(color, .5f));
            }
            for (int i = 0; i < settings.upgrades.Length; i++)
            {
                var node = settings.upgrades[i];
                bool owned = menu.Progress.Owns(node.id);
                bool available = menu.Progress.Available(i);
                Color color = owned ? settings.orange : available ? settings.cyan : settings.locked;
                float pulse = owned || !available ? 1f : .72f + .28f * Mathf.Sin(Time.unscaledTime * 4f);
                Rect box = new Rect(node.position.x - 21, node.position.y - 21, 42, 42);
                Panel(box, Fade(settings.panel), Fade(color, pulse), 6);
                Icon(node.icon ? node.icon : star, new Rect(box.x + 11, box.y + 11, 20, 20), color);
                Label(node.title, box.x - 16, box.yMax + 8, color);
                Label(owned ? "INSTALADA" : node.cost + " FRAG.", box.x - 8, box.yMax + 21, color);
                if (!owned && node.coreCost > 0) Label("+" + node.coreCost + " NUCLEO", box.x - 8, box.yMax + 34, color);
                if (i == menu.Selection) Cursor(box);
            }
            var selected = settings.upgrades[Mathf.Clamp(menu.Selection, 0, settings.upgrades.Length - 1)];
            Panel(new Rect(317, 98, 211, 205), Fade(settings.cyan, .025f), Fade(settings.cyan, .25f), 5);
            Icon(selected.icon ? selected.icon : star, new Rect(334, 113, 32, 32), settings.orange);
            Label(selected.title, 334, 160, Text);
            Wrapped(selected.description, new Rect(334, 181, 176, 80), Text);
            string status = menu.Progress.Owns(selected.id) ? "COMPRADA" : menu.Progress.Available(menu.Selection) ? "DISPONIBLE" : "BLOQUEADA";
            Label(status, 334, 265, menu.Progress.Owns(selected.id) ? settings.orange : settings.cyan);
            Label("COSTO " + selected.cost + (selected.coreCost > 0 ? " + " + selected.coreCost + " NUCLEO" : ""), 334, 283, settings.cyan);
            if (!string.IsNullOrEmpty(menu.Message)) Label(menu.Message, 22, 320, settings.orange);
        }

        private void Inventory()
        {
            var settings = menu.Settings;
            BuildEntries();
            for (int i = 0; i < 12; i++)
            {
                var box = new Rect(24 + i % 4 * 61, 107 + i / 4 * 61, 51, 51);
                Panel(box, Fade(settings.cyan, .035f), Fade(settings.cyan, .35f), 5);
                Item(i, out Texture2D icon, out _, out _);
                Icon(icon, new Rect(box.x + 10, box.y + 10, 30, 30), Text);
                if (menu.Selection == i) Cursor(box);
            }
            Panel(new Rect(295, 98, 233, 205), Fade(settings.cyan, .025f), Fade(settings.cyan, .25f), 5);
            Item(menu.Selection, out Texture2D selected, out string title, out string description);
            Icon(selected, new Rect(385, 112, 48, 48), settings.orange);
            Wrapped(title, new Rect(312, 178, 199, 30), settings.orange);
            Wrapped(description, new Rect(312, 215, 195, 77), Text);
            if (!string.IsNullOrEmpty(menu.Message)) Label(menu.Message, 22, 320, settings.orange);
        }

        private void Add(Texture2D icon, string title, string description)
            => entries.Add(new Entry { icon = icon, title = title, description = description });

        private Texture2D UpgradeIcon(BraceletUpgradeEffect effect)
        {
            foreach (var upgrade in menu.Settings.upgrades)
                if (upgrade.effect == effect && upgrade.icon) return upgrade.icon;
            return star;
        }

        // El inventario se arma con el estado real de Kael: armas, vida, fragmentos, llave y mejoras instaladas.
        private void BuildEntries()
        {
            entries.Clear();
            var settings = menu.Settings;
            if (menu.TryGetComponent(out KaelSpear spear))
                Add(UpgradeIcon(BraceletUpgradeEffect.SpearDamage), "LANZA DE KAEL",
                    "Arma cuerpo a cuerpo. Hacia abajo en el aire rebota sobre espinas. Dano por golpe: " + spear.Damage + ".");
            if (menu.TryGetComponent(out KaelShooter shooter))
                Add(UpgradeIcon(BraceletUpgradeEffect.ArrowDamage), "ARCO",
                    "Dispara flechas en recto y en diagonal. Dano por flecha: " + shooter.Damage + ".");
            if (menu.TryGetComponent(out KaelHealth health))
                Add(heart, "CORAZONES", "Vida actual: " + health.Remaining + " de " + health.Max + ".");
            string level = LevelHUD.LevelShardTotal > 0
                ? " En este nivel: " + LevelHUD.LevelShards + " de " + LevelHUD.LevelShardTotal + "." : "";
            Add(shard, "FRAGMENTOS DE ECO X" + menu.Progress.Fragments, "Se gastan en la pestana de mejoras." + level);
            if (LabKey.Has)
                Add(key, "LLAVE DE LA BASE", "Obtenida en el Nivel 1. Abre la barrera de la base en el Templo de Nemesis.");
            var progress = menu.Progress;
            fruitIndex = -1;
            if (progress.Fruits > 0)
            {
                fruitIndex = entries.Count;
                Add(fruit, "FRUTA DE LA SELVA X" + progress.Fruits,
                    "Consumible. Pulsa A para comer una y recuperar 1 corazon. Puedes llevar " + BraceletProgress.MaxFruits + ".");
            }
            if (progress.Cores > 0)
                Add(core, "NUCLEO DE CENTINELA X" + progress.Cores,
                    "Lo sueltan los centinelas pesados y el Rex. Se gasta en las mejoras de nivel II.");
            for (int i = 0; i < progress.ChipCount; i++) Add(chip, "CHIP " + progress.ChipTitle(i), progress.ChipText(i));
            if (settings.items != null)
                foreach (var item in settings.items) Add(item.icon, item.title, item.description);
        }

        private void Item(int index, out Texture2D icon, out string title, out string description)
        {
            icon = null; title = "CASILLA VACIA"; description = "";
            if (index < 0 || index >= entries.Count) return;
            icon = entries[index].icon; title = entries[index].title; description = entries[index].description;
        }

        private void Cursor(Rect box)
        {
            var settings = menu.Settings;
            float time = Mathf.Clamp01((Time.unscaledTime - menu.CursorTime) / settings.cursorSeconds);
            float bounce = Mathf.Round(Mathf.Sin(time * Mathf.PI) * settings.cursorBounce);
            box.xMin -= 3 + bounce; box.xMax += 3 + bounce;
            box.yMin -= 3 + bounce; box.yMax += 3 + bounce;
            Panel(box, Color.clear, Fade(settings.orange), 7);
            if (time < 1f) Panel(new Rect(box.x - 2, box.y - 2, box.width + 4, box.height + 4), Color.clear, Fade(Color.white, 1f - time), 7);
        }

        private void Wrapped(string text, Rect rect, Color color)
        {
            string line = "";
            float y = rect.y;
            foreach (string word in PixelHudFont.Clean(text).Split(' '))
            {
                string candidate = line.Length == 0 ? word : line + " " + word;
                if (PixelHudFont.Measure(candidate, 1) > rect.width && line.Length > 0)
                {
                    Label(line, rect.x, y, color);
                    y += 13;
                    if (y + 8 > rect.yMax) return;
                    line = word;
                }
                else line = candidate;
            }
            if (line.Length > 0) Label(line, rect.x, y, color);
        }

        private void Footer(float height)
        {
            float y = height - 29;
            Line(new Vector2(20, y - 12), new Vector2(530, y - 12), Fade(menu.Settings.cyan, .4f));
            Panel(new Rect(20, y - 3, 17, 17), Color.clear, Fade(new Color(.4f, 1f, .5f)), 5);
            Label("A", 26, y + 2, new Color(.4f, 1f, .5f));
            Label("SELECCIONAR", 44, y + 2, Text);
            Panel(new Rect(134, y - 3, 17, 17), Color.clear, Fade(new Color(1f, .4f, .35f)), 5);
            Label("B", 140, y + 2, new Color(1f, .4f, .35f));
            Label("CERRAR", 158, y + 2, Text);
            Panel(new Rect(227, y - 3, 47, 17), Color.clear, Fade(menu.Settings.cyan), 3);
            Label("LB/RB", 232, y + 2, Text);
            Label("PESTANA", 281, y + 2, Text);
            Label("TAB  ENTER  ESC  Q/E", 381, y + 2, menu.Settings.cyan);
        }
    }
}
