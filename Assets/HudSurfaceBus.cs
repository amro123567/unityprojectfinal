using UnityEngine;

public static class HudSurfaceBus
{
    public static Canvas ActiveCanvas;

    internal static void Clear()
    {
        ActiveCanvas = null;
    }
}
