namespace DawnTeamRespawn;

internal enum ReviveState
{
    None,               // nothing to show: alive, teammate alive, or horse not in any open town
    WaitingForDawn,     // team wiped, horse in a town, but it isn't morning
    NotInTown,          // it's morning, but you aren't in a town
    TooFarFromHorse,    // it's morning and you're in town, but too far from your horse
    Eligible            // it's morning, you're in town and near your horse: revive
}