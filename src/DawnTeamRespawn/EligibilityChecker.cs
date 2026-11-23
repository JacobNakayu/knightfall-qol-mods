
using Il2Cpp;

using UnityEngine;

namespace DawnTeamRespawn;

internal static class EligibilityChecker
{
    private const float MaxHorseDistance = 8f;
    private static Farm[] _farms;

    public static ReviveState GetState(Player player)
    {
        if (!TeamDead(player)) return ReviveState.None;
        if (!HorseInTown(player)) return ReviveState.None;

        if (!IsDaySegment(GM_Adventure.DaySegment.Morning)) return ReviveState.WaitingForDawn;
        if (!InTown(player)) return ReviveState.NotInTown;
        if (!NearHorse(player)) return ReviveState.TooFarFromHorse;

        return ReviveState.Eligible;
    }

    private static bool TeamDead(Player player)
    {
        if (!player.data.isGhost) return false;

        PlayerHandler handler = PlayerHandler.instance;
        if (handler == null) return false;

        return handler.CheckPlayersAlive(player.TeamID, out _) == 0;
    }

    private static bool IsDaySegment(GM_Adventure.DaySegment segment)
    {
        GM_Adventure game = GM_Adventure.instance;
        return game != null && game.daySegment == segment;
    }

    private static bool InTown(Player player)
    {
        return IsInOpenTown(player.transform.position);
    }

    private static bool NearHorse(Player player)
    {
        Horse horse = player.data.belovedHorse;
        if (horse == null) return false;

        float distance = Vector3.Distance(player.transform.position, horse.transform.position);
        return distance <= MaxHorseDistance;
    }

    private static bool HorseInTown(Player player)
    {
        Horse horse = player.data.belovedHorse;
        if (horse == null) return false;

        return IsInOpenTown(horse.transform.position);
    }

    private static bool IsInOpenTown(Vector3 position)
    {
        if (!EnsureFarms()) return false;

        foreach (Farm farm in _farms)
        {
            Collider area = farm.col;
            if (area == null || !area.enabled) continue;

            Bounds bounds = area.bounds;
            if (Vector3.Distance(position, bounds.center) <= bounds.extents.x) return true;
        }
        return false;
    }


    private static bool EnsureFarms()
    {
        if (_farms != null && _farms.Length > 0 && _farms[0] != null) return true;

        _farms = UnityEngine.Object.FindObjectsOfType<Farm>().ToArray();
        return _farms.Length > 0;
    }

}