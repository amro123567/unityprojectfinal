using System.Collections;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Attach to a persistent GameObject or any UI button handler
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    [Header("Optional Loading Screen")]
    public GameObject loadingScreen;    // assign a Loading UI panel (can be null)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // Call from buttons: SceneLoader.Instance.LoadScene("mainscene")
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadAsync(sceneName));
    }

    public void LoadScene(int sceneIndex)
    {
        StartCoroutine(LoadAsync(sceneIndex));
    }

    // Reload current scene (useful for "Retry")
    public void ReloadCurrentScene()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        LoadScene(0);   // index 0 = main menu (first in Build Settings)
    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }

    private IEnumerator LoadAsync(string sceneName)
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            Debug.Log("Loading: " + (progress * 100f) + "%");
            yield return null;
        }

        Time.timeScale = 1f;

        if (loadingScreen != null) loadingScreen.SetActive(false);

        string cn = string.IsNullOrEmpty(sceneName)
            ? string.Empty
            : sceneName.Trim().Replace(" ", string.Empty);

        bool wantGameplayHud = !cn.Equals("MainMenu", StringComparison.OrdinalIgnoreCase);
        MandatoryCourseUi.SyncUiNow(wantGameplayHud);
    }

    private IEnumerator LoadAsync(int sceneIndex)
    {
        if (loadingScreen != null) loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);

        while (!operation.isDone)
        {
            yield return null;
        }

        Time.timeScale = 1f;

        if (loadingScreen != null) loadingScreen.SetActive(false);

        MandatoryCourseUi.SyncUiNow(sceneIndex != 0);
    }
}
