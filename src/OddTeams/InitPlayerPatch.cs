using HarmonyLib;

using Il2Cpp;

namespace OddTeams;

/// <summary>
/// InitPlayer(i, j, team) sets up player j of team i and sends RPCA_InitPlayer to everyone,
/// using i as the spawn camp and the team's ID as the team. Changing the arguments here
/// moves the second player of a pair into a new solo team for every client at once.
/// </summary>
[HarmonyPatch(typeof(GM_Adventure), nameof(GM_Adventure.InitPlayer))]
internal static class InitPlayerPatch
{
    private static void Prefix(ref int i, ref int j, ref Team currTeam)
    {
        if (!StartGamePatch.SplitActive) return;

        // The first player of each team stays; only later players are moved out.
        if (j == 0) return;
        if (currTeam == null || currTeam.Players == null || j >= currTeam.Players.Count) return;

        // Read the player before replacing the team they're found in.
        Player player = currTeam.Players[j];
        StartGamePatch.MovedPlayers.Add((currTeam, player));

        // A view ID is unique per player and far smaller than the game's own team IDs.
        ulong newTeamId = (ulong)player.refs.view.ViewID;

        currTeam = new Team(newTeamId, player);
        i = StartGamePatch.NextFreeTeamIndex;
        StartGamePatch.NextFreeTeamIndex++;
        j = 0;
    }
}