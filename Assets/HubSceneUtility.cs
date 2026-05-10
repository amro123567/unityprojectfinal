using UnityEngine.SceneManagement;

/// <summary>
/// Shared hub vs level classification for pause/menu flow. Keeps PauseFlow independent of BootstrapRuntime compilation order.
/// </summary>
public static class HubSceneUtility
{
    /// <summary>
    /// True for build index 0 and for editor Play on scenes named MainMenu / Main Menu (name contains MainMenu).
    /// </summary>
    public static bool IsMainHubScene(Scene scene)
    {
        string n = string.IsNullOrEmpty(scene.name) ? string.Empty : scene.name.Trim();

        if (scene.buildIndex == 0)
            return true;

        if (scene.buildIndex < 0)
        {
            string compact = n.Replace(" ", string.Empty);
            if (compact.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (n.IndexOf("MainMenu", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }
}
