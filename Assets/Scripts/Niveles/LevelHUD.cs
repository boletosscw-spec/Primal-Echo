using UnityEngine;

namespace PrimalEcho
{
    // HUD con sprites (mezcla de Pixel UI Sci-Fi Free + corazones de Humble Pixel; ver Assets/Sprites/HUD/Licencias).
    // Vida, fragmentos, tiempo, llave y avisos de checkpoint. Los sprites están en Assets/Resources/HUD.
    public sealed class LevelHUD : MonoBehaviour
    {
        [SerializeField] private string levelName = "";
        [SerializeField] private string intro = "";
        [SerializeField, Min(0)] private int totalShards;
        [Tooltip("Tamaño de los corazones respecto al resto del HUD (1 = tamaño original)")]
        [SerializeField, Range(.3f, 1f)] private float heartScale = .67f;
        private BraceletProgress bracelet;
        private GameObject playerObject;
        /// <summary>Fragmentos recogidos y totales del nivel actual (los muestra el inventario del brazalete).</summary>
        public static int LevelShards => instance != null ? instance.shards : 0;
        public static int LevelShardTotal => instance != null ? instance.totalShards : 0;
        private KaelHealth health;
        private KaelRespawn respawn;
        private int maxHealth, shards, lastHp = -1;
        private float elapsed, messageUntil, hitUntil;
        private string message;
        private static bool stopped;

        public static void StopTimer() => stopped = true;

        public void Configure(string title, string introText, int shardCount)
        {
            levelName = title; intro = introText; totalShards = shardCount;
        }

        private static LevelHUD instance;
        /// <summary>Muestra un aviso abajo en la pantalla desde cualquier script.</summary>
        public static void Notify(string text, float seconds = 3f) { if (instance != null) instance.Show(text, seconds); }

        private void Awake() { stopped = false; instance = this; }
        private void OnEnable() => EchoShard.Collected += OnShard;
        private void OnDisable() => EchoShard.Collected -= OnShard;
        private void OnShard() { shards++; Show("Fragmento de eco recogido"); }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            playerObject = player;
            if (player != null)
            {
                health = player.GetComponent<KaelHealth>();
                respawn = player.GetComponent<KaelRespawn>();
                if (health != null) maxHealth = health.Max;
                if (respawn != null)
                {
                    respawn.CheckpointChanged += label => Show("Checkpoint · " + label);
                    respawn.Respawned += () => Show("El eco te devuelve a " + respawn.CheckpointName);
                }
            }
            Show(intro, 5f);
        }

        private void Show(string text, float seconds = 3f)
        {
            message = text;
            messageUntil = Time.time + seconds;
        }

        private void Update()
        {
            if (!stopped) elapsed += Time.deltaTime;
            int hp = health != null ? health.Remaining : 0;
            if (lastHp >= 0 && hp < lastHp) hitUntil = Time.time + 0.25f;
            lastHp = hp;
        }

        // ───────────── Sprites ─────────────
        private static readonly Color Cyan = new Color(0.45f, 0.95f, 1f), Amber = new Color(1f, 0.78f, 0.25f), Text = new Color(0.85f, 1f, 0.92f);
        private static Texture2D heartFull, heartEmpty, heartHit, panel, inset, shardIcon, keyIcon, font;
        private static bool loaded;

        private static Texture2D Tex(string name)
        {
            var t = Resources.Load<Texture2D>("HUD/" + name);
            if (t == null) { Debug.LogWarning("HUD: falta Assets/Resources/HUD/" + name + ".png"); return null; }
            t.filterMode = FilterMode.Point;
            return t;
        }

        private static void Load()
        {
            if (loaded) return;
            loaded = true;
            heartFull = Tex("corazon_lleno"); heartEmpty = Tex("corazon_vacio"); heartHit = Tex("corazon_golpe");
            panel = Tex("panel"); inset = Tex("panel_inset");
            shardIcon = Tex("icono_fragmento"); keyIcon = Tex("icono_llave"); font = Tex("fuente");
        }

        // Dibuja una parte de una textura (coordenadas en píxeles desde ARRIBA-izquierda)
        private static void Part(Texture2D t, float sx, float sy, float sw, float sh, Rect dst)
        {
            if (dst.width <= 0f || dst.height <= 0f) return;
            GUI.DrawTextureWithTexCoords(dst, t, new Rect(sx / t.width, 1f - (sy + sh) / t.height, sw / t.width, sh / t.height));
        }

        // Marco de 9 partes con borde de b píxeles; las esquinas no se estiran
        private static void NineSlice(Texture2D t, Rect r, int b, int ui)
        {
            if (t == null) return;
            float e = b * ui, w = t.width, h = t.height, mw = w - 2 * b, mh = h - 2 * b;
            float cx = r.x + e, cy = r.y + e, cw = r.width - 2 * e, ch = r.height - 2 * e;
            Part(t, 0, 0, b, b, new Rect(r.x, r.y, e, e));
            Part(t, w - b, 0, b, b, new Rect(r.xMax - e, r.y, e, e));
            Part(t, 0, h - b, b, b, new Rect(r.x, r.yMax - e, e, e));
            Part(t, w - b, h - b, b, b, new Rect(r.xMax - e, r.yMax - e, e, e));
            Part(t, b, 0, mw, b, new Rect(cx, r.y, cw, e));
            Part(t, b, h - b, mw, b, new Rect(cx, r.yMax - e, cw, e));
            Part(t, 0, b, b, mh, new Rect(r.x, cy, e, ch));
            Part(t, w - b, b, b, mh, new Rect(r.xMax - e, cy, e, ch));
            Part(t, b, b, mw, mh, new Rect(cx, cy, cw, ch));
        }

        // ───────────── Fuente de píxeles (8 px de alto; solo ASCII, sin acentos) ─────────────
        // por carácter desde el espacio (32): x, y, ancho y avance dentro de fuente.png
        private static string Clean(string text) => PixelHudFont.Clean(text);
        private static int TextWidth(string text, int ui) => PixelHudFont.Measure(text, ui);
        private static void Label(string text, float x, float y, int ui, Color color, bool shadow = true)
            => PixelHudFont.Draw(text, x, y, ui, color, shadow);

        private void OnGUI()
        {
            Load();
            if (font == null || panel == null) return;
            int ui = Mathf.Max(2, Mathf.RoundToInt(Screen.height / 480f));   // un píxel del arte = ui píxeles de pantalla
            Color old = GUI.color;
            int pad = 6 * ui, margin = 8 * ui;

            // ── Izquierda: vida + nombre del nivel + llave ──
            int hp = health != null ? health.Remaining : 0;
            int hearts = Mathf.Max(1, health != null ? health.Max : maxHealth);
            int hs = Mathf.Max(1, Mathf.RoundToInt(ui * heartScale));   // escala entera: los corazones siguen nítidos
            int hw = (heartFull != null ? heartFull.width : 21) * hs, hh = (heartFull != null ? heartFull.height : 18) * hs, gap = 2 * hs;
            float nameW = TextWidth(Clean(levelName), ui);
            float w = Mathf.Max(hearts * (hw + gap) - gap, nameW) + 2 * pad;
            float lineH = 10 * ui;
            float h = hh + lineH + (LabKey.Has ? 12 * ui : 0) + 2 * pad;
            NineSlice(panel, new Rect(margin, margin, w, h), 4, ui);
            for (int i = 0; i < hearts; i++)
            {
                Texture2D t = i < hp ? heartFull : heartEmpty;
                if (i == hp && Time.time < hitUntil) t = heartHit;                 // el corazón recién perdido destella
                float bob = (hp == 1 && i == 0 && hearts > 1) ? Mathf.Round(Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 2f) * -ui : 0f; // último corazón late
                if (t != null) GUI.DrawTexture(new Rect(margin + pad + i * (hw + gap), margin + pad + bob, hw, hh), t);
            }
            Label(levelName, margin + pad, margin + pad + hh + 3 * ui, ui, Text);
            if (LabKey.Has && keyIcon != null)
            {
                GUI.color = Amber;
                GUI.DrawTexture(new Rect(margin + pad, margin + pad + hh + lineH + ui, 16 * ui, 16 * ui), keyIcon);
                GUI.color = old;
                Label("Llave de la base", margin + pad + 18 * ui, margin + pad + hh + lineH + 5 * ui, ui, Amber);
            }

            // ── Derecha: fragmentos y tiempo ──
            int m = (int)(elapsed / 60f), s = (int)(elapsed % 60f);
            string time = $"{m:00}:{s:00}";
            // El saldo es el mismo número que muestra el brazalete; debajo, el avance del nivel.
            if (bracelet == null && playerObject != null) bracelet = playerObject.GetComponent<BraceletProgress>();
            string levelText = totalShards > 0 ? $"{shards}/{totalShards}" : "";
            string balance = bracelet != null ? bracelet.Fragments.ToString() : levelText;
            if (bracelet == null) levelText = "";
            bool showShards = balance.Length > 0;
            float shardW = Mathf.Max(TextWidth(balance, ui), TextWidth(levelText, ui));
            float rw = Mathf.Max(TextWidth(time, ui) + 0f, (showShards ? 24 * ui + shardW : 0f)) + 2 * pad;
            float rh = (showShards ? 22 * ui : 0) + 10 * ui + 2 * pad;
            float rx = Screen.width - margin - rw;
            NineSlice(panel, new Rect(rx, margin, rw, rh), 4, ui);
            float ry = margin + pad;
            if (showShards)
            {
                NineSlice(inset, new Rect(rx + pad, ry, 20 * ui, 20 * ui), 3, ui);
                if (shardIcon != null) { GUI.color = Cyan; GUI.DrawTexture(new Rect(rx + pad + 2 * ui, ry + 2 * ui, 16 * ui, 16 * ui), shardIcon); GUI.color = old; }
                if (levelText.Length > 0)
                {
                    Label(balance, rx + pad + 24 * ui, ry + 1 * ui, ui, Cyan);
                    Label(levelText, rx + pad + 24 * ui, ry + 11 * ui, ui, Text);
                }
                else Label(balance, rx + pad + 24 * ui, ry + 6 * ui, ui, Cyan);
                ry += 22 * ui;
            }
            Label(time, rx + pad, ry + 1 * ui, ui, Text);

            // ── Aviso inferior ──
            if (Time.time < messageUntil && !string.IsNullOrEmpty(message))
            {
                string msg = Clean(message);
                float mw = TextWidth(msg, ui) + 2 * pad, mh = 10 * ui + 2 * pad;
                float fade = Mathf.Clamp01((messageUntil - Time.time) / 0.4f);
                GUI.color = new Color(1f, 1f, 1f, fade);
                var box = new Rect((Screen.width - mw) / 2f, Screen.height - mh - 24 * ui, mw, mh);
                NineSlice(panel, box, 4, ui);
                Label(msg, box.x + pad, box.y + pad + ui, ui, Text);
                GUI.color = old;
            }
        }
    }
}
