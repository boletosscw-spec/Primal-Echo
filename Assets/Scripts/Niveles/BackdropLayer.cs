using UnityEngine;

namespace PrimalEcho
{
    // Capa de fondo estilo Hollow Knight: siempre cubre toda la cámara, se repite en horizontal
    // y se desplaza más lento que el mundo según su profundidad.
    [DefaultExecutionOrder(10000)] // después de Cinemachine, para que no tiemble
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BackdropLayer : MonoBehaviour
    {
        [Tooltip("0 = pegado a la cámara (muy lejos) · 1 = se mueve igual que el mundo")]
        [SerializeField, Range(0f, 1f)] private float parallaxX = 0.1f;
        [SerializeField, Range(0f, 0.5f)] private float parallaxY = 0.03f;
        [SerializeField, Min(1f)] private float cover = 1.08f;
        [SerializeField] private bool anchorBottom;
        [Tooltip("Capas de suelo del fondo: al subir la cámara se hunden (así no parece suelo pisable bajo plataformas altas)")]
        [SerializeField, Range(0f, 1f)] private float sinkWhenHigher;
        private Camera cam;
        private SpriteRenderer main;
        private SpriteRenderer[] copies;
        private float startCamY;

        public void Configure(float factorX, float factorY, bool bottom)
        {
            parallaxX = factorX; parallaxY = factorY; anchorBottom = bottom;
        }

        public void Configure(float factorX, float factorY, bool bottom, float sink)
        {
            Configure(factorX, factorY, bottom);
            sinkWhenHigher = sink;
        }

        private void Start()
        {
            cam = Camera.main;
            main = GetComponent<SpriteRenderer>();
            main.drawMode = SpriteDrawMode.Simple;
            if (cam != null) startCamY = cam.transform.position.y;
            copies = new SpriteRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject(i == 0 ? "Copia_Izq" : "Copia_Der");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = main.sprite; sr.color = main.color;
                sr.sortingLayerID = main.sortingLayerID; sr.sortingOrder = main.sortingOrder;
                sr.sharedMaterial = main.sharedMaterial;
                sr.flipX = !main.flipX; // espejo: la unión no se nota
                sr.maskInteraction = main.maskInteraction;
                go.transform.localPosition = new Vector3((i == 0 ? -1f : 1f) * main.sprite.bounds.size.x, 0f, 0f);
                copies[i] = sr;
            }
        }

        private void LateUpdate()
        {
            if (cam == null || main == null || main.sprite == null) return;
            float viewH = cam.orthographicSize * 2f, viewW = viewH * cam.aspect;
            Vector2 size = main.sprite.bounds.size;
            float scale = Mathf.Max(viewW * cover / size.x, viewH * cover / size.y);
            transform.localScale = new Vector3(scale, scale, 1f);
            float width = size.x * scale * 2f; // periodo con espejo = 2 anchos
            Vector3 c = cam.transform.position;
            float x = c.x - Mathf.Repeat(c.x * parallaxX, width);
            if (c.x - x > width * 0.5f) x += width; // la copia central siempre queda cerca de la cámara
            float y = anchorBottom
                ? c.y - viewH * 0.5f + size.y * scale * 0.5f - 0.2f
                : c.y - (c.y - startCamY) * parallaxY;
            // limitar el desplazamiento vertical para no mostrar bordes
            float slack = (size.y * scale - viewH) * 0.5f;
            y = Mathf.Clamp(y, c.y - slack, c.y + slack);
            if (sinkWhenHigher > 0f) y -= Mathf.Max(0f, c.y - startCamY) * sinkWhenHigher;
            // x,y son el centro deseado; se compensa el pivote del sprite (algunos fondos tienen el pivote abajo)
            Vector2 pivotOffset = (Vector2)main.sprite.bounds.center * scale;
            transform.position = new Vector3(x - pivotOffset.x, y - pivotOffset.y, transform.position.z);
        }
    }
}
