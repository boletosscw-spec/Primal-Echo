// Mientras está activo, las capas de fondo dejan de recolocarse y se comportan como parte del mundo:
// así un zoom o paneo de cámara (menú del brazalete) mueve fondo y suelo juntos, sin que el fondo se deslice.
public static class ParallaxFreeze
{
    public static bool Active;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() => Active = false;
}
