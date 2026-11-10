using UnityEngine;

namespace PrimalEcho
{
    // Objetos que solo sirven para ver algo mientras editas el mapa (por ejemplo, dónde aparece el jefe).
    // Al dar Play desaparecen.
    public sealed class SoloVistaEditor : MonoBehaviour
    {
        private void Awake() => Destroy(gameObject);
    }
}
