using ColonyManagerRedux;

namespace FarmingHysteresis.ColonyManagerRedux;

/// <summary>
/// Step 1 skeleton tab; relies entirely on the base <c>ManagerTab</c>'s default job
/// list/creation UI. A real editor for scope/bounds lands in Step 2.
/// </summary>
internal sealed class ManagerTab_FarmingHysteresis(Manager manager)
    : ManagerTab<ManagerJob_FarmingHysteresis, ManagerSettings_FarmingHysteresis>(manager);
