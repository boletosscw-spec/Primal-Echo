using UnityEngine;

namespace PrimalEcho
{
    public enum ItemKind { Fruit, Core, Chip }

    // Objeto recogible: fruta de la selva, núcleo de centinela o chip de memoria.
    // Dibuja su propio sprite a partir del icono de Assets/Resources/HUD, así no necesita arte en la escena.
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemKind kind;
        [SerializeField] private string chipId = "";
        [SerializeField] private string chipTitle = "";
        [SerializeField, TextArea] private string chipText = "";
        private Transform visual;
        private float phase;
        private bool taken;

        public void Configure(ItemKind value, string id = "", string title = "", string text = "")
        {
            kind = value; chipId = id; chipTitle = title; chipText = text;
        }

        public static string IconName(ItemKind kind) =>
            kind == ItemKind.Fruit ? "HUD/icono_fruta" : kind == ItemKind.Core ? "HUD/icono_nucleo" : "HUD/icono_chip";

        /// <summary>Crea el objeto en el mundo (lo usan los enemigos al soltar núcleos).</summary>
        public static ItemPickup Spawn(ItemKind kind, Vector3 position)
        {
            var go = new GameObject(kind == ItemKind.Core ? "Nucleo_Centinela" : kind == ItemKind.Fruit ? "Fruta_Selva" : "Chip_Memoria");
            go.transform.position = position;
            var item = go.AddComponent<ItemPickup>();
            item.kind = kind;
            return item;
        }

        private void Awake()
        {
            if (!TryGetComponent(out Collider2D trigger))
            {
                var circle = gameObject.AddComponent<CircleCollider2D>();
                circle.radius = .6f;
                trigger = circle;
            }
            trigger.isTrigger = true;
            phase = transform.position.x;
        }

        private void Start()
        {
            var player = FindFirstObjectByType<PlayerController>();
            var progress = player ? player.GetComponent<BraceletProgress>() : null;
            // Un chip ya leído no vuelve a aparecer.
            if (kind == ItemKind.Chip && progress != null && progress.HasChip(chipId)) { Destroy(gameObject); return; }

            var texture = Resources.Load<Texture2D>(IconName(kind));
            if (texture == null) return;
            texture.filterMode = FilterMode.Point;
            var go = new GameObject("Visual");
            go.transform.SetParent(transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 16f);
            var reference = player ? player.GetComponentInChildren<SpriteRenderer>() : null;
            if (reference != null)
            {
                renderer.sortingLayerID = reference.sortingLayerID;
                renderer.sortingOrder = reference.sortingOrder - 1;
            }
            else renderer.sortingOrder = 6;
            visual = go.transform;
        }

        private void Update()
        {
            if (visual != null) visual.localPosition = Vector3.up * Mathf.Sin(Time.time * 2.4f + phase) * .12f;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryTake(other);
        private void OnTriggerStay2D(Collider2D other) => TryTake(other);

        private void TryTake(Collider2D other)
        {
            if (taken) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null) return;
            // Si por alguna razón Kael no tiene el progreso del brazalete, se lo ponemos en vez de ignorar el objeto.
            if (!player.TryGetComponent(out BraceletProgress progress)) progress = player.gameObject.AddComponent<BraceletProgress>();

            // Primero se marca como recogido y se oculta, para que desaparezca pase lo que pase después.
            taken = true;
            foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
            if (visual != null) visual.gameObject.SetActive(false);

            try
            {
                switch (kind)
                {
                    case ItemKind.Fruit:
                        if (progress.AddFruit())
                            LevelHUD.Notify("Fruta de la selva (" + progress.Fruits + "/" + BraceletProgress.MaxFruits + ") · comela desde el brazalete");
                        else if (player.TryGetComponent(out KaelHealth health) && health.Heal(1))
                            LevelHUD.Notify("Bolsa de frutas llena · Kael se come esta (+1 corazon)");
                        else
                            LevelHUD.Notify("Bolsa de frutas llena (" + BraceletProgress.MaxFruits + "/" + BraceletProgress.MaxFruits + ")");
                        break;
                    case ItemKind.Core:
                        progress.AddCore();
                        LevelHUD.Notify("Nucleo de centinela obtenido · sirve para las mejoras de nivel II", 4f);
                        break;
                    default:
                        progress.AddChip(chipId, chipTitle, chipText);
                        LevelHUD.Notify("Chip de memoria: " + chipTitle + " · leelo en el inventario", 4f);
                        break;
                }
                Sonidos.Play(kind == ItemKind.Core ? "llave_recoger" : "objeto_recoger", 0.6f, kind == ItemKind.Fruit ? 1.1f : 1f, transform.position, 0.02f);
            }
            finally
            {
                Destroy(gameObject);
            }
        }
    }

    // Núcleos de centinela: cada JEFE suelta uno al caer (Atlas en el Nivel 1, Aeris en el Nivel 2) y el Rex tres.
    // Los centinelas pesados (si se usan) sueltan uno. Así LANZA II y FLECHAS II siempre se pueden comprar.
    internal static class ItemDrops
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
            EnemyHealth.Died -= Drop;
            EnemyHealth.Died += Drop;
        }

        private static void Drop(EnemyHealth enemy)
        {
            int cores = enemy.GetComponent<CentinelaRex>() ? 3
                : enemy.GetComponent<Atlas>() || enemy.GetComponent<Aeris>() ? 1
                : enemy.TryGetComponent(out CentinelaTerrestre walker) && walker.Heavy ? 1 : 0;
            if (cores == 0) return;
            // Se deja en el suelo debajo del enemigo (Aeris muere volando: el núcleo no debe quedar en el aire)
            Vector2 origin = enemy.transform.position + Vector3.up;
            var hit = Physics2D.Raycast(origin, Vector2.down, 40f, 1 << 6);
            Vector3 floor = hit.collider != null ? (Vector3)hit.point + Vector3.up * 0.9f : (Vector3)origin;
            for (int i = 0; i < cores; i++)
                ItemPickup.Spawn(ItemKind.Core, floor + Vector3.right * (i - (cores - 1) * .5f) * 1.2f);
        }
    }
}
