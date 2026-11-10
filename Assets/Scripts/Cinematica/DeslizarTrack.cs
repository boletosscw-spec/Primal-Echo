using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrimalEcho
{
    // Pista de movimiento simple para objetos de la cinemática (va ligada a un Transform).
    [TrackColor(0.75f, 0.4f, 0.85f)]
    [TrackClipType(typeof(DeslizarClip))]
    [TrackBindingType(typeof(Transform))]
    public sealed class DeslizarTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (TimelineClip clip in GetClips())
            {
                var deslizar = clip.asset as DeslizarClip;
                if (deslizar != null) deslizar.duracion = clip.duration;
            }
            return ScriptPlayable<DeslizarMixer>.Create(graph, inputCount);
        }
    }

    public sealed class DeslizarMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var target = playerData as Transform;
            if (target == null) return;
            int count = playable.GetInputCount();
            for (int i = 0; i < count; i++)
            {
                if (playable.GetInputWeight(i) <= 0f) continue;
                var input = (ScriptPlayable<DeslizarBehaviour>)playable.GetInput(i);
                DeslizarBehaviour b = input.GetBehaviour();
                float t = b.segundos > 0.0001d ? Mathf.Clamp01((float)(input.GetTime() / b.segundos)) : 1f;
                target.localPosition = Vector3.Lerp(b.desde, b.hasta, Mathf.SmoothStep(0f, 1f, t));
                return;
            }
        }
    }
}
