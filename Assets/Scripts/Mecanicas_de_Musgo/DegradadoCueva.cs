using UnityEngine;

namespace PrimalEcho
{
    // Degradado en las orillas del fondo negro de la cueva para que el cambio bosque -> cueva no se vea
    // como un corte recto. Crea dos franjas (izquierda y derecha) que van del color del fondo a transparente.
    // Lo agrega Nivel1MusgoBuilder al objeto "Fondo_Cueva"; la textura se genera al iniciar.
    public sealed class DegradadoCueva : MonoBehaviour
    {
        [SerializeField] private float x0, x1, y0, y1;
        [SerializeField, Min(0.5f)] private float ancho = 10f;
        [SerializeField] private Color color = Color.black;
        [SerializeField] private int orden = -50;

        public void Configure(float desdeX, float hastaX, float desdeY, float hastaY, Color c, float anchoDegradado, int sortingOrder)
        {
            x0 = desdeX; x1 = hastaX; y0 = desdeY; y1 = hastaY; color = c; ancho = anchoDegradado; orden = sortingOrder;
        }

        private void Start()
        {
            var sprite = CrearSprite();
            Franja("Degradado_Izq", x0 - ancho / 2f, sprite, true);
            Franja("Degradado_Der", x1 + ancho / 2f, sprite, false);
        }

        private void Franja(string nombre, float centroX, Sprite sprite, bool haciaIzquierda)
        {
            var go = new GameObject(nombre);
            go.transform.SetParent(transform.parent, false);
            go.transform.position = new Vector3(centroX, (y0 + y1) / 2f, transform.position.z);
            // el sprite va de opaco (izquierda) a transparente (derecha); la franja izquierda se voltea
            go.transform.localScale = new Vector3(haciaIzquierda ? -ancho : ancho, y1 - y0, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = orden;
            var propio = GetComponent<SpriteRenderer>();
            if (propio != null)
            {
                sr.sortingLayerID = propio.sortingLayerID;
                sr.sharedMaterial = propio.sharedMaterial;
            }
        }

        private static Sprite CrearSprite()
        {
            const int n = 64; // textura cuadrada: así el sprite mide 1x1 unidad y la escala da el tamaño real
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Degradado_Cueva"
            };
            for (int x = 0; x < n; x++)
            {
                float t = 1f - (x + 0.5f) / n;          // 1 junto a la cueva, 0 en el bosque
                float a = t * t * (3f - 2f * t);         // curva suave
                for (int y = 0; y < n; y++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n); // 1x1 unidad
        }
    }
}
