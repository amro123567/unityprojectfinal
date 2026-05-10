using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class OptionsHost
{
    static GameObject shell;

    static Font FontHold => Resources.GetBuiltinResource<Font>("Arial.ttf");

    public static void DropSurface()
    {
        if (shell == null)
            return;

        Object.Destroy(shell);
        shell = null;
    }

    public static void Toggle(Transform context)
    {
        if (shell == null)
            BuildSurface(context);

        shell.SetActive(!shell.activeSelf);
    }

    static void BuildSurface(Transform context)
    {
        Canvas host = HudSurfaceBus.ActiveCanvas;

        if (host == null && context != null)
            host = context.GetComponentInParent<Canvas>();

        if (host == null)
            return;

        shell = new GameObject("AudioOptionsPlane");

        shell.transform.SetParent(host.transform, false);

        RectTransform frame = shell.AddComponent<RectTransform>();

        frame.anchorMin = new Vector2(0.61f, 0.52f);

        frame.anchorMax = new Vector2(0.94f, 0.92f);

        frame.offsetMin = Vector2.zero;

        frame.offsetMax = Vector2.zero;

        shell.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.07f, 0.94f);

        VerticalLayoutGroup column = VerticalStack(shell.transform);

        Label(column.transform, "Split Mix Console", 30f);

        AudioManager mgr = AudioManager.Instance;

        float musicSeed = mgr != null ? mgr.GetMusic01() : 0.65f;

        float sfxSeed = mgr != null ? mgr.GetSfx01() : 0.82f;

        BuildMixerRow(column.transform, "Wind & Woodwinds", musicSeed, v =>
        {
            if (mgr != null)
                mgr.SetMusicLevel01(v);
        });

        BuildMixerRow(column.transform, "Footfalls & Steel", sfxSeed, v =>
        {
            if (mgr != null)
                mgr.SetSfxLevel01(v);
        });

        SimpleButton(column.transform, "Close Panel", ClosePanel);

        shell.transform.SetAsLastSibling();

        shell.SetActive(false);
    }

    sealed class Channel
    {
        public float Value;

        public Text Readout;
    }

    static void BuildMixerRow(Transform parent, string title, float seed, Action<float> sink)
    {
        GameObject row = new GameObject($"MixerRow_{title}");

        row.transform.SetParent(parent, false);

        LayoutElement ruler = row.AddComponent<LayoutElement>();

        ruler.minHeight = 96f;

        VerticalLayoutGroup stack = row.AddComponent<VerticalLayoutGroup>();
        stack.spacing = 12f;

        Label(stack.transform, title, 26f);

        Channel channel = new Channel
        {
            Value = Mathf.Clamp01(seed)
        };

        GameObject controlLine = new GameObject("ControlStrip");

        controlLine.transform.SetParent(stack.transform, false);

        HorizontalLayoutGroup line = controlLine.AddComponent<HorizontalLayoutGroup>();
        line.childAlignment = TextAnchor.MiddleCenter;
        line.spacing = 18f;

        MixerButton(line.transform, "-", () =>
        {
            channel.Value = Mathf.Clamp01(channel.Value - 0.05f);

            sink.Invoke(channel.Value);

            RefreshReadout(channel);
        });

        channel.Readout = BuildReadout(line.transform, channel.Value);

        MixerButton(line.transform, "+", () =>
        {
            channel.Value = Mathf.Clamp01(channel.Value + 0.05f);

            sink.Invoke(channel.Value);

            RefreshReadout(channel);
        });

        sink.Invoke(channel.Value);

        RefreshReadout(channel);
    }

    static void RefreshReadout(Channel channel)
    {
        if (channel.Readout != null)
            channel.Readout.text = $"{Mathf.RoundToInt(channel.Value * 100f)}%";
    }

    static Text BuildReadout(Transform parent, float seed)
    {
        GameObject chip = new GameObject("ValueReadout");

        chip.transform.SetParent(parent, false);

        LayoutElement sizing = chip.AddComponent<LayoutElement>();

        sizing.minWidth = 108f;

        Text text = chip.AddComponent<Text>();

        text.font = FontHold;
        text.fontSize = 28;

        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;

        text.text = $"{Mathf.RoundToInt(Mathf.Clamp01(seed) * 100f)}%";

        chip.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.45f);

        return text;
    }

    static void MixerButton(Transform parent, string label, UnityAction action)
    {
        GameObject go = new GameObject($"MixerBtn_{label}");

        go.transform.SetParent(parent, false);

        LayoutElement layout = go.AddComponent<LayoutElement>();

        layout.minHeight = 60f;

        layout.minWidth = 68f;

        Image plate = go.AddComponent<Image>();
        plate.color = new Color(0.93f, 0.55f, 0.28f);

        Button btn = go.AddComponent<Button>();

        btn.onClick.AddListener(action);

        GameObject caption = new GameObject("Caption");

        caption.transform.SetParent(go.transform, false);

        RectTransform textRect = caption.AddComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;

        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text textField = caption.AddComponent<Text>();

        textField.font = FontHold;

        textField.fontSize = 38;
        textField.text = label;
        textField.color = Color.white;

        textField.alignment = TextAnchor.MiddleCenter;
    }

    static VerticalLayoutGroup VerticalStack(Transform parent)
    {
        GameObject tray = new GameObject("MixerTray");

        tray.transform.SetParent(parent, false);

        RectTransform rect = tray.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;

        rect.offsetMin = new Vector2(26f, 26f);

        rect.offsetMax = new Vector2(-26f, -26f);

        VerticalLayoutGroup group = tray.AddComponent<VerticalLayoutGroup>();
        group.childAlignment = TextAnchor.UpperCenter;
        group.spacing = 26f;

        return group;
    }

    static Text Label(Transform parent, string caption, float size)
    {
        GameObject textGo = new GameObject($"Label_{caption}");

        textGo.transform.SetParent(parent, false);

        LayoutElement ruler = textGo.AddComponent<LayoutElement>();
        ruler.minHeight = size + 8f;

        Text text = textGo.AddComponent<Text>();
        text.font = FontHold;
        text.fontSize = (int)size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = caption;

        return text;
    }

    static void SimpleButton(Transform parent, string label, UnityAction callback)
    {
        GameObject go = new GameObject($"CloseBtn_{label}");

        go.transform.SetParent(parent, false);

        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.minHeight = 64f;

        Image plate = go.AddComponent<Image>();
        plate.color = new Color(0.89f, 0.49f, 0.23f);

        Button btn = go.AddComponent<Button>();
        btn.onClick.AddListener(callback);

        GameObject caption = new GameObject("Txt");

        caption.transform.SetParent(go.transform, false);

        RectTransform textRect = caption.AddComponent<RectTransform>();

        textRect.anchorMin = Vector2.zero;

        textRect.anchorMax = Vector2.one;

        textRect.offsetMin = Vector2.zero;

        textRect.offsetMax = Vector2.zero;

        Text textField = caption.AddComponent<Text>();

        textField.font = FontHold;

        textField.fontSize = 26;
        textField.text = label;

        textField.color = Color.white;

        textField.alignment = TextAnchor.MiddleCenter;
    }

    static void ClosePanel()
    {
        if (shell != null)
            shell.SetActive(false);
    }
}
