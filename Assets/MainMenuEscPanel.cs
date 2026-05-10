using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On the main menu, Esc toggles a help panel (levels + input hints). Combat pause is handled by PauseFlow.
/// </summary>
public sealed class MainMenuEscPanel : MonoBehaviour
{
    Font uiFont;
    GameObject sheet;
    bool built;

    public void Setup(Font font)
    {
        uiFont = font;
    }

    void Start()
    {
        EnsureBuilt();
        if (sheet != null)
            sheet.SetActive(false);
    }

    void Update()
    {
        if (!built || sheet == null)
            return;

        bool toggle = Input.GetKeyDown(KeyCode.Escape)
            || Input.GetKeyDown(KeyCode.Tab)
            || Input.GetKeyDown(KeyCode.P);

        if (toggle)
            sheet.SetActive(!sheet.activeSelf);
    }

    void EnsureBuilt()
    {
        if (built)
            return;

        built = true;

        if (uiFont == null)
        {
            uiFont = GameUiFonts.DefaultUIFont();
        }

        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;

        sheet = new GameObject("MainMenuEscSheet");
        RectTransform frame = sheet.AddComponent<RectTransform>();
        frame.SetParent(canvas.transform, false);
        frame.SetAsLastSibling();
        frame.anchorMin = Vector2.zero;
        frame.anchorMax = Vector2.one;
        frame.offsetMin = Vector2.zero;
        frame.offsetMax = Vector2.zero;

        sheet.AddComponent<Image>().color = new Color(0.04f, 0.03f, 0.025f, 0.9f);

        GameObject column = new GameObject("Column");
        column.transform.SetParent(sheet.transform, false);
        RectTransform colRect = column.AddComponent<RectTransform>();
        colRect.anchorMin = new Vector2(0.5f, 0.5f);
        colRect.anchorMax = new Vector2(0.5f, 0.5f);
        colRect.sizeDelta = new Vector2(740f, 460f);

        VerticalLayoutGroup v = column.AddComponent<VerticalLayoutGroup>();
        v.childAlignment = TextAnchor.MiddleCenter;
        v.spacing = 18f;
        v.padding = new RectOffset(28, 28, 28, 28);

        Line(column.transform, "How to play", 34, new Color(0.98f, 0.65f, 0.38f));
        Line(
            column.transform,
            "Levels: New Journey starts at Level 1. Continue loads your last saved level.",
            22,
            new Color(0.94f, 0.86f, 0.76f));
        Line(
            column.transform,
            "Esc, Tab, or P on this menu — toggle this panel.",
            20,
            Color.white);
        Line(
            column.transform,
            "During a level — Esc opens Pause; choose Main Grove to return here.",
            20,
            Color.white);

        GameObject quitBtn = new GameObject("QuitButton");
        quitBtn.transform.SetParent(column.transform, false);
        LayoutElement quitSizing = quitBtn.AddComponent<LayoutElement>();
        quitSizing.minHeight = 72f;
        quitSizing.minWidth = 360f;

        Image plate = quitBtn.AddComponent<Image>();
        plate.color = new Color(0.2f, 0.12f, 0.08f, 0.95f);

        Button quit = quitBtn.AddComponent<Button>();
        quit.targetGraphic = plate;

        GameObject cap = new GameObject("Caption");
        cap.transform.SetParent(quitBtn.transform, false);
        RectTransform cr = cap.AddComponent<RectTransform>();
        cr.anchorMin = Vector2.zero;
        cr.anchorMax = Vector2.one;

        Text capText = cap.AddComponent<Text>();
        capText.font = uiFont;
        capText.fontSize = 26;
        capText.alignment = TextAnchor.MiddleCenter;
        capText.color = Color.white;
        capText.text = "Quit Game";
        quit.onClick.AddListener(() => SceneLoader.Instance?.QuitGame());
    }

    void Line(Transform parent, string raw, int size, Color color)
    {
        GameObject go = new GameObject("Line");
        go.transform.SetParent(parent, false);

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = size + 12f;

        Text t = go.AddComponent<Text>();
        t.font = uiFont;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAnchor.MiddleCenter;
        t.text = raw;
    }
}
