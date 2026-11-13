using System.Collections.Generic;
using UnityEngine;

namespace PrimalEcho
{
    // Texto con el estilo del HUD dentro de un panel holográfico (lo usan los carteles y el tutorial).
    //  · Las PALABRAS EN MAYÚSCULAS salen en naranja.
    //  · [ENTRE CORCHETES] se dibuja como una tecla o botón.
    //  · "·" o un salto de línea parten el renglón.
    public sealed class HoloText
    {
        public static readonly Color Cyan = new Color(.25f, .92f, 1f), Orange = new Color(1f, .6f, .2f),
            Green = new Color(.45f, 1f, .55f), Body = new Color(.85f, 1f, .96f), Fill = new Color(.015f, .1f, .15f, .88f);
        private const int Space = 5, KeyPad = 3;
        private struct Word { public string text; public bool strong, key; public int width; }
        private readonly List<List<Word>> lines = new List<List<Word>>();
        private int textWidth;

        public bool Empty => lines.Count == 0;
        public static int Scale => Mathf.Max(2, Mathf.RoundToInt(Screen.height / 480f));
        public float Width(int ui) => (textWidth + 14) * ui;
        public float Height(int ui) => (lines.Count * 13 + 9) * ui;

        public HoloText(string text, int maxLineWidth = 190)
        {
            foreach (string part in (text ?? "").Replace("\n", "·").Split('·'))
            {
                var line = new List<Word>();
                int width = 0;
                foreach (string raw in PixelHudFont.Clean(part).Split(' '))
                {
                    if (raw.Length == 0) continue;
                    bool key = raw.Length > 2 && raw[0] == '[' && raw[raw.Length - 1] == ']';
                    string shown = key ? raw.Substring(1, raw.Length - 2).Replace('_', ' ') : raw;
                    int w = PixelHudFont.Measure(shown, 1) + (key ? 2 * KeyPad : 0);
                    if (line.Count > 0 && width + Space + w > maxLineWidth) { lines.Add(line); line = new List<Word>(); width = 0; }
                    width += (line.Count > 0 ? Space : 0) + w;
                    line.Add(new Word { text = shown, strong = key || IsStrong(shown), key = key, width = w });
                }
                if (line.Count > 0) lines.Add(line);
            }
            foreach (var line in lines) textWidth = Mathf.Max(textWidth, LineWidth(line));
        }

        private static bool IsStrong(string word)
        {
            int letters = 0;
            foreach (char c in word)
            {
                if (!char.IsLetter(c)) continue;
                if (!char.IsUpper(c)) return false;
                letters++;
            }
            return letters >= 2;
        }

        private static int LineWidth(List<Word> line)
        {
            int width = 0;
            for (int i = 0; i < line.Count; i++) width += (i > 0 ? Space : 0) + line[i].width;
            return width;
        }

        private static void Box(float x, float y, float w, float h, Color color, float a)
        {
            GUI.color = new Color(color.r, color.g, color.b, color.a * a);
            GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        }

        private static void Frame(float x, float y, float w, float h, int t, Color color, float a)
        {
            Box(x, y, w, t, color, a); Box(x, y + h - t, w, t, color, a);
            Box(x, y, t, h, color, a); Box(x + w - t, y, t, h, color, a);
        }

        /// <summary>Dibuja el panel con su esquina superior izquierda en (x, y). Llamar desde OnGUI (Repaint).</summary>
        public void Draw(float x, float y, int ui, float alpha, Color border)
        {
            if (Empty || alpha <= 0.01f) return;
            Color old = GUI.color;
            float a = alpha * (.95f + .05f * Mathf.Sin(Time.unscaledTime * 17f)); // parpadeo leve de holograma
            float w = Width(ui), h = Height(ui);
            x = Mathf.Round(x); y = Mathf.Round(y);
            Box(x, y, w, h, Fill, a);
            Frame(x, y, w, h, ui, border, a);
            int tick = 3 * ui; // esquinas marcadas en naranja
            Box(x, y, tick, ui, Orange, a); Box(x, y, ui, tick, Orange, a);
            Box(x + w - tick, y + h - ui, tick, ui, Orange, a); Box(x + w - ui, y + h - tick, ui, tick, Orange, a);
            for (int i = 0; i < lines.Count; i++)
            {
                float cx = x + 7 * ui + (textWidth - LineWidth(lines[i])) * ui / 2f;
                float cy = y + 7 * ui + i * 13 * ui;
                foreach (var word in lines[i])
                {
                    if (word.key)
                    {
                        Box(cx, cy - 3 * ui, word.width * ui, 13 * ui, new Color(1f, .6f, .2f, .16f), a);
                        Frame(cx, cy - 3 * ui, word.width * ui, 13 * ui, ui, Orange, a);
                    }
                    GUI.color = new Color(1f, 1f, 1f, a);
                    PixelHudFont.Draw(word.text, Mathf.Round(cx + (word.key ? KeyPad * ui : 0)), cy, ui, word.strong ? Orange : Body);
                    cx += (word.width + Space) * ui;
                }
            }
            GUI.color = old;
        }
    }
}
