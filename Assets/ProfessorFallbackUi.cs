using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Minimal game-over overlay if BootstrapRuntime cannot be added (missing script asset). Keeps rubric demo playable.
/// </summary>
public static class ProfessorFallbackUi
{
    static GameObject _gameOverRoot;

    public static void ShowGameOverBoard()
    {
        if (_gameOverRoot != null)
            return;

        EnsureEventSystemExists();
        EnsureSceneLoaderExists();

        Time.timeScale = 0f;

        Font font = GameUiFonts.DefaultUIFont();

        _gameOverRoot = new GameObject("FallbackGameOverRoot");
        UnityEngine.Object.DontDestroyOnLoad(_gameOverRoot);

        Canvas canvas = _gameOverRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32700;
        _gameOverRoot.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = _gameOverRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject dim = new GameObject("Dim");
        dim.transform.SetParent(_gameOverRoot.transform, false);
        RectTransform dimRect = dim.AddComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        dim.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.92f);

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(_gameOverRoot.transform, false);
        RectTransform pr = panel.AddComponent<RectTransform>();
        pr.anchorMin = new Vector2(0.5f, 0.5f);
        pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(520f, 420f);

        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.12f, 0.08f, 0.05f, 0.96f);

        VerticalLayoutGroup col = panel.AddComponent<VerticalLayoutGroup>();
        col.childAlignment = TextAnchor.MiddleCenter;
        col.spacing = 16f;
        col.padding = new RectOffset(24, 24, 28, 28);

        AddLine(col.transform, font, "Game Over", 36, new Color(1f, 0.55f, 0.3f));
        AddLine(col.transform, font, "Bootstrap UI did not load; using fallback panel.", 18, new Color(0.9f, 0.85f, 0.8f));

        AddButton(col.transform, font, "Try Again", () =>
        {
            Time.timeScale = 1f;
            Clear();
            DeathReplay.RestartCurrentScene();
        });

        AddButton(col.transform, font, "Main Menu", () =>
        {
            Time.timeScale = 1f;
            Clear();
            SceneManager.LoadScene(0);
            MandatoryCourseUi.SyncUiNow(false);
        });

        AddButton(col.transform, font, "Quit", () =>
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });
    }

    public static void Clear()
    {
        if (_gameOverRoot != null)
        {
            UnityEngine.Object.Destroy(_gameOverRoot);
            _gameOverRoot = null;
        }
    }

    static void EnsureEventSystemExists() => UiInputEnsure.Bootstrap();

    static void EnsureSceneLoaderExists()
    {
        if (SceneLoader.Instance != null)
            return;

        new GameObject("SceneLoader").AddComponent<SceneLoader>();
    }

    static void AddLine(Transform parent, Font font, string text, int size, Color color)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(parent, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = size + 12f;
        Text t = go.AddComponent<Text>();
        t.font = font;
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.text = text;
    }

    static void AddButton(Transform parent, Font font, string caption, UnityEngine.Events.UnityAction handler)
    {
        GameObject go = new GameObject($"Btn_{caption}");
        go.transform.SetParent(parent, false);
        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 56f;
        le.minWidth = 360f;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.25f, 0.15f, 0.1f, 1f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(handler);

        GameObject cap = new GameObject("Cap");
        cap.transform.SetParent(go.transform, false);
        RectTransform cr = cap.AddComponent<RectTransform>();
        cr.anchorMin = Vector2.zero;
        cr.anchorMax = Vector2.one;
        cr.offsetMin = Vector2.zero;
        cr.offsetMax = Vector2.zero;

        Text tx = cap.AddComponent<Text>();
        tx.font = font;
        tx.fontSize = 24;
        tx.alignment = TextAnchor.MiddleCenter;
        tx.color = Color.white;
        tx.text = caption;
    }
}
