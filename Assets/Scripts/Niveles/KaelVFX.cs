using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    public enum KaelFx
    {
        PolvoPasos, PolvoSalto, PolvoAterrizaje, PolvoDeslizamiento,
        EmbestidaLanza, DisparoFlecha, ImpactoGeneral, ImpactoTecnologico, ChoqueBloqueo,
        KiroReparacion
    }

    // Efectos VFX de Kael (hojas en Assets/Resources/KaelVFX).
    //  · Movimiento: polvo al correr, saltar, aterrizar y rodar/deslizarse (se detecta solo).
    //  · Combate: KaelSpear, KaelShooter y ArrowHit llaman a SpearThrust / BowShot / Impact.
    //  · Bloqueo y reparación de Kiro: se disparan al entrar en esos estados del Animator.
    // Se agrega solo a Kael al cargar la escena (igual que KaelShooter).
    [DefaultExecutionOrder(60)]
    [RequireComponent(typeof(PlayerController))]
    public sealed class KaelVFX : MonoBehaviour
    {
        private readonly struct Sheet
        {
            public readonly string name; public readonly int frames; public readonly float fps;
            public readonly float size;   // ancho de un cuadro, en "alturas de Kael"
            public readonly Vector2 pivot; public readonly int order;
            public Sheet(string name, int frames, float fps, float size, float pivotX, float pivotY, int order)
            {
                this.name = name; this.frames = frames; this.fps = fps; this.size = size;
                pivot = new Vector2(pivotX, pivotY); this.order = order;
            }
        }

        // Mismo orden que el enum KaelFx
        private static readonly Sheet[] Sheets =
        {
            new Sheet("polvo_pasos",         6, 16f, 0.95f, 0.50f, 0.04f, 1),
            new Sheet("polvo_salto",         5, 14f, 1.45f, 0.50f, 0.10f, 1),
            new Sheet("polvo_aterrizaje",    6, 14f, 1.50f, 0.50f, 0.05f, 1),
            new Sheet("polvo_deslizamiento", 6, 14f, 1.25f, 0.50f, 0.04f, 1),
            new Sheet("embestida_lanza",     8, 24f, 1.05f, 0.10f, 0.50f, 2),
            new Sheet("disparo_flecha",      6, 20f, 1.15f, 0.30f, 0.50f, 2),
            new Sheet("impacto_general",     5, 18f, 1.20f, 0.50f, 0.50f, 3),
            new Sheet("impacto_tecnologico", 5, 18f, 1.40f, 0.50f, 0.50f, 3),
            new Sheet("choque_bloqueo",      5, 18f, 1.25f, 0.50f, 0.50f, 2),
            new Sheet("kiro_reparacion",     6, 12f, 1.25f, 0.50f, 0.50f, 2),
        };

        private static readonly Sprite[][] Cache = new Sprite[Sheets.Length][];
        private static readonly int BlockState = Animator.StringToHash("Base Layer.Bloquear");
        private static readonly int RepairState = Animator.StringToHash("Base Layer.Reparar_Kiro");
        private static readonly int RollState = Animator.StringToHash("Base Layer.Rodar");
        private static KaelVFX current;
        private static float unit = 1.8f; // altura de Kael en unidades de mundo

        [SerializeField, Min(0.05f)] private float stepInterval = 0.26f;
        [SerializeField, Min(0f)] private float runSpeed = 4f;      // velocidad mínima para levantar polvo al correr
        [SerializeField, Min(0f)] private float dashSpeed = 13f;    // por encima de esto se considera dash/rodada
        [SerializeField, Min(0f)] private float landSpeed = 6f;     // caída mínima para el polvo de aterrizaje

        private PlayerController player;
        private Rigidbody2D body;
        private Collider2D col;
        private Animator animator;
        private SpriteRenderer visual;
        private bool wasGrounded = true, wasSliding;
        private float airVy, stepTimer;
        private Vector3 lastFeet;
        private int lastState;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            AddToPlayers();
            SceneManager.sceneLoaded += (_, __) => AddToPlayers();
        }

        private static void AddToPlayers()
        {
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (pc.GetComponent<KaelVFX>() == null) pc.gameObject.AddComponent<KaelVFX>();
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            body = GetComponent<Rigidbody2D>();
            col = GetComponent<CapsuleCollider2D>();
            animator = GetComponentInChildren<Animator>();
            visual = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            current = this;
            if (col != null && col.bounds.size.y > 0.1f) unit = col.bounds.size.y;
            lastFeet = Feet();
        }

        private void OnDisable() { if (current == this) current = null; }

        private Vector3 Feet()
        {
            if (col == null) return transform.position;
            Bounds b = col.bounds;
            return new Vector3(b.center.x, b.min.y, 0f);
        }

        private void Update()
        {
            if (body == null) return;
            bool grounded = player.IsGrounded;
            Vector2 velocity = body.linearVelocity;
            Vector3 feet = Feet();
            float facing = player.Facing;

            // Salto y aterrizaje
            if (!grounded) airVy = velocity.y;
            if (grounded != wasGrounded && player.CanAct)
            {
                if (!grounded && velocity.y > 2f) Play(KaelFx.PolvoSalto, lastFeet);
                else if (grounded && airVy < -landSpeed) Play(KaelFx.PolvoAterrizaje, feet);
            }
            if (grounded) lastFeet = feet;
            wasGrounded = grounded;

            // Estados del Animator: rodar, bloquear, reparar a Kiro
            int state = animator != null ? animator.GetCurrentAnimatorStateInfo(0).fullPathHash : 0;
            bool entered = state != lastState;
            lastState = state;
            if (entered && state == BlockState)
                Play(KaelFx.ChoqueBloqueo, Front(0.42f, 0.55f), follow: transform);
            else if (entered && state == RepairState)
                Play(KaelFx.KiroReparacion, Front(0.45f, 0.45f), flipX: facing < 0f, follow: transform);

            // Deslizamiento / rodada (dash a ras de suelo)
            bool sliding = grounded && (Mathf.Abs(velocity.x) > dashSpeed || state == RollState);
            if (sliding && !wasSliding)
                Play(KaelFx.PolvoDeslizamiento, feet - new Vector3(facing * 0.2f * unit, 0f, 0f), flipX: facing > 0f);
            wasSliding = sliding;

            // Pasos al correr (el polvo queda detrás de Kael)
            bool running = grounded && !sliding && player.CanAct && Mathf.Abs(velocity.x) > runSpeed;
            if (!running) { stepTimer = Mathf.Min(stepTimer, 0.08f); return; }
            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f) return;
            stepTimer = stepInterval;
            float dir = Mathf.Sign(velocity.x);
            Play(KaelFx.PolvoPasos, feet - new Vector3(dir * 0.22f * unit, 0f, 0f), flipX: dir > 0f);
        }

        // Punto frente a Kael: forward y height en "alturas de Kael"
        private Vector3 Front(float forward, float height)
        {
            Vector3 feet = Feet();
            return new Vector3(feet.x + player.Facing * forward * unit, feet.y + height * unit, 0f);
        }

        // ---------- Llamadas desde el combate ----------

        /// <summary>Estela de la embestida de lanza; sigue a Kael mientras dura.</summary>
        public static void SpearThrust(Transform kael, Vector2 origin, Vector2 direction) =>
            PlayDirected(KaelFx.EmbestidaLanza, origin, direction, kael);

        /// <summary>Destello al soltar la flecha, en la punta del arco.</summary>
        public static void BowShot(Vector2 origin, Vector2 direction) =>
            PlayDirected(KaelFx.DisparoFlecha, origin, direction, null);

        /// <summary>Impacto en el punto de golpe: tecnológico contra robots, general contra lo demás.</summary>
        public static void Impact(Vector2 point, Component target)
        {
            bool tech = target != null && (target.GetComponentInParent<RobotPatrol>() != null
                || target.GetComponentInParent<CentinelaVisual>() != null
                || target.GetComponentInParent<CentinelaRex>() != null);
            Play(tech ? KaelFx.ImpactoTecnologico : KaelFx.ImpactoGeneral, point);
        }

        public static KaelVfxClip PlayDirected(KaelFx fx, Vector2 position, Vector2 direction, Transform follow)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Play(fx, position, angle, false, follow, direction.x < -0.01f); // hacia la izquierda no queda de cabeza
        }

        public static KaelVfxClip Play(KaelFx fx, Vector3 position, float angle = 0f, bool flipX = false,
            Transform follow = null, bool flipY = false)
        {
            Sprite[] frames = Frames(fx);
            if (frames == null) return null;
            Sheet sheet = Sheets[(int)fx];
            var go = new GameObject("VFX_" + sheet.name);
            position.z = 0f;
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            float size = sheet.size * unit;
            go.transform.localScale = new Vector3(flipX ? -size : size, flipY ? -size : size, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = 10 + sheet.order;
            if (current != null && current.visual != null)
            {
                sr.sortingLayerID = current.visual.sortingLayerID;
                sr.sortingOrder = current.visual.sortingOrder + sheet.order;
                sr.sharedMaterial = current.visual.sharedMaterial;
            }
            var clip = go.AddComponent<KaelVfxClip>();
            clip.Init(sr, frames, sheet.fps, follow);
            return clip;
        }

        private static Sprite[] Frames(KaelFx fx)
        {
            int index = (int)fx;
            Sprite[] cached = Cache[index];
            if (cached != null && cached.Length > 0 && cached[0] != null) return cached;
            Sheet sheet = Sheets[index];
            var texture = Resources.Load<Texture2D>("KaelVFX/" + sheet.name);
            if (texture == null)
            {
                Debug.LogWarning("KaelVFX: falta la hoja Assets/Resources/KaelVFX/" + sheet.name + ".png");
                return null;
            }
            texture.filterMode = FilterMode.Point;
            float width = (float)texture.width / sheet.frames; // cada cuadro mide 1 unidad de ancho
            var frames = new Sprite[sheet.frames];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = Sprite.Create(texture, new Rect(i * width, 0f, width, texture.height), sheet.pivot, width,
                    0, SpriteMeshType.FullRect);
            Cache[index] = frames;
            return frames;
        }
    }

    // Reproduce una hoja una sola vez y se destruye.
    public sealed class KaelVfxClip : MonoBehaviour
    {
        private SpriteRenderer target;
        private Sprite[] frames;
        private Transform follow;
        private Vector3 offset;
        private float fps = 12f, time;

        public void Init(SpriteRenderer renderer, Sprite[] sprites, float framesPerSecond, Transform followTarget)
        {
            target = renderer; frames = sprites; fps = framesPerSecond; follow = followTarget;
            if (follow != null) offset = transform.position - follow.position;
        }

        private void LateUpdate()
        {
            time += Time.deltaTime;
            int index = (int)(time * fps);
            if (frames == null || index >= frames.Length) { Destroy(gameObject); return; }
            target.sprite = frames[index];
            if (follow != null) transform.position = follow.position + offset;
        }
    }
}
