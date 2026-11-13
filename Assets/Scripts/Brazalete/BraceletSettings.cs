using System;
using UnityEngine;

namespace PrimalEcho
{
    public enum BraceletUpgradeEffect { Health, SpearDamage, ArrowDamage }

    [Serializable]
    public sealed class BraceletUpgrade
    {
        public string id;
        public string title;
        [TextArea] public string description;
        [Min(0)] public int cost;
        [Tooltip("Núcleos de centinela que cuesta además de los fragmentos.")]
        [Min(0)] public int coreCost;
        [Min(1)] public int amount = 1;
        public BraceletUpgradeEffect effect;
        [Tooltip("-1 = sin requisito. Otro valor es el índice de una mejora previa.")]
        public int prerequisite = -1;
        public Vector2 position;
        public Texture2D icon;
    }

    [Serializable]
    public sealed class BraceletItem
    {
        public string title;
        [TextArea] public string description;
        public Texture2D icon;
    }

    [CreateAssetMenu(menuName = "Primal Echo/Brazalete/Configuración")]
    public sealed class BraceletSettings : ScriptableObject
    {
        [Header("Animación y proyección")]
        public Sprite[] frames = Array.Empty<Sprite>();
        [Min(1f)] public float framesPerSecond = 12f;
        [Min(.01f)] public float projectionSeconds = .25f;
        public Vector2 wrist = new Vector2(.24f, .76f);
        [Range(0f, 1f)] public float backgroundDim = .5f;
        [Range(.2f, 1f)] public float zoom = .78f;
        [Min(.01f)] public float cameraSeconds = .3f;
        [Tooltip("Altura visible mínima en múltiplos de los límites de Kael. Evita cortar al personaje.")]
        [Min(1f)] public float minimumVisiblePlayerHeights = 2.2f;

        [Header("Holograma")]
        public Color cyan = new Color(.25f, .92f, 1f);
        public Color orange = new Color(1f, .52f, .18f);
        public Color locked = new Color(.4f, .46f, .5f);
        public Color panel = new Color(.015f, .12f, .17f, .78f);
        public Vector2Int referenceResolution = new Vector2Int(960, 540);
        public Rect panelRect = new Rect(370, 78, 550, 380);
        [Min(1)] public int corner = 8;
        [Min(1)] public int scanlineSpacing = 4;
        [Min(0f)] public float scanlineSpeed = 5f;
        [Range(0f, .2f)] public float flicker = .025f;
        [Min(.01f)] public float cursorSeconds = .16f;
        [Min(0f)] public float cursorBounce = 4f;

        [Header("Navegación")]
        [Range(.1f, 1f)] public float deadZone = .5f;
        [Min(.05f)] public float repeatDelay = .3f;
        [Min(.05f)] public float repeatInterval = .12f;
        public BraceletUpgrade[] upgrades = Array.Empty<BraceletUpgrade>();
        [Tooltip("Objetos adicionales. La llave se consulta directamente en LabKey.Has.")]
        public BraceletItem[] items = Array.Empty<BraceletItem>();
    }
}
