using System;
using UnityEngine;

namespace PrimalEcho
{
    // Fragmento de eco: coleccionable opcional.
    [RequireComponent(typeof(Collider2D))]
    public sealed class EchoShard : MonoBehaviour
    {
        public static event Action Collected;
        [SerializeField] private Transform visual;
        private Vector3 rest;
        private float phase;

        public void Configure(Transform v) => visual = v;

        private void Awake()
        {
            if (visual != null) rest = visual.localPosition;
            phase = transform.position.x;
        }

        private void Update()
        {
            if (visual != null) visual.localPosition = rest + Vector3.up * Mathf.Sin(Time.time * 2.4f + phase) * 0.12f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            Collected?.Invoke();
            gameObject.SetActive(false);
        }
    }
}
