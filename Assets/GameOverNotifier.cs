using UnityEngine;

/// <summary>
/// Shows game-over UI via BootstrapRuntime. Ensures the host exists so death always has a recipient.
/// </summary>
public static class GameOverNotifier
{
    static GameOverNotifier()
    {
        _ = typeof(BootstrapRuntime);
    }

    public static void Raise()
    {
        BootstrapRuntime.EnsureHostExists();

        BootstrapRuntime bootstrap = BootstrapRuntime.Active;
        if (bootstrap == null)
        {
            Debug.LogError(
                "Game over UI cannot show: BootstrapRuntime failed to initialize. " +
                "Ensure Assets/BootstrapRuntime.cs compiled and Assets/GameOverNotifier.cs is unchanged.");

            Time.timeScale = 0f;
            return;
        }

        bootstrap.ShowGameOverScreen();
    }
}
