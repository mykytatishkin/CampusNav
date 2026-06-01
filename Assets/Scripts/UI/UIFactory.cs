using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shared UI factory methods for building runtime UI elements.
/// All screens use these to create consistent UI.
/// </summary>
public static class UIFactory
{
    // App color palette
    public static readonly Color BgDark = new(0.08f, 0.09f, 0.12f, 0.96f);
    public static readonly Color BgPanel = new(0.12f, 0.13f, 0.17f, 0.95f);
    public static readonly Color BgCard = new(0.16f, 0.17f, 0.22f);
    public static readonly Color BgCardHover = new(0.22f, 0.23f, 0.30f);
    public static readonly Color AccentBlue = new(0.2f, 0.55f, 1f);
    public static readonly Color AccentGreen = new(0.2f, 0.78f, 0.4f);
    public static readonly Color AccentOrange = new(0.95f, 0.6f, 0.15f);
    public static readonly Color AccentRed = new(0.9f, 0.3f, 0.3f);
    public static readonly Color TextPrimary = Color.white;
    public static readonly Color TextSecondary = new(0.7f, 0.7f, 0.75f);
    public static readonly Color TextMuted = new(0.5f, 0.5f, 0.55f);

    public static RectTransform CreateFullscreenPanel(Transform parent, string name, Color? color = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var img = obj.AddComponent<Image>();
        img.color = color ?? BgDark;
        img.raycastTarget = true;
        return rect;
    }

    public static RectTransform CreatePanel(Transform parent, string name, float width, float height, Color? color = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        var img = obj.AddComponent<Image>();
        img.color = color ?? BgPanel;
        return rect;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, string text,
        float fontSize = 14, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color ?? TextPrimary;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static TextMeshProUGUI CreateTextAbsolute(Transform parent, string name, string text,
        Vector2 position, Vector2 size, float fontSize = 14, Color? color = null,
        TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color ?? TextPrimary;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    public static Button CreateButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, Color bgColor, Color textColor,
        UnityEngine.Events.UnityAction onClick, float fontSize = 16)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        img.color = bgColor;
        img.raycastTarget = true;

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        // Label
        var txtObj = new GameObject("Label");
        txtObj.transform.SetParent(obj.transform, false);
        var txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = new Vector2(8, 4);
        txtRect.offsetMax = new Vector2(-8, -4);
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        tmp.raycastTarget = false;

        return btn;
    }

    public static Button CreateBackButton(Transform parent, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject("BackButton");
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(12, -12);
        rect.sizeDelta = new Vector2(80, 36);

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.25f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.35f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.2f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        var txtObj = new GameObject("Label");
        txtObj.transform.SetParent(obj.transform, false);
        var txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "\u2190 Back";
        tmp.fontSize = 14;
        tmp.color = TextPrimary;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    public static (ScrollRect scroll, RectTransform content) CreateScrollList(Transform parent,
        string name, Vector2 position, Vector2 size)
    {
        // Scroll View
        var scrollObj = new GameObject(name);
        scrollObj.transform.SetParent(parent, false);
        var scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = position;
        scrollRect.sizeDelta = size;

        var scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = new Color(0.1f, 0.1f, 0.13f, 0.5f);

        scrollObj.AddComponent<RectMask2D>();

        var scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        // Content container
        var contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        var contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        var vlg = contentObj.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;

        var csf = contentObj.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;

        return (scroll, contentRect);
    }

    public static TMP_InputField CreateSearchField(Transform parent, string name,
        Vector2 position, Vector2 size, string placeholder = "Search...")
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        var img = obj.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.2f);

        // Text area
        var textArea = new GameObject("Text Area");
        textArea.transform.SetParent(obj.transform, false);
        var taRect = textArea.AddComponent<RectTransform>();
        taRect.anchorMin = Vector2.zero;
        taRect.anchorMax = Vector2.one;
        taRect.offsetMin = new Vector2(10, 2);
        taRect.offsetMax = new Vector2(-10, -2);

        // Placeholder
        var phObj = new GameObject("Placeholder");
        phObj.transform.SetParent(textArea.transform, false);
        var phRect = phObj.AddComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = Vector2.zero;
        phRect.offsetMax = Vector2.zero;
        var phTmp = phObj.AddComponent<TextMeshProUGUI>();
        phTmp.text = placeholder;
        phTmp.fontSize = 14;
        phTmp.color = TextMuted;
        phTmp.fontStyle = FontStyles.Italic;
        phTmp.alignment = TextAlignmentOptions.MidlineLeft;
        phTmp.raycastTarget = false;

        // Input text
        var txtObj = new GameObject("Text");
        txtObj.transform.SetParent(textArea.transform, false);
        var txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.offsetMin = Vector2.zero;
        txtRect.offsetMax = Vector2.zero;
        var txtTmp = txtObj.AddComponent<TextMeshProUGUI>();
        txtTmp.fontSize = 14;
        txtTmp.color = TextPrimary;
        txtTmp.alignment = TextAlignmentOptions.MidlineLeft;
        txtTmp.raycastTarget = false;

        var input = obj.AddComponent<TMP_InputField>();
        input.textViewport = taRect;
        input.textComponent = txtTmp;
        input.placeholder = phTmp;
        input.fontAsset = TMP_Settings.defaultFontAsset;
        input.pointSize = 14;

        return input;
    }

    public static GameObject CreateListItem(Transform parent, string text, string subtitle,
        Color accentColor, UnityEngine.Events.UnityAction onClick)
    {
        var obj = new GameObject("Item");
        obj.transform.SetParent(parent, false);

        var le = obj.AddComponent<LayoutElement>();
        le.minHeight = 52;
        le.preferredHeight = 52;

        var img = obj.AddComponent<Image>();
        img.color = BgCard;

        var btn = obj.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = BgCard;
        colors.highlightedColor = BgCardHover;
        colors.pressedColor = accentColor * 0.3f;
        colors.fadeDuration = 0.06f;
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        // Accent bar on left
        var bar = new GameObject("Accent");
        bar.transform.SetParent(obj.transform, false);
        var barRect = bar.AddComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 0);
        barRect.anchorMax = new Vector2(0, 1);
        barRect.pivot = new Vector2(0, 0.5f);
        barRect.anchoredPosition = Vector2.zero;
        barRect.sizeDelta = new Vector2(4, 0);
        var barImg = bar.AddComponent<Image>();
        barImg.color = accentColor;
        barImg.raycastTarget = false;

        // Main text
        var mainObj = new GameObject("Title");
        mainObj.transform.SetParent(obj.transform, false);
        var mainRect = mainObj.AddComponent<RectTransform>();
        mainRect.anchorMin = new Vector2(0, 0.5f);
        mainRect.anchorMax = new Vector2(1, 1);
        mainRect.offsetMin = new Vector2(14, 0);
        mainRect.offsetMax = new Vector2(-8, -4);
        var mainTmp = mainObj.AddComponent<TextMeshProUGUI>();
        mainTmp.text = text;
        mainTmp.fontSize = 14;
        mainTmp.color = TextPrimary;
        mainTmp.fontStyle = FontStyles.Bold;
        mainTmp.alignment = TextAlignmentOptions.BottomLeft;
        mainTmp.raycastTarget = false;

        // Subtitle
        var subObj = new GameObject("Subtitle");
        subObj.transform.SetParent(obj.transform, false);
        var subRect = subObj.AddComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0, 0);
        subRect.anchorMax = new Vector2(1, 0.5f);
        subRect.offsetMin = new Vector2(14, 4);
        subRect.offsetMax = new Vector2(-8, 0);
        var subTmp = subObj.AddComponent<TextMeshProUGUI>();
        subTmp.text = subtitle;
        subTmp.fontSize = 11;
        subTmp.color = TextSecondary;
        subTmp.alignment = TextAlignmentOptions.TopLeft;
        subTmp.raycastTarget = false;

        return obj;
    }
}
