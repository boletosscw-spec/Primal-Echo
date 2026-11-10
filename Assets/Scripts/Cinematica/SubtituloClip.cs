using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrimalEcho
{
    // Clip de Timeline con una línea de narración. El texto se edita en el Inspector al seleccionar el clip.
    [Serializable]
    public sealed class SubtituloClip : PlayableAsset, ITimelineClipAsset
    {
        [TextArea(2, 4)] public string texto = "";

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<SubtituloBehaviour>.Create(graph);
            playable.GetBehaviour().texto = texto;
            return playable;
        }
    }

    public sealed class SubtituloBehaviour : PlayableBehaviour
    {
        public string texto;
    }
}
