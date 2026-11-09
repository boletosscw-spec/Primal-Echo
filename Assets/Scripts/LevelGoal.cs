using UnityEngine;

namespace PrimalEcho
{
    public class LevelGoal : MonoBehaviour
    {
        [SerializeField] private string levelTitle = "NIVEL 1 COMPLETADO";
        [SerializeField] private string levelSubtitle = "El Bosque de las Ruinas explorado con éxito";
        
        private bool isCompleted = false;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.GetComponent<PlayerController>() != null || collision.CompareTag("Player") || collision.name == "Player")
            {
                isCompleted = true;
                var pc = collision.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.PlayVictory();
                }
            }
        }

        private void OnGUI()
        {
            if (!isCompleted) return;

            int w = Screen.width;
            int h = Screen.height;

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 22;
            boxStyle.normal.textColor = Color.white;
            boxStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle subStyle = new GUIStyle(GUI.skin.label);
            subStyle.fontSize = 15;
            subStyle.normal.textColor = new Color(0.8f, 1f, 0.9f);
            subStyle.alignment = TextAnchor.MiddleCenter;

            Rect rect = new Rect((w - 500) / 2, 80, 500, 110);
            GUI.Box(rect, GUIContent.none);

            GUI.Label(new Rect(rect.x, rect.y + 15, rect.width, 35), "★ " + levelTitle + " ★", boxStyle);
            GUI.Label(new Rect(rect.x, rect.y + 55, rect.width, 30), levelSubtitle, subStyle);
        }
    }
}
