using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace PrimalEcho
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 8.0f;
        [SerializeField] private float acceleration = 75f;
        [SerializeField] private float deceleration = 65f;
        [SerializeField] private float airAcceleration = 65f;

        [Header("Jumping & Double Jump")]
        [SerializeField] private float jumpForce = 12.5f;
        [SerializeField] private float doubleJumpForce = 11.5f;
        [SerializeField] private int maxExtraJumps = 1;
        [SerializeField] private float fallMultiplier = 1.8f;
        [SerializeField] private float lowJumpMultiplier = 2.2f;
        [SerializeField] private float maxFallSpeed = -18f;
        [SerializeField] private float apexThreshold = 1.8f;
        [SerializeField] private float apexGravityMultiplier = 0.65f;
        [Tooltip("Salto de altura fija: soltar el botón no corta el salto (también el doble salto)")]
        [SerializeField] private bool fixedHeightJump = true;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayer = 1 << 6;

        [SerializeField] private float probeOffset = 0.05f;
        [SerializeField, Range(0f, 1f)] private float feetWidth = 0.75f;
        [SerializeField] private float detachTime = 0.09f;
        [SerializeField, Range(0f, 1f)] private float jumpCut = 0.45f;
        [SerializeField] private float facingDeadZone = 0.05f;

        [SerializeField] private KaelTraversal traversal = new KaelTraversal();

        private InputAction move, jump, dash;
        private bool dashRequested, cutRequested;
        private float lockedUntil;
        private readonly Collider2D[] terrainHits = new Collider2D[1];
        public bool IsGrounded => isGrounded;
        public float Facing => isFacingRight ? 1f : -1f;
        public bool CanAct => currentState != AnimState.Death && currentState != AnimState.Victory;
        public Vector2 MoveInput => move.ReadValue<Vector2>();

        [Header("Sprites & Animation")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] idleSprites;
        [SerializeField] private Sprite[] runSprites;
        [SerializeField] private Sprite[] jumpSprites;
        [SerializeField] private Sprite[] fallSprites;
        [SerializeField] private Sprite[] landSprites;
        [SerializeField] private Sprite[] hurtSprites;
        [SerializeField] private Sprite[] deathSprites;
        [SerializeField] private Sprite[] victorySprites;
        [SerializeField] private Sprite[] spawnSprites;

        [Header("Fluid Animation Frame Rates")]
        [SerializeField] private float idleFrameRate = 20f;
        [SerializeField] private float runFrameRate = 28f;
        [SerializeField] private float jumpFrameRate = 48f;
        [SerializeField] private float fallFrameRate = 32f;
        [SerializeField] private float landFrameRate = 24f;
        [SerializeField] private float hurtFrameRate = 16f;
        [SerializeField] private float deathFrameRate = 15f;
        [SerializeField] private float victoryFrameRate = 16f;
        [SerializeField] private float spawnFrameRate = 18f;

        [Header("Fluidity & VFX")]
        [SerializeField] private bool enableSquashAndStretch = true;
        [SerializeField] private bool enableDoubleJumpVFX = true;

        private Rigidbody2D rb;
        private CapsuleCollider2D col;
        private bool isGrounded;
        private bool wasGrounded = true;
        private int extraJumpsRemaining;
        private float jumpGraceTimer;
        private float coyoteCounter;
        private float jumpBufferCounter;
        private float horizontalInput;
        private bool isFacingRight = true;
        private float preLandYVelocity;

        // Smooth squash & stretch preserving user's base Transform scale
        private Vector3 baseScale = Vector3.one;
        private Vector2 squashStretch = Vector2.one;
        private float doubleJumpFlashTimer;

        // Active runtime sub-arrays for jump/fall/land phases
        private Sprite[] activeJumpSprites;
        private Sprite[] activeFallSprites;
        private Sprite[] activeLandSprites;

        // Cached procedural sprite for Double Jump VFX
        private static Sprite cachedRingSprite;

        // Animation state
        private float animTimer;
        private int currentFrame;

        public enum AnimState { Idle, Run, Jump, DoubleJump, Fall, Land, Hurt, Death, Victory, Spawn }
        private AnimState currentState = AnimState.Idle;

        public AnimState CurrentState => currentState;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<CapsuleCollider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            // Los sprites de Kael tienen pivote BottomCenter (0.5, 0) en los pies (localY = 0).
            // Aseguramos que la base del CapsuleCollider2D coincida exactamente con los pies (localY = 0) para que no flote.
            if (col != null && Mathf.Abs(col.offset.y - col.size.y * 0.5f) > 0.05f)
            {
                col.offset = new Vector2(0f, col.size.y * 0.5f);
            }

            Vector3 s = transform.localScale;
            baseScale = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), s.z);
            isFacingRight = s.x >= 0f;

            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.freezeRotation = true;

            if (groundCheckPoint == null)
            {
                Transform existing = transform.Find("GroundCheckPoint");
                if (existing != null)
                {
                    groundCheckPoint = existing;
                }
            }

            move = InputSystem.actions.FindAction("Player/Move", true);
            jump = InputSystem.actions.FindAction("Player/Jump", true);
            dash = InputSystem.actions.FindAction("Player/Dash", true);
            extraJumpsRemaining = maxExtraJumps;

#if UNITY_EDITOR
            EnsureEditorSpritesLoaded();
#endif
            PrepareJumpFallLandArrays();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                EnsureEditorSpritesLoaded();
            }
        }

        public void EnsureEditorSpritesLoaded()
        {
            const string basePath = "Assets/Sprites/Personajes/Kael_Primal_Echo_Sprites_Mejorados/Spritesheets_Transparentes/";

            if (idleSprites == null || idleSprites.Length == 0)
                idleSprites = LoadSpritesFromSheet(basePath + "KaelIdle_Transparente.png", "KaelIdle_Transparente", 40);

            if (runSprites == null || runSprites.Length == 0)
                runSprites = LoadSpritesFromSheet(basePath + "KaelRun_Transparente.png", "KaelRun_Transparente", 40);

            Sprite[] allJump = LoadSpritesFromSheet(basePath + "KaelJump_Transparente.png", "KaelJump_Transparente", 40);
            Sprite[] allLand = LoadSpritesFromSheet(basePath + "KaelLand_Transparente.png", "KaelLand_Transparente", 40);

            if (allJump != null && allJump.Length >= 42)
            {
                if (jumpSprites == null || jumpSprites.Length == 0 || jumpSprites.Length == 49)
                {
                    // Frames 3..27: takeoff, upward ascent and mid-air apex tuck
                    jumpSprites = SliceArray(allJump, 3, 27);
                }
                if (fallSprites == null || fallSprites.Length == 0)
                {
                    // Frames 28..41: mid-air descent with legs extended downward
                    List<Sprite> fallList = new List<Sprite>(SliceArray(allJump, 28, 41));
                    if (allLand != null && allLand.Length > 0)
                    {
                        fallList.Add(allLand[0]);
                    }
                    fallSprites = fallList.ToArray();
                }
            }

            if (allLand != null && allLand.Length >= 5)
            {
                if (landSprites == null || landSprites.Length == 0)
                {
                    landSprites = allLand;
                }
            }

            if (hurtSprites == null || hurtSprites.Length == 0)
                hurtSprites = LoadSpritesFromSheet(basePath + "KaelHurt_Transparente.png", "KaelHurt_Transparente", 40);

            if (deathSprites == null || deathSprites.Length == 0)
                deathSprites = LoadSpritesFromSheet(basePath + "KaelDeath_Transparente.png", "KaelDeath_Transparente", 40);

            if (victorySprites == null || victorySprites.Length == 0)
                victorySprites = LoadSpritesFromSheet(basePath + "KaelVictory_Transparente.png", "KaelVictory_Transparente", 40);

            if (spawnSprites == null || spawnSprites.Length == 0)
                spawnSprites = LoadSpritesFromSheet(basePath + "KaelSpawn_Transparente.png", "KaelSpawn_Transparente", 40);

            if (idleFrameRate < 18f) idleFrameRate = 20f;
            if (runFrameRate < 24f) runFrameRate = 28f;
            if (jumpFrameRate < 40f) jumpFrameRate = 48f;
            if (fallFrameRate < 28f) fallFrameRate = 32f;
            if (landFrameRate < 20f) landFrameRate = 24f;
        }

        private static Sprite[] LoadSpritesFromSheet(string path, string prefix, float minDimension)
        {
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (allAssets == null || allAssets.Length == 0) return null;

            List<Sprite> list = new List<Sprite>();
            foreach (var a in allAssets)
            {
                if (a is Sprite s && s.name.StartsWith(prefix))
                {
                    if (s.rect.width >= minDimension && s.rect.height >= minDimension)
                    {
                        list.Add(s);
                    }
                }
            }
            list.Sort((a, b) => ExtractFrameIndex(a.name).CompareTo(ExtractFrameIndex(b.name)));
            return list.ToArray();
        }

        private static int ExtractFrameIndex(string name)
        {
            int idx = name.LastIndexOf('_');
            if (idx >= 0 && int.TryParse(name.Substring(idx + 1), out int val))
                return val;
            return 0;
        }
#endif

        private void PrepareJumpFallLandArrays()
        {
            if (jumpSprites != null && jumpSprites.Length >= 42)
            {
                activeJumpSprites = SliceArray(jumpSprites, 3, 27);
                if (fallSprites == null || fallSprites.Length == 0)
                {
                    activeFallSprites = SliceArray(jumpSprites, 28, 41);
                }
                else
                {
                    activeFallSprites = fallSprites;
                }
            }
            else
            {
                activeJumpSprites = jumpSprites;
                activeFallSprites = (fallSprites != null && fallSprites.Length > 0) ? fallSprites : jumpSprites;
            }

            if (landSprites != null && landSprites.Length >= 5)
            {
                activeLandSprites = SliceArray(landSprites, 1, landSprites.Length - 1);
            }
            else
            {
                activeLandSprites = landSprites;
            }

            if (idleFrameRate < 18f) idleFrameRate = 20f;
            if (runFrameRate < 24f) runFrameRate = 28f;
            if (jumpFrameRate < 40f) jumpFrameRate = 48f;
            if (fallFrameRate < 28f) fallFrameRate = 32f;
            if (landFrameRate < 20f) landFrameRate = 24f;
        }

        private static Sprite[] SliceArray(Sprite[] source, int startInclusive, int endInclusive)
        {
            if (source == null || source.Length == 0) return source;
            startInclusive = Mathf.Clamp(startInclusive, 0, source.Length - 1);
            endInclusive = Mathf.Clamp(endInclusive, startInclusive, source.Length - 1);
            int count = endInclusive - startInclusive + 1;
            Sprite[] result = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = source[startInclusive + i];
            }
            return result;
        }

        private void OnEnable()
        {
            jump.performed += QueueJump;
            jump.canceled += CutJump;
            dash.performed += QueueDash;
        }

        private void OnDisable()
        {
            jump.performed -= QueueJump;
            jump.canceled -= CutJump;
            dash.performed -= QueueDash;
            jumpBufferCounter = 0f;
            dashRequested = cutRequested = false;
            traversal.CancelDash();
        }

        private void QueueJump(InputAction.CallbackContext context)
        {
            if (CanAct) jumpBufferCounter = jumpBufferTime;
        }

        private void CutJump(InputAction.CallbackContext context) => cutRequested = true;
        private void QueueDash(InputAction.CallbackContext context) => dashRequested = CanAct;

        private bool TouchesTerrain(Vector2 center, Vector2 size)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(groundLayer);
            return Physics2D.OverlapBox(center, size, 0f, filter, terrainHits) > 0;
        }

        private bool CheckGrounded()
        {
            if (jumpGraceTimer > 0f || rb.linearVelocity.y > 0f) return false;
            Bounds bounds = col.bounds;
            Vector2 feet = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : new Vector2(bounds.center.x, bounds.min.y);
            return TouchesTerrain(feet + Vector2.down * probeOffset,
                new Vector2(bounds.size.x * feetWidth, groundCheckRadius));
        }

        private int WallSide()
        {
            Bounds bounds = col.bounds;
            Vector2 size = traversal.ProbeSize(bounds);
            Vector2 offset = Vector2.right * (bounds.extents.x + size.x * 0.5f);
            if (TouchesTerrain((Vector2)bounds.center + offset, size)) return 1;
            if (TouchesTerrain((Vector2)bounds.center - offset, size)) return -1;
            return 0;
        }

        private void Update()
        {
            horizontalInput = CanAct ? MoveInput.x : 0f;
            if (CanAct)
            {
                if (Mathf.Abs(horizontalInput) > facingDeadZone)
                    isFacingRight = horizontalInput > 0f;
            }
            UpdateVisualPolish();
            UpdateAnimation();
        }

        private void UpdateGround()
        {
            jumpGraceTimer -= Time.fixedDeltaTime;
            wasGrounded = isGrounded;
            isGrounded = CheckGrounded();
            if (!isGrounded)
            {
                coyoteCounter -= Time.fixedDeltaTime;
                preLandYVelocity = rb.linearVelocity.y;
                return;
            }
            coyoteCounter = coyoteTime;
            extraJumpsRemaining = maxExtraJumps;
            if (!wasGrounded && preLandYVelocity < -3f)
            {
                float impact = Mathf.Clamp01(Mathf.Abs(preLandYVelocity) / 18f);
                TriggerSquashStretch(new Vector2(1f + 0.12f * impact, 1f - 0.12f * impact));
                SpawnDustPuff(3);
            }
        }

        private void TryJump()
        {
            if (jumpBufferCounter <= 0f) return;
            if (isGrounded || coyoteCounter > 0f)
                ExecuteJump(jumpForce, false);
            else
            {
                int wall = WallSide();
                if (wall != 0)
                {
                    ExecuteJump(traversal.WallJump.y, false);
                    rb.linearVelocityX = -wall * traversal.WallJump.x;
                    lockedUntil = Time.fixedTime + traversal.WallControlDelay;
                    isFacingRight = wall < 0;
                }
                else if (extraJumpsRemaining > 0)
                {
                    extraJumpsRemaining--;
                    ExecuteJump(doubleJumpForce, true);
                }
            }
            jumpBufferCounter -= Time.fixedDeltaTime;
        }

        private void ExecuteJump(float force, bool isDoubleJump)
        {
            float vx = rb.linearVelocity.x;
            // On double jump, allow instant horizontal redirect in mid-air like Hollow Knight's Monarch Wings
            if (isDoubleJump && Mathf.Abs(horizontalInput) > 0.1f)
            {
                vx = horizontalInput * moveSpeed;
            }

            rb.linearVelocity = new Vector2(vx, force);
            jumpBufferCounter = 0f;
            coyoteCounter = 0f;
            jumpGraceTimer = detachTime;
            cutRequested |= !jump.IsPressed();
            isGrounded = false;

            if (isDoubleJump)
            {
                currentState = AnimState.DoubleJump;
                // Start at launch frame of jump animation for immediate upward kick
                currentFrame = 1;
                animTimer = 0f;
                doubleJumpFlashTimer = 0.18f;
                TriggerSquashStretch(new Vector2(0.88f, 1.14f));
                if (enableDoubleJumpVFX)
                {
                    SpawnDoubleJumpWingsVFX();
                }
            }
            else
            {
                currentState = AnimState.Jump;
                currentFrame = 0;
                animTimer = 0f;
                TriggerSquashStretch(new Vector2(0.91f, 1.10f));
                SpawnDustPuff(4);
            }
        }

        private void FixedUpdate()
        {
            if (!CanAct)
            {
                traversal.CancelDash();
                jumpBufferCounter = 0f;
                dashRequested = cutRequested = false;
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                return;
            }

            UpdateGround();
            float gravityStep = Physics2D.gravity.y * rb.gravityScale * Time.fixedDeltaTime;
            bool dashing = traversal.StepDash(rb, dashRequested && Time.fixedTime >= lockedUntil, Facing, gravityStep);
            dashRequested = false;
            if (dashing)
            {
                jumpBufferCounter -= Time.fixedDeltaTime;
                return;
            }

            TryJump();
            if (!fixedHeightJump && cutRequested && rb.linearVelocity.y > 0f) rb.linearVelocityY *= jumpCut;
            cutRequested = false;

            // Responsive horizontal movement
            float targetSpeed = horizontalInput * moveSpeed;
            float accel = isGrounded
                ? ((Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration)
                : airAcceleration;
            float newSpeed = Time.fixedTime < lockedUntil ? rb.linearVelocity.x : Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);

            // Hollow Knight gravity curve: floaty apex + snappy fall + terminal velocity clamp
            float yVel = rb.linearVelocity.y;
            float multiplier = 1f;
            if (yVel < 0f) multiplier = fallMultiplier;
            else if (!isGrounded && yVel < apexThreshold && (jump.IsPressed() || fixedHeightJump)) multiplier = apexGravityMultiplier;
            else if (yVel > 0f && !jump.IsPressed() && !fixedHeightJump) multiplier = lowJumpMultiplier;
            // Unity applies its own gravity step immediately after FixedUpdate.
            yVel = Mathf.Max(maxFallSpeed, yVel + gravityStep * multiplier) - gravityStep;
            rb.linearVelocity = new Vector2(newSpeed, yVel);
        }

        private void TriggerSquashStretch(Vector2 factor)
        {
            if (!enableSquashAndStretch) return;
            squashStretch = factor;
        }

        private void UpdateVisualPolish()
        {
            // Smoothly interpolate squash & stretch back to (1, 1)
            squashStretch = Vector2.Lerp(squashStretch, Vector2.one, 16f * Time.deltaTime);
            float dir = isFacingRight ? 1f : -1f;
            transform.localScale = new Vector3(
                baseScale.x * squashStretch.x * dir,
                baseScale.y * squashStretch.y,
                baseScale.z
            );

            // Subtle cyan/white glow flash on Double Jump
            if (spriteRenderer != null)
            {
                if (doubleJumpFlashTimer > 0f)
                {
                    doubleJumpFlashTimer -= Time.deltaTime;
                    float t = Mathf.Clamp01(doubleJumpFlashTimer / 0.18f);
                    spriteRenderer.color = Color.Lerp(Color.white, new Color(0.75f, 0.98f, 1f, 1f), t);
                }
                else if (spriteRenderer.color != Color.white)
                {
                    spriteRenderer.color = Color.white;
                }
            }
        }

        private void UpdateAnimation()
        {
            if (currentState != AnimState.Death && currentState != AnimState.Victory &&
                currentState != AnimState.Hurt && currentState != AnimState.Spawn)
            {
                AnimState targetState = currentState;

                if (!isGrounded)
                {
                    if (rb.linearVelocity.y > 0.05f)
                    {
                        targetState = (currentState == AnimState.DoubleJump) ? AnimState.DoubleJump : AnimState.Jump;
                    }
                    else
                    {
                        targetState = AnimState.Fall;
                    }
                }
                else
                {
                    if (!wasGrounded && (currentState == AnimState.Fall || currentState == AnimState.Jump || currentState == AnimState.DoubleJump) &&
                        activeLandSprites != null && activeLandSprites.Length > 0)
                    {
                        targetState = AnimState.Land;
                    }
                    else if (currentState == AnimState.Land)
                    {
                        // Cancel landing animation immediately into Run if moving horizontally after first impact frame
                        if (Mathf.Abs(horizontalInput) > 0.1f && currentFrame >= 1)
                        {
                            targetState = AnimState.Run;
                        }
                        else
                        {
                            targetState = AnimState.Land;
                        }
                    }
                    else if (Mathf.Abs(horizontalInput) > 0.1f || Mathf.Abs(rb.linearVelocity.x) > 0.4f)
                    {
                        targetState = AnimState.Run;
                    }
                    else
                    {
                        targetState = AnimState.Idle;
                    }
                }

                if (targetState != currentState)
                {
                    currentState = targetState;
                    currentFrame = 0;
                    animTimer = 0f;
                }
            }

            Sprite[] currentSprites = null;
            float frameRate = 20f;
            bool loop = true;
            int loopStartFrame = 0;

            switch (currentState)
            {
                case AnimState.Idle:
                    currentSprites = idleSprites;
                    frameRate = idleFrameRate;
                    loop = true;
                    break;
                case AnimState.Run:
                    currentSprites = runSprites;
                    // Scale run animation speed smoothly with actual velocity for zero foot-sliding
                    float speedRatio = Mathf.Clamp(Mathf.Abs(rb.linearVelocity.x) / Mathf.Max(1f, moveSpeed), 0.65f, 1.25f);
                    frameRate = runFrameRate * speedRatio;
                    loop = true;
                    break;
                case AnimState.Jump:
                case AnimState.DoubleJump:
                    currentSprites = (activeJumpSprites != null && activeJumpSprites.Length > 0) ? activeJumpSprites : jumpSprites;
                    frameRate = (currentState == AnimState.DoubleJump) ? jumpFrameRate * 1.15f : jumpFrameRate;
                    loop = false;
                    break;
                case AnimState.Fall:
                    currentSprites = (activeFallSprites != null && activeFallSprites.Length > 0) ? activeFallSprites :
                                     ((landSprites != null && landSprites.Length > 0) ? landSprites : jumpSprites);
                    frameRate = fallFrameRate;
                    loop = true;
                    loopStartFrame = (currentSprites != null && currentSprites.Length > 6) ? 4 : 0;
                    break;
                case AnimState.Land:
                    currentSprites = (activeLandSprites != null && activeLandSprites.Length > 0) ? activeLandSprites : landSprites;
                    frameRate = landFrameRate;
                    loop = false;
                    break;
                case AnimState.Hurt:
                    currentSprites = hurtSprites;
                    frameRate = hurtFrameRate;
                    loop = false;
                    break;
                case AnimState.Death:
                    currentSprites = deathSprites;
                    frameRate = deathFrameRate;
                    loop = false;
                    break;
                case AnimState.Victory:
                    currentSprites = victorySprites;
                    frameRate = victoryFrameRate;
                    loop = false;
                    break;
                case AnimState.Spawn:
                    currentSprites = spawnSprites;
                    frameRate = spawnFrameRate;
                    loop = false;
                    break;
            }

            if (currentSprites != null && currentSprites.Length > 0)
            {
                animTimer += Time.deltaTime;
                float frameDuration = 1f / Mathf.Max(1f, frameRate);

                // Advance frames smoothly (handles high frame rates cleanly)
                while (animTimer >= frameDuration)
                {
                    animTimer -= frameDuration;
                    if (currentFrame < currentSprites.Length - 1)
                    {
                        currentFrame++;
                    }
                    else if (loop)
                    {
                        currentFrame = Mathf.Clamp(loopStartFrame, 0, currentSprites.Length - 1);
                    }
                    else
                    {
                        if (currentState == AnimState.Land)
                        {
                            currentState = Mathf.Abs(horizontalInput) > 0.1f ? AnimState.Run : AnimState.Idle;
                            currentFrame = 0;
                            currentSprites = currentState == AnimState.Run ? runSprites : idleSprites;
                        }
                        else if (currentState == AnimState.Hurt || currentState == AnimState.Spawn)
                        {
                            currentState = AnimState.Idle;
                            currentFrame = 0;
                            currentSprites = idleSprites;
                        }
                        break;
                    }
                }

                if (spriteRenderer != null && currentSprites != null && currentSprites.Length > 0)
                {
                    currentFrame = Mathf.Clamp(currentFrame, 0, currentSprites.Length - 1);
                    spriteRenderer.sprite = currentSprites[currentFrame];
                }
            }
        }

        // Procedural Hollow Knight Monarch Wings / Primal Echo Double Jump Burst VFX
        private void SpawnDoubleJumpWingsVFX()
        {
            if (col == null) return;
            Vector3 feetPos = new Vector3(col.bounds.center.x, col.bounds.min.y + 0.1f, transform.position.z);

            GameObject vfxObj = new GameObject("DoubleJump_EchoBurst");
            vfxObj.transform.position = feetPos;

            SpriteRenderer sr = vfxObj.AddComponent<SpriteRenderer>();
            sr.sprite = GetOrCreateRingSprite();
            sr.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 10) - 1;
            sr.color = new Color(0.55f, 0.95f, 1f, 0.85f);

            vfxObj.AddComponent<DoubleJumpBurstEffect>();
            SpawnDustPuff(6, isEnergy: true);
        }

        private void SpawnDustPuff(int count, bool isEnergy = false)
        {
            if (col == null) return;
            Vector3 feetPos = new Vector3(col.bounds.center.x, col.bounds.min.y + 0.05f, transform.position.z);
            Sprite dot = GetOrCreateRingSprite();

            for (int i = 0; i < count; i++)
            {
                GameObject p = new GameObject(isEnergy ? "EchoMote" : "DustMote");
                p.transform.position = feetPos + new Vector3(Random.Range(-0.35f, 0.35f), Random.Range(-0.05f, 0.15f), 0f);
                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = dot;
                sr.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 10) + 1;
                sr.color = isEnergy
                    ? new Color(0.6f, 0.95f, 1f, 0.9f)
                    : new Color(0.85f, 0.9f, 0.85f, 0.65f);

                var mote = p.AddComponent<SimpleParticleMote>();
                float angle = Random.Range(isEnergy ? -160f : 15f, isEnergy ? -20f : 165f) * Mathf.Deg2Rad;
                float spd = Random.Range(1.2f, 3.2f);
                mote.velocity = new Vector2(Mathf.Cos(angle) * spd, Mathf.Sin(angle) * spd * 0.6f);
                mote.initialScale = isEnergy ? Random.Range(0.18f, 0.32f) : Random.Range(0.12f, 0.22f);
            }
        }

        private static Sprite GetOrCreateRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;

            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.48f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center.x) / maxR;
                    float dy = (y - center.y) / (maxR * 0.45f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // Glowing ring + soft core
                    float ring = Mathf.Exp(-Mathf.Pow((dist - 0.65f) * 5.5f, 2f));
                    float core = Mathf.Clamp01(1f - dist) * 0.45f;
                    float alpha = Mathf.Clamp01(ring + core) * Mathf.Clamp01((1f - dist) * 3f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return cachedRingSprite;
        }

        public void ApplyImpulse(Vector2 velocity, float controlDelay)
        {
            traversal.CancelDash();
            dashRequested = cutRequested = false;
            jumpBufferCounter = coyoteCounter = 0f;
            lockedUntil = Time.fixedTime + controlDelay;
            rb.linearVelocity = velocity;
            jumpGraceTimer = detachTime;
            isGrounded = false;
        }

        public void Bounce(float speed) => ApplyImpulse(new Vector2(rb.linearVelocityX, speed), 0f);

        public void PlayHurt()
        {
            if (currentState == AnimState.Death) return;
            currentState = AnimState.Hurt;
            currentFrame = 0;
            animTimer = 0f;
        }

        public void PlayDeath()
        {
            currentState = AnimState.Death;
            currentFrame = 0;
            animTimer = 0f;
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }

        public void PlayVictory()
        {
            if (currentState == AnimState.Death) return;
            currentState = AnimState.Victory;
            currentFrame = 0;
            animTimer = 0f;
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        public void PlaySpawn()
        {
            currentState = AnimState.Spawn;
            currentFrame = 0;
            animTimer = 0f;
        }

        public void ResetState()
        {
            currentState = AnimState.Idle;
            currentFrame = 0;
            animTimer = 0f;
        }

        public void SetSprites(Sprite[] idle, Sprite[] run, Sprite[] jump, Sprite[] land, Sprite[] fall = null)
        {
            idleSprites = idle;
            runSprites = run;
            jumpSprites = jump;
            landSprites = land;
            if (fall != null && fall.Length > 0)
            {
                fallSprites = fall;
            }
            PrepareJumpFallLandArrays();
        }

        public void SetExtraSprites(Sprite[] hurt, Sprite[] death, Sprite[] victory, Sprite[] spawn)
        {
            hurtSprites = hurt;
            deathSprites = death;
            victorySprites = victory;
            spawnSprites = spawn;
        }

        private void OnDrawGizmosSelected()
        {
            if (col == null) col = GetComponent<CapsuleCollider2D>();
            if (col != null)
            {
                Bounds b = col.bounds;
                Vector2 feetCenter = new Vector2(b.center.x, b.min.y) + Vector2.down * probeOffset;
                Vector2 boxSize = new Vector2(b.size.x * feetWidth, groundCheckRadius);
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireCube(feetCenter, boxSize);
            }
        }
    }

    public class DoubleJumpBurstEffect : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float elapsed;
        private const float duration = 0.24f;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            transform.localScale = new Vector3(0.5f, 0.35f, 1f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easeOut = 1f - Mathf.Pow(1f - t, 3f);

            transform.localScale = new Vector3(
                Mathf.Lerp(0.55f, 2.1f, easeOut),
                Mathf.Lerp(0.35f, 0.85f, easeOut),
                1f
            );

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.85f, 0f, t);
                sr.color = c;
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }

    public class SimpleParticleMote : MonoBehaviour
    {
        public Vector2 velocity;
        public float initialScale = 0.2f;
        private SpriteRenderer sr;
        private float elapsed;
        private const float lifetime = 0.32f;

        private void Start()
        {
            sr = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.one * initialScale;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            transform.position += (Vector3)(velocity * Time.deltaTime);
            velocity *= (1f - 4f * Time.deltaTime);
            transform.localScale = Vector3.one * Mathf.Lerp(initialScale, 0.02f, t);

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.8f, 0f, t);
                sr.color = c;
            }

            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
