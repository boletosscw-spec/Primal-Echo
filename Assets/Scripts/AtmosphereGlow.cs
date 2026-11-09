using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PrimalEcho
{
    public class AtmosphereGlow : MonoBehaviour
    {
        [SerializeField] private float minIntensity = 0.8f;
        [SerializeField] private float maxIntensity = 1.3f;
        [SerializeField] private float pulseSpeed = 1.8f;
        [SerializeField] private bool randomPhase = true;

        private Light2D lightComponent;
        private float phaseOffset;

        private void Start()
        {
            lightComponent = GetComponent<Light2D>();
            if (randomPhase)
            {
                phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            }
        }

        private void Update()
        {
            if (lightComponent == null) return;

            float t = (Mathf.Sin(Time.time * pulseSpeed + phaseOffset) + 1f) * 0.5f;
            lightComponent.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        }

        public void SetGlow(float min, float max, float speed)
        {
            minIntensity = min;
            maxIntensity = max;
            pulseSpeed = speed;
        }
    }
}
