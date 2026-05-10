using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reloads the active level like a cold start: enemies and scene reset, and persisted mid-level
/// position/HP must not hydrate until the player uses Continue from the hub.
/// </summary>
public static class DeathReplay
{
    static bool _skipNextHydrate;

    /// <summary>Consume once in HeroKnight before applying save-derived transform/HP.</summary>
    internal static bool ConsumeSkipHydrateIfSet()
    {
        if (!_skipNextHydrate)
            return false;

        _skipNextHydrate = false;
        return true;
    }

    public static void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        PauseFlow.PauseMenuEnabled = true;
        ProfessorFallbackUi.Clear();

        _skipNextHydrate = true;

        Scene scene = SceneManager.GetActiveScene();
        int idx = scene.buildIndex;

        if (idx >= 0 && idx < SceneManager.sceneCountInBuildSettings)
            SceneManager.LoadScene(idx, LoadSceneMode.Single);
        else if (!string.IsNullOrEmpty(scene.name))
            SceneManager.LoadScene(scene.name, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(0, LoadSceneMode.Single);
    }
}
