using ColonyManagerRedux;
using FarmingHysteresis.Extensions;
using LudeonTK;

namespace FarmingHysteresis.ColonyManagerRedux.Debug;

/// <summary>
/// Dev-mode diagnostics for why a clicked cell's grower is or isn't sowing/harvesting/cutting,
/// mirroring <c>ColonyManagerRedux.Managers.Dialog_MiningDebugOptions</c>'s
/// click-a-cell-get-a-message pattern.
/// </summary>
[HotSwappable]
internal sealed class Dialog_FarmingHysteresisDebugOptions(Manager manager)
    : Dialog_DebugOptionLister
{
    private readonly Manager manager = manager;

    protected override void DoListingItems(Rect inRect, float columnWidth)
    {
        DebugToolMap(
            "Hysteresis Cell Info",
            columnWidth,
            delegate
            {
                var cell = UI.MouseCell();
                foreach (var line in DescribeCell(manager, cell))
                {
                    Messages.Message(line, MessageTypeDefOf.SilentInput);
                }
            },
            false
        );

#if v1_5
        base.DoListingItems(inRect, columnWidth);
#endif
    }

    /// <summary>
    /// One message line per fact about <paramref name="cell"/> - split out as a pure function so
    /// the diagnostic content itself is unit-testable without a live map/UI.
    /// </summary>
    internal static List<string> DescribeCell(Manager manager, IntVec3 cell)
    {
        var map = manager.map;
        var lines = new List<string>();

        var grower = cell.GetPlantToGrowSettable(map);
        if (grower == null)
        {
            lines.Add($"{cell}: no plant-to-grow-settable here (not a growing zone or grower).");
            return lines;
        }

        lines.Add($"{cell}: grower = {ManagerJob_FarmingHysteresis.GrowerLabel(grower)}");

        var plant = cell.GetPlant(map);
        lines.Add(plant == null ? "Plant on cell: none" : $"Plant on cell: {plant.def.defName}");

        lines.Add($"Wanted plant def: {grower.GetPlantDefToGrow()?.defName ?? "none"}");
        lines.Add($"AllowSow: {grower.GetAllowSow()}");
        lines.Add($"AllowHarvest: {grower.GetAllowHarvest()}");

        var job = ManagerJob_FarmingHysteresis.FindOwningJob(manager, grower);
        if (job == null)
        {
            lines.Add("Owning CMR job: none");
            return lines;
        }

        lines.Add(
            $"Owning CMR job: {job.TargetsLabel} (IsManaged={job.IsManaged}, SwitchMode={job.SwitchMode})"
        );

        var targetPlantDef = job.TargetPlantDef;
        lines.Add($"Job's target plant def: {targetPlantDef?.defName ?? "none"}");
        lines.Add(
            $"Latch state: {Trigger_Hysteresis.DescribeLatchMode(job.HysteresisTrigger.LatchModeValue, job.HysteresisMode)}"
        );

        var hasLeftoverPlants =
            targetPlantDef != null
            && ManagerJob_FarmingHysteresis.GrowerHasLeftoverPlants(
                grower.Cells.Select(c => c.GetPlant(map)?.def),
                targetPlantDef,
                [.. job.RotationEntries.Select(e => e.PlantDef).OfType<ThingDef>()]
            );
        lines.Add($"Grower has leftover plants somewhere: {hasLeftoverPlants}");

        var isThisPlantALeftover =
            plant != null
            && targetPlantDef != null
            && ManagerJob_FarmingHysteresis.IsLeftoverPlant(
                plant.def,
                targetPlantDef,
                [.. job.RotationEntries.Select(e => e.PlantDef).OfType<ThingDef>()]
            );
        lines.Add($"This cell's plant is itself a leftover: {isThisPlantALeftover}");

        var protectedFromCut =
            plant != null
            && FarmingHysteresisMod.HysteresisController.ShouldProtectLeftoverFromCut(
                grower,
                plant
            );
        lines.Add($"ShouldProtectLeftoverFromCut for this plant: {protectedFromCut}");

        return lines;
    }
}
