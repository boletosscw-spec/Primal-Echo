using System;
using UnityEngine;

namespace PrimalEcho
{
    // Sonidos generados por código (respaldo mientras no exista el archivo en Resources/Sonidos).
    // Cada receta mezcla ruido filtrado, osciladores con barrido de tono y envolventes cortas,
    // al estilo de los efectos de juegos 2D. Se generan una sola vez y se guardan en memoria.
    public static class Sintesis
    {
        private const int SR = 22050;
        private const float Tau = Mathf.PI * 2f;
        private static System.Random rng = new System.Random(1234);

        public static AudioClip Crear(string nombre)
        {
            rng = new System.Random(nombre.Length * 7919 + nombre[0] * 31);
            switch (nombre)
            {
                // ---------- Kael ----------
                case "kael_paso": return Paso(nombre, 0.07f, 700f, 0.5f);
                case "kael_salto": return Barrido(nombre, 0.13f, 500f, 1500f, 0.7f, 260f, 520f);
                case "kael_doble_salto": return DobleSalto(nombre);
                case "kael_dash": return Silbido(nombre, 0.24f, 2600f, 600f, 0.03f);
                case "kael_aterrizaje": return Golpe(nombre, 0.18f, 85f, 45f, 900f, 0.8f);
                case "kael_lanza": return Silbido(nombre, 0.12f, 3800f, 1400f, 0.01f);
                case "kael_golpe": return Impacto(nombre, 0.16f, 170f, 60f, 0.55f);
                case "kael_arco": return Arco(nombre);
                case "flecha_impacto": return Golpe(nombre, 0.09f, 240f, 110f, 2500f, 0.5f);
                case "kael_dano": return Dano(nombre);
                case "kael_muerte": return Muerte(nombre);
                case "kael_rebote": return Boing(nombre, 0.14f, 320f, 760f);
                case "hongo": return Boing(nombre, 0.26f, 190f, 620f);

                // ---------- Kiro ----------
                case "kiro_destello": return Destello(nombre);
                case "kiro_pulso": return PulsoKiro(nombre);
                case "kiro_pitido": return Pitido(nombre, 0.07f, 1300f, 1900f);
                case "kiro_alerta": return DosPitidos(nombre);

                // ---------- Atlas ----------
                case "atlas_rugido": return Rugido(nombre, 1.5f, 78f);
                case "atlas_rascar": return Rascar(nombre);
                case "atlas_paso": return Golpe(nombre, 0.26f, 60f, 38f, 380f, 0.9f);
                case "atlas_choque": return Choque(nombre, 0.9f, 52f);
                case "atlas_aturdido": return Aturdido(nombre);
                case "atlas_pisoton": return Golpe(nombre, 0.55f, 48f, 30f, 520f, 1f);
                case "atlas_escudo_bloqueo": return Campana(nombre, 0.35f, new[] { 1850f, 2790f, 4210f }, 0.08f);
                case "atlas_lanzar": return Silbido(nombre, 0.32f, 900f, 300f, 0.06f);
                case "atlas_anillo": return Zap(nombre);
                case "atlas_escudo_roto": return Cristales(nombre);
                case "atlas_muerte": return MuerteAtlas(nombre);

                // ---------- Aeris ----------
                case "aeris_chillido": return Chillido(nombre);
                case "aeris_aleteo": return Aleteo(nombre);
                case "aeris_apuntar": return Barrido(nombre, 0.6f, 600f, 2400f, 0.15f, 700f, 1500f);
                case "aeris_picada": return Silbido(nombre, 0.45f, 2600f, 700f, 0.05f);
                case "aeris_clavado": return Choque(nombre, 0.5f, 70f);
                case "aeris_huevo": return Boing(nombre, 0.12f, 500f, 260f);
                case "aeris_explosion": return Golpe(nombre, 0.7f, 70f, 32f, 1800f, 1.2f);
                case "aeris_plumas": return Silbido(nombre, 0.18f, 4200f, 1800f, 0.01f);
                case "aeris_viento": return Silbido(nombre, 2.2f, 700f, 450f, 0.25f);
                case "aeris_rayo": return Zap(nombre);
                case "aeris_muerte": return MuerteAtlas(nombre);

                // ---------- Compuertas de la base ----------
                case "compuerta_retumbo": return Sirena(nombre);
                case "compuerta_caida": return Silbido(nombre, 0.3f, 1500f, 400f, 0.03f);
                case "compuerta_impacto": return Choque(nombre, 1.0f, 58f);
                case "compuerta_grieta": return Grieta(nombre);
                case "compuerta_romper": return Derrumbe(nombre, 0.9f, 2400f);

                // ---------- Rocas y estalactitas ----------
                case "roca_impacto": return Golpe(nombre, 0.17f, 95f, 55f, 1300f, 0.7f);
                case "roca_romper": return Derrumbe(nombre, 0.32f, 1600f);
                case "estalactita_retumbo": return Retumbo(nombre, 0.75f);
                case "estalactita_caida": return Silbido(nombre, 0.36f, 1100f, 280f, 0.05f);
                case "estalactita_impacto": return Choque(nombre, 1.1f, 42f);
                case "estalactita_grieta": return Grieta(nombre);
                case "estalactita_romper": return Derrumbe(nombre, 0.9f, 1100f);

                // ---------- Objetos ----------
                case "eco_carga": return Arpegio(nombre, new[] { 880f, 1320f, 1760f, 2640f }, 0.05f, 0.5f);
                case "objeto_recoger": return Arpegio(nombre, new[] { 660f, 990f }, 0.07f, 0.35f);
                case "llave_recoger": return Arpegio(nombre, new[] { 523f, 784f, 1047f, 1568f }, 0.08f, 0.8f);
                case "checkpoint": return Acorde(nombre, new[] { 523f, 659f, 784f, 1047f }, 1.2f);
                case "gota": return Gota(nombre);
            }

            // Voces sintetizadas: voz_<personaje>_silaba_<n>
            if (nombre.StartsWith("voz_") && nombre.Contains("_silaba_"))
            {
                string quien = nombre.Substring(4, nombre.IndexOf("_silaba_") - 4);
                int n = nombre[nombre.Length - 1] - '0';
                return Silaba(nombre, quien, n);
            }
            return null;
        }

        // ================= Herramientas =================

        private sealed class Lp { private float y; public float F(float x, float fc) { float a = 1f - Mathf.Exp(-Tau * fc / SR); y += a * (x - y); return y; } }
        private sealed class Hp { private float y; public float F(float x, float fc) { float a = 1f - Mathf.Exp(-Tau * fc / SR); y += a * (x - y); return x - y; } }
        private sealed class Osc
        {
            private float ph;
            public float Sin(float f) { ph += f / SR; ph -= Mathf.Floor(ph); return Mathf.Sin(Tau * ph); }
            public float Cuadrada(float f) { return (float)Math.Tanh(Sin(f) * 4f); }
            public float Sierra(float f) { ph += f / SR; ph -= Mathf.Floor(ph); return 2f * ph - 1f; }
            public float Triangular(float f) { ph += f / SR; ph -= Mathf.Floor(ph); return 4f * Mathf.Abs(ph - 0.5f) - 1f; }
        }
        private static float R() => (float)(rng.NextDouble() * 2.0 - 1.0);
        private static float Env(float t, float ataque, float caida) => t < ataque ? t / ataque : Mathf.Exp(-(t - ataque) / caida);
        private static float Lerp(float a, float b, float t) => a + (b - a) * Mathf.Clamp01(t);
        private static float Exp(float a, float b, float t) => a * Mathf.Pow(b / a, Mathf.Clamp01(t));

        private static AudioClip Clip(string nombre, float dur, Func<float, float, float> f)
        {
            int n = Mathf.Max(1, (int)(dur * SR));
            var d = new float[n];
            float pico = 0.0001f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SR;
                d[i] = f(t, t / dur);
                pico = Mathf.Max(pico, Mathf.Abs(d[i]));
            }
            float g = 0.85f / pico;
            int fade = Mathf.Min(n, (int)(0.006f * SR));
            for (int i = 0; i < n; i++)
            {
                d[i] *= g;
                if (i < 40) d[i] *= i / 40f;                     // sin chasquido al inicio
                if (i > n - fade) d[i] *= (n - i) / (float)fade; // ni al final
            }
            var clip = AudioClip.Create(nombre, n, 1, SR, false);
            clip.SetData(d, 0);
            return clip;
        }

        // ================= Recetas =================

        private static AudioClip Paso(string n, float dur, float corte, float cuerpo)
        {
            var lp = new Lp(); var o = new Osc();
            return Clip(n, dur, (t, p) => lp.F(R(), corte) * Env(t, 0.002f, 0.02f) + cuerpo * o.Sin(Exp(140f, 70f, p)) * Env(t, 0.002f, 0.025f));
        }

        private static AudioClip Barrido(string n, float dur, float f0, float f1, float ruido, float s0, float s1)
        {
            var lp = new Lp(); var hp = new Hp(); var o = new Osc();
            return Clip(n, dur, (t, p) =>
            {
                float fc = Exp(f0, f1, p);
                float aire = hp.F(lp.F(R(), fc * 1.5f), fc * 0.5f) * ruido;
                return (aire + 0.35f * o.Sin(Exp(s0, s1, p))) * Env(t, 0.01f, dur * 0.35f);
            });
        }

        private static AudioClip DobleSalto(string n)
        {
            var lp = new Lp(); var hp = new Hp(); var a = new Osc(); var b = new Osc();
            return Clip(n, 0.2f, (t, p) =>
            {
                float aire = hp.F(lp.F(R(), Exp(900f, 3000f, p)), 500f) * 0.6f;
                float brillo = 0.3f * a.Sin(Exp(900f, 1900f, p)) + 0.18f * b.Sin(Exp(1350f, 2850f, p));
                return (aire + brillo) * Env(t, 0.008f, 0.06f);
            });
        }

        private static AudioClip Silbido(string n, float dur, float f0, float f1, float ataque)
        {
            var lp = new Lp(); var hp = new Hp(); var lp2 = new Lp();
            return Clip(n, dur, (t, p) =>
            {
                float fc = Exp(f0, f1, p);
                float x = hp.F(lp2.F(lp.F(R(), fc * 1.6f), fc * 1.6f), fc * 0.4f);
                float env = t < ataque ? t / ataque : Mathf.Pow(1f - (t - ataque) / (dur - ataque), 1.6f);
                return x * env;
            });
        }

        private static AudioClip Golpe(string n, float dur, float f0, float f1, float corteRuido, float ruido)
        {
            var lp = new Lp(); var o = new Osc();
            return Clip(n, dur, (t, p) =>
            {
                float cuerpo = o.Sin(Exp(f0, f1, p)) * Env(t, 0.002f, dur * 0.35f);
                float polvo = lp.F(R(), Exp(corteRuido, corteRuido * 0.3f, p)) * Env(t, 0.001f, dur * 0.25f) * ruido * 2f;
                return (float)Math.Tanh((cuerpo + polvo) * 1.4f);
            });
        }

        private static AudioClip Impacto(string n, float dur, float f0, float f1, float ruido)
        {
            var hp = new Hp(); var o = new Osc();
            return Clip(n, dur, (t, p) =>
            {
                float chasquido = hp.F(R(), 1500f) * Env(t, 0.0005f, 0.012f) * ruido * 2f;
                float cuerpo = o.Sin(Exp(f0, f1, p)) * Env(t, 0.001f, 0.05f);
                return (float)Math.Tanh((chasquido + cuerpo) * 2f);
            });
        }

        private static AudioClip Arco(string n)
        {
            var o = new Osc(); var o2 = new Osc(); var hp = new Hp();
            return Clip(n, 0.3f, (t, p) =>
            {
                float vib = 1f + 0.015f * Mathf.Sin(Tau * 28f * t);
                float cuerda = (0.6f * o.Triangular(196f * vib) + 0.3f * o2.Sin(392f * vib)) * Env(t, 0.002f, 0.09f);
                float aire = hp.F(R(), 2500f) * Env(t, 0.001f, 0.03f) * 0.7f;
                return cuerda + aire;
            });
        }

        private static AudioClip Dano(string n)
        {
            var o = new Osc(); var lp = new Lp();
            return Clip(n, 0.22f, (t, p) =>
            {
                float crujido = o.Cuadrada(Exp(320f, 140f, p)) * Env(t, 0.002f, 0.07f) * 0.6f;
                float golpe = lp.F(R(), 1800f) * Env(t, 0.001f, 0.04f);
                return (float)Math.Tanh((crujido + golpe) * 1.6f);
            });
        }

        private static AudioClip Muerte(string n)
        {
            var o = new Osc(); var lp = new Lp();
            return Clip(n, 0.9f, (t, p) =>
                o.Cuadrada(Exp(420f, 70f, p) * (1f + 0.03f * Mathf.Sin(Tau * 9f * t))) * (1f - p) * 0.5f
                + lp.F(R(), 600f) * Env(t, 0.01f, 0.25f) * 0.6f);
        }

        private static AudioClip Boing(string n, float dur, float f0, float f1)
        {
            var o = new Osc();
            return Clip(n, dur, (t, p) =>
            {
                float f = p < 0.35f ? Exp(f0, f1, p / 0.35f) : Exp(f1, f1 * 0.75f, (p - 0.35f) / 0.65f);
                f *= 1f + 0.05f * Mathf.Sin(Tau * 18f * t);
                return o.Sin(f) * Env(t, 0.004f, dur * 0.45f);
            });
        }

        private static AudioClip Destello(string n)
        {
            var a = new Osc(); var b = new Osc(); var c = new Osc(); var hp = new Hp();
            return Clip(n, 0.75f, (t, p) =>
            {
                float sube = Exp(1.0f, 1.5f, Mathf.Min(1f, p * 3f));
                float brillo = 0.4f * a.Sin(1175f * sube) + 0.3f * b.Sin(1760f * sube) + 0.2f * c.Sin(2637f * sube);
                brillo *= 0.7f + 0.3f * Mathf.Sin(Tau * 14f * t); // titila
                float whoosh = hp.F(R(), 3000f) * Env(t, 0.005f, 0.12f) * 0.8f;
                return (brillo + whoosh) * Env(t, 0.004f, 0.25f);
            });
        }

        private static AudioClip PulsoKiro(string n)
        {
            var a = new Osc(); var b = new Osc(); var lp = new Lp();
            return Clip(n, 0.6f, (t, p) =>
            {
                float grave = a.Sin(Exp(160f, 50f, p)) * Env(t, 0.003f, 0.18f);
                float anillo = 0.35f * b.Sin(620f + 80f * Mathf.Sin(Tau * 6f * t)) * Env(t, 0.01f, 0.2f);
                float aire = lp.F(R(), 1400f) * Env(t, 0.001f, 0.08f) * 0.7f;
                return (float)Math.Tanh((grave + anillo + aire) * 1.5f);
            });
        }

        private static AudioClip Pitido(string n, float dur, float f0, float f1)
        {
            var o = new Osc();
            return Clip(n, dur, (t, p) => o.Cuadrada(Exp(f0, f1, p)) * 0.5f * Env(t, 0.003f, dur * 0.5f));
        }

        private static AudioClip DosPitidos(string n)
        {
            var o = new Osc();
            return Clip(n, 0.22f, (t, p) =>
            {
                if (t < 0.08f) return o.Cuadrada(1500f) * 0.5f * Env(t, 0.003f, 0.04f);
                if (t > 0.11f) return o.Cuadrada(2100f) * 0.5f * Env(t - 0.11f, 0.003f, 0.05f);
                return 0f;
            });
        }

        private static AudioClip Rugido(string n, float dur, float f0)
        {
            var o = new Osc(); var o2 = new Osc(); var lp = new Lp(); var lp2 = new Lp(); var metal = new Osc();
            return Clip(n, dur, (t, p) =>
            {
                float env = p < 0.25f ? p / 0.25f : Mathf.Pow(1f - (p - 0.25f) / 0.75f, 1.3f);
                float f = f0 * (1f + 0.25f * Mathf.Sin(Mathf.PI * p)) * (1f + 0.02f * R());
                float am = 0.65f + 0.35f * Mathf.Sin(Tau * 24f * t);
                float voz = (o.Sierra(f) + 0.6f * o2.Sierra(f * 1.505f)) * am;
                voz = lp2.F(voz, 900f);
                float aliento = lp.F(R(), 700f) * 0.9f;
                float chirrido = 0.18f * metal.Sin(310f + 40f * Mathf.Sin(Tau * 3f * t));
                return (float)Math.Tanh((voz + aliento + chirrido) * env * 1.8f);
            });
        }

        private static AudioClip Chillido(string n)
        {
            var o = new Osc(); var o2 = new Osc(); var hp = new Hp();
            return Clip(n, 1.1f, (t, p) =>
            {
                float env = p < 0.15f ? p / 0.15f : Mathf.Pow(1f - (p - 0.15f) / 0.85f, 1.2f);
                float f = 520f * (1f + 0.35f * Mathf.Sin(Mathf.PI * p)) * (1f + 0.04f * Mathf.Sin(Tau * 31f * t));
                float voz = o.Sierra(f) * 0.6f + o2.Cuadrada(f * 1.5f) * 0.25f;
                float aire = hp.F(R(), 2500f) * 0.35f;
                return (float)Math.Tanh((voz + aire) * env * 1.6f);
            });
        }

        private static AudioClip Aleteo(string n)
        {
            var lp = new Lp();
            return Clip(n, 0.6f, (t, p) =>
            {
                float golpe = Mathf.Repeat(t, 0.2f);
                float env = golpe < 0.12f ? Mathf.Sin(Mathf.PI * golpe / 0.12f) : 0f;
                return lp.F(R(), 900f) * env * 2f;
            });
        }

        private static AudioClip Sirena(string n)
        {
            var o = new Osc();
            return Clip(n, 0.7f, (t, p) => o.Cuadrada(Mathf.Repeat(t, 0.35f) < 0.175f ? 760f : 980f) * 0.45f * (1f - p * 0.3f));
        }

        private static AudioClip Rascar(string n)
        {
            var lp = new Lp(); var hp = new Hp();
            return Clip(n, 0.85f, (t, p) =>
            {
                float golpe = Mathf.Repeat(t, 0.28f);
                float raspado = hp.F(lp.F(R(), 2200f), 500f) * (golpe < 0.2f ? Mathf.Sin(Mathf.PI * golpe / 0.2f) : 0f);
                return raspado * (1f - 0.4f * p);
            });
        }

        private static AudioClip Choque(string n, float dur, float f0)
        {
            var o = new Osc(); var lp = new Lp(); var m1 = new Osc(); var m2 = new Osc();
            return Clip(n, dur, (t, p) =>
            {
                float bum = o.Sin(Exp(f0 * 1.6f, f0 * 0.7f, p)) * Env(t, 0.002f, dur * 0.3f);
                float escombro = lp.F(R(), Exp(2500f, 300f, p)) * Env(t, 0.001f, dur * 0.28f) * 1.6f;
                float metal = (0.25f * m1.Sin(423f) + 0.18f * m2.Sin(697f)) * Env(t, 0.001f, dur * 0.22f);
                return (float)Math.Tanh((bum + escombro + metal) * 1.5f);
            });
        }

        private static AudioClip Aturdido(string n)
        {
            var o = new Osc(); var pajaro = new Osc();
            return Clip(n, 0.9f, (t, p) =>
            {
                float mareo = o.Sin(950f + 180f * Mathf.Sin(Tau * 5f * t)) * 0.35f * (1f - p);
                float fase = Mathf.Repeat(t, 0.22f);
                float pio = fase < 0.06f ? pajaro.Sin(Exp(2200f, 3200f, fase / 0.06f)) * 0.3f * Mathf.Sin(Mathf.PI * fase / 0.06f) : 0f;
                return mareo + pio;
            });
        }

        private static AudioClip Campana(string n, float dur, float[] fs, float caida)
        {
            var os = new Osc[fs.Length]; for (int i = 0; i < os.Length; i++) os[i] = new Osc();
            var hp = new Hp();
            return Clip(n, dur, (t, p) =>
            {
                float s = hp.F(R(), 4000f) * Env(t, 0.0005f, 0.006f);
                for (int i = 0; i < fs.Length; i++) s += os[i].Sin(fs[i]) * Env(t, 0.001f, caida * (1f + i * 0.4f)) / (1 + i);
                return s;
            });
        }

        private static AudioClip Zap(string n)
        {
            var o = new Osc(); var fm = new Osc();
            return Clip(n, 0.38f, (t, p) =>
            {
                float mod = fm.Sin(Exp(60f, 220f, p)) * 300f;
                return o.Sierra(Exp(380f, 1250f, p) + mod) * 0.45f * Env(t, 0.01f, 0.15f);
            });
        }

        private static AudioClip Cristales(string n)
        {
            var hp = new Hp(); var granos = new Osc[10]; var fq = new float[10]; var t0 = new float[10];
            for (int i = 0; i < 10; i++) { granos[i] = new Osc(); fq[i] = 2000f + (float)rng.NextDouble() * 4000f; t0[i] = (float)rng.NextDouble() * 0.35f; }
            return Clip(n, 0.85f, (t, p) =>
            {
                float s = hp.F(R(), 2500f) * Env(t, 0.001f, 0.12f) * 0.8f;
                for (int i = 0; i < 10; i++) if (t > t0[i]) s += granos[i].Sin(fq[i]) * Env(t - t0[i], 0.0005f, 0.05f) * 0.25f;
                return s;
            });
        }

        private static AudioClip MuerteAtlas(string n)
        {
            var o = new Osc(); var o2 = new Osc(); var lp = new Lp(); var lp2 = new Lp();
            return Clip(n, 2.2f, (t, p) =>
            {
                float f = Exp(120f, 28f, p);
                float motor = lp2.F(o.Sierra(f) + 0.5f * o2.Cuadrada(f * 0.5f), 700f) * (1f - p);
                float vapor = lp.F(R(), Exp(3000f, 400f, p)) * Env(t, 0.05f, 0.6f) * 0.6f;
                return (float)Math.Tanh((motor + vapor) * 1.6f);
            });
        }

        private static AudioClip Derrumbe(string n, float dur, float corte)
        {
            var lp = new Lp(); var o = new Osc();
            var t0 = new float[7]; for (int i = 0; i < 7; i++) t0[i] = (float)rng.NextDouble() * dur * 0.6f;
            return Clip(n, dur, (t, p) =>
            {
                float s = 0f;
                for (int i = 0; i < 7; i++) if (t > t0[i]) s += Env(t - t0[i], 0.001f, 0.05f);
                float ruido = lp.F(R(), corte) * (0.4f + s);
                return (float)Math.Tanh((ruido + o.Sin(70f) * Env(t, 0.002f, 0.12f)) * (1f - p * 0.6f));
            });
        }

        private static AudioClip Retumbo(string n, float dur)
        {
            var lp = new Lp(); var lp2 = new Lp();
            return Clip(n, dur, (t, p) => lp2.F(lp.F(R(), 160f), 160f) * 6f * Mathf.Sin(Mathf.PI * p) * (0.75f + 0.25f * Mathf.Sin(Tau * 11f * t)));
        }

        private static AudioClip Grieta(string n)
        {
            var hp = new Hp();
            var t0 = new float[6]; for (int i = 0; i < 6; i++) t0[i] = (float)rng.NextDouble() * 0.18f;
            return Clip(n, 0.28f, (t, p) =>
            {
                float s = 0f;
                for (int i = 0; i < 6; i++) if (t > t0[i]) s += Env(t - t0[i], 0.0003f, 0.008f);
                return hp.F(R(), 1800f) * (s + Env(t, 0.0005f, 0.02f));
            });
        }

        private static AudioClip Arpegio(string n, float[] notas, float paso, float cola)
        {
            var os = new Osc[notas.Length]; for (int i = 0; i < os.Length; i++) os[i] = new Osc();
            float dur = paso * notas.Length + cola;
            return Clip(n, dur, (t, p) =>
            {
                float s = 0f;
                for (int i = 0; i < notas.Length; i++)
                {
                    float ti = t - i * paso;
                    if (ti > 0f) s += (os[i].Triangular(notas[i]) * 0.6f + 0.3f * Mathf.Sin(Tau * notas[i] * 2f * ti)) * Env(ti, 0.003f, cola * 0.4f);
                }
                return s;
            });
        }

        private static AudioClip Acorde(string n, float[] notas, float dur)
        {
            var os = new Osc[notas.Length]; for (int i = 0; i < os.Length; i++) os[i] = new Osc();
            return Clip(n, dur, (t, p) =>
            {
                float s = 0f;
                for (int i = 0; i < notas.Length; i++)
                {
                    float ti = t - i * 0.06f;
                    if (ti > 0f) s += os[i].Sin(notas[i] * (1f + 0.004f * Mathf.Sin(Tau * 5f * t))) * Env(ti, 0.02f, dur * 0.35f);
                }
                return s;
            });
        }

        private static AudioClip Gota(string n)
        {
            // "plink" y dos ecos suaves
            return Clip(n, 0.5f, (t, p) =>
            {
                float s = 0f;
                float[] retraso = { 0f, 0.13f, 0.27f };
                float[] gan = { 1f, 0.35f, 0.15f };
                for (int k = 0; k < 3; k++)
                {
                    float tk = t - retraso[k];
                    if (tk > 0f && tk < 0.07f) s += Mathf.Sin(Tau * (1100f * tk + 9000f * tk * tk)) * Env(tk, 0.001f, 0.02f) * gan[k];
                }
                return s;
            });
        }

        // Sílaba sintetizada con armónicos con forma de vocal
        private static AudioClip Silaba(string n, string quien, int variante)
        {
            float f0, dur; bool robot = false, grave = false;
            switch (quien)
            {
                case "orin": f0 = 118f; dur = 0.12f; grave = true; break;
                case "kael": f0 = 205f; dur = 0.09f; break;
                case "kiro": f0 = 1100f; dur = 0.06f; robot = true; break;
                case "nemesis": f0 = 92f; dur = 0.14f; robot = true; break;
                default: f0 = 180f; dur = 0.1f; break;
            }
            float[][] vocales = { new[] { 1f, 0.8f, 0.6f, 0.3f }, new[] { 1f, 0.55f, 0.2f, 0.1f }, new[] { 1f, 0.3f, 0.65f, 0.45f }, new[] { 1f, 0.2f, 0.1f, 0.35f } };
            float[] w = vocales[Mathf.Clamp(variante, 0, 3)];
            float[] salto = { 1f, 1.12f, 0.94f, 1.06f };
            f0 *= salto[Mathf.Clamp(variante, 0, 3)];
            var os = new Osc[4]; for (int i = 0; i < 4; i++) os[i] = new Osc();
            var lp = new Lp();
            return Clip(n, dur, (t, p) =>
            {
                float f = f0 * (1f + 0.06f * (1f - p)) * (1f + 0.01f * Mathf.Sin(Tau * 6f * t));
                float s;
                if (robot)
                {
                    s = os[0].Cuadrada(quien == "kiro" ? Exp(f, f * 1.35f, p) : f) * 0.5f;
                    if (quien == "nemesis") s = (s + 0.5f * os[1].Sierra(f * 2.01f)) * (0.7f + 0.3f * Mathf.Sign(Mathf.Sin(Tau * 40f * t)));
                }
                else
                {
                    s = 0f;
                    for (int i = 0; i < 4; i++) s += os[i].Sin(f * (i + 1)) * w[i];
                    if (grave) s += lp.F(R(), 1200f) * 0.25f; // voz rasposa de viejo
                }
                float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(p)) * (p < 0.15f ? p / 0.15f : 1f);
                return s * env;
            });
        }
    }
}
