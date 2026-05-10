using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using CodeMonkey.HealthSystemCM;

/// <summary>
/// Self-contained course UI: main menu hub, gameplay HUD + health bar fill, oversized PAUSE button, pause panel (resume / audio / main menu / quit).
/// Does not rely on PauseFlow or the old Bootstrap canvas stack.
/// </summary>
[DefaultExecutionOrder(-9000)]
public sealed class MandatoryCourseUi : MonoBehaviour
{
    public static MandatoryCourseUi Instance { get; private set; }

    const int SortOrder = 45000;

    Font _font;
    Canvas _rootCanvas;
    Transform _layer;

    HeroKnight _tracked;

    GameObject _levelRoot;
    Text _healthText;
    Image _healthFill;
    Button _pauseOpenButton;

    GameObject _pauseOverlay;
    bool _paused;

    bool _scheduledRetry;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RuntimeBootMandatoryUi()
    {
        Ensure();
        Scene s = SceneManager.GetActiveScene();
        if (Instance != null && s.IsValid() && s.isLoaded)
            Instance.RebuildForScene(s);
    }

    public static void Ensure()
    {
        if (Instance != null)
            return;

        GameObject go = new GameObject("MandatoryCourseUi");
        DontDestroyOnLoad(go);
        go.AddComponent<MandatoryCourseUi>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _font = GameUiFonts.DefaultUIFont();
    }

    void Start()
    {
        if (_scheduledRetry)
            return;

        _scheduledRetry = true;
        StartCoroutine(CoRebuildNextFrame());
    }

    IEnumerator CoRebuildNextFrame()
    {
        yield return null;
        Scene s = SceneManager.GetActiveScene();
        if (s.IsValid() && s.isLoaded)
            RebuildForScene(s);
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void RebuildForScene(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        _font ??= GameUiFonts.DefaultUIFont();
        _paused = false;
        Time.timeScale = 1f;

        EnsureEventSystem();
        EnsurePersistedServices();

        ProfessorFallbackUi.Clear();
        OptionsHost.DropSurface();

        TearDownUi();

        BuildCanvasSkeleton();
        PauseFlow.FocusCanvas = _rootCanvas;

        if (HubSceneUtility.IsMainHubScene(scene))
            BuildHub(scene);
        else
            BuildLevel(scene);

        Debug.Log(
            $"MandatoryCourseUi: built UI for '{scene.name}' (buildIndex={scene.buildIndex}). " +
            "If nothing shows, check Console errors and Hierarchy → DontDestroyOnLoad → MandatoryCourseUi.");
    }

    Font TextFontOrDefault()
    {
        _font ??= GameUiFonts.DefaultUIFont();
        return _font;
    }

    void Update()
    {
        if (_pauseOverlay != null && _levelRoot != null && _levelRoot.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab) ||
                Input.GetKeyDown(KeyCode.P))
                TogglePauseOverlay();
        }
    }

    void LateUpdate()
    {
        UpdateHealthHud();
    }

    void UpdateHealthHud()
    {
        if (_healthText == null && _healthFill == null)
            return;

        if (_tracked == null)
            _tracked = FindFirstHero();

        HeroKnight hero = _tracked;

        if (hero == null)
        {
            if (_healthText != null)
                _healthText.text = "-- / --";

            if (_healthFill != null)
                _healthFill.fillAmount = 0f;

            return;
        }

        float max = Mathf.Max(1f, hero.GetHealthMax());
        float cur = Mathf.Clamp(hero.GetHealthCurrent(), 0f, max);
        float n = Mathf.Clamp01(cur / max);

        if (_healthFill != null)
            _healthFill.fillAmount = n;

        if (_healthText != null)
        {
            int ci = Mathf.RoundToInt(cur);
            int mx = Mathf.RoundToInt(max);
            _healthText.text = $"{SceneLabel()}\n{ci} / {mx} HP";
        }
    }

    string SceneLabel()
    {
        Scene s = SceneManager.GetActiveScene();
        if (s.buildIndex >= 1)
            return $"Level {s.buildIndex}";
        string n = s.name;
        if (n.StartsWith("Level", StringComparison.OrdinalIgnoreCase))
            return n;
        return n.Length > 0 ? n : "Level";
    }

    static HeroKnight FindFirstHero()
    {
        HeroKnight hero = UnityEngine.Object.FindFirstObjectByType<HeroKnight>();
        return hero;
    }

    void TogglePauseOverlay()
    {
        if (_pauseOverlay == null)
            return;

        SetPaused(!_paused);
    }

    void SetPaused(bool pause)
    {
        _paused = pause;
        if (_pauseOverlay != null)
            _pauseOverlay.SetActive(pause);

        Time.timeScale = pause ? 0f : 1f;

        if (_pauseOpenButton != null)
            _pauseOpenButton.interactable = !pause;

        if (_pauseOverlay != null)
            _pauseOverlay.transform.SetAsLastSibling();
    }

    void TearDownUi()
    {
        _levelRoot = null;
        _healthText = null;
        _healthFill = null;
        _pauseOpenButton = null;
        _pauseOverlay = null;
        _tracked = null;

        if (_rootCanvas != null)
        {
            Destroy(_rootCanvas.gameObject);
            _rootCanvas = null;
        }

        _layer = null;
    }

    void BuildCanvasSkeleton()
    {
        GameObject holder = new GameObject("MandatoryUiRoot");
        holder.transform.SetParent(transform, false);

        _rootCanvas = holder.AddComponent<Canvas>();
        _rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _rootCanvas.sortingOrder = SortOrder;
        _rootCanvas.overrideSorting = true;

        holder.AddComponent<GraphicRaycaster>();

        CanvasScaler scaler = holder.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject layer = new GameObject("Layer");
        layer.transform.SetParent(holder.transform, false);
        RectTransform lr = layer.AddComponent<RectTransform>();
        Stretch(lr);

        _layer = layer.transform;
    }

    void BuildHub(Scene scene)
    {
        GameObject hub = new GameObject("MandatoryHubUi");
        hub.transform.SetParent(_layer, false);
        Stretch(hub.AddComponent<RectTransform>());

        Image hubBg = hub.AddComponent<Image>();
        hubBg.sprite = SpriteUtilityWhite();
        hubBg.color = new Color(0.05f, 0.035f, 0.022f, 0.94f);

        GameObject card = new GameObject("MenuCard");
        card.transform.SetParent(hub.transform, false);
        RectTransform cr = card.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0.52f);
        cr.anchorMax = new Vector2(0.5f, 0.52f);
        cr.sizeDelta = new Vector2(680f, 720f);

        Image cardBg = card.AddComponent<Image>();
        cardBg.sprite = SpriteUtilityWhite();
        cardBg.color = new Color(0.1f, 0.06f, 0.036f, 1f);

        VerticalLayoutGroup v = card.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 16f;
        v.padding = new RectOffset(32, 32, 32, 36);

        AddCaption(v.transform, "Autumn Vanguard", 40, new Color(0.98f, 0.6f, 0.34f));

        AddCaption(
            v.transform,
            "New Journey clears save. Continue restores last level + health.",
            20,
            new Color(0.92f, 0.84f, 0.72f));

        SaveSystem saver = SaveSystem.Instance;
        bool canContinue = saver != null && saver.HasSave;

        AddMenuButton(v.transform, "NEW JOURNEY", () =>
        {
            saver?.DeleteSave();
            Time.timeScale = 1f;
            SceneLoader.Instance?.LoadScene(1);
        });

        Button cont = AddMenuButton(v.transform, "CONTINUE", () =>
        {
            if (saver?.HasSave != true)
                return;

            int target = Mathf.Clamp(
                saver.GetData().currentLevel,
                1,
                Mathf.Max(1, SceneManager.sceneCountInBuildSettings - 1));

            Time.timeScale = 1f;
            SceneLoader.Instance?.LoadScene(target);
        });
        cont.interactable = canContinue;

        AddMenuButton(v.transform, "AUDIO / MIX", () => OptionsHost.Toggle(_rootCanvas.transform));

        AddMenuButton(v.transform, "QUIT", () =>
        {
            SceneLoader.Instance?.QuitGame();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });

        hub.transform.SetAsLastSibling();
    }

    void BuildLevel(Scene scene)
    {
        _levelRoot = new GameObject("MandatoryLevelHud");
        _levelRoot.transform.SetParent(_layer, false);
        Stretch(_levelRoot.AddComponent<RectTransform>());

        HideLegacyHealthBars();

        BuildHealthStrip(_levelRoot.transform);
        BuildPauseOpenButton(_levelRoot.transform);

        GameObject veil = new GameObject("MandatoryPauseVeil");
        veil.transform.SetParent(_layer, false);
        Stretch(veil.AddComponent<RectTransform>());

        Image dim = veil.AddComponent<Image>();
        dim.sprite = SpriteUtilityWhite();
        dim.color = new Color(0.02f, 0.02f, 0.03f, 0.86f);
        dim.raycastTarget = true;

        GameObject card = new GameObject("MandatoryPauseCard");
        card.transform.SetParent(veil.transform, false);
        RectTransform cr = card.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0.5f, 0.52f);
        cr.anchorMax = new Vector2(0.5f, 0.52f);
        cr.sizeDelta = new Vector2(560f, 520f);

        Image cbg = card.AddComponent<Image>();
        cbg.sprite = SpriteUtilityWhite();
        cbg.color = new Color(0.09f, 0.056f, 0.032f, 0.99f);

        VerticalLayoutGroup v = card.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 14f;
        v.padding = new RectOffset(24, 24, 28, 28);

        AddCaption(v.transform, "PAUSED", 38, Color.white);
        AddCaption(v.transform, "Tap RESUME / use Esc or P.", 17, new Color(0.9f, 0.8f, 0.65f));

        AddMenuButton(v.transform, "RESUME", () => SetPaused(false));
        AddMenuButton(v.transform, "AUDIO / MIX", () => OptionsHost.Toggle(_rootCanvas.transform));
        AddMenuButton(v.transform, "MAIN MENU", () =>
        {
            SetPaused(false);
            Time.timeScale = 1f;
            SceneLoader.Instance?.LoadScene(0);
        });

        AddMenuButton(v.transform, "QUIT", () =>
        {
            SceneLoader.Instance?.QuitGame();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        });

        _pauseOverlay = veil;
        _pauseOverlay.SetActive(false);

        _levelRoot.transform.SetAsFirstSibling();

        veil.transform.SetAsLastSibling();
    }

    void BuildHealthStrip(Transform parent)
    {
        GameObject row = new GameObject("MandatoryHealthHud");
        row.transform.SetParent(parent, false);

        RectTransform rr = row.AddComponent<RectTransform>();
        rr.anchorMin = new Vector2(0f, 1f);
        rr.anchorMax = new Vector2(0f, 1f);
        rr.pivot = new Vector2(0f, 1f);
        rr.anchoredPosition = new Vector2(34f, -28f);
        rr.sizeDelta = new Vector2(520f, 96f);

        GameObject backdrop = new GameObject("Backdrop");
        backdrop.transform.SetParent(row.transform, false);
        RectTransform br = backdrop.AddComponent<RectTransform>();
        br.anchorMin = Vector2.zero;
        br.anchorMax = new Vector2(1f, 0f);
        br.pivot = new Vector2(0.5f, 0f);
        br.offsetMin = new Vector2(0f, -32f);
        br.offsetMax = new Vector2(0f, 0f);

        Image back = backdrop.AddComponent<Image>();
        back.sprite = SpriteUtilityWhite();
        back.color = new Color(0.1f, 0.056f, 0.03f, 0.94f);

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(backdrop.transform, false);
        RectTransform fr = fillGo.AddComponent<RectTransform>();
        fr.anchorMin = Vector2.zero;
        fr.anchorMax = Vector2.one;
        fr.offsetMin = new Vector2(4f, 4f);
        fr.offsetMax = new Vector2(-4f, -4f);

        _healthFill = fillGo.AddComponent<Image>();
        _healthFill.sprite = SpriteUtilityWhite();
        _healthFill.type = Image.Type.Filled;
        _healthFill.fillMethod = Image.FillMethod.Horizontal;
        _healthFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _healthFill.color = new Color(0.93f, 0.21f, 0.13f, 1f);
        _healthFill.fillAmount = 1f;

        GameObject textGo = new GameObject("HudText");
        textGo.transform.SetParent(row.transform, false);
        RectTransform tr = textGo.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = new Vector2(6f, 4f);
        tr.offsetMax = new Vector2(-6f, -36f);

        _healthText = textGo.AddComponent<Text>();
        _healthText.font = TextFontOrDefault();
        _healthText.fontSize = 26;
        _healthText.fontStyle = FontStyle.Bold;
        _healthText.alignment = TextAnchor.UpperLeft;
        _healthText.color = new Color(0.16f, 0.09f, 0.045f);

        Outline ol = textGo.AddComponent<Outline>();
        ol.effectColor = new Color(1f, 1f, 1f, 0.42f);
        ol.effectDistance = new Vector2(1f, -1f);
    }

    void BuildPauseOpenButton(Transform parent)
    {
        GameObject go = new GameObject("MandatoryPauseBeacon");
        go.transform.SetParent(parent, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(1f, 1f);
        r.anchorMax = new Vector2(1f, 1f);
        r.pivot = new Vector2(1f, 1f);
        r.anchoredPosition = new Vector2(-18f, -18f);
        r.sizeDelta = new Vector2(280f, 96f);

        Image plate = go.AddComponent<Image>();
        plate.sprite = SpriteUtilityWhite();
        plate.color = new Color(1f, 0.72f, 0.06f, 1f);

        _pauseOpenButton = go.AddComponent<Button>();
        _pauseOpenButton.targetGraphic = plate;
        _pauseOpenButton.onClick.AddListener(TogglePauseOverlay);

        GameObject cap = new GameObject("Cap");
        cap.transform.SetParent(go.transform, false);
        Stretch(cap.AddComponent<RectTransform>());
        Text t = cap.AddComponent<Text>();
        t.font = TextFontOrDefault();
        t.fontSize = 30;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.black;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = "TAP HERE — PAUSE";
    }

    void HideLegacyHealthBars()
    {
        try
        {
            HealthBarUI[] bars = UnityEngine.Object.FindObjectsOfType<HealthBarUI>(true);
            for (int i = 0; i < bars.Length; i++)
            {
                if (bars[i] != null)
                    bars[i].gameObject.SetActive(false);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"MandatoryCourseUi: legacy health HUD hide skipped: {ex.Message}");
        }
    }

    void AddCaption(Transform holder, string line, int size, Color color)
    {
        GameObject go = new GameObject("CaptionLine");
        go.transform.SetParent(holder, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = size + 12f;

        Text t = go.AddComponent<Text>();
        t.font = TextFontOrDefault();
        t.fontSize = size;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.text = line;
    }

    Button AddMenuButton(Transform holder, string label, UnityAction onClick)
    {
        GameObject go = new GameObject($"MenuBtn_{label}");
        go.transform.SetParent(holder, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 74f;

        Image plate = go.AddComponent<Image>();
        plate.sprite = SpriteUtilityWhite();
        plate.color = new Color(0.24f, 0.15f, 0.084f, 1f);

        Button b = go.AddComponent<Button>();
        b.colors = ButtonStyle();
        b.onClick.AddListener(onClick);

        GameObject txt = new GameObject("Lbl");
        txt.transform.SetParent(go.transform, false);
        Stretch(txt.AddComponent<RectTransform>());

        Text tfield = txt.AddComponent<Text>();
        tfield.font = TextFontOrDefault();
        tfield.fontSize = 26;
        tfield.alignment = TextAnchor.MiddleCenter;
        tfield.color = new Color(0.97f, 0.91f, 0.74f);
        tfield.text = label;

        return b;
    }

    static ColorBlock ButtonStyle()
    {
        ColorBlock c = ColorBlock.defaultColorBlock;
        c.normalColor = Color.white;
        c.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        c.highlightedColor = new Color(0.92f, 0.78f, 0.55f);

        c.pressedColor = new Color(0.72f, 0.52f, 0.22f);

        return c;
    }

    static void Stretch(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = Vector2.zero;
        r.offsetMax = Vector2.zero;
    }

    static Sprite _whiteSprite;

    static Sprite SpriteUtilityWhite()
    {
        if (_whiteSprite != null)
            return _whiteSprite;

        Texture2D t = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(
            t,
            new Rect(0f, 0f, t.width, t.height),
            new Vector2(0.5f, 0.5f),
            100f);

        return _whiteSprite;
    }

    void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("MandatoryEventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    void EnsurePersistedServices()
    {
        if (SceneLoader.Instance == null)
            new GameObject("SceneLoader").AddComponent<SceneLoader>();

        if (SaveSystem.Instance == null)
            new GameObject("SaveSystem").AddComponent<SaveSystem>();
    }
}
