using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PrimalEcho
{
    // Cuando la ventana no tiene la misma proporción que la cámara del juego quedan franjas
    // arriba/abajo (o a los lados) que ninguna cámara limpia, y ahí se quedaban "pegados"
    // textos viejos del HUD, subtítulos de la cinemática, etc. Esta cámara vacía se dibuja
    // antes que todas y pinta la pantalla completa de negro en cada frame.
    public sealed class LimpiadorPantalla : MonoBehaviour
    {
        private static LimpiadorPantalla instancia;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Crear()
        {
            if (instancia != null) return;
            var go = new GameObject("LimpiadorPantalla");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<LimpiadorPantalla>();

            var cam = go.AddComponent<Camera>();
            cam.depth = -100f;
            cam.cullingMask = 0;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.rect = new Rect(0f, 0f, 1f, 1f);
            cam.orthographic = true;
            cam.useOcclusionCulling = false;
            cam.allowHDR = false;
            cam.allowMSAA = false;

            var datos = cam.GetUniversalAdditionalCameraData();
            if (datos != null)
            {
                datos.renderType = CameraRenderType.Base;
                datos.renderShadows = false;
                datos.renderPostProcessing = false;
                datos.requiresColorOption = CameraOverrideOption.Off;
                datos.requiresDepthOption = CameraOverrideOption.Off;
            }
        }

        private void OnDestroy() { if (instancia == this) instancia = null; }
    }
}
