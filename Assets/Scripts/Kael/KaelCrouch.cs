using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    /// <summary>
    /// Agacharse: en el suelo, mantener ABAJO (stick, cruceta, S o flecha abajo).
    ///  · El cuerpo de Kael se hace bajito para pasar por grietas (la entrada de la cueva del Nivel 1).
    ///  · Avanza despacio mientras está agachado.
    ///  · Si tiene techo encima no se puede levantar ni saltar: sigue agachado hasta salir.
    /// La animación es Kael_Agacharse (parámetro IsCrouching del Animator, lo pone KaelAnimationDriver).
    /// </summary>
    [DefaultExecutionOrder(50)] // después de PlayerController: limita la velocidad que él calculó
    [RequireComponent(typeof(PlayerController), typeof(CapsuleCollider2D))]
    public sealed class KaelCrouch : MonoBehaviour
    {
        [SerializeField, Range(0.3f, 0.9f)] private float crouchHeight = 0.55f;   // fracción de la altura normal
        [SerializeField] private float crawlSpeed = 2.4f;
        [SerializeField] private float stickThreshold = 0.6f;
        [SerializeField] private LayerMask terrain = 1 << 6;

        private PlayerController player;
        private CapsuleCollider2D body;
        private Rigidbody2D rb;
        private Vector2 standSize, standOffset;
        public bool Crouched { get; private set; }
        /// <summary>Hay techo encima: no se puede levantar.</summary>
        public bool Blocked { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= Loaded;
            SceneManager.sceneLoaded += Loaded;
            Add();
        }

        private static void Loaded(Scene s, LoadSceneMode m) => Add();

        private static void Add()
        {
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (!pc.GetComponent<KaelCrouch>() && pc.GetComponent<CapsuleCollider2D>()) pc.gameObject.AddComponent<KaelCrouch>();
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            body = GetComponent<CapsuleCollider2D>();
            rb = GetComponent<Rigidbody2D>();
            standSize = body.size;
            standOffset = body.offset;
        }

        private bool WantsCrouch => player.CanAct && player.IsGrounded && !player.IsDashing && player.MoveInput.y < -stickThreshold;

        private void FixedUpdate()
        {
            Blocked = Crouched && !RoomToStand();
            bool crouch = WantsCrouch || Blocked;
            if (crouch != Crouched) Set(crouch);
            if (!Crouched) return;

            Vector2 v = rb.linearVelocity;
            v.x = Mathf.Clamp(v.x, -crawlSpeed, crawlSpeed);
            if (Blocked && v.y > 0f) v.y = 0f;            // con techo encima no salta
            rb.linearVelocity = v;
        }

        private void Set(bool crouch)
        {
            Crouched = crouch;
            if (crouch)
            {
                float h = standSize.y * crouchHeight;
                body.size = new Vector2(standSize.x, Mathf.Max(standSize.x, h));
                body.offset = new Vector2(standOffset.x, body.size.y * 0.5f);   // los pies se quedan en su lugar
            }
            else
            {
                body.size = standSize;
                body.offset = standOffset;
            }
        }

        // ¿Cabe Kael parado? Revisa el espacio que ocuparía el cuerpo completo, arriba del cuerpo agachado.
        private bool RoomToStand()
        {
            Vector3 scale = transform.lossyScale;
            float sx = Mathf.Abs(scale.x), sy = Mathf.Abs(scale.y);
            Vector2 feet = (Vector2)transform.position + new Vector2(standOffset.x * sx, 0f);
            float full = standSize.y * sy, low = body.size.y * sy;
            Vector2 size = new Vector2(standSize.x * sx * 0.9f, full - low - 0.02f);
            if (size.y <= 0.01f) return true;
            Vector2 center = feet + Vector2.up * (low + size.y * 0.5f + 0.01f);
            foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f, terrain))
                if (hit != body && !hit.isTrigger && !hit.usedByEffector) return false;   // las ramas de un sentido no cuentan
            return true;
        }

        private void OnDisable()
        {
            if (Crouched && body != null) Set(false);
        }
    }
}
