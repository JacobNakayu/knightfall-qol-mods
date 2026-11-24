using DawnTeamRespawn;

using HarmonyLib;

using Il2Cpp;

using MelonLoader;

using UnityEngine;

[assembly: MelonInfo(typeof(DawnTeamRespawnMod), "Dawn Team Respawn", "0.1.0", "Jacob Nakayu")]
[assembly: MelonGame("Landfall Games", "Knightfall")]

namespace DawnTeamRespawn;

public class DawnTeamRespawnMod : MelonMod
{
    private const float CheckInterval = 0.25f;
    private const float FlashInterval = 0.5f;
    private const float MinGhostTime = 2f;

    private ReviveState _lastState = ReviveState.None;
    private float _nextCheckTime;
    private float _nextFlashTime;
    private float _ghostSince = -1f; // -1 means you're not a ghost

    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("Dawn Team Respawn Loaded");
    }

    public override void OnUpdate()
    {
        Player localPlayer = Player.localplayer;
        if (localPlayer == null || !localPlayer.data.isGhost)
        {
            _lastState = ReviveState.None;
            _ghostSince = -1;
            return;
        }

        if (_ghostSince < 0f) _ghostSince = Time.time;
        if (Time.time < _ghostSince + MinGhostTime) return; // Delay to prevent a weird ghost overlap if you die right next to your horse in town in the morning
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + CheckInterval;

        ReviveState state = EligibilityChecker.GetState(localPlayer);
        if (state != _lastState)
        {
            LoggerInstance.Msg($"State: {_lastState} => {state}");
            OnStateChanged(localPlayer, _lastState, state);
            _lastState = state;
        }

        if (NeedsToReachHorse(state) && Time.time >= _nextFlashTime)
        {
            ReviveActions.FlashGetToHorse();
            _nextFlashTime = Time.time + FlashInterval;
        }
    }

    private void OnStateChanged(Player player, ReviveState from, ReviveState to)
    {
        if (from == ReviveState.None && to != ReviveState.None)
        {
            ReviveActions.ShowBanner("YOUR TEAM HAS FALLEN", "Your horse can revive you at dawn");
        }
        else if (NeedsToReachHorse(from) && to == ReviveState.WaitingForDawn)
        {
            ReviveActions.ShowBanner("YOU MISSED THE DAWN", "Your horse can revive you tomorrow");
        }

        if (to == ReviveState.Eligible)
        {
            ReviveActions.Revive(player);
        }
    }

    private static bool NeedsToReachHorse(ReviveState state)
    {
        return state is ReviveState.NotInTown or ReviveState.TooFarFromHorse;
    }
}

[HarmonyPatch(typeof(Pl_Revive), nameof(Pl_Revive.RPCA_Revive))]
internal static class ReviveSpeedResetPatch
{
    // Players (on this client) who still carry the wipe boost
    private static readonly HashSet<int> Boosted = new();

    private static void Prefix(Pl_Revive __instance)
    {
        Player player = __instance.player;
        PlayerHandler handler = PlayerHandler.instance;
        if (player == null || handler == null) return;
        if (handler.CheckPlayersAlive(player.TeamID, out _) != 0) return;

        // Whole team is down: every member got the wipe boost
        foreach (Player p in handler.CurrentPlayers)
        {
            if (p != null && p.TeamID == player.TeamID) Boosted.Add(p.GetInstanceID());
        }
    }

    // Runs on every client: resets whoever was revived, if they were boosted, however they were revived
    private static void Postfix(Pl_Revive __instance)
    {
        Player player = __instance.player;
        if (player == null || !Boosted.Remove(player.GetInstanceID())) return;

        ReviveActions.ResetWipeSpeedBoost(player);
    }
}