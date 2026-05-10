using UnityEngine;

/// <summary>
/// Invokes game-over UI without a compile-time dependency from player scripts → BootstrapRuntime.
/// </summary>
public static class GameOverNotifier
{
    public static void Raise()
    {
        GameObject host = GameObject.Find("BootstrapRuntime");
        if (host == null)
        {
            Debug.LogError(
                "Game over UI cannot show: GameObject named 'BootstrapRuntime' was not found. " +
                "Ensure Assets/BootstrapRuntime.cs is present and you are using Play from a level scene.");

            Time.timeScale = 0f;
            return;
        }

        host.SendMessage("ShowGameOverScreen", SendMessageOptions.RequireReceiver);
    }
}
