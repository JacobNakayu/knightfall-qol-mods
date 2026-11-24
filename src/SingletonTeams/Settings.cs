using MelonLoader;
using MelonLoader.Utils;

using UnityEngine;

namespace SingletonTeams;

internal static class Settings
{
    private static MelonPreferences_Category _category;

    private static MelonPreferences_Entry<int> _minPlayersForPairs;

    internal const int MaxTeams = 14;

    public static int MinPlayersForPairs => Mathf.Clamp(_minPlayersForPairs.Value, 0, MaxTeams + 1);

    public static void Init()
    {
        _category = MelonPreferences.CreateCategory("SingletonTeams");
        _category.SetFilePath(Path.Combine(MelonEnvironment.UserDataDirectory, "SingletonTeams.cfg"));

        _minPlayersForPairs = _category.CreateEntry("MinPlayersForPairs", 8,
            description: "Lobbies with fewer players than this play solo. 15 = always solo when there are enough spawn camps. Odd-sized lobbys under 14 players always play solo, and only the host's configuration is accounted for.");

        // Writes the file with defaults on first run so there's something to edit
        _category.SaveToFile(false);
    }
}
