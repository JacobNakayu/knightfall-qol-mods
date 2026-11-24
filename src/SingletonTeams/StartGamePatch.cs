using HarmonyLib;

using Il2Cpp;

using Il2CppPhoton.Pun;

using MelonLoader;

namespace SingletonTeams;

/// <summary>
/// With an odd number of players or a "small lobby", everyone plays solo instead of one player
/// being left alone. GM_Adventure.StartGame (host only) pairs players up internally
/// and then calls InitPlayer for each player; InitPlayerPatch moves the second player
/// of each pair into a new team, and the postfix here removes them from the pair.
/// </summary>
[HarmonyPatch(typeof(GM_Adventure), nameof(GM_Adventure.StartGame))]
internal static class StartGamePatch
{
    // One team per spawn camp, and the map has 14 camps.
    private const int MaxTeams = Settings.MaxTeams;

    // Set up in the Prefix and used by InitPlayerPatch while StartGame runs.
    public static bool SplitActive;
    public static int NextFreeTeamIndex;

    // Players InitPlayerPatch moved out, and the team they were moved out of.
    public static readonly List<(Team OriginalTeam, Player Player)> MovedPlayers = new();

    private static void Prefix()
    {
        SplitActive = false;
        MovedPlayers.Clear();

        // Only the host decides teams; everyone else receives them through RPCA_InitPlayer.
        if (!PhotonNetwork.IsMasterClient) return;

        PlayerHandler handler = PlayerHandler.instance;
        if (handler == null) return;

        int playerCount = handler.CurrentPlayers.Count;
        bool isOdd = playerCount % 2 == 1;
        bool isSmallLobby = playerCount < Settings.MinPlayersForPairs;

        if (playerCount == 1) return;
        if (!isOdd && !isSmallLobby) return;

        // All-solo needs one team, and one spawn camp, per player.
        if (playerCount > MaxTeams)
        {
            MelonLogger.Warning($"{playerCount} players is more than the {MaxTeams} spawn camps; keeping the normal teams.");
            return;
        }

        // The game makes (players + 1) / 2 teams numbered from 0,
        // so the first unused team number is exactly that count.
        SplitActive = true;
        NextFreeTeamIndex = (playerCount + 1) / 2;
        MelonLogger.Msg($"{playerCount} players: everyone plays solo.");
    }

    private static void Postfix()
    {
        // StartGame built the pairs directly in the host's teams, so a moved player is
        // still listed in their original team until they're removed here. Doing this after
        // StartGame finishes avoids changing the list while the game is looping over it.
        foreach (var (originalTeam, player) in MovedPlayers)
        {
            if (!originalTeam.Players.Remove(player))
            {
                MelonLogger.Warning($"Could not remove {player.data.playerName} from their original team.");
            }
        }

        MovedPlayers.Clear();
        SplitActive = false;
    }
}