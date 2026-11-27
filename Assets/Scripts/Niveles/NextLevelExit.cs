using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrimalEcho
{
    // Va junto a LevelGoal: tras la animación de victoria carga el siguiente nivel.
    [RequireComponent(typeof(Collider2D))]
    public sealed class NextLevelExit : MonoBehaviour
    {
        [SerializeField] private string nextScene = "";
        [SerializeField, Min(0f)] private float delay = 3.5f;
        [SerializeField] private bool requiresLabKey;
        private bool done;

        public bool CanExit => !requiresLabKey || LabKey.Has;

        public void Configure(string scene, bool requireKey = false)
        {
            nextScene = scene;
            requiresLabKey = requireKey;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (done || other.GetComponentInParent<PlayerController>() == null) return;
            if (!CanExit)
            {
                LevelHUD.Notify("Necesitas la llave de la base para seguir.", 4f);
                return;
            }
            done = true;
            LevelHUD.StopTimer();
            SincronizacionPartida.Guardar(nextScene);
            if (!string.IsNullOrEmpty(nextScene)) StartCoroutine(Load());
        }

        private IEnumerator Load()
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(nextScene);
        }
    }
}
