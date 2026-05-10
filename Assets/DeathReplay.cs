using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reloads the active level without game-over overlays (death path requested: instant replay).
/// </summary>
public static class DeathReplay
{
    public static void RestartCurrentScene()
    {
        Time.timeScale = 1f;
        ProfessorFallbackUi.Clear();

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
