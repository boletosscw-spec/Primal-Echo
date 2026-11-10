using UnityEngine;

namespace PrimalEcho
{
    /// <summary>La llave de la base tecnológica. Se guarda entre niveles (y entre sesiones).</summary>
    public static class LabKey
    {
        private const string Pref = "PrimalEcho_LlaveBase";
        public static bool Has
        {
            get => PlayerPrefs.GetInt(Pref, 0) == 1;
            set { PlayerPrefs.SetInt(Pref, value ? 1 : 0); PlayerPrefs.Save(); }
        }
    }

    /// <summary>Tarjeta-llave del Nivel 1: al tocarla, Kael la guarda y abre la puerta de la base del Nivel 2.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class KeyPickup : MonoBehaviour
    {
        [SerializeField] private Transform visual;
        private Vector3 basePos;
        private bool taken;

        public void Configure(Transform v) => visual = v;

        private void Start()
        {
            LabKey.Has = false; // empezar el nivel de la llave = partida nueva: hay que recogerla
            if (visual != null) basePos = visual.localPosition;
        }

        private void Update()
        {
            if (visual == null) return;
            visual.localPosition = basePos + Vector3.up * (Mathf.Sin(Time.time * 2.5f) * 0.15f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.7f) * 8f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (taken || other.GetComponentInParent<PlayerController>() == null) return;
            taken = true;
            LabKey.Has = true;
            LevelHUD.Notify("¡Llave de la base obtenida! Abre la puerta del Templo de Némesis", 5f);
            Destroy(gameObject);
        }
    }
}
