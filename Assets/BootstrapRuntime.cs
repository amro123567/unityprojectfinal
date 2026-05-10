using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Services + scene portal. All menu / pause / HUD is built by MandatoryCourseUi for reliability.
/// </summary>
public sealed class BootstrapRuntime : MonoBehaviour
{
    public static BootstrapRuntime Active { get; private set; }

    Font _gameOverFont;

    GameObject _gameOverOverlay;

    public static void EnsureHostExists()
    {
        if (Active != null)
            return;

        GameObject host = new GameObject("BootstrapRuntime");
        DontDestroyOnLoad(host);
        host.AddComponent<BootstrapRuntime>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateHostBeforeFirstScene() => EnsureHostExists();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateHostAfterFirstScene() => EnsureHostExists();

    void Awake()
    {
        if (Active != null && Active != this)
        {
            Destroy(gameObject);
            return;
        }

        Active = this;

        SceneManager.sceneLoaded += OnSceneLoaded;
        PrepareGlobalServices();
        MandatoryCourseUi.Ensure();

        Scene boot = SceneManager.GetActiveScene();
        if (boot.IsValid() && boot.isLoaded)
            DispatchSceneBoot(boot);
        else
            StartCoroutine(CoDeferredDispatch());
    }

    IEnumerator CoDeferredDispatch()
    {
        yield return null;
        Scene s = SceneManager.GetActiveScene();
        if (s.IsValid() && s.isLoaded)
            DispatchSceneBoot(s);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Active == this)
            Active = null;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) =>
        DispatchSceneBoot(scene);

    void DispatchSceneBoot(Scene scene)
    {
        try
        {
            DispatchSceneBootCore(scene);
        }
        catch (Exception ex)
        {
            Debug.LogError(
                $"BootstrapRuntime scene boot failed for '{scene.name}': {ex.Message}\n{ex.StackTrace}");
        }
    }

    void DispatchSceneBootCore(Scene scene)
    {
        EnsureEventSystemForUi();
        ProfessorFallbackUi.Clear();

        PauseFlow.PauseMenuEnabled = true;

        ClearGameOverUi();
        OptionsHost.DropSurface();

        MandatoryCourseUi.Ensure();

        MandatoryCourseUi.Instance?.RebuildForScene(scene);

        if (!HubSceneUtility.IsMainHubScene(scene))
            SpawnExitGate(scene.buildIndex);

        WarmSlidePool();
    }

    /// <summary>Public hook if other systems spawn UI before Mandatory runs.</summary>
    public void SpawnVictoryPortal(int buildIndex) =>
        SpawnExitGate(buildIndex);

    void PrepareGlobalServices()
    {
        if (SceneLoader.Instance == null)
            new GameObject("SceneLoader").AddComponent<SceneLoader>();

        if (SaveSystem.Instance == null)
            new GameObject("SaveSystem").AddComponent<SaveSystem>();
    }

    static void EnsureEventSystemForUi() => UiInputEnsure.Bootstrap();

    void WarmSlidePool()
    {
        if (ObjectPool.Instance != null)
            return;

        GameObject pooledPrefab = Resources.Load<GameObject>("PooledSlideDust");
        GameObject holder = new GameObject("ObjectPool");
        DontDestroyOnLoad(holder);
        ObjectPool pool = holder.AddComponent<ObjectPool>();

        if (pooledPrefab != null && pool != null)
            pool.RegisterRuntimePool("SlideDust", pooledPrefab, 26);
    }

    void ClearGameOverUi()
    {
        if (_gameOverOverlay != null)
        {
            Destroy(_gameOverOverlay);
            _gameOverOverlay = null;
        }

        PauseFlow.PauseMenuEnabled = true;
    }

    public void ShowGameOverScreen()
    {
        if (_gameOverOverlay != null)
            return;

        _gameOverFont = GameUiFonts.DefaultUIFont();
        Time.timeScale = 0f;
        PauseFlow.PauseMenuEnabled = false;

        Canvas shell = BuildTopSortingCanvas();
        _gameOverOverlay = shell.gameObject;

        GameObject tintGo = new GameObject("GameOverTint");
        tintGo.transform.SetParent(shell.transform, false);

        RectTransform tintRect = tintGo.AddComponent<RectTransform>();

        StretchUiFull(tintRect);

        Image dim = tintGo.AddComponent<Image>();

        dim.color = new Color(0.02f, 0.015f, 0.012f, 0.92f);

        dim.raycastTarget = true;

        VerticalLayoutGroup stack =
            PanelStack(shell.transform, new Color(0f, 0f, 0f, 0.52f));

        Heading(stack.transform, "Game Over", 44).color =
            new Color(0.98f, 0.58f, 0.35f);

        Text blurbGo = Heading(stack.transform, "The forest claims another wanderer.", 22);

        blurbGo.color = new Color(0.92f, 0.82f, 0.74f);

        PrimaryButton(stack.transform, "Try Again", () =>
        {
            Time.timeScale = 1f;
            ClearGameOverUi();
            DeathReplay.RestartCurrentScene();
        });

        PrimaryButton(stack.transform, "Main Menu", () =>
        {
            Time.timeScale = 1f;

            ClearGameOverUi();

            SceneLoader.Instance?.LoadScene(0);
        });

        stack.transform.SetAsLastSibling();
    }

    void SpawnExitGate(int idx)
    {
        GoalGate[] oldGates = UnityEngine.Object.FindObjectsOfType<GoalGate>();

        foreach (GoalGate gg in oldGates)
        {
            if (gg != null)
                Destroy(gg.gameObject);
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        Vector3 origin = player != null
            ? player.transform.position + Vector3.right * 32f + Vector3.up * 0.75f
            : new Vector3(18f, -1f, 0f);

        GameObject gate = new GameObject("VictoryPortal");

        gate.transform.position = origin;

        BoxCollider2D box = gate.AddComponent<BoxCollider2D>();

        box.isTrigger = true;

        box.size = new Vector2(2.4f, 6f);

        GoalGate gateBehaviour = gate.AddComponent<GoalGate>();

        gateBehaviour.Configure(idx);
    }

    Canvas BuildTopSortingCanvas()
    {
        GameObject go = new GameObject("BootstrapTopUICanvas");
        go.transform.SetParent(transform, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        go.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    static void StretchUiFull(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;

        rect.anchorMax = Vector2.one;

        rect.offsetMin = Vector2.zero;

        rect.offsetMax = Vector2.zero;
    }

    VerticalLayoutGroup PanelStack(Transform parent, Color backdrop)
    {
        GameObject panel = new GameObject("MainPanel");

        panel.transform.SetParent(parent, false);

        Image img = panel.AddComponent<Image>();

        img.color = backdrop;

        RectTransform rectTransform = panel.GetComponent<RectTransform>();

        rectTransform.sizeDelta = new Vector2(640f, 720f);

        VerticalLayoutGroup row = panel.AddComponent<VerticalLayoutGroup>();
        row.childAlignment = TextAnchor.MiddleCenter;

        row.spacing = 26f;

        row.padding = new RectOffset(32, 32, 32, 32);
        ContentSizeFitter fitter = panel.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return row;
    }

    Text Heading(Transform parent, string caption, int size)
    {
        GameObject go = new GameObject("Heading");

        go.transform.SetParent(parent, false);

        Text text = go.AddComponent<Text>();
        text.font = _gameOverFont ?? GameUiFonts.DefaultUIFont();
        text.fontSize = size;

        text.alignment = TextAnchor.MiddleCenter;
        text.text = caption;
        text.color = Color.white;
        LayoutElement metrics = go.AddComponent<LayoutElement>();

        metrics.minHeight = size + 10f;

        return text;
    }

    Button PrimaryButton(Transform parent, string caption,
        UnityEngine.Events.UnityAction listener)
    {
        GameObject go = new GameObject($"Button_{caption}");

        go.transform.SetParent(parent, false);

        Image panelImg = go.AddComponent<Image>();

        panelImg.color = new Color(0.18f, 0.12f, 0.08f, 0.75f);

        Button button = go.AddComponent<Button>();

        ColorBlock colors = button.colors;

        colors.highlightedColor = new Color(0.78f, 0.45f, 0.18f);

        colors.pressedColor = new Color(0.55f, 0.28f, 0.1f);

        button.colors = colors;

        LayoutElement sizing = go.AddComponent<LayoutElement>();

        sizing.minHeight = 80f;

        sizing.minWidth = 420f;

        GameObject textGo = new GameObject("Caption");

        textGo.transform.SetParent(go.transform, false);

        RectTransform textRect = textGo.AddComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGo.AddComponent<Text>();
        text.font = _gameOverFont ?? GameUiFonts.DefaultUIFont();
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = caption;
        text.color = new Color(0.98f, 0.86f, 0.71f);

        button.onClick.AddListener(listener);

        return button;
    }
}
