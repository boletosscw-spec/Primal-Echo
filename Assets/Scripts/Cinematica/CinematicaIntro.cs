using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // Va junto al PlayableDirector de la cinemática: al terminar el Timeline (o al saltarla) carga el nivel.
    [RequireComponent(typeof(PlayableDirector))]
    public sealed class CinematicaIntro : MonoBehaviour
    {
        [SerializeField] private string siguienteEscena = "Nivel_00_Tutorial";
        [SerializeField] private bool sePuedeSaltar = true;
        private PlayableDirector director;
        private bool cargando;

        public void Configure(string escena) => siguienteEscena = escena;

        private void Awake() => director = GetComponent<PlayableDirector>();

        private void Update()
        {
            if (cargando) return;
            bool termino = director.playableAsset == null || director.time >= director.duration - 0.05d;
            bool saltar = sePuedeSaltar && Time.timeSinceLevelLoad > 0.5f && SaltarPulsado();
            if (!termino && !saltar) return;
            cargando = true;
            if (string.IsNullOrEmpty(siguienteEscena)) return;
            // Si el tutorial aún no se ha generado, se pasa directo al Nivel 1 en lugar de fallar.
            bool existe = Application.CanStreamedLevelBeLoaded(siguienteEscena);
            SceneManager.LoadScene(existe ? siguienteEscena : "Nivel_01_Valle_del_Eco");
        }

        private static bool SaltarPulsado()
        {
            Keyboard k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame))
                return true;
            Gamepad g = Gamepad.current;
            return g != null && (g.buttonSouth.wasPressedThisFrame || g.startButton.wasPressedThisFrame);
        }
    }
}
