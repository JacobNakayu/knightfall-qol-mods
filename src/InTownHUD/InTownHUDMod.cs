using HarmonyLib;

using Il2Cpp;

using InTownHUD;

using MelonLoader;

using UnityEngine;

[assembly: MelonInfo(typeof(InTownHUDMod), "In-Town HUD", "0.1.0", "Jacob Nakayu")]
[assembly: MelonGame("Landfall Games", "Knightfall")]

namespace InTownHUD;

public class InTownHUDMod : MelonMod
{
    private const float CheckInterval = 0.25f;

    private float _nextCheckTime;
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("In-Town HUD Loaded");
    }

    public override void OnUpdate()
    {
        // Throttle the check to make this mod cheaper to run
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + CheckInterval;

        GM_Adventure game = GM_Adventure.instance;
        Player localPlayer = Player.localplayer;
        if (game == null || localPlayer == null)
        {
            IconHandler.SetVisible(false);
            return;
        }

        if (!IconHandler.HasColor) IconHandler.SetDaySegment(game.daySegment);
        IconHandler.SetVisible(localPlayer.data.inFarm);
    }


}

[HarmonyPatch(typeof(GM_Adventure), nameof(GM_Adventure.EnterDaySegment))]
internal static class DaySegmentPatch
{
    private static void Postfix(int newDaySeg)
    {
        var seg = (GM_Adventure.DaySegment)newDaySeg;
        IconHandler.SetDaySegment(seg);
    }
}