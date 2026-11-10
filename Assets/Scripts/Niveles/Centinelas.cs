using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    public enum CentinelaTipo { Explorador, AlaCian, AlaAmbar }

    // Centinelas biomecánicos (hojas en Assets/Resources/Centinelas/<tipo>/).
    //  · Explorador (CentinelaTerrestre): corre hacia Kael, da un zarpazo y huye rápido; salta huecos y escalones.
    //  · Ala Ámbar (CentinelaVolador, cuerpo a cuerpo): ronda, se lanza en picada y escapa a toda velocidad.
    //  · Ala Cian (CentinelaVolador, a distancia): guarda distancia, dispara adelantando el tiro y se recoloca.
    // Al cargar la escena reemplaza a los enemigos antiguos (ver Convert): robots -> Explorador,
    // avispas -> voladores, plantas carnívoras -> se eliminan. Este componente lleva las animaciones.
    public sealed class CentinelaVisual : MonoBehaviour
    {
        private enum Anim { Idle, Move, Run, Attack, Hurt, Jump, Fall }

        private const float PixelsPerUnit = 56f;   // celda de 128 px ≈ 2.3 unidades; el cuerpo mide ≈ 1.2
        private const float FlyerDrop = 0.54f;     // los voladores se dibujan centrados en su colisionador
        private const float Size = 1.4f;           // tamaño general de los centinelas (1 = cuerpo de ≈ 1.2 unidades)
        private const float EliteScale = 1.3f;     // los antiguos robots pesados son Exploradores aún más grandes
        private const int TerrainLayer = 6;
        private static readonly string[] Folders = { "Explorador", "AlaCian", "AlaAmbar" };
        private static readonly float[] FeetPivot = { 0.23f, 0.25f, 0.19f };
        private static readonly float[] Fps = { 6f, 10f, 15f, 14f, 12f, 12f, 10f };
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
        private float time, speed, runSpeed, scale = 1f;
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
            feedback = GetComponent<EnemyFeedback>();
            sprite = GetComponentInChildren<SpriteRenderer>(true);
            bool ground = tipo == CentinelaTipo.Explorador;
            clips = new[]
            {
                Load(tipo, "Idle"),
                Load(tipo, ground ? "Caminar" : "Patrulla"),
                Load(tipo, ground ? "Correr" : "Patrulla"),
                Load(tipo, "Ataque"),
                Load(tipo, "Dano"),
                ground ? Load(tipo, "Saltar") : null,
                ground ? Load(tipo, "Caer") : null,
            };
            death = Load(tipo, "Destruccion");
            if (sprite == null || clips[0] == null) { enabled = false; return; }

            // Fuera la IA y la animación de marcador de posición del enemigo antiguo
            foreach (var old in new Behaviour[] { GetComponent<SpriteFrames>(), GetComponent<RobotPatrol>(), GetComponent<WaspFlyer>() })
                if (old != null) { old.enabled = false; Destroy(old); }

            sprite.sprite = clips[0][0];
            sprite.flipX = false;
            runSpeed = ground ? 2.6f : float.MaxValue;
            scale = (elite ? EliteScale : 1f) * Size;
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

        private void OnEnable() { lastPosition = transform.position; speed = 0f; }

        private void Update()
        {
            if (!ready || sprite == null) return;
            float dt = Time.deltaTime;
            if (dt > 0f) speed = Mathf.Lerp(speed, (transform.position - lastPosition).magnitude / dt, 0.25f);
            lastPosition = transform.position;
            visualPosition = sprite.transform.position;
            facingLeft = sprite.transform.lossyScale.x < 0f;

            bool attacking = walker != null ? walker.Attacking : flyer != null && flyer.Attacking;
            bool airborne = walker != null && walker.Airborne;
            Anim next = feedback != null && feedback.Stunned ? Anim.Hurt
                : attacking ? Anim.Attack
                : airborne ? (walker.VerticalSpeed > 0f ? Anim.Jump : Anim.Fall)
                : speed > runSpeed ? Anim.Run
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
                fps = frames.Length / Mathf.Max(0.1f, walker != null ? walker.AttackSeconds : flyer.AttackSeconds);
            int index = (int)(time * fps);
            bool once = current == Anim.Attack || current == Anim.Hurt || current == Anim.Jump;
            sprite.sprite = frames[once ? Mathf.Min(index, frames.Length - 1) : index % frames.Length];
        }

        // Al morir deja los restos con la animación de destrucción
        private void OnDestroy()
        {
            if (!ready || quitting || death == null || !gameObject.scene.isLoaded) return;
            if (ReferenceEquals(health, null) || health.Remaining > 0) return;
            var go = new GameObject("Centinela_Restos");
            go.transform.position = visualPosition;
            go.transform.localScale = new Vector3(facingLeft ? -scale : scale, scale, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = death[0];
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
            go.AddComponent<CentinelaRestos>().Init(renderer, death, 10f, ground);
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

    // Explorador: patrulla; al ver a Kael corre hacia él, da un zarpazo corto y huye a toda velocidad
    // (a veces de un salto hacia atrás). Espera a distancia y vuelve a entrar. Si lo golpean, también huye.
    // Salta huecos y escalones bajos para no quedarse atascado.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(EnemyHealth))]
    public sealed class CentinelaTerrestre : MonoBehaviour
    {
        private enum State { Patrol, Chase, Windup, Lunge, Retreat, Stalk }

        [SerializeField, Min(0f)] private float walkSpeed = 1.8f;
        [SerializeField, Min(0f)] private float runSpeed = 5.6f;
        [SerializeField, Min(0f)] private float fleeSpeed = 8.5f;
        [SerializeField, Min(0f)] private float sightRange = 9f;
        [Header("Ataque")]
        [SerializeField, Min(0.1f)] private float attackRange = 0.9f;   // espacio borde a borde para iniciar el zarpazo
        [SerializeField, Min(0f)] private float windupSeconds = 0.25f;
        [SerializeField, Min(0f)] private float lungeSpeed = 10f;
        [SerializeField, Min(0f)] private float lungeSeconds = 0.2f;
        [Header("Huida")]
        [SerializeField, Min(1f)] private float retreatDistance = 5f;
        [SerializeField, Min(0.2f)] private float retreatSeconds = 1.2f;
        [SerializeField] private Vector2 waitSeconds = new Vector2(0.35f, 0.9f);
        [SerializeField, Min(0f)] private float hopSpeed = 7.5f;
        [SerializeField] private LayerMask groundLayers = 1 << 6;

        private Rigidbody2D body;
        private BoxCollider2D box;
        private EnemyFeedback feedback;
        private Transform visual, player;
        private Collider2D playerCollider;
        private State state = State.Patrol;
        private int direction = -1, lungeDir = 1, fleeDir = 1;
        private float timer, hopUntil;
        private bool grounded = true, wasHit;

        public bool Attacking => state == State.Windup || state == State.Lunge;
        public bool Airborne => !grounded;
        public float VerticalSpeed => body != null ? body.linearVelocity.y : 0f;
        public float AttackSeconds => windupSeconds + lungeSeconds + 0.1f;

        public void Setup(Transform bodyVisual, bool elite)
        {
            visual = bodyVisual;
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            feedback = GetComponent<EnemyFeedback>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            if (elite) { runSpeed *= 0.85f; fleeSpeed *= 0.8f; windupSeconds *= 1.4f; } // grande: pega fuerte pero es más lento
        }

        private void OnEnable() { state = State.Patrol; timer = 0f; wasHit = false; grounded = true; }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged == null) return;
            player = tagged.transform;
            playerCollider = tagged.GetComponent<Collider2D>();
            // No se empujan físicamente; el daño va por la caja de golpe
            if (box != null)
                foreach (var c in tagged.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(box, c);
        }

        private bool FloorAt(float x, Bounds b) =>
            Physics2D.Raycast(new Vector2(x, b.min.y + 0.3f), Vector2.down, 1.3f, groundLayers);

        private bool FloorAhead(int dir, Bounds b) => FloorAt(b.center.x + dir * (b.extents.x + 0.2f), b);

        private bool WallAhead(int dir, Bounds b) =>
            Physics2D.Raycast(b.center, Vector2.right * dir, b.extents.x + 0.15f, groundLayers);

        private bool CanRun(int dir, Bounds b) => FloorAhead(dir, b) && !WallAhead(dir, b);

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
            if (!FloorAhead(dir, b) && FloorAt(b.center.x + dir * 3.4f, b)
                && !Physics2D.Raycast(b.center, Vector2.right * dir, 3.4f, groundLayers))
            {
                Hop(dir * Mathf.Max(speed, 6f), 8.5f);
                return true;
            }
            return false;
        }

        private void BeginRetreat(int away, bool hop, Bounds b)
        {
            state = State.Retreat; timer = retreatSeconds; fleeDir = away;
            if (hop && !WallAhead(away, b) && FloorAt(b.center.x + away * 3f, b)) Hop(away * fleeSpeed, hopSpeed);
        }

        private void Face(int dir)
        {
            if (visual == null) return;
            Vector3 s = visual.localScale;
            s.x = Mathf.Abs(s.x) * (dir > 0 ? 1f : -1f); // el sprite mira a la derecha
            visual.localScale = s;
        }

        private void FixedUpdate()
        {
            if (body == null) return;
            Bounds b = box.bounds;
            grounded = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.2f, groundLayers);
            if (feedback != null && feedback.Stunned) { wasHit = true; return; } // deja actuar el retroceso
            if (!grounded || Time.time < hopUntil) return;

            float dx = 0f, gap = 99f; bool sees = false;
            if (player != null)
            {
                dx = player.position.x - transform.position.x;
                float playerHalf = playerCollider != null ? playerCollider.bounds.extents.x : 0.25f;
                gap = Mathf.Abs(dx) - b.extents.x - playerHalf;
                sees = Mathf.Abs(dx) < sightRange && Mathf.Abs(player.position.y - transform.position.y) < 3.5f;
            }
            int toPlayer = dx >= 0f ? 1 : -1;
            timer -= Time.fixedDeltaTime;
            float vx = 0f;
            int face = direction;

            if (wasHit)
            {
                wasHit = false;
                if (sees) { BeginRetreat(-toPlayer, true, b); if (!grounded) return; }
            }

            switch (state)
            {
                case State.Patrol:
                    if (!CanRun(direction, b)) direction = -direction;
                    vx = direction * walkSpeed;
                    if (sees) state = State.Chase;
                    break;

                case State.Chase:
                    direction = face = toPlayer;
                    if (!sees) { state = State.Patrol; break; }
                    if (gap <= attackRange) { state = State.Windup; timer = windupSeconds; break; }
                    if (CanRun(toPlayer, b)) vx = toPlayer * runSpeed;
                    else if (TryJumpObstacle(toPlayer, runSpeed, b)) return;
                    break;

                case State.Windup: // se agazapa un instante: es la ventana para golpearlo
                    direction = face = toPlayer;
                    if (timer <= 0f) { state = State.Lunge; timer = lungeSeconds; lungeDir = toPlayer; }
                    break;

                case State.Lunge:
                    face = lungeDir;
                    if (gap > 0.05f && CanRun(lungeDir, b)) vx = lungeDir * lungeSpeed;
                    if (timer <= 0f)
                    {
                        BeginRetreat(-toPlayer, Random.value < 0.5f, b);
                        if (!grounded) return;
                    }
                    break;

                case State.Retreat:
                    direction = face = fleeDir;
                    bool far = Mathf.Abs(dx) >= retreatDistance;
                    if (!far && CanRun(fleeDir, b)) vx = fleeDir * fleeSpeed;
                    else if (!far && TryJumpObstacle(fleeDir, fleeSpeed, b)) return;
                    else timer = 0f; // ya está lejos o acorralado
                    if (timer <= 0f) { state = State.Stalk; timer = Random.Range(waitSeconds.x, waitSeconds.y); }
                    break;

                case State.Stalk: // vigila de frente y mantiene la distancia antes de volver a entrar
                    direction = face = toPlayer;
                    if (gap < 2.2f && CanRun(-toPlayer, b)) vx = -toPlayer * walkSpeed * 1.6f;
                    if (timer <= 0f) state = sees ? State.Chase : State.Patrol;
                    break;
            }

            body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
            Face(face);
        }
    }

    // Centinelas alados. Kinemáticos, como la avispa antigua.
    //  · Ámbar (cuerpo a cuerpo): ronda a Kael cambiando de lado, avisa un instante, se lanza en picada
    //    hacia donde Kael va a estar y escapa rápido hacia arriba.
    //  · Cian (a distancia): se coloca lejos, dispara adelantando el tiro, cambia de posición tras cada
    //    disparo y huye si Kael se le acerca.
    // Los dos huyen al recibir un golpe.
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth))]
    public sealed class CentinelaVolador : MonoBehaviour
    {
        private enum State { Patrol, Position, Telegraph, Dive, Shoot, Flee }

        [SerializeField] private bool ranged;
        [SerializeField] private Vector2 patrolRange = new Vector2(2.5f, 0.7f);
        [SerializeField, Min(0f)] private float sightRange = 9.5f;
        [SerializeField, Min(0f)] private float cruiseSpeed = 5.5f;
        [SerializeField, Min(0f)] private float fleeSpeed = 11f;
        [SerializeField] private Vector2 fleeOffset = new Vector2(6.5f, 3.2f);
        [Header("Ámbar: picada")]
        [SerializeField] private Vector2 orbit = new Vector2(3.2f, 1.9f);
        [SerializeField, Min(0f)] private float telegraphSeconds = 0.28f;
        [SerializeField, Min(0f)] private float diveSpeed = 13f;
        [SerializeField, Min(0.1f)] private float diveSeconds = 0.55f;
        [Header("Cian: disparo")]
        [SerializeField] private Vector2 keepAway = new Vector2(5f, 2.2f);
        [SerializeField, Min(0f)] private float dangerRange = 3.2f;   // si Kael se acerca más, huye
        [SerializeField, Min(0.1f)] private float shootSeconds = 0.5f;
        [SerializeField, Min(0f)] private float fireAt = 0.3f;
        [SerializeField, Min(0.5f)] private float boltSpeed = 9f;
        [SerializeField, Min(1)] private int boltDamage = 1;

        private Rigidbody2D body, playerBody;
        private EnemyFeedback feedback;
        private SpriteRenderer sprite;
        private Transform player;
        private Vector2 home, diveTarget, diveDir, fleeTarget;
        private State state = State.Patrol;
        private float t, timer;
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
        }

        private void StartFlee(Vector2 pos, Vector2 kael)
        {
            int away = pos.x >= kael.x ? 1 : -1;
            fleeTarget = kael + new Vector2(away * fleeOffset.x, fleeOffset.y);
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
                        target = kael + new Vector2(side * keepAway.x, keepAway.y + Mathf.Sin(t * 3f) * 0.3f);
                        if (timer <= 0f && Vector2.Distance(pos, target) < 1.5f) { state = State.Shoot; timer = shootSeconds; fired = false; }
                    }
                    else
                    {
                        target = kael + new Vector2(side * orbit.x, orbit.y + Mathf.Sin(t * 3f) * 0.4f);
                        if (timer <= 0f && Vector2.Distance(pos, target) < 2f) { state = State.Telegraph; timer = telegraphSeconds; }
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
            Vector2 origin = pos + new Vector2(facing * 0.7f, 0.1f);
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
        private float fps, time, groundY, fall, hold = 1.5f;

        public void Init(SpriteRenderer renderer, Sprite[] sprites, float framesPerSecond, float ground, float holdSeconds = 1.5f)
        {
            target = renderer; frames = sprites; fps = framesPerSecond; groundY = ground; hold = holdSeconds;
        }

        private void Update()
        {
            if (target == null || frames == null) { Destroy(gameObject); return; }
            time += Time.deltaTime;
            target.sprite = frames[Mathf.Min((int)(time * fps), frames.Length - 1)];
            Vector3 p = transform.position;
            if (p.y > groundY)
            {
                fall += 30f * Time.deltaTime;
                p.y = Mathf.Max(groundY, p.y - fall * Time.deltaTime);
                transform.position = p;
            }
            float rest = time - frames.Length / fps - hold; // se queda un momento y luego se desvanece
            if (rest <= 0f) return;
            Color c = target.color; c.a = 1f - rest / 0.6f; target.color = c;
            if (rest >= 0.6f) Destroy(gameObject);
        }
    }
}
