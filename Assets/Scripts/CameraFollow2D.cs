using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PrimalEcho
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -10f);
        [SerializeField] private float smoothTime = 0.25f;
        [Tooltip("Suavizado vertical: más alto = la cámara casi no brinca cuando Kael salta (como Hollow Knight)")]
        [SerializeField] private float yDamping = 0.45f;
        [Tooltip("Al caer rápido la cámara mira hacia abajo para ver dónde vas a aterrizar")]
        [SerializeField] private float fallLookDistance = 3f;
        [SerializeField] private float fallSpeed = 9f;
        [SerializeField] private Vector2 deadZone = new Vector2(.07f, .09f);
        [SerializeField] private float lookAheadDistance = 2f;
        [SerializeField] private float lookAheadSpeed = 5f;
        [SerializeField] private float verticalHold = .3f;
        [SerializeField] private float verticalDistance = 3f;
        [SerializeField] private float stickThreshold = .5f;
        [SerializeField] private float roomBlend = .45f;
        [SerializeField] private bool useBounds = true;
        [SerializeField] private Vector2 minBounds = new Vector2(-8, 6);
        [SerializeField] private Vector2 maxBounds = new Vector2(108, 18);
        [SerializeField, Min(1)] private int pixelsPerUnit = 32;
        [SerializeField] private Vector2Int referenceResolution = new Vector2Int(684, 384);
        [SerializeField] private float shakeStrength = .14f;
        [SerializeField] private float shakeDuration = .12f;

        private Transform rig;
        private PlayerController player;
        private Rigidbody2D body2D;
        private CinemachineImpulseSource impulse;
        private PolygonCollider2D levelBounds;
        private CinemachineVirtualCamera levelCamera;
        private readonly List<CameraRoom2D> rooms = new List<CameraRoom2D>();
        private readonly Dictionary<CameraRoom2D, CinemachineVirtualCamera> cameras = new Dictionary<CameraRoom2D, CinemachineVirtualCamera>();
        private readonly List<CinemachineFramingTransposer> bodies = new List<CinemachineFramingTransposer>();
        private Vector2 look;
        private int lastVertical;
        private float held;

        private void Awake()
        {
            if (!target) target = GameObject.FindGameObjectWithTag("Player")?.transform;
            SetTarget(target);
            var camera = GetComponent<Camera>();
            camera.orthographic = true;
            if (!TryGetComponent(out PixelPerfectCamera pixels)) pixels = gameObject.AddComponent<PixelPerfectCamera>();
            pixels.assetsPPU = pixelsPerUnit;
            pixels.refResolutionX = referenceResolution.x;
            pixels.refResolutionY = referenceResolution.y;
            pixels.cropFrame = PixelPerfectCamera.CropFrame.StretchFill; // llena toda la pantalla, sin marco negro
            pixels.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            if (!TryGetComponent(out CinemachineBrain brain)) brain = gameObject.AddComponent<CinemachineBrain>();
            brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
            brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, roomBlend);

            rig = new GameObject("Cámaras de sala").transform;
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(rig.gameObject, gameObject.scene);
            levelBounds = rig.gameObject.AddComponent<PolygonCollider2D>();
            levelBounds.isTrigger = true;
            SetBoundsPolygon();
            levelCamera = CreateCamera("Mapa", useBounds ? levelBounds : null);
            impulse = rig.gameObject.AddComponent<CinemachineImpulseSource>();
            impulse.m_ImpulseDefinition.m_ImpulseType = CinemachineImpulseDefinition.ImpulseTypes.Uniform;
            impulse.m_ImpulseDefinition.m_ImpulseShape = CinemachineImpulseDefinition.ImpulseShapes.Bump;
            impulse.m_ImpulseDefinition.m_ImpulseDuration = shakeDuration;
        }

        private CinemachineVirtualCamera CreateCamera(string label, PolygonCollider2D polygon)
        {
            var go = new GameObject(label);
            go.transform.SetParent(rig);
            go.transform.position = transform.position;
            var cam = go.AddComponent<CinemachineVirtualCamera>();
            cam.Follow = target;
            cam.Priority = 0;
            cam.m_Lens.OrthographicSize = HalfHeight;
            var body = cam.AddCinemachineComponent<CinemachineFramingTransposer>();
            body.m_CameraDistance = Mathf.Abs(offset.z);
            body.m_TrackedObjectOffset = new Vector3(offset.x + look.x, offset.y + look.y);
            body.m_DeadZoneWidth = deadZone.x;
            body.m_DeadZoneHeight = deadZone.y;
            body.m_XDamping = smoothTime;
            body.m_YDamping = yDamping;
            bodies.Add(body);
            go.AddComponent<CinemachinePixelPerfect>();
            if (polygon) go.AddComponent<CinemachineConfiner2D>().m_BoundingShape2D = polygon;
            go.AddComponent<CinemachineImpulseListener>();
            return cam;
        }

        private float HalfHeight => referenceResolution.y * .5f / pixelsPerUnit;

        private void SetBoundsPolygon()
        {
            float halfWidth = HalfHeight * referenceResolution.x / referenceResolution.y;
            Vector2 extent = new Vector2(halfWidth, HalfHeight);
            Vector2 min = minBounds - extent, max = maxBounds + extent;
            levelBounds.points = new[] { min, new Vector2(min.x, max.y), max, new Vector2(max.x, min.y) };
        }

        public void SetBounds(Vector2 min, Vector2 max)
        {
            minBounds = min;
            maxBounds = max;
            useBounds = true;
            if (levelBounds) SetBoundsPolygon();
            if (levelCamera)
            {
                var confiner = levelCamera.GetComponent<CinemachineConfiner2D>() ?? levelCamera.gameObject.AddComponent<CinemachineConfiner2D>();
                confiner.m_BoundingShape2D = levelBounds;
                confiner.InvalidateCache();
            }
        }

        public void SetTarget(Transform value)
        {
            target = value;
            player = target ? target.GetComponent<PlayerController>() : null;
            body2D = target ? target.GetComponent<Rigidbody2D>() : null;
            if (levelCamera) levelCamera.Follow = value;
            foreach (var cam in cameras.Values) cam.Follow = value;
        }

        public void SetRoom(CameraRoom2D room, bool inside)
        {
            rooms.Remove(room);
            if (inside)
            {
                if (!cameras.ContainsKey(room)) cameras.Add(room, CreateCamera(room.name, room.Bounds));
                rooms.Add(room);
            }
            foreach (var cam in cameras.Values) cam.Priority = -1;
            if (rooms.Count > 0) cameras[rooms[rooms.Count - 1]].Priority = 1;
        }

        public void Shake(Vector2 direction)
        {
            if (impulse) impulse.GenerateImpulseWithVelocity(direction.normalized * shakeStrength);
        }

        private void Update()
        {
            if (!player) return;
            float y = player.MoveInput.y;
            int vertical = Mathf.Abs(y) > stickThreshold ? (int)Mathf.Sign(y) : 0;
            if (vertical != lastVertical) held = 0f;
            lastVertical = vertical;
            held += Time.deltaTime;
            Vector2 desired = new Vector2(player.Facing * lookAheadDistance, held >= verticalHold ? vertical * verticalDistance : 0f);
            if (body2D && body2D.linearVelocity.y < -fallSpeed) desired.y = Mathf.Min(desired.y, -fallLookDistance);
            look = Vector2.MoveTowards(look, desired, lookAheadSpeed * Time.deltaTime);
            foreach (var body in bodies)
                body.m_TrackedObjectOffset = new Vector3(offset.x + look.x, offset.y + look.y);
        }

        private void OnDestroy()
        {
            if (rig) Destroy(rig.gameObject);
        }
    }
}
