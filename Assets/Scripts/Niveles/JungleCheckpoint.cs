using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PrimalEcho
{
    // Farola-checkpoint: al tocarla se enciende y guarda el punto de reaparición.
    [RequireComponent(typeof(Collider2D))]
    public sealed class JungleCheckpoint : MonoBehaviour
    {
        [SerializeField] private string label = "Checkpoint";
        [SerializeField] private Light2D lamp;
        [SerializeField] private Color offColor = new Color(0.45f, 0.55f, 0.6f);
        [SerializeField] private Color onColor = new Color(1f, 0.82f, 0.45f);
        [SerializeField] private float offIntensity = 0.35f, onIntensity = 1.6f;

        public void Configure(string checkpointLabel, Light2D light)
        {
            label = checkpointLabel;
            lamp = light;
        }

        private void Awake() => SetLit(false);

        private void OnTriggerEnter2D(Collider2D other)
        {
            var respawn = other.GetComponentInParent<KaelRespawn>();
            if (respawn == null) return;
            respawn.SetCheckpoint(transform.position, label);
            foreach (var cp in FindObjectsByType<JungleCheckpoint>(FindObjectsSortMode.None)) cp.SetLit(cp == this);
        }

        private void SetLit(bool on)
        {
            if (lamp == null) return;
            lamp.color = on ? onColor : offColor;
            lamp.intensity = on ? onIntensity : offIntensity;
        }
    }
}
