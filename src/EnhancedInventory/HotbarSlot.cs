using Il2CppTMPro;

using UnityEngine;
using UnityEngine.UI;

namespace EnhancedInventory;

internal class HotbarSlot
{
    public GameObject Root { get; }

    // Inventory slot icon components
    private readonly Image _background;
    private readonly CanvasRenderer _renderer;
    private readonly Image _icon;
    private readonly TextMeshProUGUI _key;
    private readonly TextMeshProUGUI _amount;

    // Inventory colors
    private readonly Color _backgroundOriginal;
    private readonly Color _rendererOriginal;
    private readonly Color _iconOriginal;
    private readonly Color _keyOriginal;
    private readonly Color _amountOriginal;

    public HotbarSlot(GameObject root)
    {
        Root = root;

        _background = root.GetComponent<Image>();
        _renderer = root.GetComponent<CanvasRenderer>();
        _icon = FindPart<Image>("Image");
        _key = FindPart<TextMeshProUGUI>("Key");
        _amount = FindPart<TextMeshProUGUI>("Amount");

        if (_background != null) _backgroundOriginal = _background.color;
        if (_renderer != null) _rendererOriginal = _renderer.GetColor();
        if (_icon != null) _iconOriginal = _icon.color;
        if (_key != null) _keyOriginal = _key.color;
        if (_amount != null) _amountOriginal = _amount.color;
    }

    public void Highlight()
    {
        SetColor(_background, Color.white);
        if (_renderer != null) _renderer.SetColor(Color.white);
        SetColor(_icon, Color.black);
        SetColor(_key, Color.black);
        SetColor(_amount, Color.black);
    }

    public void Restore()
    {
        SetColor(_background, _backgroundOriginal);
        if (_renderer != null) _renderer.SetColor(_rendererOriginal);
        SetColor(_icon, _iconOriginal);
        SetColor(_key, _keyOriginal);
        SetColor(_amount, _amountOriginal);
    }

    private T FindPart<T>(string childName) where T : Component
    {
        Transform child = Root.transform.Find(childName);
        if (child == null) return null;
        return child.GetComponent<T>();
    }

    private static void SetColor(Graphic graphic, Color color)
    {
        if (graphic != null) graphic.color = color;
    }
}