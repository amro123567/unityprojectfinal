using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public sealed class BootstrapRuntime : MonoBehaviour
{
    public static BootstrapRuntime Active { get; private set; }

    Transform hudSurface;
    Font uiFont;

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

        if (Object.FindObjectOfType<EventSystem>() == null)
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
        OptionsHost.DropSurface();
        HudSurfaceBus.Clear();

        foreach (Transform child in hudSurface)
            Destroy(child.gameObject);

        int index = scene.buildIndex;

        if (index <= 0)
            BuildMainMenuShell();
        else
        {
            BuildHudShell();
            BuildPauseMenuShell();

            SpawnExitGate(scene.buildIndex);
        }
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
        Canvas canvas = CreateCanvas(hudSurface);
        GraphicRaycaster raycaster = canvas.gameObject.AddComponent<GraphicRaycaster>();
        raycaster.ignoreReversedGraphics = true;

        VerticalLayoutGroup stack = PanelStack(canvas.transform, Color.clear);

        Text title = Heading(stack.transform, "Autumn Vanguard", 38);
        title.color = new Color(0.95f, 0.6f, 0.35f);

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
    }

    void BuildPauseMenuShell()
    {
        GameObject hud = new GameObject("PauseController");
        hud.transform.SetParent(hudSurface, false);
        hud.AddComponent<PauseFlow>();

    }

    void BuildHudShell()
    {
        Canvas canvas = CreateCanvas(hudSurface);
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
        HudSurfaceBus.ActiveCanvas = canvas;
    }

    Canvas CreateCanvas(Transform parent)
    {
        GameObject go = new GameObject("RuntimeCanvas");

        go.transform.SetParent(parent, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4000;

        CanvasScaler scaler = go.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
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
