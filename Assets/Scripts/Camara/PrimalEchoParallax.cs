using UnityEngine;

// Add to every Parallax layer. Set Factor based on 00-08 entries in the metadata.
// All layers must share the same initial local/world position and Pixels Per Unit.
[ExecuteAlways]
public class PrimalEchoParallax : MonoBehaviour
{
    [Range(0f, 1.5f)] public float factor = 0.5f;
    public Transform targetCamera;
    private Vector3 originPosition;
    private Vector3 originCameraPosition;

    private void OnEnable()
    {
        if (targetCamera == null && Camera.main != null)
            targetCamera = Camera.main.transform;
        originPosition = transform.position;
        if (targetCamera != null) originCameraPosition = targetCamera.position;
    }
    private void LateUpdate()
    {
        if (targetCamera == null || ParallaxFreeze.Active) return;
        Vector3 delta = targetCamera.position - originCameraPosition;
        transform.position = originPosition + new Vector3(delta.x * factor, delta.y * factor, 0f);
    }
}
