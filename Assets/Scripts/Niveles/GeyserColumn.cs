using UnityEngine;

namespace PrimalEcho
{
    // Géiser: mientras Kael está en la columna de agua sube a velocidad constante.
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class GeyserColumn : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float liftSpeed = 12f;
        [SerializeField] private Color bubbleColor = new Color(0.65f, 0.95f, 1f, 0.75f);
        [SerializeField, Min(1)] private int bubbles = 10;
        private BoxCollider2D column;
        private Transform[] pool;
        private float[] phase;
        private static Sprite dot;

        private void Awake()
        {
            column = GetComponent<BoxCollider2D>();
            column.isTrigger = true;
            if (dot == null)
            {
                var tex = new Texture2D(2, 2) { filterMode = FilterMode.Point };
                tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                tex.Apply();
                dot = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 16f);
            }
            pool = new Transform[bubbles];
            phase = new float[bubbles];
            for (int i = 0; i < bubbles; i++)
            {
                var go = new GameObject("Burbuja");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dot; sr.color = bubbleColor; sr.sortingOrder = 6;
                pool[i] = go.transform;
                phase[i] = Random.value;
            }
        }

        private void Update()
        {
            Vector2 size = column.size, off = column.offset;
            for (int i = 0; i < pool.Length; i++)
            {
                float p = (phase[i] + Time.time * 0.9f) % 1f;
                float x = off.x + (Mathf.PerlinNoise(i * 3.1f, Time.time) - 0.5f) * size.x;
                pool[i].localPosition = new Vector3(x, off.y - size.y * 0.5f + p * size.y, 0f);
            }
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            var body = other.attachedRigidbody;
            if (body != null && body.linearVelocity.y < liftSpeed) body.linearVelocity = new Vector2(body.linearVelocity.x, liftSpeed);
        }
    }
}
