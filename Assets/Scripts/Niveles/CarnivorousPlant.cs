using UnityEngine;

namespace PrimalEcho
{
    // Planta carnívora: fija, abre la boca cuando Kael está cerca. Hace daño al tocarla.
    public sealed class CarnivorousPlant : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite closed, open;
        [SerializeField, Min(0f)] private float biteRange = 3f;
        private Transform player;

        public void Configure(SpriteRenderer renderer, Sprite closedSprite, Sprite openSprite)
        {
            visual = renderer; closed = closedSprite; open = openSprite;
        }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) player = tagged.transform;
        }

        private void Update()
        {
            if (visual == null || player == null) return;
            Vector2 d = player.position - transform.position;
            visual.sprite = d.magnitude < biteRange ? open : closed;
            Vector3 s = visual.transform.localScale;
            s.x = Mathf.Abs(s.x) * (d.x >= 0f ? -1f : 1f);
            visual.transform.localScale = s;
        }
    }
}
