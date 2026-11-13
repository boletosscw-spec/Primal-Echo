using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    public enum CentinelaTipo { Explorador, AlaCian, AlaAmbar }

    // Centinelas biomecánicos (hojas en Assets/Resources/Centinelas/<tipo>/).
    //  · Explorador (CentinelaTerrestre): tricerátops blindado; acecha, embiste con los cuernos, cornea de cerca y barre con la cola.
    //  · Ala Ámbar (CentinelaVolador, cuerpo a cuerpo): ronda, se lanza en picada y escapa a toda velocidad.
    //  · Ala Cian (CentinelaVolador, a distancia): guarda distancia, dispara adelantando el tiro y se recoloca.
    // Al cargar la escena reemplaza a los enemigos antiguos (ver Convert): robots -> Explorador,
    // avispas -> voladores, plantas carnívoras -> se eliminan. Este componente lleva las animaciones.
    public sealed class CentinelaVisual : MonoBehaviour
    {
        private enum Anim { Idle, Move, Attack, Hurt }

        private const float PixelsPerUnit = 56f;   // celda de 128 px ≈ 2.3 unidades; el cuerpo mide ≈ 1.2
        private const float FlyerDrop = 0.54f;     // los voladores se dibujan centrados en su colisionador
        // TAMAÑO de los centinelas. Sube o baja estos números para hacerlos más grandes o más chicos.
        private const float GroundSize = 3.2f;     // terrestres: con 3.2 el cuerpo mide ≈ 6.7 x 3.5 unidades (antes 1.4)
        private const float FlyerSize = 2f;        // alados (antes 1.4)
        private const float MinSize = 1.4f;        // si no caben (techo bajo, pasillo estrecho) se encogen como mucho hasta aquí
        // VIDA de los centinelas (la lanza y las flechas quitan 2 sin mejoras).
        private const int GroundHealth = 10, EliteHealth = 18, FlyerHealth = 5;
        private const float EliteScale = 1.15f;    // los antiguos robots pesados son Exploradores aún más grandes
        private const int TerrainLayer = 6;
        private const int EnemySortingOrder = 8;   // la decoración del mapa está en 5: con menos de 6 el enemigo queda tapado (invisible)
        private static readonly string[] Folders = { "Explorador", "AlaCian", "AlaAmbar" };
        private static readonly float[] FeetPivot = { 0.23f, 0.25f, 0.19f };
        private static readonly float[] Fps = { 6f, 10f, 14f, 12f };
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();
        private static readonly HashSet<int> Reviewed = new HashSet<int>();
        private static bool quitting;

        [SerializeField] private CentinelaTipo tipo;
        private SpriteRenderer sprite;
        private EnemyHealth health;
        private EnemyFeedback feedback;
        private CentinelaTerrestre walker;
        private CentinelaVolador flyer;
        private Sprite[][] clips;
        private Sprite[] death;
        private Anim current;
        private float time, speed, scale = 1f;
        private Vector3 lastPosition, visualPosition;
        private bool ready, facingLeft;

        public CentinelaTipo Tipo => tipo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            quitting = false;
            Reviewed.Clear();
            Application.quitting += () => quitting = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            Convert();
            SceneManager.sceneLoaded += (_, __) => Convert();
        }

        // Reemplaza a todos los enemigos antiguos de la escena.
        private static void Convert()
        {
            foreach (var patrol in FindObjectsByType<RobotPatrol>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!Reviewed.Add(patrol.GetInstanceID())) continue;
                bool elite = patrol.name.StartsWith("Robot_Pesado");
                patrol.gameObject.AddComponent<CentinelaVisual>().Setup(CentinelaTipo.Explorador, elite);
            }

            var wasps = new List<WaspFlyer>();
            foreach (var wasp in FindObjectsByType<WaspFlyer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (Reviewed.Add(wasp.GetInstanceID())) wasps.Add(wasp);
            wasps.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
            for (int i = 0; i < wasps.Count; i++)
                wasps[i].gameObject.AddComponent<CentinelaVisual>()
                    .Setup(i % 2 == 0 ? CentinelaTipo.AlaCian : CentinelaTipo.AlaAmbar, false);

            foreach (var plant in FindObjectsByType<CarnivorousPlant>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Destroy(plant.gameObject);
        }

        public void Setup(CentinelaTipo type, bool elite)
        {
            tipo = type;
            health = GetComponent<EnemyHealth>();
            if (health != null) health.SetHealth(type != CentinelaTipo.Explorador ? FlyerHealth : elite ? EliteHealth : GroundHealth);
            feedback = GetComponent<EnemyFeedback>();
            sprite = GetComponentInChildren<SpriteRenderer>(true);
            bool ground = tipo == CentinelaTipo.Explorador;
            if (sprite == null) { enabled = false; return; }
            if (!ground)
            {
                var patrol = Load(tipo, "Patrulla");
                clips = new[] { Load(tipo, "Idle"), patrol, Load(tipo, "Ataque"), Load(tipo, "Dano") };
                death = Load(tipo, "Destruccion");
                if (clips[0] == null) { enabled = false; return; }
                sprite.sprite = clips[0][0];
            }

            // Fuera la IA y la animación de marcador de posición del enemigo antiguo
            foreach (var old in new Behaviour[] { GetComponent<SpriteFrames>(), GetComponent<RobotPatrol>(), GetComponent<WaspFlyer>() })
                if (old != null) { old.enabled = false; Destroy(old); }

            sprite.flipX = false;
            sprite.sortingOrder = EnemySortingOrder; // por encima de la decoración (orden 5) y por debajo de Kael (10)
            scale = ground ? FitGround((elite ? EliteScale : 1f) * GroundSize) : FlyerSize;
            sprite.transform.localScale = new Vector3(scale, scale, 1f);

            if (ground)
            {
                // Cuerpo bajo y ancho en lugar de la caja alta del robot
                Vector2 size = new Vector2(1.3f, 1.1f) * scale;
                foreach (var box in GetComponentsInChildren<BoxCollider2D>(true))
                {
                    box.size = box.isTrigger ? size * 0.9f : size;
                    box.offset = new Vector2(0f, size.y / 2f);
                }
                sprite.transform.localPosition = Vector3.zero;
                walker = gameObject.AddComponent<CentinelaTerrestre>();
                walker.Setup(sprite.transform, elite);
            }
            else
            {
                sprite.transform.localPosition = new Vector3(0f, -FlyerDrop * scale, 0f);
                foreach (var circle in GetComponents<CircleCollider2D>()) circle.radius = 0.55f * scale;
                flyer = gameObject.AddComponent<CentinelaVolador>();
                flyer.Setup(sprite, tipo == CentinelaTipo.AlaCian);
            }
            lastPosition = transform.position;
            visualPosition = sprite.transform.position;
            ready = true;
        }

        // Tamaño que cabe donde está colocado: mira el techo y las paredes para no quedar encajado en el mapa
        private float FitGround(float wanted)
        {
            int mask = 1 << TerrainLayer;
            Vector2 p = transform.position;
            float fit = wanted;
            var up = Physics2D.Raycast(p + Vector2.up * 0.15f, Vector2.up, 1.1f * wanted + 0.2f, mask);
            if (up.collider != null) fit = Mathf.Min(fit, (up.distance - 0.05f) / 1.1f);
            Vector2 mid = p + Vector2.up * 0.6f;
            float half = 0.65f * fit + 0.15f;
            var left = Physics2D.Raycast(mid, Vector2.left, half, mask);
            var right = Physics2D.Raycast(mid, Vector2.right, half, mask);
            if (left.collider != null && right.collider != null)
                fit = Mathf.Min(fit, (left.distance + right.distance - 0.3f) / 1.3f);
            else if (left.collider != null) transform.position += Vector3.right * (half - left.distance);   // pegado a una pared: se separa
            else if (right.collider != null) transform.position += Vector3.left * (half - right.distance);
            return Mathf.Max(MinSize, fit);
        }

        private void OnEnable() { lastPosition = transform.position; speed = 0f; }

        private void Update()
        {
            if (!ready || sprite == null) return;
            float dt = Time.deltaTime;
            if (dt > 0f) speed = Mathf.Lerp(speed, (transform.position - lastPosition).magnitude / dt, 0.25f);
            lastPosition = transform.position;
            visualPosition = sprite.transform.position;
            facingLeft = sprite.transform.lossyScale.x < 0f;

            if (walker != null) return;
            Anim next = feedback != null && feedback.Stunned ? Anim.Hurt
                : flyer != null && flyer.Attacking ? Anim.Attack
                : speed > 0.25f ? Anim.Move : Anim.Idle;
            Sprite[] frames = clips[(int)next] ?? clips[0];
            if (next != current)
            {
                if (frames != (clips[(int)current] ?? clips[0])) time = 0f; // misma hoja: no reiniciar
                current = next;
            }
            time += dt;

            float fps = Fps[(int)current];
            if (current == Anim.Attack) // la animación completa dura lo mismo que el ataque
                fps = frames.Length / Mathf.Max(0.1f, flyer.AttackSeconds);
            int index = (int)(time * fps);
            bool once = current == Anim.Attack || current == Anim.Hurt;
            sprite.sprite = frames[once ? Mathf.Min(index, frames.Length - 1) : index % frames.Length];
        }

        // Al morir deja los restos con la animación de destrucción
        private void OnDestroy()
        {
            if (!ready || quitting || (tipo != CentinelaTipo.Explorador && death == null) || !gameObject.scene.isLoaded) return;
            if (ReferenceEquals(health, null) || health.Remaining > 0) return;
            var go = new GameObject("Centinela_Restos");
            go.transform.position = visualPosition;
            go.transform.localScale = new Vector3(facingLeft ? -scale : scale, scale, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            if (death != null) renderer.sprite = death[0];
            if (sprite != null)
            {
                renderer.sortingLayerID = sprite.sortingLayerID;
                renderer.sortingOrder = sprite.sortingOrder - 1;
                renderer.sharedMaterial = sprite.sharedMaterial;
            }
            float ground = visualPosition.y;
            if (tipo != CentinelaTipo.Explorador)
            {
                var hit = Physics2D.Raycast((Vector2)visualPosition + Vector2.up * 0.2f, Vector2.down, 40f, 1 << TerrainLayer);
                if (hit.collider != null) ground = hit.point.y;
            }
            var remains = go.AddComponent<CentinelaRestos>();
            if (tipo == CentinelaTipo.Explorador)
            {
                var animator = CentinelaTerrestre.AddAnimator(go);
                animator.Play("Base Layer.Muerte", 0, 0f);
                animator.Update(0f);
                remains.InitAnimated(renderer, animator.GetCurrentAnimatorStateInfo(0).length, ground);
            }
            else remains.Init(renderer, death, 10f, ground);
        }

        private static Sprite[] Load(CentinelaTipo type, string clip)
        {
            string key = Folders[(int)type] + "/" + clip;
            if (Cache.TryGetValue(key, out Sprite[] cached) && cached.Length > 0 && cached[0] != null) return cached;
            var texture = Resources.Load<Texture2D>("Centinelas/" + key);
            if (texture == null)
            {
                Debug.LogWarning("Centinelas: falta la hoja Assets/Resources/Centinelas/" + key + ".png");
                return null;
            }
            texture.filterMode = FilterMode.Point;
            int count = Mathf.Max(1, Mathf.RoundToInt((float)texture.width / texture.height)); // celdas cuadradas
            float width = (float)texture.width / count;
            float ppu = PixelsPerUnit * texture.height / 128f;
            var pivot = new Vector2(0.5f, FeetPivot[(int)type]);
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Sprite.Create(texture, new Rect(i * width, 0f, width, texture.height), pivot, ppu,
                    0, SpriteMeshType.FullRect);
            Cache[key] = frames;
            return frames;
        }
    }

    // Centinela terrestre (tricerátops biomecánico): pesado y blindado; pelea con los cuernos y la cola.
    //  · Al ver a Kael RUGE y lo ACECHA de frente: busca distancia para tomar carrera (retrocede si lo tiene encima).
    //  · EMBESTIDA: aviso con los cristales en ámbar, siguiendo a Kael con la mirada, y carrera larga con los cuernos.
    //    No se le frena a golpes: hay que saltarla o esquivarla. Si choca contra un muro queda ATURDIDO (ventana para pegarle).
    //  · CORNADA: si Kael está pegado a su cara, golpe corto de cuernos.
    //  · COLETAZO: si Kael se le pone detrás (o al frenar después de rebasarlo), barrido bajo con la cola; se esquiva saltando.
    //  · Castiga el arco: si Kael se planta a tensar, embiste antes y avisa menos.
    //  · Si Kael se sube donde no llega, se aparta de debajo y espera a tener carrera.
    //  · Tarda un instante en girarse; al recibir golpes contraataca con cuernos o cola según dónde esté Kael.
    //  · Si le disparan desde lejos, va a buscar a Kael. Los pesados, con media vida, encadenan dos embestidas.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(EnemyHealth))]
    public sealed class CentinelaTerrestre : MonoBehaviour
    {
        private enum State { Patrol, Roar, Stalk, Windup, Charge, Brake, GoreWindup, Gore, Tail, WallStun, Recover }

        [SerializeField, Min(0f)] private float walkSpeed = 2.2f;
        [SerializeField, Min(0f)] private float runSpeed = 6f;
        [SerializeField, Min(0f)] private float backSpeed = 3.2f;
        [SerializeField, Min(0f)] private float sightRange = 12f;
        [SerializeField, Min(0f)] private float turnDelay = 0.28f;
        [Header("Embestida")]
        [SerializeField, Min(0f)] private float chargeMinGap = 3.5f;    // carrera mínima (borde a borde) para embestir
        [SerializeField, Min(0f)] private float chargeIdealGap = 6f;    // distancia a la que le gusta acechar
        [SerializeField, Min(0f)] private float windupSeconds = 0.6f;
        [SerializeField, Min(0f)] private float chargeSpeed = 13.5f;
        [SerializeField, Min(0.1f)] private float chargeMaxSeconds = 1.1f;
        [SerializeField] private Vector2 chargeCooldown = new Vector2(2.2f, 3.2f);
        [Header("Cornada")]
        [SerializeField, Min(0.1f)] private float goreRange = 1.3f;
        [SerializeField, Min(0f)] private float goreWindup = 0.32f;
        [SerializeField, Min(0.05f)] private float goreSeconds = 0.28f;
        [SerializeField, Min(0f)] private float goreSpeed = 7f;
        [Header("Coletazo")]
        [SerializeField, Min(0f)] private float tailRange = 2.4f;
        [Header("Pausas")]
        [SerializeField, Min(0f)] private float wallStunSeconds = 1.8f;
        [SerializeField, Min(0f)] private float brakeSeconds = 0.28f;
        [SerializeField, Min(0f)] private float recoverSeconds = 1.3f;   // quieto y sin defensa después de cada ataque
        [SerializeField] private LayerMask groundLayers = 1 << 6;

        private Rigidbody2D body;
        private BoxCollider2D box;
        private EnemyFeedback feedback;
        private Transform visual, player;
        private Collider2D playerCollider;
        private KaelShooter playerShooter;
        private State state = State.Patrol;
        private int direction = -1, attackDir = 1, chain;
        private float timer, duration = 1f, elapsed, hopUntil;
        private float nextAttack, nextRoar, counterAt, alertUntil, behindTime, crowded, lostTime;
        private bool grounded = true, wasHit;

        private const float RoarSeconds = 0.5f, TailSeconds = 0.6f;
        private Animator animator;
        private BoxCollider2D attackBox;
        private Vector2 contactSize, contactOffset;
        private EnemyHealth health;
        private int initialHealth;
        private bool heavy, hitWindow;
        public bool Heavy => heavy;
        private float stunEnds, hitStart;
        private int playing;
        private static readonly int Idle = Clip("Idle"), Walk = Clip("Caminar"), Run = Clip("Correr"),
            Warn = Clip("Aviso"), ChargeClip = Clip("Embestida"), TailClip = Clip("Coletazo"), Hurt = Clip("Dano"),
            RoarClip = Clip("Rugido"), Stun = Clip("Aturdido"), Back = Clip("Retroceder"), Red = Clip("Fase2_Roja");
        private static int Clip(string name) => Animator.StringToHash("Base Layer." + name);

        internal static Animator AddAnimator(GameObject target)
        {
            var animator = target.AddComponent<Animator>();
            animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("CentinelaTerrestre/Terrestre");
            animator.applyRootMotion = false;
            return animator;
        }

        private float Progress => 1f - Mathf.Clamp01(timer / duration);
        private bool Armored => state == State.Windup || state == State.Charge; // en plena embestida no se le interrumpe
        private bool Enraged => heavy && health != null && health.Remaining * 2 <= initialHealth;

        private void LateUpdate()
        {
            if (animator == null) return;
            int clip;
            float progress = -1f;
            if (feedback != null && feedback.Stunned && !Armored)
            {
                if (feedback.StunnedUntil != stunEnds) { stunEnds = feedback.StunnedUntil; hitStart = Time.time; }
                float t = Mathf.InverseLerp(hitStart, stunEnds, Time.time);
                clip = t < 0.5f ? Hurt : Stun;
                progress = t < 0.5f ? t * 2f : (t - 0.5f) * 2f;
            }
            else
            {
                switch (state)
                {
                    case State.Roar: clip = RoarClip; progress = Progress; break;
                    case State.Windup: case State.GoreWindup: clip = Warn; progress = Progress; break;
                    // Carrera: alterna los dos cuadros de cabeza baja; al frenar, el último
                    case State.Charge: clip = ChargeClip; progress = (int)(elapsed / 0.08f) % 2 == 0 ? 0.3f : 0.6f; break;
                    case State.Brake: clip = ChargeClip; progress = 0.85f; break;
                    case State.Gore: clip = ChargeClip; progress = Progress; break;
                    case State.Tail: clip = TailClip; progress = Progress; break;
                    case State.WallStun: clip = Stun; progress = Mathf.Repeat(elapsed * 3f, 1f); break;
                    default:
                        float vx = body.linearVelocity.x;
                        if (Mathf.Abs(vx) < 0.1f) clip = Enraged ? Red : Idle;
                        else if (state == State.Stalk && vx * visual.localScale.x < 0f) clip = Back;
                        else clip = Mathf.Abs(vx) > walkSpeed + 0.2f ? Run : Walk;
                        break;
                }
            }
            animator.speed = progress >= 0f ? 0f : 1f;
            if (clip != playing || progress >= 0f)
            {
                animator.Play(clip, 0, Mathf.Clamp(progress, 0f, 0.99f));
                animator.Update(0f);
                playing = clip;
            }
        }

        public bool Attacking => state == State.Windup || state == State.Charge || state == State.GoreWindup
            || state == State.Gore || state == State.Tail;
        public bool Airborne => !grounded;
        public float VerticalSpeed => body != null ? body.linearVelocity.y : 0f;
        public float AttackSeconds => windupSeconds + chargeMaxSeconds;

        public void Setup(Transform bodyVisual, bool elite)
        {
            visual = bodyVisual;
            animator = AddAnimator(bodyVisual.gameObject);
            heavy = elite;
            health = GetComponent<EnemyHealth>();
            initialHealth = health.Remaining;
            var hazard = GetComponentInChildren<ContactHazard>(true);
            if (hazard != null)
            {
                hazard.CanDealDamage = () => hitWindow && isActiveAndEnabled && (feedback == null || !feedback.Stunned || Armored);
                attackBox = hazard.GetComponent<BoxCollider2D>();
                if (attackBox != null) { contactSize = attackBox.size; contactOffset = attackBox.offset; }
            }
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            feedback = GetComponent<EnemyFeedback>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.mass = elite ? 10f : 6f; // grande: los golpes lo empujan menos
            if (elite) { runSpeed *= 0.85f; chargeSpeed *= 0.92f; windupSeconds *= 1.15f; } // el pesado pega más pero es algo más lento
        }

        private void OnEnable()
        {
            state = State.Patrol; timer = 0f; duration = 1f; elapsed = 0f;
            wasHit = false; grounded = true; hitWindow = false; playing = 0;
            behindTime = crowded = lostTime = 0f; chain = 0;
        }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged == null) return;
            player = tagged.transform;
            playerCollider = tagged.GetComponent<Collider2D>();
            playerShooter = tagged.GetComponent<KaelShooter>();
            // No se empujan físicamente; el daño va por la caja de golpe
            if (box != null)
                foreach (var c in tagged.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(box, c);
        }

        private void Begin(State next, float seconds)
        {
            state = next; timer = duration = Mathf.Max(0.01f, seconds); elapsed = 0f; crowded = 0f; behindTime = 0f;
        }

        private bool FloorAt(float x, Bounds b) =>
            Physics2D.Raycast(new Vector2(x, b.min.y + 0.3f), Vector2.down, 1.3f, groundLayers);

        private bool FloorAhead(int dir, Bounds b) => FloorAt(b.center.x + dir * (b.extents.x + 0.2f), b);

        private bool WallAhead(int dir, Bounds b) =>
            Physics2D.Raycast(b.center, Vector2.right * dir, b.extents.x + 0.15f, groundLayers);

        private bool CanRun(int dir, Bounds b) => FloorAhead(dir, b) && !WallAhead(dir, b);

        // ¿Hay camino despejado hasta Kael para embestir?
        private bool LaneClear(int dir, Bounds b, float distance) =>
            !Physics2D.Raycast(b.center, Vector2.right * dir, b.extents.x + Mathf.Max(0.5f, distance), groundLayers);

        private void Hop(float vx, float vy)
        {
            body.linearVelocity = new Vector2(vx, vy);
            direction = vx >= 0f ? 1 : -1;
            hopUntil = Time.time + 0.15f;
            grounded = false;
            Face(direction);
        }

        // Salta un hueco o un escalón bajo si hay donde caer
        private bool TryJumpObstacle(int dir, float speed, Bounds b)
        {
            if (WallAhead(dir, b))
            {
                Vector2 above = new Vector2(b.center.x, b.max.y + 0.9f);
                if (Physics2D.Raycast(above, Vector2.right * dir, b.extents.x + 0.8f, groundLayers)) return false; // muro alto
                Hop(dir * speed * 0.7f, 9.5f);
                return true;
            }
            float reach = b.extents.x + 2.4f;
            if (!FloorAhead(dir, b) && FloorAt(b.center.x + dir * reach, b)
                && !Physics2D.Raycast(b.center, Vector2.right * dir, reach, groundLayers))
            {
                Hop(dir * Mathf.Max(speed, 6f), 8.5f);
                return true;
            }
            return false;
        }

        private void OpenWindow(int side, float reach, bool low)
        {
            hitWindow = true;
            if (attackBox == null) return;
            float width = reach * box.size.x;
            float height = contactSize.y * (low ? 0.55f : 1f);
            attackBox.size = new Vector2(contactSize.x + width, height);
            attackBox.offset = new Vector2(contactOffset.x + side * width / 2f, height / 2f);
        }

        private void Face(int dir)
        {
            if (visual == null) return;
            Vector3 s = visual.localScale;
            s.x = Mathf.Abs(s.x) * (dir > 0 ? 1f : -1f); // el sprite mira a la derecha
            visual.localScale = s;
        }

        private static void Shake(Vector2 direction)
        {
            if (Camera.main && Camera.main.TryGetComponent(out CameraFollow2D camera)) camera.Shake(direction);
        }

        private void FixedUpdate()
        {
            if (body == null) return;
            float dt = Time.fixedDeltaTime;
            hitWindow = false;
            if (attackBox != null) { attackBox.size = contactSize; attackBox.offset = contactOffset; }
            Bounds b = box.bounds;
            grounded = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.2f, groundLayers);
            bool armored = Armored;
            if (feedback != null && feedback.Stunned)
            {
                alertUntil = Time.time + 5f;
                if (!armored) { wasHit = true; return; } // deja actuar el retroceso
            }
            if (Time.time < hopUntil || (!grounded && !armored)) return;

            float dx = 0f, dy = 0f, gap = 99f; bool sees = false;
            if (player != null)
            {
                dx = player.position.x - b.center.x;
                dy = player.position.y - b.min.y;
                float playerHalf = playerCollider != null ? playerCollider.bounds.extents.x : 0.25f;
                gap = Mathf.Abs(dx) - b.extents.x - playerHalf;
                sees = (Mathf.Abs(dx) < sightRange && dy > -4f && dy < b.size.y + 4f) || Time.time < alertUntil;
            }
            int toPlayer = dx >= 0f ? 1 : -1;
            bool reachable = dy > -1.5f && dy < b.size.y + 0.4f; // Kael está a su altura (no subido donde no llega)
            timer -= dt;
            elapsed += dt;
            float vx = 0f;

            // Acaba de recibir un golpe: contraataca según dónde esté Kael (no siempre, para dejar castigarlo)
            if (wasHit)
            {
                wasHit = false;
                if (state == State.WallStun || state == State.Recover) { /* sigue quieto: es la ventana para pegarle, no contraataca */ }
                else if (sees && reachable && gap < tailRange && Time.time >= counterAt)
                {
                    counterAt = Time.time + 2.5f;
                    attackDir = direction;
                    if (toPlayer != direction) Begin(State.Tail, TailSeconds);
                    else Begin(State.GoreWindup, goreWindup * 0.7f);
                }
                else if (state == State.Patrol || state == State.Roar) { direction = toPlayer; Begin(State.Stalk, 0f); }
                else if (state != State.Stalk) Begin(State.Recover, recoverSeconds * 0.5f);
            }

            switch (state)
            {
                case State.Patrol:
                    if (!CanRun(direction, b)) direction = -direction;
                    vx = direction * walkSpeed;
                    if (sees)
                    {
                        direction = toPlayer; vx = 0f; lostTime = 0f;
                        nextAttack = Mathf.Max(nextAttack, Time.time + 0.35f);
                        if (Time.time >= nextRoar) { nextRoar = Time.time + 8f; Begin(State.Roar, RoarSeconds); }
                        else Begin(State.Stalk, 0f);
                    }
                    break;

                case State.Roar:
                    direction = toPlayer;
                    if (timer <= 0f) Begin(State.Stalk, 0f);
                    break;

                case State.Stalk: // de frente a Kael, buscando la distancia para su mejor ataque
                    if (!sees)
                    {
                        lostTime += dt;
                        if (lostTime > 1.5f) state = State.Patrol;
                        break;
                    }
                    lostTime = 0f;
                    if (toPlayer != direction)
                    {
                        // Kael está detrás: si está a tiro, coletazo; si no, tarda un instante en girarse
                        if (reachable && gap < tailRange) { attackDir = direction; Begin(State.Tail, TailSeconds); break; }
                        behindTime += dt;
                        if (behindTime >= turnDelay) { direction = toPlayer; behindTime = 0f; }
                        break;
                    }
                    behindTime = 0f;
                    if (!reachable)
                    {
                        // Kael está arriba, fuera de su alcance: no se queda debajo; se aparta para tener carrera cuando baje
                        if (gap < chargeMinGap && CanRun(-direction, b)) vx = -direction * backSpeed;
                        break;
                    }
                    bool ready = Time.time >= nextAttack;
                    bool bow = playerShooter != null && playerShooter.Rooted; // plantado tensando el arco
                    if (ready && (gap >= chargeMinGap || (bow && gap > goreRange + 0.5f)) && LaneClear(direction, b, gap))
                    {
                        attackDir = direction;
                        chain = Enraged ? 1 : 0;
                        Begin(State.Windup, windupSeconds * (bow ? 0.75f : 1f));
                        break;
                    }
                    if (gap <= goreRange)
                    {
                        if (ready) { attackDir = direction; Begin(State.GoreWindup, goreWindup); }
                        break; // entre ataques se queda plantado: no huye de Kael
                    }
                    if (gap < chargeMinGap)
                    {
                        // Demasiado cerca para embestir: retrocede para tomar carrera; si Kael lo acosa
                        // o está acorralado, va a por la cornada
                        crowded += dt;
                        // Espera quieto un momento; si Kael sigue encima, se acerca para la cornada
                        if (crowded >= 1.6f && CanRun(direction, b)) vx = direction * walkSpeed;
                        break;
                    }
                    crowded = 0f;
                    if (gap > chargeIdealGap)
                    {
                        if (CanRun(direction, b)) vx = direction * (gap > chargeIdealGap + 3f ? runSpeed : walkSpeed);
                        else if (TryJumpObstacle(direction, runSpeed, b)) return;
                    }
                    break;

                case State.Windup: // aviso: sigue a Kael con la mirada y fija la dirección al final
                    if (Progress < 0.65f && toPlayer != attackDir && gap > 0.3f) attackDir = toPlayer;
                    direction = attackDir;
                    if (timer <= 0f) Begin(State.Charge, chargeMaxSeconds);
                    break;

                case State.Charge: // carrera larga con los cuernos por delante; no se le puede frenar
                    direction = attackDir;
                    OpenWindow(attackDir, 0.45f, false);
                    vx = attackDir * chargeSpeed;
                    bool stalled = elapsed > 0.15f && Mathf.Abs(body.linearVelocity.x) < 1f;
                    bool passed = dx * attackDir < -(b.extents.x + 1.2f); // ya rebasó a Kael
                    if (WallAhead(attackDir, b) || stalled)
                    {
                        // Se estrella: queda aturdido y es el momento de pegarle
                        hitWindow = false; vx = 0f;
                        Shake(Vector2.right * attackDir);
                        nextAttack = Time.time + wallStunSeconds + 0.8f;
                        Begin(State.WallStun, wallStunSeconds);
                    }
                    else if (passed || timer <= 0f || !FloorAhead(attackDir, b))
                    {
                        vx = FloorAhead(attackDir, b) ? vx * 0.5f : 0f;
                        Begin(State.Brake, brakeSeconds);
                    }
                    break;

                case State.Brake: // derrapa al frenar
                    direction = attackDir;
                    if (CanRun(attackDir, b)) vx = attackDir * chargeSpeed * 0.5f * Mathf.Clamp01(timer / duration);
                    if (timer <= 0f)
                    {
                        nextAttack = Time.time + Random.Range(chargeCooldown.x, chargeCooldown.y);
                        if (toPlayer != attackDir && reachable && gap < tailRange) Begin(State.Tail, TailSeconds); // remata con la cola
                        else if (chain > 0 && sees && reachable)
                        {
                            chain--; // furia: se gira y vuelve a embestir casi sin aviso
                            direction = attackDir = toPlayer;
                            Begin(State.Windup, windupSeconds * 0.6f);
                        }
                        else Begin(State.Recover, recoverSeconds);
                    }
                    break;

                case State.GoreWindup:
                    direction = attackDir;
                    if (timer <= 0f) Begin(State.Gore, goreSeconds);
                    break;

                case State.Gore: // cornada corta
                    direction = attackDir;
                    float thrust = Progress;
                    if (thrust >= 0.2f && thrust < 0.8f) OpenWindow(attackDir, 0.55f, false);
                    if (thrust < 0.7f && gap > 0.05f && CanRun(attackDir, b)) vx = attackDir * goreSpeed;
                    if (timer <= 0f)
                    {
                        nextAttack = Time.time + Random.Range(1.6f, 2.2f);
                        Begin(State.Recover, recoverSeconds);
                    }
                    break;

                case State.Tail: // barrido bajo hacia atrás: se esquiva saltando
                    direction = attackDir;
                    float sweep = Progress;
                    if (sweep >= 0.5f && sweep < 5f / 6f) OpenWindow(-attackDir, 0.9f, true);
                    if (timer <= 0f)
                    {
                        nextAttack = Mathf.Max(nextAttack, Time.time + 0.6f);
                        Begin(State.Recover, recoverSeconds);
                    }
                    break;

                case State.WallStun:
                    if (timer <= 0f) Begin(State.Recover, 0.2f);
                    break;

                case State.Recover: // ventana para pegarle
                    if (timer <= 0f) Begin(State.Stalk, 0f);
                    break;
            }

            body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
            Face(direction);
        }
    }

    // Centinelas alados. Kinemáticos, como la avispa antigua.
    //  · Ámbar (cuerpo a cuerpo): ronda a Kael cambiando de lado, avisa un instante, se lanza en picada
    //    hacia donde Kael va a estar y escapa rápido hacia arriba.
    //  · Cian (a distancia): se coloca lejos, dispara adelantando el tiro, cambia de posición tras cada
    //    disparo y huye si Kael se le acerca.
    // Los dos huyen al recibir un golpe.
    // Siempre se colocan sobre la DIAGONAL DE TIRO de Kael (45° desde la punta del arco) y se quedan ahí
    // un momento antes de atacar: apuntando en diagonal hacia arriba, la flecha les da.
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
    public sealed class CentinelaVolador : MonoBehaviour
    {
        private enum State { Patrol, Position, Telegraph, Dive, Shoot, Flee }

        [SerializeField] private bool ranged;
        [SerializeField] private Vector2 patrolRange = new Vector2(2.5f, 0.7f);
        [SerializeField, Min(0f)] private float sightRange = 11.5f;
        [SerializeField, Min(0f)] private float cruiseSpeed = 7f;
        [SerializeField, Min(0f)] private float fleeSpeed = 11f;
        [Header("Diagonal de tiro de Kael (distancia medida sobre la línea de 45°)")]
        [SerializeField, Min(1f)] private float ambarDistance = 5f;
        [SerializeField, Min(1f)] private float cianDistance = 7f;
        [SerializeField, Min(1f)] private float fleeDistance = 9f;
        [SerializeField, Min(0f)] private float holdSeconds = 1.3f;   // tiempo quieto en la diagonal antes de atacar
        [Header("Ámbar: picada")]
        [SerializeField, Min(0f)] private float telegraphSeconds = 0.28f;
        [SerializeField, Min(0f)] private float diveSpeed = 13f;
        [SerializeField, Min(0.1f)] private float diveSeconds = 0.55f;
        [Header("Cian: disparo")]
        [SerializeField, Min(0f)] private float dangerRange = 4f;   // si Kael se acerca más, huye
        [SerializeField, Min(0.1f)] private float shootSeconds = 0.5f;
        [SerializeField, Min(0f)] private float fireAt = 0.3f;
        [SerializeField, Min(0.5f)] private float boltSpeed = 9f;
        [SerializeField, Min(1)] private int boltDamage = 1;

        private Rigidbody2D body, playerBody;
        private EnemyFeedback feedback;
        private SpriteRenderer sprite;
        private Transform player, playerVisual;
        private Vector2 home, diveTarget, diveDir, fleeTarget;
        private State state = State.Patrol;
        private float t, timer, settled;
        private int side = 1, facing = 1;
        private bool fired, wasHit;

        public bool Attacking => state == State.Telegraph || state == State.Dive || state == State.Shoot;
        public float AttackSeconds => ranged ? shootSeconds : telegraphSeconds + 0.4f;

        public void Setup(SpriteRenderer bodySprite, bool shooter)
        {
            sprite = bodySprite; ranged = shooter;
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            feedback = GetComponent<EnemyFeedback>();
            home = transform.position;
            t = Random.value * 6f;
        }

        private void OnEnable() { home = transform.position; state = State.Patrol; wasHit = false; }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged == null) return;
            player = tagged.transform;
            playerBody = tagged.GetComponent<Rigidbody2D>();
            var kaelSprite = tagged.GetComponentInChildren<SpriteRenderer>();
            if (kaelSprite != null) playerVisual = kaelSprite.transform;
        }

        // Punto sobre la diagonal de tiro de Kael: 45° hacia arriba desde la punta del arco
        // (misma punta que usa KaelShooter para la flecha diagonal), a la distancia indicada.
        private Vector2 OnAimDiagonal(int toSide, float distance)
        {
            if (player == null) return body.position;
            Transform k = playerVisual != null ? playerVisual : player;
            Vector3 s = k.lossyScale;
            Vector2 tip = (Vector2)k.position + new Vector2(toSide * 0.615f * Mathf.Abs(s.x), 1.19f * Mathf.Abs(s.y));
            return tip + new Vector2(toSide, 1f) * (distance * 0.70710678f);
        }

        private void StartFlee(Vector2 pos, Vector2 kael)
        {
            int away = pos.x >= kael.x ? 1 : -1;
            fleeTarget = OnAimDiagonal(away, fleeDistance); // se aleja sin salirse de la diagonal
            state = State.Flee; timer = 0.9f;
        }

        private void FixedUpdate()
        {
            if (body == null) return;
            float dt = Time.fixedDeltaTime;
            Vector2 pos = body.position;
            Vector2 push = feedback != null ? feedback.Push : Vector2.zero;
            if (feedback != null && feedback.Stunned)
            {
                body.MovePosition(pos + push * dt);
                wasHit = true;
                return;
            }
            t += dt;
            timer -= dt;
            Vector2 kael = player != null ? (Vector2)player.position + Vector2.up * 0.8f : pos;
            Vector2 kaelVelocity = playerBody != null ? playerBody.linearVelocity : Vector2.zero;
            float distance = Vector2.Distance(kael, pos);
            bool sees = player != null && distance < sightRange;
            if (wasHit) { wasHit = false; if (sees) StartFlee(pos, kael); }

            Vector2 target = pos; float spd = cruiseSpeed;
            bool faceMovement = false;

            switch (state)
            {
                case State.Patrol:
                    target = home + new Vector2(Mathf.Sin(t * 1.1f) * patrolRange.x, Mathf.Sin(t * 2.2f) * patrolRange.y);
                    spd = 2.5f; faceMovement = true;
                    if (sees) { state = State.Position; timer = Random.Range(0.6f, 1.2f); side = pos.x >= kael.x ? 1 : -1; }
                    break;

                case State.Position:
                    if (!sees) { state = State.Patrol; break; }
                    if (ranged)
                    {
                        if (distance < dangerRange) { StartFlee(pos, kael); break; }
                        target = OnAimDiagonal(side, cianDistance + Mathf.Sin(t * 3f) * 0.3f); // se mece a lo largo de la línea
                        settled = Vector2.Distance(pos, target) < 0.8f ? settled + dt : 0f;
                        if (timer <= 0f && settled >= holdSeconds) { state = State.Shoot; timer = shootSeconds; fired = false; }
                    }
                    else
                    {
                        target = OnAimDiagonal(side, ambarDistance + Mathf.Sin(t * 3f) * 0.3f);
                        settled = Vector2.Distance(pos, target) < 0.8f ? settled + dt : 0f;
                        if (timer <= 0f && settled >= holdSeconds) { state = State.Telegraph; timer = telegraphSeconds; settled = 0f; }
                    }
                    break;

                case State.Telegraph: // se frena un instante antes de lanzarse
                    if (timer <= 0f)
                    {
                        diveTarget = kael + kaelVelocity * 0.2f;
                        diveDir = (diveTarget - pos).normalized;
                        state = State.Dive; timer = diveSeconds;
                    }
                    break;

                case State.Dive: // pasa de largo a través de Kael y sale por el otro lado
                    target = pos + diveDir * 10f; spd = diveSpeed;
                    if (timer <= 0f || Vector2.Dot(diveTarget - pos, diveDir) < -0.6f) StartFlee(pos, kael);
                    break;

                case State.Shoot: // quieto mientras carga y dispara
                    if (!fired && timer <= shootSeconds - fireAt) { fired = true; Fire(pos, kael, kaelVelocity); }
                    if (timer <= 0f)
                    {
                        if (Random.value < 0.5f) side = -side; // cambia de lado para no ser predecible
                        state = State.Position; timer = Random.Range(1.2f, 2f);
                    }
                    break;

                case State.Flee:
                    target = fleeTarget; spd = fleeSpeed; faceMovement = true;
                    if (timer <= 0f || Vector2.Distance(pos, target) < 0.4f)
                    {
                        state = State.Position; timer = Random.Range(0.7f, 1.4f);
                        side = pos.x >= kael.x ? 1 : -1;
                    }
                    break;
            }

            Vector2 next = Vector2.MoveTowards(pos, target, spd * dt) + push * dt;
            float look = faceMovement ? next.x - pos.x : state == State.Dive ? diveDir.x : kael.x - pos.x;
            if (Mathf.Abs(look) > 0.001f) facing = look > 0f ? 1 : -1;
            if (sprite != null)
            {
                Vector3 s = sprite.transform.localScale;
                s.x = Mathf.Abs(s.x) * facing; // el sprite mira a la derecha
                sprite.transform.localScale = s;
            }
            body.MovePosition(next);
        }

        // Apunta a donde Kael va a estar cuando llegue el disparo
        private void Fire(Vector2 pos, Vector2 kael, Vector2 kaelVelocity)
        {
            Vector2 origin = pos + new Vector2(facing * 1f, 0.15f);
            float travel = Vector2.Distance(origin, kael) / boltSpeed;
            Vector2 direction = (kael + kaelVelocity * travel * 0.8f - origin).normalized;
            if (direction.sqrMagnitude < 0.01f) direction = Vector2.right * facing;
            CentinelaBolt.Spawn(origin, direction * boltSpeed, boltDamage, sprite);
        }
    }

    // Disparo de energía del Ala Cian: recto, daña a Kael y se apaga contra el terreno.
    public sealed class CentinelaBolt : MonoBehaviour
    {
        private static Sprite shape;
        private int damage = 1;

        public static void Spawn(Vector2 origin, Vector2 velocity, int damage, SpriteRenderer like)
        {
            var go = new GameObject("Centinela_Disparo");
            go.transform.SetPositionAndRotation(origin,
                Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg));
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Shape();
            renderer.sortingOrder = 12;
            if (like != null)
            {
                renderer.sortingLayerID = like.sortingLayerID;
                renderer.sortingOrder = like.sortingOrder + 2;
                renderer.sharedMaterial = like.sharedMaterial;
            }
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = velocity;
            var circle = go.AddComponent<CircleCollider2D>();
            circle.isTrigger = true; circle.radius = 0.16f;
            go.AddComponent<CentinelaBolt>().damage = damage;
            Destroy(go, 4f);
        }

        // Óvalo cian con núcleo blanco, 12x6 px
        private static Sprite Shape()
        {
            if (shape != null) return shape;
            var texture = new Texture2D(12, 6) { filterMode = FilterMode.Point };
            for (int x = 0; x < 12; x++)
                for (int y = 0; y < 6; y++)
                {
                    float dx = (x - 5.5f) / 6f, dy = (y - 2.5f) / 3f;
                    float d = dx * dx + dy * dy;
                    texture.SetPixel(x, y, d > 1f ? Color.clear : d < 0.3f ? Color.white : new Color(0.3f, 0.9f, 1f, 1f));
                }
            texture.Apply();
            shape = Sprite.Create(texture, new Rect(0, 0, 12, 6), new Vector2(0.5f, 0.5f), 16f);
            return shape;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var kael = other.GetComponentInParent<KaelHealth>();
            if (kael != null)
            {
                kael.Hit(damage, transform.position);
                Destroy(gameObject);
                return;
            }
            if (!other.isTrigger && other.gameObject.layer == 6) Destroy(gameObject); // terreno
        }
    }

    // Restos de un centinela destruido: reproduce la destrucción una vez, cae al suelo y se desvanece.
    public sealed class CentinelaRestos : MonoBehaviour
    {
        private SpriteRenderer target;
        private Sprite[] frames;
        private float duration, fps, time, groundY, fall, hold = 1.5f;

        public void Init(SpriteRenderer renderer, Sprite[] sprites, float framesPerSecond, float ground, float holdSeconds = 1.5f)
        {
            InitAnimated(renderer, sprites.Length / framesPerSecond, ground);
            frames = sprites; fps = framesPerSecond; hold = holdSeconds;
        }

        internal void InitAnimated(SpriteRenderer renderer, float seconds, float ground)
        {
            target = renderer; duration = seconds; groundY = ground;
        }

        private void Update()
        {
            if (target == null) { Destroy(gameObject); return; }
            time += Time.deltaTime;
            if (frames != null) target.sprite = frames[Mathf.Min((int)(time * fps), frames.Length - 1)];
            Vector3 p = transform.position;
            if (p.y > groundY)
            {
                fall += 30f * Time.deltaTime;
                p.y = Mathf.Max(groundY, p.y - fall * Time.deltaTime);
                transform.position = p;
            }
            float rest = time - duration - hold; // se queda un momento y luego se desvanece
            if (rest <= 0f) return;
            Color c = target.color; c.a = 1f - rest / 0.6f; target.color = c;
            if (rest >= 0.6f) Destroy(gameObject);
        }
    }
}
