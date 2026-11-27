using UnityEngine;

namespace PrimalEcho
{
    // Lista de temas del juego. El asset está en Assets/Resources/PistasMusica.asset;
    // para cambiar una canción, arrastra otro audio al campo correspondiente.
    [CreateAssetMenu(fileName = "PistasMusica", menuName = "Primal Echo/Pistas de música")]
    public sealed class PistasMusica : ScriptableObject
    {
        public AudioClip menu;
        public AudioClip exploracion;
        public AudioClip combate;
        public AudioClip jefe;
        public AudioClip nemesis;
        [Tooltip("Suena en la pantalla de GAME OVER.")] public AudioClip gameOver;
        [Range(0f, 1f)] public float volumen = 0.6f;
    }
}
