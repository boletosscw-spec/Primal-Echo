using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PrimalEcho
{
    // Un plano de la cinemática: muestra una imagen mientras dura el clip (el ease es el fundido)
    // y la mueve despacio de un encuadre a otro (efecto de paneo / zoom).
    [Serializable]
    public sealed class PlanoClip : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("Activado: la imagen se escala para cubrir la cámara y se aplican zoom y desplazamiento. " +
                 "Desactivado: solo aparece y desaparece (para personajes).")]
        public bool ajustarAPantalla = true;
        [Tooltip("1 = cubre la pantalla justo. Más de 1 = acercamiento.")]
        [Min(0.1f)] public float zoomInicio = 1.05f;
        [Min(0.1f)] public float zoomFin = 1.15f;
        [Tooltip("Dónde queda el centro de la imagen respecto a la cámara, en fracción de pantalla (0.1 = 10 %).")]
        public Vector2 desplazamientoInicio = Vector2.zero;
        public Vector2 desplazamientoFin = Vector2.zero;

        [NonSerialized] public double duracion = 1d; // la rellena PlanoTrack

        public ClipCaps clipCaps => ClipCaps.Blending;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<PlanoBehaviour>.Create(graph);
            PlanoBehaviour b = playable.GetBehaviour();
            b.ajustarAPantalla = ajustarAPantalla;
            b.zoomInicio = zoomInicio;
            b.zoomFin = zoomFin;
            b.desplazamientoInicio = desplazamientoInicio;
            b.desplazamientoFin = desplazamientoFin;
            b.duracion = duracion;
            return playable;
        }
    }

    public sealed class PlanoBehaviour : PlayableBehaviour
    {
        public bool ajustarAPantalla;
        public float zoomInicio, zoomFin;
        public Vector2 desplazamientoInicio, desplazamientoFin;
        public double duracion = 1d;
    }
}
