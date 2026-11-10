using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrimalEcho
{
    // Pista de planos: va ligada a un SpriteRenderer (un fondo o un personaje de la cinemática).
    [TrackColor(0.25f, 0.7f, 0.55f)]
    [TrackClipType(typeof(PlanoClip))]
    [TrackBindingType(typeof(SpriteRenderer))]
    public sealed class PlanoTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (TimelineClip clip in GetClips())
            {
                var plano = clip.asset as PlanoClip;
                if (plano != null) plano.duracion = clip.duration;
            }
            return ScriptPlayable<PlanoMixer>.Create(graph, inputCount);
        }
    }

    public sealed class PlanoMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var renderer = playerData as SpriteRenderer;
            if (renderer == null) return;

            float total = 0f, best = 0f, t = 0f;
            PlanoBehaviour plano = null;
            int count = playable.GetInputCount();
            for (int i = 0; i < count; i++)
            {
                float weight = playable.GetInputWeight(i);
                total += weight;
                if (weight <= best) continue;
                best = weight;
                var input = (ScriptPlayable<PlanoBehaviour>)playable.GetInput(i);
                plano = input.GetBehaviour();
                t = plano.duracion > 0.0001d ? Mathf.Clamp01((float)(input.GetTime() / plano.duracion)) : 0f;
            }

            total = Mathf.Clamp01(total);
            renderer.enabled = total > 0.001f;
            Color c = renderer.color;
            c.a = total;
            renderer.color = c;

            if (plano == null || !plano.ajustarAPantalla || renderer.sprite == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            float viewH = cam.orthographicSize * 2f, viewW = viewH * cam.aspect;
            Bounds bounds = renderer.sprite.bounds;
            float scale = Mathf.Max(viewW / bounds.size.x, viewH / bounds.size.y) * Mathf.Lerp(plano.zoomInicio, plano.zoomFin, t);
            Vector2 offset = Vector2.Lerp(plano.desplazamientoInicio, plano.desplazamientoFin, t);
            Vector3 camPos = cam.transform.position;
            Transform tr = renderer.transform;
            tr.localScale = new Vector3(scale, scale, 1f);
            // se compensa el pivote del sprite para que "desplazamiento 0" sea la imagen centrada
            tr.position = new Vector3(
                camPos.x + offset.x * viewW - bounds.center.x * scale,
                camPos.y + offset.y * viewH - bounds.center.y * scale,
                tr.position.z);
        }
    }
}
