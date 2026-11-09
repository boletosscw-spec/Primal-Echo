using UnityEngine;
using UnityEngine.InputSystem;

namespace PrimalEcho
{
    [RequireComponent(typeof(Animator))]
    public class KaelAnimationDriver : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private GameObject arrowPrefab;

        [Header("Configuración de Movimiento")]
        [SerializeField] private float maxRunSpeed = 8.0f;

        private Animator animator;
        public GameObject ArrowPrefab => arrowPrefab;
        // Control de Xbox (o cualquier gamepad): Y arco · LB bloquear · RB/RT rodar-dash · B interactuar
        private static Gamepad Pad => Gamepad.current;
        private static bool PadDown(UnityEngine.InputSystem.Controls.ButtonControl b) => b != null && b.wasPressedThisFrame;
        private static bool PadHeld(UnityEngine.InputSystem.Controls.ButtonControl b) => b != null && b.isPressed;
        private PlayerController.AnimState lastControllerState = PlayerController.AnimState.Idle;
        private bool isClimbingTest;
        private float bowShotTimer;
        private Vector2 pendingBowDir;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int VerticalVelHash = Animator.StringToHash("VerticalVelocity");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
        private static readonly int IsClimbingHash = Animator.StringToHash("IsClimbing");
        private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");

        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int RollHash = Animator.StringToHash("Roll");
        private static readonly int AttackSpearHash = Animator.StringToHash("AttackSpear");
        private static readonly int AttackSpearAirHash = Animator.StringToHash("AttackSpearAir");
        private static readonly int AttackBowHash = Animator.StringToHash("AttackBow");
        private static readonly int AttackBowDiagHash = Animator.StringToHash("AttackBowDiagonal");
        private static readonly int BlockHash = Animator.StringToHash("Block");
        private static readonly int HurtHash = Animator.StringToHash("Hurt");
        private static readonly int DieHash = Animator.StringToHash("Die");
        private static readonly int InteractHash = Animator.StringToHash("Interact");
        private static readonly int VictoryHash = Animator.StringToHash("Victory");
        private static readonly int RepairKiroHash = Animator.StringToHash("RepairKiro");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureDriverAttachedAtRuntime()
        {
            PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
            if (pc != null && pc.GetComponent<KaelAnimationDriver>() == null)
            {
                pc.gameObject.AddComponent<KaelAnimationDriver>();
            }
        }

        private void Awake()
        {
            animator = GetComponent<Animator>();
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();

#if UNITY_EDITOR
            if (animator != null && animator.runtimeAnimatorController == null)
            {
                animator.runtimeAnimatorController = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                    "Assets/Animations/Kael/Kael.controller"
                );
            }
            if (arrowPrefab == null)
            {
                arrowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Arrow.prefab"
                );
            }
#endif
        }

        public void SetArrowPrefab(GameObject prefab)
        {
            arrowPrefab = prefab;
        }

        private void Update()
        {
            if (animator == null || rb == null) return;

            bool isGrounded = playerController != null && playerController.IsGrounded;
            float horizInput = playerController != null ? Mathf.Abs(playerController.MoveInput.x) : 0f;
            float velSpeed = Mathf.Abs(rb.linearVelocity.x) / Mathf.Max(0.1f, maxRunSpeed);
            float normalizedSpeed = Mathf.Max(horizInput, velSpeed);

            // Si mantiene LeftAlt camina (Speed ~ 0.35 -> estado Caminar), si no corre (Speed ~ 1.0 -> estado Correr)
            if (normalizedSpeed > 0.1f)
            {
                float stickX = Pad != null ? Mathf.Abs(Pad.leftStick.x.ReadValue()) : 0f;
                bool walkSlow = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || (stickX > 0.1f && stickX < 0.55f);
                normalizedSpeed = walkSlow ? 0.35f : Mathf.Max(0.75f, normalizedSpeed);
            }
            else
            {
                normalizedSpeed = 0f;
            }

            bool isCrouching = isGrounded && (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)
                || (playerController != null && playerController.MoveInput.y < -0.6f));
            if (isCrouching)
            {
                normalizedSpeed = 0f;
            }

            animator.SetFloat(SpeedHash, normalizedSpeed);
            animator.SetFloat(VerticalVelHash, rb.linearVelocity.y);
            animator.SetBool(IsGroundedHash, isGrounded);
            animator.SetBool(IsCrouchingHash, isCrouching);

            // Sincronizar con estados de PlayerController (Salto, Doble Salto, Daño, Muerte, Victoria)
            if (playerController != null)
            {
                var currentState = playerController.CurrentState;
                if (currentState != lastControllerState)
                {
                    if (currentState == PlayerController.AnimState.Jump ||
                        currentState == PlayerController.AnimState.DoubleJump)
                    {
                        animator.ResetTrigger(JumpHash);
                        animator.SetTrigger(JumpHash);
                    }
                    else if (currentState == PlayerController.AnimState.Hurt)
                    {
                        animator.SetTrigger(HurtHash);
                    }
                    else if (currentState == PlayerController.AnimState.Death)
                    {
                        animator.SetTrigger(DieHash);
                    }
                    else if (currentState == PlayerController.AnimState.Victory)
                    {
                        animator.SetTrigger(VictoryHash);
                    }
                    lastControllerState = currentState;
                }
            }

            // Rodar (LeftShift o C)
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.C)
                || (Pad != null && (PadDown(Pad.rightShoulder) || PadDown(Pad.rightTrigger))))
            {
                animator.SetTrigger(RollHash);
            }

            // Ataque con Arco: horizontal o diagonal arriba si mantiene W/Arriba (K, X o Clic Derecho)
            // Si Kael tiene KaelShooter (disparo estilo Cuphead), ese script se encarga del arco
            if (GetComponentInParent<KaelShooter>() == null &&
                (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.X) || Input.GetMouseButtonDown(1)
                || (Pad != null && PadDown(Pad.buttonNorth))))
            {
                bool diagUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)
                    || (playerController != null && playerController.MoveInput.y > 0.5f);
                animator.SetTrigger(diagUp ? AttackBowDiagHash : AttackBowHash);

                float facing = transform.localScale.x >= 0f ? 1f : -1f;
                pendingBowDir = diagUp
                    ? new Vector2(facing, 0.75f).normalized
                    : new Vector2(facing, 0f);
                bowShotTimer = 0.35f;
            }

            if (bowShotTimer > 0f)
            {
                bowShotTimer -= Time.deltaTime;
                if (bowShotTimer <= 0f)
                {
                    SpawnArrow(pendingBowDir);
                }
            }

            // Bloquear (Mantener L o F)
            bool blockPressed = Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.F) || (Pad != null && PadDown(Pad.leftShoulder));
            bool blockHeld = Input.GetKey(KeyCode.L) || Input.GetKey(KeyCode.F) || (Pad != null && PadHeld(Pad.leftShoulder));
            if (blockPressed)
            {
                animator.SetTrigger(BlockHash);
            }
            animator.SetBool(IsBlockingHash, blockHeld);

            // Interactuar (E)
            if (Input.GetKeyDown(KeyCode.E) || (Pad != null && PadDown(Pad.buttonEast)))
            {
                animator.SetTrigger(InteractHash);
            }

            // Reparar a Kiro (R)
            if (Input.GetKeyDown(KeyCode.R))
            {
                animator.SetTrigger(RepairKiroHash);
            }

            // Trepar (Alternar con T para probar o mantener en escalera)
            if (Input.GetKeyDown(KeyCode.T))
            {
                isClimbingTest = !isClimbingTest;
                animator.SetBool(IsClimbingHash, isClimbingTest);
            }
            else if (isClimbingTest && (Input.GetKeyDown(KeyCode.Space) || isGrounded && normalizedSpeed > 0.5f))
            {
                isClimbingTest = false;
                animator.SetBool(IsClimbingHash, false);
            }

            // Teclas rápidas de prueba para Daño (H) y Victoria (V)
            if (Input.GetKeyDown(KeyCode.H))
            {
                animator.SetTrigger(HurtHash);
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                animator.SetTrigger(VictoryHash);
            }
        }

        private void SpawnArrow(Vector2 dir)
        {
            if (arrowPrefab == null) return;
            Vector3 spawnPos = transform.position + new Vector3(dir.x * 0.45f, 0.82f + dir.y * 0.25f, 0f);
            GameObject arrowObj = Instantiate(arrowPrefab, spawnPos, Quaternion.identity);
            ArrowProjectile proj = arrowObj.GetComponent<ArrowProjectile>();
            if (proj == null)
            {
                proj = arrowObj.AddComponent<ArrowProjectile>();
            }
            proj.Launch(dir);
        }

    }
}
