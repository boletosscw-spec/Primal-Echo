using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // Arco: una flecha por disparo. Kael se queda quieto mientras tensa y suelta.
    //  · Normal: recto, diagonal arriba o arriba (en el aire también abajo).
    //  · Apuntado fijo (como en Cuphead): mantén el botón de fijar, Kael no se mueve y apuntas en 8 direcciones
    //    con el stick / flechas (en el suelo, todo menos recto hacia abajo). Se muestra una mira.
    // Teclado: K / X / clic derecho = disparar · Ctrl = fijar apuntado
    // Control: Y = disparar · LT = fijar apuntado
    [DefaultExecutionOrder(50)]
    [RequireComponent(typeof(PlayerController))]
    public sealed class KaelShooter : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float drawSeconds = 0.45f;    // tensar el arco (quieto): coincide con el cuadro de tensado completo
        [SerializeField, Min(0f)] private float recoverSeconds = 0.18f; // después de soltar (quieto)
        [SerializeField, Min(0f)] private float cooldown = 0.55f;       // entre flechas
        [SerializeField, Min(1)] private int damage = 2;
        [SerializeField, Min(1f)] private float arrowSpeed = 28f;
        [SerializeField, Min(0.1f)] private float arrowScale = 1.8f;
        [Header("Punta del arco (unidades locales del sprite de Kael)")]
        [SerializeField] private Vector2 bowTipStraight = new Vector2(0.685f, 0.915f);
        [SerializeField] private Vector2 bowTipDiagonal = new Vector2(0.615f, 1.19f);
        [SerializeField] private Vector2 bowTipDown = new Vector2(0.45f, 0.55f);

        private PlayerController player;
        private Rigidbody2D body;
        private Animator animator;
        private KaelAnimationDriver driver;
        private InputAction shoot, aimLock;
        private Transform reticle;
        private SpriteRenderer reticleSprite;
        private float drawEnds = -1f, rootedUntil = -1f, nextShot;
        private Vector2 aim;
        private bool drawing;

        public void AddDamage(int amount) => damage += amount;
        public int Damage => damage;

        public bool Rooted => Time.time < rootedUntil;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            AddToPlayers();
            SceneManager.sceneLoaded += (_, __) => AddToPlayers();
        }

        private static void AddToPlayers()
        {
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (pc.GetComponent<KaelShooter>() == null) pc.gameObject.AddComponent<KaelShooter>();
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            driver = GetComponentInChildren<KaelAnimationDriver>();
            shoot = new InputAction("Disparar", InputActionType.Button);
            shoot.AddBinding("<Keyboard>/k");
            shoot.AddBinding("<Keyboard>/x");
            shoot.AddBinding("<Mouse>/rightButton");
            shoot.AddBinding("<Gamepad>/buttonNorth");
            aimLock = new InputAction("FijarApuntado", InputActionType.Button);
            aimLock.AddBinding("<Keyboard>/leftCtrl");
            aimLock.AddBinding("<Keyboard>/rightCtrl");
            aimLock.AddBinding("<Gamepad>/leftTrigger");
            BuildReticle();
        }

        private void OnEnable() { shoot.Enable(); aimLock.Enable(); }
        private void OnDisable() { shoot.Disable(); aimLock.Disable(); if (reticle != null) reticle.gameObject.SetActive(false); }
        private void OnDestroy() { shoot.Dispose(); aimLock.Dispose(); if (reticle != null) Destroy(reticle.gameObject); }

        public bool AimLocked => aimLock != null && aimLock.IsPressed() && player.CanAct;

        private void Update()
        {
            UpdateReticle();
            if (!player.CanAct) { drawing = false; return; }
            if (!drawing && shoot.WasPressedThisFrame() && Time.time >= nextShot)
            {
                drawing = true;
                aim = AimDirection();
                drawEnds = Time.time + drawSeconds;
                rootedUntil = drawEnds + recoverSeconds;
                nextShot = Time.time + cooldown;
                if (animator != null) animator.SetTrigger(aim.y > 0.3f ? "AttackBowDiagonal" : "AttackBow");
            }
            if (drawing)
            {
                aim = AimDirection(); // se puede corregir la puntería mientras tensa
                if (Time.time >= drawEnds) { drawing = false; Release(aim); }
            }
        }

        private void FixedUpdate()
        {
            // Quieto mientras dispara o mientras fija el apuntado (en el aire solo se frena el avance horizontal)
            if ((Rooted || (AimLocked && player.IsGrounded)) && player.CanAct)
                body.linearVelocity = new Vector2(0f, Mathf.Min(body.linearVelocity.y, player.IsGrounded ? 0f : body.linearVelocity.y));
        }

        // La flecha sale de la punta del arco del sprite (respeta la escala y el giro de Kael)
        private Vector3 BowTip(Vector2 dir)
        {
            Vector2 local = dir.y > 0.3f ? bowTipDiagonal : (dir.y < -0.3f ? bowTipDown : bowTipStraight);
            if (Mathf.Abs(dir.x) < 0.1f) local.x = 0.15f; // recto hacia arriba/abajo
            var sr = player.GetComponentInChildren<SpriteRenderer>();
            Transform t = sr != null ? sr.transform : transform;
            if (sr != null && sr.flipX) local.x = -local.x;
            Vector3 world = t.TransformPoint(local);
            world.z = 0f;
            return world;
        }

        private Vector2 AimDirection()
        {
            Vector2 m = player.MoveInput;
            if (AimLocked)
            {
                // 8 direcciones como en Cuphead
                int ax = Mathf.Abs(m.x) > 0.35f ? (int)Mathf.Sign(m.x) : 0;
                int ay = Mathf.Abs(m.y) > 0.35f ? (int)Mathf.Sign(m.y) : 0;
                if (ax == 0 && ay == 0) ax = (int)player.Facing;
                if (player.IsGrounded && ay < 0 && ax == 0) ax = (int)player.Facing; // en el suelo no recto abajo
                return new Vector2(ax, ay).normalized;
            }
            float face = player.Facing;
            bool up = m.y > 0.35f, down = m.y < -0.35f && !player.IsGrounded;
            bool side = Mathf.Abs(m.x) > 0.35f;
            if (side) face = Mathf.Sign(m.x);
            if (up) return side ? new Vector2(face, 1f).normalized : (Mathf.Abs(m.x) < 0.15f ? Vector2.up : new Vector2(face, 1f).normalized);
            if (down) return side ? new Vector2(face, -1f).normalized : Vector2.down;
            return new Vector2(face, 0f);
        }

        private void BuildReticle()
        {
            // Mira: punta de flecha de 7x7 px
            var tex = new Texture2D(7, 7) { filterMode = FilterMode.Point };
            for (int x = 0; x < 7; x++)
                for (int y = 0; y < 7; y++)
                {
                    int fromTip = 6 - x;                    // la punta queda a la derecha (dirección 0°)
                    int arm = Mathf.Abs(y - 3) * 2;
                    tex.SetPixel(x, y, fromTip >= arm && fromTip - arm <= 1 ? Color.white : Color.clear);
                }
            tex.Apply();
            var go = new GameObject("Mira_Arco");
            reticle = go.transform;
            reticleSprite = go.AddComponent<SpriteRenderer>();
            reticleSprite.sprite = Sprite.Create(tex, new Rect(0, 0, 7, 7), new Vector2(0.5f, 0.5f), 10f);
            reticleSprite.color = new Color(1f, 0.95f, 0.6f, 0.85f);
            reticleSprite.sortingOrder = 30;
            var kaelSprite = GetComponentInChildren<SpriteRenderer>();
            if (kaelSprite != null) reticleSprite.sharedMaterial = kaelSprite.sharedMaterial;
            go.SetActive(false);
        }

        private void UpdateReticle()
        {
            if (reticle == null) return;
            bool show = AimLocked || drawing;
            reticle.gameObject.SetActive(show);
            if (!show) return;
            Vector2 dir = AimDirection();
            Vector3 tip = BowTip(dir);
            reticle.position = tip + (Vector3)(dir * 1.2f);
            reticle.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            reticleSprite.color = new Color(1f, 0.95f, 0.6f, 0.55f + 0.3f * Mathf.Sin(Time.time * 10f));
        }

        private void Release(Vector2 dir)
        {
            GameObject prefab = driver != null ? driver.ArrowPrefab : null;
            Vector3 origin = BowTip(dir);
            KaelVFX.BowShot(origin, dir);
            GameObject arrow;
            if (prefab != null) arrow = Instantiate(prefab, origin, Quaternion.identity);
            else
            {
                arrow = new GameObject("Flecha");
                arrow.transform.position = origin;
                arrow.AddComponent<Rigidbody2D>();
                arrow.AddComponent<BoxCollider2D>().isTrigger = true;
                arrow.AddComponent<ArrowProjectile>();
            }
            arrow.transform.localScale = arrow.transform.localScale * arrowScale;
            if (!arrow.TryGetComponent(out ArrowHit hit)) hit = arrow.AddComponent<ArrowHit>();
            hit.Init(dir, damage);
            var proj = arrow.GetComponent<ArrowProjectile>();
            if (proj != null) proj.Launch(dir);
            var rb = arrow.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * arrowSpeed;
        }
    }
}
