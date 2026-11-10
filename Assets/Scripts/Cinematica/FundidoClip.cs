using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrimalEcho
{
    // Mientras dura el clip la pantalla está en negro; el ease de entrada y salida del clip es el fundido.
    [Serializable]
    public sealed class FundidoClip : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            return ScriptPlayable<FundidoBehaviour>.Create(graph);
        }
    }

    public sealed class FundidoBehaviour : PlayableBehaviour { }
}
