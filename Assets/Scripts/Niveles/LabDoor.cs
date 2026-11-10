using UnityEngine;

namespace PrimalEcho
{
    /// <summary>
    /// Puerta de la base (barrera láser). Cerrada bloquea el paso; si Kael se acerca con la llave, se apaga y abre.
    /// Sin llave muestra un aviso.
    /// </summary>
    public sealed class LabDoor : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer barrier;
        [SerializeField] private Collider2D block;
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] openFrames;
        [SerializeField] private float fps = 12f;
        [SerializeField] private float reach = 3.5f;
        private Transform player;
        private bool opening, open;
        private float clock, nextHint;

        public void Configure(SpriteRenderer sr, Collider2D solid, Sprite[] idle, Sprite[] opened)
        {
            barrier = sr; block = solid; idleFrames = idle; openFrames = opened;
        }

        private void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        private void Update()
        {
            if (open) return;
            clock += Time.deltaTime;
            if (opening)
            {
                int frame = Mathf.FloorToInt(clock * fps);
                if (openFrames == null || frame >= openFrames.Length)
                {
                    open = true;
                    if (barrier != null) barrier.enabled = false;
                    return;
                }
                if (barrier != null) barrier.sprite = openFrames[frame];
                return;
            }
            if (barrier != null && idleFrames != null && idleFrames.Length > 0)
                barrier.sprite = idleFrames[Mathf.FloorToInt(clock * fps) % idleFrames.Length];
            if (player == null) return;
            Vector2 d = player.position - transform.position;
            if (Mathf.Abs(d.x) > reach || Mathf.Abs(d.y) > 4f) return;
            if (LabKey.Has)
            {
                opening = true; clock = 0f;
                if (block != null) block.enabled = false;
                LevelHUD.Notify("Usas la llave: la barrera se apaga", 3f);
            }
            else if (Time.time >= nextHint)
            {
                nextHint = Time.time + 4f;
                LevelHUD.Notify("Barrera cerrada · necesitas la llave de la base (está en el Nivel 1)", 3.5f);
            }
        }
    }
}
