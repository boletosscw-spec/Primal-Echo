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
        private bool done;

        public void Configure(string scene) => nextScene = scene;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (done || other.GetComponentInParent<PlayerController>() == null) return;
            done = true;
            LevelHUD.StopTimer();
            if (!string.IsNullOrEmpty(nextScene)) StartCoroutine(Load());
        }

        private IEnumerator Load()
        {
            yield return new WaitForSeconds(delay);
            SceneManager.LoadScene(nextScene);
        }
    }
}
