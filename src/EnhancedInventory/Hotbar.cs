using MelonLoader;

using UnityEngine;

namespace EnhancedInventory;

internal static class Hotbar
{
    private const string InventoryPath = "Game/Canvas/Inventory";
    private static readonly string[] SlotNames = { "Weapon", "Bomb", "Bandage", "Map", "Trumpet", "Flag" };

    private static HotbarSlot[] _slots;

    public static void Highlight(int key)
    {
        if (!EnsureSlots()) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (i == key)
            {
                _slots[i].Highlight();
            }
            else
            {
                _slots[i].Restore();
            }
        }
    }

    private static bool EnsureSlots()
    {
        if (_slots != null && _slots[0].Root != null) return true;

        GameObject inventory = GameObject.Find(InventoryPath);
        if (inventory == null) return false;

        var slots = new HotbarSlot[SlotNames.Length];
        for (int i = 0; i < SlotNames.Length; i++)
        {
            Transform child = inventory.transform.Find(SlotNames[i]);
            if (child == null)
            {
                MelonLogger.Warning($"Hotbar slot '{SlotNames[i]}' not found.");
                return false;
            }
            slots[i] = new HotbarSlot(child.gameObject);
        }

        _slots = slots;
        return true;
    }
}