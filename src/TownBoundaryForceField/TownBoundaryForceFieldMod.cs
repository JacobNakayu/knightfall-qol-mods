using MelonLoader;

using TownBoundaryForceField;

[assembly: MelonInfo(typeof(TownBoundaryForceFieldMod), "Town Boundary Force Field", "0.1.0", "Jacob Nakayu")]
[assembly: MelonGame("Landfall Games", "Knightfall")]

namespace TownBoundaryForceField;

public class TownBoundaryForceFieldMod : MelonMod
{
    public override void OnInitializeMelon()
    {
        LoggerInstance.Msg("Town Boundary Force Field Loaded");
    }

    public override void OnUpdate()
    {
        ForceFieldManager.Tick();
    }
}
