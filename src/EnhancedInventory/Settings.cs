using MelonLoader;
using MelonLoader.Utils;

namespace EnhancedInventory;

internal static class Settings
{
    private static MelonPreferences_Category _category = null!;

    private static MelonPreferences_Entry<bool> _scrollAllInventoryItems = null!;

    private const int LastHotbarSlot = 5;

    public static int MinScrollSlot => 0;
    public static int MaxScrollSlot => _scrollAllInventoryItems.Value ? LastHotbarSlot : 2;
    public static bool ScrollAllInventoryItems
    {
        get => _scrollAllInventoryItems.Value;
        set => _scrollAllInventoryItems.Value = value;
    }

    public static void Init()
    {
        _category = MelonPreferences.CreateCategory("EnhancedInventory");
        _category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "EnhancedInventory.cfg"));

        _scrollAllInventoryItems = _category.CreateEntry("ScrollAllInventoryItems", false,
            description: "Cycle all six inventory items instead of only Weapon, Bomb, and Bandage.");

        // Writes the file with defaults on first run so there's something to edit
        _category.SaveToFile(false);
    }

    public static void Save() => _category.SaveToFile(false);
}
