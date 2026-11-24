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

        IconHandler.SetVisible(localPlayer.data.inFarm);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.EnterFarm))]
internal static class GetLocalFarmName
{
    private static void Postfix(Player __instance, Farm farm)
    {
        if (__instance != Player.localplayer) return;
        IconHandler.SetCurrentFarmName(farm);
    }
}