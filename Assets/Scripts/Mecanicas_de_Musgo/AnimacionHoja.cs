using System.Collections.Generic;
using UnityEngine;

namespace PrimalEcho
{
    // Anima un SpriteRenderer con una hoja de cuadros (Assets/Arte/Valle_del_Eco/Animaciones).
    // Los cuadros se cortan una sola vez por textura y se comparten entre todas las plantas.
    public sealed class AnimacionHoja : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer destino;
        [SerializeField] private Texture2D hoja;
        [SerializeField, Min(1)] private int columnas = 1, cuadros = 1;
        [SerializeField, Min(1)] private int anchoCuadro = 1, altoCuadro = 1;
        [SerializeField, Min(0)] private int separacion = 2;
        [SerializeField, Min(0.1f)] private float fps = 12f;
        [SerializeField] private bool desfaseAleatorio = true;

        private static readonly Dictionary<Texture2D, Sprite[]> cache = new Dictionary<Texture2D, Sprite[]>();
        private Sprite[] frames;
        private float offset;

        public Sprite[] Frames => frames;
        public float Fps { get => fps; set => fps = Mathf.Max(0.1f, value); }
        public bool Pausado { get; set; }

        public void Configure(SpriteRenderer sr, Texture2D tex, int cols, int count, int fw, int fh, int pad, float framesPerSecond)
        {
            destino = sr; hoja = tex; columnas = cols; cuadros = count;
            anchoCuadro = fw; altoCuadro = fh; separacion = pad; fps = framesPerSecond;
        }

        /// <summary>Corta la hoja en cuadros con el pivote al centro y 100 píxeles por unidad.</summary>
        public static Sprite[] Cortar(Texture2D tex, int cols, int count, int fw, int fh, int pad)
        {
            if (tex == null || cols <= 0 || count <= 0) return null;
            if (cache.TryGetValue(tex, out var hechos) && hechos != null && hechos.Length == count && hechos[0] != null) return hechos;
            var lista = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                int c = i % cols, r = i / cols;
                float x = c * (fw + pad);
                float yTop = r * (fh + pad);
                float y = tex.height - yTop - fh;
                if (x + fw > tex.width || y < 0f) { System.Array.Resize(ref lista, i); break; }
                lista[i] = Sprite.Create(tex, new Rect(x, y, fw, fh), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                lista[i].name = tex.name + "_" + i;
            }
            cache[tex] = lista;
            return lista;
        }

        private void Awake()
        {
            if (destino == null) destino = GetComponent<SpriteRenderer>();
            frames = Cortar(hoja, columnas, cuadros, anchoCuadro, altoCuadro, separacion);
            offset = desfaseAleatorio ? Random.value * 10f : 0f;
        }

        private void Update()
        {
            if (Pausado || destino == null || frames == null || frames.Length == 0) return;
            destino.sprite = frames[(int)((Time.time + offset) * fps) % frames.Length];
        }
    }
}
