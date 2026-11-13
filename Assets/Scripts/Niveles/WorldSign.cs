using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // Cartel de ayuda con el estilo del HUD: panel holográfico y fuente de píxeles.
    // Aparece cuando Kael se acerca. El formato del texto lo explica HoloText.
    // Sustituye en juego a los carteles de TextMesh que coloca el generador de niveles (objeto "Carteles").
    public sealed class WorldSign : MonoBehaviour
    {
        [SerializeField, TextArea] private string text = "";
        [SerializeField, Min(1f)] private float showDistance = 11f;
        [SerializeField, Min(40)] private int maxLineWidth = 170; // en píxeles de la fuente

        private HoloText panel;
        private Transform player;
        private float alpha;

        public void Configure(string value) { text = value; panel = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
            ConvertAll();
        }

        private static void Loaded(Scene scene, LoadSceneMode mode) => ConvertAll();

        private static void ConvertAll()
        {
            foreach (var mesh in FindObjectsByType<TextMesh>(FindObjectsSortMode.None))
            {
                var parent = mesh.transform.parent;
                if (parent == null || parent.name != "Carteles" || mesh.GetComponent<WorldSign>()) continue;
                mesh.gameObject.AddComponent<WorldSign>().Configure(mesh.text);
                if (mesh.TryGetComponent(out MeshRenderer oldText)) oldText.enabled = false;
            }
        }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) player = tagged.transform;
        }

        private void Update()
        {
            bool near = player != null && Mathf.Abs(player.position.x - transform.position.x) < showDistance
                && Mathf.Abs(player.position.y - transform.position.y) < 9f;
            alpha = Mathf.MoveTowards(alpha, near ? 1f : 0f, Time.deltaTime * 3f);
        }

        private void OnGUI()
        {
            if (alpha <= 0.01f || Event.current.type != EventType.Repaint || Time.timeScale <= 0f) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            if (panel == null) panel = new HoloText(text, maxLineWidth);
            Vector3 screen = cam.WorldToScreenPoint(transform.position);
            if (panel.Empty || screen.z < 0f) return;
            int ui = HoloText.Scale;
            float w = panel.Width(ui), h = panel.Height(ui);
            float bob = Mathf.Round(Mathf.Sin(Time.time * 2f + transform.position.x) * 2f) * ui;
            float x = Mathf.Clamp(screen.x - w / 2f, 4 * ui, Screen.width - w - 4 * ui);
            int oldDepth = GUI.depth;
            GUI.depth = 5; // por detrás del HUD y del brazalete
            panel.Draw(x, Screen.height - screen.y - h / 2f + bob, ui, alpha, HoloText.Cyan);
            GUI.depth = oldDepth;
        }
    }
}
