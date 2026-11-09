using UnityEngine;

namespace PrimalEcho
{
    // HUD simple: vida, fragmentos, tiempo del nivel y avisos de checkpoint.
    public sealed class LevelHUD : MonoBehaviour
    {
        [SerializeField] private string levelName = "";
        [SerializeField] private string intro = "";
        [SerializeField, Min(0)] private int totalShards;
        private KaelHealth health;
        private KaelRespawn respawn;
        private int maxHealth, shards;
        private float elapsed, messageUntil;
        private string message;
        private static bool stopped;
        private GUIStyle big, small;

        public static void StopTimer() => stopped = true;

        public void Configure(string title, string introText, int shardCount)
        {
            levelName = title; intro = introText; totalShards = shardCount;
        }

        private void Awake() => stopped = false;
        private void OnEnable() => EchoShard.Collected += OnShard;
        private void OnDisable() => EchoShard.Collected -= OnShard;
        private void OnShard() { shards++; Show("Fragmento de eco recogido"); }

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
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
        }

        private void OnGUI()
        {
            if (big == null)
            {
                big = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
                big.normal.textColor = new Color(1f, 0.45f, 0.45f);
                small = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                small.normal.textColor = new Color(0.85f, 1f, 0.92f);
            }
            int hp = health != null ? health.Remaining : 0;
            string hearts = new string('♥', Mathf.Max(0, hp)) + new string('·', Mathf.Max(0, maxHealth - hp));
            GUI.Label(new Rect(20, 14, 400, 30), hearts, big);
            GUI.Label(new Rect(20, 44, 600, 24), levelName, small);
            int m = (int)(elapsed / 60f), s = (int)(elapsed % 60f);
            string stats = (totalShards > 0 ? $"Fragmentos {shards}/{totalShards}    " : "") + $"Tiempo {m:00}:{s:00}";
            GUI.Label(new Rect(Screen.width - 340, 14, 320, 24), stats, small);
            if (Time.time < messageUntil && !string.IsNullOrEmpty(message))
            {
                var center = new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = 17 };
                GUI.Label(new Rect(0, Screen.height - 70, Screen.width, 30), message, center);
            }
        }
    }
}
