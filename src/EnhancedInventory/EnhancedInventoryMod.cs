using EnhancedInventory;

using HarmonyLib;

using Il2Cpp;

using MelonLoader;

using UnityEngine;


[assembly: MelonInfo(typeof(EnhancedInventoryMod), "Enhanced Inventory", "0.1.0", "Jacob Nakayu")]
[assembly: MelonGame("Landfall Games", "Knightfall")]

namespace EnhancedInventory;

public class EnhancedInventoryMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("Enhanced Inventory Loaded");
    }

    public override void OnUpdate()
    {
        Player localPlayer = Player.localplayer;
        if (localPlayer == null) return;
        if (localPlayer.data.isGhost) return;

        float scroll = Input.mouseScrollDelta.y;
        if (scroll == 0f) return;

        int currentKey = (int)localPlayer.data.currentHeldObject - 1;
        int direction = scroll < 0f ? 1 : -1;

        int nextKey = ScrollSwitcher.GetNextSlot(currentKey, direction);
        ScrollSwitcher.RequestSwitch(localPlayer, nextKey);
    }
}

[HarmonyPatch(typeof(Pl_Items), nameof(Pl_Items.RPCA_SwitchItem))]
internal static class RPCA_SwitchItemPatch
{
    private static void Postfix(Pl_Items __instance, int key)
    {
        Player localPlayer = Player.localplayer;
        if (localPlayer == null) return;
        if (__instance != localPlayer.refs.items) return;

        Hotbar.Highlight(key);
    }
}