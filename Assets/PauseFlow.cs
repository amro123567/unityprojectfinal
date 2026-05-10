using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PauseFlow : MonoBehaviour
{
    public static Canvas FocusCanvas;

    /// <summary>When false, Esc pause is ignored (e.g. game over).</summary>
    public static bool PauseMenuEnabled { get; set; } = true;

    bool menuOpen;

    RectTransform veil;

    Font labelFont;

    void Awake()
    {
        labelFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (labelFont == null)
            labelFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildPanel();
        SetMenuVisible(false);
    }

    void Update()
    {
        if (!PauseMenuEnabled)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleMenu();
    }

    /// <returns>False for main-menu scenes; true when playing a level scene (includes editor Play with buildIndex -1).</returns>
    static bool IsGameplayLevelScene(Scene scene)
    {
        bool namedMenu =
            scene.name.IndexOf("MainMenu", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (scene.buildIndex == 0)
            return false;

        if (scene.buildIndex < 0)
            return !namedMenu;

        return scene.buildIndex >= 1;
    }

    void ToggleMenu()
    {
        if (veil == null)
            return;

        if (!IsGameplayLevelScene(SceneManager.GetActiveScene()))
            return;

        SetMenuVisible(!menuOpen);
    }

    void SetMenuVisible(bool state)
    {
        menuOpen = state;
        veil.gameObject.SetActive(state);
        Time.timeScale = state ? 0f : 1f;
    }

    void BuildPanel()
    {
        if (veil != null)
            return;

        if (FocusCanvas == null)
            return;

        GameObject overlay = new GameObject("PauseVeil");

        veil = overlay.AddComponent<RectTransform>();

        veil.SetParent(FocusCanvas.transform, false);

        veil.SetAsLastSibling();
        veil.anchorMin = Vector2.zero;

        veil.anchorMax = Vector2.one;

        veil.offsetMin = Vector2.zero;
        veil.offsetMax = Vector2.zero;

        Image tint = overlay.AddComponent<Image>();

        tint.color = new Color(0.05f, 0.03f, 0.02f, 0.74f);

        tint.raycastTarget = true;

        VerticalLayoutGroup stack = StackColumn(overlay.transform);

        Title(stack.transform, "Paused", 38f);

        Title(stack.transform, "(Esc closes this pause menu)", 18);

        LinkButton(stack.transform, "Resume", ResumeClicked);

        LinkButton(stack.transform, "Audio Levels", TuneAudioHandler);

        LinkButton(stack.transform, "Main Grove", JourneyHome);

        LinkButton(stack.transform, "Quit", QuitHandler);

        overlay.SetActive(false);
    }

    VerticalLayoutGroup StackColumn(Transform parent)
    {
        GameObject column = new GameObject("PauseColumn");

        column.transform.SetParent(parent, false);

        RectTransform rect = column.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.52f);

        rect.anchorMax = new Vector2(0.5f, 0.52f);

        rect.sizeDelta = new Vector2(560f, 520f);

        VerticalLayoutGroup group = column.AddComponent<VerticalLayoutGroup>();
        group.childAlignment = TextAnchor.MiddleCenter;

        group.spacing = 18f;

        group.padding = new RectOffset(20, 20, 20, 20);
        ContentSizeFitter fitter = column.AddComponent<ContentSizeFitter>();

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return group;
    }

    void Title(Transform holder, string line, float size)
    {
        GameObject textGo = new GameObject("PausedTitle");

        textGo.transform.SetParent(holder, false);

        LayoutElement spacer = textGo.AddComponent<LayoutElement>();

        spacer.minHeight = size + 16f;

        Text text = textGo.AddComponent<Text>();
        text.font = labelFont;
        text.fontSize = (int)size;

        text.alignment = TextAnchor.MiddleCenter;

        text.text = line;

        ColorUtility.TryParseHtmlString("#FFD8B7", out Color tone);

        text.color = tone;
    }

    void LinkButton(Transform holder, string label, UnityEngine.Events.UnityAction handler)
    {
        GameObject go = new GameObject($"Pause_{label}");

        go.transform.SetParent(holder, false);

        LayoutElement sizing = go.AddComponent<LayoutElement>();

        sizing.minHeight = 64f;

        sizing.minWidth = 420f;

        Image plate = go.AddComponent<Image>();

        plate.color = new Color(0.18f, 0.1f, 0.06f, 0.92f);

        Button button = go.AddComponent<Button>();

        ColorBlock colors = button.colors;

        colors.highlightedColor = new Color(0.68f, 0.4f, 0.22f);

        colors.pressedColor = new Color(0.52f, 0.28f, 0.08f);

        button.colors = colors;

        GameObject caption = new GameObject("Caption");

        caption.transform.SetParent(go.transform, false);

        RectTransform textRect = caption.AddComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text textField = caption.AddComponent<Text>();

        textField.font = labelFont;
        textField.fontSize = 26;
        textField.alignment = TextAnchor.MiddleCenter;
        textField.text = label;
        ColorUtility.TryParseHtmlString("#FFE5CC", out Color ink);

        textField.color = ink;

        button.onClick.AddListener(handler);
    }

    void ResumeClicked()
    {
        SetMenuVisible(false);
    }

    void TuneAudioHandler()
    {
        if (FocusCanvas != null)
            OptionsHost.Toggle(FocusCanvas.transform);
    }

    void JourneyHome()
    {
        SetMenuVisible(false);
        SceneLoader loader = SceneLoader.Instance;

        if (loader != null)
            loader.LoadScene(0);
    }

    void QuitHandler()
    {
        SceneLoader.Instance?.QuitGame();
    }
}
