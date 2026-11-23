using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PrimalEcho
{
    // Brillo suave que respira (flores, hongos y cristales).
    [RequireComponent(typeof(Light2D))]
    public sealed class PulsoLuz : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float amplitud = 0.18f;
        [SerializeField, Min(0.1f)] private float velocidad = 1.6f;
        private Light2D luz;
        private float baseIntensidad, fase;

        private void Awake()
        {
            luz = GetComponent<Light2D>();
            baseIntensidad = luz.intensity;
            fase = transform.position.x * 0.7f;
        }

        private void Update()
        {
            luz.intensity = baseIntensidad * (1f + amplitud * Mathf.Sin(Time.time * velocidad + fase));
        }
    }
}
