using Il2Cpp;

using Il2CppInterop.Runtime.InteropTypes.Arrays;

using Il2CppPhoton.Pun;

namespace EnhancedInventory;

internal static class ScrollSwitcher
{
    public static int GetNextSlot(int current, int direction)
    {
        int first = Settings.MinScrollSlot;
        int last = Settings.MaxScrollSlot;

        // Holding something outside the configured range (including nothing):
        // scrolling down enters at the first slot, scrolling up at the last.
        if (current < first || current > last)
        {
            return direction > 0 ? first : last;
        }

        int next = current + direction;
        if (next > last) next = first;
        if (next < first) next = last;
        return next;
    }

    public static void RequestSwitch(Player player, int key)
    {
        var args = new Il2CppReferenceArray<Il2CppSystem.Object>(1);
        args[0] = new Il2CppSystem.Int32 { m_value = key }.BoxIl2CppObject();
        player.refs.view.RPC("RPCA_SwitchItem", RpcTarget.All, args);
    }
}