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
        private float phase, blockedUntil;
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
            if (taken || Time.time < blockedUntil) return;
            var player = other.GetComponentInParent<PlayerController>();
            if (player == null || !player.TryGetComponent(out BraceletProgress progress)) return;
            switch (kind)
            {
                case ItemKind.Fruit:
                    if (!progress.AddFruit())
                    {
                        blockedUntil = Time.time + 3f;
                        LevelHUD.Notify("Ya llevas " + BraceletProgress.MaxFruits + " frutas. Come una desde el brazalete");
                        return;
                    }
                    LevelHUD.Notify("Fruta de la selva (" + progress.Fruits + "/" + BraceletProgress.MaxFruits + ") · comela desde el brazalete");
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
            taken = true;
            Destroy(gameObject);
        }
    }

    // Los centinelas pesados sueltan un núcleo al morir; el Rex suelta tres.
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
                : enemy.TryGetComponent(out CentinelaTerrestre walker) && walker.Heavy ? 1 : 0;
            Vector3 origin = enemy.transform.position + Vector3.up;
            for (int i = 0; i < cores; i++)
                ItemPickup.Spawn(ItemKind.Core, origin + Vector3.right * (i - (cores - 1) * .5f) * 1.2f);
        }
    }
}
