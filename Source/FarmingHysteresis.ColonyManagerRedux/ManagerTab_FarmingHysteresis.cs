using ColonyManagerRedux;
using FarmingHysteresis.Defs;
using ilyvion.Laboratory.Extensions;
using static ColonyManagerRedux.Constants;

namespace FarmingHysteresis.ColonyManagerRedux;

internal sealed class ManagerTab_FarmingHysteresis(Manager manager)
    : ManagerTab<ManagerJob_FarmingHysteresis, ManagerSettings_FarmingHysteresis>(manager)
{
    private const float TargetPlantIconSize = 24f;
    private const float TargetPlantRowPadding = 5f;

    /// <summary>
    /// Whenever the migration gate is still suppressing takeover for this save, show the migrate
    /// notice even with no job selected - there's nothing useful to configure in the meantime
    /// (see <see cref="DoMainContent"/>), unlike the base class's default of a blank panel.
    /// </summary>
    protected override bool DoMainContentWhenNothingSelected => IsMigrationPending;

    private static bool IsMigrationPending =>
        CmrMigrationGameComponent.CurrentStatus
            is CmrMigrationGateStatus.AwaitingChoice
                or CmrMigrationGateStatus.Declined;

    protected override void DoMainContent(Rect rect)
    {
        if (IsMigrationPending)
        {
            DrawMigrationPendingNotice(rect);
            return;
        }

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
        DrawSection(
            "FarmingHysteresis.Job",
            "TargetPlant",
            ref position,
            width,
            DrawTargetPlant,
            "FarmingHysteresis.CMR.TargetPlant".Translate()
        );
        DrawSection(
            "FarmingHysteresis.Job",
            "Trigger",
            ref position,
            width,
            DrawTriggerConfig,
            "FarmingHysteresis.CMR.Trigger".Translate()
        );
        Widgets_Section.EndSectionColumn("FarmingHysteresis.Job", position);
    }

    private static float DrawTargetPlant(ManagerJob_FarmingHysteresis job, Vector2 pos, float width)
    {
        var start = pos;
        var validTargetPlants = job.ValidTargetPlants.ToList();

        if (validTargetPlants.Count == 0)
        {
            job.TargetPlantDef = null;
            var message = "FarmingHysteresis.CMR.TargetPlant.NoneAvailable".Translate();
            var emptyHeight = Mathf.Max(ListEntryHeight, Text.CalcHeight(message, width));
            Widgets.Label(new Rect(pos.x, pos.y, width, emptyHeight), message);
            pos.y += emptyHeight;
            return pos.y - start.y;
        }

        if (job.TargetPlantDef != null && !validTargetPlants.Contains(job.TargetPlantDef))
        {
            job.TargetPlantDef = null;
        }

        pos.y += DrawTargetPlantRow(job, validTargetPlants, pos, width);

        return pos.y - start.y;
    }

    /// <summary>
    /// The plant picker itself, styled to match <c>Trigger_Hysteresis.DrawProductRow</c> (icon +
    /// label over a hover highlight, opened via an invisible button) instead of a plain
    /// <c>Widgets.ButtonText</c> - a generic-looking button doesn't read as well here as
    /// the growers' own "Plant: X" gizmo does. The FloatMenu options mirror vanilla's own
    /// <c>Command_SetPlantToGrow.ProcessInput</c> construction (icon via the
    /// <c>shownItemForIcon</c> overload, trailing info-card button) rather than plain text
    /// entries, so it looks and behaves the same as the grower's own plant picker.
    /// </summary>
    private static float DrawTargetPlantRow(
        ManagerJob_FarmingHysteresis job,
        List<ThingDef> validTargetPlants,
        Vector2 pos,
        float width
    )
    {
        var targetPlantDef = job.TargetPlantDef;
        var rowHeight = TargetPlantIconSize + (2 * TargetPlantRowPadding);
        var rowRect = new Rect(pos.x, pos.y, width, rowHeight);

        GUI.color = new Color(1f, 1f, 1f, 0.5f);
        Widgets.DrawHighlightIfMouseover(rowRect);
        GUI.color = Color.white;

        if (targetPlantDef != null)
        {
            var iconRect = new Rect(
                pos.x + TargetPlantRowPadding,
                pos.y + TargetPlantRowPadding,
                TargetPlantIconSize,
                TargetPlantIconSize
            );
            Widgets.DefIcon(iconRect, targetPlantDef);

            var labelRect = new Rect(
                iconRect.xMax + TargetPlantRowPadding,
                pos.y,
                width - iconRect.width - (3 * TargetPlantRowPadding),
                rowHeight
            );
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, targetPlantDef.LabelCap);
            Text.Anchor = TextAnchor.UpperLeft;
        }
        else
        {
            var labelRect = new Rect(
                pos.x + TargetPlantRowPadding,
                pos.y,
                width - (2 * TargetPlantRowPadding),
                rowHeight
            );
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(labelRect, "FarmingHysteresis.CMR.TargetPlant.None".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
        }

        if (Widgets.ButtonInvisible(rowRect, doMouseoverSound: false))
        {
            var options = validTargetPlants
                .Select(plantDef => new FloatMenuOption(
                    plantDef.LabelCap,
                    () => job.TargetPlantDef = plantDef,
                    plantDef,
                    null,
                    forceBasicStyle: false,
                    MenuOptionPriority.Default,
                    null,
                    null,
                    29f,
                    rect => Widgets.InfoCardButton(rect.x + 5f, rect.y + ((rect.height - 24f) / 2f), plantDef)
                ))
                .ToList();
            Find.WindowStack.Add(new FloatMenu(options));
        }

        return rowHeight;
    }

    private static float DrawTriggerConfig(ManagerJob_FarmingHysteresis job, Vector2 pos, float width)
    {
        var start = pos;
        job.HysteresisTrigger.DrawTriggerConfig(ref pos, width, ListEntryHeight);
        return pos.y - start.y;
    }

    /// <summary>
    /// Shown in place of the usual job-editing panel while <see cref="CmrMigrationGameComponent"/>
    /// is still suppressing takeover for this save (dialog not yet answered, or answered "no") -
    /// configuring jobs wouldn't do anything useful yet since the old always-on engine, not CMR,
    /// is the one actually controlling growers. Doubles as the on-demand retry the player asked
    /// for after declining the initial one-time dialog.
    /// </summary>
    private static void DrawMigrationPendingNotice(Rect rect)
    {
        Widgets.DrawMenuSection(rect);

        var innerRect = rect.ContractedBy(Margin);
        var message = "FarmingHysteresis.CMR.Migration.PendingNotice".Translate();
        var messageHeight = Text.CalcHeight(message, innerRect.width);
        Widgets.Label(new Rect(innerRect.x, innerRect.y, innerRect.width, messageHeight), message);

        var buttonRect = new Rect(
            innerRect.x,
            innerRect.y + messageHeight + Margin,
            ButtonSize.x,
            ButtonSize.y
        );
        if (Widgets.ButtonText(buttonRect, "FarmingHysteresis.CMR.Migration.MigrateNow".Translate()))
        {
            CmrMigrationGate.Migrate();
        }
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
            Zone zone => job.SpecificGrowingZones.Contains(zone),
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
            case Zone zone when selected:
                _ = job.SpecificGrowingZones.Add(zone);
                break;
            case Zone zone:
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
