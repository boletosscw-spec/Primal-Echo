using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // CENTINELA REX · jefe final (hojas en Assets/Resources/CentinelaRex).
    // Aparece solo en el Templo de Némesis, delante de la salida, y la salida queda cerrada hasta vencerlo.
    //
    // Cómo pelea:
    //  · Cerca y de frente: MORDIDA.
    //  · Si Kael se le pone detrás: COLETAZO (barrido bajo a ambos lados; se esquiva saltando).
    //  · Lejos: GRUÑIDO de aviso -> EMBESTIDA -> COLETAZO al frenar.
    //  · Si Kael se queda quieto tensando el arco, lo castiga con la embestida aunque esté a media distancia.
    //  · Tarda un instante en girarse: rodearlo abre una ventana, pero responde con la cola.
    //  · Cada cierto daño se tambalea y contraataca.
    //  · Con media vida entra en FURIA: avisa menos tiempo, corre más y descansa menos.
    // No se le puede empujar ni aturdir a golpes, y vuelve a su sitio con la vida llena si Kael muere.
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(EnemyHealth))]
    public sealed class CentinelaRex : MonoBehaviour
    {
        private enum State { Dormant, Roar, Approach, Bite, Growl, Charge, Tail, Recover, Stagger }
        private enum Clip { Idle, Caminar, Correr, Mordida, Dano, Muerte, Grunido, Embestida, Coletazo }

        private static readonly string[] ClipNames = { "Idle", "Caminar", "Correr", "Mordida", "Dano", "Muerte", "Grunido", "Embestida", "Coletazo" };
        private static readonly int[] ClipFrames = { 8, 8, 8, 8, 4, 8, 6, 8, 8 };
        private const float PixelsPerUnit = 46f;      // cuerpo ≈ 5.6 unidades de largo y 3.4 de alto
        private const float FeetPivot = 24f / 224f;
        private const int TerrainLayer = 6;
        private static bool quitting;

        [SerializeField, Min(1)] private int maxHealth = 60;
        [SerializeField, Min(1)] private int attackDamage = 2;
        [SerializeField, Min(0f)] private float wakeRange = 12f;
        [Header("Movimiento")]
        [SerializeField, Min(0f)] private float walkSpeed = 2.2f;
        [SerializeField, Min(0f)] private float runSpeed = 5f;
        [SerializeField, Min(0f)] private float turnDelay = 0.35f;
        [Header("Mordida")]
        [SerializeField, Min(0f)] private float biteRange = 1.6f;
        [SerializeField, Min(0.1f)] private float biteSeconds = 0.7f;
        [Header("Gruñido + embestida")]
        [SerializeField, Min(0.1f)] private float growlSeconds = 0.8f;
        [SerializeField, Min(0f)] private float chargeSpeed = 14f;
        [SerializeField, Min(0.1f)] private float chargeSeconds = 1.1f;
        [SerializeField, Min(0f)] private float chargeMinGap = 4.5f;
        [SerializeField, Min(0f)] private float chargeCooldown = 3.5f;
        [Header("Coletazo")]
        [SerializeField, Min(0f)] private float tailRange = 3f;
        [SerializeField, Min(0.1f)] private float tailSeconds = 0.75f;
        [Header("Aguante")]
        [SerializeField, Min(0f)] private float recoverSeconds = 0.7f;
        [SerializeField, Min(1)] private int staggerEvery = 12;

        private Rigidbody2D body;
        private BoxCollider2D box;
        private EnemyHealth health;
        private SpriteRenderer sprite;
        private Sprite[][] clips;
        private Transform player;
        private Collider2D playerCollider;
        private KaelHealth playerHealth;
        private KaelShooter playerShooter;
        private KaelRespawn playerRespawn;
        private Collider2D goal;
        private Vector3 home;
        private State state = State.Dormant;
        private int facing = -1, chargeDir = -1, lastHealth, damageTaken;
        private float timer, duration = 1f, behindTime, nextCharge, flashUntil, moveSpeed;
        private bool hitDone, engaged;
        private Texture2D pixel;

        private bool Furious => health != null && health.Remaining * 2 <= maxHealth;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            quitting = false;
            Application.quitting += () => quitting = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            TrySpawn();
            SceneManager.sceneLoaded += (_, __) => TrySpawn();
        }

        // Coloca al jefe delante de la meta del último nivel y despeja esa plataforma de otros enemigos.
        private static void TrySpawn()
        {
            if (!SceneManager.GetActiveScene().name.Contains("Templo")) return;
            if (FindAnyObjectByType<CentinelaRex>(FindObjectsInactive.Include) != null) return;
            var levelGoal = FindAnyObjectByType<LevelGoal>();
            if (levelGoal == null) return;

            Vector2 g = levelGoal.transform.position;
            Vector2 spot = new Vector2(g.x - 9f, g.y - 1.5f);
            // Si en la escena está el marcador "Vista_Centinela_Rex", el jefe aparece donde lo hayas puesto
            if (SoloVistaEditor.RexSpot.HasValue && SoloVistaEditor.RexScene == SceneManager.GetActiveScene().name)
                spot = SoloVistaEditor.RexSpot.Value;
            var hit = Physics2D.Raycast(spot + Vector2.up * 2.5f, Vector2.down, 12f, 1 << TerrainLayer);
            if (hit.collider != null) spot = hit.point;

            foreach (var enemy in FindObjectsByType<EnemyHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Vector2 p = enemy.transform.position;
                if (Mathf.Abs(p.x - spot.x) < 26f && Mathf.Abs(p.y - spot.y) < 9f) Destroy(enemy.gameObject);
            }
            // El jefe pelea solo: si su sala era una emboscada, se desactiva (sin compuertas ni enemigos extra)
            foreach (var arena in FindObjectsByType<ArenaLock>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (Mathf.Abs(arena.transform.position.x - spot.x) < 30f) Destroy(arena.gameObject);

            var go = new GameObject("Centinela_Rex");
            go.transform.position = new Vector3(spot.x, spot.y + 0.02f, 0f);
            var rex = go.AddComponent<CentinelaRex>();
            rex.goal = levelGoal.GetComponent<Collider2D>();
            if (rex.goal != null) rex.goal.enabled = false; // la salida se abre al vencerlo
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            box = GetComponent<BoxCollider2D>();
            health = GetComponent<EnemyHealth>();
            body.gravityScale = 3f; body.mass = 60f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            box.size = new Vector2(3f, 2.6f);
            box.offset = new Vector2(0f, 1.3f);
            health.SetHealth(maxHealth);
            lastHealth = maxHealth;
            home = transform.position;

            var visual = new GameObject("Visual");
            visual.transform.SetParent(transform, false);
            sprite = visual.AddComponent<SpriteRenderer>();
            sprite.sortingOrder = 8; // por encima de la decoración del mapa (5), por debajo de Kael (10)

            // Tocarlo también hace daño
            var hurt = new GameObject("Golpe");
            hurt.transform.SetParent(transform, false);
            var hurtBox = hurt.AddComponent<BoxCollider2D>();
            hurtBox.isTrigger = true; hurtBox.size = box.size * 0.9f; hurtBox.offset = box.offset;
            hurt.AddComponent<ContactHazard>();

            clips = new Sprite[ClipNames.Length][];
            for (int i = 0; i < clips.Length; i++) clips[i] = Load(ClipNames[i], ClipFrames[i]);
            if (clips[0] != null) sprite.sprite = clips[0][0];
            pixel = Texture2D.whiteTexture;
        }

        private void Start()
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged == null) return;
            player = tagged.transform;
            playerCollider = tagged.GetComponent<Collider2D>();
            playerHealth = tagged.GetComponent<KaelHealth>();
            playerShooter = tagged.GetComponent<KaelShooter>();
            playerRespawn = tagged.GetComponent<KaelRespawn>();
            if (playerRespawn != null) playerRespawn.Respawned += ResetFight;
            var kaelSprite = tagged.GetComponentInChildren<SpriteRenderer>();
            if (kaelSprite != null)
            {
                sprite.sharedMaterial = kaelSprite.sharedMaterial;
                sprite.sortingLayerID = kaelSprite.sortingLayerID;
            }
            foreach (var c in tagged.GetComponentsInChildren<Collider2D>()) Physics2D.IgnoreCollision(box, c);
        }

        // Kael murió: el jefe vuelve a su sitio con la vida llena
        private void ResetFight()
        {
            health.SetHealth(maxHealth);
            lastHealth = maxHealth; damageTaken = 0;
            body.linearVelocity = Vector2.zero;
            transform.position = home;
            engaged = false; state = State.Dormant; facing = -1;
        }

        private void Begin(State next, float seconds)
        {
            state = next; timer = duration = Mathf.Max(0.05f, seconds); hitDone = false;
        }

        private bool FloorAhead(int dir, Bounds b) =>
            Physics2D.Raycast(new Vector2(b.center.x + dir * (b.extents.x + 0.3f), b.min.y + 0.3f), Vector2.down, 1.3f, 1 << TerrainLayer);

        private bool WallAhead(int dir, Bounds b) =>
            Physics2D.Raycast(b.center, Vector2.right * dir, b.extents.x + 0.2f, 1 << TerrainLayer);

        private bool CanRun(int dir, Bounds b) => FloorAhead(dir, b) && !WallAhead(dir, b);

        private void Strike(Bounds zone)
        {
            if (hitDone || playerCollider == null || playerHealth == null) return;
            if (!zone.Intersects(playerCollider.bounds)) return;
            hitDone = true;
            playerHealth.Hit(attackDamage, transform.position);
        }

        private void FixedUpdate()
        {
            Bounds b = box.bounds;
            float dx = 0f, dy = 0f, gap = 99f;
            if (player != null)
            {
                dx = player.position.x - b.center.x;
                dy = player.position.y - b.min.y;
                float playerHalf = playerCollider != null ? playerCollider.bounds.extents.x : 0.25f;
                gap = Mathf.Abs(dx) - b.extents.x - playerHalf;
            }
            int toPlayer = dx >= 0f ? 1 : -1;
            bool fury = Furious;
            float tempo = fury ? 0.65f : 1f; // en furia todo avisa y descansa menos
            timer -= Time.fixedDeltaTime;
            float progress = 1f - Mathf.Clamp01(timer / duration);
            float vx = 0f;

            // Daño recibido: destello y, cada cierto daño, se tambalea (salvo en pleno ataque)
            int remaining = health.Remaining;
            if (remaining < lastHealth)
            {
                damageTaken += lastHealth - remaining;
                flashUntil = Time.time + 0.09f;
                if (state == State.Dormant) { engaged = true; facing = toPlayer; Begin(State.Roar, growlSeconds); }
                else if (damageTaken >= staggerEvery && (state == State.Approach || state == State.Recover || state == State.Bite))
                {
                    damageTaken = 0;
                    Begin(State.Stagger, 0.45f);
                }
            }
            lastHealth = remaining;

            switch (state)
            {
                case State.Dormant:
                    if (player != null && Mathf.Abs(dx) < wakeRange && Mathf.Abs(dy) < 6f)
                    {
                        engaged = true; facing = toPlayer;
                        Begin(State.Roar, growlSeconds + 0.3f);
                    }
                    break;

                case State.Roar:
                    if (timer <= 0f) { state = State.Approach; nextCharge = Time.time + 1f; }
                    break;

                case State.Approach:
                    if (player == null || Mathf.Abs(dx) > 24f || Mathf.Abs(dy) > 10f) { engaged = false; state = State.Dormant; break; }
                    if (toPlayer != facing)
                    {
                        // Kael está detrás: si está a tiro, coletazo; si no, tarda un instante en girarse
                        if (gap < tailRange) { Begin(State.Tail, tailSeconds * tempo); break; }
                        behindTime += Time.fixedDeltaTime;
                        if (behindTime >= turnDelay * tempo) { facing = toPlayer; behindTime = 0f; }
                        break;
                    }
                    behindTime = 0f;
                    if (gap <= biteRange && dy < 3.2f) { Begin(State.Bite, biteSeconds * tempo); break; }
                    bool bowThreat = playerShooter != null && playerShooter.Rooted;
                    if (Time.time >= nextCharge && (gap > chargeMinGap || (bowThreat && gap > 2.5f)))
                    {
                        Begin(State.Growl, growlSeconds * tempo);
                        break;
                    }
                    if (CanRun(facing, b)) vx = facing * (gap > 3f ? runSpeed * (fury ? 1.25f : 1f) : walkSpeed);
                    break;

                case State.Bite: // el golpe cae a mitad de la animación
                    if (progress > 0.4f && progress < 0.7f)
                        Strike(new Bounds(new Vector3(b.center.x + facing * (b.extents.x + 1f), b.min.y + 1.4f, 0f), new Vector3(2.4f, 2.8f, 10f)));
                    if (timer <= 0f) Begin(State.Recover, recoverSeconds * 0.5f * tempo);
                    break;

                case State.Growl: // aviso: sigue a Kael con la mirada y fija la dirección al final
                    if (progress < 0.8f) facing = toPlayer;
                    if (timer <= 0f) { chargeDir = facing; Begin(State.Charge, chargeSeconds); }
                    break;

                case State.Charge:
                    vx = chargeDir * chargeSpeed * (fury ? 1.15f : 1f);
                    Strike(b);
                    bool passed = dx * chargeDir < -3f; // ya rebasó a Kael
                    if (timer <= 0f || passed || !CanRun(chargeDir, b))
                    {
                        vx = 0f;
                        nextCharge = Time.time + chargeCooldown * tempo;
                        Begin(State.Tail, tailSeconds * tempo); // remata con la cola al frenar
                    }
                    break;

                case State.Tail: // barrido bajo a los dos lados: se esquiva saltando
                    if (progress > 0.5f && progress < 0.8f)
                        Strike(new Bounds(new Vector3(b.center.x, b.min.y + 0.7f, 0f), new Vector3(b.size.x + 5f, 1.4f, 10f)));
                    if (timer <= 0f) Begin(State.Recover, recoverSeconds * tempo);
                    break;

                case State.Recover: // ventana para pegarle
                    if (timer <= 0f) state = State.Approach;
                    break;

                case State.Stagger:
                    if (timer <= 0f)
                    {
                        if (gap < tailRange) Begin(State.Tail, tailSeconds * tempo); // contraataca
                        else { state = State.Approach; nextCharge = Time.time; }
                    }
                    break;
            }

            moveSpeed = Mathf.Abs(vx);
            body.linearVelocity = new Vector2(vx, body.linearVelocity.y);
        }

        private void Update()
        {
            if (sprite == null || clips == null) return;
            Vector3 s = sprite.transform.localScale;
            s.x = Mathf.Abs(s.x) * facing; // el sprite mira a la derecha
            sprite.transform.localScale = s;
            sprite.color = Time.time < flashUntil ? new Color(1f, 0.45f, 0.4f, 1f)
                : Furious ? new Color(1f, 0.86f, 0.82f, 1f) : Color.white;

            Clip clip = Clip.Idle; bool once = false; float fps = 7f;
            switch (state)
            {
                case State.Roar: case State.Growl: clip = Clip.Grunido; once = true; break;
                case State.Approach:
                    if (moveSpeed > walkSpeed + 0.5f) { clip = Clip.Correr; fps = 13f; }
                    else if (moveSpeed > 0.1f) { clip = Clip.Caminar; fps = 9f; }
                    break;
                case State.Bite: clip = Clip.Mordida; once = true; break;
                case State.Charge: clip = Clip.Embestida; fps = 15f; break;
                case State.Tail: clip = Clip.Coletazo; once = true; break;
                case State.Stagger: clip = Clip.Dano; once = true; break;
            }
            Sprite[] frames = clips[(int)clip] ?? clips[0];
            if (frames == null) return;
            int index = once
                ? Mathf.Min(frames.Length - 1, (int)((1f - Mathf.Clamp01(timer / duration)) * frames.Length))
                : (int)(Time.time * fps) % frames.Length;
            sprite.sprite = frames[index];
        }

        // Barra de vida mientras dura la pelea
        private void OnGUI()
        {
            if (!engaged || health == null || pixel == null) return;
            float width = Mathf.Min(520f, Screen.width * 0.5f), x = (Screen.width - width) / 2f, y = Screen.height - 54f;
            var label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16, fontStyle = FontStyle.Bold };
            label.normal.textColor = new Color(1f, 0.92f, 0.8f);
            GUI.Label(new Rect(x, y - 26f, width, 24f), Furious ? "CENTINELA REX · FURIA" : "CENTINELA REX", label);
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(x - 3f, y - 3f, width + 6f, 18f), pixel);
            GUI.color = Furious ? new Color(1f, 0.35f, 0.2f) : new Color(0.25f, 0.85f, 1f);
            GUI.DrawTexture(new Rect(x, y, width * Mathf.Clamp01((float)health.Remaining / maxHealth), 12f), pixel);
            GUI.color = old;
        }

        private void OnDestroy()
        {
            if (playerRespawn != null) playerRespawn.Respawned -= ResetFight;
            if (quitting || !gameObject.scene.isLoaded) return;
            if (ReferenceEquals(health, null) || health.Remaining > 0) return;
            if (goal != null) goal.enabled = true; // vencido: se abre la salida

            Sprite[] death = clips != null ? clips[(int)Clip.Muerte] : null;
            if (death == null) return;
            var go = new GameObject("Centinela_Rex_Restos");
            go.transform.position = transform.position;
            go.transform.localScale = new Vector3(facing, 1f, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = death[0];
            if (sprite != null)
            {
                renderer.sortingLayerID = sprite.sortingLayerID;
                renderer.sortingOrder = sprite.sortingOrder - 1;
                renderer.sharedMaterial = sprite.sharedMaterial;
            }
            go.AddComponent<CentinelaRestos>().Init(renderer, death, 7f, transform.position.y, 6f);
        }

        private static Sprite[] Load(string clip, int count)
        {
            var texture = Resources.Load<Texture2D>("CentinelaRex/" + clip);
            if (texture == null)
            {
                Debug.LogWarning("Centinela Rex: falta la hoja Assets/Resources/CentinelaRex/" + clip + ".png");
                return null;
            }
            texture.filterMode = FilterMode.Point;
            float width = (float)texture.width / count;
            float ppu = PixelsPerUnit * width / 272f; // por si Unity redujo la textura
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Sprite.Create(texture, new Rect(i * width, 0f, width, texture.height),
                    new Vector2(0.5f, FeetPivot), ppu, 0, SpriteMeshType.FullRect);
            return frames;
        }
    }
}
