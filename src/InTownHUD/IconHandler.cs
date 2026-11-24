using System.Reflection;

using Il2Cpp;

using Il2CppTMPro;

using MelonLoader;

using UnityEngine;
using UnityEngine.UI;

namespace InTownHUD;

internal static class IconHandler
{
    private const string CanvasPath = "Game/Canvas";
    private const string IconResource = "InTownHUD.houses.png";
    private const float IconSize = 48f;
    private const float Spacing = 8f;

    private static GameObject _root;
    private static Sprite _sprite;

    private static Image _image;
    private static Outline _iconOutline;
    private static TextMeshProUGUI _label;

    private static bool _visible;
    private static bool _isNight;
    private static bool _hasColor;

    private static string _currentFarmName = "CAMP";

    public static bool HasColor => _root != null && _hasColor;

    public static void SetVisible(bool visible)
    {
        if (_root != null && visible == _visible) return;
        if (!EnsureIcon()) return;
        _root.SetActive(visible);
        _visible = visible;
    }

    public static void SetDaySegment(GM_Adventure.DaySegment segment)
    {
        SetNight(segment is GM_Adventure.DaySegment.Evening or GM_Adventure.DaySegment.Night);
    }

    public static void SetCurrentFarmName(Farm farm)
    {
        string name = farm.townName;
        _currentFarmName = string.IsNullOrEmpty(name) ? "CAMP" : farm.townName.ToUpper();
        if (_label != null) _label.text = $"IN {_currentFarmName}";
    }

    private static void SetNight(bool isNight)
    {
        if (_root != null && _hasColor && isNight == _isNight) return;
        if (!EnsureIcon()) return;

        Color main = isNight ? Color.white : Color.black;
        Color edge = isNight ? Color.black : Color.white;

        _image.color = main;
        _iconOutline.effectColor = edge;
        _label.color = main;
        _label.outlineColor = edge;

        _isNight = isNight;
        _hasColor = true;
    }

    private static bool EnsureIcon()
    {
        if (_root != null) return true;

        GameObject canvas = GameObject.Find(CanvasPath);
        if (canvas == null) return false;

        HeaderHandler headers = HeaderHandler.instance;
        if (headers == null || headers.headerText == null) return false;

        if (!EnsureSprite()) return false;

        // Container: pinned to the top-left corner; children are placed inside it
        _root = new("InTownHUD");
        _root.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = _root.AddComponent<RectTransform>();
        rootRect.anchorMin = rootRect.anchorMax = rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(20f, -20f);
        rootRect.sizeDelta = new Vector2(250f, IconSize);

        CreateIcon(_root.transform);
        CreateLabel(_root.transform, headers.headerText.font);

        _root.SetActive(false);
        _visible = false;
        _hasColor = false;
        return true;
    }

    private static void CreateIcon(Transform parent)
    {
        GameObject go = new("Icon");
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(IconSize, IconSize);

        Image image = go.AddComponent<Image>();
        image.sprite = _sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        _iconOutline = go.AddComponent<Outline>();
        _iconOutline.effectDistance = new Vector2(1f, 1f);

        _image = image;
    }

    private static void CreateLabel(Transform parent, TMP_FontAsset font)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);

        // Stretch to fill the container, minus the space the icon takes on the left
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(IconSize + Spacing, 0f);
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = $"IN {_currentFarmName}";
        label.fontSize = 28;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft; // vertically centered next to the icon
        label.raycastTarget = false;

        label.enableAutoSizing = true;
        label.fontSizeMin = 14;
        label.fontSizeMax = 28;

        label.enableWordWrapping = false;

        _ = label.fontMaterial;
        label.outlineWidth = 0.1f;

        _label = label;
    }

    private static bool EnsureSprite()
    {
        if (_sprite != null) return true;

        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(IconResource);
        if (stream == null)
        {
            MelonLogger.Error($"Embedded resource '{IconResource}' not found.");
            return false;
        }

        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        byte[] bytes = buffer.ToArray();

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, true); // LoadImage resizes it to the PNG's real size
        if (!ImageConversion.LoadImage(texture, bytes))
        {
            MelonLogger.Error("Failed to decode icon PNG.");
            return false;
        }
        texture.hideFlags = HideFlags.DontUnloadUnusedAsset;

        _sprite = Sprite.Create(texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f));
        _sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return true;
    }
}