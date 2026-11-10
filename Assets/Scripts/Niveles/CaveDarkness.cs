using UnityEngine;

namespace PrimalEcho
{
    /// <summary>
    /// Al entrar a una cueva la pantalla se oscurece poco a poco, dejando un círculo de luz alrededor de Kael
    /// (como en Hollow Knight). Al salir vuelve la luz. Se añade sola a la cámara (lo hace CaveAmbience).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CaveDarkness : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float maxDarkness = 0.72f;
        [SerializeField] private float fadeSeconds = 1.4f;
        [Tooltip("Radio de luz alrededor de Kael (unidades)")]
        [SerializeField] private float lightRadius = 3.5f;
        [Tooltip("Desde esta distancia ya está oscuro del todo")]
        [SerializeField] private float darkRadius = 11f;
        [SerializeField] private Color color = new Color(0.03f, 0.02f, 0.07f, 1f);
        [SerializeField] private int sortingOrder = 30; // encima de Kael (10) y los enemigos; el HUD siempre va arriba

        private const float Size = 60f;
        private Transform player;
        private SpriteRenderer overlay;
        private float amount;

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            var go = new GameObject("Oscuridad_Cueva");
            go.transform.SetParent(transform, false);
            overlay = go.AddComponent<SpriteRenderer>();
            overlay.sprite = BuildSprite();
            overlay.sortingOrder = sortingOrder;
            overlay.enabled = false;
            go.transform.localScale = new Vector3(Size, Size, 1f);
        }

        private Sprite BuildSprite()
        {
            const int N = 256;
            var tex = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Oscuridad" };
            var px = new Color32[N * N];
            float r0 = lightRadius / Size, r1 = darkRadius / Size; // en coordenadas de 0 a 1
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N - 0.5f, dy = (y + 0.5f) / N - 0.5f;
                    float t = Mathf.InverseLerp(r0, r1, Mathf.Sqrt(dx * dx + dy * dy));
                    float a = t * t * (3f - 2f * t);
                    px[y * N + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f), N); // 1 unidad, se escala a Size
        }

        private void LateUpdate()
        {
            if (overlay == null) return;
            bool inside = player != null && CaveAmbience.IsInside((Vector2)player.position + Vector2.up);
            amount = Mathf.MoveTowards(amount, inside ? 1f : 0f, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
            overlay.enabled = amount > 0.001f;
            if (!overlay.enabled) return;
            Vector3 center = player != null ? player.position + Vector3.up : transform.position;
            overlay.transform.position = new Vector3(center.x, center.y, 0f);
            var c = color; c.a = amount * maxDarkness;
            overlay.color = c;
        }
    }
}
