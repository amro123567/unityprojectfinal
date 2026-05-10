using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maps the active scene to the level slot used in SaveSystem (build index when valid; otherwise parses scene name).
/// </summary>
public static class SceneLevelSlot
{
    public static int Active()
    {
        return ForScene(SceneManager.GetActiveScene());
    }

    public static int ForScene(Scene scene)
    {
        if (!scene.IsValid())
            return 1;

        int b = scene.buildIndex;
        if (b >= 0)
            return b;

        string name = scene.name.Trim();
        string compact = name.Replace(" ", string.Empty);

        if (compact.Equals("MainMenu", System.StringComparison.OrdinalIgnoreCase))
            return 0;

        if (name.StartsWith("Level", System.StringComparison.OrdinalIgnoreCase))
        {
            string tail = name.Substring("Level".Length).Trim();
            if (int.TryParse(tail, out int n))
                return n;
        }

        return Mathf.Max(1, b);
    }
}
