using UnityEngine;

namespace PrimalEcho
{
    // Fondo por zonas, como en Hollow Knight: dentro del área de las cuevas (máscara) se ven las capas
    // de cueva y fuera de ella las de jungla. No depende de dónde esté Kael.
    [DefaultExecutionOrder(10001)]
    public sealed class CaveAmbience : MonoBehaviour
    {
        [SerializeField] private Rect[] caveZones;
        [SerializeField] private SpriteRenderer[] outdoorLayers;
        [SerializeField] private SpriteRenderer[] caveLayers;

        public void Configure(Rect[] zones, SpriteRenderer[] outdoor, SpriteRenderer[] cave)
        {
            caveZones = zones; outdoorLayers = outdoor; caveLayers = cave;
            Apply();
        }

        private void Start() => Apply();

        // BackdropLayer crea sus copias laterales en Start; se ajustan aquí también.
        private void LateUpdate()
        {
            enabled = false;
            Apply();
        }

        private void Apply()
        {
            Set(outdoorLayers, SpriteMaskInteraction.VisibleOutsideMask);
            Set(caveLayers, SpriteMaskInteraction.VisibleInsideMask);
        }

        private static void Set(SpriteRenderer[] layers, SpriteMaskInteraction mode)
        {
            if (layers == null) return;
            foreach (var layer in layers)
            {
                if (layer == null) continue;
                layer.enabled = true;
                var c = layer.color; c.a = 1f; layer.color = c;
                foreach (var sr in layer.GetComponentsInChildren<SpriteRenderer>(true)) sr.maskInteraction = mode;
            }
        }
    }
}
