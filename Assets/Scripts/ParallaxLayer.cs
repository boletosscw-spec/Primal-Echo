using UnityEngine;

namespace PrimalEcho.Environment
{
    /// <summary>
    /// Controla el efecto Parallax 2D siguiendo suavemente a la cámara (y al jugador)
    /// con repetición horizontal infinita sin cortes ni desajustes.
    /// Ejecuta después de CameraFollow2D (DefaultExecutionOrder 100) para evitar jitter.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Configuración de Seguimiento Parallax")]
        [Tooltip("Cuánto acompaña la capa a la cámara (1.0 = pegada a la cámara como el cielo lejano; 0.85 = montañas lejanas; 0.5 = árboles cercanos; 0.0 = fija en el mundo).")]
        [SerializeField] private Vector2 parallaxMultiplier = new Vector2(0.85f, 0.90f);

        [Header("Repetición Horizontal Infinita")]
        [SerializeField] private bool infiniteHorizontal = true;
        [SerializeField] private bool mirrorAlternateTiles = true;
        [SerializeField] private float spriteWidth = 0f;

        private Transform cameraTransform;
        private Vector3 startLayerPos;
        private Vector3 startCameraPos;
        private bool initialized;

        private void Start()
        {
            InitializeIfNeeded();
        }

        private void InitializeIfNeeded()
        {
            if (initialized) return;

            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
            else
            {
                Camera anyCam = Object.FindFirstObjectByType<Camera>();
                if (anyCam != null) cameraTransform = anyCam.transform;
            }

            if (cameraTransform == null) return;

            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Garantizar opacidad 100% en las capas de árboles/panorama para que no se vean translúcidas
                if (!gameObject.name.Contains("Niebla") && sr.color.a < 0.99f)
                {
                    Color c = sr.color;
                    c.a = 1f;
                    sr.color = c;
                    foreach (SpriteRenderer childSr in GetComponentsInChildren<SpriteRenderer>(true))
                    {
                        Color cc = childSr.color;
                        cc.a = 1f;
                        childSr.color = cc;
                    }
                }

                if (sr.sprite != null && spriteWidth <= 0.01f)
                {
                    spriteWidth = sr.bounds.size.x;
                }
            }

            startCameraPos = cameraTransform.position;

            // Auto-ajuste para que los fondos principales cubran siempre el 100% del alto de la cámara sin dejar franjas negras arriba ni abajo
            Camera cam = cameraTransform.GetComponent<Camera>();
            if (cam != null && cam.orthographic && sr != null && sr.sprite != null)
            {
                float camHeight = cam.orthographicSize * 2f;
                float camWidth = camHeight * cam.aspect;
                bool isFullFrameBg = gameObject.name.Contains("Panorama") || gameObject.name.Contains("Bosque") || gameObject.name.Contains("Cielo");

                if (isFullFrameBg)
                {
                    float rawW = sr.sprite.bounds.size.x;
                    float rawH = sr.sprite.bounds.size.y;
                    if (rawW > 0.1f && rawH > 0.1f)
                    {
                        float reqScaleX = (camWidth * 1.02f) / rawW;
                        float reqScaleY = (camHeight * 1.03f) / rawH;
                        float uniformScale = Mathf.Max(reqScaleX, reqScaleY);
                        transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
                        transform.position = new Vector3(startCameraPos.x, startCameraPos.y + 0.15f, transform.position.z);
                        spriteWidth = rawW * uniformScale;
                    }
                }
            }

            // Auto-corrección si la escena tenía valores antiguos (X=40 lejos de la cámara o infiniteHorizontal desactivado)
            if (!infiniteHorizontal)
            {
                infiniteHorizontal = true;
            }
            if (Mathf.Abs(transform.position.x - startCameraPos.x) > 15f)
            {
                transform.position = new Vector3(startCameraPos.x, transform.position.y, transform.position.z);
            }
            if (parallaxMultiplier.y < 0.5f)
            {
                // Convertir multiplicadores antiguos (donde 0.02 era cielo) a seguimiento real de cámara
                parallaxMultiplier = new Vector2(Mathf.Clamp(1f - parallaxMultiplier.x, 0.45f, 0.96f), 0.88f);
            }

            startLayerPos = transform.position;

            if (infiniteHorizontal && sr != null && sr.sprite != null && spriteWidth > 0.01f)
            {
                EnsureTiledChildren(sr);
            }

            initialized = true;
        }

        /// <summary>
        /// Garantiza que existan copias laterales a izquierda y derecha para cubrir toda la vista
        /// sin importar la resolución o el movimiento de la cámara.
        /// </summary>
        private void EnsureTiledChildren(SpriteRenderer mainSr)
        {
            // Si el generador del mapa ya creó los bloques laterales, no duplicarlos
            if (transform.childCount >= 4) return;

            float localStepX = mainSr.sprite.bounds.size.x;
            if (localStepX <= 0.001f) return;

            for (int offset = -3; offset <= 3; offset++)
            {
                if (offset == 0) continue;
                string tileName = $"_Tile_{offset:+#;-#;0}";
                if (transform.Find(tileName) != null) continue;

                GameObject clone = new GameObject(tileName);
                clone.transform.SetParent(transform, false);
                clone.transform.localPosition = new Vector3(offset * localStepX, 0f, 0f);
                clone.transform.localRotation = Quaternion.identity;
                clone.transform.localScale = Vector3.one;

                SpriteRenderer cloneSr = clone.AddComponent<SpriteRenderer>();
                cloneSr.sprite = mainSr.sprite;
                cloneSr.color = mainSr.color;
                cloneSr.sortingLayerID = mainSr.sortingLayerID;
                cloneSr.sortingOrder = mainSr.sortingOrder;
                cloneSr.sharedMaterial = mainSr.sharedMaterial;

                bool flip = mirrorAlternateTiles && (Mathf.Abs(offset) % 2 == 1);
                cloneSr.flipX = mainSr.flipX ^ flip;
            }
        }

        private void LateUpdate()
        {
            if (!initialized)
            {
                InitializeIfNeeded();
                if (!initialized) return;
            }

            if (cameraTransform == null) return;

            Vector3 camPos = cameraTransform.position;
            Vector3 camDelta = camPos - startCameraPos;

            // Posición base siguiendo a la cámara según el factor de profundidad
            // En Y mantenemos seguimiento 1:1 con la cámara para que el fondo nunca se salga por arriba ni por abajo del marco de la cámara
            float targetX = startLayerPos.x + camDelta.x * parallaxMultiplier.x;
            float targetY = startLayerPos.y + camDelta.y;

            if (infiniteHorizontal && spriteWidth > 0.01f)
            {
                // Si usamos espejo alternado, el periodo de repetición exacta es 2 * spriteWidth
                float repeatPeriod = mirrorAlternateTiles ? (spriteWidth * 2f) : spriteWidth;
                if (repeatPeriod > 0.01f)
                {
                    float diffX = camPos.x - targetX;
                    int wraps = Mathf.RoundToInt(diffX / repeatPeriod);
                    targetX += wraps * repeatPeriod;
                }
            }

            transform.position = new Vector3(targetX, targetY, startLayerPos.z);
        }

        public void Configure(Vector2 followMultiplier, bool infinite = true, bool mirrorAlternate = true, float width = 0f)
        {
            parallaxMultiplier = followMultiplier;
            infiniteHorizontal = infinite;
            mirrorAlternateTiles = mirrorAlternate;
            if (width > 0.01f) spriteWidth = width;
        }

        public void SetMultiplier(Vector2 multiplier)
        {
            parallaxMultiplier = multiplier;
        }
    }
}
