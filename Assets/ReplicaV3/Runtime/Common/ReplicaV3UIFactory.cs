using UnityEngine;
using UnityEngine.UI;

public static class ReplicaV3UIFactory
{
    private static Font sCachedFont;

    public static Font DefaultFont
    {
        get
        {
            if (sCachedFont == null)
            {
                // Unity 6+ replacement for Arial
                try { sCachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (System.ArgumentException) { }

                // Fallback for older Unity versions
                if (sCachedFont == null)
                {
                    try { sCachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (System.ArgumentException) { }
                }
            }

            return sCachedFont;
        }
    }

    public static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    public static Image CreateImage(string name, Transform parent, Color color, bool raycastTarget = true)
    {
        var rect = CreateRect(name, parent);
        var image = EnsureComponent<Image>(rect.gameObject);
        image.color = color;
        image.raycastTarget = raycastTarget;
        return image;
    }

    public static Text CreateText(
        string name,
        Transform parent,
        string content,
        int fontSize,
        TextAnchor alignment,
        Color color,
        FontStyle fontStyle = FontStyle.Normal,
        bool raycastTarget = false)
    {
        var rect = CreateRect(name, parent);
        var text = EnsureComponent<Text>(rect.gameObject);
        text.font = DefaultFont;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.fontStyle = fontStyle;
        text.raycastTarget = raycastTarget;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    public static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color backgroundColor,
        Color labelColor,
        out Text labelText)
    {
        var image = CreateImage(name, parent, backgroundColor, true);
        var button = EnsureComponent<Button>(image.gameObject);
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.14f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.45f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        labelText = CreateText("Label", image.transform, label, 22, TextAnchor.MiddleCenter, labelColor, FontStyle.Bold);
        Stretch((RectTransform)labelText.transform);
        return button;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static T EnsureComponent<T>(GameObject go) where T : Component
    {
        if (!go.TryGetComponent<T>(out var component))
        {
            component = go.AddComponent<T>();
        }

        return component;
    }
}
