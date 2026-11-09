using UnityEngine;

namespace PrimalEcho
{
    // Emboscada: al entrar se cierran las compuertas y aparecen los enemigos. Al vencerlos se abren.
    [RequireComponent(typeof(Collider2D))]
    public sealed class ArenaLock : MonoBehaviour
    {
        [SerializeField] private GameObject[] gates;
        [SerializeField] private GameObject[] enemies;
        [SerializeField] private float dropHeight = 3f;
        private Vector3[] starts;
        private KaelRespawn player;
        private bool active, cleared;

        public void Configure(GameObject[] arenaGates, GameObject[] arenaEnemies) { gates = arenaGates; enemies = arenaEnemies; }

        private void Awake()
        {
            starts = new Vector3[enemies.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null) continue;
                starts[i] = enemies[i].transform.position;
                enemies[i].SetActive(false);
            }
            SetGates(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (active || cleared) return;
            var respawn = other.GetComponentInParent<KaelRespawn>();
            if (respawn == null) return;
            player = respawn;
            player.Respawned += ResetArena;
            active = true;
            SetGates(true);
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null) continue;
                enemies[i].transform.position = starts[i] + Vector3.up * dropHeight;
                enemies[i].SetActive(true);
            }
        }

        private void Update()
        {
            if (!active) return;
            foreach (var e in enemies) if (e != null) return;
            active = false; cleared = true;
            SetGates(false);
            if (player != null) player.Respawned -= ResetArena;
        }

        private void ResetArena()
        {
            if (!active) return;
            active = false;
            SetGates(false);
            if (player != null) player.Respawned -= ResetArena;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == null) continue;
                enemies[i].SetActive(false);
                enemies[i].transform.position = starts[i];
            }
        }

        private void SetGates(bool closed)
        {
            foreach (var g in gates) if (g != null) g.SetActive(closed);
        }

        private void OnDestroy() { if (player != null) player.Respawned -= ResetArena; }
    }
}
