using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Helpers for building uGUI hierarchies from code, plus wiring private [SerializeField]s by name.
public static class UIKit
{
    public static readonly Color PanelColor = new Color(.05f, .03f, .08f, .9f);
    public static readonly Color AccentColor = new Color(.55f, .18f, .32f, 1f);
    public static readonly Color TextColor = new Color(.93f, .9f, .95f, 1f);
    public static readonly Color MutedTextColor = new Color(.62f, .58f, .68f, 1f);

    public static Sprite RoundedSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    public static Sprite KnobSprite => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

    public static GameObject Create(string name, Transform parent, params Type[] components)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = LayerMask.NameToLayer("UI");

        foreach (Type component in components)
            go.AddComponent(component);

        return go;
    }

    public static void ConfigureCanvas(GameObject go, int sortingOrder)
    {
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
    }

    public static RectTransform Stretch(GameObject go, float left = 0, float bottom = 0, float right = 0, float top = 0)
    {
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        return rect;
    }

    public static RectTransform Place(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static Image Panel(string name, Transform parent, Color color, bool rounded = false)
    {
        Image image = Create(name, parent, typeof(Image)).GetComponent<Image>();
        image.color = color;

        if (rounded)
        {
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, string text, float size,
        TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft, Color? color = null, FontStyles style = FontStyles.Normal)
    {
        TextMeshProUGUI tmp = Create(name, parent, typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = alignment;
        tmp.color = color ?? TextColor;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Every button gets the menu hover and click sounds (UI_ButtonSound)
    public static Button Button(string name, Transform parent, string label, float fontSize, Vector2 size, bool flat = false)
    {
        GameObject go = Create(name, parent, typeof(Image), typeof(Button), typeof(LayoutElement), typeof(UI_ButtonSound));
        Image image = go.GetComponent<Image>();
        Button button = go.GetComponent<Button>();

        if (flat)
        {
            image.color = new Color(0, 0, 0, 0); // Invisible, but still catches clicks
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = Text("Label", go.transform, label, fontSize, TextAlignmentOptions.Center, TextColor);
        }
        else
        {
            image.sprite = RoundedSprite;
            image.type = Image.Type.Sliced;
            button.targetGraphic = image;
            button.colors = ButtonColors();
            Text("Label", go.transform, label, fontSize, TextAlignmentOptions.Center, TextColor);
        }

        Stretch(go.transform.Find("Label").gameObject);

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        ((RectTransform)go.transform).sizeDelta = size;

        return button;
    }

    // A 0-1 slider with the parts of Unity's default one: a track, an accent fill up to the value and a round handle
    public static Slider Slider(string name, Transform parent, Vector2 size, float value)
    {
        GameObject go = Create(name, parent, typeof(Slider), typeof(LayoutElement));
        float handleSize = size.y * .7f;
        float trackInset = size.y * .3f;

        Image track = Panel("Background", go.transform, new Color(.18f, .12f, .22f, 1f), rounded: true);
        Stretch(track.gameObject, 0, trackInset, 0, trackInset);

        // The slider moves the fill's right edge to the handle's centre
        GameObject fillArea = Create("Fill Area", go.transform);
        Stretch(fillArea, handleSize * .5f, trackInset, handleSize * .5f, trackInset);
        Image fill = Panel("Fill", fillArea.transform, AccentColor, rounded: true);
        RectTransform fillRect = (RectTransform)fill.transform;
        fillRect.offsetMin = new Vector2(-handleSize * .5f, 0);
        fillRect.offsetMax = Vector2.zero;

        GameObject handleArea = Create("Handle Slide Area", go.transform);
        Stretch(handleArea, handleSize * .5f, 0, handleSize * .5f, 0);
        Image handle = Create("Handle", handleArea.transform, typeof(Image)).GetComponent<Image>();
        handle.sprite = KnobSprite;
        ((RectTransform)handle.transform).sizeDelta = new Vector2(handleSize, handleSize - size.y);

        Slider slider = go.GetComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = (RectTransform)handle.transform;
        slider.targetGraphic = handle;
        slider.colors = HandleColors();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = value;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        ((RectTransform)go.transform).sizeDelta = size;

        return slider;
    }

    // An on/off switch that looks like a button: the accent fill shows while it's on. The menu that owns it sets the label.
    public static Toggle ToggleButton(string name, Transform parent, string label, Vector2 size)
    {
        GameObject go = Create(name, parent, typeof(Image), typeof(Toggle), typeof(LayoutElement), typeof(UI_ButtonSound));
        Image frame = go.GetComponent<Image>();
        frame.sprite = RoundedSprite;
        frame.type = Image.Type.Sliced;

        // Inset, so the frame around it still lights up on hover
        Image fill = Panel("On", go.transform, AccentColor, rounded: true);
        Stretch(fill.gameObject, 4, 4, 4, 4);
        fill.raycastTarget = false;

        TextMeshProUGUI text = Text("Label", go.transform, label, 26, TextAlignmentOptions.Center, TextColor, FontStyles.Bold);
        Stretch(text.gameObject);

        Toggle toggle = go.GetComponent<Toggle>();
        toggle.targetGraphic = frame;
        toggle.graphic = fill;
        toggle.colors = ButtonColors();
        toggle.isOn = true;

        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        ((RectTransform)go.transform).sizeDelta = size;

        return toggle;
    }

    public static ColorBlock HandleColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = TextColor;
        colors.highlightedColor = new Color(1f, .78f, .88f, 1f);
        colors.selectedColor = new Color(1f, .78f, .88f, 1f);
        colors.pressedColor = new Color(1f, .6f, .76f, 1f);
        colors.disabledColor = new Color(.45f, .42f, .48f, .8f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = .08f;
        return colors;
    }

    public static ColorBlock ButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(.13f, .08f, .17f, .95f);
        colors.highlightedColor = new Color(.42f, .16f, .3f, 1f);
        colors.selectedColor = new Color(.42f, .16f, .3f, 1f);
        colors.pressedColor = new Color(.6f, .22f, .4f, 1f);
        colors.disabledColor = new Color(.1f, .09f, .11f, .6f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = .08f;
        return colors;
    }

    public static void NoNavigation(Selectable selectable)
    {
        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.None;
        selectable.navigation = navigation;
    }

    public static VerticalLayoutGroup Vertical(GameObject go, float spacing, TextAnchor alignment, RectOffset padding = null)
    {
        VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(go);
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = padding ?? new RectOffset();
        return layout;
    }

    public static HorizontalLayoutGroup Horizontal(GameObject go, float spacing, TextAnchor alignment)
    {
        HorizontalLayoutGroup layout = GetOrAdd<HorizontalLayoutGroup>(go);
        layout.spacing = spacing;
        layout.childAlignment = alignment;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        return layout;
    }

    public static void FitVertically(GameObject go)
    {
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(go);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    // Not "GetComponent() ?? AddComponent()": in the editor a missing component is a fake null that ?? doesn't catch
    public static T GetOrAdd<T>(GameObject go) where T : Component =>
        go.TryGetComponent(out T component) ? component : go.AddComponent<T>();

    // Sets a (possibly private) serialized field by name. Throws on typos so a broken prefab never goes unnoticed.
    public static void Wire(Object target, string field, Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        Property(serialized, field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void WireArray(Object target, string field, params Object[] values)
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = Property(serialized, field);
        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void WireString(Object target, string field, string value)
    {
        SerializedObject serialized = new SerializedObject(target);
        Property(serialized, field).stringValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void WireBool(Object target, string field, bool value)
    {
        SerializedObject serialized = new SerializedObject(target);
        Property(serialized, field).boolValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static SerializedProperty Property(SerializedObject serialized, string field)
    {
        SerializedProperty property = serialized.FindProperty(field);

        if (property == null)
            throw new ArgumentException($"{serialized.targetObject.GetType().Name} has no serialized field '{field}'.");

        return property;
    }
}
