using UnityEngine;

namespace PrimalEcho
{
    // Gotitas que salen volando cuando un slime revienta.
    public sealed class SalpicaduraSlime : MonoBehaviour
    {
        private Vector2[] vel;
        private Transform[] gotas;
        private SpriteRenderer[] srs;
        private float t;

        public static void Crear(Vector3 pos, Sprite sprite, Color color)
        {
            if (sprite == null) return;
            var go = new GameObject("Salpicadura_Slime");
            go.transform.position = pos;
            var s = go.AddComponent<SalpicaduraSlime>();
            const int n = 7;
            s.vel = new Vector2[n]; s.gotas = new Transform[n]; s.srs = new SpriteRenderer[n];
            for (int i = 0; i < n; i++)
            {
                var g = new GameObject("Gota");
                g.transform.SetParent(go.transform, false);
                var sr = g.AddComponent<SpriteRenderer>();
                sr.sprite = sprite; sr.sortingOrder = 11; sr.color = color;
                float esc = Random.Range(0.12f, 0.28f);
                g.transform.localScale = new Vector3(esc, esc, 1f);
                float ang = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                s.vel[i] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * Random.Range(3f, 7f);
                s.gotas[i] = g.transform; s.srs[i] = sr;
            }
        }

        private void Update()
        {
            t += Time.deltaTime;
            for (int i = 0; i < gotas.Length; i++)
            {
                vel[i] += Vector2.down * 22f * Time.deltaTime;
                gotas[i].localPosition += (Vector3)(vel[i] * Time.deltaTime);
                var c = srs[i].color; c.a = Mathf.Clamp01(1f - t / 0.7f); srs[i].color = c;
            }
            if (t > 0.75f) Destroy(gameObject);
        }
    }
}
