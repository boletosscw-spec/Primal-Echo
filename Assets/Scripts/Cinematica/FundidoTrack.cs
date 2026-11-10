using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrimalEcho
{
    // Pista de fundido a negro: controla el alpha de un CanvasGroup (una imagen negra a pantalla completa).
    [TrackColor(0.1f, 0.1f, 0.1f)]
    [TrackClipType(typeof(FundidoClip))]
    [TrackBindingType(typeof(CanvasGroup))]
    public sealed class FundidoTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<FundidoMixer>.Create(graph, inputCount);
        }
    }

    public sealed class FundidoMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var group = playerData as CanvasGroup;
            if (group == null) return;
            float total = 0f;
            int count = playable.GetInputCount();
            for (int i = 0; i < count; i++) total += playable.GetInputWeight(i);
            group.alpha = Mathf.Clamp01(total);
        }
    }
}
