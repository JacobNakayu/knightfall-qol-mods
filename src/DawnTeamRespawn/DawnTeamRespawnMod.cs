using DawnTeamRespawn;

using MelonLoader;

using UnityEngine;

using Il2Cpp;

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

    private bool _teamWasWiped;

    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("Dawn Team Respawn Loaded");
    }

    public override void OnUpdate()
    {
        Player localPlayer = Player.localplayer;
        if (localPlayer == null)
        {
            _teamWasWiped = false;
            _lastState = ReviveState.None;
            _ghostSince = -1;
            return;
        }
        if (!localPlayer.data.isGhost)
        {
            if (_teamWasWiped)
            {
                ReviveActions.ResetWipeSpeedBoost(localPlayer);
                _teamWasWiped = false;
            }
            _lastState = ReviveState.None;
            _ghostSince = -1;
            return;
        }

        if (_ghostSince < 0f) _ghostSince = Time.time;
        if (Time.time < _ghostSince + MinGhostTime) return; // Delay to prevent a weird ghost overlap if you die right next to your horse in town in the morning
        if (Time.time < _nextCheckTime) return;
        _nextCheckTime = Time.time + CheckInterval;

        ReviveState state = EligibilityChecker.GetState(localPlayer);
        if (state != ReviveState.None) _teamWasWiped = true;
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