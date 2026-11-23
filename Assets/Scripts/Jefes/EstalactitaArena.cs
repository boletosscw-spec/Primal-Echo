using System.Collections;
using UnityEngine;

namespace PrimalEcho
{
    // Estalactita gigante que cae del techo y cierra un lado de la arena de un jefe.
    //  · Antes de caer, el techo retumba y caen piedritas (aviso).
    //  · Al caer se clava en el suelo: es una pared que Kael NO puede trepar (no está en la capa del terreno).
    //  · Agrietar(): se cuartea (fase 2 de Atlas: ya no lo aturde).
    //  · Romper(): estalla en pedazos y deja libre el paso (al vencer al jefe).
    // Arte: Resources/Jefes/<carpeta>/Estalactita.png y Escombro.png (si faltan, usa formas simples).
    // También sirve para las COMPUERTAS blindadas de la base (arte "Compuerta", sonidos "compuerta_*").
    public sealed class EstalactitaArena : MonoBehaviour
    {
        private const float Caida = 4.5f;      // desde qué altura extra cae
        private const float AnchoPared = 1.0f;

        private static PhysicsMaterial2D sinFriccion;
        private SpriteRenderer sprite;
        private Transform visual;
        private BoxCollider2D pared;
        private Sprite escombro;
        private float alto, suelo;
        private bool rompiendo;
        private string sonido = "estalactita";

        public bool Asentada { get; private set; }

        /// <summary>Crea la estalactita en x, buscando el techo de la cueva, y la hace caer tras 'retraso' segundos.</summary>
        public static EstalactitaArena Crear(float x, float sueloY, string carpeta, float retraso,
            string arte = "Estalactita", string sonido = "estalactita")
        {
            float techo = sueloY + 9f;
            var hit = Physics2D.Raycast(new Vector2(x, sueloY + 1.5f), Vector2.up, 25f, 1 << 6);
            if (hit.collider != null) techo = hit.point.y;
            float alto = techo - sueloY + 1.2f; // la parte de arriba se mete en el techo

            var go = new GameObject("Estalactita_Arena");
            go.layer = 0; // capa normal: bloquea a Kael y al jefe, pero Kael no puede deslizarse ni saltar en ella
            go.transform.position = new Vector3(x, sueloY, 0f);
            var e = go.AddComponent<EstalactitaArena>();
            e.alto = alto; e.suelo = sueloY;

            e.pared = go.AddComponent<BoxCollider2D>();
            e.pared.size = new Vector2(AnchoPared, alto);
            e.pared.offset = new Vector2(0f, alto / 2f);
            if (sinFriccion == null) sinFriccion = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
            e.pared.sharedMaterial = sinFriccion;
            e.pared.enabled = false; // se activa al clavarse

            var vis = new GameObject("Visual");
            vis.transform.SetParent(go.transform, false);
            e.visual = vis.transform;
            e.sprite = vis.AddComponent<SpriteRenderer>();
            e.sprite.sortingOrder = 7;
            e.sonido = sonido;
            Sprite dibujo = ProyectilesJefe.Cargar(carpeta, arte, null, alto);
            if (dibujo != null) e.sprite.sprite = dibujo;
            else
            {
                e.sprite.sprite = JefeBase.Cuadro;
                e.sprite.color = new Color(0.32f, 0.26f, 0.25f);
                vis.transform.localScale = new Vector3(1.4f, alto, 1f);
            }
            e.escombro = ProyectilesJefe.Cargar(carpeta, "Escombro", null, 0.6f) ?? ProyectilesJefe.Cargar(carpeta, "Roca", JefeBase.Circulo, 0.6f);
            e.sprite.enabled = false;
            e.StartCoroutine(e.CaerRutina(retraso));
            return e;
        }

        private Vector3 PosFinal => new Vector3(0f, alto / 2f, 0f);

        private IEnumerator CaerRutina(float retraso)
        {
            yield return new WaitForSeconds(retraso);
            // Aviso: el techo retumba y caen piedritas justo donde va a caer
            Sacudida.Hacer(0.8f, 0.6f); // retumbo previo
            Sonidos.Play(sonido + "_retumbo", 0.7f, 1f, transform.position);
            float t = 0f;
            while (t < 0.55f)
            {
                t += 0.11f;
                Pedazo(new Vector3(transform.position.x + Random.Range(-1f, 1f), suelo + alto - 1.4f, 0f),
                       new Vector2(Random.Range(-0.5f, 0.5f), 0f), 0.35f, 1.1f);
                yield return new WaitForSeconds(0.11f);
            }
            // Caída
            sprite.enabled = true;
            Sonidos.Play(sonido + "_caida", 0.55f, 1f, transform.position);
            t = 0f;
            const float dur = 0.32f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / dur);
                visual.localPosition = PosFinal + Vector3.up * (Caida * (1f - p * p));
                yield return null;
            }
            visual.localPosition = PosFinal;
            pared.enabled = true;
            Asentada = true;
            Sacudida.Hacer(4f, 0.6f); // impacto: temblor fuerte + vibración
            Sonidos.Play(sonido + "_impacto", 0.9f, 1f, transform.position);
            Polvo(new Vector3(transform.position.x, suelo + 0.4f, 0f), 3.2f);
            for (int i = 0; i < 5; i++)
                Pedazo(new Vector3(transform.position.x, suelo + 0.3f, 0f), new Vector2(Random.Range(-5f, 5f), Random.Range(3f, 7f)), Random.Range(0.3f, 0.55f), 0.9f);
        }

        /// <summary>Se cuartea: más oscura, tiembla y suelta pedazos (sigue siendo pared).</summary>
        public void Agrietar()
        {
            if (sprite == null) return;
            sprite.color = sprite.sprite == JefeBase.Cuadro ? new Color(0.22f, 0.17f, 0.16f) : new Color(0.68f, 0.6f, 0.58f);
            StartCoroutine(Temblar(0.3f));
            Sacudida.Hacer(1.5f, 0.3f);
            Sonidos.Play(sonido + "_grieta", 0.7f, 1f, transform.position);
            for (int i = 0; i < 4; i++)
                Pedazo(new Vector3(transform.position.x, suelo + Random.Range(1f, alto - 2f), 0f), new Vector2(Random.Range(-4f, 4f), Random.Range(1f, 4f)), Random.Range(0.3f, 0.5f), 0.9f);
        }

        /// <summary>Estalla en pedazos y libera el paso.</summary>
        public void Romper(float retraso)
        {
            if (rompiendo) return;
            rompiendo = true;
            StopAllCoroutines();
            StartCoroutine(RomperRutina(retraso));
        }

        private IEnumerator RomperRutina(float retraso)
        {
            yield return new WaitForSeconds(retraso);
            if (Asentada) yield return Temblar(0.5f);
            pared.enabled = false;
            if (sprite.enabled)
            {
                sprite.enabled = false;
                Sacudida.Hacer(3.5f, 0.55f);
                Sonidos.Play(sonido + "_romper", 0.85f, 1f, transform.position);
                Polvo(new Vector3(transform.position.x, suelo + alto * 0.4f, 0f), 4.5f);
                for (int i = 0; i < 12; i++)
                {
                    float y = suelo + Random.Range(0.3f, alto - 1.5f);
                    Pedazo(new Vector3(transform.position.x + Random.Range(-0.6f, 0.6f), y, 0f),
                           new Vector2(Random.Range(-6f, 6f), Random.Range(1f, 7f)), Random.Range(0.35f, 0.75f), 1.3f);
                }
            }
            yield return new WaitForSeconds(1.5f);
            Destroy(gameObject);
        }

        private IEnumerator Temblar(float segundos)
        {
            float t = 0f;
            while (t < segundos)
            {
                t += Time.deltaTime;
                visual.localPosition = PosFinal + new Vector3(Random.Range(-0.08f, 0.08f), 0f, 0f);
                yield return null;
            }
            visual.localPosition = PosFinal;
        }

        private void Polvo(Vector3 pos, float tamano)
        {
            var go = new GameObject("Polvo_Estalactita");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = JefeBase.Circulo; sr.color = new Color(0.55f, 0.48f, 0.42f, 0.55f); sr.sortingOrder = 9;
            go.AddComponent<EfectoCrecer>().Init(tamano, 0.6f);
        }

        /// <summary>Pedazo de roca que sale volando y se desvanece (decorativo, no hace daño).</summary>
        private void Pedazo(Vector3 pos, Vector2 vel, float tamano, float vida)
        {
            var go = new GameObject("Pedazo");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * (escombro == JefeBase.Circulo ? tamano * 0.6f : tamano);
            go.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = escombro; sr.sortingOrder = 9;
            if (escombro == JefeBase.Circulo) sr.color = new Color(0.4f, 0.33f, 0.3f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 2.2f;
            rb.linearVelocity = vel;
            rb.angularVelocity = Random.Range(-360f, 360f);
            go.AddComponent<Desvanecer>().Init(vida);
        }
    }

    /// <summary>Desvanece el sprite durante su vida y luego lo destruye.</summary>
    public sealed class Desvanecer : MonoBehaviour
    {
        private float vida, t; private SpriteRenderer sr; private Color c;
        public void Init(float segundos) => vida = segundos;
        private void Start() { sr = GetComponent<SpriteRenderer>(); if (sr) c = sr.color; }
        private void Update()
        {
            t += Time.deltaTime;
            if (sr) sr.color = new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01(1f - (t - vida * 0.5f) / (vida * 0.5f)));
            if (t >= vida) Destroy(gameObject);
        }
    }
}
