using MelonLoader;
using MelonLoader.Utils;

using UnityEngine;

namespace EnhancedInventory;

internal static class Settings
{
    private static MelonPreferences_Category _category;

    private static MelonPreferences_Entry<int> _minScrollSlot;
    private static MelonPreferences_Entry<int> _maxScrollSlot;

    private const int LastHotbarSlot = 5;

    public static int MinScrollSlot => Mathf.Clamp(Mathf.Min(_minScrollSlot.Value, _maxScrollSlot.Value), 0, LastHotbarSlot);
    public static int MaxScrollSlot => Mathf.Clamp(Mathf.Max(_minScrollSlot.Value, _maxScrollSlot.Value), 0, LastHotbarSlot);

    // Hotbar slot numbers, matching Hotbar.SlotNames:
    // 0 = Weapon, 1 = Bomb, 2 = Bandage, 3 = Map, 4 = Trumpet, 5 = Flag
    private const string SlotHelp = "0 = Weapon, 1 = Bomb, 2 = Bandage, 3 = Map, 4 = Trumpet, 5 = Flag";

    public static void Init()
    {
        _category = MelonPreferences.CreateCategory("EnhancedInventory");
        _category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "EnhancedInventory.cfg"));

        _minScrollSlot = _category.CreateEntry("MinScrollSlot", 0,
            description: $"First slot the scroll wheel cycles through ({SlotHelp})");
        _maxScrollSlot = _category.CreateEntry("MaxScrollSlot", 2,
            description: $"Last slot the scroll wheel cycles through ({SlotHelp})");

        // Writes the file with defaults on first run so there's something to edit
        _category.SaveToFile(false);
    }
}
