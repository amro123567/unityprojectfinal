using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class BootstrapRuntime : MonoBehaviour
{
    public static BootstrapRuntime Active { get; private set; }

    Transform hudSurface;
    Font uiFont;
    GameObject gameOverOverlay;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void CreateHost()
    {
        if (Active != null)
            return;

        GameObject host = new GameObject("BootstrapRuntime");
        DontDestroyOnLoad(host);
        Active = host.AddComponent<BootstrapRuntime>();
    }

    void Awake()
    {
        if (Active != null && Active != this)
        {
            Destroy(gameObject);
            return;
        }

        Active = this;
        uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (uiFont == null)
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        hudSurface = new GameObject("RuntimeUISurface").transform;
        hudSurface.SetParent(transform, false);

        SceneManager.sceneLoaded += SceneLoadedHandler;
        PrepareGlobalServices();
        SceneLoadedHandler(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneLoadedHandler;
    }

    void PrepareGlobalServices()
    {
        if (SceneLoader.Instance == null)
            new GameObject("SceneLoader").AddComponent<SceneLoader>();

        if (SaveSystem.Instance == null)
            new GameObject("SaveSystem").AddComponent<SaveSystem>();

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        WarmSlidePool();
    }

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

    void SceneLoadedHandler(Scene scene, LoadSceneMode mode)
    {
        PauseFlow.PauseMenuEnabled = true;

        ClearGameOverUi();
        OptionsHost.DropSurface();
        PauseFlow.FocusCanvas = null;

        foreach (Transform child in hudSurface)
            Destroy(child.gameObject);

        bool useMainMenuUi = IsMainHubScene(scene);

        if (useMainMenuUi)
            BuildMainMenuShell();
        else
        {
            BuildHudShell();
            BuildPauseMenuShell();

            SpawnExitGate(scene.buildIndex);
        }
    }

    /// <summary>
    /// True for build index 0 and for editor Play on scenes named MainMenu / "Main Menu".
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

    void ClearGameOverUi()
    {
        if (gameOverOverlay != null)
        {
            Destroy(gameOverOverlay);
            gameOverOverlay = null;
        }

        PauseFlow.PauseMenuEnabled = true;
    }

    public void ShowGameOverScreen()
    {
        if (gameOverOverlay != null)
            return;

        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (uiFont == null)
                uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        Time.timeScale = 0f;
        PauseFlow.PauseMenuEnabled = false;

        Canvas shell = BuildTopSortingCanvas();
        gameOverOverlay = shell.gameObject;

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
            Scene s = SceneManager.GetActiveScene();
            ClearGameOverUi();
            if (s.buildIndex >= 0)
                SceneManager.LoadScene(s.buildIndex);
            else if (!string.IsNullOrEmpty(s.name))
                SceneManager.LoadScene(s.name);
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

    void BuildMainMenuShell()
    {
        Canvas canvas = CreateCanvas(hudSurface, 5200);
        GraphicRaycaster raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;

        GameObject dimGo = new GameObject("MainMenuBackdrop");
        dimGo.transform.SetParent(canvas.transform, false);

        RectTransform dimRect = dimGo.AddComponent<RectTransform>();

        StretchUiFull(dimRect);

        Image dimImg = dimGo.AddComponent<Image>();
        dimImg.color = new Color(0.1f, 0.06f, 0.036f, 0.62f);
        dimImg.raycastTarget = false;

        VerticalLayoutGroup stack = PanelStack(
            canvas.transform,
            new Color(0.08f, 0.045f, 0.028f, 0.93f));

        Text title = Heading(stack.transform, "Autumn Vanguard", 40);
        title.color = new Color(0.95f, 0.62f, 0.37f);

        Text tag = Heading(stack.transform, "Fall course build • Two grove levels • Pause & save-ready", 20);
        tag.color = new Color(0.93f, 0.8f, 0.62f);

        Text keys = Heading(stack.transform, "Help: Esc, Tab, or P on this hub", 18);
        keys.color = new Color(0.85f, 0.73f, 0.52f);

        SaveSystem saver = SaveSystem.Instance;

        bool canResume = saver != null && saver.HasSave;

        PrimaryButton(stack.transform, "New Journey", () =>
        {
            saver?.DeleteSave();
            Time.timeScale = 1f;

            SceneLoader.Instance?.LoadScene(1);
        });

        Button resume = PrimaryButton(stack.transform, "Continue", () =>
        {
            if (saver?.HasSave == true)
            {
                int target = Mathf.Clamp(saver.GetData().currentLevel, 1,
                    Mathf.Max(1, SceneManager.sceneCountInBuildSettings - 1));

                Time.timeScale = 1f;
                SceneLoader.Instance?.LoadScene(target);
            }
        });

        resume.interactable = canResume;

        PrimaryButton(stack.transform, "Adjust Audio", () => OptionsHost.Toggle(canvas.transform));

        PrimaryButton(stack.transform, "Quit", () => SceneLoader.Instance?.QuitGame());

        MainMenuEscPanel esc = canvas.gameObject.AddComponent<MainMenuEscPanel>();
        esc.Setup(uiFont);
    }

    void BuildPauseMenuShell()
    {
        GameObject hud = new GameObject("PauseController");
        hud.transform.SetParent(hudSurface, false);
        hud.AddComponent<PauseFlow>();

    }

    void BuildHudShell()
    {
        Canvas canvas = CreateCanvas(hudSurface, 4900);
        canvas.gameObject.AddComponent<GraphicRaycaster>();

        GameObject hud = new GameObject("HudBlock");
        hud.transform.SetParent(canvas.transform, false);

        RectTransform rect = hud.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(42f, -36f);

        UnityEngine.UI.Outline halo = hud.AddComponent<UnityEngine.UI.Outline>();

        UnityEngine.UI.Shadow shadow = hud.AddComponent<UnityEngine.UI.Shadow>();

        Text label = hud.AddComponent<Text>();

        ColorUtility.TryParseHtmlString("#2E1F12", out Color body);
        halo.effectColor = new Color(0f, 0f, 0f, 0.55f);

        halo.effectDistance = new Vector2(1.2f, -1f);

        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);

        shadow.effectDistance = new Vector2(1.8f, -1.8f);

        label.font = uiFont;
        label.fontSize = 26;
        label.alignment = TextAnchor.UpperLeft;
        label.color = body;
        label.raycastTarget = false;

        LiveHud hudDriver = hud.AddComponent<LiveHud>();
        hudDriver.Bind(label);

        PauseFlow.FocusCanvas = canvas;

        AppendHudPauseControl(canvas);
    }

    Canvas CreateCanvas(Transform parent, int sortingOrder = 4900)
    {
        GameObject go = new GameObject("RuntimeCanvas");

        go.transform.SetParent(parent, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
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

    void AppendHudPauseControl(Canvas gameplayCanvas)
    {
        GameObject dock = new GameObject("HudPauseDock");
        dock.transform.SetParent(gameplayCanvas.transform, false);

        RectTransform dockRect = dock.AddComponent<RectTransform>();
        dockRect.anchorMin = new Vector2(1f, 1f);
        dockRect.anchorMax = new Vector2(1f, 1f);
        dockRect.pivot = new Vector2(1f, 1f);
        dockRect.anchoredPosition = new Vector2(-22f, -20f);

        VerticalLayoutGroup col = dock.AddComponent<VerticalLayoutGroup>();
        col.childAlignment = TextAnchor.UpperRight;
        col.spacing = 10f;

        GameObject hint = new GameObject("PauseHints");
        hint.transform.SetParent(dock.transform, false);

        LayoutElement hintLe = hint.AddComponent<LayoutElement>();
        hintLe.minHeight = 38f;

        Text ht = hint.AddComponent<Text>();
        ht.font = uiFont;
        ht.fontSize = 17;
        ht.alignment = TextAnchor.MiddleRight;
        ht.color = new Color(0.92f, 0.74f, 0.54f);
        ht.text = "Pause: Esc  •  Tab  •  P";

        GameObject tap = new GameObject("HudPauseTap");
        tap.transform.SetParent(dock.transform, false);

        LayoutElement tapLe = tap.AddComponent<LayoutElement>();
        tapLe.minWidth = 154f;
        tapLe.minHeight = 54f;

        Image plate = tap.AddComponent<Image>();
        plate.color = new Color(0.26f, 0.17f, 0.09f, 0.94f);

        Button btn = tap.AddComponent<Button>();
        btn.targetGraphic = plate;

        GameObject cap = new GameObject("Cap");
        cap.transform.SetParent(tap.transform, false);
        RectTransform capRect = cap.AddComponent<RectTransform>();
        StretchUiFull(capRect);

        Text capText = cap.AddComponent<Text>();
        capText.font = uiFont;
        capText.fontSize = 23;
        capText.text = "Pause menu";
        capText.color = Color.white;
        capText.alignment = TextAnchor.MiddleCenter;

        btn.onClick.AddListener(() => PauseFlow.RequestTogglePause());
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
        text.font = uiFont;
        text.fontSize = size;

        text.alignment = TextAnchor.MiddleCenter;
        text.text = caption;
        text.color = Color.white;
        LayoutElement metrics = go.AddComponent<LayoutElement>();

        metrics.minHeight = size + 10f;

        return text;
    }

    Button PrimaryButton(Transform parent, string caption, UnityEngine.Events.UnityAction listener)
    {
        GameObject go = new GameObject($"Button_{caption}");

        go.transform.SetParent(parent, false);

        Image panel = go.AddComponent<Image>();

        panel.color = new Color(0.18f, 0.12f, 0.08f, 0.75f);

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
        text.font = uiFont;
        text.fontSize = 26;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = caption;
        text.color = new Color(0.98f, 0.86f, 0.71f);

        button.onClick.AddListener(listener);

        return button;
    }
}
