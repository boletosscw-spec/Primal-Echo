using System;
using UnityEngine;

namespace PrimalEcho
{
    // Kiro apagado junto a Orin. Cuando Orin se lo pide (línea "#REPARAR" de su diálogo), Kael se hinca
    // y lo repara con la animación Kael_Reparar_Kiro; Kiro se enciende y desde ahí acompaña a Kael.
    public sealed class ReparacionKiro : MonoBehaviour
    {
        [SerializeField] private Sprite kiroApagado, kiroDespierto;
        [SerializeField] private Sprite[] reparar = Array.Empty<Sprite>();   // 6 cuadros: Kael + Kiro
        [SerializeField] private Texture2D hojaChispas;                     // efecto de reparación: 6 cuadros en fila
        private Sprite[] chispas, dormir;
        private KiroCompanero kiroPresentado;
        [SerializeField, Min(10f)] private float ppuReparar = 170f;          // tamaño de la hoja respecto a Kael en el juego

        // Medidas de la hoja de reparación (cuadros de 576x544 px)
        private const float KiroEnCuadro = 132f;   // centro de Kiro respecto al centro del cuadro (px)
        private const float KaelEnCuadro = -88f;   // centro de Kael respecto al centro del cuadro (px)
        private const float PisoEnCuadro = 7f;     // espacio vacío bajo los pies (px)
        private const float SegundosPorCuadro = 0.43f, Pausa = 0.24f;

        private SpriteRenderer apagado, anim, efecto;
        private PlayerController kael;
        private SpriteRenderer kaelSprite;
        private Action alTerminar;
        private float t = -1f;

        public bool Hecha { get; private set; }

        public void Configure(Sprite off, Sprite on, Sprite[] frames, Texture2D fx, float ppu)
        {
            kiroApagado = off; kiroDespierto = on; reparar = frames; hojaChispas = fx; ppuReparar = ppu;
        }

        private void Start()
        {
            if (hojaChispas != null) chispas = AnimacionHoja.Cortar(hojaChispas, 6, 6, hojaChispas.width / 6, hojaChispas.height, 0);
            dormir = KiroCompanero.CargarCuadros("dormir");
            bool animado = dormir != null && dormir.Length >= 2;
            apagado = Renderer("Kiro_Apagado", animado ? dormir[dormir.Length - 2] : kiroApagado, 9);
            if (apagado != null && animado)
            {
                apagado.transform.localScale = Vector3.one * KiroCompanero.Escala;
                apagado.transform.localPosition = Vector3.up * (KiroCompanero.AlturaPies * KiroCompanero.Escala);
                apagado.flipX = true;
            }
        }

        private SpriteRenderer Renderer(string n, Sprite s, int orden)
        {
            if (s == null) return null;
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = orden;
            return sr;
        }

        /// <summary>Empieza la reparación; al terminar llama a <paramref name="listo"/>.</summary>
        public void Iniciar(Action listo)
        {
            alTerminar = listo;
            if (Hecha || reparar == null || reparar.Length == 0) { Terminar(); return; }
            var tagged = GameObject.FindGameObjectWithTag("Player");
            kael = tagged != null ? tagged.GetComponent<PlayerController>() : null;
            if (kael != null)
            {
                kael.MovimientoGuiado = 0f;
                kaelSprite = kael.GetComponent<SpriteRenderer>();
                if (kaelSprite != null) kaelSprite.enabled = false;
                var rb = kael.GetComponent<Rigidbody2D>();
                Vector2 pie = new Vector2(transform.position.x + (KaelEnCuadro - KiroEnCuadro) / ppuReparar, transform.position.y + 0.05f);
                if (rb != null) { rb.linearVelocity = Vector2.zero; rb.position = pie; }
                kael.transform.position = pie;
            }
            if (apagado != null) apagado.enabled = false;

            anim = Renderer("Kael_Repara_Kiro", reparar[0], 10);
            float escala = reparar[0].pixelsPerUnit / ppuReparar;
            anim.transform.localScale = new Vector3(escala, escala, 1f);
            Colocar(anim, transform.position.x - KiroEnCuadro / ppuReparar, transform.position.y - PisoEnCuadro / ppuReparar, reparar[0]);
            if (chispas != null && chispas.Length > 0)
            {
                efecto = Renderer("Chispas", chispas[0], 11);
                efecto.transform.localScale = new Vector3(0.75f, 0.75f, 1f);
                efecto.transform.position = transform.position + new Vector3(-0.1f, 0.65f, 0f);
                efecto.enabled = false;
            }
            t = 0f;
        }

        // Pone el sprite de modo que el centro de su cuadro quede en x y la orilla inferior en y.
        private static void Colocar(SpriteRenderer sr, float x, float y, Sprite s)
        {
            Vector3 esc = sr.transform.lossyScale;
            float ppu = s.pixelsPerUnit;
            float cx = (s.rect.width * 0.5f - s.pivot.x) / ppu, minY = -s.pivot.y / ppu;
            sr.transform.position = new Vector3(x - cx * esc.x, y - minY * esc.y, 0f);
        }

        private void Update()
        {
            if (Hecha) return;
            if (t < 0f)
            {
                if (apagado != null && dormir != null && dormir.Length >= 2)
                    apagado.sprite = dormir[dormir.Length - 2 + (int)(Time.time * 2f) % 2];
                return;
            }
            t += Time.deltaTime;
            int cuadro = Mathf.Min(reparar.Length - 1, (int)(t / SegundosPorCuadro));
            anim.sprite = reparar[cuadro];
            if (efecto != null)
            {
                bool chispea = cuadro >= 2 && cuadro <= 4;
                efecto.enabled = chispea;
                if (chispea) efecto.sprite = chispas[(int)(t * 12f) % chispas.Length];
            }
            if (t >= reparar.Length * SegundosPorCuadro + Pausa) Terminar();
        }

        private void Terminar()
        {
            Hecha = true;
            t = -1f;
            if (anim != null) Destroy(anim.gameObject);
            if (efecto != null) Destroy(efecto.gameObject);
            if (apagado != null) apagado.enabled = false;
            if (kael != null)
            {
                if (kaelSprite != null) kaelSprite.enabled = true;
                kael.FaceStrike(1f);
                kael.MovimientoGuiado = float.NaN;
            }
            kiroPresentado = KiroCompanero.Despertar(kiroDespierto, transform.position, duranteEncuentro: true);
            var listo = alTerminar; alTerminar = null;
            listo?.Invoke();
        }

        internal void LiberarKiro()
        {
            if (kiroPresentado != null) kiroPresentado.LiberarPresentacion();
            kiroPresentado = null;
        }

        private void OnDisable()
        {
            LiberarKiro();
            // Si se interrumpe a medias (cambio de escena), Kael no se queda invisible ni congelado.
            if (t >= 0f && kael != null)
            {
                if (kaelSprite != null) kaelSprite.enabled = true;
                kael.MovimientoGuiado = float.NaN;
            }
        }
    }
}
