using UnityEngine;

namespace PrimalEcho
{
    /// <summary>
    /// Partículas de ambiente de la jungla, pegadas a la cámara: luciérnagas azules (atrás y adelante),
    /// insectos que revolotean y hojas que caen. Se crean al iniciar el nivel.
    /// Los insectos y las hojas no se ven dentro de las cuevas (máscara "Mascara_Cuevas").
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class JungleParticles : MonoBehaviour
    {
        [SerializeField] private Material glow;
        [SerializeField] private Material leaf;
        [SerializeField] private Material insect;
        [Tooltip("Zona alrededor de la cámara donde aparecen (un poco más grande que la pantalla)")]
        [SerializeField] private Vector2 area = new Vector2(26f, 16f);
        [SerializeField] private Color fireflyColor = new Color(0.45f, 0.9f, 1f, 1f);
        [SerializeField, Range(0f, 3f)] private float amount = 1f;

        public void Configure(Material glowMat, Material leafMat, Material insectMat)
        {
            glow = glowMat; leaf = leafMat; insect = insectMat;
        }

        private void Start()
        {
            if (glow != null)
            {
                Fireflies("Luciernagas_Fondo", 8f, 0.07f, 0.15f, 0.55f, -45, 60);
                Fireflies("Luciernagas_Frente", 2.5f, 0.14f, 0.26f, 0.95f, 35, 25); // encima de la oscuridad de cueva: brillan
            }
            if (insect != null) Insects();
            if (leaf != null) Leaves();
        }

        private ParticleSystem Make(string name, Material mat, int order, int max, SpriteMaskInteraction mask, Vector2 offset, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(transform.position.x + offset.x, transform.position.y + offset.y, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.duration = 10f;
            main.prewarm = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // se quedan en el mundo aunque la cámara se mueva
            main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(max * amount));
            main.gravityModifier = 0f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(size.x, size.y, 0.01f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sharedMaterial = mat;
            r.sortingOrder = order;
            r.maskInteraction = mask;
            return ps;
        }

        private void Fireflies(string name, float rate, float minSize, float maxSize, float alpha, int order, int max)
        {
            var ps = Make(name, glow, order, max, SpriteMaskInteraction.None, Vector2.zero, area);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
            main.startColor = new Color(fireflyColor.r, fireflyColor.g, fireflyColor.b, alpha);
            var emission = ps.emission;
            emission.rateOverTime = rate * amount;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.3f;
            noise.scrollSpeed = 0.2f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            // Parpadeo: aparece, baja, sube y se apaga
            var blink = new Gradient();
            blink.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.35f, 0.45f),
                        new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = blink;
            var pulse = new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(0.5f, 0.75f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0.5f));
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, pulse);
            ps.Play();
        }

        private void Insects()
        {
            var ps = Make("Insectos", insect, 4, 14, SpriteMaskInteraction.VisibleOutsideMask, new Vector2(0f, -2f), new Vector2(area.x, area.y * 0.6f));
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.32f);
            main.startColor = new Color(1f, 1f, 1f, 0.9f);
            var emission = ps.emission;
            emission.rateOverTime = 1.5f * amount;
            var noise = ps.noise; // vuelo nervioso
            noise.enabled = true;
            noise.strength = 1.3f;
            noise.frequency = 1.2f;
            noise.scrollSpeed = 0.6f;
            noise.quality = ParticleSystemNoiseQuality.Medium;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.9f), new GradientAlphaKey(0f, 1f) });
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = fade;
            var wings = ps.textureSheetAnimation; // aleteo: 2 cuadros
            wings.enabled = true;
            wings.mode = ParticleSystemAnimationMode.Grid;
            wings.numTilesX = 2;
            wings.numTilesY = 1;
            wings.animation = ParticleSystemAnimationType.WholeSheet;
            wings.cycleCount = 50;
            ps.Play();
        }

        private void Leaves()
        {
            var ps = Make("Hojas", leaf, 6, 30, SpriteMaskInteraction.VisibleOutsideMask, new Vector2(0f, 3f), new Vector2(area.x, area.y));
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 13f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.82f, 0.35f, 1f), new Color(0.88f, 0.68f, 0.3f, 1f));
            var emission = ps.emission;
            emission.rateOverTime = 1.2f * amount;
            var vel = ps.velocityOverLifetime; // caen despacio y el viento las lleva un poco a la izquierda
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.1f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.95f, -0.5f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var noise = ps.noise; // vaivén
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.45f;
            noise.scrollSpeed = 0.3f;
            noise.quality = ParticleSystemNoiseQuality.Low;
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-1.8f, 1.8f);
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            var color = ps.colorOverLifetime;
            color.enabled = true;
            color.color = fade;
            ps.Play();
        }
    }
}
