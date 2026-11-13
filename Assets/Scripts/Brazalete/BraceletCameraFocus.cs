using Cinemachine;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PrimalEcho
{
    // Suspende el seguimiento durante la pausa y restaura exactamente la cámara capturada.
    internal sealed class BraceletCameraFocus
    {
        private Camera camera;
        private CameraFollow2D follow;
        private CinemachineBrain brain;
        private PixelPerfectCamera pixels;
        private bool followed, driven, snapped;
        private Vector3 position, destination;
        private Quaternion rotation;
        private float size, targetSize;

        public void Capture(Camera value, Bounds player, float zoom, float minimumPlayerHeights)
        {
            camera = value;
            if (!camera) return;
            position = camera.transform.position;
            rotation = camera.transform.rotation;
            size = camera.orthographicSize;
            targetSize = Mathf.Max(size * zoom, player.extents.y * minimumPlayerHeights);
            destination = new Vector3(player.center.x + targetSize * camera.aspect * .42f,
                player.center.y + targetSize * .1f, position.z);
            follow = camera.GetComponent<CameraFollow2D>();
            brain = camera.GetComponent<CinemachineBrain>();
            pixels = camera.GetComponent<PixelPerfectCamera>();
            followed = follow && follow.enabled;
            driven = brain && brain.enabled;
            snapped = pixels && pixels.enabled;
            if (followed) follow.enabled = false;
            if (driven) brain.enabled = false;
            if (snapped) pixels.enabled = false;
            ParallaxFreeze.Active = true;
        }

        public void Evaluate(float amount)
        {
            if (!camera) return;
            float t = Mathf.SmoothStep(0f, 1f, amount);
            camera.transform.position = Vector3.Lerp(position, destination, t);
            camera.orthographicSize = Mathf.Lerp(size, targetSize, t);
        }

        public void Restore()
        {
            ParallaxFreeze.Active = false;
            if (!camera) return;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.orthographicSize = size;
            if (follow) follow.enabled = followed;
            if (brain) brain.enabled = driven;
            if (pixels) pixels.enabled = snapped;
            camera = null;
        }
    }
}
