using UnityEngine.SceneManagement;

/// <summary>
/// Shared hub vs level classification for pause/menu flow. Keeps PauseFlow independent of BootstrapRuntime compilation order.
/// </summary>
public static class HubSceneUtility
{
    /// <summary>
    /// Hub is identified by scene <b>name</b> (MainMenu), not by build index — the first scene in Build Settings is not
    /// always the menu (and a level can legally be at build index 0 in student/minimal builds).
    /// </summary>
    public static bool IsMainHubScene(Scene scene)
    {
        if (!scene.IsValid())
            return false;

        string n = string.IsNullOrEmpty(scene.name) ? string.Empty : scene.name.Trim();
        if (n.Length == 0)
            return false;

        string compact = n.Replace(" ", string.Empty);

        // Authoritative gameplay scenes: never show the hub overlay on these.
        if (compact.Length >= 6 &&
            compact.StartsWith("Level", System.StringComparison.OrdinalIgnoreCase))
            return false;

        // Hub is only the menu scene (exact name / compact), not "first build index" or loose substring matches.
        return compact.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase);
    }
}
