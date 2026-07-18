using ColonyManagerRedux;
using FarmingHysteresis.Defs;
using ilyvion.Laboratory.Extensions;
using static ColonyManagerRedux.Constants;

namespace FarmingHysteresis.ColonyManagerRedux;

internal sealed class ManagerTab_FarmingHysteresis(Manager manager)
    : ManagerTab<ManagerJob_FarmingHysteresis, ManagerSettings_FarmingHysteresis>(manager)
{
    protected override void DoMainContent(Rect rect)
    {
        Widgets.DrawMenuSection(rect);

        Widgets_Section.BeginSectionColumn(rect, "FarmingHysteresis.Job", out var position, out var width);
        DrawSection(
            "FarmingHysteresis.Job",
            "GrowerScope",
            ref position,
            width,
            DrawGrowerScope,
            "FarmingHysteresis.CMR.GrowerScope".Translate()
        );
        Widgets_Section.EndSectionColumn("FarmingHysteresis.Job", position);
    }

    private static void DrawAssignmentModeSelector(
        ManagerJob_FarmingHysteresis job,
        ref Vector2 pos,
        float width
    )
    {
        var modes = (GrowerAssignmentMode[])Enum.GetValues(typeof(GrowerAssignmentMode));
        var cellWidth = width / modes.Length;
        var cellRect = new Rect(pos.x, pos.y, cellWidth, ListEntryHeight);

        foreach (var mode in modes)
        {
            Utilities.DrawToggle(
                cellRect,
                $"FarmingHysteresis.CMR.GrowerScope.{mode}".Translate(),
                $"FarmingHysteresis.CMR.GrowerScope.{mode}.Tip".Translate(),
                job.AssignmentMode == mode,
                () => job.AssignmentMode = mode,
                () => { },
                wrap: false
            );
            cellRect.x += cellWidth;
        }

        pos.y += ListEntryHeight;
    }

    private static float DrawGrowerScope(ManagerJob_FarmingHysteresis job, Vector2 pos, float width)
    {
        var start = pos;

        DrawAssignmentModeSelector(job, ref pos, width);

        switch (job.AssignmentMode)
        {
            case GrowerAssignmentMode.Area:
                global::ColonyManagerRedux.AreaAllowedGUI.DoAllowedAreaSelectors(
                    ref pos,
                    width,
                    ref job.GrowerArea,
                    5,
                    job.Manager
                );
                Utilities.DrawToggle(
                    ref pos,
                    width,
                    "ColonyManagerRedux.InvertArea".Translate(),
                    "ColonyManagerRedux.InvertArea.Tip".Translate(),
                    ref job.InvertGrowerArea
                );
                break;
            case GrowerAssignmentMode.Specific:
                pos.y += DrawSpecificGrowers(job, pos, width);
                break;
            case GrowerAssignmentMode.All:
            default:
                break;
        }

        return pos.y - start.y;
    }

    private static float DrawSpecificGrowers(
        ManagerJob_FarmingHysteresis job,
        Vector2 pos,
        float width
    )
    {
        var growers = FarmingHysteresisControlDef
            .AllControlledPlantGrowers(job.Manager.map)
            .ToList();

        if (growers.Count == 0)
        {
            var emptyRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
            Widgets.Label(emptyRect, "FarmingHysteresis.CMR.GrowerScope.NoEligibleGrowers".Translate());
            return ListEntryHeight;
        }

        var start = pos;
        var rowRect = new Rect(pos.x, pos.y, width, ListEntryHeight);
        foreach (var grower in growers)
        {
            if (Widgets_Section.CanCull(rowRect.y, rowRect.height))
            {
                rowRect.y += ListEntryHeight;
                continue;
            }

            var owningJob = ManagerJob_FarmingHysteresis.FindOwningJob(job.Manager, grower);
            var claimedByOtherJob = owningJob != null && owningJob != job;
            var label = ManagerJob_FarmingHysteresis.GrowerLabel(grower);

            if (claimedByOtherJob)
            {
                var wasEnabled = GUI.enabled;
                GUI.enabled = false;
                var allowed = IsSpecificallySelected(job, grower);
                Utilities.DrawToggle(
                    rowRect,
                    label,
                    "FarmingHysteresis.CMR.GrowerScope.ManagedByOtherJob".Translate(
                        owningJob!.TargetsLabel
                    ),
                    allowed,
                    () => { },
                    () => { }
                );
                GUI.enabled = wasEnabled;
            }
            else
            {
                var allowed = IsSpecificallySelected(job, grower);
                Utilities.DrawToggle(
                    rowRect,
                    label,
                    (TipSignal)label,
                    allowed,
                    () => SetSpecificallySelected(job, grower, true),
                    () => SetSpecificallySelected(job, grower, false)
                );
            }

            if (Mouse.IsOver(rowRect) && !Find.CameraDriver.IsPanning())
            {
                switch (grower)
                {
                    case Thing thing:
                        CameraJumper.TryJump(thing);
                        break;
                    case Zone zone when zone.Cells.Count > 0:
                        CameraJumper.TryJump(zone.Cells.First(), zone.Map);
                        break;
                    default:
                        break;
                }
            }

            rowRect.y += ListEntryHeight;
        }

        return rowRect.y - start.y;
    }

    private static bool IsSpecificallySelected(
        ManagerJob_FarmingHysteresis job,
        IPlantToGrowSettable grower
    ) =>
        grower switch
        {
            Zone_Growing zone => job.SpecificGrowingZones.Contains(zone),
            Building_PlantGrower building => job.SpecificPlantGrowerBuildings.Contains(building),
            _ => false,
        };

    private static void SetSpecificallySelected(
        ManagerJob_FarmingHysteresis job,
        IPlantToGrowSettable grower,
        bool selected
    )
    {
        switch (grower)
        {
            case Zone_Growing zone when selected:
                _ = job.SpecificGrowingZones.Add(zone);
                break;
            case Zone_Growing zone:
                _ = job.SpecificGrowingZones.Remove(zone);
                break;
            case Building_PlantGrower building when selected:
                _ = job.SpecificPlantGrowerBuildings.Add(building);
                break;
            case Building_PlantGrower building:
                _ = job.SpecificPlantGrowerBuildings.Remove(building);
                break;
            default:
                break;
        }
        job.Notify_TargetsChanged();
    }
}
