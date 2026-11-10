using UnityEngine;

namespace PrimalEcho
{
    // Objetos que solo sirven para ver algo mientras editas el mapa. Al dar Play desaparecen.
    // "Vista_Centinela_Rex" además marca DÓNDE aparece el jefe: muévelo en la escena y el Rex saldrá ahí.
    public sealed class SoloVistaEditor : MonoBehaviour
    {
        public static Vector3? RexSpot { get; private set; }
        public static string RexScene { get; private set; }

        private void Awake()
        {
            if (name.Contains("Rex")) { RexSpot = transform.position; RexScene = gameObject.scene.name; }
            Destroy(gameObject);
        }
    }
}
