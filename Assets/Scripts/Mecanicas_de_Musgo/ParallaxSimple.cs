using UnityEngine;

namespace PrimalEcho
{
    // Capa de decoración con profundidad: positivo = más lejos (se mueve con la cámara),
    // negativo = primer plano (se mueve al revés). Cuando la cámara está encima del objeto,
    // el objeto queda justo donde se colocó en el editor.
    public sealed class ParallaxSimple : MonoBehaviour
    {
        [SerializeField, Range(-1f, 1f)] private float factor = 0.3f;
        [SerializeField] private float alturaReferencia = 8f;
        private Vector3 basePos;
        private Transform cam;

        public void Configure(float value) => factor = value;

        private void Start()
        {
            basePos = transform.position;
            if (Camera.main != null) cam = Camera.main.transform;
        }

        private void LateUpdate()
        {
            if (cam == null) return;
            float dx = (cam.position.x - basePos.x) * factor;
            float dy = (cam.position.y - alturaReferencia) * factor * 0.5f;
            transform.position = new Vector3(basePos.x + dx, basePos.y + dy, basePos.z);
        }
    }
}
