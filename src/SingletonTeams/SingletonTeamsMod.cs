using MelonLoader;

using SingletonTeams;

[assembly: MelonInfo(typeof(SingletonTeamsMod), "Singleton Teams", "0.1.0", "Jacob Nakayu")]
[assembly: MelonGame("Landfall Games", "Knightfall")]

namespace SingletonTeams;

public class SingletonTeamsMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        Settings.Init();
        LoggerInstance.Msg("Singleton Teams Loaded");
    }
}