using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrimalEcho
{
    // Mueve un objeto de un punto a otro (posición local) y lo deja en el punto final hasta que acaba el clip.
    [Serializable]
    public sealed class DeslizarClip : PlayableAsset, ITimelineClipAsset
    {
        public Vector3 desde;
        public Vector3 hasta;
        [Tooltip("Segundos que tarda en llegar. 0 = todo el clip.")]
        [Min(0f)] public float segundos;

        [NonSerialized] public double duracion = 1d; // la rellena DeslizarTrack

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<DeslizarBehaviour>.Create(graph);
            DeslizarBehaviour b = playable.GetBehaviour();
            b.desde = desde;
            b.hasta = hasta;
            b.segundos = segundos > 0f ? segundos : duracion;
            return playable;
        }
    }

    public sealed class DeslizarBehaviour : PlayableBehaviour
    {
        public Vector3 desde, hasta;
        public double segundos = 1d;
    }
}
