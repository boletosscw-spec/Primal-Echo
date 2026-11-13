using System.Globalization;
using System.Text;
using UnityEngine;

namespace PrimalEcho
{
    public static class PixelHudFont
    {
        private static Texture2D font;
        private static void Part(Texture2D t, float sx, float sy, float sw, float sh, Rect dst)
        {
            if (dst.width <= 0f || dst.height <= 0f) return;
            GUI.DrawTextureWithTexCoords(dst, t, new Rect(sx / t.width, 1f - (sy + sh) / t.height, sw / t.width, sh / t.height));
        }

        private static readonly int[] Glyphs =
        {
            0,0,0,4,7,0,1,2,14,0,3,4,21,0,5,6,28,0,5,6,35,0,5,6,42,0,5,6,49,0,2,3,
            56,0,3,4,63,0,3,4,70,0,5,6,77,0,5,6,84,0,2,3,91,0,3,4,98,0,2,3,105,0,5,6,
            0,9,5,6,7,9,3,4,14,9,5,6,21,9,5,6,28,9,5,6,35,9,5,6,42,9,5,6,49,9,5,6,
            56,9,5,6,63,9,5,6,70,9,3,4,77,9,2,3,84,9,4,5,91,9,5,6,98,9,4,5,105,9,5,6,
            0,18,5,6,7,18,5,6,14,18,5,6,21,18,5,6,28,18,5,6,35,18,5,6,42,18,5,6,49,18,5,6,
            56,18,5,6,63,18,3,4,70,18,5,6,77,18,5,6,84,18,5,6,91,18,5,6,98,18,5,6,105,18,5,6,
            0,27,5,6,7,27,5,6,14,27,5,6,21,27,5,6,28,27,5,6,35,27,5,6,42,27,5,6,49,27,5,6,
            56,27,5,6,63,27,5,6,70,27,5,6,77,27,3,4,84,27,5,6,91,27,3,4,98,27,5,6,105,27,5,6,
            0,36,2,3,7,36,5,6,14,36,5,6,21,36,5,6,28,36,5,6,35,36,5,6,42,36,4,5,49,36,5,6,
            56,36,5,6,63,36,1,2,70,36,3,4,77,36,4,5,84,36,2,3,91,36,5,6,98,36,5,6,105,36,5,6,
            0,45,5,6,7,45,5,6,14,45,4,5,21,45,5,6,28,45,4,5,35,45,5,6,42,45,5,6,49,45,5,6,
            56,45,5,6,63,45,5,6,70,45,5,6,77,45,4,5,84,45,1,2,91,45,4,5,98,45,5,6
        };

        public static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder();
            foreach (char c in s.Replace('·', '-').Replace('—', '-').Replace('–', '-').Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (c >= 32 && c <= 126) sb.Append(c);
            }
            return sb.ToString();
        }

        public static int Measure(string s, int ui)
        {
            s = Clean(s);
            int w = 0;
            foreach (char c in s) w += Glyphs[(c - 32) * 4 + 3];
            return Mathf.Max(0, w - 1) * ui;
        }

        public static void Draw(string s, float x, float y, int ui, Color color, bool shadow = true)
        {
            if (font == null) font = Resources.Load<Texture2D>("HUD/fuente");
            if (font == null) return;
            font.filterMode = FilterMode.Point;
            s = Clean(s);
            Color old = GUI.color;
            for (int pass = shadow ? 0 : 1; pass < 2; pass++)
            {
                GUI.color = pass == 0 ? new Color(0f, 0f, 0f, 0.8f * old.a) : new Color(color.r, color.g, color.b, color.a * old.a);
                float cx = x + (pass == 0 ? ui : 0), cy = y + (pass == 0 ? ui : 0);
                foreach (char c in s)
                {
                    int i = (c - 32) * 4;
                    int gw = Glyphs[i + 2];
                    if (gw > 0) Part(font, Glyphs[i], Glyphs[i + 1], gw, 8, new Rect(cx, cy, gw * ui, 8 * ui));
                    cx += Glyphs[i + 3] * ui;
                }
            }
            GUI.color = old;
        }

    }
}
