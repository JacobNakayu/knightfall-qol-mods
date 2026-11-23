using MelonLoader;

using OddTeams;

[assembly: MelonInfo(typeof(OddTeamsMod), "Odd Teams", "0.1.0", "Jacob Nakayu")]
[assembly: MelonGame("Landfall Games", "Knightfall")]

namespace OddTeams;

public class OddTeamsMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("Odd Teams Loaded");
    }
}