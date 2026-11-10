using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

namespace PrimalEcho
{
    // Pista de narración: escribe el texto del clip activo en un Text de la UI y lo funde con el ease del clip.
    [TrackColor(0.95f, 0.82f, 0.35f)]
    [TrackClipType(typeof(SubtituloClip))]
    [TrackBindingType(typeof(Text))]
    public sealed class SubtituloTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<SubtituloMixer>.Create(graph, inputCount);
        }
    }

    public sealed class SubtituloMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var label = playerData as Text;
            if (label == null) return;
            float best = 0f;
            string texto = null;
            int count = playable.GetInputCount();
            for (int i = 0; i < count; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= best) continue;
                best = weight;
                texto = ((ScriptPlayable<SubtituloBehaviour>)playable.GetInput(i)).GetBehaviour().texto;
            }
            if (texto != null && label.text != texto) label.text = texto;
            Color c = label.color;
            c.a = Mathf.Clamp01(best);
            label.color = c;
        }
    }
}
