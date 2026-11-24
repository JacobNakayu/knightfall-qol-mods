using Il2Cpp;

using Il2CppInterop.Runtime.InteropTypes.Arrays;

using Il2CppPhoton.Pun;

namespace EnhancedInventory;

internal static class ScrollSwitcher
{
    // Only allow scroll select for Weapon, Bomb, and Bandage (the first three items)
    private const int FirstSlot = 0;
    private const int LastSlot = 2;

    public static int GetNextSlot(int current, int direction)
    {
        // Holding something outside the scroll range (nothing, map, trumpet, flag):
        // scrolling down enters at the first slot, scrolling up at the last.
        if (current < FirstSlot || current > LastSlot)
        {
            return direction > 0 ? FirstSlot : LastSlot;
        }

        int next = current + direction;
        if (next > LastSlot) next = FirstSlot;
        if (next < FirstSlot) next = LastSlot;
        return next;
    }


    public static void RequestSwitch(Player player, int key)
    {
        var args = new Il2CppReferenceArray<Il2CppSystem.Object>(1);
        args[0] = new Il2CppSystem.Int32 { m_value = key }.BoxIl2CppObject();
        player.refs.view.RPC("RPCA_SwitchItem", RpcTarget.All, args);
    }
}