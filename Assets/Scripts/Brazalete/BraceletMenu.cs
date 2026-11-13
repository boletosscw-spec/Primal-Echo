using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    [DefaultExecutionOrder(200)]
    [RequireComponent(typeof(PlayerController), typeof(BraceletProgress))]
    public sealed class BraceletMenu : MonoBehaviour
    {
        [SerializeField] private BraceletSettings settings;
        [Header("Sonidos opcionales")]
        public AudioClip abrir;
        public AudioClip cerrar;
        public AudioClip moverCursor;
        public AudioClip comprarMejora;
        public AudioClip error;
        [SerializeField, Range(0f, 1f)] private float volume = .7f;

        public enum MenuPhase { Closed, Opening, Open, Closing }
        public MenuPhase Phase { get; private set; }
        public bool IsActive => Phase != MenuPhase.Closed;
        public int Tab { get; private set; }
        public int Selection { get; private set; }
        public float Projection { get; private set; }
        public float CursorTime { get; private set; }
        public int Frame { get; private set; }
        public string Message { get; private set; }
        public BraceletSettings Settings => settings;
        public BraceletProgress Progress { get; private set; }
        public Vector3 WristPosition
        {
            get
            {
                Vector2 local = settings.wrist;
                if (visual.flipX) local.x = -local.x;
                return visual.transform.TransformPoint(local);
            }
        }

        private PlayerController player;
        private Rigidbody2D body;
        private Animator animator;
        private SpriteRenderer visual;
        private KaelSpear spear;
        private KaelShooter shooter;
        private AudioSource audioSource;
        private BraceletHologram view;
        private InputActionMap controls;
        private InputAction toggle, cancel, select, previous, next, navigate;
        private readonly List<Behaviour> suspended = new List<Behaviour>();
        private readonly BraceletCameraFocus focus = new BraceletCameraFocus();
        private Sprite previousSprite;
        private Vector2 previousVelocity;
        private Vector3 previousScale;
        private float previousTimeScale, phaseTime, openingTime, closingProjection, closingFocus;
        private float nextMove, releasedAt;
        private Vector2Int lastMove;
        private int upgradesSelection, inventorySelection;
        private bool captured, previousVisibility;
        private bool toggleRequested, cancelRequested, selectRequested, previousRequested, nextRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= SceneLoaded;
            SceneManager.sceneLoaded += SceneLoaded;
            AddToPlayers();
        }

        private static void SceneLoaded(Scene scene, LoadSceneMode mode) => AddToPlayers();
        private static void AddToPlayers()
        {
            foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                if (!pc.GetComponent<BraceletMenu>()) pc.gameObject.AddComponent<BraceletMenu>();
        }

        private void Awake()
        {
            player = GetComponent<PlayerController>();
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            visual = GetComponentInChildren<SpriteRenderer>();
            if (!settings) settings = Resources.Load<BraceletSettings>("Brazalete/ConfiguracionBrazalete");
            Progress = GetComponent<BraceletProgress>();
            Progress.Configure(settings);
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true;
            audioSource.spatialBlend = 0f;
            BuildControls();
            view = gameObject.AddComponent<BraceletHologram>();
            view.Configure(this);
        }

        private void BuildControls()
        {
            controls = new InputActionMap("Brazalete");
            toggle = Button("AbrirCerrar", "<Keyboard>/tab", "<Gamepad>/start");
            cancel = Button("Cerrar", "<Keyboard>/escape", "<Gamepad>/buttonEast");
            select = Button("Seleccionar", "<Keyboard>/enter", "<Gamepad>/buttonSouth");
            previous = Button("PestanaAnterior", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            next = Button("PestanaSiguiente", "<Keyboard>/e", "<Gamepad>/rightShoulder");
            toggle.performed += _ => toggleRequested = true;
            cancel.performed += _ => cancelRequested = true;
            select.performed += _ => selectRequested = true;
            previous.performed += _ => previousRequested = true;
            next.performed += _ => nextRequested = true;
            navigate = controls.AddAction("Navegar", InputActionType.Value, expectedControlLayout: "Vector2");
            navigate.AddBinding("<Gamepad>/leftStick");
            navigate.AddBinding("<Gamepad>/dpad");
            navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            navigate.AddCompositeBinding("2DVector").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        }

        private InputAction Button(string name, string keyboard, string gamepad)
        {
            var action = controls.AddAction(name, InputActionType.Button);
            action.AddBinding(keyboard);
            action.AddBinding(gamepad);
            return action;
        }

        private void OnEnable() => controls?.Enable();
        private void OnDisable()
        {
            controls?.Disable();
            toggleRequested = cancelRequested = selectRequested = previousRequested = nextRequested = false;
            Restore();
        }

        private void OnDestroy()
        {
            Restore();
            controls?.Dispose();
            if (audioSource) Destroy(audioSource);
            if (view) Destroy(view);
        }

        private void Update()
        {
            bool pressedToggle = toggleRequested, pressedCancel = cancelRequested, pressedSelect = selectRequested;
            bool pressedPrevious = previousRequested, pressedNext = nextRequested;
            toggleRequested = cancelRequested = selectRequested = previousRequested = nextRequested = false;
            // Skip the frame that returned control, so a UI button cannot also become a gameplay press.
            if (Time.unscaledTime < releasedAt) return;
            if (pressedToggle)
            {
                if (!IsActive) { if (TryOpen()) return; }
                else Close();
            }
            if (!IsActive) return;
            if (pressedCancel) Close();
            phaseTime += Time.unscaledDeltaTime;
            if (Phase == MenuPhase.Closing) return;
            openingTime += Time.unscaledDeltaTime;
            float frameDuration = 1f / settings.framesPerSecond;
            float projectionStart = frameDuration * 5f;
            Projection = Mathf.Clamp01((openingTime - projectionStart) / settings.projectionSeconds);
            Frame = openingTime < frameDuration * 6f ? Mathf.Min(5, (int)(openingTime / frameDuration))
                : 6 + (int)((openingTime - frameDuration * 6f) / frameDuration) % 2;
            if (Projection < 1f) return;
            Phase = MenuPhase.Open;
            if (pressedPrevious || pressedNext) SetTab(1 - Tab);
            Navigate();
            if (pressedSelect) Select();
        }

        public bool TryOpen()
        {
            if (IsActive || !player.enabled || !player.CanAct || !player.IsGrounded || Time.timeScale <= 0f) return false;
            if (player.CurrentState == PlayerController.AnimState.Hurt || player.CurrentState == PlayerController.AnimState.Spawn) return false;
            spear = GetComponent<KaelSpear>();
            shooter = GetComponent<KaelShooter>();
            if ((spear && spear.IsAttacking) || (shooter && shooter.Rooted) || BusyAnimation()) return false;
            if (!visual || !settings || settings.frames == null || settings.frames.Length != 8) return false;
            previousTimeScale = Time.timeScale;
            previousVelocity = body.linearVelocity;
            previousScale = player.transform.localScale;
            previousSprite = visual.sprite;
            previousVisibility = visual.enabled;
            captured = true;
            suspended.Clear();
            Suspend(player);
            Suspend(animator);
            foreach (var driver in GetComponentsInChildren<KaelAnimationDriver>()) Suspend(driver);
            Suspend(spear);
            Suspend(shooter);
            body.linearVelocity = Vector2.zero;
            focus.Capture(Camera.main, GetComponent<Collider2D>().bounds, settings.zoom, settings.minimumVisiblePlayerHeights);
            Time.timeScale = 0f;
            Frame = 0;
            openingTime = phaseTime = Projection = 0f;
            Message = "";
            Phase = MenuPhase.Opening;
            lastMove = Vector2Int.zero;
            Play(abrir);
            return true;
        }

        private bool BusyAnimation()
        {
            if (!animator || !animator.enabled || !animator.runtimeAnimatorController) return false;
            return BusyState(animator.GetCurrentAnimatorStateInfo(0)) ||
                (animator.IsInTransition(0) && BusyState(animator.GetNextAnimatorStateInfo(0)));
        }

        private static bool BusyState(AnimatorStateInfo state)
        {
            return state.IsName("Ataque_Lanza") || state.IsName("Ataque_Aereo_Lanza") || state.IsName("Ataque_Arco") ||
                state.IsName("Arco_Diagonal_Arriba") || state.IsName("Danio") || state.IsName("Rodar");
        }

        private void Suspend(Behaviour behaviour)
        {
            if (!behaviour || !behaviour.enabled) return;
            suspended.Add(behaviour);
            behaviour.enabled = false;
        }

        public void Close()
        {
            if (!IsActive || Phase == MenuPhase.Closing) return;
            closingProjection = Projection;
            closingFocus = Mathf.Clamp01(openingTime / settings.cameraSeconds);
            phaseTime = 0f;
            Phase = MenuPhase.Closing;
            Play(cerrar);
        }

        private void LateUpdate()
        {
            if (!captured) return;
            if (Phase == MenuPhase.Closing)
            {
                float projectionTime = settings.projectionSeconds * closingProjection;
                Projection = projectionTime > 0f ? closingProjection * (1f - Mathf.Clamp01(phaseTime / projectionTime)) : 0f;
                float animationTime = Mathf.Max(0f, phaseTime - projectionTime);
                Frame = Mathf.Max(0, 5 - (int)(animationTime * settings.framesPerSecond));
                float duration = projectionTime + 6f / settings.framesPerSecond;
                focus.Evaluate(closingFocus * (1f - Mathf.Clamp01(phaseTime / duration)));
                if (phaseTime >= duration) { Restore(); return; }
            }
            else focus.Evaluate(Mathf.Clamp01(openingTime / settings.cameraSeconds));
            visual.sprite = settings.frames[Frame];
            visual.enabled = true;
        }

        private void Restore()
        {
            if (!captured) return;
            captured = false;
            focus.Restore();
            if (visual) { visual.sprite = previousSprite; visual.enabled = previousVisibility; }
            if (body) body.linearVelocity = previousVelocity;
            if (player) player.transform.localScale = previousScale;
            foreach (var behaviour in suspended) if (behaviour) behaviour.enabled = true;
            suspended.Clear();
            Time.timeScale = previousTimeScale;
            Phase = MenuPhase.Closed;
            Projection = 0f;
            releasedAt = Time.unscaledTime + .02f;
        }

        public void SetTab(int tab)
        {
            if (Phase != MenuPhase.Open) return;
            if (Tab == 0) upgradesSelection = Selection;
            else inventorySelection = Selection;
            Tab = tab;
            Selection = Tab == 0 ? upgradesSelection : inventorySelection;
            Moved();
        }

        private void Navigate()
        {
            Vector2 value = navigate.ReadValue<Vector2>();
            Vector2Int direction = Vector2Int.zero;
            if (Mathf.Abs(value.x) >= settings.deadZone || Mathf.Abs(value.y) >= settings.deadZone)
                direction = Mathf.Abs(value.x) > Mathf.Abs(value.y) ? new Vector2Int((int)Mathf.Sign(value.x), 0) : new Vector2Int(0, (int)Mathf.Sign(value.y));
            if (direction == Vector2Int.zero) { lastMove = direction; return; }
            bool fresh = direction != lastMove;
            if (!fresh && Time.unscaledTime < nextMove) return;
            nextMove = Time.unscaledTime + (fresh ? settings.repeatDelay : settings.repeatInterval);
            lastMove = direction;
            int index = Selection;
            if (Tab == 1)
            {
                int x = Mathf.Clamp(Selection % 4 + direction.x, 0, 3);
                int y = Mathf.Clamp(Selection / 4 - direction.y, 0, 2);
                index = y * 4 + x;
            }
            else if (settings.upgrades.Length > 0)
            {
                Vector2 origin = settings.upgrades[Selection].position;
                float nearest = float.PositiveInfinity;
                for (int i = 0; i < settings.upgrades.Length; i++)
                {
                    Vector2 delta = settings.upgrades[i].position - origin;
                    if (Vector2.Dot(delta, new Vector2(direction.x, -direction.y)) <= 0f) continue;
                    float score = delta.sqrMagnitude / Vector2.Dot(delta.normalized, new Vector2(direction.x, -direction.y));
                    if (score < nearest) { nearest = score; index = i; }
                }
            }
            if (index == Selection) return;
            Selection = index;
            Moved();
        }

        private void Moved()
        {
            CursorTime = Time.unscaledTime;
            Message = "";
            Play(moverCursor);
        }

        public void Select()
        {
            if (Phase != MenuPhase.Open) return;
            if (Tab == 1)
            {
                if (!view.IsFruit(Selection)) return;
                if (Progress.UseFruit()) { Message = "FRUTA CONSUMIDA: +1 CORAZON"; Play(comprarMejora); }
                else { Message = "LA VIDA YA ESTA COMPLETA"; Play(error); }
                return;
            }
            if (settings.upgrades.Length == 0) return;
            var upgrade = settings.upgrades[Selection];
            if (Progress.TryPurchase(Selection))
            {
                Message = "MEJORA INSTALADA";
                Play(comprarMejora);
            }
            else
            {
                Message = Progress.Owns(upgrade.id) ? "YA INSTALADA" : !Progress.Available(Selection) ? "REQUIERE OTRA MEJORA"
                    : Progress.Fragments < upgrade.cost ? "FALTAN FRAGMENTOS" : "FALTAN NUCLEOS DE CENTINELA";
                Play(error);
            }
        }

        private void Play(AudioClip clip) { if (clip && audioSource) audioSource.PlayOneShot(clip, volume); }
    }
}
