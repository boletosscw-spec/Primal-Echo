using UnityEngine;

namespace PrimalEcho
{
    // Animación simple por cuadros para enemigos y objetos.
    public sealed class SpriteFrames : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(0.1f)] private float fps = 6f;
        private float offset;

        public void Configure(SpriteRenderer renderer, Sprite[] sprites, float framesPerSecond)
        {
            target = renderer; frames = sprites; fps = framesPerSecond;
        }

        public float Fps { get => fps; set => fps = Mathf.Max(0.1f, value); }

        private void Awake() => offset = Random.value * 10f;

        private void Update()
        {
            if (target == null || frames == null || frames.Length == 0) return;
            target.sprite = frames[(int)((Time.time + offset) * fps) % frames.Length];
        }
    }
}
